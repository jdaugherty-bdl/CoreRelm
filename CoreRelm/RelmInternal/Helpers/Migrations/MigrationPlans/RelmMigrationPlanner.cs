using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using CoreRelm.Models.Migrations.MigrationPlans;
using CoreRelm.Models.Migrations.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Migrations.MigrationPlans
{
    public sealed class RelmMigrationPlanner : IRelmMigrationPlanner
    {
        public MigrationPlan Plan(SchemaSnapshot desired, SchemaSnapshot actual, MigrationPlanOptions options)
        {
            if (!string.Equals(desired.DatabaseName, actual.DatabaseName, StringComparison.Ordinal))
                throw new InvalidOperationException($"Desired database '{desired.DatabaseName}' does not match actual database '{actual.DatabaseName}'.");

            var migrationOperations = new List<IMigrationOperation>();
            var warnings = new List<string>();
            var blockers = new List<string>();

            EnsureUuidV4Function(desired.DatabaseName, actual, migrationOperations);

            // Only operate on scope tables (selected model set tables) for this DB
            var scope = new HashSet<string>(options.ScopeTables, StringComparer.Ordinal);

            // Deterministic table order
            var desiredTables = desired.Tables.Keys
                .Where(t => scope.Contains(t))
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToList();

            foreach (var tableName in desiredTables)
            {
                var desiredTable = desired.Tables[tableName];

                if (!actual.Tables.TryGetValue(tableName, out var actualTable))
                {
                    migrationOperations.Add(new CreateTableOperation(desiredTable));
                    continue;
                }

                // Columns: add missing, alter changed (safe unless destructive)
                PlanColumns(tableName, desiredTable, actualTable, options, migrationOperations, warnings, blockers);

                // Indexes: differences in unique/order/collation are meaningful => drop+create
                PlanIndexes(tableName, desiredTable, actualTable, options, migrationOperations, warnings);

                // Foreign keys: compare by constraint name; if missing => add; if differs => drop+add
                PlanForeignKeys(tableName, desiredTable, actualTable, options, migrationOperations, warnings);

                // Triggers: compare by name + statement; if missing => create; if differs => drop+create
                //PlanTriggers(tableName, desiredTable, actualTable, options, migrationOperations, warnings);
                var desiredTableWithInternalIdTrigger = EnsureInternalIdTriggerDesired(desiredTable); 
                PlanTriggers(tableName, desiredTableWithInternalIdTrigger, actualTable, options, migrationOperations, warnings);
            }

            // Destructive cleanup scoped to desired tables only:
            // Drop indexes/fks/triggers that exist in actual but not desired (only if options.Destructive)
            if (options.Destructive)
            {
                foreach (var tableName in desiredTables)
                {
                    var desiredTable = desired.Tables[tableName];
                    if (!actual.Tables.TryGetValue(tableName, out var actualTable)) continue;

                    PlanDestructiveDrops(tableName, desiredTable, actualTable, migrationOperations, warnings);
                }
            }

            // Sort ops in execution order:
            migrationOperations = OrderOperations(migrationOperations);

            return new MigrationPlan(desired.DatabaseName, migrationOperations, warnings, blockers);
        }

        private static void PlanColumns(
            string tableName,
            TableSchema desiredTable,
            TableSchema actualTable,
            MigrationPlanOptions options,
            List<IMigrationOperation> ops,
            List<string> warnings,
            List<string> blockers)
        {
            foreach (var desiredCol in desiredTable.Columns.Values.OrderBy(c => c.OrdinalPosition))
            {
                if (!actualTable.Columns.TryGetValue(desiredCol.ColumnName, out var actualCol))
                {
                    ops.Add(new AddColumnOperation(tableName, desiredCol));
                    continue;
                }

                var diffs = ColumnDiff(desiredCol, actualCol);
                if (diffs.Count == 0) continue;

                var safe = IsSafeColumnChange(desiredCol, actualCol);

                if (safe || options.Destructive)
                {
                    ops.Add(new AlterColumnOperation(tableName, desiredCol, string.Join("; ", diffs)));
                }
                else
                {
                    blockers.Add($"Table `{tableName}` column `{desiredCol.ColumnName}` differs ({string.Join("; ", diffs)}) and requires --destructive.");
                }
            }

            // NOTE: We do not drop extra columns unless destructive mode + explicit feature later.
            // In destructive mode, dropping columns can be added once you’re ready.
        }

        private static List<string> ColumnDiff(ColumnSchema desired, ColumnSchema actual)
        {
            var diffs = new List<string>();

            if (!string.Equals(NormalizeType(desired.ColumnType), NormalizeType(actual.ColumnType), StringComparison.OrdinalIgnoreCase))
                diffs.Add($"type {actual.ColumnType} -> {desired.ColumnType}");

            if (desired.IsNullable != actual.IsNullable)
                diffs.Add($"nullable {actual.IsNullable} -> {desired.IsNullable}");

            // DefaultValue: INFORMATION_SCHEMA gives raw default; desired may be SQL expression.
            // We compare string-wise; you can normalize later.
            var aDef = actual.DefaultValue;
            var dDef = desired.DefaultValue;
            if (!string.Equals(aDef ?? "", dDef ?? "", StringComparison.OrdinalIgnoreCase))
                diffs.Add($"default '{aDef}' -> '{dDef}'");

            // Auto increment
            if (desired.IsAutoIncrement != actual.IsAutoIncrement)
                diffs.Add($"auto_increment {actual.IsAutoIncrement} -> {desired.IsAutoIncrement}");

            return diffs;
        }

        private static bool IsSafeColumnChange(ColumnSchema desired, ColumnSchema actual)
        {
            // Safe change rules (non-destructive):
            // - widen varchar
            // - not-null -> nullable
            // - add/change default (usually safe; you may tighten later)
            // - enabling ON UPDATE semantics for last_updated (safe)
            // Anything else -> destructive required
            var typeSafe = IsSafeTypeChange(actual.ColumnType, desired.ColumnType);
            var nullSafe = actual.IsNullable == false && desired.IsNullable == true || actual.IsNullable == desired.IsNullable;
            var autoSafe = actual.IsAutoIncrement == desired.IsAutoIncrement; // changing auto_increment is not safe
            return typeSafe && nullSafe && autoSafe;
        }

        private static bool IsSafeTypeChange(string actualType, string desiredType)
        {
            var a = NormalizeType(actualType);
            var d = NormalizeType(desiredType);

            if (string.Equals(a, d, StringComparison.OrdinalIgnoreCase))
                return true;

            // varchar(N) widening only
            if (TryParseVarchar(a, out var aLen) && TryParseVarchar(d, out var dLen))
                return dLen >= aLen;

            // allow integer widening (int -> bigint)
            if (string.Equals(a, "int", StringComparison.OrdinalIgnoreCase) && string.Equals(d, "bigint", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static string NormalizeType(string t)
        {
            return t.Trim();
        }

        private static bool TryParseVarchar(string t, out int len)
        {
            len = 0;
            t = t.Trim().ToLowerInvariant();
            if (!t.StartsWith("varchar(") || !t.EndsWith(")")) return false;
            var inner = t.Substring("varchar(".Length, t.Length - "varchar(".Length - 1);
            return int.TryParse(inner, out len);
        }

        private static void PlanIndexes(
            string tableName,
            TableSchema desiredTable,
            TableSchema actualTable,
            MigrationPlanOptions options,
            List<IMigrationOperation> ops,
            List<string> warnings)
        {
            foreach (var desiredIdx in desiredTable.Indexes.Values.OrderBy(i => i.IndexName, StringComparer.Ordinal))
            {
                if (!actualTable.Indexes.TryGetValue(desiredIdx.IndexName, out var actualIdx))
                {
                    ops.Add(new CreateIndexOperation(tableName, desiredIdx));
                    continue;
                }

                if (IndexDiffers(desiredIdx, actualIdx))
                {
                    // Meaningful differences: drop+create
                    ops.Add(new DropIndexOperation(tableName, desiredIdx.IndexName));
                    ops.Add(new CreateIndexOperation(tableName, desiredIdx));
                }
            }

            // NOTE: index drops for indexes not in desired are handled in destructive mode in PlanDestructiveDrops.
        }

        private static bool IndexDiffers(IndexSchema desiredIdx, IndexSchema actualIdx)
        {
            if (desiredIdx.IsUnique != actualIdx.IsUnique)
                return true;

            // Compare columns in order, including collation/direction
            if (desiredIdx.Columns.Count != actualIdx.Columns.Count)
                return true;

            for (int i = 0; i < desiredIdx.Columns.Count; i++)
            {
                var d = desiredIdx.Columns[i];
                var a = actualIdx.Columns.OrderBy(c => c.SeqInIndex).ElementAt(i);

                if (!string.Equals(d.ColumnName, a.ColumnName, StringComparison.Ordinal))
                    return true;

                // Treat collation (A/D/null) differences as meaningful
                var dCol = d.Collation ?? "";
                var aCol = a.Collation ?? "";
                if (!string.Equals(dCol, aCol, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void PlanForeignKeys(
            string tableName,
            TableSchema desiredTable,
            TableSchema actualTable,
            MigrationPlanOptions options,
            List<IMigrationOperation> ops,
            List<string> warnings)
        {
            foreach (var desiredFk in desiredTable.ForeignKeys.Values.OrderBy(f => f.ConstraintName, StringComparer.Ordinal))
            {
                if (!actualTable.ForeignKeys.TryGetValue(desiredFk.ConstraintName, out var actualFk))
                {
                    ops.Add(new AddForeignKeyOperation(tableName, desiredFk));
                    continue;
                }

                if (ForeignKeyDiffers(desiredFk, actualFk))
                {
                    // Drop+add is data-safe; allow even in non-destructive mode
                    ops.Add(new DropForeignKeyOperation(tableName, desiredFk.ConstraintName));
                    ops.Add(new AddForeignKeyOperation(tableName, desiredFk));
                }
            }
        }

        private static bool ForeignKeyDiffers(ForeignKeySchema desired, ForeignKeySchema actual)
        {
            if (!string.Equals(desired.ReferencedTableName, actual.ReferencedTableName, StringComparison.Ordinal))
                return true;

            if (desired.ColumnNames.Count != actual.ColumnNames.Count || desired.ReferencedColumnNames.Count != actual.ReferencedColumnNames.Count)
                return true;

            for (int i = 0; i < desired.ColumnNames.Count; i++)
            {
                if (!string.Equals(desired.ColumnNames[i], actual.ColumnNames[i], StringComparison.Ordinal))
                    return true;
            }

            for (int i = 0; i < desired.ReferencedColumnNames.Count; i++)
            {
                if (!string.Equals(desired.ReferencedColumnNames[i], actual.ReferencedColumnNames[i], StringComparison.Ordinal))
                    return true;
            }

            if (!string.Equals(desired.DeleteRule, actual.DeleteRule, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.Equals(desired.UpdateRule, actual.UpdateRule, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static void PlanTriggers(
            string tableName,
            TableSchema desiredTable,
            TableSchema actualTable,
            MigrationPlanOptions options,
            List<IMigrationOperation> ops,
            List<string> warnings)
        {
            foreach (var desiredTrg in desiredTable.Triggers.Values.OrderBy(t => t.TriggerName, StringComparer.Ordinal))
            {
                if (!actualTable.Triggers.TryGetValue(desiredTrg.TriggerName, out var actualTrg))
                {
                    ops.Add(new CreateTriggerOperation(tableName, desiredTrg));
                    continue;
                }

                if (TriggerDiffers(desiredTrg, actualTrg))
                {
                    // Drop+create is data-safe
                    ops.Add(new DropTriggerOperation(tableName, desiredTrg.TriggerName));
                    ops.Add(new CreateTriggerOperation(tableName, desiredTrg));
                }
            }
        }

        private static bool TriggerDiffers(TriggerSchema desired, TriggerSchema actual)
        {
            if (!string.Equals(desired.EventManipulation, actual.EventManipulation, StringComparison.OrdinalIgnoreCase))
                return true;
            if (!string.Equals(desired.ActionTiming, actual.ActionTiming, StringComparison.OrdinalIgnoreCase))
                return true;

            // Normalize whitespace for comparison
            var dStmt = NormalizeSql(desired.ActionStatement);
            var aStmt = NormalizeSql(actual.ActionStatement);
            return !string.Equals(dStmt, aStmt, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeSql(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            return string.Join(" ", s.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private static void PlanDestructiveDrops(
            string tableName,
            TableSchema desiredTable,
            TableSchema actualTable,
            List<IMigrationOperation> ops,
            List<string> warnings)
        {
            // Drop indexes present in actual but not desired (excluding PRIMARY)
            foreach (var actualIdxName in actualTable.Indexes.Keys.OrderBy(x => x, StringComparer.Ordinal))
            {
                if (string.Equals(actualIdxName, "PRIMARY", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!desiredTable.Indexes.ContainsKey(actualIdxName))
                    ops.Add(new DropIndexOperation(tableName, actualIdxName));
            }

            // Drop foreign keys present in actual but not desired
            foreach (var actualFkName in actualTable.ForeignKeys.Keys.OrderBy(x => x, StringComparer.Ordinal))
            {
                if (!desiredTable.ForeignKeys.ContainsKey(actualFkName))
                    ops.Add(new DropForeignKeyOperation(tableName, actualFkName));
            }

            // Drop triggers present in actual but not desired
            foreach (var actualTrgName in actualTable.Triggers.Keys.OrderBy(x => x, StringComparer.Ordinal))
            {
                if (!desiredTable.Triggers.ContainsKey(actualTrgName))
                    ops.Add(new DropTriggerOperation(tableName, actualTrgName));
            }
        }

        private static int RankOperations(IMigrationOperation op) => op switch
        {
            CreateFunctionOperation => 5,
            CreateTableOperation => 10,
            AddColumnOperation => 20,
            AlterColumnOperation => 30,
            DropIndexOperation => 40,
            CreateIndexOperation => 50,
            DropForeignKeyOperation => 60,
            AddForeignKeyOperation => 70,
            DropTriggerOperation => 80,
            CreateTriggerOperation => 90,
            _ => 999
        };

        private static List<IMigrationOperation> OrderOperations(List<IMigrationOperation> ops)
        {
            // Enforce execution order:
            // 1. CreateTable
            // 2. Add/Alter columns
            // 3. DropIndex then CreateIndex
            // 4. DropFK then AddFK
            // 5. DropTrigger then CreateTrigger
            return ops
                .OrderBy(RankOperations)
                .ThenBy(o => o.Description, StringComparer.Ordinal)
                .ToList();
        }

        private static void EnsureUuidV4Function(string dbName, SchemaSnapshot actual, List<IMigrationOperation> migrationOperations)
        {
            // If function already exists, do nothing.
            if (actual.Functions.ContainsKey("uuid_v4"))
                return;

            // Create function SQL (UUIDv4 using RANDOM_BYTES)
            var createSql = MySqlUuidV4FunctionSql();

            migrationOperations.Add(new CreateFunctionOperation("uuid_v4", createSql));
        }

        private static string MySqlUuidV4FunctionSql()
        {
            // Uses RANDOM_BYTES for v4 randomness; returns CHAR(36) lower-case.
            // MySQL requires delimiters when creating routines.
            return @"DELIMITER $$
                CREATE FUNCTION uuid_v4()
                RETURNS CHAR(36)
                NOT DETERMINISTIC
                NO SQL
                SQL SECURITY INVOKER
                RETURN LOWER(CONCAT(
                  HEX(RANDOM_BYTES(4)), '-',
                  HEX(RANDOM_BYTES(2)), '-',
                  '4', SUBSTR(HEX(RANDOM_BYTES(2)), 2, 3), '-',
                  CONCAT(HEX(FLOOR(ASCII(RANDOM_BYTES(1)) / 64) + 8), SUBSTR(HEX(RANDOM_BYTES(2)), 2, 3)), '-',
                  HEX(RANDOM_BYTES(6))
                ));
                $$
                DELIMITER ;
                ".Trim();
        }

        private static TableSchema EnsureInternalIdTriggerDesired(TableSchema table)
        {
            // Deterministic trigger name convention
            var triggerName = $"trg_{table.TableName}_InternalId_bi";

            // If caller already declared it explicitly, keep it as-is
            if (table.Triggers.ContainsKey(triggerName))
                return table;

            // Ensure the table has InternalId column (your base schema invariant)
            if (!table.Columns.ContainsKey("InternalId"))
                return table; // or throw; but returning is safer if you later support non-RelmModel tables

            var trigger = new TriggerSchema(
                TriggerName: triggerName,
                EventManipulation: "INSERT",
                ActionTiming: "BEFORE",
                ActionStatement: "SET NEW.InternalId = IFNULL(NEW.InternalId, uuid_v4())"
            );

            var newTriggers = table.Triggers
                .ToDictionary(k => k.Key, v => v.Value, StringComparer.Ordinal);

            newTriggers[triggerName] = trigger;

            return table with { Triggers = newTriggers };
        }
    }
}