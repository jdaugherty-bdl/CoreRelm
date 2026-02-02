using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models.Migrations;
using CoreRelm.RelmInternal.Helpers.Migrations.MigrationPlans;
using CoreRelm.RelmInternal.Helpers.Migrations.Provisioning;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Migrations
{
    public sealed class DefaultRelmMigrationSqlProvider : IMigrationSqlProvider
    {
        public async Task<MigrationGenerateResult> Generate(string migrationName, string stampUtc, string setName, string dbName, List<ValidatedModelType> modelsForDb, bool destructive, CancellationToken cancellationToken, bool quiet, bool doApply, MySqlDatabaseProvisioner provisioner)
        {
            var tables = modelsForDb
                .OrderBy(m => m.TableName, StringComparer.Ordinal)
                .ThenBy(m => m.ClrType.FullName, StringComparer.Ordinal)
                .ToList();

            var sql = await MigrationSqlGeneratorHelper.BuildPlaceholderMigrationSql(migrationName, stampUtc, setName, dbName, tables, cancellationToken, provisioner, quiet: quiet, doApply: doApply);

            // In the real version, if plan has no operations, return NoChanges and sql would be null.
            // For placeholder, treat as changes.
            if (string.IsNullOrWhiteSpace(sql))
                return MigrationGenerateResult.NoChanges(dbName, $"[Relm] No changes detected for database '{dbName}'. No migration file written.");

            return MigrationGenerateResult.Changes(
                dbName,
                sql,
                $"[Relm] Migration SQL generated for database '{dbName}' (diff not computed yet).");
        }
    }
}
