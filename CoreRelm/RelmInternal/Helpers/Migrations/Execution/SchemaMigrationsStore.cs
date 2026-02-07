using CoreRelm.Interfaces;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Execution;
using MySql.Data.MySqlClient;
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
        private const string migrationName = "[Relm] Ensure Schema Migrations Table";

        public async Task<int> EnsureTableAsync(IRelmContext context, MigrationOptions migrationOptions)
        {
            var databaseName = context.ContextOptions.DatabaseConnection!.Database;
            var provider = _providerFactory.CreateProvider(migrationOptions);
            var models = new List<ValidatedModelType>
            {
                new(typeof(AppliedMigration), databaseName, RelmHelper.GetDalTable<AppliedMigration>() ?? "schema_migrations")
            };

            var result = await provider.GenerateAsync(migrationOptions, migrationName, DateTime.UtcNow, databaseName, models);



            const string sql = @"CREATE TABLE IF NOT EXISTS `schema_migrations` (
              `id` BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,

              `file_name` VARCHAR(255) NOT NULL,
              `checksum_sha256` CHAR(64) NOT NULL,
              `applied_utc` DATETIME NOT NULL,

              `active` TINYINT(1) NOT NULL DEFAULT 1,
              `InternalId` VARCHAR(45) NOT NULL,
              `create_date` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
              `last_updated` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

              UNIQUE KEY `uq_schema_migrations_InternalId` (`InternalId`),
              UNIQUE KEY `uq_schema_migrations_file_name` (`file_name`)
            ) ENGINE=InnoDB;";

            var rowsUpdated = context.DoDatabaseWork<int>(sql);

            return rowsUpdated;
        }

        public async Task<Dictionary<string, AppliedMigration>> GetAppliedAsync(IRelmContext context, CancellationToken ct = default)
        {
            var result = new Dictionary<string, AppliedMigration>(StringComparer.Ordinal);

            var query = @"SELECT * FROM schema_migrations
                ORDER BY applied_utc;";

            var migrations = context.GetDataObjects<AppliedMigration>(query)
                ?.Where(x => x != null)
                .ToDictionary(x => x!.FileName, x => x);

            return result;
        }

        public async Task<int> RecordAppliedAsync(IRelmContext context, string migrationFile, string checksumSha256, CancellationToken ct = default)
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
