using BDL.Common.Logging.Extensions;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using CoreRelm.Models.Migrations.MigrationPlans;
using CoreRelm.Models.Migrations.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.MigrationEnums;
using static CoreRelm.Enums.SecurityEnums;
using static CoreRelm.Enums.StoredProcedures;

namespace CoreRelm.RelmInternal.Helpers.Migrations.MigrationPlans
{
    internal sealed class RelmMigrationPlanner(ILogger<RelmMigrationPlanner>? log = null) : IRelmMigrationPlanner
    {
        public MigrationPlan Plan(SchemaSnapshot desired, SchemaSnapshot actual, MigrationPlanOptions options)
        {
            log?.LogFormatted(LogLevel.Information, "Creating migration plan", args: [], preIncreaseLevel: true);

            if (string.IsNullOrWhiteSpace(desired.DatabaseName))
                throw new ArgumentException("Desired schema snapshot must have a database name.", nameof(desired.DatabaseName));

            if (string.IsNullOrWhiteSpace(actual.DatabaseName))
                throw new ArgumentException("Actual schema snapshot must have a database name.", nameof(actual.DatabaseName));

            if (!string.Equals(desired.DatabaseName, actual.DatabaseName, StringComparison.Ordinal))
                throw new InvalidOperationException($"Desired database '{desired.DatabaseName}' does not match actual database '{actual.DatabaseName}'.");

            log?.LogFormatted(LogLevel.Information, "Planning migration for database '{DatabaseName}' with {DesiredTableCount} desired tables and {ActualTableCount} actual tables", args: [desired.DatabaseName, desired.Tables.Count, actual.Tables.Count], preIncreaseLevel: true);

            var migrationOperations = new List<IMigrationOperation>();
            var warnings = new List<string>();
            var blockers = new List<string>();

            // Only operate on scope tables (selected model set tables) for this DB
            var scope = new HashSet<string>(options.ScopeTables, StringComparer.Ordinal);
            log?.LogFormatted(LogLevel.Information, "Migration scope includes {ScopeTableCount} tables: {ScopeTables}", args: [scope.Count, string.Join(", ", scope)]);

            // Deterministic table order
            var desiredTables = desired.Tables.Keys
                .Intersect(scope)
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToList();
            log?.LogFormatted(LogLevel.Information, "Inspecting each table in order to generate plan", args: []);

            log?.SaveIndentLevel("tables");
            foreach (var tableName in desiredTables)
            {
                log?.RestoreIndentLevel("tables");

                log?.LogFormatted(LogLevel.Information, "Planning migration for table '{TableName}'", args: [tableName], preIncreaseLevel: true);
                
                var desiredTable = desired.Tables[tableName];
                log?.LogFormatted(LogLevel.Information, "Found desired table in tables list", args: [desiredTable.TableName], preIncreaseLevel: true);

                if (!actual.Tables.TryGetValue(desiredTable.TableName, out var actualTable))
                {
                    log?.LogFormatted(LogLevel.Information, "Table '{TableName}' does not exist in actual schema; planning CreateTable", args: [desiredTable.TableName], singleIndentLine: true);
                    migrationOperations.Add(new CreateTableOperation(desiredTable));
                    continue;
                }
                else
                    log?.LogFormatted(LogLevel.Information, "Table '{TableName}' exists in actual schema; planning column/index/fk/trigger operations", args: [desiredTable.TableName]);

                // Columns: add missing, alter changed (safe unless destructive)
                var columnPlan = PlanColumns(desiredTable, actualTable, options);
                migrationOperations.AddRange(columnPlan.MigrationOperations);
                warnings.AddRange(columnPlan.Warnings);
                blockers.AddRange(columnPlan.Blockers);

                // Indexes: differences in unique/order/collation are meaningful => drop+create
                var indexPlan = PlanIndexes(desiredTable, actualTable, options);
                migrationOperations.AddRange(indexPlan.MigrationOperations);
                warnings.AddRange(indexPlan.Warnings);

                // Foreign keys: compare by constraint name; if missing => add; if differs => drop+add
                var foreignKeyPlan = PlanForeignKeys(desiredTable, actualTable, options);
                migrationOperations.AddRange(foreignKeyPlan.MigrationOperations);
                warnings.AddRange(foreignKeyPlan.Warnings);

                // Triggers: compare by name + statement; if missing => create; if differs => drop+create
                var triggerPlan = PlanTriggers(desiredTable, actualTable, options);
                migrationOperations.AddRange(triggerPlan.MigrationOperations);
                warnings.AddRange(triggerPlan.Warnings);
            }
            log?.RestoreIndentLevel("tables");

            log?.LogFormatted(LogLevel.Information, "Planned column/index/fk/trigger operations for all tables in scope. Total operations so far: {OperationCount}", args: [migrationOperations.Count]);

            var functionPlan = PlanFunctions(desired, actual, options);
            migrationOperations.AddRange(functionPlan.MigrationOperations);
            warnings.AddRange(functionPlan.Warnings);

            // Destructive cleanup scoped to desired tables only:
            // Drop indexes/fks/triggers that exist in actual but not desired (only if options.Destructive)
            if (options.Destructive)
            {
                log?.LogFormatted(LogLevel.Information, "Destructive mode is enabled; planning destructive drops for indexes/foreign keys/triggers that exist in actual but not desired", args: []);

                log?.SaveIndentLevel("destructive_drops");
                foreach (var tableName in desiredTables)
                {
                    log?.RestoreIndentLevel("destructive_drops");

                    log?.LogFormatted(LogLevel.Information, "Planning destructive drops for table '{TableName}'", args: [tableName], preIncreaseLevel: true);

                    var desiredTable = desired.Tables[tableName];
                    if (!actual.Tables.TryGetValue(tableName, out var actualTable))
                    {
                        log?.LogFormatted(LogLevel.Information, "Table '{TableName}' does not exist in actual schema; skipping destructive drops", args: [tableName]);
                        continue;
                    }

                    PlanDestructiveDrops(tableName, desiredTable, actualTable, migrationOperations, warnings);
                }
                log?.RestoreIndentLevel("destructive_drops");

                log?.LogFormatted(LogLevel.Information, "Finished planning destructive drops. Total operations so far: {OperationCount}", args: [migrationOperations.Count]);
            }

            // pull foreign keys that reference newly created tables out from create table definition and moves them to create foreign key operations
            migrationOperations = ReorderForeignKeys(migrationOperations, warnings, blockers);

            // Sort ops in execution order:
            log?.LogFormatted(LogLevel.Information, "Sorting operations in execution order", args: []);
            migrationOperations = OrderOperations(migrationOperations);

            log?.LogFormatted(LogLevel.Information, "Finished creating migration plan with {OperationCount} operations, {WarningCount} warnings, and {BlockerCount} blockers", args: [migrationOperations.Count, warnings.Count, blockers.Count], preDecreaseLevel: true);
            return new MigrationPlan(
                desired.DatabaseName,
                options.MigrationName,
                options.MigrationFileName,
                options.ModelSetName,
                RelmMigrationType.Migration,
                migrationOperations, 
                warnings, 
                blockers, 
                options.StampUtc);
        }

