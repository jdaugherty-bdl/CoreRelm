using CoreRelm.Interfaces.Metadata;
using CoreRelm.Models.Migrations.Introspection;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
            string connectionString,
            SchemaIntrospectionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new SchemaIntrospectionOptions();

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string is required.", nameof(connectionString));

            await using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);

            var databaseName = await GetCurrentDatabaseAsync(conn, cancellationToken);
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new InvalidOperationException("No active database selected. Ensure the connection string specifies Database=<name>.");

            var tableNames = await LoadTableNamesAsync(conn, databaseName, options.IncludeViews, cancellationToken);

            // Load all schema components
            var columns = await LoadColumnsAsync(conn, databaseName, cancellationToken);
            var indexes = await LoadIndexesAsync(conn, databaseName, cancellationToken);
            var foreignKeys = await LoadForeignKeysAsync(conn, databaseName, cancellationToken);
            var triggers = await LoadTriggersAsync(conn, databaseName, cancellationToken);
            var functions = await LoadFunctionsAsync(conn, databaseName, cancellationToken);

            // Assemble per-table structures
            var tables = new Dictionary<string, TableSchema>(StringComparer.Ordinal);

            foreach (var tableName in tableNames.OrderBy(t => t, StringComparer.Ordinal))
            {
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

        private static async Task<string?> GetCurrentDatabaseAsync(MySqlConnection conn, CancellationToken cancellationToken)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DATABASE();";
            var obj = await cmd.ExecuteScalarAsync(cancellationToken);
            return obj as string;
        }

        private static async Task<List<string>> LoadTableNamesAsync(
            MySqlConnection conn, 
            string databaseName,
            bool includeViews,
            CancellationToken cancellationToken)
        {
            var result = new List<string>();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = includeViews
                ? @"SELECT TABLE_NAME
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_SCHEMA = @db
                        AND TABLE_TYPE IN ('BASE TABLE','VIEW')
                    ORDER BY TABLE_NAME;"
                : @"SELECT TABLE_NAME
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_SCHEMA = @db
                        AND TABLE_TYPE = 'BASE TABLE'
                    ORDER BY TABLE_NAME;";

            cmd.Parameters.AddWithValue("@database_name", databaseName);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(reader.GetString(0));
            }

            return result;
        }

        private static async Task<Dictionary<string, Dictionary<string, ColumnSchema>>> LoadColumnsAsync(
            MySqlConnection conn,
            string databaseName,
            CancellationToken cancellationToken)
        {
            var columnSchemas = new Dictionary<string, Dictionary<string, ColumnSchema>>(StringComparer.Ordinal);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT TABLE_NAME, COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, COLUMN_DEFAULT, EXTRA, ORDINAL_POSITION
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = @database_name
                ORDER BY TABLE_NAME, ORDINAL_POSITION;";
            cmd.Parameters.AddWithValue("@database_name", databaseName);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var tableName = reader.GetString(0);
                var columnName = reader.GetString(1);
                var columnType = reader.GetString(2);
                var isNullable = string.Equals(reader.GetString(3), "YES", StringComparison.OrdinalIgnoreCase);
                var defaultValue = reader.IsDBNull(4) ? null : reader.GetString(4);
                var extra = reader.IsDBNull(5) ? null : reader.GetString(5);
                var ordinal = reader.GetInt32(6);

                var isAuto = extra != null && extra.Contains("auto_increment", StringComparison.OrdinalIgnoreCase);

                var schema = new ColumnSchema(
                    ColumnName: columnName,
                    ColumnType: columnType,
                    IsNullable: isNullable,
                    IsPrimaryKey: false,  // to be filled in later
                    IsForeignKey: false,  // to be filled in later
                    IsReadOnly: false,    // to be filled in later
                    IsUnique: false,      // to be filled in later
                    DefaultValue: defaultValue,
                    DefaultValueSql: defaultValue,
                    IsAutoIncrement: isAuto,
                    Extra: extra,
                    OrdinalPosition: ordinal
                );

                if (!columnSchemas.TryGetValue(tableName, out var columns))
                {
                    columns = new Dictionary<string, ColumnSchema>(StringComparer.Ordinal);
                    columnSchemas[tableName] = columns;
                }

                columns[columnName] = schema;
            }

            return columnSchemas;
        }

        private static async Task<Dictionary<string, Dictionary<string, IndexSchema>>> LoadIndexesAsync(
            MySqlConnection conn,
            string databaseName,
            CancellationToken cancellationToken)
        {
            // We read INFORMATION_SCHEMA.STATISTICS and group by table+index name.
            // MySQL reports one row per index column.
            var indexListing = new Dictionary<(string Table, string Index), List<(string Column, int Seq, string? Collation, bool NonUnique)>>();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT TABLE_NAME, INDEX_NAME, NON_UNIQUE, COLUMN_NAME, SEQ_IN_INDEX, COLLATION
                FROM INFORMATION_SCHEMA.STATISTICS
                WHERE TABLE_SCHEMA = @database_name
                ORDER BY TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX;";
            cmd.Parameters.AddWithValue("@database_name", databaseName);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var table = reader.GetString(0);
                var index = reader.GetString(1);
                var nonUnique = reader.GetInt32(2) == 1;
                var columnName = reader.GetString(3);
                var sequenceInIndex = reader.GetInt32(4);
                var collation = reader.IsDBNull(5) ? null : reader.GetString(5);

                var tableKey = (table, index);
                if (!indexListing.TryGetValue(tableKey, out var indexList))
                {
                    indexList = [];
                    indexListing[tableKey] = indexList;
                }

                indexList.Add((columnName, sequenceInIndex, collation, nonUnique));
            }

            var indexSchemas = new Dictionary<string, Dictionary<string, IndexSchema>>(StringComparer.Ordinal);
            foreach (var group in indexListing)
            {
                var tableName = group.Key.Table;
                var indexName = group.Key.Index;
                var rows = group.Value;

                var isUnique = !rows.Any(r => r.NonUnique);

                var columns = rows
                    .OrderBy(r => r.Seq)
                    .Select(r => new IndexColumnSchema(r.Column, r.Seq, r.Collation))
                    .ToList();

                if (!indexSchemas.TryGetValue(tableName, out var indexes))
                {
                    indexes = new Dictionary<string, IndexSchema>(StringComparer.Ordinal);
                    indexSchemas[tableName] = indexes;
                }

                indexes[indexName] = new IndexSchema(indexName, isUnique, columns);
            }

            return indexSchemas;
        }

        private static async Task<Dictionary<string, Dictionary<string, ForeignKeySchema>>> LoadForeignKeysAsync(
            MySqlConnection conn,
            string databaseName,
            CancellationToken cancellationToken)
        {
            // Join KEY_COLUMN_USAGE with REFERENTIAL_CONSTRAINTS to get update/delete rules.
            // One row per constrained column.
            var foreignKeySchemas = new Dictionary<string, List<FkRow>>(StringComparer.Ordinal);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT kcu.CONSTRAINT_NAME, kcu.TABLE_NAME, kcu.COLUMN_NAME, kcu.REFERENCED_TABLE_NAME, kcu.REFERENCED_COLUMN_NAME, 
                    rc.UPDATE_RULE, rc.DELETE_RULE, kcu.ORDINAL_POSITION
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
                  ON rc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA
                 AND rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                WHERE kcu.CONSTRAINT_SCHEMA = @database_name
                  AND kcu.REFERENCED_TABLE_NAME IS NOT NULL
                ORDER BY kcu.TABLE_NAME, kcu.CONSTRAINT_NAME, kcu.ORDINAL_POSITION;";
            cmd.Parameters.AddWithValue("@database_name", databaseName);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var constraint = reader.GetString(0);
                var table = reader.GetString(1);
                var columnName = reader.GetString(2);
                var referencedTable = reader.GetString(3);
                var referencedColumn = reader.GetString(4);
                var updateRule = reader.GetString(5);
                var deleteRule = reader.GetString(6);
                var ordinal = reader.GetInt32(7);

                if (!foreignKeySchemas.TryGetValue(constraint, out var list))
                {
                    list = [];
                    foreignKeySchemas[constraint] = list;
                }

                list.Add(new FkRow(constraint, table, columnName, referencedTable, referencedColumn, updateRule, deleteRule, ordinal));
            }

            var byTable = new Dictionary<string, Dictionary<string, ForeignKeySchema>>(StringComparer.Ordinal);

            foreach (var kvp in foreignKeySchemas)
            {
                var constraintName = kvp.Key;
                var rows = kvp.Value;

                var first = rows.First();
                var table = first.TableName;

                var localCols = rows.OrderBy(r => r.Ordinal).Select(r => r.ColumnName).ToList();
                var refCols = rows.OrderBy(r => r.Ordinal).Select(r => r.ReferencedColumnName).ToList();

                var foreignKeySchema = new ForeignKeySchema(
                    ConstraintName: constraintName,
                    TableName: table,
                    ColumnNames: localCols,
                    ReferencedTableName: first.ReferencedTableName,
                    ReferencedColumnNames: refCols,
                    UpdateRule: first.UpdateRule,
                    DeleteRule: first.DeleteRule
                );

                if (!byTable.TryGetValue(table, out var foreignKeys))
                {
                    foreignKeys = new Dictionary<string, ForeignKeySchema>(StringComparer.Ordinal);
                    byTable[table] = foreignKeys;
                }

                foreignKeys[constraintName] = foreignKeySchema;
            }

            return byTable;
        }

        private static async Task<Dictionary<string, Dictionary<string, TriggerSchema>>> LoadTriggersAsync(
            MySqlConnection conn,
            string databaseName,
            CancellationToken cancellationToken)
        {
            var triggerSchemas = new Dictionary<string, Dictionary<string, TriggerSchema>>(StringComparer.Ordinal);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT TRIGGER_NAME, EVENT_MANIPULATION, ACTION_TIMING, EVENT_OBJECT_TABLE, ACTION_STATEMENT
                FROM INFORMATION_SCHEMA.TRIGGERS
                WHERE TRIGGER_SCHEMA = @database_name
                ORDER BY EVENT_OBJECT_TABLE, TRIGGER_NAME;";
            cmd.Parameters.AddWithValue("@database_name", databaseName);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var triggerName = reader.GetString(0);
                var eventManipulation = reader.GetString(1);
                var timing = reader.GetString(2);
                var table = reader.GetString(3);
                var stmt = reader.GetString(4);

                var schema = new TriggerSchema(
                    TriggerName: triggerName,
                    EventManipulation: eventManipulation,
                    ActionTiming: timing,
                    ActionStatement: stmt
                );

                if (!triggerSchemas.TryGetValue(table, out var trigger))
                {
                    trigger = new Dictionary<string, TriggerSchema>(StringComparer.Ordinal);
                    triggerSchemas[table] = trigger;
                }

                trigger[triggerName] = schema;
            }

            return triggerSchemas;
        }

        private static async Task<Dictionary<string, FunctionSchema>> LoadFunctionsAsync(
            MySqlConnection conn,
            string dbName,
            CancellationToken ct)
        {
            var functions = new Dictionary<string, FunctionSchema>(StringComparer.OrdinalIgnoreCase);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT
                  ROUTINE_NAME,
                  DTD_IDENTIFIER,
                  ROUTINE_DEFINITION
                FROM INFORMATION_SCHEMA.ROUTINES
                WHERE ROUTINE_SCHEMA = @db
                  AND ROUTINE_TYPE = 'FUNCTION'
                ORDER BY ROUTINE_NAME;";
            cmd.Parameters.AddWithValue("@db", dbName);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var name = reader.GetString(0);
                var returnType = reader.IsDBNull(1) ? "" : reader.GetString(1);
                var def = reader.IsDBNull(2) ? "" : reader.GetString(2);

                functions[name] = new FunctionSchema(
                    FunctionName: name,
                    ReturnType: returnType,
                    RoutineDefinition: def
                );
            }

            return functions;
        }
    }
}
