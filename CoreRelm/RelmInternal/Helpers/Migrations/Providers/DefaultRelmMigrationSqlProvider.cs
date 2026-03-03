using BDL.Common.Logging.Extensions;
using CoreRelm.Interfaces.Metadata;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Migrations.Contexts;
using CoreRelm.Models;
using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using CoreRelm.Models.Migrations.MigrationPlans;
using CoreRelm.Models.Migrations.Rendering;
using CoreRelm.Options;
using CoreRelm.RelmInternal.Helpers.Migrations.Introspection;
using CoreRelm.RelmInternal.Helpers.Migrations.MigrationPlans;
using CoreRelm.RelmInternal.Helpers.Migrations.Provisioning;
using CoreRelm.RelmInternal.Models.Migrations.MigrationPlans;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Migrations.Providers
{
    internal sealed class DefaultRelmMigrationSqlProvider(
            IRelmSchemaIntrospector introspector,
            IRelmMigrationPlanner planner,
            IRelmMigrationSqlRenderer renderer,
            IRelmDatabaseProvisioner provisioner,
            IRelmDesiredSchemaBuilder desiredBuilder,
            MigrationOptions migrationOptions,
            ILogger<IMigrationSqlProvider>? log = null) : IMigrationSqlProvider
    {
        public MigrationGenerateResult Generate(List<ValidatedModelType> modelsForDb, string migrationFileName)
        {
            // This provider is intended to be used in an async flow,
            // but IMigrationSqlProvider is sync. We keep it sync by calling .GetAwaiter().GetResult().
            // If you prefer, change the provider interface to async later.

            return GenerateAsync(modelsForDb, migrationFileName)
                .GetAwaiter()
                .GetResult();
        }

        public async Task<MigrationGenerateResult> GenerateAsync(List<ValidatedModelType> modelsForDb, string migrationFileName)
        {
            // This provider is intended to be used in an async flow,
            // but IMigrationSqlProvider is sync. We keep it sync by calling .GetAwaiter().GetResult().
            // If you prefer, change the provider interface to async later.
            var artifact = await GenerateArtifactAsync(modelsForDb, migrationFileName);

            return new MigrationGenerateResult(artifact.Plan.DatabaseName, artifact.HasChanges, artifact.Sql, artifact.Message);
        }

        internal async Task<GeneratedMigrationArtifact> GenerateArtifactAsync(List<ValidatedModelType> modelsForDb, string migrationFileName)
        {
            log?.SaveIndentLevel("DefaultRelmMigrationSqlProvider.GenerateAsync");

            log?.LogFormatted(LogLevel.Information, "Generating migration SQL", args: [], preIncreaseLevel: true);

            // Determine scope tables (table names derived from model metadata already resolved by your resolver)
            var scopeTables = modelsForDb
                .Select(m => m.TableName)
                .Distinct(StringComparer.Ordinal)
                .ToHashSet(StringComparer.Ordinal);
            log?.LogFormatted(LogLevel.Information, "Scope tables for migration: {Count}", args: [scopeTables.Count], singleIndentLine: true);

            // Desired schema is built from your CoreRelm metadata pipeline.
            // For now, we build it from the resolved model list (table names + columns etc. must come from CoreRelm metadata reader).
            // If you already have a DesiredSnapshot builder in CoreRelm, call that instead.
            log?.SaveIndentLevel("desired");
            var desired = await desiredBuilder.BuildAsync(migrationOptions.DatabaseName, modelsForDb);
            log?.RestoreIndentLevel("desired");

            // Actual schema:
            // - if DB exists, introspect it
            // - otherwise warn and treat as empty (per your policy for non-apply paths)
            SchemaSnapshot actual;
            log?.LogFormatted(LogLevel.Information, "Checking database availability...", args: [], postIncreaseLevel: true);
            //_migrationOptions.DatabaseName = dbName;
            log?.SaveIndentLevel("availability");
            var dbExists = await provisioner.WarnIfMissingAsync(migrationOptions, message => log?.LogWarning(message));
            log?.RestoreIndentLevel("availability");

            if (dbExists)
            {
                var dbConn = migrationOptions.ConnectionStringTemplate?.Replace("{db}", "INFORMATION_SCHEMA", StringComparison.Ordinal);
                if (string.IsNullOrWhiteSpace(dbConn))
                    throw new ArgumentException("Connection string is required.", nameof(dbConn));

                var context = new RelmContextOptionsBuilder(dbConn)
                    .SetAutoInitializeDataSets(false)
                    .SetAutoVerifyTables(false)
                    .Build<InformationSchemaContext>();

                log?.SaveIndentLevel("introspecting");
                actual = await introspector.LoadSchemaAsync(context, new SchemaIntrospectionOptions { DatabaseName = migrationOptions.DatabaseName });
                log?.RestoreIndentLevel("introspecting");
            }
            else
                actual = SchemaSnapshotFactory.Empty(migrationOptions.DatabaseName);

            log?.LogFormatted(LogLevel.Information, "Planning migration...", args: []);
            var planOptions = new MigrationPlanOptions(
                DropFunctionsOnCreate: migrationOptions.DropFunctionsOnCreate,
                Destructive: migrationOptions.Destructive,
                MigrationName: $"Migration_{DateTime.UtcNow:yyyyMMdd_HHmmss}",
                MigrationFileName: migrationFileName,
                ModelSetName: "Default", // You can customize this if you have multiple model sets or want to include more context
                ScopeTables: scopeTables,
                StampUtc: migrationOptions.StampUtc
            );

            log?.SaveIndentLevel("planning");
            var plan = planner.Plan(desired, actual, planOptions);
            log?.RestoreIndentLevel("planning");

            if (plan.Operations.Count == 0 && plan.Blockers.Count == 0)
            {
                var noChangeResult = MigrationGenerateResult.NoChanges(
                    migrationOptions.DatabaseName,
                    $"No changes detected for database '{migrationOptions.DatabaseName}'. No migration file written.");

                return new GeneratedMigrationArtifact(plan, noChangeResult.Sql, noChangeResult.HasChanges, noChangeResult.Message);
            }

            log?.SaveIndentLevel("rendering");
            var sql = renderer.Render(plan, new MySqlRenderOptions(
                IncludeUseDatabase: true,
                WrapTriggersWithDelimiter: true,
                TriggerDelimiter: "$$",
                FunctionDelimiter: "$$"
            ));
            log?.RestoreIndentLevel("rendering");

            log?.LogFormatted(LogLevel.Information, "Migration SQL generated with {OperationCount} operations and {BlockerCount} blockers.", args: [plan.Operations.Count, plan.Blockers.Count]);

            log?.RestoreIndentLevel("DefaultRelmMigrationSqlProvider.GenerateAsync");

            var changeResult = MigrationGenerateResult.Changes(
                migrationOptions.DatabaseName,
                sql,
                $"Changes detected for database '{migrationOptions.DatabaseName}'. Migration will be generated.");

            return new GeneratedMigrationArtifact(plan, changeResult.Sql, changeResult.HasChanges, changeResult.Message);
        }
    }
}