        private List<IMigrationOperation> ReorderForeignKeys(
            List<IMigrationOperation> migrationOperations,
            List<string> warnings,
            List<string> blockers)
        {
            log?.SaveIndentLevel("ReorderForeignKeys");

            log?.LogFormatted(LogLevel.Information, "Reordering foreign keys that reference newly created tables to ensure proper execution order. Total operations now: {OperationCount}", args: [migrationOperations.Count]);

            log?.LogFormatted(LogLevel.Information, "Identifying CreateTable operations in the migration plan", args: [], preIncreaseLevel: true);
            var createTableOperations = migrationOperations
                .Where(x => x is CreateTableOperation createTableOperation)
                .Cast<CreateTableOperation>()
                .ToList();

            if (createTableOperations.Count == 0)
            {
                log?.LogFormatted(LogLevel.Information, "No CreateTable operations found; no reordering needed", args: [], singleIndentLine: true);
                log?.RestoreIndentLevel("ReorderForeignKeys");
                return migrationOperations;
            }
            else
                log?.LogFormatted(LogLevel.Information, "Found {CreateTableCount} CreateTable operations", args: [createTableOperations.Count], preIncreaseLevel: true);

            var createTableNames = createTableOperations.Select(x => x.Table.TableName).ToArray();
            log?.LogFormatted(LogLevel.Information, "Newly created tables: {CreateTableNames}", args: [string.Join(", ", createTableNames)]);

            var tableForeignKeys = createTableOperations
                .Where(x => (x.Table?.ForeignKeys?.Count ?? 0) > 0)
                .ToList();
            log?.LogFormatted(LogLevel.Information, "Found {ForeignKeyCount} CreateTable operations that have foreign keys", args: [tableForeignKeys.Count]);

            log?.LogFormatted(LogLevel.Information, "Inspecting foreign keys in CreateTable operations to identify those referencing newly created tables", args: []);

            log?.SaveIndentLevel("foreign_keys");
            foreach (var migrationOperation in tableForeignKeys)
            {
                log?.RestoreIndentLevel("foreign_keys");

                log?.LogFormatted(LogLevel.Information, "Inspecting table '{TableName}' for foreign keys referencing newly created tables", args: [migrationOperation.Table.TableName], preIncreaseLevel: true);

                var conflictingForeignKeys = migrationOperation
                    .Table
                    .ForeignKeys
                    .Where(x => createTableNames.Contains(x.Value.ReferencedTableName))
                    .ToList();

                if (conflictingForeignKeys.Count == 0)
                {
                    log?.LogFormatted(LogLevel.Information, "No foreign keys in table '{TableName}' reference newly created tables; no reordering needed", args: [migrationOperation.Table.TableName]);
                    continue;
                }
                else
                    log?.LogFormatted(LogLevel.Information, "Table '{TableName}' has {ConflictingForeignKeyCount} foreign keys that reference newly created tables and will be reordered", args: [migrationOperation.Table.TableName, conflictingForeignKeys.Count]);

                log?.LogFormatted(LogLevel.Information, "Reordering foreign keys for table '{TableName}'", args: [migrationOperation.Table.TableName], preIncreaseLevel: true);

                log?.SaveIndentLevel("conflicting_foreign_keys");
                foreach (var conflictingForeignKey in conflictingForeignKeys)
                {
                    log?.RestoreIndentLevel("conflicting_foreign_keys");

                    log?.LogFormatted(LogLevel.Information, "Processing foreign key '{ForeignKeyName}' in table '{TableName}' that references newly created table '{ReferencedTableName}'", args: [conflictingForeignKey.Value.ConstraintName, migrationOperation.Table.TableName, conflictingForeignKey.Value.ReferencedTableName], preIncreaseLevel: true);

                    if (string.IsNullOrWhiteSpace(conflictingForeignKey.Value.ConstraintName))
                    {
                        log?.LogFormatted(LogLevel.Warning, "Foreign key in table '{TableName}' referencing newly created table '{ReferencedTableName}' does not have a constraint name; it will be created in the original position, which may cause issues if the referenced table doesn't exist yet. Consider adding a name to this foreign key for better migration ordering.", args: [migrationOperation.Table.TableName, conflictingForeignKey.Value.ReferencedTableName]);
                        warnings.Add($"Table `{migrationOperation.Table.TableName}` has an unnamed foreign key referencing a newly created table `{conflictingForeignKey.Value.ReferencedTableName}`; it will be created in the original position, which may cause issues if the referenced table doesn't exist yet. Consider adding a name to this foreign key for better migration ordering.");
                        continue;
                    }
                    else
                        log?.LogFormatted(LogLevel.Information, "Foreign key has a constraint name and will be reordered to ensure the referenced table '{ReferencedTableName}' is created first", args: [conflictingForeignKey.Value.ReferencedTableName], preIncreaseLevel: true);

                    if (string.IsNullOrWhiteSpace(migrationOperation.Table.TableName))
                    {
                        log?.LogFormatted(LogLevel.Warning, "Table name for foreign key is missing; cannot reorder this foreign key. Please ensure the table has a valid name.", args: []);
                        blockers.Add($"Foreign key referencing `{conflictingForeignKey.Value.ReferencedTableName}` cannot be reordered because its table name is missing. Please ensure the table has a valid name.");
                        continue;
                    }
                    else
                        log?.LogFormatted(LogLevel.Information, "Table name for foreign key is '{TableName}'; proceeding with reordering", args: [migrationOperation.Table.TableName]);

                    log?.LogFormatted(LogLevel.Information, "Adding AddForeignKey operation to the end of the migration operations list", args: []);
                    var newFk = new AddForeignKeyOperation(migrationOperation.Table.TableName, conflictingForeignKey.Value);
                    migrationOperations.Add(newFk);

                    log?.LogFormatted(LogLevel.Information, "Removing original foreign key from CreateTable operation for table '{TableName}'", args: [migrationOperation.Table.TableName], postDecreaseLevel: true);
                    migrationOperation.Table.ForeignKeys.Remove(conflictingForeignKey.Key);

                    log?.LogFormatted(LogLevel.Information, "Finished processing foreign key '{ForeignKeyName}' for table '{TableName}'", args: [conflictingForeignKey.Value.ConstraintName, migrationOperation.Table.TableName]);
                }
                log?.RestoreIndentLevel("conflicting_foreign_keys");

                log?.LogFormatted(LogLevel.Information, "Finished reordering foreign keys for table '{TableName}'", args: [migrationOperation.Table.TableName]);
            }
            log?.RestoreIndentLevel("foreign_keys");

            log?.LogFormatted(LogLevel.Information, "Finished reordering foreign keys. Total operations now: {OperationCount}", args: [migrationOperations.Count]);

            log?.RestoreIndentLevel("ReorderForeignKeys");

            return migrationOperations;
        }

        private (List<IMigrationOperation> MigrationOperations, List<string> Warnings, List<string> Blockers) PlanColumns(
            TableSchema desiredTable,
            TableSchema actualTable,
            MigrationPlanOptions options)
        {
            var migrationOperations = new List<IMigrationOperation>();
            var warnings = new List<string>();
            var blockers = new List<string>();

            log?.SaveIndentLevel("PlanColumns");

            // NOTE: We do not drop extra columns unless destructive mode + explicit feature later.
            // In destructive mode, dropping columns can be added once you’re ready.
            log?.LogFormatted(LogLevel.Information, "Planning column operations for table '{TableName}'", args: [desiredTable.TableName], preIncreaseLevel: true);

            log?.SaveIndentLevel("columns");
            foreach (var desiredColumn in desiredTable.Columns.Values.OrderBy(c => c.OrdinalPosition))
            {
                log?.RestoreIndentLevel("columns");

                log?.LogFormatted(LogLevel.Information, "Planning column '{ColumnName}'", args: [desiredColumn.ColumnName], preIncreaseLevel: true);

                ColumnSchema? actualColumn = null;
                if (!string.IsNullOrWhiteSpace(desiredColumn.ColumnName) && !actualTable.Columns.TryGetValue(desiredColumn.ColumnName, out actualColumn))
                {
                    log?.LogFormatted(LogLevel.Information, "Column '{ColumnName}' does not exist in actual schema; planning AddColumn", args: [desiredColumn.ColumnName], singleIndentLine: true);
                    migrationOperations.Add(new AddColumnOperation(desiredTable.TableName, desiredColumn));
                    continue;
                }
                else                    
                    log?.LogFormatted(LogLevel.Information, "Column '{ColumnName}' exists in actual schema; comparing for differences", args: [desiredColumn.ColumnName], preIncreaseLevel: true);

                var diffs = ColumnDiff(desiredColumn, actualColumn);
                if (diffs.Count == 0)
                {
                    log?.LogFormatted(LogLevel.Information, "Column '{ColumnName}' has no differences, no changes will be planned", args: [desiredColumn.ColumnName]);
                    continue;
                }

                log?.LogFormatted(LogLevel.Information, "Column '{ColumnName}' has {DiffCount} differences: {Diffs}", args: [desiredColumn.ColumnName, diffs.Count, string.Join("; ", diffs)]);

                var safe = IsSafeColumnChange(desiredColumn, actualColumn!);
                log?.LogFormatted(LogLevel.Information, "Column '{ColumnName}' change is considered {Safety}. Safe: {IsSafe}", args: [desiredColumn.ColumnName, safe ? "safe" : "destructive", safe]);

                if (safe || options.Destructive)
                {
                    if (options.Destructive)
                        log?.LogFormatted(LogLevel.Information, "Destructive changes are turned on", args: []);

                    log?.LogFormatted(LogLevel.Information, "Planning AlterColumn for column '{ColumnName}' due to differences", args: [desiredColumn.ColumnName]);
                    migrationOperations.Add(new AlterColumnOperation(desiredTable.TableName, desiredColumn, string.Join("; ", diffs)));
                }
                else
                {
                    log?.LogFormatted(LogLevel.Warning, "Column '{ColumnName}' has differences that require destructive changes but destructive mode is not enabled. Differences: {Diffs}", args: [desiredColumn.ColumnName, string.Join("; ", diffs)]);
                    blockers.Add($"Table `{desiredTable.TableName}` column `{desiredColumn.ColumnName}` differs ({string.Join("; ", diffs)}) and requires --destructive.");
                }

                log?.LogFormatted(LogLevel.Information, "Finished planning column '{ColumnName}'", args: [desiredColumn.ColumnName]);
            }
            log?.RestoreIndentLevel("columns");

            log?.LogFormatted(LogLevel.Information, "Finished planning columns for table '{TableName}', total operations planned: {OperationCount}", args: [desiredTable.TableName, migrationOperations.Count]);

            log?.RestoreIndentLevel("PlanColumns");
            
            return (migrationOperations, warnings, blockers);
        }

