using BDL.Common.Logging.Extensions;
using CoreRelm.Interfaces.Metadata;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models;
using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using CoreRelm.Models.Migrations.MigrationPlans;
using CoreRelm.Models.Migrations.Rendering;
using CoreRelm.RelmInternal.Contexts;
using CoreRelm.RelmInternal.Helpers.Migrations.Introspection;
using CoreRelm.RelmInternal.Helpers.Migrations.MigrationPlans;
using CoreRelm.RelmInternal.Helpers.Migrations.Provisioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Migrations
{
    internal sealed class DefaultRelmMigrationSqlProvider : IMigrationSqlProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IRelmSchemaIntrospector _introspector;
        private readonly IRelmMigrationPlanner _planner;
        private readonly IRelmMigrationSqlRenderer _renderer;
        private readonly MySqlDatabaseProvisioner _provisioner;
        private readonly IRelmDesiredSchemaBuilder _desiredBuilder;
        private readonly MigrationOptions _migrationOptions;
        private ILogger<MySqlDatabaseProvisioner>? _log;

        internal DefaultRelmMigrationSqlProvider(
            IServiceProvider serviceProvider,
            IRelmSchemaIntrospector introspector,
            IRelmMigrationPlanner planner,
            IRelmMigrationSqlRenderer renderer,
            MySqlDatabaseProvisioner provisioner,
            IRelmDesiredSchemaBuilder desiredBuilder,
            MigrationOptions migrationOptions,
            ILogger<MySqlDatabaseProvisioner>? log)
        {
            _serviceProvider = serviceProvider;
            _introspector = introspector;
            _planner = planner;
            _renderer = renderer;
            _provisioner = provisioner;
            _desiredBuilder = desiredBuilder;
            _migrationOptions = migrationOptions;
            _log = log;
        }

        public MigrationGenerateResult Generate(
            DateTime stampUtc,
            string dbName,
            List<ValidatedModelType> modelsForDb)
        {
            // This provider is intended to be used in an async flow,
            // but IMigrationSqlProvider is sync. We keep it sync by calling .GetAwaiter().GetResult().
            // If you prefer, change the provider interface to async later.

            return GenerateAsync(stampUtc, dbName, modelsForDb)
                .GetAwaiter()
                .GetResult();
        }

        public async Task<MigrationGenerateResult> GenerateAsync(
            DateTime stampUtc,
            string dbName,
            List<ValidatedModelType> modelsForDb)
        {
            _log?.LogFormatted(LogLevel.Information, "Generating migration SQL", args: [], preIncreaseLevel: true);

            // Determine scope tables (table names derived from model metadata already resolved by your resolver)
            var scopeTables = modelsForDb
                .Select(m => m.TableName)
                .Distinct(StringComparer.Ordinal)
                .ToHashSet(StringComparer.Ordinal);
            _log?.LogFormatted(LogLevel.Information, "Scope tables for migration: {Count}", args: [scopeTables.Count], singleIndentLine: true);

            // Desired schema is built from your CoreRelm metadata pipeline.
            // For now, we build it from the resolved model list (table names + columns etc. must come from CoreRelm metadata reader).
            // If you already have a DesiredSnapshot builder in CoreRelm, call that instead.
            var desired = await _desiredBuilder.BuildAsync(dbName, modelsForDb);

            // Actual schema:
            // - if DB exists, introspect it
            // - otherwise warn and treat as empty (per your policy for non-apply paths)
            SchemaSnapshot actual;
            _log?.LogFormatted(LogLevel.Information, "Checking database availability...", args: []);
            //var dbExists = await _provisioner.DatabaseExistsAsync(_migrationOptions, dbName);
            _log?.IncreaseIndent();
            var dbExists = await DbAvailabilityHelper.WarnIfMissingAsync(_migrationOptions, _provisioner, dbName, message => _log?.LogWarning(message));
            _log?.DecreaseIndent();

            if (!dbExists)
                actual = SchemaSnapshotFactory.Empty(dbName);
            else
            {
                var dbConn = _migrationOptions.ConnectionStringTemplate?.Replace("{db}", dbName, StringComparison.Ordinal);
                if (string.IsNullOrWhiteSpace(dbConn))
                    throw new ArgumentException("Connection string is required.", nameof(dbConn));

                var context = new InformationSchemaContext(dbConn);
                actual = await _introspector.LoadSchemaAsync(context);
            }

            _log?.LogFormatted(LogLevel.Information, "Planning migration...", args: []);
            var planOptions = new MigrationPlanOptions(
                Destructive: _migrationOptions.Destructive,
                ScopeTables: scopeTables,
                StampUtc: stampUtc
            );

            var plan = _planner.Plan(desired, actual, planOptions);

            if (plan.Operations.Count == 0 && plan.Blockers.Count == 0)
            {
                return MigrationGenerateResult.NoChanges(
                    dbName,
                    $"No changes detected for database '{dbName}'. No migration file written.");
            }

            var sql = _renderer.Render(plan, new MySqlRenderOptions(
                IncludeUseDatabase: true,
                WrapTriggersWithDelimiter: true,
                TriggerDelimiter: "$$",
                FunctionDelimiter: "$$"
            ));

            return MigrationGenerateResult.Changes(
                dbName,
                sql,
                $"Changes detected for database '{dbName}'. Migration will be generated.");
        }
    }
}
