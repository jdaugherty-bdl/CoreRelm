using CoreRelm.Extensions;
using CoreRelm.Models;
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

    public sealed class MigrationApplier
    {
        private readonly MySqlDatabaseProvisioner _provisioner = new();
        private readonly SchemaMigrationsStore _store = new();
        private readonly MySqlScriptRunner _runner = new();

        public async Task<bool> ApplyAsync(
            string serverConn,
            string dbTemplate,
            string dbName,
            string migrationFileName,
            string sql,
            bool quiet,
            CancellationToken ct)
        {
            var ok = await DbAvailabilityHelper.EnsureForApplyOrMigrateAsync(
                _provisioner,
                serverConn,
                dbName,
                logInfo: msg => { if (!quiet) Console.WriteLine(msg); },
                logWarn: msg => Console.WriteLine("ERROR: " + msg),
                ct: ct);

            if (!ok) return false;

            var dbConn = dbTemplate.Replace("{db}", dbName, StringComparison.Ordinal);

            /*
            await using var conn = new MySqlConnection(dbConn);
            await conn.OpenAsync(ct);
            */
            var context = new RelmContext(dbConn);

            await _store.EnsureTableAsync(context, ct);

            // Drift safety: if a migration file name was already applied, do not reapply
            var applied = await _store.GetAppliedAsync(context, ct);
            if (applied.ContainsKey(migrationFileName))
            {
                if (!quiet)
                    Console.WriteLine($"Already applied on `{dbName}`: {migrationFileName}");
                return true;
            }

            var checksum = sql.Sha256Hex();

            try
            {
                await _runner.ExecuteScriptAsync(context, sql, ct);
                await _store.RecordAppliedAsync(context, migrationFileName, checksum, ct);

                if (!quiet)
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
