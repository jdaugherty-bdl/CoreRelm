using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Rendering
{
    public sealed record CreateTableOperation(TableSchema Table) : IMigrationOperation
    {
        public string Description => $"Create table `{Table.TableName}`";
    }

    public sealed record AddColumnOperation(string TableName, ColumnSchema Column) : IMigrationOperation
    {
        public string Description => $"Add column `{Column.ColumnName}` to `{TableName}`";
    }

    public sealed record AlterColumnOperation(string TableName, ColumnSchema Desired, string Reason) : IMigrationOperation
    {
        public string Description => $"Alter column `{Desired.ColumnName}` on `{TableName}` ({Reason})";
    }

    public sealed record DropIndexOperation(string TableName, string IndexName) : IMigrationOperation
    {
        public string Description => $"Drop index `{IndexName}` on `{TableName}`";
    }

    public sealed record CreateIndexOperation(string TableName, IndexSchema Index) : IMigrationOperation
    {
        public string Description => $"Create index `{Index.IndexName}` on `{TableName}`";
    }

    public sealed record DropForeignKeyOperation(string TableName, string ConstraintName) : IMigrationOperation
    {
        public string Description => $"Drop foreign key `{ConstraintName}` on `{TableName}`";
    }

    public sealed record AddForeignKeyOperation(string TableName, ForeignKeySchema ForeignKey) : IMigrationOperation
    {
        public string Description => $"Add foreign key `{ForeignKey.ConstraintName}` on `{TableName}`";
    }

    public sealed record DropTriggerOperation(string TableName, string TriggerName) : IMigrationOperation
    {
        public string Description => $"Drop trigger `{TriggerName}` on `{TableName}`";
    }

    public sealed record CreateTriggerOperation(string TableName, TriggerSchema Trigger) : IMigrationOperation
    {
        public string Description => $"Create trigger `{Trigger.TriggerName}` on `{TableName}`";
    }

    public sealed record CreateFunctionOperation(string FunctionName, string CreateSql) : IMigrationOperation
    {
        public string Description => $"Create function `{FunctionName}`";
    }
}
