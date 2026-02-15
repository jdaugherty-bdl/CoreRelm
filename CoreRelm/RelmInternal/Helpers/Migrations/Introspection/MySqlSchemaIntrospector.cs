using CoreRelm.Interfaces;
using CoreRelm.Interfaces.Metadata;
using CoreRelm.Models;
using CoreRelm.Models.Migrations.Introspection;
using CoreRelm.RelmInternal.Contexts;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static CoreRelm.Enums.Indexes;

namespace CoreRelm.RelmInternal.Helpers.Migrations.Introspection
{

    internal sealed class MySqlSchemaIntrospector : IRelmSchemaIntrospector
    {
        private sealed record FkRow(
            string ConstraintName,
            string TableName,
            string ColumnName,
            string ReferencedTableName,
            string ReferencedColumnName,
            string UpdateRule,
            string DeleteRule,
            int Ordinal
        );

        public async Task<SchemaSnapshot> LoadSchemaAsync(
            InformationSchemaContext relmContext,
            SchemaIntrospectionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new SchemaIntrospectionOptions();

            var databaseName = await GetCurrentDatabaseAsync(relmContext, cancellationToken);
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new InvalidOperationException("No active database selected. Ensure the connection string specifies Database=<name>.");

            var tableNames = await LoadTableNamesAsync(relmContext, databaseName, options.IncludeViews, cancellationToken);

            // Load all schema components
            var columns = await LoadColumnsAsync(relmContext, databaseName, cancellationToken);
            var indexes = await LoadIndexesAsync(relmContext, databaseName, cancellationToken);
            var foreignKeys = await LoadForeignKeysAsync(relmContext, databaseName, cancellationToken);
            var triggers = await LoadTriggersAsync(relmContext, databaseName, cancellationToken);
            var functions = await LoadFunctionsAsync(relmContext, databaseName, cancellationToken);

            // Assemble per-table structures
            var tables = new Dictionary<string, TableSchema>(StringComparer.Ordinal);

            foreach (var tableName in tableNames?.OrderBy(t => t, StringComparer.Ordinal).ToArray() ?? [])
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    continue;

                var tableColumns = columns.TryGetValue(tableName, out var colDict)
                    ? new Dictionary<string, ColumnSchema>(colDict, StringComparer.Ordinal)
                    : new Dictionary<string, ColumnSchema>(StringComparer.Ordinal);

                var tableIndexes = indexes.TryGetValue(tableName, out var idxDict)
                    ? new Dictionary<string, IndexSchema>(idxDict, StringComparer.Ordinal)
                    : new Dictionary<string, IndexSchema>(StringComparer.Ordinal);

                var tableFks = foreignKeys.TryGetValue(tableName, out var fkDict)
                    ? new Dictionary<string, ForeignKeySchema>(fkDict, StringComparer.Ordinal)
                    : new Dictionary<string, ForeignKeySchema>(StringComparer.Ordinal);

                var tableTriggers = triggers.TryGetValue(tableName, out var trgDict)
                    ? new Dictionary<string, TriggerSchema>(trgDict, StringComparer.Ordinal)
                    : new Dictionary<string, TriggerSchema>(StringComparer.Ordinal);

                tables[tableName] = new TableSchema(
                    TableName: tableName,
                    Columns: tableColumns,
                    Indexes: tableIndexes,
                    ForeignKeys: tableFks,
                    Triggers: tableTriggers
                );
            }

            return new SchemaSnapshot(databaseName, tables, functions);
        }

        private static async Task<string?> GetCurrentDatabaseAsync(IRelmContext relmContext, CancellationToken cancellationToken)
        {
            var currentDatabase = await relmContext.GetScalarAsync<string>("SELECT DATABASE();", cancellationToken: cancellationToken);

            return currentDatabase;
        }

        private static async Task<List<string?>?> LoadTableNamesAsync(
            IRelmContext relmContext, 
            string databaseName,
            bool includeViews,
            CancellationToken cancellationToken)
        {
            var tableQuery = $@"SELECT TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = @database_name
                    AND TABLE_TYPE IN (@view_types)
                ORDER BY TABLE_NAME;";

            var result = await relmContext.GetDataListAsync<string>(tableQuery, new Dictionary<string, object>
            {
                { "@database_name", databaseName },
                { "@view_types", includeViews ? "BASE TABLE,VIEW" : "BASE TABLE" }
            }, cancellationToken: cancellationToken);

            return result?.ToList();
        }

