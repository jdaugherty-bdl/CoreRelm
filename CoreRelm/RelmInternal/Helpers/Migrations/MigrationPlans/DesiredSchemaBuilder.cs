using CoreRelm.Attributes;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Migrations.MigrationPlans
{
    public sealed class DesiredSchemaBuilder : IRelmDesiredSchemaBuilder
    {
        public async Task<SchemaSnapshot> BuildAsync(string dbName, List<ValidatedModelType> modelsForDb)
        {
            var desired = Build(dbName, modelsForDb);

            return await Task.FromResult(desired);
        }

        public SchemaSnapshot Build(string databaseName, IReadOnlyList<ValidatedModelType> modelsForDb)
        {
            // modelsForDb already contains only models for this database
            var tables = new Dictionary<string, TableSchema>(StringComparer.Ordinal);

            // Resolve table schemas deterministically
            foreach (var model in modelsForDb
                         .OrderBy(m => m.TableName, StringComparer.Ordinal)
                         .ThenBy(m => m.ClrType.FullName, StringComparer.Ordinal))
            {
                var clrType = model.ClrType;

                // sanity: enforce attributes exist (your resolver already does this, but keep it safe)
                var dbAttr = clrType.GetCustomAttributes(true).OfType<RelmDatabase>().FirstOrDefault()
                             ?? throw new InvalidOperationException($"Missing [RelmDatabase] on {clrType.FullName}");
                var tblAttr = clrType.GetCustomAttributes(true).OfType<RelmTable>().FirstOrDefault()
                              ?? throw new InvalidOperationException($"Missing [RelmTable] on {clrType.FullName}");

                if (!string.Equals(dbAttr.DatabaseName, databaseName, StringComparison.Ordinal))
                    continue; // caller should have pre-grouped; skip just in case

                var tableName = tblAttr.TableName;

                // Precompute CLR property name -> DB column name map for this type
                var nameMap = BuildColumnNameMap(clrType);

                // Columns (properties tagged with [RelmColumn], including inherited)
                var columnProps = GetAllInstanceProperties(clrType)
                    .Select(p => (Prop: p, Attr: p.GetCustomAttributes(true).OfType<RelmColumn>().FirstOrDefault()))
                    .Where(x => x.Attr != null)
                    .ToList();

                var desiredColumns = new Dictionary<string, ColumnSchema>(StringComparer.Ordinal);

                // Index groups: indexName -> list of (columnName, descending)
                var indexGroups = new Dictionary<string, List<(string ColumnName, bool Desc)>>(StringComparer.Ordinal);

                // Ordinal position: base columns first in your template order, then remaining sorted
                // We’ll compute later after collecting all columns.
                foreach (var (prop, colAttr) in columnProps)
                {
                    var colName = ResolveColumnName(prop, colAttr!, nameMap);

                    // Map CLR type to MySQL type
                    var mysqlType = MySqlTypeMapper.ToMySqlType(prop.PropertyType, colAttr!);

                    var isNullable = colAttr!.IsNullable;
                    var isPk = colAttr.PrimaryKey;
                    var isAuto = colAttr.Autonumber;
                    var isUnique = colAttr.Unique;

                    // default value SQL
                    var defaultSql = colAttr.DefaultValue;

                    // Enforce your last_updated rule (no triggers; column semantics)
                    if (string.Equals(colName, "last_updated", StringComparison.OrdinalIgnoreCase))
                    {
                        // If DefaultValue is empty, we still want the ON UPDATE semantics.
                        // We store exactly what the renderer expects after "DEFAULT".
                        defaultSql = "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP";
                        isNullable = false;
                    }

                    if (string.Equals(colName, "create_date", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrWhiteSpace(defaultSql))
                            defaultSql = "CURRENT_TIMESTAMP";
                        isNullable = false;
                    }

                    // internal ordering computed later
                    desiredColumns[colName] = new ColumnSchema
                    {
                        ColumnName = colName,
                        ColumnType = mysqlType,
                        IsNullable = isNullable,
                        IsPrimaryKey = isPk,
                        IsForeignKey = false,
                        IsReadOnly = false,
                        IsUnique = isUnique,
                        IsAutoIncrement = isAuto,
                        DefaultValue = string.IsNullOrWhiteSpace(defaultSql) ? null : defaultSql,
                        //DefaultValueSql = string.IsNullOrWhiteSpace(defaultSql) ? null : defaultSql,
                        Extra = null,
                        OrdinalPosition = 0
                    };

                    // Index grouping
                    //if (!string.IsNullOrWhiteSpace(colAttr.Index))
                    if (colAttr.Index)
                    {
                        if (!indexGroups.TryGetValue(colAttr.ColumnName, out var list))
                        {
                            list = [];
                            indexGroups[colAttr.ColumnName] = list;
                        }

                        list.Add((colName, colAttr.IndexDescending));
                    }
                }

                // Assign ordinal positions deterministically
                var orderedCols = OrderColumns([.. desiredColumns.Values]);
                var ordinal = 1;
                foreach (var c in orderedCols)
                {
                    desiredColumns[c.ColumnName!] = c.Clone();
                    desiredColumns[c.ColumnName!].OrdinalPosition = ordinal++;
                }

                // Indexes from groups (non-unique by default; uniqueness comes from column Unique or PK)
                var desiredIndexes = new Dictionary<string, IndexSchema>(StringComparer.Ordinal);
                foreach (var (indexName, cols) in indexGroups.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    var ordered = cols
                        .Distinct()
                        .OrderBy(c => c.ColumnName, StringComparer.Ordinal)
                        .ToList();

                    var idxCols = new List<IndexColumnSchema>();
                    var seq = 1;
                    foreach (var c in ordered)
                    {
                        idxCols.Add(new IndexColumnSchema(
                            ColumnName: c.ColumnName,
                            SeqInIndex: seq++,
                            Collation: c.Desc ? "D" : "A"
                        ));
                    }

                    desiredIndexes[indexName] = new IndexSchema
                    {
                        IndexName = indexName,
                        IsUnique = false,
                        Columns = idxCols
                    };
                }

                // Foreign keys: navigation properties with [RelmForeignKey]
                var desiredFks = new Dictionary<string, ForeignKeySchema>(StringComparer.Ordinal);

                foreach (var navProp in GetAllInstanceProperties(clrType))
                {
                    var fkAttr = navProp.GetCustomAttributes(true).OfType<RelmForeignKey>().FirstOrDefault();
                    if (fkAttr is null) continue;

                    // Navigation property type must be a model type
                    var principalType = navProp.PropertyType;

                    var principalDbAttr = principalType.GetCustomAttributes(true).OfType<RelmDatabase>().FirstOrDefault()
                                          ?? throw new InvalidOperationException($"Missing [RelmDatabase] on principal type {principalType.FullName} referenced by {clrType.FullName}.{navProp.Name}");

                    var principalTblAttr = principalType.GetCustomAttributes(true).OfType<RelmTable>().FirstOrDefault()
                                           ?? throw new InvalidOperationException($"Missing [RelmTable] on principal type {principalType.FullName} referenced by {clrType.FullName}.{navProp.Name}");

                    if (!string.Equals(principalDbAttr.DatabaseName, databaseName, StringComparison.Ordinal))
                        throw new InvalidOperationException($"Cross-database FK not supported: {databaseName}.{tableName} -> {principalDbAttr.DatabaseName}.{principalTblAttr.TableName}");

                    if (fkAttr.LocalKeys is null || fkAttr.ForeignKeys is null)
                        throw new InvalidOperationException($"[RelmForeignKey] on {clrType.FullName}.{navProp.Name} must specify LocalKeys and ForeignKeys.");

                    if (fkAttr.LocalKeys.Length != fkAttr.ForeignKeys.Length)
                        throw new InvalidOperationException($"[RelmForeignKey] on {clrType.FullName}.{navProp.Name} has mismatched key counts.");

                    // Resolve local columns from CLR property names
                    var localCols = new List<string>();
                    foreach (var localClrName in fkAttr.LocalKeys)
                    {
                        var localProp = FindProperty(clrType, localClrName);
                        var localColAttr = localProp.GetCustomAttributes(true).OfType<RelmColumn>().FirstOrDefault()
                                           ?? throw new InvalidOperationException($"Local key '{localClrName}' on {clrType.FullName}.{navProp.Name} must have [RelmColumn].");

                        localCols.Add(ResolveColumnName(localProp, localColAttr, nameMap));
                    }

                    // Resolve referenced columns from principal CLR property names
                    var principalNameMap = BuildColumnNameMap(principalType);
                    var refCols = new List<string>();
                    foreach (var refClrName in fkAttr.ForeignKeys)
                    {
                        var refProp = FindProperty(principalType, refClrName);
                        var refColAttr = refProp.GetCustomAttributes(true).OfType<RelmColumn>().FirstOrDefault()
                                         ?? throw new InvalidOperationException($"Foreign key '{refClrName}' on {clrType.FullName}.{navProp.Name} must have [RelmColumn] on {principalType.FullName}.");

                        refCols.Add(ResolveColumnName(refProp, refColAttr, principalNameMap));
                    }

                    var fkName = $"fk_{tableName}_{ToSnake(navProp.Name)}";

                    desiredFks[fkName] = new ForeignKeySchema
                    {
                        ConstraintName = fkName,
                        TableName = tableName,
                        ColumnNames = localCols,
                        ReferencedTableName = principalTblAttr.TableName,
                        ReferencedColumnNames = refCols,
                        UpdateRule = "RESTRICT",
                        DeleteRule = "CASCADE"
                    };
                }

                // Triggers: leave empty; planner will inject InternalId trigger (and function) rules
                var desiredTriggers = new Dictionary<string, TriggerSchema>(StringComparer.Ordinal);

                tables[tableName] = new TableSchema(
                    TableName: tableName,
                    Columns: desiredColumns,
                    Indexes: desiredIndexes,
                    ForeignKeys: desiredFks,
                    Triggers: desiredTriggers
                );
            }

            return new SchemaSnapshot(databaseName, tables, null);
        }

        private static PropertyInfo FindProperty(Type t, string name)
        {
            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) 
                ?? throw new InvalidOperationException($"Property '{name}' not found on type {t.FullName}.");

            return p;
        }

        private static List<PropertyInfo> GetAllInstanceProperties(Type t)
        {
            var props = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);

            var current = t;
            while (current != null && current != typeof(object))
            {
                foreach (var p in current.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (!props.ContainsKey(p.Name))
                        props[p.Name] = p;
                }
                current = current.BaseType;
            }

            return [.. props.Values];
        }

        private static Dictionary<string, string> BuildColumnNameMap(Type t)
        {
            // Map CLR property name -> DB column name using your CoreRelm naming rule.
            var map = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var p in GetAllInstanceProperties(t))
            {
                map[p.Name] = ConvertPropertyNameToDbColumn(p.Name);
            }

            return map;
        }

        private static string ResolveColumnName(PropertyInfo prop, RelmColumn attr, Dictionary<string, string> nameMap)
        {
            if (!string.IsNullOrWhiteSpace(attr.ColumnName))
                return attr.ColumnName;

            return nameMap.TryGetValue(prop.Name, out var v) ? v : ConvertPropertyNameToDbColumn(prop.Name);
        }

        private static List<ColumnSchema> OrderColumns(List<ColumnSchema> cols)
        {
            // Base template order then by name
            int Rank(string name) => name switch
            {
                "id" => 0,
                "active" => 1,
                "InternalId" => 2,
                "create_date" => 3,
                "last_updated" => 4,
                _ => 100
            };

            return [.. cols
                .OrderBy(c => Rank(c.ColumnName))
                .ThenBy(c => c.ColumnName, StringComparer.OrdinalIgnoreCase)];
        }

        private static string ConvertPropertyNameToDbColumn(string clrName)
        {
            // Special case
            if (clrName == "InternalId")
                return "InternalId";

            // If it ends with InternalId, keep suffix as "InternalId" and underscore-prefix part
            const string suffix = "InternalId";
            if (clrName.EndsWith(suffix, StringComparison.Ordinal) && clrName.Length > suffix.Length)
            {
                var prefix = clrName[..^suffix.Length];
                var snakePrefix = ToSnake(prefix);
                return $"{snakePrefix}_InternalId";
            }

            return ToSnake(clrName);
        }

        private static string ToSnake(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                var ch = name[i];
                if (char.IsUpper(ch))
                {
                    if (i > 0) sb.Append('_');
                    sb.Append(char.ToLowerInvariant(ch));
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }

        private static class MySqlTypeMapper
        {
            public static string ToMySqlType(Type clrType, RelmColumn col)
            {
                // unwrap Nullable<T>
                var t = Nullable.GetUnderlyingType(clrType) ?? clrType;

                if (t == typeof(string))
                {
                    var size = col.ColumnSize > 0 ? col.ColumnSize : 255;
                    return $"varchar({size})";
                }

                if (t == typeof(int)) return "int";
                if (t == typeof(long)) return "bigint";
                if (t == typeof(short)) return "smallint";
                if (t == typeof(byte)) return "tinyint unsigned";
                if (t == typeof(bool)) return "tinyint(1)";
                if (t == typeof(DateTime)) return "datetime";
                if (t == typeof(DateTimeOffset)) return "datetime";
                if (t == typeof(decimal))
                {
                    var p = 18;
                    var s = 2;
                    if (col.CompoundColumnSize is { Length: >= 2 })
                    {
                        p = col.CompoundColumnSize[0];
                        s = col.CompoundColumnSize[1];
                    }
                    return $"decimal({p},{s})";
                }
                if (t == typeof(double)) return "double";
                if (t == typeof(float)) return "float";
                if (t == typeof(Guid)) return "varchar(45)";
                if (t == typeof(byte[])) return "blob";

                // If you hit this, add a mapping or provide a custom mapper hook.
                throw new NotSupportedException($"No MySQL type mapping for CLR type {t.FullName} (property uses [RelmColumn]).");
            }
        }
    }
}
