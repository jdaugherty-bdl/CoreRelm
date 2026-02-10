using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using CoreRelm.Models.Migrations.MigrationPlans;
using CoreRelm.Models.Migrations.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Indexes;
using static CoreRelm.Enums.SecurityEnums;

namespace CoreRelm.RelmInternal.Helpers.Migrations.Rendering
{
    public sealed class MySqlMigrationSqlRenderer : IRelmMigrationSqlRenderer
    {
        public string Render(MigrationPlan plan, MySqlRenderOptions? options = null)
        {
            options ??= new MySqlRenderOptions();

            var query = new StringBuilder();

            query.AppendLine("-- =============================================");
            query.AppendLine($"-- LEDGERLITE");
            query.AppendLine($"-- Migration Script for database: {plan.DatabaseName}");
            query.AppendLine($"-- Generated UTC: {plan.StampUtc:yyyy-MM-ddTHH:mm:ssZ}");
            query.AppendLine("-- =============================================");
            query.AppendLine();

            if (options.IncludeUseDatabase)
            {
                query.AppendLine($"CREATE DATABASE IF NOT EXISTS `{EscapeIdentifier(plan.DatabaseName)}`;");
                query.AppendLine($"USE `{EscapeIdentifier(plan.DatabaseName)}`;");
                query.AppendLine();
            }

            if (plan.Blockers.Count > 0)
            {
                query.AppendLine("-- BLOCKERS (migration cannot be applied safely):");
                foreach (var blocker in plan.Blockers)
                    query.AppendLine($"--   - {blocker}");
                query.AppendLine();
                // We still render ops, but caller should treat blockers as fatal unless overridden.
            }

            if (plan.Warnings.Count > 0)
            {
                query.AppendLine("-- WARNINGS:");
                foreach (var warning in plan.Warnings)
                    query.AppendLine($"--   - {warning}");
                query.AppendLine();
            }

            foreach (var operation in plan.Operations)
            {
                query.AppendLine($"-- {operation.Description}");
                RenderOperation(query, operation, options);
                query.AppendLine();
            }

            return query.ToString();
        }