        private static async Task<Dictionary<string, Dictionary<string, ColumnSchema>>> LoadColumnsAsync(
            IRelmContext relmContext,
            string databaseName,
            CancellationToken cancellationToken)
        {
            var columnSchemas = new Dictionary<string, Dictionary<string, ColumnSchema>>(StringComparer.Ordinal);

            /*
            TABLE_NAME, COLUMN_NAME, COLUMN_TYPE, COLUMN_KEY, IS_NULLABLE, COLUMN_DEFAULT AS default_value, EXTRA, ORDINAL_POSITION
            */
            var columnQuery = @"SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = @database_name
                ORDER BY TABLE_NAME, ORDINAL_POSITION;";

            var columnResults = (await relmContext.GetDataObjectsAsync<ColumnSchema>(columnQuery, new Dictionary<string, object>
            {
                ["@database_name"] = databaseName
            }, cancellationToken: cancellationToken))
                ?.ToList();

            if (columnResults != null)
            {
                foreach (var column in columnResults)
                {
                    if (column == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(column.TableName))
                        throw new InvalidOperationException($"Column schema {column.ColumnName} missing TableName.");

                    if (column.ColumnKey?.Contains("PRI", StringComparison.OrdinalIgnoreCase) ?? false)
                        column.IsPrimaryKey = true;

                    if (column.ColumnKey?.Contains("UNI", StringComparison.OrdinalIgnoreCase) ?? false)
                        column.IsUnique = true;

                    if (column.Extra?.Contains("auto_increment", StringComparison.OrdinalIgnoreCase) ?? false)
                        column.IsAutoIncrement = true;

                    if (!string.IsNullOrWhiteSpace(column.DefaultValue))
                        column.DefaultValue = GetDefaultClause(column.DefaultValue, column.Extra);

                    if (!columnSchemas.TryGetValue(column.TableName, out var columns))
                    {
                        columns = new Dictionary<string, ColumnSchema>(StringComparer.Ordinal);
                        columnSchemas[column.TableName] = columns;
                    }

                    columns[column.ColumnName!] = column;
                }
            }

            return columnSchemas;
        }

