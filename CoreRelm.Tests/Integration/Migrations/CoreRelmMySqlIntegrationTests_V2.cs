using CoreRelm.Attributes;
using CoreRelm.Extensions;
using CoreRelm.Interfaces.Metadata;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Interfaces.ModelSets;
using CoreRelm.Migrations;
using CoreRelm.Migrations.Contexts;
using CoreRelm.Models;
using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Tooling.Apply;
using CoreRelm.Models.Migrations.Tooling.Drift;
using CoreRelm.Options;
using CoreRelm.RelmInternal.Contexts;
using CoreRelm.RelmInternal.Helpers.Migrations.Execution;
using CoreRelm.RelmInternal.Helpers.Migrations.Introspection;
using CoreRelm.RelmInternal.Helpers.Migrations.MigrationPlans;
using CoreRelm.RelmInternal.Helpers.Migrations.Provisioning;
using CoreRelm.RelmInternal.Helpers.Migrations.Rendering;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Integration.Migrations
{

    [Collection(MySql84Collection.Name)]
    public sealed class CoreRelmMySqlIntegrationTests_V2 : IClassFixture<JsonConfigurationFixture>
    {
        private readonly MySql84Fixture _db;
        private readonly JsonConfigurationFixture _config;

        private const string TestDatabaseName = "it_db"; // Must match [RelmDatabase("it_db")] attributes in test models.

        public CoreRelmMySqlIntegrationTests_V2(MySql84Fixture db, JsonConfigurationFixture config)
        {
            _db = db;
            _config = config;
            RelmHelper.UseConfiguration(_config.Configuration);
        }

        // --- Integration Test Model ---
        // The database name MUST match TestDatabaseName to satisfy DesiredSchemaBuilder enforcement. [1](https://bostondatalabs-my.sharepoint.com/personal/jdaugherty_bostondatalabs_com/Documents/Microsoft%20Copilot%20Chat%20Files/MigrationSqlProviderFactory.cs)
        [RelmDatabase(TestDatabaseName)]
        [RelmTable("it_users")]
        private sealed class ItUser : RelmModel
        {
            [RelmColumn]
            public string Username { get; set; } = string.Empty;
        }

        [Fact]
        public async Task Generate_Apply_Status_IsClean()
        {
            // Arrange
            // 1) Build your tooling with REAL implementations here (introspector, planner, renderer, store, provisioner)
            // 2) Use _fixture.GetConnectionString() as the base connection
            //
            // Tip: Testcontainers exposes the container connection string, which you can feed into your DAL. [8](https://dotnet.testcontainers.org/modules/mssql/)[2](https://www.freecodecamp.org/news/how-to-use-testcontainers-in-net/)

            // TODO: build RelmMigrationTooling via DI or direct construction with MySqlSchemaIntrospector, etc.
            await RecreateDatabaseAsync();

            var ct = CancellationToken.None;
            var connectionTemplate = _db.BuildConnectionStringTemplate();

            // Build real CoreRelm plumbing graph.
            var provisioner = new MySqlDatabaseProvisioner(log: null);
            var runner = new MySqlScriptRunner();
            var introspector = new MySqlSchemaIntrospector();
            var desiredBuilder = new DesiredSchemaBuilder(log: null);
            var planner = new RelmMigrationPlanner(log: null);
            var renderer = new MySqlMigrationSqlRenderer(new Version(1, 0, 0), log: null);

            var providerFactory = new MigrationSqlProviderFactory(
                provisioner,
                introspector,
                planner,
                renderer,
                desiredBuilder,
                _log: null);

            var migrationsStore = new SchemaMigrationsStore(providerFactory, runner, log: null);
            var applier = new MigrationApplier(migrationsStore, runner, provisioner);

            // Create migration options (record/object with init props in your repo).
            var options = new MigrationOptions
            {
                ConnectionStringTemplate = connectionTemplate,
                DatabaseName = TestDatabaseName,
                Destructive = true,
                DropFunctionsOnCreate = true,
                Quiet = true,
                StampUtc = DateTime.UtcNow,
                MigrationName = "integration_test",
                MigrationsPath = null,
                SaveSystemMigrations = false,
                MigrationErrorPath = ".",
                CancelToken = ct
            };

            // Generate migration SQL for one model.
            var models = new List<ValidatedModelType>
            {
                new(typeof(ItUser), TestDatabaseName, "it_users")
            };

            var migrationFile = $"RelmMigration_{DateTime.UtcNow:yyyyMMdd_HHmmss}_create_it_users__db-{TestDatabaseName}.sql";

            // Act
            var provider = providerFactory.CreateProvider(options);
            var generate = await provider.GenerateAsync(models, migrationFile);

            Assert.True(generate.HasChanges);
            Assert.False(string.IsNullOrWhiteSpace(generate.Sql));

            // Apply migration using CoreRelm applier.
            var ok = await applier.ApplyAsync(options, migrationFile, generate.Sql!);
            Assert.True(ok);

            // Now check status/drift using real tooling (status path hits real DB + migrations store).
            var tooling = BuildToolingForStatus(introspector, desiredBuilder, planner, renderer, provisioner, migrationsStore, connectionTemplate);

            var scriptsByDb = new Dictionary<string, IReadOnlyList<MigrationScript>>(StringComparer.Ordinal)
            {
                [TestDatabaseName] = [
                    // MigrationScript signature is (FileName, SqlText, ChecksumSha256).
                    new MigrationScript(migrationFile, generate.Sql!, generate.Sql!.Sha256Hex())
                ]
            };

            var request = new MigrationStatusRequest(
                ConnectionString: "",
                ConnectionStringTemplate: connectionTemplate,
                WarnIfDatabaseMissing: true,
                ScriptsByDatabase: scriptsByDb,
                Options: new StatusOptions()
            );

            var status = await tooling.GetStatusAsync(request);


            // Assert
            Assert.True(status.ByDatabase.ContainsKey(TestDatabaseName));
            var perDb = status.ByDatabase[TestDatabaseName];

            Assert.True(perDb.DatabaseExists);
            Assert.Equal(0, perDb.PendingCount);
            Assert.False(status.AnyDrift);
            Assert.Empty(perDb.DriftFiles);
        }

        [Fact]
        public async Task AfterApply_Generate_IsNoOp()
        {
            // Arrange: ensure schema already applied from previous test or do apply here
            await RecreateDatabaseAsync();

            var ct = CancellationToken.None;
            var connectionTemplate = _db.BuildConnectionStringTemplate();

            var provisioner = new MySqlDatabaseProvisioner(log: null);
            var runner = new MySqlScriptRunner();
            var introspector = new MySqlSchemaIntrospector();
            var desiredBuilder = new DesiredSchemaBuilder(log: null);
            var planner = new RelmMigrationPlanner(log: null);
            var renderer = new MySqlMigrationSqlRenderer(new Version(1, 0, 0), log: null);

            var providerFactory = new MigrationSqlProviderFactory(
                provisioner, introspector, planner, renderer, desiredBuilder, _log: null);

            var migrationsStore = new SchemaMigrationsStore(providerFactory, runner, log: null);
            var applier = new MigrationApplier(migrationsStore, runner, provisioner);

            var options = new MigrationOptions
            {
                ConnectionStringTemplate = connectionTemplate,
                DatabaseName = TestDatabaseName,
                Destructive = true,
                DropFunctionsOnCreate = true,
                Quiet = true,
                StampUtc = DateTime.UtcNow,
                MigrationName = "integration_test",
                MigrationsPath = null,
                SaveSystemMigrations = false,
                MigrationErrorPath = ".",
                CancelToken = ct
            };

            var models = new List<ValidatedModelType>
            {
                new(typeof(ItUser), TestDatabaseName, "it_users")
            };

            var migrationFile = $"RelmMigration_{DateTime.UtcNow:yyyyMMdd_HHmmss}_create_it_users__db-{TestDatabaseName}.sql";


            // Act: re-run GenerateMigrationsAsync
            var provider = providerFactory.CreateProvider(options);
            var first = await provider.GenerateAsync(models, migrationFile);

            // Assert: HasChanges == false
            Assert.True(first.HasChanges);
            Assert.False(string.IsNullOrWhiteSpace(first.Sql));

            var ok = await applier.ApplyAsync(options, migrationFile, first.Sql!);
            Assert.True(ok);

            // Re-generate after apply should be no changes.
            var second = await provider.GenerateAsync(models, migrationFile);
            Assert.False(second.HasChanges);
        }

        // Arrange: apply migration, then modify the migration script contents/checksum

        // Act: GetStatusAsync

        // Assert: AnyDrift == true
        [Fact]
        public async Task Drift_IsDetected_WhenChecksumMismatch()
        {
            await RecreateDatabaseAsync();

            var ct = CancellationToken.None;
            var connectionTemplate = _db.BuildConnectionStringTemplate();

            var provisioner = new MySqlDatabaseProvisioner(log: null);
            var runner = new MySqlScriptRunner();
            var introspector = new MySqlSchemaIntrospector();
            var desiredBuilder = new DesiredSchemaBuilder(log: null);
            var planner = new RelmMigrationPlanner(log: null);
            var renderer = new MySqlMigrationSqlRenderer(new Version(1, 0, 0), log: null);

            var providerFactory = new MigrationSqlProviderFactory(
                provisioner, introspector, planner, renderer, desiredBuilder, _log: null);

            var migrationsStore = new SchemaMigrationsStore(providerFactory, runner, log: null);
            var applier = new MigrationApplier(migrationsStore, runner, provisioner);

            var options = new MigrationOptions
            {
                ConnectionStringTemplate = connectionTemplate,
                DatabaseName = TestDatabaseName,
                Destructive = true,
                DropFunctionsOnCreate = true,
                Quiet = true,
                StampUtc = DateTime.UtcNow,
                MigrationName = "integration_test",
                MigrationsPath = null,
                SaveSystemMigrations = false,
                MigrationErrorPath = ".",
                CancelToken = ct
            };

            var models = new List<ValidatedModelType>
            {
                new(typeof(ItUser), TestDatabaseName, "it_users")
            };

            var migrationFile = $"RelmMigration_{DateTime.UtcNow:yyyyMMdd_HHmmss}_create_it_users__db-{TestDatabaseName}.sql";

            var provider = providerFactory.CreateProvider(options);
            var gen = await provider.GenerateAsync(models, migrationFile);
            Assert.True(gen.HasChanges);
            var ok = await applier.ApplyAsync(options, migrationFile, gen.Sql!);
            Assert.True(ok);

            // Status with checksum mismatch should report drift.
            var tooling = BuildToolingForStatus(introspector, desiredBuilder, planner, renderer, provisioner, migrationsStore, connectionTemplate);

            var scriptsByDb = new Dictionary<string, IReadOnlyList<MigrationScript>>(StringComparer.Ordinal)
            {
                [TestDatabaseName] = [
                    new MigrationScript(migrationFile, gen.Sql!, "deadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeef")
                ]
            };

            var request = new MigrationStatusRequest(
                ConnectionString: "",
                ConnectionStringTemplate: connectionTemplate,
                WarnIfDatabaseMissing: true,
                ScriptsByDatabase: scriptsByDb,
                Options: new StatusOptions()
            );

            var status = await tooling.GetStatusAsync(request);

            Assert.True(status.AnyDrift);
            Assert.Single(status.ByDatabase[TestDatabaseName].DriftFiles);
        }

        private static RelmMigrationTooling BuildToolingForStatus(
            IRelmSchemaIntrospector introspector,
            IRelmDesiredSchemaBuilder desiredBuilder,
            IRelmMigrationPlanner planner,
            IRelmMigrationSqlRenderer renderer,
            IRelmDatabaseProvisioner provisioner,
            IRelmSchemaMigrationsStore migrationsStore,
            string connectionStringTemplate)
        {
            // ModelSetResolver is unused for GetStatusAsync; provide a stub that would fail if called.
            var resolver = new ThrowingModelSetResolver();

            // Use internal ctor that accepts the InformationSchemaContext factory.
            return new RelmMigrationTooling(
                resolver,
                introspector,
                desiredBuilder,
                planner,
                renderer,
                provisioner,
                migrationsStore,
                log: null,
                informationSchemaContextFactory: cs => new RelmContextOptionsBuilder(cs)
                    //.SetAutoOpenConnection(false)
                    //.SetAutoInitializeDataSets(false)
                    //.SetAutoVerifyTables(false)
                    .Build<InformationSchemaContext>() 
                    ?? throw new InvalidOperationException("Failed to create InformationSchemaContext"),
                appliedMigrationContextFactory: cs => new RelmContextOptionsBuilder(cs)
                    //.SetAutoOpenConnection(false)
                    //.SetAutoInitializeDataSets(false)
                    //.SetAutoVerifyTables(false)
                    .Build<RelmInternalAppliedMigrationContext>()
                    ?? throw new InvalidOperationException("Failed to create RelmInternalAppliedMigrationContext")
            );
        }

        private async Task RecreateDatabaseAsync()
        {
            var serverConn = _db.BuildServerConnectionString();
            await using var conn = new MySqlConnection(serverConn);
            await conn.OpenAsync();

            // Drop & recreate per test for isolation (works with fixed DB name required by attributes).
            await using (var drop = conn.CreateCommand())
            {
                drop.CommandText = $"DROP DATABASE IF EXISTS `{TestDatabaseName}`;";
                await drop.ExecuteNonQueryAsync();
            }

            await using (var create = conn.CreateCommand())
            {
                create.CommandText = $"CREATE DATABASE `{TestDatabaseName}`;";
                await create.ExecuteNonQueryAsync();
            }
        }

        private sealed class ThrowingModelSetResolver : IModelSetResolver
        {
            public ModelSetsFile LoadModelSets(string? modelSetsPath)
            {
                throw new NotSupportedException("Load model sets is not used in these integration tests.");
            }

            public ResolvedModelSet ResolveSet(ModelSetsFile modelSets, string setName, Assembly modelsAssembly)
                => throw new NotSupportedException("Model set resolution is not used in these integration tests.");

            public (ResolvedModelSet Resolved, ResolvedModelSetDiagnostics Diagnostics) ResolveSetWithDiagnostics(ModelSetsFile file, string setName, Assembly modelsAssembly)
            {
                throw new NotSupportedException("Model set resolution with diagnostics is not used in these integration tests.");
            }
        }
    }
}
