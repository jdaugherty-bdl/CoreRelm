using CoreRelm.Attributes;
using CoreRelm.Attributes.BaseClasses;
using CoreRelm.Exceptions;
using CoreRelm.Extensions;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using CoreRelm.RelmInternal.Extensions;
using CoreRelm.RelmInternal.Helpers.Migrations.Rendering;
using CoreRelm.RelmInternal.Models;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Indexes;

namespace CoreRelm.RelmInternal.Helpers.Migrations.MigrationPlans
{
    public sealed class DesiredSchemaBuilder : IRelmDesiredSchemaBuilder
    {
        public async Task<SchemaSnapshot> BuildAsync(string databaseFilter, List < ValidatedModelType> modelsForDb)
        {
            var desired = Build(databaseFilter, modelsForDb);

            return await Task.FromResult(desired);
        }

        public SchemaSnapshot Build(string databaseFilter, IReadOnlyList<ValidatedModelType> modelsForDb)
        {
            // modelsForDb already contains only models for this database
            var tables = new Dictionary<string, TableSchema>(StringComparer.Ordinal);

            // Resolve table schemas deterministically
            foreach (var model in modelsForDb
                         .OrderBy(m => m.TableName, StringComparer.Ordinal)
                         .ThenBy(m => m.ClrType.FullName, StringComparer.Ordinal))
            {
                if (model == null)
                    continue;

                var modelClrType = model.ClrType;

                // sanity: enforce attributes exist (your resolver already does this, but keep it safe)
                /*
                var dbAttr = modelClrType.GetCustomAttributes(true).OfType<RelmDatabase>().FirstOrDefault()
                             ?? throw new InvalidOperationException($"Missing [RelmDatabase] on {modelClrType.FullName}");
                var tblAttr = modelClrType.GetCustomAttributes(true).OfType<RelmTable>().FirstOrDefault()
                              ?? throw new InvalidOperationException($"Missing [RelmTable] on {modelClrType.FullName}");

                if (!string.Equals(dbAttr.DatabaseName, databaseName, StringComparison.Ordinal))
                    continue; // caller should have pre-grouped; skip just in case

                var tableName = tblAttr.TableName;
                */
                var dbAttr = GetTypeAttribute(modelClrType, nameof(RelmDatabase))
                    ?? throw new MissingRelmDatabaseAttributeException(modelClrType);
                var tableAttr = GetTypeAttribute(modelClrType, nameof(RelmTable))
                    ?? throw new MissingRelmTableAttributeException(modelClrType);

                var databaseName = GetStringProperty(dbAttr, nameof(RelmDatabase.DatabaseName))
                    ?? throw new InvalidOperationException($"[{dbAttr?.GetType().Name}] on {modelClrType.FullName} must expose DatabaseName.");
                var tableName = GetStringProperty(tableAttr, nameof(RelmTable.TableName))
                    ?? throw new InvalidOperationException($"[{tableAttr.GetType().Name}] on {modelClrType.FullName} must expose TableName.");

                if (!string.Equals(databaseName, databaseFilter, StringComparison.Ordinal))
                    continue;

                // Precompute CLR property name -> DB column name map for this type
                var nameMap = BuildColumnNameMap(modelClrType);

                // Columns (properties tagged with [RelmColumn], including inherited)
                var columnProperties = GetAllInstanceProperties(modelClrType)
                    .Select(p => (PropertyDetails: p, ColumnDetails: p.GetCustomAttributes(true).OfType<RelmColumn>().FirstOrDefault()))
                    .Where(x => x.ColumnDetails != null)
                    .ToList();

                var compositeIndexColumnsList = model.ClrType
                    .GetCustomAttributes(true)
                    .OfType<RelmIndexColumnBase>()
                    .Where(x => x != null)
                    .ToList();

                var compositeIndexColumns = GetCompositeIndexColumns(compositeIndexColumnsList);

                // get all composite relmindexes, both on class and on properties
                /* We want to support:
                   - Property-level: [RelmIndex] on properties, which implicitly index that property (column) and can be used for single-column indexes or to group multiple columns into the same index via IndexKey.
                   - Class-level, w/Columns: [RelmIndex] on the class, which must specify IndexedProperties to indicate which properties (columns) are part of the index, and can be used for composite indexes without needing IndexKey.
                   - Class-level, Unique: [RelmUnique] on the class, which must specify ConstraintProperties to indicate which properties (columns) are part of the unique constraint/index.
                   For property-level indexes, we collect them and group by IndexKey (if specified) to allow them to be combined into composite indexes. If IndexKey is not specified, they are treated as single-column indexes.
                   For class-level indexes, we directly use the specified IndexedProperties. If IndexedPropertyNames is used instead, we resolve those to properties and then to columns.
                   This approach allows flexibility in how indexes are defined while ensuring we can build the desired schema accurately.
                   NOTE: [RelmColumn(index: true)] on a property is a shorthand for creating an index on that single column, and it detected and handled below while parsing [RelmColum] attributes.
                */
                var compositeIndexes = GetPropertyIndexes([], modelClrType);
                compositeIndexes = GetClassIndexes(compositeIndexes, compositeIndexColumns, model);
                compositeIndexes = GetUniqueIndexes(compositeIndexes, model);

                // group all composite index columns by index key
                var indexGroups = GroupCompositeIndexes(compositeIndexes, columnProperties, nameMap, model);

                // Ordinal position: base columns first in your template order, then remaining sorted
                // We’ll compute later after collecting all columns.
                var desiredColumns = new Dictionary<string, ColumnSchema>(StringComparer.Ordinal);
                foreach (var (columnProperty, columnAttribute) in columnProperties)
                {
                    var columnName = ResolveColumnName(columnProperty, columnAttribute!, nameMap)
                        ?? throw new InvalidOperationException($"Unable to resolve column name for {columnProperty.Name}");

                    // Map CLR type to MySQL type
                    var mysqlType = MySqlTypeMapper.ToMySqlType((Nullable.GetUnderlyingType(columnProperty.PropertyType) ?? columnProperty.PropertyType).BaseType == typeof(Enum) ? (Nullable.GetUnderlyingType(columnProperty.PropertyType) ?? columnProperty.PropertyType).BaseType : columnProperty.PropertyType, columnAttribute!);

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
                        //.OrderBy(c => c.ColumnName, StringComparer.Ordinal)
                        .ToList();

                    var indexColumnSchemas = new List<IndexColumnSchema>();
                    var sequenceInIndex = 1;
                    foreach (var (ColumnName, IndexDescriptor) in orderedIndexColumns)
                    {
                        var columnProperty = IndexDescriptor.IndexedProperties?.FirstOrDefault(x => x.ColumnName == ColumnName);
                        indexColumnSchemas.Add(new IndexColumnSchema(
                            ColumnName: ColumnName,
                            SubPart: columnProperty == null ? null : $"({columnProperty.Length})",
                            Collation: IndexDescriptor.Descending ? "DESC" : "ASC",
                            Expression: columnProperty?.Expression,
                            SeqInIndex: sequenceInIndex++
                        ));
                    }

                    var isUnique = indexColumns.Any(c => c.IndexDefinition.IndexTypeValue == IndexType.UNIQUE);
                    desiredIndexes[indexName] = new IndexSchema
                    {
                        IndexName = indexName,
                        IndexTypeValue = indexColumns.All(x => x.IndexDefinition.IndexTypeValue == IndexType.UNIQUE) ? IndexType.UNIQUE : (indexColumns.FirstOrDefault().IndexDefinition.IndexTypeValue ?? IndexType.None),
                        Columns = indexColumnSchemas
                    };
                }

                // Foreign keys: navigation properties with [RelmForeignKey]
                var desiredForeignKeys = new Dictionary<string, ForeignKeySchema>(StringComparer.Ordinal);
                foreach (var navProp in GetAllInstanceProperties(modelClrType))
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
                        ?? throw new InvalidOperationException($"Missing [RelmDatabase] on principal type {principalType.FullName} referenced by {modelClrType.FullName}.{navProp.Name}");

                    var principalTblAttr = principalType.GetCustomAttributes(true).OfType<RelmTable>().FirstOrDefault()
                        ?? throw new InvalidOperationException($"Missing [RelmTable] on principal type {principalType.FullName} referenced by {modelClrType.FullName}.{navProp.Name}");

                    if (!string.Equals(principalDbAttr.DatabaseName, databaseName, StringComparison.Ordinal))
                        throw new InvalidOperationException($"Cross-database FK not supported: {databaseName}.{tableName} -> {principalDbAttr.DatabaseName}.{principalTblAttr.TableName}");

                    if (foreignKeyAttr.LocalKeys is null || foreignKeyAttr.ForeignKeys is null)
                        throw new InvalidOperationException($"[RelmForeignKey] on {modelClrType.FullName}.{navProp.Name} must specify LocalKeys and ForeignKeys.");

                    if (foreignKeyAttr.LocalKeys.Length != foreignKeyAttr.ForeignKeys.Length)
                        throw new InvalidOperationException($"[RelmForeignKey] on {modelClrType.FullName}.{navProp.Name} has mismatched key counts.");

                    // Resolve local columns from CLR property names
                    var localCols = new List<string?>();
                    foreach (var localClrName in foreignKeyAttr.LocalKeys)
                    {
                        var localProp = FindProperty(modelClrType, localClrName);
                        var localColAttr = localProp.GetCustomAttributes(true).OfType<RelmColumn>().FirstOrDefault()
                                           ?? throw new InvalidOperationException($"Local key '{localClrName}' on {modelClrType.FullName}.{navProp.Name} must have [RelmColumn].");

                        localCols.Add(ResolveColumnName(localProp, localColAttr, nameMap));
                    }

                    // Resolve referenced columns from principal CLR property names
                    var principalNameMap = BuildColumnNameMap(principalType);
                    var refCols = new List<string?>();
                    foreach (var refClrName in foreignKeyAttr.ForeignKeys)
                    {
                        var refProp = FindProperty(principalType, refClrName);
                        var refColAttr = refProp.GetCustomAttributes(true).OfType<RelmColumn>().FirstOrDefault()
                                         ?? throw new InvalidOperationException($"Foreign key '{refClrName}' on {modelClrType.FullName}.{navProp.Name} must have [RelmColumn] on {principalType.FullName}.");

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

                // Triggers
                var classTriggers = GetClassTriggers(model);
                var desiredTriggers = GroupTriggers(classTriggers, model);

                tables[tableName] = new TableSchema(
                    TableName: tableName,
                    Columns: desiredColumns,
                    Indexes: desiredIndexes,
                    ForeignKeys: desiredForeignKeys,
                    Triggers: desiredTriggers
                );
            }

            return new SchemaSnapshot(databaseFilter, tables, null);
        }

        private static Attribute? GetTypeAttribute(Type t, string shortName)
            => t.GetCustomAttributes(inherit: true).OfType<Attribute>()
                .FirstOrDefault(a => a.GetType().Name is var n &&
                                     (n.Equals(shortName, StringComparison.Ordinal) || n.Equals(shortName + "Attribute", StringComparison.Ordinal)));

        private static Attribute? GetMemberAttribute(MemberInfo member, string shortName)
            => member.GetCustomAttributes(inherit: true).OfType<Attribute>()
                .FirstOrDefault(a => a.GetType().Name is var n &&
                                     (n.Equals(shortName, StringComparison.Ordinal) || n.Equals(shortName + "Attribute", StringComparison.Ordinal)));

        private static string? GetStringProperty(Attribute? attribute, string propName)
        {
            if (attribute == null)
                return null;

            var attributeProperty = attribute
                .GetType()
                .GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);

            if (attributeProperty == null)
                return null;

            return attributeProperty.PropertyType == typeof(string)
                ? (string?)attributeProperty.GetValue(attribute)
                : (typeof(Enum).IsAssignableFrom(Nullable.GetUnderlyingType(attributeProperty.PropertyType) ?? attributeProperty.PropertyType)
                    ? ((Enum?)attributeProperty.GetValue(attribute))?.ToString()
                    : null);
        }

