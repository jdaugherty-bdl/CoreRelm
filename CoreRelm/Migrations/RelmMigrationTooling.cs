using CoreRelm.Interfaces.Metadata;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Interfaces.Migrations.Tooling;
using CoreRelm.Interfaces.ModelSets;
using CoreRelm.Models;
using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using CoreRelm.Models.Migrations.MigrationPlans;
using CoreRelm.Models.Migrations.Rendering;
using CoreRelm.Models.Migrations.Tooling.Apply;
using CoreRelm.Models.Migrations.Tooling.Drift;
using CoreRelm.Models.Migrations.Tooling.Generation;
using CoreRelm.Models.Migrations.Tooling.Validation;
using CoreRelm.RelmInternal.Contexts;
using CoreRelm.RelmInternal.Helpers.Migrations.Introspection;
using CoreRelm.RelmInternal.Helpers.Migrations.MigrationPlans;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mysqlx.Notice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static CoreRelm.Enums.CharsetEnums;

namespace CoreRelm.Migrations
{

    public sealed class RelmMigrationTooling(
        IModelSetResolver modelSetResolver,
        IRelmSchemaIntrospector introspector,
        IRelmDesiredSchemaBuilder desiredBuilder,
        IRelmMigrationPlanner planner,
        IRelmMigrationSqlRenderer renderer,
        IRelmDatabaseProvisioner provisioner,
        IRelmSchemaMigrationsStore migrationsStore,
        ILogger<RelmMigrationTooling>? log = null) : IRelmMigrationTooling
    {
        private readonly Func<string, InformationSchemaContext> _informationSchemaContextFactory = dbConn =>
            new InformationSchemaContext(dbConn, autoInitializeDataSets: false, autoVerifyTables: false);

        // internal constructor for testing
        internal RelmMigrationTooling(
            IModelSetResolver modelSetResolver,
            IRelmSchemaIntrospector introspector,
            IRelmDesiredSchemaBuilder desiredBuilder,
            IRelmMigrationPlanner planner,
            IRelmMigrationSqlRenderer renderer,
            IRelmDatabaseProvisioner provisioner,
            IRelmSchemaMigrationsStore migrationsStore,
            ILogger<RelmMigrationTooling>? log,
            Func<string, InformationSchemaContext> informationSchemaContextFactory) : this(
                modelSetResolver,
                introspector,
                desiredBuilder,
                planner,
                renderer,
                provisioner,
                migrationsStore,
                log)
        {
            _informationSchemaContextFactory = informationSchemaContextFactory ?? throw new ArgumentNullException(nameof(informationSchemaContextFactory));
        }

        private IReadOnlyDictionary<string, List<ValidatedModelType>>? ResolveAndValidateModelSets(ModelSetsFile modelSets, string setName, Assembly modelsAssembly, List<string> warnings, List<string> errors)
        {
            ResolvedModelSet resolved;
            try
            {
                resolved = modelSetResolver.ResolveSet(modelSets, setName, modelsAssembly);
                warnings.AddRange(resolved.Warnings);
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
                return null;
            }

            return resolved.ModelsByDatabase;
        }

        public ModelSetValidationReport ValidateModelSet(ModelSetsFile modelSets, string setName, Assembly modelsAssembly, ModelSetValidateOptions? options = null)
        {
            options ??= new ModelSetValidateOptions();

            var warnings = new List<string>();
            var errors = new List<string>();

            var databaseValidations = ResolveAndValidateModelSets(modelSets, setName, modelsAssembly, warnings, errors);
            if (databaseValidations == null)
                return new ModelSetValidationReport(setName, warnings, errors, new Dictionary<string, IReadOnlyList<ValidatedModelType>>());

            // Apply database filter if requested
            if (!string.IsNullOrWhiteSpace(options.DatabaseFilter))
            {
                databaseValidations = databaseValidations
                    .Where(kvp => string.Equals(kvp.Key, options.DatabaseFilter, StringComparison.Ordinal))
                    .ToDictionary(k => k.Key, v => v.Value, StringComparer.Ordinal);
            }

            var result = databaseValidations.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<ValidatedModelType>)[.. kvp.Value.OrderBy(t => t.ClrType.FullName, StringComparer.Ordinal)],
                StringComparer.Ordinal);

