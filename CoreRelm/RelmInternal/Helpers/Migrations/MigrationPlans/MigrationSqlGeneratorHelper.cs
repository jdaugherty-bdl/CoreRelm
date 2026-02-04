using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using CoreRelm.RelmInternal.Helpers.Migrations.Introspection;
using CoreRelm.RelmInternal.Helpers.Migrations.Provisioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Migrations.MigrationPlans
{
    internal class MigrationSqlGeneratorHelper
    {
        /*
        public async static Task<MigrationGenerateResult> GenerateMigrationForDatabase(string migrationName, string stampUtc, string setName, string dbName, List<ValidatedModelType> modelsForDb, CancellationToken cancellationToken)
        {
            // Deterministic ordering
            var tables = modelsForDb
                .OrderBy(m => m.TableName, StringComparer.Ordinal)
                .ThenBy(m => m.ClrType.FullName, StringComparer.Ordinal)
                .ToList();

            // TODO (later): replace this placeholder with CoreRelm:
            // desired = metadataReader.Describe(...)
            // actual = introspector.LoadSchema(...)
            // plan = planner.Plan(... scoped destructive ...)
            // if plan empty -> return NoChanges(...)
            // sql = renderer.Render(plan)

            var sql = await BuildPlaceholderMigrationSql(
                migrationName: migrationName,
                stampUtc: stampUtc,
                setName: setName,
                dbName: dbName,
                tables: tables,
                cancellationToken: cancellationToken);

            // In the real version, if plan has no operations, return NoChanges and sql would be null.
            // For placeholder, treat as changes.
            if (string.IsNullOrWhiteSpace(sql))
                return MigrationGenerateResult.NoChanges(dbName, $"No changes detected for database '{dbName}'. No migration file written.");

            return MigrationGenerateResult.Changes(
                dbName,
                sql,
                $"Changes detected for database '{dbName}'. Migration will be generated.");
        }
        */
        public async static Task<string?> BuildPlaceholderMigrationSql(MigrationOptions migrationOptions, string migrationName, string stampUtc, string dbName, List<ValidatedModelType> tables, MySqlDatabaseProvisioner provisioner)
        {
            var dbConn = migrationOptions.ConnectionStringTemplate.Replace("{db}", dbName, StringComparison.Ordinal);

            var exists = false;
            if (migrationOptions.Apply)
            {
                var ok = await DbAvailabilityHelper.EnsureForApplyOrMigrateAsync(
                    migrationOptions,
                    provisioner,
                    dbName,
                    logInfo: msg => { if (!migrationOptions.Quiet) Console.WriteLine(msg); },
                    logWarn: msg => Console.WriteLine("ERROR: " + msg));

                if (!ok)
                {
                    Environment.ExitCode = 1;
                    return null;
                }
            }
            else
            {
                exists = await DbAvailabilityHelper.WarnIfMissingAsync(
                    migrationOptions,
                    provisioner,
                    dbName,
                    logWarn: msg => { if (!migrationOptions.Quiet) Console.WriteLine("WARNING: " + msg); });
            }

            SchemaSnapshot snapshot;
            if (exists)
            {
                var introspector = new MySqlSchemaIntrospector();
                snapshot = await introspector.LoadSchemaAsync(dbConn, cancellationToken: migrationOptions.CancelToken);
            }
            else
                snapshot = SchemaSnapshotFactory.Empty(dbName);

            Console.WriteLine(snapshot.DatabaseName);
            Console.WriteLine(snapshot.Tables.Count);
            
            var sb = new StringBuilder();

            sb.AppendLine($"-- Migration: {migrationName}");
            sb.AppendLine($"-- GeneratedUtc: {stampUtc}");
            sb.AppendLine($"-- ModelSet: {migrationOptions.SetName}");
            sb.AppendLine($"-- TargetDatabase: {dbName}");
            sb.AppendLine($"-- Tables in scope ({tables.Count}):");
            foreach (var t in tables)
                sb.AppendLine($"--   - {t.TableName}  ({t.ClrType.FullName})");

            sb.AppendLine();
            sb.AppendLine($"-- TODO:");
            sb.AppendLine($"--   Replace this placeholder with SQL produced by CoreRelm’s schema introspector + migration planner.");
            sb.AppendLine($"--   This file exists so the workflow and naming conventions are stable now.");
            sb.AppendLine();
            sb.AppendLine($"-- Use the target database explicitly (optional; depends on how you execute scripts)");
            sb.AppendLine($"USE `{dbName}`;");
            sb.AppendLine();

            // You can optionally add your uuid_v4 function guard as a placeholder too,
            // but leave it empty for now to avoid assumptions.
            sb.AppendLine($"-- TODO: create uuid_v4() if missing");
            sb.AppendLine($"-- TODO: create/alter tables");
            sb.AppendLine($"-- TODO: ensure InternalId UUIDv4 BEFORE INSERT triggers");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
