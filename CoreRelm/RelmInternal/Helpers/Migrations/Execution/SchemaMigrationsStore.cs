using BDL.Common.Logging.Extensions;
using BDL.Common.Logging.Models.StaticLogging;
using CoreRelm.Extensions;
using CoreRelm.Interfaces;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models;
using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    public sealed class SchemaMigrationsStore(
        IRelmMigrationSqlProviderFactory providerFactory,
        IRelmSqlScriptRunner runner,
        ILogger<SchemaMigrationsStore>? log = null) : IRelmSchemaMigrationsStore
    {
        private const string migrationName = "[Relm] Ensure Schema Migrations Table";

        public async Task<int> EnsureSchemaMigrationTableAsync(IRelmContext context, MigrationOptions migrationOptions)
        {
            log?.SaveIndentLevel("EnsureSchemaMigrationTableAsync");
            log?.LogFormatted(LogLevel.Information, "Ensuring schema migrations table exists for database: {Database}", args: [context.ContextOptions.DatabaseConnection?.Database]);

            if (string.IsNullOrWhiteSpace(context.ContextOptions.DatabaseConnection?.Database))
                throw new InvalidOperationException("Database connection information is missing or incomplete.");

            var databaseName = context.ContextOptions.DatabaseConnection!.Database;

            log?.LogFormatted(LogLevel.Information, "Generating migration SQL to ensure schema migrations table exists");
            var provider = providerFactory.CreateProvider(migrationOptions);

            log?.LogFormatted(LogLevel.Information, "Preparing model information for migrations table SQL generation");
            var models = new List<ValidatedModelType>
            {
                new(typeof(AppliedMigration), databaseName, RelmHelper.GetDalTable<AppliedMigration>() ?? "schema_migrations")
            };

            log?.LogFormatted(LogLevel.Information, "Invoking migration SQL provider to generate SQL for ensuring schema migrations table exists...");
            log?.SaveIndentLevel("GenerateAsync");
            var result = await provider.GenerateAsync(DateTime.UtcNow, databaseName, models);
            log?.RestoreIndentLevel("GenerateAsync");
            if (!result.HasChanges)
            {
                log?.LogFormatted(LogLevel.Information, "No schema changes detected for migrations table. Message: {Message}", args: [result.Message], singleIndentLine: true);
                return 0;
            }
            else
                log?.LogFormatted(LogLevel.Information, "Schema changes detected for migrations table. Message: {Message}", args: [result.Message], singleIndentLine: true);

            if (string.IsNullOrWhiteSpace(result.Sql))
                throw new InvalidOperationException("Migration SQL generation failed: no SQL returned.");

            var checksum = result.Sql.Sha256Hex();
            log?.LogFormatted(LogLevel.Information, "Generated migration SQL checksum (SHA-256): {Checksum}", args: [checksum], preIncreaseLevel: true);

            var safeName = UrlStringHelper.Slugify(migrationName);
            log?.LogFormatted(LogLevel.Information, "Generated safe migration name for file naming: {SafeName}", args: [safeName]);

            var migrationFileName = $"SYSTEM_MIGRATION_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{safeName}__db-{databaseName}.sql";
            log?.LogFormatted(LogLevel.Information, "Constructed migration file name: {FileName}", args: [migrationFileName]);

            var migrationFilePath = Path.Combine(migrationOptions.MigrationsPath, migrationFileName);
            if (migrationOptions.SaveSystemMigrations && !string.IsNullOrWhiteSpace(migrationOptions.MigrationsPath))
            {
                log?.LogFormatted(LogLevel.Information, "Saving generated migration SQL to file as per configuration. Path: {MigrationsPath}", args: [migrationOptions.MigrationsPath], singleIndentLine: true);
                if (!Directory.Exists(migrationOptions.MigrationsPath))
                    Directory.CreateDirectory(migrationOptions.MigrationsPath);

                await File.WriteAllTextAsync(migrationFilePath, result.Sql, migrationOptions.CancelToken);
            }

            log?.LogFormatted(LogLevel.Information, "Executing migration SQL to ensure schema migrations table exists...");
            await runner.ExecuteScriptAsync(context, result.Sql, migrationOptions.CancelToken, exceptionHandler: (ex) =>
            {
                log?.LogFormatted(LogLevel.Error, "Error executing migration SQL for ensuring schema migrations table exists: {ExceptionMessage}", args: [ex.Message], preIncreaseLevel: true);

                if (ex is DbException dbEx)
                    log?.LogFormatted(LogLevel.Error, "Database error details - Error Code: {ErrorCode}, SQL State: {SqlState}", args: [dbEx.ErrorCode, dbEx.SqlState]);

                var errorFileName = $"ERROR_{migrationFileName}";
                var errorFilePath = Path.Combine(migrationOptions.MigrationErrorPath, errorFileName);
                log?.LogFormatted(LogLevel.Information, "Saving failed migration SQL to error path for analysis. Path: {ErrorFilePath}", args: [errorFilePath]);

                if (File.Exists(migrationFilePath))
                {
                    log?.LogFormatted(LogLevel.Information, "Removing previous failed migration SQL file. Path: {MigrationFilePath}", args: [migrationFilePath]);
                    File.Delete(migrationFilePath);
                }

                if (!Directory.Exists(migrationOptions.MigrationErrorPath))
                {
                    log?.LogFormatted(LogLevel.Information, "Creating migration error directory as it does not exist. Path: {ErrorPath}", args: [migrationOptions.MigrationErrorPath]);
                    Directory.CreateDirectory(migrationOptions.MigrationErrorPath);
                }

                log?.LogFormatted(LogLevel.Information, "Saving migration file", args: []);
                File.WriteAllText(errorFilePath, result.Sql);

                throw ex;
            });

            log?.LogFormatted(LogLevel.Information, "Recording applied migration - '{Recording}'", args: ["Ensuring schema migrations table exists"]);
            var rowsUpdated = await RecordAppliedMigrationAsync(context, migrationFileName, checksum, migrationOptions.CancelToken);

            log?.LogFormatted(LogLevel.Information, "Recorded applied migration. Rows updated: {RowsUpdated}", args: [rowsUpdated]);

            log?.LogFormatted(LogLevel.Information, "Completed ensuring schema migrations table exists for database: {Database}", args: [databaseName], preDecreaseLevel: true);

            log?.RestoreIndentLevel("EnsureSchemaMigrationTableAsync");
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
