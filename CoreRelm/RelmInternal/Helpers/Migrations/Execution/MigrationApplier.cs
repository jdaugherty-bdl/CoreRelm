using CoreRelm.Extensions;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Interfaces.Migrations.Execution;
using CoreRelm.Models;
using CoreRelm.Models.Migrations;
using CoreRelm.Options;
using CoreRelm.RelmInternal.Contexts;
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
    internal sealed class MigrationApplier(
        IRelmSchemaMigrationsStore store,
        IRelmSqlScriptRunner runner,
        IRelmDatabaseProvisioner provisioner)
        : IMigrationApplier
    {
        public async Task<bool> ApplyAsync(
            MigrationOptions migrationOptions,
            string migrationFileName,
            string sql)
        {
            var ok = await provisioner.EnsureForApplyOrMigrateAsync(
                migrationOptions,
                logInfo: (msg, args) => { if (!migrationOptions.Quiet) Console.WriteLine(msg); },
                logWarn: (msg, args) => Console.WriteLine("ERROR: " + msg));

            if (!ok) return false;

            var dbConn = migrationOptions.ConnectionStringTemplate?.Replace("{db}", migrationOptions.DatabaseName, StringComparison.Ordinal)
                ?? throw new InvalidOperationException("Database connection string template is not set.");

            var context = new RelmContextOptionsBuilder(dbConn)
                .SetAutoInitializeDataSets(false)
                .SetAutoVerifyTables(false) // turn off auto-verify because we may be creating tables here
                .Build<RelmInternalAppliedMigrationContext>() 
                ?? 
                throw new InvalidOperationException("Failed to build RelmContext for migration application.");

            var checksum = sql.Sha256Hex();

            await store.EnsureSchemaMigrationTableAsync(context, migrationOptions);

            // Drift safety: if a migration file name was already applied, do not reapply
            var applied = await store.GetAppliedMigrationsAsync(context, migrationOptions.CancelToken);
            if (applied?.ContainsKey(migrationFileName) ?? false)
            {
                if (!migrationOptions.Quiet)
                    Console.WriteLine($"Already applied on `{migrationOptions.DatabaseName}`: {migrationFileName}");
                return true;
            }

            try
            {
                await runner.ExecuteScriptAsync(context, sql, migrationOptions.CancelToken);
                await store.RecordAppliedMigrationAsync(context, migrationFileName, RelmMigrationType.Migration, checksum, migrationOptions.CancelToken);

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