            return new ModelSetValidationReport(setName, warnings, errors, result);
        }

        public async Task<MigrationGenerationResult> GenerateMigrationsAsync(ModelSetsFile modelSets, string setName, Assembly modelsAssembly, GenerateMigrationsOptions options)
        {
            var warnings = new List<string>();
            var errors = new List<string>();
            var stamp = DateTime.UtcNow;

            var byDb = new Dictionary<string, PerDatabaseMigrationResult>(StringComparer.Ordinal);

            var databaseValidations = ResolveAndValidateModelSets(modelSets, setName, modelsAssembly, warnings, errors);
            if (databaseValidations == null)
                return new MigrationGenerationResult(setName, stamp, byDb, warnings, errors);

            var emptyDatabaseOptions = new MigrationOptions
            {
                ConnectionStringTemplate = options.ConnectionStringTemplate,
                DropFunctionsOnCreate = options.DropFunctionsOnCreate,
                Destructive = options.Destructive,
                CancelToken = options.CancelToken
            };

            var orderedDatabaseNames = databaseValidations.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
            foreach (var databaseName in orderedDatabaseNames)
            {
                emptyDatabaseOptions.DatabaseName = databaseName;

                var types = databaseValidations[databaseName].OrderBy(t => t.ClrType.FullName, StringComparer.Ordinal).ToList();

                // optionally ensure db exists during generate (usually false)
                if (options.EnsureDatabaseExistsDuringGenerate)
                {
                    try
                    {
                        await provisioner.InitializeEmptyDatabaseAsync(emptyDatabaseOptions, DatabaseCharset.Utf8mb4, DatabaseCollation.Utf8mb40900AiCi);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"EnsureDatabaseExists failed for `{databaseName}`: {ex.Message}");
                    }
                }

                // Determine actual schema (or empty if missing & policy says so)
                var dbExists = false;
                if (options.TreatMissingDatabaseAsEmpty && !options.EnsureDatabaseExistsDuringGenerate)
                {
                    dbExists = false;
                }
                else
                {
                    try
                    {
                        dbExists = await provisioner.DatabaseExistsAsync(emptyDatabaseOptions);
                    }
                    catch (Exception ex)
                    {
                        warnings.Add($"Could not verify existence of `{databaseName}`: {ex.Message}");
                    }
                }

                SchemaSnapshot actual;
                if (!dbExists && options.TreatMissingDatabaseAsEmpty)
                {
                    warnings.Add($"Database `{databaseName}` missing; treating as empty schema for generation. It will be created during apply/migrate.");
                    actual = SchemaSnapshotFactory.Empty(databaseName); // you already have this helper pattern
                }
                else
                {
                    var dbConn = options.ConnectionStringTemplate?.Replace("{db}", databaseName, StringComparison.Ordinal)
                        ?? throw new ArgumentException("Connection string template is required.", nameof(options.ConnectionStringTemplate));

                    actual = await introspector.LoadSchemaAsync(_informationSchemaContextFactory(dbConn));
                }

                // Build desired schema from CLR types subset
                var desired = desiredBuilder.Build(databaseName, types);
                // ^ You’ll implement this in CoreRelm based on your existing DesiredSchemaBuilder logic.
                // It should create a schema snapshot compatible with your planner.

                var scopeTables = desired.Tables.Keys.ToHashSet(StringComparer.Ordinal);

                var planOptions = new MigrationPlanOptions(
                    DropFunctionsOnCreate: options.DropFunctionsOnCreate,
                    Destructive: options.Destructive,
                    ScopeTables: scopeTables,
                    StampUtc: stamp
                );

                var plan = planner.Plan(desired, actual, planOptions);

                if (plan.Blockers.Count > 0)
                {
                    byDb[databaseName] = new PerDatabaseMigrationResult(
                        databaseName,
                        HasChanges: true,
                        Sql: null,
                        OperationCount: plan.Operations.Count,
                        Messages: ["Blockers detected; SQL not generated."],
                        Blockers: plan.Blockers
                    );
                    continue;
                }

                if (plan.Operations.Count == 0)
                {
                    byDb[databaseName] = new PerDatabaseMigrationResult(
                        databaseName,
                        HasChanges: false,
                        Sql: null,
                        OperationCount: 0,
                        Messages: [$"No changes detected for database '{databaseName}'. No migration file should be written."],
                        Blockers: []
                    );
                    continue;
                }

                var renderOptions = new MySqlRenderOptions
                {
                    IncludeUseDatabase = options.IncludeUseDatabase // ensures generated SQL includes "USE db;"
                };

                var sql = renderer.Render(plan, renderOptions); // renderer includes USE db;

                byDb[databaseName] = new PerDatabaseMigrationResult(
                    databaseName,
                    HasChanges: true,
                    Sql: sql,
                    OperationCount: plan.Operations.Count,
                    Messages: [$"Changes detected for database '{databaseName}'."],
                    Blockers: []
                );
            }

            return new MigrationGenerationResult(setName, stamp, byDb, warnings, errors);
        }

        public ApplyMigrationsResult ApplyMigrations(ApplyMigrationsRequest request)
        {
            // implement same as DbMigrateHandler.cs

            throw new NotSupportedException("ApplyMigrations is intentionally not implemented yet. Use host orchestration (LedgerLite) for apply.");
        }

        public async Task<MigrationStatusResult> GetStatusAsync(MigrationStatusRequest request)
        {
            var databaseStatuses = new Dictionary<string, PerDatabaseStatus>(StringComparer.Ordinal);
            var anyDrift = false;

            var emptyDatabaseOptions = new MigrationOptions
            {
                ConnectionStringTemplate = request.ConnectionStringTemplate,
                CancelToken = default
            };

            foreach (var databaseMigrationFiles in request.ScriptsByDatabase)
            {
                var dbName = databaseMigrationFiles.Key;
                emptyDatabaseOptions.DatabaseName = dbName;

                var warnings = new List<string>();
                var driftFiles = new List<string>();

                bool exists;
                try
                {
                    exists = await provisioner.DatabaseExistsAsync(emptyDatabaseOptions);
                }
                catch (Exception ex)
                {
                    exists = false;
                    if (request.WarnIfDatabaseMissing)
                        warnings.Add($"Could not verify existence of `{dbName}`: {ex.Message} (will be created during apply/migrate).");
                }

                if (!exists)
                {
                    if (request.WarnIfDatabaseMissing)
                        warnings.Add($"Database `{dbName}` does not exist (will be created during apply/migrate).");

                    databaseStatuses[dbName] = new PerDatabaseStatus(dbName, false, AppliedCount: 0, PendingCount: databaseMigrationFiles.Value.Count, DriftFiles: driftFiles, Warnings: warnings);
                    continue;
                }


                var dbConn = request.ConnectionStringTemplate?.Replace("{db}", dbName, StringComparison.Ordinal)
                    ?? throw new ArgumentException("Connection string template is required.", nameof(request.ConnectionStringTemplate));

                // Ensure SchemaMigrations table exists, then read applied list
                //var applied = await migrationsStore.GetAppliedMigrationsAsync(new InformationSchemaContext(dbConn, autoInitializeDataSets: false, autoVerifyTables: false));
                var applied = await migrationsStore.GetAppliedMigrationsAsync(_informationSchemaContextFactory(dbConn));
                // ^ implemented in store: open connection, ensure table, return map filename->checksum

                if (applied == null)
                {
                    warnings.Add("Failed to read applied migrations; treating as none applied. Error details should be in logs.");
                    applied = [];
                }

                var appliedCount = applied.Count;
                var pendingCount = 0;

                var fileOrderedDatabaseScripts = databaseMigrationFiles.Value.OrderBy(s => s.FileName, StringComparer.Ordinal);
                foreach (var script in fileOrderedDatabaseScripts)
                {
                    if (!applied.TryGetValue(script.FileName, out var appliedMigration))
                    {
                        pendingCount++;
                        continue;
                    }

                    if (!string.Equals(appliedMigration.ChecksumSha256, script.ChecksumSha256, StringComparison.OrdinalIgnoreCase))
                    {
                        anyDrift = true;
                        driftFiles.Add(script.FileName);
                    }
                }

                databaseStatuses[dbName] = new PerDatabaseStatus(dbName, true, appliedCount, pendingCount, driftFiles, warnings);
            }

            return new MigrationStatusResult(databaseStatuses, anyDrift);
        }
    }
}
