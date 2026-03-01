using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using CoreRelm.Models.Migrations.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Migrations.Rollback
{
    /// <summary>
    /// Thin test-only helper factory to keep tests readable while the rollback contracts settle.
    /// Replace the bodies with your real migration operation types/builders.
    /// </summary>
    internal static class RollbackTestOperationFactory
    {
        public static IMigrationOperation CreateTable(string tableName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

            return new CreateTableOperation(
                new TableSchema(
                    TableName: tableName,
                    Columns: new Dictionary<string, ColumnSchema>(),
                    Indexes: new Dictionary<string, IndexSchema>(),
                    ForeignKeys: new Dictionary<string, ForeignKeySchema>(),
                    Triggers: new Dictionary<string, TriggerSchema>()));
        }

        public static IMigrationOperation DropTable(string tableName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

            return new DropTableOperation(
                new TableSchema(
                    TableName: tableName,
                    Columns: new Dictionary<string, ColumnSchema>(),
                    Indexes: new Dictionary<string, IndexSchema>(),
                    ForeignKeys: new Dictionary<string, ForeignKeySchema>(),
                    Triggers: new Dictionary<string, TriggerSchema>()));
        }

        public static IMigrationOperation AddColumn(string tableName, string columnName, string storeType)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
            ArgumentException.ThrowIfNullOrWhiteSpace(storeType);

            return new AddColumnOperation(
                new ColumnSchema
                {
                    TableName = tableName,
                    ColumnName = columnName,
                    ColumnType = storeType
                });
        }

        public static IMigrationOperation DropColumn(string tableName, string columnName, string storeType = "varchar(100)")
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
            ArgumentException.ThrowIfNullOrWhiteSpace(storeType);

            return new DropColumnOperation(
                new ColumnSchema
                {
                    TableName = tableName,
                    ColumnName = columnName,
                    ColumnType = storeType
                });
        }

        public static IMigrationOperation AlterColumn(string tableName, string columnName, string currentStoreType, string desiredStoreType, string reason)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentStoreType);
            ArgumentException.ThrowIfNullOrWhiteSpace(desiredStoreType);
            ArgumentException.ThrowIfNullOrWhiteSpace(reason);

            return new AlterColumnOperation(
                Current: new ColumnSchema
                {
                    TableName = tableName,
                    ColumnName = columnName,
                    ColumnType = currentStoreType
                },
                Desired: new ColumnSchema
                {
                    TableName = tableName,
                    ColumnName = columnName,
                    ColumnType = desiredStoreType
                },
                Reason: reason);
        }

        public static IMigrationOperation CreateIndex(string tableName, string indexName, IReadOnlyList<string> columns)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            ArgumentException.ThrowIfNullOrWhiteSpace(indexName);
            ArgumentNullException.ThrowIfNull(columns);

            return new CreateIndexOperation(
                new IndexSchema
                {
                    TableName = tableName,
                    IndexName = indexName,
                    Columns = [.. columns.Select(x => new IndexColumnSchema(x, null, null, null, 0))],
                    ColumnName = columns.FirstOrDefault(),
                    SeqInIndex = 1
                });
        }

        public static IMigrationOperation DropIndexWithOriginalDefinition(string tableName, string indexName, IReadOnlyList<string> columns)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            ArgumentException.ThrowIfNullOrWhiteSpace(indexName);
            ArgumentNullException.ThrowIfNull(columns);

            return new DropIndexOperation(
                new IndexSchema
                {
                    TableName = tableName,
                    IndexName = indexName,
                    Columns = [.. columns.Select(x => new IndexColumnSchema(x, null, null, null, 0))],
                    ColumnName = columns.FirstOrDefault(),
                    SeqInIndex = 1
                });
        }

        public static IMigrationOperation AddForeignKey(
            string tableName,
            string foreignKeyName,
            string columnName,
            string principalTable,
            string principalColumn)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            ArgumentException.ThrowIfNullOrWhiteSpace(foreignKeyName);
            ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
            ArgumentException.ThrowIfNullOrWhiteSpace(principalTable);
            ArgumentException.ThrowIfNullOrWhiteSpace(principalColumn);

            return new AddForeignKeyOperation(
                new ForeignKeySchema
                {
                    TableName = tableName,
                    ConstraintName = foreignKeyName,
                    ColumnName = columnName,
                    ColumnNames = new List<string> { columnName },
                    ReferencedTableName = principalTable,
                    ReferencedColumnName = principalColumn,
                    ReferencedColumnNames = new List<string> { principalColumn },
                    OrdinalPosition = 1
                });
        }

        public static IMigrationOperation DropForeignKeyWithOriginalDefinition(
            string tableName,
            string foreignKeyName,
            string columnName,
            string principalTable,
            string principalColumn)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            ArgumentException.ThrowIfNullOrWhiteSpace(foreignKeyName);
            ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
            ArgumentException.ThrowIfNullOrWhiteSpace(principalTable);
            ArgumentException.ThrowIfNullOrWhiteSpace(principalColumn);

            return new DropForeignKeyOperation(
                new ForeignKeySchema
                {
                    TableName = tableName,
                    ConstraintName = foreignKeyName,
                    ColumnName = columnName,
                    ColumnNames = new List<string> { columnName },
                    ReferencedTableName = principalTable,
                    ReferencedColumnName = principalColumn,
                    ReferencedColumnNames = new List<string> { principalColumn },
                    OrdinalPosition = 1
                });
        }

        public static IMigrationOperation CreateTrigger(string triggerName, string tableName, string bodySql)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(triggerName);
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            ArgumentException.ThrowIfNullOrWhiteSpace(bodySql);

            return new CreateTriggerOperation(
                new TriggerSchema
                {
                    TriggerName = triggerName,
                    EventObjectTable = tableName,
                    ActionStatement = bodySql
                });
        }

        public static IMigrationOperation DropTriggerWithOriginalDefinition(string triggerName, string tableName, string bodySql)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(triggerName);
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            ArgumentException.ThrowIfNullOrWhiteSpace(bodySql);

            return new DropTriggerOperation(
                new TriggerSchema
                {
                    TriggerName = triggerName,
                    EventObjectTable = tableName,
                    ActionStatement = bodySql
                });
        }

        public static IMigrationOperation CreateFunction(string functionName, string bodySql)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
            ArgumentException.ThrowIfNullOrWhiteSpace(bodySql);

            return new CreateFunctionOperation(
                new FunctionSchema
                {
                    RoutineName = functionName,
                    RoutineDefinition = bodySql
                });
        }

        public static IMigrationOperation DropFunctionWithOriginalDefinition(string functionName, string bodySql)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
            ArgumentException.ThrowIfNullOrWhiteSpace(bodySql);

            return new DropFunctionOperation(
                new FunctionSchema
                {
                    RoutineName = functionName,
                    RoutineDefinition = bodySql
                });
        }

        public static IMigrationOperation NonReversibleOperation(string description = "Non-reversible test operation")
        {
            return new NonReversibleTestOperation(description);
        }

        public static IMigrationOperation UnknownReversibilityOperation(string description = "Unknown reversibility test operation")
        {
            return new UnknownReversibilityTestOperation(description);
        }

        internal sealed record NonReversibleTestOperation(string Description) : IMigrationOperation;

        internal sealed record UnknownReversibilityTestOperation(string Description) : IMigrationOperation;
    }
}
