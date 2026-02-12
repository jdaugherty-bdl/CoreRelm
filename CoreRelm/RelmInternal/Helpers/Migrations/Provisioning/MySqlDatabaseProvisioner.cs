using BDL.Common.Logging.Extensions;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models;
using CoreRelm.Models.Migrations;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Migrations.Provisioning
{

    public sealed class MySqlDatabaseProvisioner(ILogger<MySqlDatabaseProvisioner>? log = null) : IRelmDatabaseProvisioner
    {
        private ILogger<MySqlDatabaseProvisioner>? _log = log;
        private static string EscapeIdentifier(string s) => s.Replace("`", "``", StringComparison.Ordinal);

        public async Task<bool> DatabaseExistsAsync(MigrationOptions migrationOptions, string databaseName)
        {
            _log?.LogFormatted(LogLevel.Information, "Checking if database '{DatabaseName}' exists.", args: [databaseName], preIncreaseLevel: true);
            _log?.LogFormatted(LogLevel.Debug, "MySqlDatabaseProvisioner.DatabaseExistsAsync: Checking existence of database '{DatabaseName}' using connection string template '{ConnectionStringTemplate}'.", args: [databaseName, migrationOptions.ConnectionStringTemplate]);

            if (string.IsNullOrWhiteSpace(migrationOptions.ConnectionStringTemplate))
            {
                var templateError = new ArgumentException("Server connection string template is required.", nameof(migrationOptions.ConnectionStringTemplate));
                _log?.LogFormatted(LogLevel.Error, "MySqlDatabaseProvisioner.DatabaseExistsAsync: {Message}", args: [templateError.Message], exception: templateError);
                throw templateError;
            }

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                var databaseNameError = new ArgumentException("Database name is required.", nameof(databaseName));
                _log?.LogFormatted(LogLevel.Error, "MySqlDatabaseProvisioner.DatabaseExistsAsync: {Message}", args: [databaseNameError.Message], exception: databaseNameError);
                throw databaseNameError;
            }

            // use straight MySqlConnection instead of a RelmContext because we may be creating the database itself
            _log?.LogFormatted(LogLevel.Information, "Connecting to database server.");
            var dbConnectionString = new MySqlConnectionStringBuilder(migrationOptions.ConnectionStringTemplate)
            {
                Database = null // ensure we're connecting to the server, not a specific database
            }.ToString();
            _log?.LogFormatted(LogLevel.Information, "Constructed server connection string '{DbConnectionString}'.", args: [dbConnectionString], preIncreaseLevel: true);

            await using var conn = new MySqlConnection(dbConnectionString);
            await conn.OpenAsync(migrationOptions.CancelToken);
            _log?.LogFormatted(LogLevel.Information, "Connected to MySQL server.", postDecreaseLevel: true);

            _log?.LogFormatted(LogLevel.Information, "Querying INFORMATION_SCHEMA.SCHEMATA for database '{DatabaseName}' existence.", args: [databaseName]);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT 1
                FROM INFORMATION_SCHEMA.SCHEMATA
                WHERE SCHEMA_NAME = @database_name
                LIMIT 1;";
            cmd.Parameters.AddWithValue("@database_name", databaseName);

            var result = await cmd.ExecuteScalarAsync(migrationOptions.CancelToken);
            _log?.LogFormatted(LogLevel.Information, "Query executed. Database '{DatabaseName}' existence: {Exists}.", args: [databaseName, result is not null], singleIndentLine: true, postDecreaseLevel: true);

            return result is not null;
        }

        public async Task InitializeEmptyDatabaseAsync(
            MigrationOptions migrationOptions,
            string databaseName,
            string? charset = null,
            string? collation = null)
        {
            _log?.LogFormatted(LogLevel.Information, "Initializing empty database '{DatabaseName}' with charset '{Charset}' and collation '{Collation}'.", args: [databaseName, charset ?? "default", collation ?? "default"], preIncreaseLevel: true);
            _log?.LogFormatted(LogLevel.Debug, "MySqlDatabaseProvisioner.InitializeEmptyDatabaseAsync: Initializing database '{DatabaseName}' using connection string template '{ConnectionStringTemplate}'.", args: [databaseName, migrationOptions.ConnectionStringTemplate]);

            if (string.IsNullOrWhiteSpace(migrationOptions.ConnectionStringTemplate))
            {
                var templateError = new ArgumentException("Server connection string template is required.", nameof(migrationOptions.ConnectionStringTemplate));
                _log?.LogFormatted(LogLevel.Error, "MySqlDatabaseProvisioner.InitializeEmptyDatabaseAsync: {Message}", args: [templateError.Message], exception: templateError);
                throw templateError;
            }

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                var databaseNameError = new ArgumentException("Database name is required.", nameof(databaseName));
                _log?.LogFormatted(LogLevel.Error, "MySqlDatabaseProvisioner.InitializeEmptyDatabaseAsync: {Message}", args: [databaseNameError.Message], exception: databaseNameError);
                throw databaseNameError;
            }

            // use straight MySqlConnection instead of a RelmContext because we may be creating the database itself
            _log?.LogFormatted(LogLevel.Information, "Connecting to database server.");
            var dbConnectionString = new MySqlConnectionStringBuilder(migrationOptions.ConnectionStringTemplate)
            {
                Database = null // ensure we're connecting to the server, not a specific database
            }.ToString();
            _log?.LogFormatted(LogLevel.Information, "Constructed server connection string '{DbConnectionString}'.", args: [dbConnectionString], preIncreaseLevel: true);

            await using var conn = new MySqlConnection(dbConnectionString);
            await conn.OpenAsync(migrationOptions.CancelToken);
            _log?.LogFormatted(LogLevel.Information, "Connected to MySQL server.", postDecreaseLevel: true);

            _log?.LogFormatted(LogLevel.Information, "Creating database '{DatabaseName}' if it does not exist.", args: [databaseName]);
            var sql = $"CREATE DATABASE IF NOT EXISTS `{EscapeIdentifier(databaseName)}`";
            if (!string.IsNullOrWhiteSpace(charset))
                sql += $" CHARACTER SET {charset}";
            if (!string.IsNullOrWhiteSpace(collation))
                sql += $" COLLATE {collation}";
            sql += ";";

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync(migrationOptions.CancelToken);
            _log?.LogFormatted(LogLevel.Information, "Database '{DatabaseName}' initialized (created if it did not exist).", args: [databaseName], singleIndentLine: true, postDecreaseLevel: true);
        }
    }
}
