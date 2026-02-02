using CoreRelm.Interfaces;
using CoreRelm.Models.Migrations.Execution;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Migrations.Execution
{
    public sealed class SchemaMigrationsStore
    {
        public async Task<int> EnsureTableAsync(IRelmContext context, CancellationToken ct = default)
        {
            const string sql = @"CREATE TABLE IF NOT EXISTS `SchemaMigrations` (
              `id` BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,

              `migration_file` VARCHAR(255) NOT NULL,
              `checksum_sha256` CHAR(64) NOT NULL,
              `applied_utc` DATETIME NOT NULL,

              `active` TINYINT(1) NOT NULL DEFAULT 1,
              `InternalId` VARCHAR(45) NOT NULL,
              `create_date` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
              `last_updated` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

              UNIQUE KEY `uq_SchemaMigrations_InternalId` (`InternalId`),
              UNIQUE KEY `uq_SchemaMigrations_migration_file` (`migration_file`)
            ) ENGINE=InnoDB;";
            /*
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync(ct);
            */

            var rowsUpdated = context.DoDatabaseWork<int>(sql);

            return rowsUpdated;
        }

        public async Task<Dictionary<string, AppliedMigration>> GetAppliedAsync(IRelmContext context, CancellationToken ct = default)
        {
            var result = new Dictionary<string, AppliedMigration>(StringComparer.Ordinal);

            /*
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT migration_file, checksum_sha256, applied_utc
                FROM SchemaMigrations
                ORDER BY applied_utc;";

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var file = reader.GetString(0);
                var checksum = reader.GetString(1);
                var appliedUtc = reader.GetDateTime(2);

                result[file] = new AppliedMigration(file, checksum, appliedUtc);
            }
            */
            var query = @"SELECT migration_file, checksum_sha256, applied_utc
                FROM SchemaMigrations
                ORDER BY applied_utc;";

            var migrations = context.GetDataObjects<AppliedMigration>(query)
                .ToDictionary(x => x.FileName, x => x);

            return result;
        }

        public async Task<int> RecordAppliedAsync(IRelmContext context, string migrationFile, string checksumSha256, CancellationToken ct = default)
        {
            /*
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO SchemaMigrations (InternalId, migration_file, checksum_sha256, applied_utc)
                VALUES (@internalId, @file, @checksum, UTC_TIMESTAMP());";
            cmd.Parameters.AddWithValue("@internalId", Guid.NewGuid().ToString()); // Internal tool; OK for bootstrap
            cmd.Parameters.AddWithValue("@file", migrationFile);
            cmd.Parameters.AddWithValue("@checksum", checksumSha256);

            await cmd.ExecuteNonQueryAsync(ct);
            */
            var appliedMigration = new AppliedMigration(
                fileName: migrationFile,
                checksumSha256: checksumSha256,
                appliedUtc: DateTime.UtcNow);

            var rowsUpdated = appliedMigration.WriteToDatabase(context);

            return rowsUpdated;
        }
    }
}
