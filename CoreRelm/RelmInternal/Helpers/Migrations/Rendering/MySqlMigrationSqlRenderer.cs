using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using CoreRelm.Models.Migrations.MigrationPlans;
using CoreRelm.Models.Migrations.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Migrations.Rendering
{
    public sealed class MySqlMigrationSqlRenderer : IRelmMigrationSqlRenderer
    {
        public string Render(MigrationPlan plan, MySqlRenderOptions? options = null)
        {
            options ??= new MySqlRenderOptions();

            var sb = new StringBuilder();

            sb.AppendLine("-- =============================================");
            sb.AppendLine($"-- Migration Script for database: {plan.DatabaseName}");
            sb.AppendLine($"-- Generated UTC: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
            sb.AppendLine("-- =============================================");
            sb.AppendLine();

            if (options.IncludeUseDatabase)
            {
                sb.AppendLine($"CREATE DATABASE IF NOT EXISTS `{EscapeIdentifier(plan.DatabaseName)}`;");
                sb.AppendLine($"USE `{EscapeIdentifier(plan.DatabaseName)}`;");
                sb.AppendLine();
            }

            if (plan.Blockers.Count > 0)
            {
                sb.AppendLine("-- BLOCKERS (migration cannot be applied safely):");
                foreach (var b in plan.Blockers)
                    sb.AppendLine($"--   - {b}");
                sb.AppendLine();
                // We still render ops, but caller should treat blockers as fatal unless overridden.
            }

            if (plan.Warnings.Count > 0)
            {
                sb.AppendLine("-- WARNINGS:");
                foreach (var w in plan.Warnings)
                    sb.AppendLine($"--   - {w}");
                sb.AppendLine();
            }

            foreach (var op in plan.Operations)
            {
                sb.AppendLine($"-- {op.Description}");
                RenderOperation(sb, op, options);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static void RenderOperation(StringBuilder sb, IMigrationOperation op, MySqlRenderOptions options)
        {
            switch (op)
            {
                case CreateTableOperation c:
                    RenderCreateTable(sb, c.Table, options);
                    break;
                case AddColumnOperation a:
                    sb.AppendLine($"ALTER TABLE `{EscapeIdentifier(a.TableName)}` ADD COLUMN {RenderColumnDefinition(a.Column)};");
                    break;
                case AlterColumnOperation m:
                    sb.AppendLine($"ALTER TABLE `{EscapeIdentifier(m.TableName)}` MODIFY COLUMN {RenderColumnDefinition(m.Desired)};");
                    break;
                case DropIndexOperation di:
                    // MySQL requires DROP PRIMARY KEY for primary; we won't emit that here.
                    if (string.Equals(di.IndexName, "PRIMARY", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Refusing to render DROP PRIMARY KEY via DropIndexOperation.");
                    sb.AppendLine($"DROP INDEX `{EscapeIdentifier(di.IndexName)}` ON `{EscapeIdentifier(di.TableName)}`;");
                    break;
                case CreateIndexOperation ci:
                    RenderCreateIndex(sb, ci.TableName, ci.Index);
                    break;
                case DropForeignKeyOperation dfk:
                    sb.AppendLine($"ALTER TABLE `{EscapeIdentifier(dfk.TableName)}` DROP FOREIGN KEY `{EscapeIdentifier(dfk.ConstraintName)}`;");
                    break;
                case AddForeignKeyOperation afk:
                    RenderAddForeignKey(sb, afk.TableName, afk.ForeignKey);
                    break;
                case DropTriggerOperation dt:
                    sb.AppendLine($"DROP TRIGGER IF EXISTS `{EscapeIdentifier(dt.TriggerName)}`;");
                    break;
                case CreateTriggerOperation ct:
                    RenderCreateTrigger(sb, ct.TableName, ct.Trigger, options);
                    break;
                case CreateFunctionOperation cf:
                    sb.AppendLine(cf.CreateSql.TrimEnd().EndsWith(";")
                        ? cf.CreateSql.TrimEnd()
                        : cf.CreateSql.TrimEnd() + ";");
                    break;
                default:
                    throw new NotSupportedException($"Unknown migration op type: {op.GetType().FullName}");
            }
        }

        private static void RenderCreateTable(StringBuilder sb, TableSchema table, MySqlRenderOptions options)
        {
            // Render CREATE TABLE with columns + primary key + unique keys + non-unique indexes + foreign keys.
            // Triggers are rendered after table creation (MySQL doesn't support triggers inside CREATE TABLE).

            sb.AppendLine($"CREATE TABLE `{EscapeIdentifier(table.TableName)}` (");

            // Columns in ordinal order
            var cols = table.Columns.Values.OrderBy(c => c.OrdinalPosition).ToList();
            var lines = new List<string>();

            foreach (var col in cols)
            {
                lines.Add("  " + RenderColumnDefinition(col));
            }

            // Primary key (if any)
            var pkCols = cols.Where(c => c.IsPrimaryKey).OrderBy(c => c.OrdinalPosition).Select(c => c.ColumnName).ToList();
            if (pkCols.Count > 0)
            {
                lines.Add($"  PRIMARY KEY ({string.Join(", ", pkCols.Select(c => $"`{EscapeIdentifier(c)}`"))})");
            }

            // Unique keys for columns marked unique (single-column)
            foreach (var col in cols.Where(c => c.IsUnique).OrderBy(c => c.ColumnName, StringComparer.OrdinalIgnoreCase))
            {
                // Skip if it's already part of PRIMARY KEY (redundant)
                if (pkCols.Contains(col.ColumnName, StringComparer.OrdinalIgnoreCase))
                    continue;

                var uqName = $"uq_{table.TableName}_{col.ColumnName}";
                lines.Add($"  UNIQUE KEY `{EscapeIdentifier(uqName)}` (`{EscapeIdentifier(col.ColumnName)}`)");
            }

            // Indexes (non-primary). If IndexName == PRIMARY, skip (already handled).
            foreach (var idx in table.Indexes.Values
                         .Where(i => !string.Equals(i.IndexName, "PRIMARY", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(i => i.IndexName, StringComparer.Ordinal))
            {
                lines.Add("  " + RenderInlineIndex(idx));
            }

            // Foreign keys (inline)
            foreach (var fk in table.ForeignKeys.Values.OrderBy(f => f.ConstraintName, StringComparer.Ordinal))
            {
                lines.Add("  " + RenderInlineForeignKey(fk));
            }

            sb.AppendLine(string.Join(",\n", lines));
            sb.AppendLine($") ENGINE=InnoDB;");
            sb.AppendLine();

            // Triggers after table creation (if present in desired schema)
            if (table.Triggers.Count > 0)
            {
                foreach (var trg in table.Triggers.Values.OrderBy(t => t.TriggerName, StringComparer.Ordinal))
                {
                    RenderCreateTrigger(sb, table.TableName, trg, options);
                    sb.AppendLine();
                }
            }
        }

        private static string RenderColumnDefinition(ColumnSchema c)
        {
            // ColumnType is assumed to be a MySQL type string (e.g. "varchar(45)", "bigint", "timestamp")
            // DefaultValueSql is used as-is (caller should provide correct SQL).
            var sb = new StringBuilder();
            sb.Append($"`{EscapeIdentifier(c.ColumnName)}` {c.ColumnType}");

            sb.Append(c.IsNullable ? " NULL" : " NOT NULL");

            // Default value (if provided)
            if (!string.IsNullOrWhiteSpace(c.DefaultValue))
            {
                // For timestamp defaults like "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP",
                // MySQL expects "DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP"
                if (c.DefaultValue.Contains("ON UPDATE", StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append($" DEFAULT {c.DefaultValue}");
                }
                else
                {
                    sb.Append($" DEFAULT {c.DefaultValue}");
                }
            }

            if (c.IsAutoIncrement)
                sb.Append(" AUTO_INCREMENT");

            return sb.ToString();
        }

        private static void RenderCreateIndex(StringBuilder sb, string tableName, IndexSchema idx)
        {
            var unique = idx.IsUnique ? "UNIQUE " : "";
            sb.Append($"CREATE {unique}INDEX `{EscapeIdentifier(idx.IndexName)}` ON `{EscapeIdentifier(tableName)}` (");

            var cols = idx.Columns.OrderBy(c => c.SeqInIndex).Select(c =>
            {
                var dir = c.Collation?.Equals("D", StringComparison.OrdinalIgnoreCase) == true ? " DESC"
                       : c.Collation?.Equals("A", StringComparison.OrdinalIgnoreCase) == true ? " ASC"
                       : ""; // unspecified
                return $"`{EscapeIdentifier(c.ColumnName)}`{dir}";
            });

            sb.Append(string.Join(", ", cols));
            sb.AppendLine(");");
        }

        private static string RenderInlineIndex(IndexSchema idx)
        {
            var keyword = idx.IsUnique ? "UNIQUE KEY" : "KEY";

            var cols = idx.Columns
                .OrderBy(c => c.SeqInIndex)
                .Select(c =>
                {
                    var dir = c.Collation?.Equals("D", StringComparison.OrdinalIgnoreCase) == true ? " DESC"
                           : c.Collation?.Equals("A", StringComparison.OrdinalIgnoreCase) == true ? " ASC"
                           : "";
                    return $"`{EscapeIdentifier(c.ColumnName)}`{dir}";
                });

            return $"{keyword} `{EscapeIdentifier(idx.IndexName)}` ({string.Join(", ", cols)})";
        }

        private static void RenderAddForeignKey(StringBuilder sb, string tableName, ForeignKeySchema fk)
        {
            sb.AppendLine($"ALTER TABLE `{EscapeIdentifier(tableName)}`");
            sb.AppendLine($"  ADD CONSTRAINT `{EscapeIdentifier(fk.ConstraintName)}`");
            sb.AppendLine($"  FOREIGN KEY ({string.Join(", ", fk.ColumnNames.Select(c => $"`{EscapeIdentifier(c)}`"))})");
            sb.AppendLine($"  REFERENCES `{EscapeIdentifier(fk.ReferencedTableName)}` ({string.Join(", ", fk.ReferencedColumnNames.Select(c => $"`{EscapeIdentifier(c)}`"))})");
            sb.AppendLine($"  ON DELETE {fk.DeleteRule}");
            sb.AppendLine($"  ON UPDATE {fk.UpdateRule};");
        }

        private static string RenderInlineForeignKey(ForeignKeySchema fk)
        {
            var localCols = string.Join(", ", fk.ColumnNames.Select(c => $"`{EscapeIdentifier(c)}`"));
            var refCols = string.Join(", ", fk.ReferencedColumnNames.Select(c => $"`{EscapeIdentifier(c)}`"));

            return $"CONSTRAINT `{EscapeIdentifier(fk.ConstraintName)}` FOREIGN KEY ({localCols}) " +
                   $"REFERENCES `{EscapeIdentifier(fk.ReferencedTableName)}` ({refCols}) " +
                   $"ON DELETE {fk.DeleteRule} ON UPDATE {fk.UpdateRule}";
        }

        private static void RenderCreateTrigger(StringBuilder sb, string tableName, TriggerSchema trg, MySqlRenderOptions options)
        {
            // MySQL needs delimiter changes if trigger body contains semicolons or BEGIN/END blocks.
            // We'll always wrap if WrapTriggersWithDelimiter = true.
            if (options.WrapTriggersWithDelimiter)
            {
                sb.AppendLine($"DELIMITER {options.TriggerDelimiter}");
            }

            sb.Append($"CREATE TRIGGER `{EscapeIdentifier(trg.TriggerName)}` {trg.ActionTiming} {trg.EventManipulation} ON `{EscapeIdentifier(tableName)}`");
            sb.AppendLine();
            sb.AppendLine("FOR EACH ROW");
            sb.AppendLine(trg.ActionStatement.TrimEnd().EndsWith(";")
                ? trg.ActionStatement.TrimEnd()
                : trg.ActionStatement.TrimEnd() + ";");

            if (options.WrapTriggersWithDelimiter)
            {
                sb.AppendLine($"{options.TriggerDelimiter}");
                sb.AppendLine("DELIMITER ;");
            }
            else
            {
                // ensure statement ends with semicolon
                if (!trg.ActionStatement.TrimEnd().EndsWith(";"))
                    sb.AppendLine(";");
            }
        }

        private static string? EscapeIdentifier(string? s)
        {
            // For backtick-quoted identifiers, escape backticks by doubling.
            return s?.Replace("`", "``", StringComparison.Ordinal);
        }
    }
}