        private static bool? GetBoolProperty(Attribute? attr, string propName)
        {
            var p = attr.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (p?.PropertyType == typeof(bool)) return (bool?)p.GetValue(attr);
            if (p?.PropertyType == typeof(bool?)) return (bool?)p.GetValue(attr);
            return null;
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

        private static List<KeyValuePair<object?, List<RelmIndexColumnBase>>> GetCompositeIndexColumns(List<RelmIndexColumnBase> compositeIndexColumnsList)
        {
            var compositeIndexColumns = new List<KeyValuePair<object?, List<RelmIndexColumnBase>>>();

            foreach (var compositeIndexColumn in compositeIndexColumnsList)
            {
                // bring IndexKey values down to RelmIndexColumnBase so we can use them when building indexes, without needing to know the generic type parameter of RelmIndex<T>
                // Access inherited members declared on RelmIndex<T> via the runtime type, not RelmIndexBase
                compositeIndexColumn.IndexKeyHolder = compositeIndexColumn
                    .GetType()
                    .GetProperty(nameof(RelmIndexColumn<object>.IndexKey), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetValue(compositeIndexColumn);

                if (!compositeIndexColumns.Any(x => Equals(x.Key, compositeIndexColumn.IndexKeyHolder)))
                    compositeIndexColumns.Add(new KeyValuePair<object?, List<RelmIndexColumnBase>>(compositeIndexColumn.IndexKeyHolder, []));

                compositeIndexColumns.First(x => Equals(x.Key, compositeIndexColumn.IndexKeyHolder)).Value.Add(compositeIndexColumn);
            }

            return compositeIndexColumns;
        }

        private static List<(RelmIndexBase IndexDetails, PropertyInfo? PropertyDetails)> GetPropertyIndexes(List<(RelmIndexBase IndexDetails, PropertyInfo? PropertyDetails)> compositeIndexes, Type modelClrType)
        {
            var instanceProperties = GetAllInstanceProperties(modelClrType);
            foreach (var instanceProperty in instanceProperties)
            {
                var indexAttr = instanceProperty.GetCustomAttributes(true).OfType<RelmIndexBase>().FirstOrDefault();
                if (indexAttr == null)
                    continue;

                // bring IndexKey values down to RelmIndexColumnBase so we can use them when building indexes, without needing to know the generic type parameter of RelmIndex<T>
                // Access inherited members declared on RelmIndex<T> via the runtime type, not RelmIndexBase
                indexAttr.IndexKeyHolder = indexAttr
                    .GetType()
                    .GetProperty(nameof(RelmIndex<object>.IndexKey), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetValue(indexAttr);

                compositeIndexes.Add((indexAttr, instanceProperty));
            }

            return compositeIndexes;
        }

        private static List<(RelmIndexBase IndexDetails, PropertyInfo? PropertyDetails)> GetClassIndexes(List<(RelmIndexBase IndexDetails, PropertyInfo? PropertyDetails)> compositeIndexes, List<KeyValuePair<object?, List<RelmIndexColumnBase>>> compositeIndexColumns, ValidatedModelType model)
        {
            var classIndexes = model.ClrType.GetCustomAttributes(true).OfType<RelmIndexBase>().ToList();
            foreach (var classIndex in classIndexes)
            {
                // bring IndexKey values down to RelmIndexColumnBase so we can use them when building indexes, without needing to know the generic type parameter of RelmIndex<T>
                // Access inherited members declared on RelmIndex<T> via the runtime type, not RelmIndexBase
                classIndex.IndexKeyHolder = classIndex
                    .GetType()
                    .GetProperty(nameof(RelmIndex<object>.IndexKey), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetValue(classIndex);

                classIndex.IndexedProperties ??= compositeIndexColumns.FirstOrDefault(x => Equals(x.Key, classIndex.IndexKeyHolder)).Value;

                compositeIndexes.Add((classIndex, null));

                if ((classIndex.IndexedPropertyNames?.Length ?? 0) == 0)
                    continue;

                classIndex.IndexedProperties = classIndex.IndexedPropertyNames
                    ?.Select(name => new RelmIndexColumnBase(name) { IndexKeyHolder = classIndex.IndexKeyHolder })
                    .ToList();

                if ((classIndex.IndexedProperties?.Count ?? 0) == 0)
                    throw new InvalidOperationException($"RelmIndex on {model.ClrType.FullName} specifies IndexedPropertyNames [{string.Join(", ", classIndex.IndexedPropertyNames ?? [])}] but they could not be resolved to class properties.");

                if (classIndex.IndexedProperties?.Count != classIndex.IndexedPropertyNames?.Length)
                    throw new InvalidOperationException($"RelmIndex on {model.ClrType.FullName} has mismatched IndexedPropertyNames and resolved IndexedProperties counts.");
            }

            return compositeIndexes;
        }

        private static List<(RelmIndexBase IndexDetails, PropertyInfo? PropertyDetails)> GetUniqueIndexes(List<(RelmIndexBase IndexDetails, PropertyInfo? PropertyDetails)> compositeIndexes, ValidatedModelType model)
        {
            var classIndexes = model.ClrType.GetCustomAttributes(true).OfType<RelmUnique>().ToList();
            foreach (var classIndex in classIndexes)
            {
                if ((classIndex.ConstraintProperties?.Length ?? 0) == 0)
                    throw new InvalidOperationException($"RelmUnique on {model.ClrType.FullName} must specify ConstraintProperties.");

                var newIndexBase = new RelmIndexBase
                {
                    IndexedProperties = classIndex.ConstraintProperties
                        ?.Select(name => new RelmIndexColumnBase(name))
                        .ToList(),
                    IndexedPropertyNames = classIndex.ConstraintProperties,
                    IndexTypeValue = IndexType.UNIQUE,
                    IndexKeyHolder = classIndex.ConstraintProperties == null ? null : string.Join(",", classIndex.ConstraintProperties)
                };

                if ((newIndexBase.IndexedProperties?.Count ?? 0) == 0)
                    throw new InvalidOperationException($"RelmUnique on {model.ClrType.FullName} specifies ConstraintProperties [{string.Join(", ", classIndex.ConstraintProperties ?? [])}] but they could not be resolved to class properties.");

                if (newIndexBase.IndexedProperties?.Count != classIndex.ConstraintProperties?.Length)
                    throw new InvalidOperationException($"RelmUnique on {model.ClrType.FullName} has mismatched ConstraintProperties and resolved IndexedProperties counts.");

                compositeIndexes.Add((newIndexBase, null));
            }

            return compositeIndexes;
        }

        private static Dictionary<string, List<(string ColumnName, RelmIndexBase IndexDefinition)>> GroupCompositeIndexes(List<(RelmIndexBase IndexDetails, PropertyInfo? PropertyDetails)> compositeIndexes, List<(PropertyInfo PropertyDetails, RelmColumn? ColumnDetails)> columnProperties, Dictionary<string, string?> nameMap, ValidatedModelType model)
        {
            var indexGroups = new Dictionary<string, List<(string ColumnName, RelmIndexBase IndexDefinition)>>(StringComparer.Ordinal);

            // Process composite indexes first
            foreach (var (indexDetails, propertyDetails) in compositeIndexes)
            {
                var columnNames = new List<(string ColumnName, RelmIndexBase IndexDefinition)>();

                if ((indexDetails.IndexedProperties is null || indexDetails.IndexedProperties.Count == 0) && propertyDetails is null)
                    throw new InvalidOperationException($"RelmIndex on type {model.ClrType.FullName} must either be attached to a property or specify IndexedProperties.");

                var propertiesNames = indexDetails.IndexedProperties ?? [new RelmIndexColumnBase(propertyDetails!.Name)];
                foreach (var indexProperty in propertiesNames)
                {
                    var (columnPropertyDetails, columnDetails) = columnProperties.FirstOrDefault(x => x.PropertyDetails.Name == indexProperty.ColumnName);
                    if (columnDetails is null)
                        throw new InvalidOperationException($"Unable to resolve column for index on property {(propertyDetails?.Name ?? "'class'")} in type {model.ClrType.FullName}");

                    var columnName = ResolveColumnName(columnPropertyDetails, columnDetails!, nameMap)
                        ?? throw new InvalidOperationException($"Unable to resolve column name for {columnPropertyDetails.Name}");

                    columnNames.Add((columnName, indexDetails));
                }

                var indexName = indexDetails.IndexName ?? $"{(indexDetails.IndexTypeValue == IndexType.UNIQUE ? "UQ" : "IX")}_{model.TableName}_{string.Join("_", columnNames.Select(x => x.ColumnName))}";
                if (!indexGroups.ContainsKey(indexName))
                    indexGroups.Add(indexName, []);

                indexGroups[indexName] = columnNames;
            }

            return indexGroups;
        }

        private static List<RelmTrigger> GetClassTriggers(ValidatedModelType model)
        {
            var classTriggers = model.ClrType
                .GetCustomAttributes(true)
                .OfType<RelmTrigger>()
                .Where(x => x != null)
                .ToList();

            var triggers = new List<RelmTrigger>();
            foreach (var classTrigger in classTriggers)
            {
                triggers.Add(classTrigger);
            }

            return triggers;
        }

        private static Dictionary<string, TriggerSchema> GroupTriggers(List<RelmTrigger> triggers, ValidatedModelType model)
        {
            var triggerGroups = new Dictionary<string, TriggerSchema>(StringComparer.Ordinal);
            foreach (var triggerDetails in triggers)
            {
                var signature = $"{triggerDetails.TriggerTime}_{triggerDetails.TriggerEvent}_{model.DatabaseName}_{model.TableName}_{triggerDetails.TriggerBody}";
                var signatureHash = signature.Sha256Hex()[..29]; // short hash to ensure uniqueness while keeping name length reasonable
                var triggerName = triggerDetails.TriggerName ?? $"TR_{triggerDetails.TriggerTime.ToString()[0]}{triggerDetails.TriggerEvent.ToString()[0]}_{signatureHash}";

                if (triggerGroups.ContainsKey(triggerName))
                    throw new InvalidOperationException($"Duplicate trigger name '{triggerName}' on {model.ClrType.FullName}. Trigger names must be unique.");
                
                triggerGroups[triggerName] = new TriggerSchema
                {
                    TriggerName = triggerName,
                    EventManipulation = triggerDetails.TriggerEvent,
                    ActionTiming = triggerDetails.TriggerTime,
                    ActionStatement = triggerDetails.TriggerBody,
                    EventObjectTable = model.TableName
                };
            }
            return triggerGroups;
        }
    }
}