        private static string? GetDefaultClause(string? defaultValue, string? extra)
        {
            // If there's no default and no special behavior, return empty
            if (string.IsNullOrEmpty(defaultValue) && string.IsNullOrEmpty(extra))
                return null;

            var defaultClause = string.Empty;
            var defaultExtra = extra?.ToUpper() ?? string.Empty;

            // 1. Handle the DEFAULT part
            if (defaultValue != null)
            {
                // MySQL 8 returns 'NULL' as a string for actual NULL defaults sometimes
                if (defaultValue.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                {
                    defaultClause = "NULL";
                }
                else if (defaultExtra.Contains("DEFAULT_GENERATED"))
                {
                    // Functional defaults (like (curdate())) already come with parentheses
                    // from INFORMATION_SCHEMA, so we don't need to add them.
                    defaultClause = defaultValue;
                }
                else
                {
                    // It's a static literal. We need to check if it's numeric or needs quotes.
                    // A simple way is to check if it's already quoted or is a number.
                    bool isNumeric = double.TryParse(defaultValue, out _);
                    defaultClause = isNumeric ? defaultValue : $"'{defaultValue}'";
                }
            }

            // 2. Handle the "ON UPDATE" part (specific to Timestamps/Datetimes)
            if (defaultExtra?.Contains("ON UPDATE CURRENT_TIMESTAMP") ?? false)
            {
                // Ensure we don't double up if the default was already the timestamp
                defaultClause = $"{defaultClause} ON UPDATE CURRENT_TIMESTAMP";
            }

            return defaultClause.Trim();
        }

        private static async Task<Dictionary<string, Dictionary<string, IndexSchema>>> LoadIndexesAsync(
            IRelmContext relmContext,
            string databaseName,
            CancellationToken cancellationToken)
        {
            // We read INFORMATION_SCHEMA.STATISTICS and group by table+index name.
            // MySQL reports one row per index column.
            var indexQuery = @"SELECT TABLE_NAME, INDEX_NAME, SUB_PART, NON_UNIQUE, COLUMN_NAME, SEQ_IN_INDEX, COLLATION, 
                    EXPRESSION, INDEX_TYPE, INDEX_COMMENT, COMMENT, IS_VISIBLE
                FROM INFORMATION_SCHEMA.STATISTICS
                WHERE TABLE_SCHEMA = @database_name
                ORDER BY TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX;";

            var indexResults = (await relmContext.GetDataObjectsAsync<IndexSchema>(indexQuery, new Dictionary<string, object>
            {
                ["@database_name"] = databaseName
            }, cancellationToken: cancellationToken))
                ?.ToList();

            var indexListing = indexResults
                ?.Where(index => index != null && !string.IsNullOrWhiteSpace(index.TableName) && !string.IsNullOrWhiteSpace(index.IndexName) && !string.IsNullOrWhiteSpace(index.ColumnName))
                .GroupBy(i => (i!.TableName, i.IndexName))
                .ToDictionary(g => (g.Key.TableName, g.Key.IndexName), g => g.ToList());

            var indexSchemas = new Dictionary<string, Dictionary<string, IndexSchema>>(StringComparer.Ordinal);
            if (indexListing == null)
                return indexSchemas;

            foreach (var group in indexListing)
            {
                var tableName = group.Key.TableName!;
                var indexName = group.Key.IndexName!;
                var indexColumns = group.Value
                    .Where(r => r != null)
                    .Select(x => x!)
                    .ToList();

                var isUnique = indexColumns
                    .Any(r => !r.NonUnique);

                var columns = indexColumns
                    .OrderBy(r => r.SeqInIndex)
                    .Select(r => new IndexColumnSchema(r!.ColumnName!, r.SubPart, r.Collation == "A" ? "ASC" : "DESC", r.Expression, r.SeqInIndex))
                    .ToList();

                if (!indexSchemas.TryGetValue(tableName, out var indexes))
                {
                    indexes = new Dictionary<string, IndexSchema>(StringComparer.Ordinal);
                    indexSchemas[tableName] = indexes;
                }

                indexes[indexName] = new IndexSchema()
                {
                    TableName = tableName,
                    IndexName = indexName,
                    IndexTypeValue = isUnique ? IndexType.UNIQUE : (indexColumns.FirstOrDefault(r => r != null)?.IndexTypeValue ?? IndexType.None),
                    NonUnique = !isUnique,
                    Columns = columns
                };
            }

            return indexSchemas;
        }

        private static async Task<Dictionary<string, Dictionary<string, ForeignKeySchema>>> LoadForeignKeysAsync(
            IRelmContext relmContext,
            string databaseName,
            CancellationToken cancellationToken)
        {
            // Join KEY_COLUMN_USAGE with REFERENTIAL_CONSTRAINTS to get update/delete rules.
            // One row per constrained column.
            var foreignKeyQuery = @"SELECT kcu.CONSTRAINT_NAME, kcu.TABLE_NAME, kcu.COLUMN_NAME, kcu.REFERENCED_TABLE_NAME, kcu.REFERENCED_COLUMN_NAME, 
                    rc.UPDATE_RULE, rc.DELETE_RULE, kcu.ORDINAL_POSITION
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
                  ON rc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA
                 AND rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                WHERE kcu.CONSTRAINT_SCHEMA = @database_name
                  AND kcu.REFERENCED_TABLE_NAME IS NOT NULL
                ORDER BY kcu.TABLE_NAME, kcu.CONSTRAINT_NAME, kcu.ORDINAL_POSITION;";

            var foreignKeyResults = (await relmContext.GetDataObjectsAsync<ForeignKeySchema>(foreignKeyQuery, new Dictionary<string, object>
            {
                ["@database_name"] = databaseName
            }, cancellationToken: cancellationToken))
                ?.ToList();

            var foreignKeySchemas = foreignKeyResults
                ?.Where(fk => fk != null && !string.IsNullOrWhiteSpace(fk.ConstraintName) && !string.IsNullOrWhiteSpace(fk.TableName) && !string.IsNullOrWhiteSpace(fk.ColumnName))
                .GroupBy(fk => fk!.ConstraintName)
                .ToDictionary(x => x.Key!, x => x.Where(y => y != null).ToList());

            var byTable = new Dictionary<string, Dictionary<string, ForeignKeySchema>>(StringComparer.Ordinal);
            if (foreignKeySchemas == null)
                return byTable;

            Dictionary<string, ForeignKeySchema> foreignKeys = [];
            foreach (var kvp in foreignKeySchemas)
            {
                var constraintName = kvp.Key;
                var rows = kvp.Value;

                if (rows == null || rows.Count(x => x != null) == 0)
                    continue;

                var first = rows.First();
                var table = first!.TableName;

                var localCols = rows.OrderBy(r => r!.OrdinalPosition).Select(r => r!.ColumnName).ToList();
                var refCols = rows.OrderBy(r => r!.OrdinalPosition).Select(r => r!.ReferencedColumnName).ToList();

                var foreignKeySchema = new ForeignKeySchema
                {
                    ConstraintName = constraintName,
                    TableName = table,
                    ColumnNames = localCols,
                    ReferencedTableName = first.ReferencedTableName,
                    ReferencedColumnNames = refCols,
                    UpdateRule = first.UpdateRule,
                    DeleteRule = first.DeleteRule
                };

                if (!string.IsNullOrWhiteSpace(table) && !byTable.TryGetValue(table, out foreignKeys))
                {
                    foreignKeys = new Dictionary<string, ForeignKeySchema>(StringComparer.Ordinal);
                    byTable[table] = foreignKeys;
                }

                foreignKeys[constraintName] = foreignKeySchema;
            }

            return byTable;
        }

        private static async Task<Dictionary<string, Dictionary<string, TriggerSchema>>> LoadTriggersAsync(
            IRelmContext relmContext,
            string databaseName,
            CancellationToken cancellationToken)
        {
            var triggerQuery = @"SELECT TRIGGER_NAME, EVENT_MANIPULATION, ACTION_TIMING, EVENT_OBJECT_TABLE, ACTION_STATEMENT
                FROM INFORMATION_SCHEMA.TRIGGERS
                WHERE TRIGGER_SCHEMA = @database_name
                ORDER BY EVENT_OBJECT_TABLE, TRIGGER_NAME;";

            var triggerResults = (await relmContext.GetDataObjectsAsync<TriggerSchema>(triggerQuery, new Dictionary<string, object>
            {
                ["@database_name"] = databaseName
            }, cancellationToken: cancellationToken))
                ?.ToList();

            var triggerSchemas = triggerResults
                ?.Where(t => t != null && !string.IsNullOrWhiteSpace(t.EventObjectTable))
                .Cast<TriggerSchema>()
                .GroupBy(t => t!.EventObjectTable)
                .ToDictionary(
                    g => g.Key!,
                    g => g.ToDictionary(
                        t => t.TriggerName!,
                        t => t,
                        StringComparer.Ordinal)
                )
                ??
                [];

            return triggerSchemas;
        }

        private static async Task<Dictionary<string, FunctionSchema>> LoadFunctionsAsync(
            InformationSchemaContext relmContext,
            string dbName,
            CancellationToken ct)
        {
            var functionSchema = relmContext.GetDataSet<FunctionSchema>();
            if (functionSchema == null)
                return [];

            var fun = await functionSchema
                .Reference(x => x.FunctionParameters)
                .Where(x => x.RoutineSchema == dbName && x.RoutineType == "FUNCTION")
                .OrderBy(x => x.RoutineName!)
                .LoadAsync(ct);

            var functionQuery = @"SELECT ROUTINE_NAME, ROUTINE_COMMENT, DTD_IDENTIFIER, ROUTINE_DEFINITION, 
                    SQL_DATA_ACCESS, SECURITY_TYPE, IS_DETERMINISTIC
                FROM INFORMATION_SCHEMA.ROUTINES
                WHERE ROUTINE_SCHEMA = @database_name
                  AND ROUTINE_TYPE = 'FUNCTION'
                ORDER BY ROUTINE_NAME;";

            var functionResults = (await relmContext.GetDataObjectsAsync<FunctionSchema>(functionQuery, new Dictionary<string, object>
            {
                ["@database_name"] = dbName
            }, cancellationToken: ct))
                ?.ToList();

            var functions = functionResults
                ?.Where(f => f != null && !string.IsNullOrWhiteSpace(f.RoutineName))
                .Cast<FunctionSchema>()
                .ToDictionary(f => f!.RoutineName!, f => f, StringComparer.Ordinal)
                ??
                [];

            return functions;
        }
    }
}
