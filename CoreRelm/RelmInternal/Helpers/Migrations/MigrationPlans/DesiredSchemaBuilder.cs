using BDL.Common.Logging.Extensions;
using CoreRelm.Attributes;
using CoreRelm.Attributes.BaseClasses;
using CoreRelm.Exceptions;
using CoreRelm.Extensions;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using CoreRelm.RelmInternal.Extensions;
using CoreRelm.RelmInternal.Helpers.Migrations.Provisioning;
using CoreRelm.RelmInternal.Helpers.Migrations.Rendering;
using CoreRelm.RelmInternal.Models;
using Microsoft.Extensions.Logging;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Indexes;
using static CoreRelm.Enums.StoredProcedures;

namespace CoreRelm.RelmInternal.Helpers.Migrations.MigrationPlans
{
    public sealed class DesiredSchemaBuilder(ILogger<DesiredSchemaBuilder>? log = null) : IRelmDesiredSchemaBuilder
    {
        private ILogger<DesiredSchemaBuilder>? _log = log;

        public async Task<SchemaSnapshot> BuildAsync(string databaseFilter, List < ValidatedModelType> modelsForDb)
        {
            var desired = Build(databaseFilter, modelsForDb);

            return desired;
        }

        public SchemaSnapshot Build(string databaseFilter, IReadOnlyList<ValidatedModelType> modelsForDb)
        {
            _log?.LogFormatted(LogLevel.Information, "Building desired schema", args: [], preIncreaseLevel: true);
           
            // modelsForDb already contains only models for this database
            var tables = new Dictionary<string, TableSchema>(StringComparer.Ordinal);
            var functions = new Dictionary<string, FunctionSchema>(StringComparer.Ordinal);

            // Resolve table schemas deterministically
            _log?.SaveIndentLevel("model");
            foreach (var model in modelsForDb
                         .OrderBy(m => m.TableName, StringComparer.Ordinal)
                         .ThenBy(m => m.ClrType.FullName, StringComparer.Ordinal))
            {
                _log?.RestoreIndentLevel("model");

                if (model == null)
                    continue;

                _log?.LogFormatted(LogLevel.Information, "Processing model: {ModelName} ({TableName})", args: [model.ClrType.Name, model.TableName], preIncreaseLevel: true);

                var modelClrType = model.ClrType;
                _log?.LogFormatted(LogLevel.Information, "CLR type name: {ModelClrType}", args: [modelClrType.Name], preIncreaseLevel: true);

                // sanity: enforce attributes exist (your resolver already does this, but keep it safe)
                var dbAttr = GetTypeAttribute(modelClrType, nameof(RelmDatabase))
                    ?? throw new MissingRelmDatabaseAttributeException(modelClrType);
                var tableAttr = GetTypeAttribute(modelClrType, nameof(RelmTable))
                    ?? throw new MissingRelmTableAttributeException(modelClrType);

                var databaseName = GetStringProperty(dbAttr, nameof(RelmDatabase.DatabaseName))
                    ?? throw new InvalidOperationException($"[{dbAttr?.GetType().Name}] on {modelClrType.FullName} must expose DatabaseName.");
                var tableName = GetStringProperty(tableAttr, nameof(RelmTable.TableName))
                    ?? throw new InvalidOperationException($"[{tableAttr.GetType().Name}] on {modelClrType.FullName} must expose TableName.");

                _log?.LogFormatted(LogLevel.Information, "Resolved table: `{DatabaseName}`.`{TableName}`", args: [databaseName, tableName]);

                if (!string.Equals(databaseName, databaseFilter, StringComparison.Ordinal))
                {
                    _log?.LogFormatted(LogLevel.Warning, "Skipping model {ModelClrType} because its database '{DatabaseName}' does not match the filter '{DatabaseFilter}'.", args: [modelClrType.FullName, databaseName, databaseFilter], singleIndentLine: true);
                    continue;
                }

                // Precompute CLR property name -> DB column name map for this type
                var nameMap = BuildColumnNameMap(modelClrType);
                _log?.LogFormatted(LogLevel.Information, "Built CLR property name to DB column name map for {ModelClrType}:\n{{\n{NameMap}\n}}", args: [modelClrType.Name, string.Join(",\n", nameMap.Select(kv => $"\t{kv.Key}: `{kv.Value}`"))]);

                // Columns (properties tagged with [RelmColumn], including inherited)
                var modelProperties = GetAllInstanceProperties(modelClrType);
                var columnProperties = modelProperties
                    .Select(p => (PropertyDetails: p, ColumnDetails: p.GetCustomAttributes(true).OfType<RelmColumn>().FirstOrDefault()))
                    .Where(x => x.ColumnDetails != null)
                    .ToList();
                _log?.LogFormatted(LogLevel.Information, "Identified {ColumnCount} properties with [{RelmColumn}].", args: [columnProperties.Count, nameof(RelmColumn)]);

                var compositeIndexColumnsList = model.ClrType
                    .GetCustomAttributes(true)
                    .OfType<RelmIndexColumnBase>()
                    .Where(x => x != null)
                    .ToList();
                _log?.LogFormatted(LogLevel.Information, "Identified {CompositeIndexColumnCount} [{RelmIndexColumnBase}].", args: [compositeIndexColumnsList.Count, nameof(RelmIndexColumnBase)]);

                _log?.LogFormatted(LogLevel.Information, "Getting [{RelmIndexColumn}] composite index column attributes", args: [nameof(RelmIndexColumn<object>)]);
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
                _log?.LogFormatted(LogLevel.Information, "Getting [{RelmIndex}] attributes attached to properties", args: [nameof(RelmIndex)]);
                var compositeIndexes = GetPropertyIndexes(modelClrType);

                _log?.LogFormatted(LogLevel.Information, "Getting [{RelmIndex}] attributes attached to class", args: [nameof(RelmIndex)]);
                var classIndexes = GetClassIndexes(compositeIndexColumns, model);
                compositeIndexes.AddRange(classIndexes);

                _log?.LogFormatted(LogLevel.Information, "Getting [{RelmUnique}] attributes attached to class", args: [nameof(RelmUnique)]);
                var uniqueIndexes = GetUniqueIndexes(model);
                compositeIndexes.AddRange(uniqueIndexes);

                // group all composite index columns by index key
                _log?.LogFormatted(LogLevel.Information, "Grouping all [{RelmIndex}]/[{RelmUnique}] attributes attached to class", args: [nameof(RelmIndex), nameof(RelmUnique)]);
                var indexGroups = GroupCompositeIndexes(compositeIndexes, columnProperties, nameMap, model);

                _log?.LogFormatted(LogLevel.Information, "Done processing all [{RelmIndex}]/[{RelmUnique}] attributes attached to class", args: [nameof(RelmIndex), nameof(RelmUnique)]);

                // Ordinal position: base columns first in your template order, then remaining sorted
                // We’ll compute later after collecting all columns.
                var desiredColumns = new Dictionary<string, ColumnSchema>(StringComparer.Ordinal);
                _log?.LogFormatted(LogLevel.Information, "Processing columns to build desired column schemas", args: []);
                _log?.SaveIndentLevel("columns");
                foreach (var (columnProperty, columnAttribute) in columnProperties)
                {
                    _log?.RestoreIndentLevel("columns");

                    _log?.LogFormatted(LogLevel.Information, "Processing property {PropertyName} with [{AttributeType}] attribute", args: [columnProperty.Name, columnAttribute!.GetType().Name], preIncreaseLevel: true);

                    var columnName = ResolveColumnName(columnProperty, columnAttribute!, nameMap)
                        ?? throw new InvalidOperationException($"Unable to resolve column name for {columnProperty.Name}");

                    _log?.LogFormatted(LogLevel.Information, "Resolved column name for property {PropertyName}: `{ColumnName}`", args: [columnProperty.Name, columnName], preIncreaseLevel: true);

                    // Map CLR type to MySQL type
                    var propertyType = Nullable.GetUnderlyingType(columnProperty.PropertyType) ?? columnProperty.PropertyType;
                    _log?.LogFormatted(LogLevel.Information, "Mapping CLR type to MySQL type for property {PropertyName} with CLR type {ClrType}", args: [columnProperty.Name, propertyType.FullName]);

                    var mysqlType = MySqlTypeMapper.ToMySqlType(propertyType.BaseType == typeof(Enum) ? propertyType.BaseType : columnProperty.PropertyType, columnAttribute!);
                    _log?.LogFormatted(LogLevel.Information, "Mapped CLR type {ClrType} to MySQL type {MySqlType} for property {PropertyName}", args: [propertyType.FullName, mysqlType, columnProperty.Name], singleIndentLine: true);

                    var isNullable = columnAttribute!.IsNullable;
                    var isPrimaryKey = columnAttribute.PrimaryKey;
                    var isAutonumber = columnAttribute.Autonumber;
                    var isUnique = columnAttribute.Unique;
                    _log?.LogFormatted(LogLevel.Information, "Column attributes for property {PropertyName}: IsNullable={IsNullable}, IsPrimaryKey={IsPrimaryKey}, IsAutonumber={IsAutonumber}, IsUnique={IsUnique}", args: [columnProperty.Name, isNullable, isPrimaryKey, isAutonumber, isUnique]);

                    // default value SQL
                    var defaultSql = columnAttribute.DefaultValue;
                    _log?.LogFormatted(LogLevel.Information, "Default value SQL for property {PropertyName}: {DefaultSql}", args: [columnProperty.Name, defaultSql], postDecreaseLevel: true);

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

                    _log?.LogFormatted(LogLevel.Information, "Added column schema for property {PropertyName} as column `{ColumnName}` with type {ColumnType}", args: [columnProperty.Name, columnName, mysqlType]);
                }
                _log?.RestoreIndentLevel("columns");

                _log?.DecreaseIndent();
                _log?.LogFormatted(LogLevel.Information, "Completed processing columns for model {ModelName}. Total columns: {ColumnCount}", args: [modelClrType.Name, desiredColumns.Count], preIncreaseLevel: true);

                // Assign ordinal positions deterministically
                _log?.LogFormatted(LogLevel.Information, "Ordering and assigning ordinal positions to columns", args: []);
                var orderedCols = OrderColumns([.. desiredColumns.Values]);
                var ordinal = 1;
                foreach (var c in orderedCols)
                {
                    desiredColumns[c.ColumnName!] = c.Clone();
                    desiredColumns[c.ColumnName!].OrdinalPosition = ordinal++;
                }

                // Index schemas from groups (non-unique by default; uniqueness comes from column Unique or PK)
                var desiredIndexes = new Dictionary<string, IndexSchema>(StringComparer.Ordinal);
                _log?.LogFormatted(LogLevel.Information, "Processing index groups to build desired index schemas", args: []);
                _log?.SaveIndentLevel("indexGroups");
                foreach (var (indexName, indexColumns) in indexGroups.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    _log?.RestoreIndentLevel("indexGroups");

                    _log?.LogFormatted(LogLevel.Information, "Processing index group: {IndexName} with {ColumnCount} columns", args: [indexName, indexColumns.Count], preIncreaseLevel: true);

                    var orderedIndexColumns = indexColumns
                        .Distinct()
                        .ToList();
                    _log?.LogFormatted(LogLevel.Information, "Ordered index columns for index {IndexName}: {Columns}", args: [indexName, string.Join(", ", orderedIndexColumns.Select(c => $"{c.ColumnName} (Descending: {c.IndexDefinition.Descending})"))]);

                    var indexColumnSchemas = new List<IndexColumnSchema>();
                    var sequenceInIndex = 1;
                    _log?.LogFormatted(LogLevel.Information, "Processing columns for index {IndexName} to build IndexColumnSchema list", args: [indexName]);
                    _log?.SaveIndentLevel("indexColumns");
                    foreach (var (ColumnName, IndexDescriptor) in orderedIndexColumns)
                    {
                        _log?.RestoreIndentLevel("indexColumns");

                        _log?.LogFormatted(LogLevel.Information, "Processing column {ColumnName} for index {IndexName}", args: [ColumnName, indexName], preIncreaseLevel: true);

                        var columnProperty = IndexDescriptor.IndexedProperties?.FirstOrDefault(x => x.ColumnName == ColumnName);
                        _log?.LogFormatted(LogLevel.Information, "Resolved column property for column {ColumnName} in index {IndexName}: {PropertyName}", args: [ColumnName, indexName, nameMap?.FirstOrDefault(x => x.Value == ColumnName).Key ?? "null"]);

                        _log?.LogFormatted(LogLevel.Information, "Creating IndexColumnSchema for column {ColumnName} in index {IndexName}", args: [ColumnName, indexName], postDecreaseLevel: true);
                        indexColumnSchemas.Add(new IndexColumnSchema(
                            ColumnName: ColumnName,
                            SubPart: columnProperty == null ? null : $"({columnProperty.Length})",
                            Collation: IndexDescriptor.Descending ? "DESC" : "ASC",
                            Expression: columnProperty?.Expression,
                            SeqInIndex: sequenceInIndex++
                        ));

                        _log?.LogFormatted(LogLevel.Information, "Added IndexColumnSchema for column {ColumnName} in index {IndexName} with SubPart={SubPart}, Collation={Collation}, Expression={Expression}", args: [ColumnName, indexName, columnProperty == null ? "null" : $"({columnProperty.Length})", IndexDescriptor.Descending ? "DESC" : "ASC", columnProperty?.Expression ?? "null"]);
                    }
                    _log?.RestoreIndentLevel("indexColumns");

                    var isUnique = indexColumns.Any(c => c.IndexDefinition.IndexTypeValue == IndexType.UNIQUE);
                    _log?.LogFormatted(LogLevel.Information, "Determined index type for index {IndexName}: {IndexType} (IsUnique: {IsUnique})", args: [indexName, isUnique ? "UNIQUE" : "NON-UNIQUE", isUnique]);

                    _log?.LogFormatted(LogLevel.Information, "Creating IndexSchema for index {IndexName} with {ColumnCount} columns", args: [indexName, indexColumnSchemas.Count]);
                    desiredIndexes[indexName] = new IndexSchema
                    {
                        IndexName = indexName,
                        IndexTypeValue = indexColumns.All(x => x.IndexDefinition.IndexTypeValue == IndexType.UNIQUE) ? IndexType.UNIQUE : (indexColumns.FirstOrDefault().IndexDefinition.IndexTypeValue ?? IndexType.None),
                        Columns = indexColumnSchemas
                    };
                }
                _log?.RestoreIndentLevel("indexGroups");

                if ((indexGroups?.Count ?? 0) == 0)
                    _log?.LogFormatted(LogLevel.Information, "No indexes defined for model {ModelName}.", args: [modelClrType.Name], singleIndentLine: true);
                else
                    _log?.LogFormatted(LogLevel.Information, "Completed processing index groups for model {ModelName}. Total indexes: {IndexCount}", args: [modelClrType.Name, desiredIndexes.Count]);

                // Foreign keys: navigation properties with [RelmForeignKey]
                var desiredForeignKeys = new Dictionary<string, ForeignKeySchema>(StringComparer.Ordinal);
                _log?.LogFormatted(LogLevel.Information, "Processing navigation properties with [{RelmForeignKey}] to build desired foreign key schemas", args: [nameof(RelmForeignKey)]);
                _log?.SaveIndentLevel("navProps");
                foreach (var navProp in modelProperties)
                {
                    _log?.RestoreIndentLevel("navProps");

                    _log?.LogFormatted(LogLevel.Information, "Processing navigation property {NavPropName} for foreign key analysis", args: [navProp.Name], preIncreaseLevel: true);

                    var foreignKeyAttr = navProp.GetCustomAttributes(true).OfType<RelmForeignKey>().FirstOrDefault();
                    if (foreignKeyAttr is null)
                    {
                        _log?.LogFormatted(LogLevel.Information, "No [{RelmForeignKey}] attribute found on property {NavPropName}. Skipping foreign key processing for this property.", args: [nameof(RelmForeignKey), navProp.Name], singleIndentLine: true);
                        continue;
                    }
                    else
                        _log?.LogFormatted(LogLevel.Information, "Found [{RelmForeignKey}] attribute on property {NavPropName}. Processing foreign key details.", args: [nameof(RelmForeignKey), navProp.Name], preIncreaseLevel: true);

                    // Navigation property type must be a model type
                    var principalType = navProp.PropertyType;
                    _log?.LogFormatted(LogLevel.Information, "Principal type for navigation property {NavPropName} is {PrincipalType}", args: [navProp.Name, principalType.Name], preIncreaseLevel: true);

                    // get either navProp.PropertyType, or if it's a collection, the generic argument type
                    _log?.SaveIndentLevel("principalType");
                    if (principalType.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(principalType))
                    {
                        _log?.LogFormatted(LogLevel.Information, "Principal type {PrincipalType} is a collection. Attempting to resolve element type.", args: [principalType.Name], preIncreaseLevel: true);

                        var genericArgs = principalType.GetGenericArguments();
                        if (genericArgs.Length == 1)
                        {
                            principalType = genericArgs[0];
                            _log?.LogFormatted(LogLevel.Information, "Resolved element type {PrincipalType} for collection navigation property {NavPropName}.", args: [principalType.Name, navProp.Name], singleIndentLine: true);
                        }
                        else
                            _log?.LogFormatted(LogLevel.Warning, "Unable to resolve element type for collection navigation property {NavPropName} with principal type {PrincipalType}. Expected exactly one generic argument, but found {GenericArgCount}. Using the collection type directly.", args: [navProp.Name, principalType.FullName, genericArgs.Length], singleIndentLine: true);
                    }
                    else
                        _log?.LogFormatted(LogLevel.Information, "Principal type {PrincipalType} for navigation property {NavPropName} is not a collection. Using the property type directly.", args: [principalType.Name, navProp.Name], singleIndentLine: true);
                    _log?.RestoreIndentLevel("principalType");

                    var principalDbAttr = GetTypeAttribute(principalType, nameof(RelmDatabase))
                        ?? throw new MissingRelmDatabaseAttributeException(principalType, modelClrType);
                    var principalTblAttr = GetTypeAttribute(principalType, nameof(RelmTable))
                        ?? throw new MissingRelmTableAttributeException(principalType, modelClrType);

                    var principalDatabaseName = GetStringProperty(principalDbAttr, nameof(RelmDatabase.DatabaseName))
                        ?? throw new InvalidOperationException($"[{principalDbAttr?.GetType().Name}] on {principalType.FullName} must expose DatabaseName.");
                    var principalTableName = GetStringProperty(principalTblAttr, nameof(RelmTable.TableName))
                        ?? throw new InvalidOperationException($"[{principalTblAttr.GetType().Name}] on {principalType.FullName} must expose TableName.");

                    if (!string.Equals(principalDatabaseName, databaseName, StringComparison.Ordinal))
                        throw new InvalidOperationException($"Cross-database FK not supported: {databaseName}.{tableName} -> {principalDatabaseName}.{principalTableName}");

                    if (foreignKeyAttr.LocalKeys is null || foreignKeyAttr.ForeignKeys is null)
                        throw new InvalidOperationException($"[RelmForeignKey] on {modelClrType.FullName}.{navProp.Name} must specify LocalKeys and ForeignKeys.");

                    if (foreignKeyAttr.LocalKeys.Length != foreignKeyAttr.ForeignKeys.Length)
                        throw new InvalidOperationException($"[RelmForeignKey] on {modelClrType.FullName}.{navProp.Name} has mismatched key counts.");

                    // Resolve local columns from CLR property names
                    // TODO: keep cache of resolved CLR property name -> column name for this type to avoid redundant resolution if multiple FKs reference the same properties
                    var localCols = new List<string?>();
                    _log?.LogFormatted(LogLevel.Information, "Resolving local columns for foreign key on navigation property {NavPropName}", args: [navProp.Name], preIncreaseLevel: true);
                    _log?.SaveIndentLevel("localKeys");
                    foreach (var localClrName in foreignKeyAttr.LocalKeys)
                    {
                        _log?.RestoreIndentLevel("localKeys");

                        _log?.LogFormatted(LogLevel.Information, "Resolving local CLR property '{LocalClrName}' for foreign key on navigation property {NavPropName}", args: [localClrName, navProp.Name], preIncreaseLevel: true);

                        var localProp = FindProperty(modelClrType, localClrName);
                        _log?.LogFormatted(LogLevel.Information, "Found local CLR property '{LocalClrName}' on {ModelClrType} for foreign key on navigation property {NavPropName}", args: [localClrName, modelClrType.Name, navProp.Name], preIncreaseLevel: true);

                        var localColAttr = localProp.GetCustomAttributes(true).OfType<RelmColumn>().FirstOrDefault()
                            ?? throw new InvalidOperationException($"Local key '{localClrName}' on {modelClrType.FullName}.{navProp.Name} must have [RelmColumn].");
                        _log?.LogFormatted(LogLevel.Information, "Found [{RelmColumn}] attribute on local CLR property '{LocalClrName}' for foreign key on navigation property {NavPropName}", args: [localClrName, navProp.Name, nameof(RelmColumn)]);

                        _log?.LogFormatted(LogLevel.Information, "Resolving column name for local CLR property '{LocalClrName}' with [{AttributeType}] attribute for foreign key on navigation property {NavPropName}", args: [localClrName, localColAttr.GetType().Name, navProp.Name]);
                        localCols.Add(ResolveColumnName(localProp, localColAttr, nameMap));

                        _log?.LogFormatted(LogLevel.Information, "Added local column '{LocalColumn}' to list of local columns for foreign key on navigation property {NavPropName}", args: [localCols.LastOrDefault(), navProp.Name]);
                    }
                    _log?.RestoreIndentLevel("localKeys");

                    _log?.LogFormatted(LogLevel.Information, "Resolved local columns for foreign key on navigation property {NavPropName}: {LocalColumns}", args: [navProp.Name, string.Join(", ", localCols.Select(c => $"`{c}`"))]);

                    var principalNameMap = BuildColumnNameMap(principalType);
                    _log?.LogFormatted(LogLevel.Information, "Built CLR property name to DB column name map for principal type {PrincipalType}:\n{{\n{NameMap}\n}}", args: [principalType.Name, string.Join(",\n", principalNameMap.Select(kv => $"\t{kv.Key}: `{kv.Value}`"))]);

                    // Resolve referenced columns from principal CLR property names
                    var refCols = new List<string?>();
                    _log?.LogFormatted(LogLevel.Information, "Resolving referenced columns for foreign key on navigation property {NavPropName}", args: [navProp.Name]);
                    _log?.SaveIndentLevel("foreignKeys");
                    foreach (var refClrName in foreignKeyAttr.ForeignKeys)
                    {
                        _log?.RestoreIndentLevel("foreignKeys");

                        _log?.LogFormatted(LogLevel.Information, "Resolving principal CLR property '{RefClrName}' for foreign key on navigation property {NavPropName}", args: [refClrName, navProp.Name], preIncreaseLevel: true);

                        var refProp = FindProperty(principalType, refClrName);
                        _log?.LogFormatted(LogLevel.Information, "Found principal CLR property '{RefClrName}' on {PrincipalType} for foreign key on navigation property {NavPropName}", args: [refClrName, principalType.Name, navProp.Name]);

                        var refColAttr = refProp.GetCustomAttributes(true).OfType<RelmColumn>().FirstOrDefault()
                                         ?? throw new InvalidOperationException($"Foreign key '{refClrName}' on {modelClrType.FullName}.{navProp.Name} must have [RelmColumn] on {principalType.FullName}.");
                        _log?.LogFormatted(LogLevel.Information, "Found [{RelmColumn}] attribute on principal CLR property '{RefClrName}' for foreign key on navigation property {NavPropName}", args: [refClrName, navProp.Name, nameof(RelmColumn)]);

                        _log?.LogFormatted(LogLevel.Information, "Resolving column name for principal CLR property '{RefClrName}' with [{AttributeType}] attribute for foreign key on navigation property {NavPropName}", args: [refClrName, refColAttr.GetType().Name, navProp.Name]);
                        refCols.Add(ResolveColumnName(refProp, refColAttr, principalNameMap));

                        _log?.LogFormatted(LogLevel.Information, "Added referenced column '{ReferencedColumn}' to list of referenced columns for foreign key on navigation property {NavPropName}", args: [refCols.LastOrDefault(), navProp.Name]);
                    }
                    _log?.RestoreIndentLevel("foreignKeys");

                    _log?.LogFormatted(LogLevel.Information, "Resolved referenced columns for foreign key on navigation property {NavPropName}: {ReferencedColumns}", args: [navProp.Name, string.Join(", ", refCols.Select(c => $"`{c}`"))]);

                    var fkName = $"FK_{tableName}_{UnderscoreNamesHelper.ConvertPropertyToUnderscoreName(navProp, forceLowerCase: true)}";
                    _log?.LogFormatted(LogLevel.Information, "Constructed foreign key constraint name for navigation property {NavPropName}: {ForeignKeyName}", args: [navProp.Name, fkName]);

                    _log?.LogFormatted(LogLevel.Information, "Creating ForeignKeySchema for navigation property {NavPropName} with constraint name {ForeignKeyName}, local columns ({LocalColumns}), referenced table `{ReferencedTable}`, and referenced columns ({ReferencedColumns})", args: [navProp.Name, fkName, string.Join(", ", localCols.Select(c => $"`{c}`")), principalTableName, string.Join(", ", refCols.Select(c => $"`{c}`"))], postDecreaseLevel: true);
                    desiredForeignKeys[fkName] = new ForeignKeySchema
                    {
                        ConstraintName = fkName,
                        TableName = tableName,
                        ColumnNames = localCols,
                        ReferencedTableName = principalTableName,
                        ReferencedColumnNames = refCols,
                        UpdateRule = "RESTRICT",
                        DeleteRule = "CASCADE"
                    };

                    _log?.LogFormatted(LogLevel.Information, "Added ForeignKeySchema for navigation property {NavPropName} with constraint name {ForeignKeyName}", args: [navProp.Name, fkName]);
                }
                _log?.RestoreIndentLevel("navProps");

                _log?.LogFormatted(LogLevel.Information, "Completed processing foreign keys for model {ModelName}. Total foreign keys: {ForeignKeyCount}", args: [modelClrType.Name, desiredForeignKeys.Count]);

                // Triggers
                _log?.SaveIndentLevel("triggers");
                var classTriggers = GetClassTriggers(model);
                var desiredTriggers = GroupTriggers(classTriggers, model);
                _log?.RestoreIndentLevel("triggers");

                _log?.LogFormatted(LogLevel.Information, "Completed processing triggers for model {ModelName}. Total triggers: {TriggerCount}", args: [modelClrType.Name, desiredTriggers.Count]);

                _log?.SaveIndentLevel("functions");
                var classFunctions = GetClassFunctions(model);
                functions = GroupFunctions(classFunctions, model);
                _log?.RestoreIndentLevel("functions");

                _log?.LogFormatted(LogLevel.Information, "Completed processing functions for model {ModelName}. Total functions: {FunctionCount}", args: [modelClrType.Name, functions.Count]);

                _log?.LogFormatted(LogLevel.Information, "Creating TableSchema for table `{TableName}` with {ColumnCount} columns, {IndexCount} indexes, {ForeignKeyCount} foreign keys, and {TriggerCount} triggers", args: [tableName, desiredColumns.Count, desiredIndexes.Count, desiredForeignKeys.Count, desiredTriggers.Count]);
                tables[tableName] = new TableSchema(
                    TableName: tableName,
                    Columns: desiredColumns,
                    Indexes: desiredIndexes,
                    ForeignKeys: desiredForeignKeys,
                    Triggers: desiredTriggers
                );

                _log?.LogFormatted(LogLevel.Information, "Created TableSchema for table `{TableName}`", args: [tableName], singleIndentLine: true);
            }
            _log?.RestoreIndentLevel("model");

            _log?.LogFormatted(LogLevel.Information, "Completed building desired schema for database '{DatabaseName}' with {TableCount} tables.", args: [databaseFilter, tables.Count]);

            return new SchemaSnapshot(databaseFilter, tables, functions);
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

        private List<KeyValuePair<object?, List<RelmIndexColumnBase>>> GetCompositeIndexColumns(List<RelmIndexColumnBase> compositeIndexColumnsList)
        {
            _log?.LogFormatted(LogLevel.Information, "Processing composite index columns", args: [], preIncreaseLevel: true);

            var compositeIndexColumns = new List<KeyValuePair<object?, List<RelmIndexColumnBase>>>();
            
            _log?.SaveIndentLevel("compositeIndexColumns");
            foreach (var compositeIndexColumn in compositeIndexColumnsList)
            {
                _log?.RestoreIndentLevel("compositeIndexColumns");
                
                _log?.LogFormatted(LogLevel.Information, "Processing composite index column attribute with index key type: {AttributeType}", args: [compositeIndexColumn.GetType().GetGenericArguments().FirstOrDefault()?.Name], preIncreaseLevel: true);

                // bring IndexKey values down to RelmIndexColumnBase so we can use them when building indexes, without needing to know the generic type parameter of RelmIndex<T>
                // Access inherited members declared on RelmIndex<T> via the runtime type, not RelmIndexBase
                compositeIndexColumn.IndexKeyHolder = compositeIndexColumn
                    .GetType()
                    .GetProperty(nameof(RelmIndexColumn<object>.IndexKey), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetValue(compositeIndexColumn);
                _log?.LogFormatted(LogLevel.Information, "Extracted IndexKey for composite index column: {IndexKey}", args: [compositeIndexColumn.IndexKeyHolder], preIncreaseLevel: true);

                if (!compositeIndexColumns.Any(x => Equals(x.Key, compositeIndexColumn.IndexKeyHolder)))
                {
                    _log?.LogFormatted(LogLevel.Information, "Creating new composite index column group");
                    compositeIndexColumns.Add(new KeyValuePair<object?, List<RelmIndexColumnBase>>(compositeIndexColumn.IndexKeyHolder, []));
                }

                _log?.LogFormatted(LogLevel.Information, "Adding composite index column: {ColumnName}", args: [compositeIndexColumn.ColumnName]);
                compositeIndexColumns.First(x => Equals(x.Key, compositeIndexColumn.IndexKeyHolder)).Value.Add(compositeIndexColumn);

            }
            _log?.RestoreIndentLevel("compositeIndexColumns");

            _log?.LogFormatted(LogLevel.Information, "Completed processing composite index columns. Total groups: {GroupCount}", args: [compositeIndexColumns.Count], postDecreaseLevel: true);
            return compositeIndexColumns;
        }

        private List<(RelmIndexBase IndexDetails, PropertyInfo? PropertyDetails)> GetPropertyIndexes(Type modelClrType)
        {
            var propertyIndexes = new List<(RelmIndexBase IndexDetails, PropertyInfo? PropertyDetails)>();

            _log?.LogFormatted(LogLevel.Information, "Processing property-level indexes", args: [], preIncreaseLevel: true);

            var instanceProperties = GetAllInstanceProperties(modelClrType);
            _log?.LogFormatted(LogLevel.Information, "Found {PropertyCount} instance properties to check for [{RelmIndex}] attributes.", args: [instanceProperties.Count, nameof(RelmIndex)]);

            _log?.SaveIndentLevel("propertyIndexes");
            foreach (var instanceProperty in instanceProperties)
            {
                _log?.RestoreIndentLevel("propertyIndexes");

                //_log?.LogFormatted(LogLevel.Information, "Checking property: {PropertyName}", args: [instanceProperty.Name], preIncreaseLevel: true);

                var indexAttr = instanceProperty.GetCustomAttributes(true).OfType<RelmIndexBase>().FirstOrDefault();
                if (indexAttr == null)
                {
                    //_log?.LogFormatted(LogLevel.Information, "No [{RelmIndex}] attribute found on property {PropertyName}.", args: [nameof(RelmIndex), instanceProperty.Name], singleIndentLine: true);
                    continue;
                }

                _log?.LogFormatted(LogLevel.Information, "Adding property index for property {PropertyName}", args: [instanceProperty.Name], preIncreaseLevel: true);
                propertyIndexes.Add((indexAttr, instanceProperty));
            }
            _log?.SaveIndentLevel("propertyIndexes");

            _log?.LogFormatted(LogLevel.Information, "Completed processing property-level indexes. Total property indexes found: {PropertyIndexCount}", args: [propertyIndexes.Count], postDecreaseLevel: true);
            return propertyIndexes;
        }

        private List<(RelmIndexBase IndexDetails, PropertyInfo? PropertyDetails)> GetClassIndexes(List<KeyValuePair<object?, List<RelmIndexColumnBase>>> compositeIndexColumns, ValidatedModelType model)
        {
            var classIndexes = new List<(RelmIndexBase IndexDetails, PropertyInfo? PropertyDetails)>();

            _log?.LogFormatted(LogLevel.Information, "Processing class-level indexes", args: [], preIncreaseLevel: true);

            var classIndexAttributes = model.ClrType.GetCustomAttributes(true).OfType<RelmIndexBase>().ToList();
            _log?.LogFormatted(LogLevel.Information, "Found {ClassIndexCount} class-level [{RelmIndex}] attributes.", args: [classIndexAttributes.Count, nameof(RelmIndex)]);

            _log?.SaveIndentLevel("classIndexes");
            foreach (var classIndex in classIndexAttributes)
            {
                _log?.RestoreIndentLevel("classIndexes");

                _log?.LogFormatted(LogLevel.Information, "Processing class index attribute [{AttributeType}] with index key type: {AttributeIndexType}", args: [classIndex.GetType().Name, classIndex.GetType().GetGenericArguments().FirstOrDefault()?.Name], preIncreaseLevel: true);

                if (classIndex is not RelmIndexNamed)
                {
                    // bring IndexKey values down to RelmIndexColumnBase so we can use them when building indexes, without needing to know the generic type parameter of RelmIndex<T>
                    // Access inherited members declared on RelmIndex<T> via the runtime type, not RelmIndexBase
                    /* This is now done in the RelmIndex<> constructor
                    classIndex.IndexKeyHolder = classIndex
                        .GetType()
                        .GetProperty(nameof(RelmIndex<object>.IndexKey), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?.GetValue(classIndex);
                    */
                    _log?.LogFormatted(LogLevel.Information, "Extracted IndexKey for class index: {IndexKey}", args: [classIndex.IndexKeyHolder], preIncreaseLevel: true);

                    classIndex.IndexedProperties ??= compositeIndexColumns.FirstOrDefault(x => Equals(x.Key, classIndex.IndexKeyHolder)).Value;
                    _log?.LogFormatted(LogLevel.Information, "Assigned IndexedProperties for class index with IndexKey {IndexKey}. Property count: {PropertyCount}", args: [classIndex.IndexKeyHolder, classIndex.IndexedProperties?.Count ?? 0]);
                }

                _log?.LogFormatted(LogLevel.Information, "Adding class index with IndexKey {IndexKey} to composite indexes list", args: [classIndex.IndexKeyHolder], preIncreaseLevel: classIndex is RelmIndexNamed);
                classIndexes.Add((classIndex, null));

                if ((classIndex.IndexedPropertyNames?.Length ?? 0) == 0)
                {
                    _log?.LogFormatted(LogLevel.Information, "No IndexedPropertyNames specified for class index with IndexKey {IndexKey}, skipping IndexedPropertyNames resolution.", args: [classIndex.IndexKeyHolder], postDecreaseLevel: true);
                    continue;
                }
                else
                    _log?.LogFormatted(LogLevel.Information, "Resolving IndexedPropertyNames to IndexedProperties for class index with IndexKey {IndexKey}. IndexedPropertyNames count: {PropertyNameCount}", args: [classIndex.IndexKeyHolder, classIndex.IndexedPropertyNames?.Length ?? 0]);

                classIndex.IndexedProperties = classIndex.IndexedPropertyNames
                    ?.Select(name => new RelmIndexColumnBase(name) { IndexKeyHolder = classIndex.IndexKeyHolder })
                    .ToList();
                _log?.LogFormatted(LogLevel.Information, "Resolved IndexedPropertyNames to IndexedProperties for class index with IndexKey {IndexKey}. Property count: {PropertyCount}", args: [classIndex.IndexKeyHolder, classIndex.IndexedProperties?.Count ?? 0]);

                if ((classIndex.IndexedProperties?.Count ?? 0) == 0)
                    throw new InvalidOperationException($"RelmIndex on {model.ClrType.FullName} specifies IndexedPropertyNames [{string.Join(", ", classIndex.IndexedPropertyNames ?? [])}] but they could not be resolved to class properties.");

                if (classIndex.IndexedProperties?.Count != classIndex.IndexedPropertyNames?.Length)
                    throw new InvalidOperationException($"RelmIndex on {model.ClrType.FullName} has mismatched IndexedPropertyNames and resolved IndexedProperties counts.");

                _log?.LogFormatted(LogLevel.Information, "Completed processing class index with IndexKey {IndexKey}", args: [classIndex.IndexKeyHolder], postDecreaseLevel: true);
            }
            _log?.RestoreIndentLevel("classIndexes");

            _log?.LogFormatted(LogLevel.Information, "Completed processing class-level indexes. Total class indexes count: {ClassIndexCount}", args: [classIndexes.Count], postDecreaseLevel: true);
            return classIndexes;
        }

        private List<(RelmIndexBase IndexDetails, PropertyInfo? PropertyDetails)> GetUniqueIndexes(ValidatedModelType model)
        {
            var uniqueIndexes = new List<(RelmIndexBase IndexDetails, PropertyInfo? PropertyDetails)>();
            
            _log?.LogFormatted(LogLevel.Information, "Processing class-level unique indexes", args: [], preIncreaseLevel: true);

            var classIndexAttributes = model.ClrType.GetCustomAttributes(true).OfType<RelmUnique>().ToList();
            _log?.LogFormatted(LogLevel.Information, "Found {ClassUniqueIndexCount} class-level [{RelmUnique}] attributes.", args: [classIndexAttributes.Count, nameof(RelmUnique)]);

            _log?.SaveIndentLevel("classUniqueIndexes");
            foreach (var classIndex in classIndexAttributes)
            {
                _log?.RestoreIndentLevel("classUniqueIndexes");
                    
                _log?.LogFormatted(LogLevel.Information, "Processing class unique index attribute [{AttributeType}]", args: [classIndex.GetType().Name], preIncreaseLevel: true);

                if ((classIndex.ConstraintProperties?.Length ?? 0) == 0)
                    throw new InvalidOperationException($"RelmUnique on {model.ClrType.FullName} must specify ConstraintProperties.");
                else                    
                    _log?.LogFormatted(LogLevel.Information, "Resolving ConstraintProperties to IndexedProperties for class unique index. ConstraintProperties count: {ConstraintPropertyCount}", args: [classIndex.ConstraintProperties?.Length ?? 0], preIncreaseLevel: true);

                var newIndexBase = new RelmIndexBase
                {
                    IndexedProperties = classIndex.ConstraintProperties
                        ?.Select(name => new RelmIndexColumnBase(name))
                        .ToList(),
                    IndexedPropertyNames = classIndex.ConstraintProperties,
                    IndexTypeValue = IndexType.UNIQUE,
                    IndexKeyHolder = classIndex.ConstraintProperties == null ? null : string.Join(",", classIndex.ConstraintProperties)
                };
                _log?.LogFormatted(LogLevel.Information, "Resolved ConstraintProperties to IndexedProperties for class unique index. IndexedProperties count: {IndexedPropertyCount}", args: [newIndexBase.IndexedPropertyNames?.Length ?? 0]);

                if ((newIndexBase.IndexedProperties?.Count ?? 0) == 0)
                    throw new InvalidOperationException($"RelmUnique on {model.ClrType.FullName} specifies ConstraintProperties [{string.Join(", ", classIndex.ConstraintProperties ?? [])}] but they could not be resolved to class properties.");

                if (newIndexBase.IndexedProperties?.Count != classIndex.ConstraintProperties?.Length)
                    throw new InvalidOperationException($"RelmUnique on {model.ClrType.FullName} has mismatched ConstraintProperties and resolved IndexedProperties counts.");

                _log?.LogFormatted(LogLevel.Information, "Adding unique index to composite indexes list for class unique index", args: []);
                uniqueIndexes.Add((newIndexBase, null));

                _log?.LogFormatted(LogLevel.Information, "Completed processing class unique index", args: [], postDecreaseLevel: true);
            }
            _log?.RestoreIndentLevel("classUniqueIndexes");

            _log?.LogFormatted(LogLevel.Information, "Completed processing class-level unique indexes. Total unique indexes count: {UniqueIndexCount}", args: [uniqueIndexes.Count], postDecreaseLevel: true);
            return uniqueIndexes;
        }

        private Dictionary<string, List<(string ColumnName, RelmIndexBase IndexDefinition)>> GroupCompositeIndexes(List<(RelmIndexBase IndexDetails, PropertyInfo? PropertyDetails)> compositeIndexes, List<(PropertyInfo PropertyDetails, RelmColumn? ColumnDetails)> columnProperties, Dictionary<string, string?> nameMap, ValidatedModelType model)
        {
            var indexGroups = new Dictionary<string, List<(string ColumnName, RelmIndexBase IndexDefinition)>>(StringComparer.Ordinal);

            _log?.LogFormatted(LogLevel.Information, "Grouping composite indexes into index definitions", args: [], preIncreaseLevel: true);

            _log?.SaveIndentLevel("indexGroups");
            foreach (var (indexDetails, propertyDetails) in compositeIndexes)
            {
                _log?.RestoreIndentLevel("indexGroups");

                _log?.LogFormatted(LogLevel.Information, "Processing composite index with IndexKey {IndexKey} on property {PropertyName}", args: [indexDetails.IndexKeyHolder, propertyDetails?.Name ?? "'class'"], preIncreaseLevel: true);


                if ((indexDetails.IndexedProperties is null || indexDetails.IndexedProperties.Count == 0) && propertyDetails is null)
                    throw new InvalidOperationException($"RelmIndex on type {model.ClrType.FullName} must either be attached to a property or specify IndexedProperties.");
                else
                    _log?.LogFormatted(LogLevel.Information, "Determining properties for composite index with IndexKey {IndexKey}. IndexedProperties count: {IndexedPropertyCount}", args: [indexDetails.IndexKeyHolder, indexDetails.IndexedProperties?.Count ?? 0], preIncreaseLevel: true);

                var propertiesNames = indexDetails.IndexedProperties ?? [new RelmIndexColumnBase(propertyDetails!.Name)];
                _log?.LogFormatted(LogLevel.Information, "Using properties for composite index with IndexKey {IndexKey}: {PropertyNames}", args: [indexDetails.IndexKeyHolder, string.Join(", ", propertiesNames.Select(p => p.ColumnName))]);

                var columnNames = new List<(string ColumnName, RelmIndexBase IndexDefinition)>();
                _log?.LogFormatted(LogLevel.Information, "Resolving column names for composite index with IndexKey {IndexKey}", args: [indexDetails.IndexKeyHolder]);
                _log?.SaveIndentLevel("indexColumns");
                foreach (var indexProperty in propertiesNames)
                {
                    _log?.RestoreIndentLevel("indexColumns");

                    _log?.LogFormatted(LogLevel.Information, "Resolving column for index property {PropertyName} in composite index with IndexKey {IndexKey}", args: [indexProperty.ColumnName, indexDetails.IndexKeyHolder], preIncreaseLevel: true);

                    var (columnPropertyDetails, columnDetails) = columnProperties.FirstOrDefault(x => x.PropertyDetails.Name == indexProperty.ColumnName);
                    if (columnDetails is null)
                        throw new InvalidOperationException($"Unable to resolve column for index on property {(propertyDetails?.Name ?? "'class'")} in type {model.ClrType.Name}");
                    else
                        _log?.LogFormatted(LogLevel.Information, "Resolved column for index property {PropertyName}: {ColumnName}", args: [indexProperty.ColumnName, columnDetails.ColumnName], preIncreaseLevel: true);

                    var columnName = ResolveColumnName(columnPropertyDetails, columnDetails!, nameMap)
                        ?? throw new InvalidOperationException($"Unable to resolve column name for {columnPropertyDetails.Name}");

                    _log?.LogFormatted(LogLevel.Information, "Resolved column name for index property {PropertyName}: {ColumnName}", args: [indexProperty.ColumnName, columnName]);

                    _log?.LogFormatted(LogLevel.Information, "Adding column {ColumnName} to index group for composite index with IndexKey {IndexKey}", args: [columnName, indexDetails.IndexKeyHolder], postDecreaseLevel: true);
                    columnNames.Add((columnName, indexDetails));

                    _log?.LogFormatted(LogLevel.Information, "Completed processing column {ColumnName} for composite index with IndexKey {IndexKey}", args: [columnName, indexDetails.IndexKeyHolder]);
                }
                _log?.RestoreIndentLevel("indexColumns");

                var indexName = indexDetails.IndexName ?? $"{(indexDetails.IndexTypeValue == IndexType.UNIQUE ? "UQ" : "IX")}_{model.TableName}_{string.Join("_", columnNames.Select(x => x.ColumnName))}";
                _log?.LogFormatted(LogLevel.Information, "Determined index name for composite index with IndexKey {IndexKey}: {IndexName}", args: [indexDetails.IndexKeyHolder, indexName]);

                if (!indexGroups.ContainsKey(indexName))
                {
                    _log?.LogFormatted(LogLevel.Information, "Creating new index group for index name {IndexName}", args: [indexName]);
                    indexGroups.Add(indexName, []);
                }

                _log?.LogFormatted(LogLevel.Information, "Adding columns to index group for index name {IndexName}. Columns: {ColumnNames}", args: [indexName, string.Join(", ", columnNames.Select(c => c.ColumnName))], postDecreaseLevel: true);
                indexGroups[indexName] = columnNames;

                _log?.LogFormatted(LogLevel.Information, "Completed processing composite index with IndexKey {IndexKey} and index name {IndexName}", args: [indexDetails.IndexKeyHolder, indexName], postDecreaseLevel: true);
            }
            _log?.RestoreIndentLevel("indexGroups");

            _log?.LogFormatted(LogLevel.Information, "Completed grouping composite indexes. Total index groups count: {IndexGroupCount}", args: [indexGroups.Count], postDecreaseLevel: true);
            return indexGroups;
        }

        private List<RelmTrigger> GetClassTriggers(ValidatedModelType model)
        {
            _log?.LogFormatted(LogLevel.Information, "Processing class-level triggers for {ModelName}", args: [model.ClrType.FullName]);

            var classTriggers = model.ClrType
                .GetCustomAttributes(true)
                .OfType<RelmTrigger>()
                .Where(x => x != null)
                .ToList();
            _log?.LogFormatted(LogLevel.Information, "Found {TriggerCount} class-level triggers on {ModelName}", args: [classTriggers.Count, model.ClrType.Name], preIncreaseLevel: true);

            var triggers = new List<RelmTrigger>();
            _log?.LogFormatted(LogLevel.Information, "Processing class-level triggers for {ModelName}", args: [model.ClrType.Name]);
            _log?.SaveIndentLevel("classTriggers");
            foreach (var classTrigger in classTriggers)
            {
                _log?.RestoreIndentLevel("classTriggers");

                _log?.LogFormatted(LogLevel.Information, "Processing class trigger with TriggerTime {TriggerTime} and TriggerEvent {TriggerEvent} on {ModelName}", args: [classTrigger.TriggerTime, classTrigger.TriggerEvent, model.ClrType.FullName], preIncreaseLevel: true);
                 if (string.IsNullOrWhiteSpace(classTrigger.TriggerBody))
                    throw new InvalidOperationException($"Trigger body cannot be null or whitespace for trigger on {model.ClrType.FullName} with TriggerTime {classTrigger.TriggerTime} and TriggerEvent {classTrigger.TriggerEvent}.");
                 
                triggers.Add(classTrigger);
                _log?.LogFormatted(LogLevel.Information, "Added class trigger with TriggerTime {TriggerTime} and TriggerEvent {TriggerEvent} to triggers list for {ModelName}", args: [classTrigger.TriggerTime, classTrigger.TriggerEvent, model.ClrType.FullName], preIncreaseLevel: true);
            }
            _log?.RestoreIndentLevel("classTriggers");

            _log?.LogFormatted(LogLevel.Information, "Completed processing class-level triggers for {ModelName}. Total triggers count: {TriggerCount}", args: [model.ClrType.Name, triggers.Count]);
            return triggers;
        }

        private Dictionary<string, TriggerSchema> GroupTriggers(List<RelmTrigger> triggers, ValidatedModelType model)
        {
            _log?.LogFormatted(LogLevel.Information, "Grouping triggers into TriggerSchema definitions for {ModelName}", args: [model.ClrType.FullName]);

            var triggerGroups = new Dictionary<string, TriggerSchema>(StringComparer.Ordinal);
            _log?.LogFormatted(LogLevel.Information, "Processing triggers for {ModelName} to create TriggerSchema definitions", args: [model.ClrType.Name], preIncreaseLevel: true);
            _log?.SaveIndentLevel("triggerGroups");
            foreach (var triggerDetails in triggers)
            {
                _log?.RestoreIndentLevel("triggerGroups");

                _log?.LogFormatted(LogLevel.Information, "Processing trigger with TriggerTime {TriggerTime} and TriggerEvent {TriggerEvent} for {ModelName}", args: [triggerDetails.TriggerTime, triggerDetails.TriggerEvent, model.ClrType.Name], preIncreaseLevel: true);

                var signature = $"{triggerDetails.TriggerTime}_{triggerDetails.TriggerEvent}_{model.DatabaseName}_{model.TableName}_{triggerDetails.TriggerBody}";
                _log?.LogFormatted(LogLevel.Information, "Generated signature for trigger: {Signature}", args: [signature]);

                var signatureHash = signature.Sha256Hex()[..29]; // short hash to ensure uniqueness while keeping name length reasonable
                _log?.LogFormatted(LogLevel.Information, "Generated signature hash for trigger: {SignatureHash}", args: [signatureHash]);

                var triggerName = triggerDetails.TriggerName ?? $"TR_{triggerDetails.TriggerTime.ToString()[0]}{triggerDetails.TriggerEvent.ToString()[0]}_{signatureHash}";
                _log?.LogFormatted(LogLevel.Information, "Determined trigger name: {TriggerName}", args: [triggerName]);

                if (triggerGroups.ContainsKey(triggerName))
                    throw new InvalidOperationException($"Duplicate trigger name '{triggerName}' on {model.ClrType.Name}. Trigger names must be unique.");
                
                _log?.LogFormatted(LogLevel.Information, "Adding TriggerSchema for trigger with name {TriggerName} to trigger groups for {ModelName}", args: [triggerName, model.ClrType.Name]);
                triggerGroups[triggerName] = new TriggerSchema
                {
                    TriggerName = triggerName,
                    EventManipulation = triggerDetails.TriggerEvent,
                    ActionTiming = triggerDetails.TriggerTime,
                    ActionStatement = triggerDetails.TriggerBody,
                    EventObjectTable = model.TableName
                };

                _log?.LogFormatted(LogLevel.Information, "TriggerSchema added", args: [], singleIndentLine: true);
            }
            _log?.RestoreIndentLevel("triggerGroups");

            _log?.LogFormatted(LogLevel.Information, "Completed grouping triggers into TriggerSchema definitions for {ModelName}. Total TriggerSchema count: {TriggerSchemaCount}", args: [model.ClrType.Name, triggerGroups.Count]);
            return triggerGroups;
        }

        private List<RelmStoredProcedure> GetClassFunctions(ValidatedModelType model)
        {
            _log?.LogFormatted(LogLevel.Information, "Processing class-level functions for {ModelName}", args: [model.ClrType.FullName]);

            var classFunctions = model.ClrType
                .GetCustomAttributes(true)
                .OfType<RelmStoredProcedure>()
                .Where(x => x != null)
                .ToList();
            _log?.LogFormatted(LogLevel.Information, "Found {FunctionCount} class-level functions on {ModelName}", args: [classFunctions.Count, model.ClrType.Name], preIncreaseLevel: true);

            var functions = new List<RelmStoredProcedure>();
            _log?.LogFormatted(LogLevel.Information, "Processing class-level functions for {ModelName}", args: [model.ClrType.Name]);
            _log?.SaveIndentLevel("classFunctions");
            foreach (var classFunction in classFunctions)
            {
                _log?.RestoreIndentLevel("classFunctions");

                _log?.LogFormatted(LogLevel.Information, "Processing class function named '{FunctionName}' on {ModelName}", args: [classFunction.Name, model.ClrType.FullName], preIncreaseLevel: true);
                if (string.IsNullOrWhiteSpace(classFunction.Body))
                    throw new InvalidOperationException($"Function body cannot be null or whitespace for function on {model.ClrType.FullName} with name '{classFunction.Name}'.");

                functions.Add(classFunction);
                _log?.LogFormatted(LogLevel.Information, "Added class function named '{FunctionName}' to functions list for {ModelName}", args: [classFunction.Name, model.ClrType.FullName], preIncreaseLevel: true);
            }
            _log?.RestoreIndentLevel("classFunctions");

            _log?.LogFormatted(LogLevel.Information, "Completed processing class-level functions for {ModelName}. Total functions count: {FunctionCount}", args: [model.ClrType.Name, functions.Count]);
            return functions;
        }

        private Dictionary<string, FunctionSchema> GroupFunctions(List<RelmStoredProcedure> functions, ValidatedModelType model)
        {
            _log?.LogFormatted(LogLevel.Information, "Grouping functions into FunctionSchema definitions for {ModelName}", args: [model.ClrType.FullName]);

            var functionGroups = new Dictionary<string, FunctionSchema>(StringComparer.Ordinal);
            _log?.LogFormatted(LogLevel.Information, "Processing functions for {ModelName} to create FunctionSchema definitions", args: [model.ClrType.Name], preIncreaseLevel: true);
            _log?.SaveIndentLevel("functionGroups");
            foreach (var functionDetails in functions)
            {
                _log?.RestoreIndentLevel("functionGroups");

                _log?.LogFormatted(LogLevel.Information, "Processing function with name {FunctionName} for {ModelName}", args: [functionDetails.Name, model.ClrType.Name], preIncreaseLevel: true);

                if (functionGroups.ContainsKey(functionDetails.Name))
                    throw new InvalidOperationException($"Duplicate function name '{functionDetails.Name}' on {model.ClrType.Name}. Function names must be unique.");

                _log?.LogFormatted(LogLevel.Information, "Adding FunctionSchema for function with name {FunctionName} to function groups for {ModelName}", args: [functionDetails.Name, model.ClrType.Name]);
                functionGroups[functionDetails.Name] = new FunctionSchema
                {
                    RoutineName = functionDetails.Name,
                    SpecificName = functionDetails.Name,
                    RoutineTypeValue = string.IsNullOrWhiteSpace(functionDetails.ReturnType) ? ProcedureType.StoredProcedure : ProcedureType.Function,
                    RoutineDefinition = functionDetails.Body,
                    RoutineComment = functionDetails.Comment,
                    DataType = functionDetails.ReturnType,
                    CharacterMaximumLength = functionDetails.ReturnSize,
                    NumericPrecision = functionDetails.ReturnSize,
                    NumericScale = functionDetails.ReturnSize,
                    DatetimePrecision = functionDetails.ReturnSize,
                    SqlDataAccessValue = functionDetails.DataAccess,
                    SecurityType = functionDetails.SecurityLevel,
                    IsDeterministicValue = functionDetails.IsDeterministic,
                };

                _log?.LogFormatted(LogLevel.Information, "FunctionSchema added", args: [], singleIndentLine: true);
            }
            _log?.RestoreIndentLevel("functionGroups");

            _log?.LogFormatted(LogLevel.Information, "Completed grouping functions into FunctionSchema definitions for {ModelName}. Total FunctionSchema count: {FunctionSchemaCount}", args: [model.ClrType.Name, functionGroups.Count]);
            return functionGroups;
        }
    }
}
