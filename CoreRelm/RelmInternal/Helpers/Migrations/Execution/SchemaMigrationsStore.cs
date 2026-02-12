using CoreRelm.Extensions;
using CoreRelm.Interfaces;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models;
using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Execution;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CoreRelm.RelmInternal.Helpers.Migrations.Execution
{
    public sealed class SchemaMigrationsStore(IRelmMigrationSqlProviderFactory providerFactory) : IRelmSchemaMigrationsStore
    {
        private readonly IRelmMigrationSqlProviderFactory _providerFactory = providerFactory;
        private readonly MySqlScriptRunner _runner = new();
        private const string migrationName = "[Relm] Ensure Schema Migrations Table";

        public async Task<int> EnsureSchemaMigrationTableAsync(IRelmContext context, MigrationOptions migrationOptions)
        {
            var databaseName = context.ContextOptions.DatabaseConnection!.Database;
            var provider = _providerFactory.CreateProvider(migrationOptions);
            var models = new List<ValidatedModelType>
            {
                new(typeof(AppliedMigration), databaseName, RelmHelper.GetDalTable<AppliedMigration>() ?? "schema_migrations")
            };

            var result = await provider.GenerateAsync(DateTime.UtcNow, databaseName, models);
            if (!result.HasChanges)
                return 0;

            if (string.IsNullOrWhiteSpace(result.Sql))
                throw new InvalidOperationException("Migration SQL generation failed: no SQL returned.");

            var checksum = result.Sql.Sha256Hex();
            var safeName = UrlStringHelper.Slugify(migrationName);
            var migrationFileName = $"SYSTEM_MIGRATION_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{safeName}__db-{databaseName}.sql";

            if (migrationOptions.SaveSystemMigrations && !string.IsNullOrWhiteSpace(migrationOptions.MigrationsPath))
            {
                if (!Directory.Exists(migrationOptions.MigrationsPath))
                    Directory.CreateDirectory(migrationOptions.MigrationsPath);

                await File.WriteAllTextAsync(Path.Combine(migrationOptions.MigrationsPath, migrationFileName), result.Sql, migrationOptions.CancelToken);
            }

            await _runner.ExecuteScriptAsync(context, result.Sql, migrationOptions.CancelToken);
            var rowsUpdated = await RecordAppliedMigrationAsync(context, migrationFileName, checksum, migrationOptions.CancelToken);

            return rowsUpdated;
        }

        public async Task<Dictionary<string, AppliedMigration>?> GetAppliedMigrationsAsync(RelmContext context, CancellationToken cancellationToken = default)
        {
            var migrationDataset = context.GetDataSet<AppliedMigration>();
            if (migrationDataset == null)
                return null;

            var orderedMigrations = (await migrationDataset
                .Where(x => x.Active == true)
                .OrderBy(x => x.AppliedUtc)
                .LoadAsync(cancellationToken))
                ?.Where(x => x != null)
                .Select(x => x!)
                .ToDictionary(x => x.FileName, x => x);

            return orderedMigrations;
        }

        public async Task<int> RecordAppliedMigrationAsync(IRelmContext context, string migrationFile, string checksumSha256, CancellationToken ct = default)
        {
            var appliedMigration = new AppliedMigration(
                fileName: migrationFile,
                checksumSha256: checksumSha256,
                appliedUtc: DateTime.UtcNow);

            var rowsUpdated = appliedMigration.WriteToDatabase(context);

            return rowsUpdated;
        }
    }
}
