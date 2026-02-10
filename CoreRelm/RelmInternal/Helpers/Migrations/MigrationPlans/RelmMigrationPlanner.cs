using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using CoreRelm.Models.Migrations.MigrationPlans;
using CoreRelm.Models.Migrations.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.SecurityEnums;

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

            EnsureUuidV4Function(actual, migrationOperations);

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
                PlanTriggers(tableName, desiredTable, actualTable, options, migrationOperations, warnings);
                /*
                var desiredTableWithInternalIdTrigger = EnsureInternalIdTriggerDesired(desiredTable); 
                PlanTriggers(tableName, desiredTableWithInternalIdTrigger, actualTable, options, migrationOperations, warnings);
                */
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

            // pull foreign keys that reference newly created tables out from create table definition and moves them to create foreign key operations
            migrationOperations = ReorderForeignKeys(migrationOperations, warnings, blockers);

            // Sort ops in execution order:
            migrationOperations = OrderOperations(migrationOperations);

            return new MigrationPlan(desired.DatabaseName, migrationOperations, warnings, blockers, options.StampUtc);
        }

        private static List<IMigrationOperation> ReorderForeignKeys(
            List<IMigrationOperation> migrationOperations,
            List<string> warnings,
            List<string> blockers)
        {
            var createTableOperations = migrationOperations
                .Where(x => x is CreateTableOperation createTableOperation)
                .Cast<CreateTableOperation>()
                .ToList();

            var createTableNames = createTableOperations.Select(x => x.Table.TableName).ToArray();

            var tableForeignKeys = createTableOperations
                .Where(x => (x.Table?.ForeignKeys?.Count ?? 0) > 0)
                .ToList();

            foreach (var migrationOperation in tableForeignKeys)
            {
                var conflictingForeignKeys = migrationOperation
                    .Table
                    .ForeignKeys
                    .Where(x => createTableNames.Contains(x.Value.ReferencedTableName))
                    .ToList();

                if (conflictingForeignKeys.Count == 0)
                    continue;

                foreach (var conflictingForeignKey in conflictingForeignKeys)
                {
                    if (string.IsNullOrWhiteSpace(conflictingForeignKey.Value.ConstraintName))
                    {
                        warnings.Add($"Table `{migrationOperation.Table.TableName}` has an unnamed foreign key referencing a newly created table `{conflictingForeignKey.Value.ReferencedTableName}`; it will be created in the original position, which may cause issues if the referenced table doesn't exist yet. Consider adding a name to this foreign key for better migration ordering.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(migrationOperation.Table.TableName))
                    {
                        blockers.Add($"Foreign key referencing `{conflictingForeignKey.Value.ReferencedTableName}` cannot be reordered because its table name is missing. Please ensure the table has a valid name.");
                        continue;
                    }

                    var newFk = new AddForeignKeyOperation(migrationOperation.Table.TableName, conflictingForeignKey.Value);
                    
                    migrationOperations.Add(newFk);
                    migrationOperation.Table.ForeignKeys.Remove(conflictingForeignKey.Key);
                }
            }

            return migrationOperations;
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
            foreach (var desiredColumn in desiredTable.Columns.Values.OrderBy(c => c.OrdinalPosition))
            {
                ColumnSchema? actualColumn = null;
                if (!string.IsNullOrWhiteSpace(desiredColumn.ColumnName) && !actualTable.Columns.TryGetValue(desiredColumn.ColumnName, out actualColumn))
                {
                    ops.Add(new AddColumnOperation(tableName, desiredColumn));
                    continue;
                }

                var diffs = ColumnDiff(desiredColumn, actualColumn);
                if (diffs.Count == 0) 
                    continue;

                var safe = IsSafeColumnChange(desiredColumn, actualColumn!);

                if (safe || options.Destructive)
                {
                    ops.Add(new AlterColumnOperation(tableName, desiredColumn, string.Join("; ", diffs)));
                }
                else
                {
                    blockers.Add($"Table `{tableName}` column `{desiredColumn.ColumnName}` differs ({string.Join("; ", diffs)}) and requires --destructive.");
                }
            }

            // NOTE: We do not drop extra columns unless destructive mode + explicit feature later.
            // In destructive mode, dropping columns can be added once you’re ready.
        }

        private static List<string> ColumnDiff(ColumnSchema? desired, ColumnSchema? actual)
        {
            if (desired == null || actual == null)
                return []; // one is null and the other isn't

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
            var nullSafe = actual.IsNullableBool == false && desired.IsNullableBool == true || actual.IsNullable == desired.IsNullable;
            var autoSafe = actual.IsAutoIncrement == desired.IsAutoIncrement; // changing auto_increment is not safe
            return typeSafe && nullSafe && autoSafe;
        }

        private static bool IsSafeTypeChange(string? actualType, string? desiredType)
        {
            var trimmedActual = NormalizeType(actualType);
            var trimmedDesired = NormalizeType(desiredType);

            if (string.Equals(trimmedActual, trimmedDesired, StringComparison.OrdinalIgnoreCase))
                return true;

            // varchar(N) widening only
            if (TryParseVarchar(trimmedActual, out var aLen) && TryParseVarchar(trimmedDesired, out var dLen))
                return dLen >= aLen;

            // allow integer widening (int -> bigint)
            if (string.Equals(trimmedActual, "int", StringComparison.OrdinalIgnoreCase) && string.Equals(trimmedDesired, "bigint", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static string? NormalizeType(string? t)
        {
            return t?.Trim();
        }

        private static bool TryParseVarchar(string? columnType, out int columnLength)
        {
            columnLength = 0;

            if (string.IsNullOrWhiteSpace(columnType))
                return false;

            columnType = columnType.Trim().ToLowerInvariant();

            if (!columnType.StartsWith("varchar(") || !columnType.EndsWith(')')) 
                return false;

            var inner = columnType.Substring("varchar(".Length, columnType.Length - "varchar(".Length - 1);
            return int.TryParse(inner, out columnLength);
        }

        private static void PlanIndexes(
            string tableName,
            TableSchema desiredTable,
            TableSchema actualTable,
            MigrationPlanOptions options,
            List<IMigrationOperation> ops,
            List<string> warnings)
        {
            foreach (var desiredIndex in desiredTable.Indexes.Values.OrderBy(i => i.IndexName, StringComparer.Ordinal))
            {
                IndexSchema? actualIndex = null;
                if (!string.IsNullOrWhiteSpace(desiredIndex.IndexName) && !actualTable.Indexes.TryGetValue(desiredIndex.IndexName, out actualIndex))
                {
                    ops.Add(new CreateIndexOperation(tableName, desiredIndex));
                    continue;
                }

                if (IndexDiffers(desiredIndex, actualIndex))
                {
                    if (string.IsNullOrWhiteSpace(desiredIndex.IndexName))
                    {
                        warnings.Add($"Table `{tableName}` has an unnamed index that differs and cannot be altered; manual intervention required.");
                        continue;
                    }

                    // Meaningful differences: drop+create
                    ops.Add(new DropIndexOperation(tableName, desiredIndex.IndexName));
                    ops.Add(new CreateIndexOperation(tableName, desiredIndex));
                }
            }

            // NOTE: index drops for indexes not in desired are handled in destructive mode in PlanDestructiveDrops.
        }

        private static bool IndexDiffers(IndexSchema? desired, IndexSchema? actual)
        {
            if (desired?.IndexTypeValue != actual?.IndexTypeValue)
                return true;

            // Compare columns in order, including collation/direction
            if (desired?.Columns?.Count != actual?.Columns?.Count)
                return true;

            if (desired == null || actual == null)
                return desired != actual; // one is null and the other isn't

            for (int i = 0; i < (desired.Columns?.Count ?? 0); i++)
            {
                var desiredColumn = desired.Columns![i];
                var actualColumn = actual.Columns!.OrderBy(c => c.SeqInIndex).ElementAt(i);

                if (!string.Equals(desiredColumn.ColumnName, actualColumn.ColumnName, StringComparison.Ordinal))
                    return true;

                // Treat collation (A/D/null) differences as meaningful
                var desiredCollation = desiredColumn.Collation ?? string.Empty;
                var actualCollation = actualColumn.Collation ?? string.Empty;
                if (!string.Equals(desiredCollation, actualCollation, StringComparison.OrdinalIgnoreCase))
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
            foreach (var desiredForeignKey in desiredTable.ForeignKeys.Values.OrderBy(f => f.ConstraintName, StringComparer.Ordinal))
            {
                ForeignKeySchema? actualForeignKey = null;
                if (!string.IsNullOrWhiteSpace(desiredForeignKey.ConstraintName) && !actualTable.ForeignKeys.TryGetValue(desiredForeignKey.ConstraintName, out actualForeignKey))
                {
                    ops.Add(new AddForeignKeyOperation(tableName, desiredForeignKey));
                    continue;
                }

                if (ForeignKeyDiffers(desiredForeignKey, actualForeignKey))
                {
                    if (string.IsNullOrWhiteSpace(desiredForeignKey.ConstraintName))
                    {
                        warnings.Add($"Table `{tableName}` has an unnamed foreign key that differs and cannot be altered; manual intervention required.");
                        continue;
                    }

                    // Drop+add is data-safe; allow even in non-destructive mode
                    ops.Add(new DropForeignKeyOperation(tableName, desiredForeignKey.ConstraintName));
                    ops.Add(new AddForeignKeyOperation(tableName, desiredForeignKey));
                }
            }
        }

        private static bool ForeignKeyDiffers(ForeignKeySchema? desired, ForeignKeySchema? actual)
        {
            if (!string.Equals(desired?.ReferencedTableName, actual?.ReferencedTableName, StringComparison.OrdinalIgnoreCase))
                return true;
            if (desired == null || actual == null)
                return desired != actual; // one is null and the other isn't

            if (desired.ColumnNames == null
                || actual.ColumnNames == null
                || desired.ReferencedColumnNames == null
                || actual.ReferencedColumnNames == null
                || desired.ColumnNames.Count != actual.ColumnNames.Count 
                || desired.ReferencedColumnNames.Count != actual.ReferencedColumnNames.Count)
                return true;

            for (int i = 0; i < desired.ColumnNames?.Count; i++)
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
            foreach (var desiredTrigger in desiredTable.Triggers.Values.OrderBy(t => t.TriggerName, StringComparer.Ordinal))
            {
                TriggerSchema? actualTrigger = null;
                if (!string.IsNullOrWhiteSpace(desiredTrigger.TriggerName) && !actualTable.Triggers.TryGetValue(desiredTrigger.TriggerName, out actualTrigger))
                {
                    ops.Add(new CreateTriggerOperation(tableName, desiredTrigger));
                    continue;
                }

                if (TriggerDiffers(desiredTrigger, actualTrigger))
                {
                    if (string.IsNullOrWhiteSpace(desiredTrigger.TriggerName))
                    {
                        warnings.Add($"Table `{tableName}` has an unnamed trigger that differs and cannot be altered; manual intervention required.");
                        continue;
                    }

                    // Drop+create is data-safe
                    ops.Add(new DropTriggerOperation(tableName, desiredTrigger.TriggerName));
                    ops.Add(new CreateTriggerOperation(tableName, desiredTrigger));
                }
            }
        }

        private static bool TriggerDiffers(TriggerSchema? desired, TriggerSchema? actual)
        {
            if (desired?.EventManipulation != actual?.EventManipulation)
                return true;

            if (desired?.ActionTiming != actual?.ActionTiming)
                return true;

            if (desired == null || actual == null)
                return desired != actual; // one is null and the other isn't

            // Normalize whitespace for comparison
            var dStmt = NormalizeSql(desired!.ActionStatement);
            var aStmt = NormalizeSql(actual!.ActionStatement);
            return !string.Equals(dStmt, aStmt, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeSql(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            return string.Join(" ", s.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries));
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
            return [.. ops
                .OrderBy(RankOperations)
                .ThenBy(o => o.Description, StringComparer.Ordinal)];
        }

        private static void EnsureUuidV4Function(SchemaSnapshot actual, List<IMigrationOperation> migrationOperations)
        {
            // If function already exists, do nothing.
            if (actual.Functions?.ContainsKey("uuid_v4") ?? false)
                return;

            // Create function SQL (UUIDv4 using RANDOM_BYTES)
            // Uses RANDOM_BYTES for v4 randomness; returns CHAR(36) lower-case.
            // MySQL requires delimiters when creating routines.
            var newFunction = new FunctionSchema
            {
                RoutineName = "uuid_v4",
                DtdIdentifier = "CHAR(45)",
                IsDeterministic = "NO",
                SqlDataAccess = "NO SQL",
                SecurityType = SqlSecurityLevel.INVOKER,
                RoutineDefinition = @"RETURN LOWER(CONCAT(
                    HEX(RANDOM_BYTES(4)), '-',
                    HEX(RANDOM_BYTES(2)), '-',
                    '4', SUBSTR(HEX(RANDOM_BYTES(2)), 2, 3), '-',
                    CONCAT(HEX(FLOOR(ASCII(RANDOM_BYTES(1)) / 64) + 8), SUBSTR(HEX(RANDOM_BYTES(2)), 2, 3)), '-',
                    HEX(RANDOM_BYTES(6))
                ));",
            };

            migrationOperations.Add(new CreateFunctionOperation("uuid_v4", newFunction));
        }
    }
}