        private static void RenderOperation(StringBuilder query, IMigrationOperation operation, MySqlRenderOptions options)
        {
            switch (operation)
            {
                case CreateTableOperation createTableOperation:
                    RenderCreateTable(query, createTableOperation.Table, options);
                    break;
                case AddColumnOperation addColumnOperation:
                    query.AppendLine($"ALTER TABLE `{EscapeIdentifier(addColumnOperation.TableName)}` ADD COLUMN {RenderColumnDefinition(addColumnOperation.Column)};");
                    break;
                case AlterColumnOperation alterColumnOperation:
                    query.AppendLine($"ALTER TABLE `{EscapeIdentifier(alterColumnOperation.TableName)}` MODIFY COLUMN {RenderColumnDefinition(alterColumnOperation.Desired)};");
                    break;
                case DropIndexOperation dropIndexOperation:
                    // MySQL requires DROP PRIMARY KEY for primary; we won't emit that here.
                    if (string.Equals(dropIndexOperation.IndexName, "PRIMARY", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Refusing to render DROP PRIMARY KEY via DropIndexOperation.");
                    query.AppendLine($"DROP INDEX `{EscapeIdentifier(dropIndexOperation.IndexName)}` ON `{EscapeIdentifier(dropIndexOperation.TableName)}`;");
                    break;
                case CreateIndexOperation createIndexOperation:
                    RenderCreateIndex(query, createIndexOperation.TableName, createIndexOperation.Index);
                    break;
                case DropForeignKeyOperation dropForeignKeyOperation:
                    query.AppendLine($"ALTER TABLE `{EscapeIdentifier(dropForeignKeyOperation.TableName)}` DROP FOREIGN KEY `{EscapeIdentifier(dropForeignKeyOperation.ConstraintName)}`;");
                    break;
                case AddForeignKeyOperation addForeignKeyOperation:
                    RenderAddForeignKey(query, addForeignKeyOperation.TableName, addForeignKeyOperation.ForeignKey);
                    break;
                case DropTriggerOperation dropTriggerOperation:
                    query.AppendLine($"DROP TRIGGER IF EXISTS `{EscapeIdentifier(dropTriggerOperation.TriggerName)}`;");
                    break;
                case CreateTriggerOperation createTriggerOperation:
                    RenderCreateTrigger(query, createTriggerOperation.TableName, createTriggerOperation.Trigger, options);
                    break;
                case CreateFunctionOperation createFunctionOperation:
                    RenderCreateFunction(query, createFunctionOperation.Function, options);
                    break;
                default:
                    throw new NotSupportedException($"Unknown migration operation type: {operation.GetType().FullName}");
            }
        }

        private static void RenderCreateTable(StringBuilder query, TableSchema tableSchema, MySqlRenderOptions options)
        {
            // Render CREATE TABLE with columns + primary key + unique keys + non-unique indexes + foreign keys.
            // Triggers are rendered after table creation (MySQL doesn't support triggers inside CREATE TABLE).

            query.AppendLine($"CREATE TABLE `{EscapeIdentifier(tableSchema.TableName)}` (");

            // Columns in ordinal order
            var orderedColumnList = tableSchema.Columns.Values.OrderBy(c => c.OrdinalPosition).ToList();
            var lines = new List<string>();

            foreach (var col in orderedColumnList)
            {
                lines.Add("  " + RenderColumnDefinition(col));
            }

            // Primary key (if any)
            var primaryKeys = orderedColumnList.Where(c => c.IsPrimaryKey).OrderBy(c => c.OrdinalPosition).Select(c => c.ColumnName).ToList();
            if (primaryKeys.Count > 0)
            {
                lines.Add($"  PRIMARY KEY ({string.Join(", ", primaryKeys.Select(c => $"`{EscapeIdentifier(c)}`"))})");
            }

            // Unique keys for columns marked unique (single-column)
            foreach (var uniqueColumn in orderedColumnList.Where(c => c.IsUnique).OrderBy(c => c.ColumnName, StringComparer.OrdinalIgnoreCase))
            {
                // Skip if it's already part of PRIMARY KEY (redundant)
                if (primaryKeys.Contains(uniqueColumn.ColumnName, StringComparer.OrdinalIgnoreCase))
                    continue;

                var uniqueName = $"UQ_{tableSchema.TableName}_{uniqueColumn.ColumnName}";
                lines.Add($"  UNIQUE KEY `{EscapeIdentifier(uniqueName)}` (`{EscapeIdentifier(uniqueColumn.ColumnName)}`)");
            }

            // Indexes (non-primary). If IndexName == PRIMARY, skip (already handled).
            foreach (var index in tableSchema.Indexes.Values
                         .Where(i => !string.Equals(i.IndexName, "PRIMARY", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(i => i.IndexName, StringComparer.Ordinal))
            {
                lines.Add("  " + RenderInlineIndex(index));
            }

            // Foreign keys (inline)
            foreach (var foreignKey in tableSchema.ForeignKeys.Values.OrderBy(f => f.ConstraintName, StringComparer.Ordinal))
            {
                lines.Add("  " + RenderInlineForeignKey(foreignKey));
            }

            query.AppendLine(string.Join(",\n", lines));
            query.AppendLine($") ENGINE=InnoDB;");
            query.AppendLine();

            // Triggers after table creation (if present in desired schema)
            if (tableSchema.Triggers.Count > 0)
            {
                foreach (var trigger in tableSchema.Triggers.Values.OrderBy(t => t.TriggerName, StringComparer.Ordinal))
                {
                    RenderCreateTrigger(query, tableSchema.TableName, trigger, options);
                    query.AppendLine();
                }
            }
        }

        private static string RenderColumnDefinition(ColumnSchema columnSchema)
        {
            // ColumnType is assumed to be a MySQL type string (e.g. "varchar(45)", "bigint", "timestamp")
            // DefaultValueSql is used as-is (caller should provide correct SQL).
            var columnDefinition = new StringBuilder();
            columnDefinition.Append($"`{EscapeIdentifier(columnSchema.ColumnName)}` {columnSchema.ColumnType}");

            columnDefinition.Append(columnSchema.IsNullableBool ? " NULL" : " NOT NULL");

            // Default value (if provided)
            if (!string.IsNullOrWhiteSpace(columnSchema.DefaultValue))
                columnDefinition.Append($" DEFAULT {columnSchema.DefaultValue}");

            if (columnSchema.IsAutoIncrement)
                columnDefinition.Append(" AUTO_INCREMENT");

            return columnDefinition.ToString();
        }

        private static void RenderCreateIndex(StringBuilder query, string tableName, IndexSchema indexSchema)
        {
            //var unique = indexSchema.IsUnique ? "UNIQUE" : "";
            query.Append($"CREATE {(indexSchema.IndexTypeValue == IndexType.None ? null : indexSchema.IndexTypeValue.ToString())} INDEX `{EscapeIdentifier(indexSchema.IndexName)}` ON `{EscapeIdentifier(tableName)}` (");

            var cols = indexSchema.Columns
                ?.OrderBy(c => c.SeqInIndex)
                .Select(c => $"`{EscapeIdentifier(c.ColumnName)}` {c.Collation}");

            query.Append(string.Join(", ", cols ?? []));
            query.AppendLine(");");
        }

        private static string RenderInlineIndex(IndexSchema indexSchema)
        {
            var keyword = $"{(indexSchema.IndexTypeValue == IndexType.UNIQUE ? IndexType.UNIQUE.ToString() : null)} KEY";

            var indexColumns = indexSchema.Columns
                ?.OrderBy(c => c.SeqInIndex)
                .Select(c => $"`{EscapeIdentifier(c.ColumnName)}` {c.Collation}");

            return $"{keyword} `{EscapeIdentifier(indexSchema.IndexName)}` ({string.Join(", ", indexColumns ?? [])})";
        }

        private static void RenderAddForeignKey(StringBuilder query, string tableName, ForeignKeySchema foreignKeySchema)
        {
            query.AppendLine($"ALTER TABLE `{EscapeIdentifier(tableName)}`");
            query.AppendLine($"  ADD CONSTRAINT `{EscapeIdentifier(foreignKeySchema.ConstraintName)}`");
            query.AppendLine($"  FOREIGN KEY ({string.Join(", ", foreignKeySchema.ColumnNames?.Select(c => $"`{EscapeIdentifier(c)}`") ?? [])})");
            query.AppendLine($"  REFERENCES `{EscapeIdentifier(foreignKeySchema.ReferencedTableName)}` ({string.Join(", ", foreignKeySchema.ReferencedColumnNames?.Select(c => $"`{EscapeIdentifier(c)}`") ?? [])})");
            query.AppendLine($"  ON DELETE {foreignKeySchema.DeleteRule}");
            query.AppendLine($"  ON UPDATE {foreignKeySchema.UpdateRule};");
        }

        private static string RenderInlineForeignKey(ForeignKeySchema inlineForeignKey)
        {
            var localCols = string.Join(", ", inlineForeignKey.ColumnNames?.Select(c => $"`{EscapeIdentifier(c)}`") ?? []);
            var refCols = string.Join(", ", inlineForeignKey.ReferencedColumnNames?.Select(c => $"`{EscapeIdentifier(c)}`") ?? []);

            return $"CONSTRAINT `{EscapeIdentifier(inlineForeignKey.ConstraintName)}` FOREIGN KEY ({localCols}) " +
                   $"REFERENCES `{EscapeIdentifier(inlineForeignKey.ReferencedTableName)}` ({refCols}) " +
                   $"ON DELETE {inlineForeignKey.DeleteRule} ON UPDATE {inlineForeignKey.UpdateRule}";
        }

        private static void RenderCreateTrigger(StringBuilder query, string tableName, TriggerSchema triggerSchema, MySqlRenderOptions options)
        {
            // MySQL needs delimiter changes if trigger body contains semicolons or BEGIN/END blocks.
            // We'll always wrap if WrapTriggersWithDelimiter = true.
            if (options.WrapTriggersWithDelimiter)
                query.AppendLine($"DELIMITER {options.TriggerDelimiter}");

            query.Append($"CREATE TRIGGER `{EscapeIdentifier(triggerSchema.TriggerName)}` {triggerSchema.ActionTiming} {triggerSchema.EventManipulation} ON `{EscapeIdentifier(tableName)}`");
            query.AppendLine();
            query.AppendLine("FOR EACH ROW");
            query.AppendLine((triggerSchema?.ActionStatement?.TrimEnd().EndsWith(";") ?? false)
                ? triggerSchema.ActionStatement.TrimEnd()
                : (triggerSchema?.ActionStatement?.TrimEnd() + ";"));

            if (options.WrapTriggersWithDelimiter)
            {
                query.AppendLine($"{options.TriggerDelimiter}");
                query.AppendLine("DELIMITER ;");
            }
        }

        private static void RenderCreateFunction(StringBuilder query, FunctionSchema functionSchema, MySqlRenderOptions options)
        {
            // MySQL needs delimiter changes if function body contains semicolons or BEGIN/END blocks.
            // We'll always wrap if WrapFunctionsWithDelimiter = true.
            if (options.WrapFunctionsWithDelimiter)
                query.AppendLine($"DELIMITER {options.FunctionDelimiter}");

            query.AppendLine($"CREATE FUNCTION `{EscapeIdentifier(functionSchema.RoutineName)}` () RETURNS {functionSchema.DtdIdentifier}");

            if (functionSchema.IsDeterministicBool)
                query.AppendLine(" DETERMINISTIC");
            else
                query.AppendLine(" NOT DETERMINISTIC"); 

            if (functionSchema.SecurityType != SqlSecurityLevel.None) // None is default, so only specify if different
                query.AppendLine($" SQL SECURITY {functionSchema.SecurityType}");

            if (!string.IsNullOrWhiteSpace(functionSchema.SqlDataAccess))
                query.AppendLine($" {functionSchema.SqlDataAccess}");

            if (!string.IsNullOrWhiteSpace(functionSchema.RoutineComment))
                query.AppendLine($" COMMENT '{functionSchema.RoutineComment.Replace("'", "''")}'");

            query.AppendLine("BEGIN");
            query.AppendLine(functionSchema.RoutineDefinition);
            if (!(functionSchema.RoutineDefinition?.TrimEnd().EndsWith(";") ?? false))
                query.AppendLine(";");
            query.AppendLine("END");

            if (options.WrapFunctionsWithDelimiter)
            {
                query.AppendLine($"{options.FunctionDelimiter}");
                query.AppendLine("DELIMITER ;");
            }
        }

        private static string? EscapeIdentifier(string? originalIdentifier)
        {
            // For backtick-quoted identifiers, escape backticks by doubling.
            return originalIdentifier?.Replace("`", "``", StringComparison.Ordinal);
        }
    }
}