        private List<string> ColumnDiff(ColumnSchema? desired, ColumnSchema? actual)
        {
            log?.SaveIndentLevel("ColumnDiff");

            log?.LogFormatted(LogLevel.Information, "Comparing columns for differences. Desired: {Desired}, Actual: {Actual}", args: [desired?.ColumnName, actual?.ColumnName], preIncreaseLevel: true);

            if (desired == null || actual == null)
            {
                log?.LogFormatted(LogLevel.Information, "One of the columns is null. Desired is null: {DesiredIsNull}, Actual is null: {ActualIsNull}", args: [desired == null, actual == null], singleIndentLine: true);
                return []; // one is null and the other isn't
            }
                
            log?.LogFormatted(LogLevel.Information, "Both columns are non-null. Proceeding with detailed comparison.", args: [], preIncreaseLevel: true);

            var diffs = new List<string>();

            if (!string.Equals(NormalizeType(desired.ColumnType), NormalizeType(actual.ColumnType), StringComparison.OrdinalIgnoreCase))
            {
                log?.LogFormatted(LogLevel.Information, "Column types differ. Actual: '{ActualType}', Desired: '{DesiredType}'", args: [actual.ColumnType, desired.ColumnType]);
                diffs.Add($"type {actual.ColumnType} -> {desired.ColumnType}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Column types are the same after normalization. Type: '{ColumnType}'", args: [NormalizeType(desired.ColumnType)]);

            if (desired.IsNullable != actual.IsNullable)
            {
                log?.LogFormatted(LogLevel.Information, "Column nullability differs. Actual: '{ActualNullable}', Desired: '{DesiredNullable}'", args: [actual.IsNullable, desired.IsNullable]);
                diffs.Add($"nullable {actual.IsNullable} -> {desired.IsNullable}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Column nullability is the same. Nullability: '{Nullability}'", args: [desired.IsNullable]);

            // DefaultValue: INFORMATION_SCHEMA gives raw default; desired may be SQL expression.
            // We compare string-wise; you can normalize later.
            var aDef = actual.DefaultValue;
            var dDef = desired.DefaultValue;
            if (!string.Equals(aDef ?? "", dDef ?? "", StringComparison.OrdinalIgnoreCase))
            {
                log?.LogFormatted(LogLevel.Information, "Column default value differs. Actual: '{ActualDefault}', Desired: '{DesiredDefault}'", args: [aDef, dDef]);
                diffs.Add($"default '{aDef}' -> '{dDef}'");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Column default values are the same. Default: '{DefaultValue}'", args: [aDef]);

            // Auto increment
            if (desired.IsAutoIncrement != actual.IsAutoIncrement)
            {
                log?.LogFormatted(LogLevel.Information, "Column auto_increment differs. Actual: {ActualAutoIncrement}, Desired: {DesiredAutoIncrement}", args: [actual.IsAutoIncrement, desired.IsAutoIncrement]);
                diffs.Add($"auto_increment {actual.IsAutoIncrement} -> {desired.IsAutoIncrement}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Column auto_increment is the same. Auto_increment: {AutoIncrement}", args: [desired.IsAutoIncrement]);

            log?.LogFormatted(LogLevel.Information, "Column differences count: {Diffs}", args: [diffs.Count], preDecreaseLevel: true);

            log?.RestoreIndentLevel("ColumnDiff");

            return diffs;
        }

        private bool IsSafeColumnChange(ColumnSchema desired, ColumnSchema actual)
        {
            log?.SaveIndentLevel("IsSafeColumnChange");

            // Safe change rules (non-destructive):
            // - widen varchar
            // - not-null -> nullable
            // - add/change default (usually safe; you may tighten later)
            // - enabling ON UPDATE semantics for last_updated (safe)
            // Anything else -> destructive required
            var typeSafe = IsSafeTypeChange(actual.ColumnType, desired.ColumnType);
            var nullSafe = actual.IsNullableBool == false && desired.IsNullableBool == true || actual.IsNullable == desired.IsNullable;
            var autoSafe = actual.IsAutoIncrement == desired.IsAutoIncrement; // changing auto_increment is not safe

            log?.LogFormatted(LogLevel.Information, "Column type change safe: {TypeSafe}, nullability change safe: {NullSafe}, auto_increment change safe: {AutoSafe}", args: [typeSafe, nullSafe, autoSafe]);

            log?.RestoreIndentLevel("IsSafeColumnChange");
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

        private (List<IMigrationOperation> MigrationOperations, List<string> Warnings) PlanIndexes(
            TableSchema desiredTable,
            TableSchema actualTable,
            MigrationPlanOptions options)
        {
            var migrationOperations = new List<IMigrationOperation>();
            var warnings = new List<string>();

            log?.SaveIndentLevel("PlanIndexes");

            log?.LogFormatted(LogLevel.Information, "Planning index operations for table '{TableName}'", args: [desiredTable.TableName], preIncreaseLevel: true);

            var filteredIndexList = desiredTable.Indexes.Values.OrderBy(i => i.IndexName, StringComparer.Ordinal).ToList();

            if (filteredIndexList.Count == 0)
                log?.LogFormatted(LogLevel.Information, "No indexes found in desired schema for table '{TableName}'; skipping index planning", args: [desiredTable.TableName], singleIndentLine: true);
            else
                log?.LogFormatted(LogLevel.Information, "Inspecting each index in order to generate plan", args: [], preIncreaseLevel: true);

            log?.SaveIndentLevel("indexes");
            foreach (var desiredIndex in filteredIndexList)
            {
                log?.RestoreIndentLevel("indexes");

                log?.LogFormatted(LogLevel.Information, "Planning index '{IndexName}'", args: [desiredIndex.IndexName], preIncreaseLevel: true);

                IndexSchema? actualIndex = null;
                if (!string.IsNullOrWhiteSpace(desiredIndex.IndexName) && !actualTable.Indexes.TryGetValue(desiredIndex.IndexName, out actualIndex))
                {
                    log?.LogFormatted(LogLevel.Information, "Index '{IndexName}' does not exist in actual schema; planning CreateIndex", args: [desiredIndex.IndexName], singleIndentLine: true);
                    migrationOperations.Add(new CreateIndexOperation(desiredTable.TableName, desiredIndex));
                    continue;
                }
                else
                    log?.LogFormatted(LogLevel.Information, "Index '{IndexName}' exists in actual schema; comparing for differences", args: [desiredIndex.IndexName], preIncreaseLevel: true);

                var diffs = IndexDiff(desiredIndex, actualIndex);
                if (diffs.Count == 0)
                {
                    log?.LogFormatted(LogLevel.Information, "Index '{IndexName}' has no differences, continuing to next index", args: [desiredIndex.IndexName]);
                    continue;
                }

                log?.LogFormatted(LogLevel.Information, "Index '{IndexName}' has {DiffCount} differences: {Diffs}", args: [desiredIndex.IndexName, diffs.Count, string.Join("; ", diffs)]);

                log?.LogFormatted(LogLevel.Information, "All index changes are considered {Safety}.", args: ["destructive"]);

                if (options.Destructive)
                {
                    log?.LogFormatted(LogLevel.Information, "Index '{IndexName}' differs from actual schema and destructive changes are turned on; planning DropIndex and CreateIndex", args: [desiredIndex.IndexName]);

                    if (string.IsNullOrWhiteSpace(desiredIndex.IndexName))
                    {
                        log?.LogFormatted(LogLevel.Warning, "Table '{TableName}' has an unnamed index that differs and cannot be altered; manual intervention required.", args: [desiredTable.TableName], singleIndentLine: true);
                        warnings.Add($"Table `{desiredTable.TableName}` has an unnamed index that differs and cannot be altered; manual intervention required.");
                        continue;
                    }

                    // NOTE: index drops for indexes not in desired are handled in destructive mode in PlanDestructiveDrops.
                    // Meaningful differences: drop+create
                    log?.LogFormatted(LogLevel.Information, "Planning DropIndex and CreateIndex for index '{IndexName}' on table '{TableName}'", args: [desiredIndex.IndexName, desiredTable.TableName]);
                    migrationOperations.Add(new DropIndexOperation(desiredTable.TableName, desiredIndex.IndexName));
                    migrationOperations.Add(new CreateIndexOperation(desiredTable.TableName, desiredIndex));
                }
                else 
                {
                    log?.LogFormatted(LogLevel.Warning, "Index '{IndexName}' has differences that require destructive changes but destructive mode is not enabled. Differences: {Diffs}", args: [desiredIndex.IndexName, string.Join("; ", diffs)]);
                    warnings.Add($"Table `{desiredTable.TableName}` index `{desiredIndex.IndexName}` differs ({string.Join("; ", diffs)}) and requires --destructive.");
                    continue;
                }

                log?.LogFormatted(LogLevel.Information, "Finished planning index '{IndexName}'", args: [desiredIndex.IndexName]);
            }
            log?.RestoreIndentLevel("indexes");

            log?.LogFormatted(LogLevel.Information, "Finished planning indexes for table '{TableName}', total operations planned: {OperationCount}", args: [desiredTable.TableName, migrationOperations.Count]);

            log?.RestoreIndentLevel("PlanIndexes");

            return (migrationOperations, warnings);
        }

        private List<string> IndexDiff(IndexSchema? desired, IndexSchema? actual)
        {
            log?.SaveIndentLevel("IndexDiff");

            log?.LogFormatted(LogLevel.Information, "Comparing indexes for differences. Desired: {Desired}, Actual: {Actual}", args: [desired?.IndexName, actual?.IndexName], preIncreaseLevel: true);

            if (desired == null || actual == null)
            {
                log?.LogFormatted(LogLevel.Information, "One of the indexes is null. Desired is null: {DesiredIsNull}, Actual is null: {ActualIsNull}", args: [desired == null, actual == null], singleIndentLine: true);
                return []; // one is null and the other isn't
            }

            log?.LogFormatted(LogLevel.Information, "Both indexes are non-null. Proceeding with detailed comparison.", args: [], preIncreaseLevel: true);

            var diffs = new List<string>();

            if (desired?.IndexTypeValue != actual?.IndexTypeValue)
            {
                log?.LogFormatted(LogLevel.Information, "Index types differ. Actual: '{ActualType}', Desired: '{DesiredType}'", args: [actual!.IndexType, desired!.IndexType]);
                diffs.Add($"type {actual!.IndexType} -> {desired!.IndexType}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Index types are the same. Type: '{IndexType}'", args: [desired!.IndexType]);

            // Compare columns in order, including collation/direction
            if (desired?.Columns?.Count != actual?.Columns?.Count)
            {
                log?.LogFormatted(LogLevel.Information, "Index columns count differs. Actual: {ActualCount}, Desired: {DesiredCount}", args: [actual!.Columns?.Count ?? 0, desired!.Columns?.Count ?? 0]);
                diffs.Add($"columns count {actual!.Columns?.Count ?? 0} -> {desired!.Columns?.Count ?? 0}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Index columns count is the same. Count: {ColumnCount}", args: [desired!.Columns?.Count ?? 0]);

            log?.LogFormatted(LogLevel.Information, "Comparing index columns list to detect for differences", args: []);

            log?.SaveIndentLevel("index_columns");
            for (int i = 0; i < (desired!.Columns?.Count ?? 0); i++)
            {
                log?.RestoreIndentLevel("index_columns");

                var desiredColumn = desired.Columns![i];
                var actualColumn = actual!.Columns!.OrderBy(c => c.SeqInIndex).ElementAt(i);

                log?.LogFormatted(LogLevel.Information, "Comparing index column {ColumnPosition}. Desired column: '{DesiredColumn}', Actual column: '{ActualColumn}'", args: [i + 1, desiredColumn.ColumnName, actualColumn.ColumnName], preIncreaseLevel: true);

                if (!string.Equals(desiredColumn.ColumnName, actualColumn.ColumnName, StringComparison.Ordinal))
                {
                    log?.LogFormatted(LogLevel.Information, "Index column names differ at position {ColumnPosition}. Actual: '{ActualColumn}', Desired: '{DesiredColumn}'", args: [i + 1, actualColumn.ColumnName, desiredColumn.ColumnName]);
                    diffs.Add($"column {i + 1} name {actualColumn.ColumnName} -> {desiredColumn.ColumnName}");
                }
                else
                    log?.LogFormatted(LogLevel.Information, "Index column names are the same at position {ColumnPosition}. Column name: '{ColumnName}'", args: [i + 1, desiredColumn.ColumnName]);

                // Treat collation (A/D/null) differences as meaningful
                var desiredCollation = desiredColumn.Collation ?? string.Empty;
                var actualCollation = actualColumn.Collation ?? string.Empty;
                if (!string.Equals(desiredCollation, actualCollation, StringComparison.OrdinalIgnoreCase))
                {
                    log?.LogFormatted(LogLevel.Information, "Index column collations differ at position {ColumnPosition}. Actual: '{ActualCollation}', Desired: '{DesiredCollation}'", args: [i + 1, actualCollation, desiredCollation]);
                    diffs.Add($"column {i + 1} collation {actualCollation} -> {desiredCollation}");
                }
                else
                    log?.LogFormatted(LogLevel.Information, "Index column collations are the same at position {ColumnPosition}. Collation: '{Collation}'", args: [i + 1, desiredCollation]);

                log?.LogFormatted(LogLevel.Information, "Finished comparing index column {ColumnPosition}", args: [i + 1]);
            }
            log?.RestoreIndentLevel("index_columns");

            log?.LogFormatted(LogLevel.Information, "Finished comparing indexes. Total differences found: {DiffCount}", args: [diffs.Count]);

            log?.RestoreIndentLevel("IndexDiff");
            
            return diffs;
        }

        private (List<IMigrationOperation> MigrationOperations, List<string> Warnings) PlanForeignKeys(
            TableSchema desiredTable,
            TableSchema actualTable,
            MigrationPlanOptions options)
        {
            var migrationOperations = new List<IMigrationOperation>();
            var warnings = new List<string>();

            log?.SaveIndentLevel("PlanForeignKeys");

            log?.LogFormatted(LogLevel.Information, "Planning foreign key operations for table '{TableName}'", args: [desiredTable.TableName], preIncreaseLevel: true);

            var filteredForeignKeyList = desiredTable.ForeignKeys.Values.OrderBy(f => f.ConstraintName, StringComparer.Ordinal).ToList();

            if (filteredForeignKeyList.Count == 0)
                log?.LogFormatted(LogLevel.Information, "No foreign keys found in desired schema for table '{TableName}'; skipping foreign key planning", args: [desiredTable.TableName], singleIndentLine: true);
            else
                log?.LogFormatted(LogLevel.Information, "Inspecting each foreign key in order to generate plan", args: [], preIncreaseLevel: true);

            log?.SaveIndentLevel("foreign_keys");
            foreach (var desiredForeignKey in filteredForeignKeyList)
            {
                log?.RestoreIndentLevel("foreign_keys");

                log?.LogFormatted(LogLevel.Information, "Planning foreign key '{ForeignKeyName}'", args: [desiredForeignKey.ConstraintName], preIncreaseLevel: true);

                ForeignKeySchema? actualForeignKey = null;
                if (!string.IsNullOrWhiteSpace(desiredForeignKey.ConstraintName) && !actualTable.ForeignKeys.TryGetValue(desiredForeignKey.ConstraintName, out actualForeignKey))
                {
                    log?.LogFormatted(LogLevel.Information, "Foreign key '{ForeignKeyName}' does not exist in actual schema; planning AddForeignKey", args: [desiredForeignKey.ConstraintName], singleIndentLine: true);
                    migrationOperations.Add(new AddForeignKeyOperation(desiredTable.TableName, desiredForeignKey));
                    continue;
                }
                else
                    log?.LogFormatted(LogLevel.Information, "Foreign key '{ForeignKeyName}' exists in actual schema; comparing for differences", args: [desiredForeignKey.ConstraintName], preIncreaseLevel: true);

                var diffs = ForeignKeyDiff(desiredForeignKey, actualForeignKey);
                if (diffs.Count == 0)
                {
                    log?.LogFormatted(LogLevel.Information, "Foreign key '{ForeignKeyName}' has no differences, continuing to next foreign key", args: [desiredForeignKey.ConstraintName]);
                    continue;
                }

                log?.LogFormatted(LogLevel.Information, "Foreign key '{ForeignKeyName}' has {DiffCount} differences: {Diffs}", args: [desiredForeignKey.ConstraintName, diffs.Count, string.Join("; ", diffs)]);

                log?.LogFormatted(LogLevel.Information, "All foreign key changes are considered {Safety}.", args: ["non-destructive"]);

                log?.LogFormatted(LogLevel.Information, "Foreign key '{ForeignKeyName}' differs from actual schema; planning DropForeignKey and AddForeignKey", args: [desiredForeignKey.ConstraintName]);

                if (string.IsNullOrWhiteSpace(desiredForeignKey.ConstraintName))
                {
                    log?.LogFormatted(LogLevel.Warning, "Table '{TableName}' has an unnamed foreign key that differs and cannot be altered; manual intervention required.", args: [desiredTable.TableName], singleIndentLine: true);
                    warnings.Add($"Table `{desiredTable.TableName}` has an unnamed foreign key that differs and cannot be altered; manual intervention required.");
                    continue;
                }

                // Drop+add is data-safe; allow even in non-destructive mode
                log?.LogFormatted(LogLevel.Information, "Planning DropForeignKey and AddForeignKey for foreign key '{ForeignKeyName}' on table '{TableName}'", args: [desiredForeignKey.ConstraintName, desiredTable.TableName]);
                migrationOperations.Add(new DropForeignKeyOperation(desiredTable.TableName, desiredForeignKey.ConstraintName));
                migrationOperations.Add(new AddForeignKeyOperation(desiredTable.TableName, desiredForeignKey));

                log?.LogFormatted(LogLevel.Information, "Finished planning foreign key '{ForeignKeyName}'", args: [desiredForeignKey.ConstraintName]);
            }
            log?.RestoreIndentLevel("foreign_keys");

            log?.LogFormatted(LogLevel.Information, "Finished planning foreign keys for table '{TableName}', total operations planned: {OperationCount}", args: [desiredTable.TableName, migrationOperations.Count]);

            log?.RestoreIndentLevel("PlanForeignKeys");

            return (migrationOperations, warnings);
        }

        private List<string> ForeignKeyDiff(ForeignKeySchema? desired, ForeignKeySchema? actual)
        {
            log?.SaveIndentLevel("ForeignKeyDiff");

            log?.LogFormatted(LogLevel.Information, "Comparing foreign keys for differences. Desired: {Desired}, Actual: {Actual}", args: [desired?.ConstraintName, actual?.ConstraintName], preIncreaseLevel: true);

            if (desired == null || actual == null)
            {
                log?.LogFormatted(LogLevel.Information, "One of the foreign keys is null. Desired is null: {DesiredIsNull}, Actual is null: {ActualIsNull}", args: [desired == null, actual == null], singleIndentLine: true);
                return []; // one is null and the other isn't
            }
            else 
                log?.LogFormatted(LogLevel.Information, "Both foreign keys are non-null. Proceeding with detailed comparison.", args: [], preIncreaseLevel: true);

            var diffs = new List<string>();

            if (!string.Equals(desired?.ReferencedTableName, actual?.ReferencedTableName, StringComparison.OrdinalIgnoreCase))
            {
                log?.LogFormatted(LogLevel.Information, "Foreign key referenced table names differ. Actual: '{ActualReferencedTable}', Desired: '{DesiredReferencedTable}'", args: [actual.ReferencedTableName, desired.ReferencedTableName]);
                diffs.Add($"referenced table {actual.ReferencedTableName} -> {desired.ReferencedTableName}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Foreign key referenced table names are the same. Referenced table: '{ReferencedTable}'", args: [desired.ReferencedTableName]);

            if (desired.ColumnNames == null || actual.ColumnNames == null)
            {
                log?.LogFormatted(LogLevel.Information, "One of the foreign keys has null local column names. Desired ColumnNames is null: {DesiredColumnNamesIsNull}, Actual ColumnNames is null: {ActualColumnNamesIsNull}", args: [desired.ColumnNames == null, actual.ColumnNames == null], singleIndentLine: true);
                diffs.Add($"local column names null {actual.ColumnNames == null} -> {desired.ColumnNames == null}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Foreign key local column names are both non-null. Proceeding with comparison.", args: []);

            if (desired.ReferencedColumnNames == null || actual.ReferencedColumnNames == null)
            {
                log?.LogFormatted(LogLevel.Information, "One of the foreign keys has null referenced column names. Desired ReferencedColumnNames is null: {DesiredReferencedColumnNamesIsNull}, Actual ReferencedColumnNames is null: {ActualReferencedColumnNamesIsNull}", args: [desired.ReferencedColumnNames == null, actual.ReferencedColumnNames == null], singleIndentLine: true);
                diffs.Add($"referenced column names null {actual.ReferencedColumnNames == null} -> {desired.ReferencedColumnNames == null}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Foreign key referenced column names are both non-null. Proceeding with comparison.", args: []);

            if (desired.ColumnNames.Count != actual.ColumnNames.Count)
            {
                log?.LogFormatted(LogLevel.Information, "Foreign key local column names count differs. Actual: {ActualCount}, Desired: {DesiredCount}", args: [actual.ColumnNames?.Count ?? 0, desired.ColumnNames?.Count ?? 0]);
                diffs.Add($"local column names count {actual.ColumnNames?.Count ?? 0} -> {desired.ColumnNames?.Count ?? 0}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Foreign key local column names count are the same. Count: {Count}", args: [desired.ColumnNames?.Count ?? 0]);

            if (desired.ReferencedColumnNames.Count != actual.ReferencedColumnNames.Count)
            {
                log?.LogFormatted(LogLevel.Information, "Foreign key referenced column names count differs. Actual: {ActualCount}, Desired: {DesiredCount}", args: [actual.ReferencedColumnNames?.Count ?? 0, desired.ReferencedColumnNames?.Count ?? 0]);
                diffs.Add($"referenced column names count {actual.ReferencedColumnNames?.Count ?? 0} -> {desired.ReferencedColumnNames?.Count ?? 0}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Foreign key referenced column names count are the same. Count: {Count}", args: [desired.ReferencedColumnNames?.Count ?? 0]);

            log?.LogFormatted(LogLevel.Information, "Comparing foreign key column names list to detect for differences", args: []);
            log?.SaveIndentLevel("foreign_key_columns");
            for (int i = 0; i < desired.ColumnNames?.Count; i++)
            {
                log?.RestoreIndentLevel("foreign_key_columns");

                log?.LogFormatted(LogLevel.Information, "Comparing foreign key local column name at position {ColumnPosition}. Desired column: '{DesiredColumn}', Actual column: '{ActualColumn}'", args: [i + 1, desired.ColumnNames[i], actual.ColumnNames[i]], preIncreaseLevel: true);

                if (!string.Equals(desired.ColumnNames[i], actual.ColumnNames[i], StringComparison.Ordinal))
                {
                    log?.LogFormatted(LogLevel.Information, "Foreign key local column names differ at position {ColumnPosition}. Actual: '{ActualColumn}', Desired: '{DesiredColumn}'", args: [i + 1, actual.ColumnNames[i], desired.ColumnNames[i]]);
                    diffs.Add($"local column {i + 1} name {actual.ColumnNames[i]} -> {desired.ColumnNames[i]}");
                }
                else
                    log?.LogFormatted(LogLevel.Information, "Foreign key local column names are the same at position {ColumnPosition}. Column name: '{ColumnName}'", args: [i + 1, desired.ColumnNames[i]]);

                log?.LogFormatted(LogLevel.Information, "Finished comparing foreign key local column name at position {ColumnPosition}", args: [i + 1]);
            }
            log?.RestoreIndentLevel("foreign_key_columns");
            log?.LogFormatted(LogLevel.Information, "Finished comparing foreign key local column names.", args: []);

            log?.LogFormatted(LogLevel.Information, "Comparing foreign key referenced column names list to detect for differences", args: []);
            log?.SaveIndentLevel("foreign_key_referenced_columns");
            for (int i = 0; i < desired.ReferencedColumnNames.Count; i++)
            {
                log?.RestoreIndentLevel("foreign_key_referenced_columns");

                log?.LogFormatted(LogLevel.Information, "Comparing foreign key referenced column name at position {ColumnPosition}. Desired column: '{DesiredColumn}', Actual column: '{ActualColumn}'", args: [i + 1, desired.ReferencedColumnNames[i], actual.ReferencedColumnNames[i]], preIncreaseLevel: true);

                if (!string.Equals(desired.ReferencedColumnNames[i], actual.ReferencedColumnNames[i], StringComparison.Ordinal))
                {
                    log?.LogFormatted(LogLevel.Information, "Foreign key referenced column names differ at position {ColumnPosition}. Actual: '{ActualColumn}', Desired: '{DesiredColumn}'", args: [i + 1, actual.ReferencedColumnNames[i], desired.ReferencedColumnNames[i]]);
                    diffs.Add($"referenced column {i + 1} name {actual.ReferencedColumnNames[i]} -> {desired.ReferencedColumnNames[i]}");
                }
                else
                    log?.LogFormatted(LogLevel.Information, "Foreign key referenced column names are the same at position {ColumnPosition}. Column name: '{ColumnName}'", args: [i + 1, desired.ReferencedColumnNames[i]]);

                log?.LogFormatted(LogLevel.Information, "Finished comparing foreign key referenced column name at position {ColumnPosition}", args: [i + 1]);
            }
            log?.RestoreIndentLevel("foreign_key_referenced_columns");

            log?.LogFormatted(LogLevel.Information, "Finished comparing foreign key referenced column names.", args: []);

            if (!string.Equals(desired.DeleteRule, actual.DeleteRule, StringComparison.OrdinalIgnoreCase))
            {
                log?.LogFormatted(LogLevel.Information, "Foreign key delete rules differ. Actual: '{ActualDeleteRule}', Desired: '{DesiredDeleteRule}'", args: [actual.DeleteRule, desired.DeleteRule]);
                diffs.Add($"delete rule {actual.DeleteRule} -> {desired.DeleteRule}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Foreign key delete rules are the same. Rule: '{DeleteRule}'", args: [desired.DeleteRule]);

            if (!string.Equals(desired.UpdateRule, actual.UpdateRule, StringComparison.OrdinalIgnoreCase))
            {
                log?.LogFormatted(LogLevel.Information, "Foreign key update rules differ. Actual: '{ActualUpdateRule}', Desired: '{DesiredUpdateRule}'", args: [actual.UpdateRule, desired.UpdateRule]);
                diffs.Add($"update rule {actual.UpdateRule} -> {desired.UpdateRule}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Foreign key update rules are the same. Rule: '{UpdateRule}'", args: [desired.UpdateRule]);

            log?.LogFormatted(LogLevel.Information, "Finished comparing foreign keys. Total differences found: {DiffCount}", args: [diffs.Count]);

            log?.RestoreIndentLevel("ForeignKeyDiff");

            return diffs;
        }

        private (List<IMigrationOperation> MigrationOperations, List<string> Warnings) PlanTriggers(
            TableSchema desiredTable,
            TableSchema actualTable,
            MigrationPlanOptions options)
        {
            var migrationOperations = new List<IMigrationOperation>();
            var warnings = new List<string>();

            log?.SaveIndentLevel("PlanTriggers");

            log?.LogFormatted(LogLevel.Information, "Planning trigger operations for table '{TableName}'", args: [desiredTable.TableName], preIncreaseLevel: true);

            var filteredTriggerList = desiredTable.Triggers.Values.OrderBy(t => t.TriggerName, StringComparer.Ordinal).ToList();

            if (filteredTriggerList.Count == 0)
                log?.LogFormatted(LogLevel.Information, "No triggers found in desired schema for table '{TableName}'; skipping trigger planning", args: [desiredTable.TableName], singleIndentLine: true);
            else
                log?.LogFormatted(LogLevel.Information, "Inspecting each trigger in order to generate plan", args: [], preIncreaseLevel: true);

            log?.SaveIndentLevel("triggers");
            foreach (var desiredTrigger in filteredTriggerList)
            {
                log?.RestoreIndentLevel("triggers");

                log?.LogFormatted(LogLevel.Information, "Planning trigger '{TriggerName}'", args: [desiredTrigger.TriggerName], preIncreaseLevel: true);

                TriggerSchema? actualTrigger = null;
                if (!string.IsNullOrWhiteSpace(desiredTrigger.TriggerName) && !actualTable.Triggers.TryGetValue(desiredTrigger.TriggerName, out actualTrigger))
                {
                    log?.LogFormatted(LogLevel.Information, "Trigger '{TriggerName}' does not exist in actual schema; planning CreateTrigger", args: [desiredTrigger.TriggerName], singleIndentLine: true);
                    migrationOperations.Add(new CreateTriggerOperation(desiredTable.TableName, desiredTrigger));
                    continue;
                }
                else
                    log?.LogFormatted(LogLevel.Information, "Trigger '{TriggerName}' exists in actual schema; comparing for differences", args: [desiredTrigger.TriggerName], preIncreaseLevel: true);

                var diffs = TriggerDiff(desiredTrigger, actualTrigger);
                if (diffs.Count == 0)
                {
                    log?.LogFormatted(LogLevel.Information, "Trigger '{TriggerName}' has no differences, continuing to next trigger", args: [desiredTrigger.TriggerName]);
                    continue;
                }

                log?.LogFormatted(LogLevel.Information, "Trigger '{TriggerName}' has {DiffCount} differences: {Diffs}", args: [desiredTrigger.TriggerName, diffs.Count, string.Join("; ", diffs)]);

                log?.LogFormatted(LogLevel.Information, "All trigger changes are considered {Safety}.", args: ["non-destructive"]);

                log?.LogFormatted(LogLevel.Information, "Trigger '{TriggerName}' differs from actual schema; planning DropTrigger and CreateTrigger", args: [desiredTrigger.TriggerName]);

                if (string.IsNullOrWhiteSpace(desiredTrigger.TriggerName))
                {
                    log?.LogFormatted(LogLevel.Warning, "Table '{TableName}' has an unnamed trigger that differs and cannot be altered; manual intervention required.", args: [desiredTable.TableName], singleIndentLine: true);
                    warnings.Add($"Table `{desiredTable.TableName}` has an unnamed trigger that differs and cannot be altered; manual intervention required.");
                    continue;
                }

                // Drop+create is data-safe
                log?.LogFormatted(LogLevel.Information, "Planning DropTrigger and CreateTrigger for trigger '{TriggerName}' on table '{TableName}'", args: [desiredTrigger.TriggerName, desiredTable.TableName]);
                migrationOperations.Add(new DropTriggerOperation(desiredTable.TableName, desiredTrigger.TriggerName));
                migrationOperations.Add(new CreateTriggerOperation(desiredTable.TableName, desiredTrigger));

                log?.LogFormatted(LogLevel.Information, "Finished planning trigger '{TriggerName}'", args: [desiredTrigger.TriggerName]);
            }
            log?.RestoreIndentLevel("triggers");

            log?.LogFormatted(LogLevel.Information, "Finished planning triggers for table '{TableName}', total operations planned: {OperationCount}", args: [desiredTable.TableName, migrationOperations.Count]);

            log?.RestoreIndentLevel("PlanTriggers");

            return (migrationOperations, warnings);
        }

        private List<string> TriggerDiff(TriggerSchema? desired, TriggerSchema? actual)
        {
            log?.SaveIndentLevel("TriggerDiff");

            log?.LogFormatted(LogLevel.Information, "Comparing triggers for differences. Desired: {Desired}, Actual: {Actual}", args: [desired?.TriggerName, actual?.TriggerName], preIncreaseLevel: true);

            if (desired == null || actual == null)
            {
                log?.LogFormatted(LogLevel.Information, "One of the triggers is null. Desired is null: {DesiredIsNull}, Actual is null: {ActualIsNull}", args: [desired == null, actual == null], singleIndentLine: true);
                return []; // one is null and the other isn't
            }
            else
                log?.LogFormatted(LogLevel.Information, "Both triggers are non-null. Proceeding with detailed comparison.", args: [], preIncreaseLevel: true);

            var diffs = new List<string>();

            if (desired?.EventManipulation != actual?.EventManipulation)
            {
                log?.LogFormatted(LogLevel.Information, "Trigger event manipulations differ. Actual: '{ActualEvent}', Desired: '{DesiredEvent}'", args: [actual.EventManipulation, desired.EventManipulation]);
                diffs.Add($"event manipulation {actual.EventManipulation} -> {desired.EventManipulation}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Trigger event manipulations are the same. Event manipulation: '{EventManipulation}'", args: [desired.EventManipulation]);

            if (desired?.ActionTiming != actual?.ActionTiming)
            {
                log?.LogFormatted(LogLevel.Information, "Trigger action timings differ. Actual: '{ActualTiming}', Desired: '{DesiredTiming}'", args: [actual.ActionTiming, desired.ActionTiming]);
                diffs.Add($"action timing {actual.ActionTiming} -> {desired.ActionTiming}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Trigger action timings are the same. Action timing: '{ActionTiming}'", args: [desired.ActionTiming]);

            // Normalize whitespace for comparison
            var dStmt = NormalizeSql(desired!.ActionStatement);
            var aStmt = NormalizeSql(actual!.ActionStatement);

            if (!string.Equals(dStmt, aStmt, StringComparison.OrdinalIgnoreCase))
            {
                log?.LogFormatted(LogLevel.Information, "Trigger action statements differ. Actual: '{ActualStatement}', Desired: '{DesiredStatement}'", args: [aStmt, dStmt]);
                diffs.Add($"action statement {aStmt} -> {dStmt}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Trigger action statements are the same. Action statement: '{ActionStatement}'", args: [dStmt]);

            log?.LogFormatted(LogLevel.Information, "Finished comparing triggers. Total differences found: {DiffCount}", args: [diffs.Count]);

            log?.RestoreIndentLevel("TriggerDiff");

            return diffs;
        }

        private (List<IMigrationOperation> MigrationOperations, List<string> Warnings) PlanFunctions(
            SchemaSnapshot desiredSchema,
            SchemaSnapshot actualSchema,
            MigrationPlanOptions options)
        {
            var migrationOperations = new List<IMigrationOperation>();
            var warnings = new List<string>();

            log?.SaveIndentLevel("PlanFunctions");

            log?.LogFormatted(LogLevel.Information, "Planning function operations for database '{DatabaseName}'", args: [desiredSchema.DatabaseName]);

            var filteredFunctionList = desiredSchema.Functions.Values.OrderBy(t => t.RoutineName, StringComparer.Ordinal).ToList();

            if (filteredFunctionList.Count == 0)
                log?.LogFormatted(LogLevel.Information, "No functions found in desired schema for database '{DatabaseName}'; skipping function planning", args: [desiredSchema.DatabaseName], singleIndentLine: true);
            else
                log?.LogFormatted(LogLevel.Information, "Inspecting each function in order to generate plan", args: []);

            log?.SaveIndentLevel("functions");
            foreach (var desiredFunction in filteredFunctionList)
            {
                log?.RestoreIndentLevel("functions");

                log?.LogFormatted(LogLevel.Information, "Planning function '{FunctionName}'", args: [desiredFunction.RoutineName], preIncreaseLevel: true);
                
                FunctionSchema? actualFunction = null;
                if (!string.IsNullOrWhiteSpace(desiredFunction.RoutineName) && !(actualSchema.Functions?.TryGetValue(desiredFunction.RoutineName, out actualFunction) ?? false))
                {
                    log?.LogFormatted(LogLevel.Information, "Function '{FunctionName}' does not exist in actual schema; planning CreateFunction", args: [desiredFunction.RoutineName], singleIndentLine: true);
                    if (options.DropFunctionsOnCreate)
                    {
                        log?.LogFormatted(LogLevel.Information, "Option 'DropFunctionsOnCreate' is enabled; planning DropFunction before CreateFunction for function '{FunctionName}'", args: [desiredFunction.RoutineName]);
                        migrationOperations.Add(new DropFunctionOperation(desiredFunction.RoutineName, desiredFunction));
                    }
                    migrationOperations.Add(new CreateFunctionOperation(desiredFunction.RoutineName, desiredFunction));
                    continue;
                }
                else 
                    log?.LogFormatted(LogLevel.Information, "Function '{FunctionName}' exists in actual schema; comparing for differences", args: [desiredFunction.RoutineName], preIncreaseLevel: true);

                var diffs = FunctionDiffers(desiredFunction, actualFunction);
                if (diffs.Count == 0)
                {
                    log?.LogFormatted(LogLevel.Information, "Function '{FunctionName}' has no differences, continuing to next function", args: [desiredFunction.RoutineName], preDecreaseLevel: true);
                    continue;
                }

                log?.LogFormatted(LogLevel.Information, "Function '{FunctionName}' has differences that require changes: {Diffs}", args: [desiredFunction.RoutineName, string.Join("; ", diffs)]);

                log?.LogFormatted(LogLevel.Information, "All function changes are considered {Safety}.", args: ["non-destructive"]);

                log?.LogFormatted(LogLevel.Information, "Function '{FunctionName}' differs from actual schema; planning DropFunction and CreateFunction", args: [desiredFunction.RoutineName]);

                if (string.IsNullOrWhiteSpace(desiredFunction.RoutineName))
                {
                    log?.LogFormatted(LogLevel.Warning, "Table '{DatabaseName}' has an unnamed function that differs and cannot be altered; manual intervention required.", args: [desiredSchema.DatabaseName], singleIndentLine: true);
                    warnings.Add($"Table `{desiredSchema.DatabaseName}` has an unnamed function that differs and cannot be altered; manual intervention required.");
                    continue;
                }

                // Drop+create is data-safe
                log?.LogFormatted(LogLevel.Information, "Function '{FunctionName}' differs from actual schema; planning DropFunction and CreateFunction", args: [desiredFunction.RoutineName]);
                migrationOperations.Add(new DropFunctionOperation(desiredFunction.RoutineName, desiredFunction));
                migrationOperations.Add(new CreateFunctionOperation(desiredFunction.RoutineName, desiredFunction));

                log?.LogFormatted(LogLevel.Information, "Finished planning function '{FunctionName}'", args: [desiredFunction.RoutineName]);
            }
            log?.RestoreIndentLevel("functions");

            log?.LogFormatted(LogLevel.Information, "Finished planning functions for database '{DatabaseName}', total operations planned: {OperationCount}", args: [desiredSchema.DatabaseName, migrationOperations.Count]);

            log?.RestoreIndentLevel("PlanFunctions");

            return (migrationOperations, warnings);
        }

        private List<string> FunctionDiffers(FunctionSchema? desired, FunctionSchema? actual)
        {
            log?.SaveIndentLevel("FunctionDiffers");

            log?.LogFormatted(LogLevel.Information, "Comparing functions for differences. Desired: {Desired}, Actual: {Actual}", args: [desired?.RoutineName, actual?.RoutineName], preIncreaseLevel: true);

            if (desired == null || actual == null)
            {
                log?.LogFormatted(LogLevel.Information, "One of the functions is null. Desired is null: {DesiredIsNull}, Actual is null: {ActualIsNull}", args: [desired == null, actual == null], singleIndentLine: true);
                return []; // one is null and the other isn't
            }
            else
                log?.LogFormatted(LogLevel.Information, "Both functions are non-null. Proceeding with detailed comparison.", args: [], preIncreaseLevel: true);

            var diffs = new List<string>();

            if (desired.RoutineTypeValue != actual.RoutineTypeValue)
            {
                log?.LogFormatted(LogLevel.Information, "Function routine types differ. Actual: '{ActualRoutineType}', Desired: '{DesiredRoutineType}'", args: [actual.RoutineTypeValue, desired.RoutineTypeValue]);
                diffs.Add($"routine type {actual.RoutineTypeValue} -> {desired.RoutineTypeValue}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Function routine types are the same. Routine type: '{RoutineType}'", args: [desired.RoutineTypeValue]);

            if (desired.RoutineComment != actual.RoutineComment)
            {
                log?.LogFormatted(LogLevel.Information, "Function routine comments differ. Actual: '{ActualComment}', Desired: '{DesiredComment}'", args: [actual.RoutineComment, desired.RoutineComment]);
                diffs.Add($"routine comment {actual.RoutineComment} -> {desired.RoutineComment}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Function routine comments are the same. Routine comment: '{RoutineComment}'", args: [desired.RoutineComment]);

            if (desired.NumericPrecision != actual.NumericPrecision)
            {
                log?.LogFormatted(LogLevel.Information, "Function numeric precision differ. Actual: {ActualNumericPrecision}, Desired: {DesiredNumericPrecision}", args: [actual.NumericPrecision, desired.NumericPrecision]);
                diffs.Add($"numeric precision {actual.NumericPrecision} -> {desired.NumericPrecision}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Function numeric precision are the same. Numeric precision: {NumericPrecision}", args: [desired.NumericPrecision]);

            if (desired.NumericScale != actual.NumericScale)
            {
                log?.LogFormatted(LogLevel.Information, "Function numeric scale differ. Actual: {ActualNumericScale}, Desired: {DesiredNumericScale}", args: [actual.NumericScale, desired.NumericScale]);
                diffs.Add($"numeric scale {actual.NumericScale} -> {desired.NumericScale}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Function numeric scale are the same. Numeric scale: {NumericScale}", args: [desired.NumericScale]);

            if (desired.CharacterMaximumLength != actual.CharacterMaximumLength)
            {
                log?.LogFormatted(LogLevel.Information, "Function character maximum length differ. Actual: {ActualCharacterMaximumLength}, Desired: {DesiredCharacterMaximumLength}", args: [actual.CharacterMaximumLength, desired.CharacterMaximumLength]);
                diffs.Add($"character maximum length {actual.CharacterMaximumLength} -> {desired.CharacterMaximumLength}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Function character maximum length are the same. Character maximum length: {CharacterMaximumLength}", args: [desired.CharacterMaximumLength]);

            if (desired.DatetimePrecision != actual.DatetimePrecision)
            {
                log?.LogFormatted(LogLevel.Information, "Function datetime precision differ. Actual: {ActualDatetimePrecision}, Desired: {DesiredDatetimePrecision}", args: [actual.DatetimePrecision, desired.DatetimePrecision]);
                diffs.Add($"datetime precision {actual.DatetimePrecision} -> {desired.DatetimePrecision}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Function datetime precision are the same. Datetime precision: {DatetimePrecision}", args: [desired.DatetimePrecision]);

            if (desired.SqlDataAccessValue != actual.SqlDataAccessValue)
            {
                log?.LogFormatted(LogLevel.Information, "Function SQL data access value differ. Actual: {ActualSqlDataAccessValue}, Desired: {DesiredSqlDataAccessValue}", args: [actual.SqlDataAccessValue, desired.SqlDataAccessValue]);
                diffs.Add($"SQL data access value {actual.SqlDataAccessValue} -> {desired.SqlDataAccessValue}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Function SQL data access value are the same. SQL data access value: {SqlDataAccessValue}", args: [desired.SqlDataAccessValue]);

            if (desired.SecurityType != actual.SecurityType)
            {
                log?.LogFormatted(LogLevel.Information, "Function security types differ. Actual: '{ActualSecurityType}', Desired: '{DesiredSecurityType}'", args: [actual.SecurityType, desired.SecurityType]);
                diffs.Add($"security type {actual.SecurityType} -> {desired.SecurityType}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Function security types are the same. Security type: '{SecurityType}'", args: [desired.SecurityType]);

            if (desired.IsDeterministicValue != actual.IsDeterministicValue)
            {
                log?.LogFormatted(LogLevel.Information, "Function deterministic values differ. Actual: {ActualIsDeterministic}, Desired: {DesiredIsDeterministic}", args: [actual.IsDeterministicValue, desired.IsDeterministicValue]);
                diffs.Add($"deterministic {actual.IsDeterministicValue} -> {desired.IsDeterministicValue}");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Function deterministic values are the same. Is deterministic: {IsDeterministic}", args: [desired.IsDeterministicValue]);

            // Normalize whitespace for comparison
            var dStmt = NormalizeSql(desired.RoutineDefinition).Trim();
            var aStmt = NormalizeSql(actual.RoutineDefinition).Trim();

            if (!string.Equals(dStmt, aStmt, StringComparison.OrdinalIgnoreCase))
            {
                log?.LogFormatted(LogLevel.Information, "Function definitions differ. Actual: {ActualRoutineDefinition}, Desired: {DesiredRoutineDefinition}", args: [aStmt, dStmt]);
                diffs.Add("routine definition differs");
            }
            else
                log?.LogFormatted(LogLevel.Information, "Function definitions are the same.", args: []);

            log?.LogFormatted(LogLevel.Information, "Finished comparing functions. Total differences found: {DiffCount}", args: [diffs.Count]);

            log?.SaveIndentLevel("FunctionDiffers");

            return diffs;
        }

        private static string NormalizeSql(string? originalSql)
        {
            if (string.IsNullOrWhiteSpace(originalSql)) 
                return string.Empty;

            // remove all comment lines
            var lines = originalSql.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            var uncommentedLines = lines.Where(line => !line.TrimStart().StartsWith("--")).ToArray();
            var uncommentedSql = string.Join(" ", uncommentedLines);


            return string.Join(" ", uncommentedSql.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries));
        }

        private void PlanDestructiveDrops(
            string tableName,
            TableSchema desiredTable,
            TableSchema actualTable,
            List<IMigrationOperation> ops,
            List<string> warnings)
        {
            log?.SaveIndentLevel("PlanDestructiveDrops");

            log?.LogFormatted(LogLevel.Information, "Planning destructive drops for table '{TableName}'", args: [tableName]);

            // Drop indexes present in actual but not desired (excluding PRIMARY)
            log?.LogFormatted(LogLevel.Information, "Planning destructive index drops for indexes present in actual but not desired", args: []);
            log?.SaveIndentLevel("indexes");
            foreach (var actualIdxName in actualTable.Indexes.Keys.OrderBy(x => x, StringComparer.Ordinal))
            {
                log?.RestoreIndentLevel("indexes");

                log?.LogFormatted(LogLevel.Information, "Inspecting index '{IndexName}' for potential destructive drop", args: [actualIdxName]);

                if (string.Equals(actualIdxName, "PRIMARY", StringComparison.OrdinalIgnoreCase))
                {
                    log?.LogFormatted(LogLevel.Information, "Skipping PRIMARY index for destructive drop consideration", args: [actualIdxName]);
                    continue;
                }
                else
                    log?.LogFormatted(LogLevel.Information, "Index '{IndexName}' is not present in desired table, marking for drop", args: [actualIdxName]);

                if (!desiredTable.Indexes.ContainsKey(actualIdxName))
                {
                    log?.LogFormatted(LogLevel.Information, "Index '{IndexName}' exists in actual schema but not in desired schema; planning DropIndex", args: [actualIdxName]);
                    ops.Add(new DropIndexOperation(tableName, actualIdxName));
                }
                else
                    log?.LogFormatted(LogLevel.Information, "Index '{IndexName}' is not present in desired table, marking for drop", args: [actualIdxName]);

                log?.LogFormatted(LogLevel.Information, "Finished inspecting index '{IndexName}'", args: [actualIdxName]);
            }
            log?.RestoreIndentLevel("indexes");

            log?.LogFormatted(LogLevel.Information, "Finished planning destructive index drops for table '{TableName}'", args: [tableName]);

            // Drop foreign keys present in actual but not desired
            log?.LogFormatted(LogLevel.Information, "Planning destructive foreign key drops for foreign keys present in actual but not desired", args: []);
            log?.RestoreIndentLevel("indexes");
            foreach (var actualFkName in actualTable.ForeignKeys.Keys.OrderBy(x => x, StringComparer.Ordinal))
            {
                log?.RestoreIndentLevel("indexes");

                log?.LogFormatted(LogLevel.Information, "Inspecting foreign key '{ForeignKeyName}' for potential destructive drop", args: [actualFkName]);

                if (!desiredTable.ForeignKeys.ContainsKey(actualFkName))
                {
                    log?.LogFormatted(LogLevel.Information, "Foreign key '{ForeignKeyName}' exists in actual schema but not in desired schema; planning DropForeignKey", args: [actualFkName]);
                    ops.Add(new DropForeignKeyOperation(tableName, actualFkName));
                }
                else
                    log?.LogFormatted(LogLevel.Information, "Foreign key '{ForeignKeyName}' exists in desired schema; skipping drop", args: [actualFkName]);

                log?.LogFormatted(LogLevel.Information, "Finished inspecting foreign key '{ForeignKeyName}'", args: [actualFkName]);
            }
            log?.RestoreIndentLevel("indexes");

            log?.LogFormatted(LogLevel.Information, "Finished planning destructive foreign key drops for table '{TableName}'", args: [tableName]);

            // Drop triggers present in actual but not desired
            log?.LogFormatted(LogLevel.Information, "Planning destructive trigger drops for triggers present in actual but not desired", args: []);
            log?.RestoreIndentLevel("indexes");
            foreach (var actualTrgName in actualTable.Triggers.Keys.OrderBy(x => x, StringComparer.Ordinal))
            {
                log?.RestoreIndentLevel("indexes");

                log?.LogFormatted(LogLevel.Information, "Inspecting trigger '{TriggerName}' for potential destructive drop", args: [actualTrgName]);

                if (!desiredTable.Triggers.ContainsKey(actualTrgName))
                {
                    log?.LogFormatted(LogLevel.Information, "Trigger '{TriggerName}' exists in actual schema but not in desired schema; planning DropTrigger", args: [actualTrgName]);
                    ops.Add(new DropTriggerOperation(tableName, actualTrgName));
                }
                else
                    log?.LogFormatted(LogLevel.Information, "Trigger '{TriggerName}' exists in desired schema; skipping drop", args: [actualTrgName]);

                log?.LogFormatted(LogLevel.Information, "Finished inspecting trigger '{TriggerName}'", args: [actualTrgName]);
            }
            log?.RestoreIndentLevel("indexes");

            log?.LogFormatted(LogLevel.Information, "Finished planning destructive trigger drops for table '{TableName}'", args: [tableName]);

            log?.LogFormatted(LogLevel.Information, "Finished planning all destructive drops for table '{TableName}'", args: [tableName]);

            log?.RestoreIndentLevel("PlanDestructiveDrops");
        }

        private static int RankOperations(IMigrationOperation op) => op switch
        {
            DropFunctionOperation => 10,
            CreateFunctionOperation => 20,
            CreateTableOperation => 30,
            AddColumnOperation => 40,
            AlterColumnOperation => 50,
            DropIndexOperation => 60,
            CreateIndexOperation => 70,
            DropForeignKeyOperation => 80,
            AddForeignKeyOperation => 90,
            DropTriggerOperation => 100,
            CreateTriggerOperation => 110,
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
    }
}