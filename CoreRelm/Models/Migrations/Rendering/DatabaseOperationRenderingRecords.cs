using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Rendering
{
    /******************************** TABLE OPERATIONS *********************************/
    public sealed record CreateTableOperation(TableSchema Table) : IMigrationOperation
    {
        public string Description => $"Create table `{Table.TableName}`";
    }

    public sealed record DropTableOperation(TableSchema Table) : IMigrationOperation
    {
        public string Description => $"Drop table `{Table.TableName}`";
    }
    
    /******************************** COLUMN OPERATIONS *********************************/
    public sealed record AddColumnOperation(ColumnSchema Column) : IMigrationOperation
    {
        public string Description => $"Add column `{Column.TableName}`.`{Column.ColumnName}`";
    }

    public sealed record AlterColumnOperation(ColumnSchema Current, ColumnSchema Desired, string Reason) : IMigrationOperation
    {
        public string Description => $"Alter column `{Current.TableName}`.`{Current.ColumnName}` => `{Desired.TableName}`.`{Desired.ColumnName}` (Reason: {Reason})";
    }

    public sealed record DropColumnOperation(ColumnSchema Column) : IMigrationOperation
    {
        public string Description => $"Drop column `{Column.TableName}`.`{Column.ColumnName}`";
    }

    /******************************** INDEX OPERATIONS *********************************/
    public sealed record CreateIndexOperation(IndexSchema Index) : IMigrationOperation
    {
        public string Description => $"Create index `{Index.TableName}`.`{Index.IndexName}`";
    }
    public sealed record DropIndexOperation(IndexSchema Index) : IMigrationOperation
    {
        public string Description => $"Drop index `{Index.TableName}`.`{Index.IndexName}`";
    }


    /******************************** FOREIGN KEY OPERATIONS *********************************/
    public sealed record AddForeignKeyOperation(ForeignKeySchema ForeignKey) : IMigrationOperation
    {
        public string Description => $"Add foreign key `{ForeignKey.TableName}`.`{ForeignKey.ConstraintName}`";
    }
    public sealed record DropForeignKeyOperation(ForeignKeySchema ForeignKey) : IMigrationOperation
    {
        public string Description => $"Drop foreign key `{ForeignKey.TableName}`.`{ForeignKey.ConstraintName}`";
    }


    /******************************** TRIGGER OPERATIONS *********************************/
    public sealed record CreateTriggerOperation(TriggerSchema Trigger) : IMigrationOperation
    {
        public string? TableName => Trigger.EventObjectTable;
        public string Description => $"Create trigger `{TableName}`.`{Trigger.TriggerName}`";
    }
    public sealed record DropTriggerOperation(TriggerSchema Trigger) : IMigrationOperation
    {
        public string? TableName => Trigger.EventObjectTable;
        public string Description => $"Drop trigger `{TableName}`.`{Trigger.TriggerName}`";
    }


    /******************************** FUNCTION (STORED PROCEDURE) OPERATIONS *********************************/
    public sealed record CreateFunctionOperation(FunctionSchema Function) : IMigrationOperation
    {
        public string? FunctionName => Function.RoutineName;
        public string Description => $"Create function `{FunctionName}`";
    }
    public sealed record DropFunctionOperation(FunctionSchema Function) : IMigrationOperation
    {
        public string? FunctionName => Function.RoutineName;
        public string Description => $"Drop function `{FunctionName}`";
    }
}
