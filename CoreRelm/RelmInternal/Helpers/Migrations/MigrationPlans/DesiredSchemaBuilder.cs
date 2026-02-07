using CoreRelm.Attributes;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using CoreRelm.RelmInternal.Extensions;
using CoreRelm.RelmInternal.Helpers.Migrations.Rendering;
using CoreRelm.RelmInternal.Models;
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
                var columnProperties = GetAllInstanceProperties(clrType)
                    .Select(p => (PropertyDetails: p, ColumnDetails: p.GetCustomAttributes(true).OfType<RelmColumn>().FirstOrDefault()))
                    .Where(x => x.ColumnDetails != null)
                    .ToList();

                var desiredColumns = new Dictionary<string, ColumnSchema>(StringComparer.Ordinal);

                // Index groups: indexName -> list of (columnName, descending)
                var indexGroups = new Dictionary<string, List<(string ColumnName, RelmIndex IndexDefinition)>>(StringComparer.Ordinal);

                // get all relmindexes, both on class and on properties
                var propertyRelmIndexes = GetAllInstanceProperties(clrType)
                        .Select(p => (IndexDetails: p.GetCustomAttributes(true).OfType<RelmIndex>().FirstOrDefault(), PropertyDetails: p))
                        .Where(p => p.IndexDetails != null)
                        .Select(p => (IndexDetails: (RelmIndex)p.IndexDetails!, PropertyDetails: (PropertyInfo?)p.PropertyDetails)) // reselect so we get the nice naming
                        .ToList()
                        ?? [];

                var compositeIndexes = model.ClrType
                    .GetCustomAttributes(true)
                    .OfType<RelmIndex>()
                    .Select(x => (IndexDetails: x, PropertyDetails: (PropertyInfo?)null))
                    .Concat(propertyRelmIndexes)
                    .Where(x => x.IndexDetails != null)
                    .ToList();

                // Process composite indexes first
                foreach (var (indexDetails, propertyDetails) in compositeIndexes)
                {
                    var columnNames = new List<(string ColumnName, RelmIndex IndexDefinition)>();

                    if ((indexDetails.IndexedProperties is null || indexDetails.IndexedProperties.Length == 0) && propertyDetails is null)
                        throw new InvalidOperationException($"RelmIndex on type {clrType.FullName} must either be attached to a property or specify IndexedProperties.");
                    
                    var propertiesNames = indexDetails.IndexedProperties ?? [propertyDetails!.Name];
                    foreach (var indexProperty in propertiesNames)
                    {
                        var (columnPropertyDetails, columnDetails) = columnProperties.FirstOrDefault(x => x.PropertyDetails.Name == indexProperty);
                        if (columnDetails is null)
                            throw new InvalidOperationException($"Unable to resolve column for index on property {(propertyDetails?.Name ?? "'class'")} in type {clrType.FullName}");

                        var columnName = ResolveColumnName(columnPropertyDetails, columnDetails!, nameMap)
                            ?? throw new InvalidOperationException($"Unable to resolve column name for {columnPropertyDetails.Name}");

                        columnNames.Add((columnName, indexDetails));
                    }

                    var indexName = indexDetails.IndexName ?? $"IX_{tableName}_{string.Join("_", columnNames.Select(x => x.ColumnName))}";
                    if (!indexGroups.ContainsKey(indexName))
                        indexGroups.Add(indexName, []);
                    
                    indexGroups[indexName] = columnNames;
                }


                // Ordinal position: base columns first in your template order, then remaining sorted
                // We’ll compute later after collecting all columns.
                foreach (var (columnProperty, columnAttribute) in columnProperties)
                {
                    var columnName = ResolveColumnName(columnProperty, columnAttribute!, nameMap)
                        ?? throw new InvalidOperationException($"Unable to resolve column name for {columnProperty.Name}");

                    // Map CLR type to MySQL type
                    var mysqlType = MySqlTypeMapper.ToMySqlType(columnProperty.PropertyType, columnAttribute!);

                    var isNullable = columnAttribute!.IsNullable;
                    var isPrimaryKey = columnAttribute.PrimaryKey;
                    var isAutonumber = columnAttribute.Autonumber;
                    var isUnique = columnAttribute.Unique;

                    // default value SQL
                    var defaultSql = columnAttribute.DefaultValue;

                    // internal ordering computed later
                    desiredColumns[columnName] = new ColumnSchema
                    {
                        ColumnName = columnName,
                        ColumnType = mysqlType,
                        IsNullable = isNullable ? "YES" : "NO",
                        IsPrimaryKey = isPrimaryKey,
                        IsForeignKey = false,
                        IsReadOnly = false,
                        IsUnique = isUnique,
                        IsAutoIncrement = isAutonumber,
                        DefaultValue = string.IsNullOrWhiteSpace(defaultSql) ? null : defaultSql,
                        Extra = null,
                        OrdinalPosition = 0
                    };

                    // single column indexes
                    if (columnAttribute.Index)
                    {
                        var indexName = $"IX_{tableName}_{columnAttribute.ColumnName ?? columnName}";
                        if (!indexGroups.TryGetValue(indexName, out var list))
                        {
                            list = [];
                            indexGroups[indexName] = list;
                        }

                        list.Add((columnName, new() { IndexedProperties = [columnProperty.Name] }));
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
                foreach (var (indexName, indexColumns) in indexGroups.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    var orderedIndexColumns = indexColumns
                        .Distinct()
                        .OrderBy(c => c.ColumnName, StringComparer.Ordinal)
                        .ToList();

                    var indexSchemas = new List<IndexColumnSchema>();
                    var sequenceInIndex = 1;
                    foreach (var (ColumnName, IndexDescriptor) in orderedIndexColumns)
                    {
                        indexSchemas.Add(new IndexColumnSchema(
                            ColumnName: ColumnName,
                            SeqInIndex: sequenceInIndex++,
                            Collation: IndexDescriptor.Descending ? "DESC" : "ASC"
                        ));
                    }

                    var isUnique = indexColumns.Any(c => c.IndexDefinition.Unique);
                    desiredIndexes[indexName] = new IndexSchema
                    {
                        IndexName = indexName,
                        IsUnique = isUnique,
                        Columns = indexSchemas
                    };
                }

                // Foreign keys: navigation properties with [RelmForeignKey]
                var desiredForeignKeys = new Dictionary<string, ForeignKeySchema>(StringComparer.Ordinal);
                foreach (var navProp in GetAllInstanceProperties(clrType))
                {
                    var foreignKeyAttr = navProp.GetCustomAttributes(true).OfType<RelmForeignKey>().FirstOrDefault();
                    if (foreignKeyAttr is null) 
                        continue;

                    // Navigation property type must be a model type
                    var principalType = navProp.PropertyType;
                    // get either navProp.PropertyType, or if it's a collection, the generic argument type
                    if (principalType.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(principalType))
                    {
                        var genericArgs = principalType.GetGenericArguments();
                        if (genericArgs.Length == 1)
                        {
                            principalType = genericArgs[0];
                        }
                    }

                    var principalDbAttr = principalType.GetCustomAttributes(true).OfType<RelmDatabase>().FirstOrDefault()
                        ?? throw new InvalidOperationException($"Missing [RelmDatabase] on principal type {principalType.FullName} referenced by {clrType.FullName}.{navProp.Name}");

                    var principalTblAttr = principalType.GetCustomAttributes(true).OfType<RelmTable>().FirstOrDefault()
                        ?? throw new InvalidOperationException($"Missing [RelmTable] on principal type {principalType.FullName} referenced by {clrType.FullName}.{navProp.Name}");

                    if (!string.Equals(principalDbAttr.DatabaseName, databaseName, StringComparison.Ordinal))
                        throw new InvalidOperationException($"Cross-database FK not supported: {databaseName}.{tableName} -> {principalDbAttr.DatabaseName}.{principalTblAttr.TableName}");

                    if (foreignKeyAttr.LocalKeys is null || foreignKeyAttr.ForeignKeys is null)
                        throw new InvalidOperationException($"[RelmForeignKey] on {clrType.FullName}.{navProp.Name} must specify LocalKeys and ForeignKeys.");

                    if (foreignKeyAttr.LocalKeys.Length != foreignKeyAttr.ForeignKeys.Length)
                        throw new InvalidOperationException($"[RelmForeignKey] on {clrType.FullName}.{navProp.Name} has mismatched key counts.");

                    // Resolve local columns from CLR property names
                    var localCols = new List<string?>();
                    foreach (var localClrName in foreignKeyAttr.LocalKeys)
                    {
                        var localProp = FindProperty(clrType, localClrName);
                        var localColAttr = localProp.GetCustomAttributes(true).OfType<RelmColumn>().FirstOrDefault()
                                           ?? throw new InvalidOperationException($"Local key '{localClrName}' on {clrType.FullName}.{navProp.Name} must have [RelmColumn].");

                        localCols.Add(ResolveColumnName(localProp, localColAttr, nameMap));
                    }

                    // Resolve referenced columns from principal CLR property names
                    var principalNameMap = BuildColumnNameMap(principalType);
                    var refCols = new List<string?>();
                    foreach (var refClrName in foreignKeyAttr.ForeignKeys)
                    {
                        var refProp = FindProperty(principalType, refClrName);
                        var refColAttr = refProp.GetCustomAttributes(true).OfType<RelmColumn>().FirstOrDefault()
                                         ?? throw new InvalidOperationException($"Foreign key '{refClrName}' on {clrType.FullName}.{navProp.Name} must have [RelmColumn] on {principalType.FullName}.");

                        refCols.Add(ResolveColumnName(refProp, refColAttr, principalNameMap));
                    }

                    var fkName = $"FK_{tableName}_{UnderscoreNamesHelper.ConvertPropertyToUnderscoreName(navProp, forceLowerCase: true)}";

                    desiredForeignKeys[fkName] = new ForeignKeySchema
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
                    ForeignKeys: desiredForeignKeys,
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

        private static Dictionary<string, string?> BuildColumnNameMap(Type t)
        {
            // Map CLR property name -> DB column name using your CoreRelm naming rule.
            var map = new Dictionary<string, string?>(StringComparer.Ordinal);

            foreach (var p in GetAllInstanceProperties(t))
            {
                map[p.Name] = UnderscoreNamesHelper.ConvertPropertyToUnderscoreName(p, forceLowerCase: true);
            }

            return map;
        }

        private static string? ResolveColumnName(PropertyInfo prop, RelmColumn attr, Dictionary<string, string?> nameMap)
        {
            if (!string.IsNullOrWhiteSpace(attr.ColumnName))
                return attr.ColumnName;

            return nameMap.TryGetValue(prop.Name, out var v) ? v : UnderscoreNamesHelper.ConvertPropertyToUnderscoreName(prop, forceLowerCase: true);
        }

        private static List<ColumnSchema> OrderColumns(List<ColumnSchema> cols)
        {
            // Base template order then by name
            int Rank(string? name) => name switch
            {
                "id" => 0,
                "active" => 100,
                "InternalId" => 120,
                "create_date" => 130,
                "last_updated" => 140,
                _ => 10
            };

            return [.. cols
                .OrderBy(c => Rank(c.ColumnName))];
                //.ThenBy(c => c.ColumnName, StringComparer.OrdinalIgnoreCase)];
        }
    }
}
