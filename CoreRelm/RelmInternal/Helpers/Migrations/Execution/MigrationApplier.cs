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

namespace CoreRelm.RelmInternal.Helpers.Migrations.Execution
{
    public sealed class MigrationApplier(IRelmMigrationSqlProviderFactory providerFactory)
    {
        private readonly MySqlDatabaseProvisioner _provisioner = new();
        private readonly SchemaMigrationsStore _store = new(providerFactory);
        private readonly MySqlScriptRunner _runner = new();

        public async Task<bool> ApplyAsync(
            MigrationOptions migrationOptions,
            string dbName,
            string migrationFileName,
            string sql)
        {
            var ok = await DbAvailabilityHelper.EnsureForApplyOrMigrateAsync(
                migrationOptions,
                _provisioner,
                dbName,
                logInfo: msg => { if (!migrationOptions.Quiet) Console.WriteLine(msg); },
                logWarn: msg => Console.WriteLine("ERROR: " + msg));

            if (!ok) return false;

            var dbConn = migrationOptions.ConnectionStringTemplate?.Replace("{db}", dbName, StringComparison.Ordinal)
                ?? throw new InvalidOperationException("Database connection string template is not set.");

            var context = new RelmContext(dbConn, autoInitializeDataSets: false, autoVerifyTables: false); // turn off auto-verify because we may be creating tables here

            var checksum = sql.Sha256Hex();

            await _store.EnsureSchemaMigrationTableAsync(context, migrationOptions);

            // Drift safety: if a migration file name was already applied, do not reapply
            var applied = await _store.GetAppliedMigrationsAsync(context, migrationOptions.CancelToken);
            if (applied?.ContainsKey(migrationFileName) ?? false)
            {
                if (!migrationOptions.Quiet)
                    Console.WriteLine($"Already applied on `{dbName}`: {migrationFileName}");
                return true;
            }

            try
            {
                await _runner.ExecuteScriptAsync(context, sql, migrationOptions.CancelToken);
                await _store.RecordAppliedMigrationAsync(context, migrationFileName, checksum, migrationOptions.CancelToken);

                if (!migrationOptions.Quiet)
                    Console.WriteLine($"Applied and recorded on `{dbName}`: {migrationFileName}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR applying {migrationFileName} on `{dbName}`: {ex.Message}");
                return false;
            }
        }
    }
}
