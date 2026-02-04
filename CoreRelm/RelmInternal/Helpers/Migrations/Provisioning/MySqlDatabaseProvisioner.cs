using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models.Migrations;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Migrations.Provisioning
{

    public sealed class MySqlDatabaseProvisioner : IRelmDatabaseProvisioner
    {
        private static string EscapeIdentifier(string s) => s.Replace("`", "``", StringComparison.Ordinal);

        public async Task<bool> DatabaseExistsAsync(MigrationOptions migrationOptions, string databaseName)
        {
            if (string.IsNullOrWhiteSpace(migrationOptions.ConnectionString))
                throw new ArgumentException("Server connection string is required.", nameof(migrationOptions.ConnectionString));
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name is required.", nameof(databaseName));

            await using var conn = new MySqlConnection(migrationOptions.ConnectionString);
            await conn.OpenAsync(migrationOptions.CancelToken);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT 1
                FROM INFORMATION_SCHEMA.SCHEMATA
                WHERE SCHEMA_NAME = @db
                LIMIT 1;";
            cmd.Parameters.AddWithValue("@db", databaseName);

            var result = await cmd.ExecuteScalarAsync(migrationOptions.CancelToken);
            return result is not null;
        }

        public async Task EnsureDatabaseExistsAsync(
            MigrationOptions migrationOptions,
            string databaseName,
            string? charset = null,
            string? collation = null)
        {
            if (string.IsNullOrWhiteSpace(migrationOptions.ConnectionString))
                throw new ArgumentException("Server connection string is required.", nameof(migrationOptions.ConnectionString));
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name is required.", nameof(databaseName));

            // use straight MySqlConnection instead of a RelmContext because we may be creating the database itself
            await using var conn = new MySqlConnection(migrationOptions.ConnectionString);
            await conn.OpenAsync(migrationOptions.CancelToken);

            var sql = $"CREATE DATABASE IF NOT EXISTS `{EscapeIdentifier(databaseName)}`";
            if (!string.IsNullOrWhiteSpace(charset))
                sql += $" CHARACTER SET {charset}";
            if (!string.IsNullOrWhiteSpace(collation))
                sql += $" COLLATE {collation}";
            sql += ";";

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync(migrationOptions.CancelToken);
        }
    }
}
