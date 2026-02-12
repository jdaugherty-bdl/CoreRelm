using CoreRelm.Interfaces.Metadata;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models;
using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using CoreRelm.Models.Migrations.MigrationPlans;
using CoreRelm.Models.Migrations.Rendering;
using CoreRelm.RelmInternal.Helpers.Migrations.Introspection;
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
        private readonly IRelmSchemaIntrospector _introspector;
        private readonly IRelmMigrationPlanner _planner;
        private readonly IRelmMigrationSqlRenderer _renderer;
        private readonly MySqlDatabaseProvisioner _provisioner;
        private readonly IRelmDesiredSchemaBuilder _desiredBuilder;
        private readonly MigrationOptions _migrationOptions;

        public DefaultRelmMigrationSqlProvider(
            IRelmSchemaIntrospector introspector,
            IRelmMigrationPlanner planner,
            IRelmMigrationSqlRenderer renderer,
            MySqlDatabaseProvisioner provisioner,
            IRelmDesiredSchemaBuilder desiredBuilder,
            MigrationOptions migrationOptions)
        {
            _introspector = introspector;
            _planner = planner;
            _renderer = renderer;
            _provisioner = provisioner;
            _desiredBuilder = desiredBuilder;
            _migrationOptions = migrationOptions;
        }

        /*
         * This is a placeholder implementation that generates SQL without computing a diff.
         * It demonstrates the inputs and outputs, but does not perform real diffing or SQL generation.
         * You can replace it with a real implementation that computes the diff and generates SQL based on the plan.
         *
         * For example, you might call into your existing migration SQL generator here, passing in the desired and actual schemas,
         * and any relevant options. The generator would compute the diff, create a migration plan, and render it to SQL.
         *
        public async Task<MigrationGenerateResult> Generate(string migrationName, string stampUtc, string dbName, List<ValidatedModelType> modelsForDb, MySqlDatabaseProvisioner provisioner)
        {
            var tables = modelsForDb
                .OrderBy(m => m.TableName, StringComparer.Ordinal)
                .ThenBy(m => m.ClrType.FullName, StringComparer.Ordinal)
                .ToList();

            var sql = await MigrationSqlGeneratorHelper.BuildPlaceholderMigrationSql(_migrationOptions, migrationName, stampUtc, dbName, tables, provisioner);

            // In the real version, if plan has no operations, return NoChanges and sql would be null.
            // For placeholder, treat as changes.
            if (string.IsNullOrWhiteSpace(sql))
                return MigrationGenerateResult.NoChanges(dbName, $"[Relm] No changes detected for database '{dbName}'. No migration file written.");

            return MigrationGenerateResult.Changes(
                dbName,
                sql,
                $"[Relm] Migration SQL generated for database '{dbName}' (diff not computed yet).");
        }
        */

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
            // Determine scope tables (table names derived from model metadata already resolved by your resolver)
            var scopeTables = modelsForDb
                .Select(m => m.TableName)
                .Distinct(StringComparer.Ordinal)
                .ToHashSet(StringComparer.Ordinal);

            // Desired schema is built from your CoreRelm metadata pipeline.
            // For now, we build it from the resolved model list (table names + columns etc. must come from CoreRelm metadata reader).
            // If you already have a DesiredSnapshot builder in CoreRelm, call that instead.
            var desired = await _desiredBuilder.BuildAsync(dbName, modelsForDb);

            // Actual schema:
            // - if DB exists, introspect it
            // - otherwise warn and treat as empty (per your policy for non-apply paths)
            SchemaSnapshot actual;
            var dbExists = await _provisioner.DatabaseExistsAsync(_migrationOptions, dbName);
            if (!dbExists)
            {
                if (!_migrationOptions.Quiet)
                    Console.WriteLine($"WARNING: Database `{dbName}` does not exist. It will be created during apply/db migrate.");

                actual = SchemaSnapshotFactory.Empty(dbName);
            }
            else
            {
                var dbConn = _migrationOptions.ConnectionStringTemplate?.Replace("{db}", dbName, StringComparison.Ordinal);
                if (string.IsNullOrWhiteSpace(dbConn))
                    throw new ArgumentException("Connection string is required.", nameof(dbConn));

                var context = new RelmContext(dbConn);
                actual = await _introspector.LoadSchemaAsync(context);
            }

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
