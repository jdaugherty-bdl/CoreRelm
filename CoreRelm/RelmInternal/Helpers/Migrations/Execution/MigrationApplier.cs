using CoreRelm.Extensions;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models;
using CoreRelm.Models.Migrations;
using CoreRelm.RelmInternal.Helpers.Migrations.Introspection;
using CoreRelm.RelmInternal.Helpers.Migrations.Provisioning;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.MigrationEnums;

namespace CoreRelm.RelmInternal.Helpers.Migrations.Execution
{
    public sealed class MigrationApplier(
        IRelmSchemaMigrationsStore store,
        IRelmSqlScriptRunner runner,
        IRelmDatabaseProvisioner provisioner)
    {
        private readonly IRelmDatabaseProvisioner _provisioner = provisioner;
        private readonly IRelmSchemaMigrationsStore _store = store;
        private readonly IRelmSqlScriptRunner _runner = runner;

        public async Task<bool> ApplyAsync(
            MigrationOptions migrationOptions,
            string migrationFileName,
            string sql)
        {
            var ok = await DbAvailabilityHelper.EnsureForApplyOrMigrateAsync(
                migrationOptions,
                _provisioner,
                logInfo: (msg, args) => { if (!migrationOptions.Quiet) Console.WriteLine(msg); },
                logWarn: (msg, args) => Console.WriteLine("ERROR: " + msg));

            if (!ok) return false;

            var dbConn = migrationOptions.ConnectionStringTemplate?.Replace("{db}", migrationOptions.DatabaseName, StringComparison.Ordinal)
                ?? throw new InvalidOperationException("Database connection string template is not set.");

            var context = new RelmContext(dbConn, autoInitializeDataSets: false, autoVerifyTables: false); // turn off auto-verify because we may be creating tables here

            var checksum = sql.Sha256Hex();

            await _store.EnsureSchemaMigrationTableAsync(context, migrationOptions);

            // Drift safety: if a migration file name was already applied, do not reapply
            var applied = await _store.GetAppliedMigrationsAsync(context, migrationOptions.CancelToken);
            if (applied?.ContainsKey(migrationFileName) ?? false)
            {
                if (!migrationOptions.Quiet)
                    Console.WriteLine($"Already applied on `{migrationOptions.DatabaseName}`: {migrationFileName}");
                return true;
            }

            try
            {
                await _runner.ExecuteScriptAsync(context, sql, migrationOptions.CancelToken);
                await _store.RecordAppliedMigrationAsync(context, migrationFileName, RelmMigrationType.Migration, checksum, migrationOptions.CancelToken);

                if (!migrationOptions.Quiet)
                    Console.WriteLine($"Applied and recorded on `{migrationOptions.DatabaseName}`: {migrationFileName}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR applying {migrationFileName} on `{migrationOptions.DatabaseName}`: {ex.Message}");
                return false;
            }
        }
    }
}
