using CoreRelm.Interfaces.Migrations;
using CoreRelm.Migrations;
using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Execution;
using CoreRelm.Models.Migrations.MigrationPlans;
using CoreRelm.Models.Migrations.Tooling.Apply;
using CoreRelm.Models.Migrations.Tooling.Drift;
using CoreRelm.Models.Migrations.Tooling.Generation;
using CoreRelm.Models.Migrations.Tooling.Validation;
using CoreRelm.RelmInternal.Contexts;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.MigrationEnums;

namespace CoreRelm.Tests.Migrations
{
    [Collection("JsonConfiguration")]
    public class RelmMigrationToolingTests : IClassFixture<JsonConfigurationFixture>
    {
        private readonly IConfiguration _configuration;

        public RelmMigrationToolingTests(JsonConfigurationFixture fixture)
        {
            _configuration = fixture.Configuration;
            RelmHelper.UseConfiguration(_configuration);
        }

        private static Assembly ThisAssembly => typeof(RelmMigrationToolingTests).Assembly;

        private static ResolvedModelSet MakeResolvedSet(params (string db, Type type)[] pairs)
        {
            var allModels = pairs
                .Select(p => new ValidatedModelType(p.type, p.db, p.type.Name))
                .ToList();

            var byDb = allModels
                .GroupBy(m => m.DatabaseName, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

            return new ResolvedModelSet(
                SetName: "set",
                AllModels: allModels,
                ModelsByDatabase: byDb,
                Warnings: ["resolver-warning"]
            );
        }

        [Fact]
        public void ValidateModelSet_AppliesDatabaseFilter()
        {
            var resolved = MakeResolvedSet(
                ("db1", typeof(string)),
                ("db2", typeof(int))
            );

            var tooling = BuildTooling(resolved, provisionerExists: true);

            var report = tooling.ValidateModelSet(new ModelSetsFile(), "set", ThisAssembly,
                new ModelSetValidateOptions(DatabaseFilter: "db1"));

            Assert.Single(report.TypesByDatabase);
            Assert.True(report.TypesByDatabase.ContainsKey("db1"));
            Assert.Equal("resolver-warning", report.Warnings[0]);
        }

        [Fact]
        public async Task GenerateMigrations_NoChanges_WhenPlannerReturnsZeroOps()
        {
            var databaseName = "db1";
            var resolved = MakeResolvedSet((databaseName, typeof(string)));

            var planner = new FakeMigrationPlanner
            {
                Handler = (desired, actual, opt) => new MigrationPlan(databaseName, [], [], [], opt.StampUtc)
            };

            var renderer = new FakeMigrationSqlRenderer();

            var tooling = BuildTooling(resolved, provisionerExists: true, planner: planner, renderer: renderer);

            var options = new GenerateMigrationsOptions(
                ConnectionStringTemplate: "Server=localhost;Database={db};Uid=x;Pwd=y;",
                Destructive: false,
                IncludeUseDatabase: true,
                EnsureDatabaseExistsDuringGenerate: false,
                TreatMissingDatabaseAsEmpty: true,
                CancelToken: CancellationToken.None
            );

            var result = await tooling.GenerateMigrationsAsync(new ModelSetsFile(), "set", ThisAssembly, options);

            Assert.True(result.ByDatabase.ContainsKey(databaseName));
            var db = result.ByDatabase[databaseName];
            Assert.False(db.HasChanges);
            Assert.Null(db.Sql);
            Assert.Equal(0, db.OperationCount);
        }

        [Fact]
        public async Task GenerateMigrations_Blockers_PreventSql()
        {
            var databaseName = "db1";
            var resolved = MakeResolvedSet((databaseName, typeof(string)));

            var planner = new FakeMigrationPlanner
            {
                Handler = (desired, actual, opt) =>
                    new MigrationPlan(databaseName, [new FakeOp()], [], ["blocker"], opt.StampUtc)
            };

            var tooling = BuildTooling(resolved, provisionerExists: true, planner: planner);

            var options = new GenerateMigrationsOptions(
                ConnectionStringTemplate: "Server=localhost;Database={db};Uid=x;Pwd=y;",
                IncludeUseDatabase: true,
                EnsureDatabaseExistsDuringGenerate: false,
                TreatMissingDatabaseAsEmpty: true,
                CancelToken: CancellationToken.None
            );

            var result = await tooling.GenerateMigrationsAsync(new ModelSetsFile(), "set", ThisAssembly, options);

            var db = result.ByDatabase[databaseName];
            Assert.True(db.HasChanges);
            Assert.Null(db.Sql);
            Assert.Contains("Blockers detected", db.Messages[0]);
            Assert.Single(db.Blockers);
        }

        [Fact]
        public async Task GenerateMigrations_PassesIncludeUseDatabaseToRenderer()
        {
            var databaseName = "db1";
            var resolved = MakeResolvedSet((databaseName, typeof(string)));

            var planner = new FakeMigrationPlanner
            {
                Handler = (desired, actual, opt) =>
                    new MigrationPlan(databaseName, [new FakeOp()], [], [], opt.StampUtc)
            };

            var renderer = new FakeMigrationSqlRenderer();

            var tooling = BuildTooling(resolved, provisionerExists: true, planner: planner, renderer: renderer);

            var options = new GenerateMigrationsOptions(
                ConnectionStringTemplate: "Server=localhost;Database={db};Uid=x;Pwd=y;",
                IncludeUseDatabase: false,
                EnsureDatabaseExistsDuringGenerate: false,
                TreatMissingDatabaseAsEmpty: true,
                CancelToken: CancellationToken.None
            );

            var result = await tooling.GenerateMigrationsAsync(new ModelSetsFile(), "set", ThisAssembly, options);

            Assert.NotNull(renderer.LastOptions);
            Assert.False(renderer.LastOptions!.IncludeUseDatabase);
        }

        [Fact]
        public async Task GenerateMigrations_TreatsMissingDatabaseAsEmpty_WhenConfigured()
        {
            var databaseName = "db1";
            var resolved = MakeResolvedSet((databaseName, typeof(string)));

            var planner = new FakeMigrationPlanner
            {
                Handler = (desired, actual, opt) =>
                    new MigrationPlan(databaseName, [new FakeOp()], [], [], opt.StampUtc)
            };

            var provisioner = new FakeDatabaseProvisioner { Exists = false };
            var tooling = BuildTooling(resolved, provisioner);

            var options = new GenerateMigrationsOptions(
                ConnectionStringTemplate: "Server=localhost;Database={db};Uid=x;Pwd=y;",
                TreatMissingDatabaseAsEmpty: true,
                EnsureDatabaseExistsDuringGenerate: false,
                IncludeUseDatabase: true,
                CancelToken: CancellationToken.None
            );

            var result = await tooling.GenerateMigrationsAsync(new ModelSetsFile(), "set", ThisAssembly, options);

            Assert.Contains(result.Warnings, w => w.Contains("missing", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetStatusAsync_MissingDatabase_MarksAllPending_AndWarns()
        {
            var databaseName = "db1";
            var resolved = MakeResolvedSet((databaseName, typeof(string)));

            var provisioner = new FakeDatabaseProvisioner { Exists = false };
            var tooling = BuildTooling(resolved, provisioner);

            var scriptsByDb = new Dictionary<string, IReadOnlyList<MigrationScript>>(StringComparer.Ordinal)
            {
                [databaseName] = [
                    new("a.sql","deadbeef", "SQL"),
                    new("b.sql","deadbeef2", "SQL")
                ]
            };

            var request = new MigrationStatusRequest(
                ConnectionString: "",
                ConnectionStringTemplate: "Server=localhost;Database={db};Uid=x;Pwd=y;",
                WarnIfDatabaseMissing: true,
                ScriptsByDatabase: scriptsByDb,
                Options: new StatusOptions()
            );

            var status = await tooling.GetStatusAsync(request);

            var db = status.ByDatabase["db1"];
            Assert.False(db.DatabaseExists);
            Assert.Equal(0, db.AppliedCount);
            Assert.Equal(2, db.PendingCount);
            Assert.True(db.Warnings.Count > 0);
        }

        [Fact]
        public async Task GetStatusAsync_DetectsDrift_WhenChecksumMismatch()
        {
            var databaseName = "db1";
            var resolved = MakeResolvedSet((databaseName, typeof(string)));

            var provisioner = new FakeDatabaseProvisioner { Exists = true };
            var store = new FakeSchemaMigrationsStore
            {
                Applied = new Dictionary<string, AppliedMigration>(StringComparer.Ordinal)
                {
                    ["a.sql"] = new AppliedMigration("a.sql", RelmMigrationType.Migration, "oldchecksum", DateTime.UtcNow)
                }
            };

            var tooling = BuildTooling(resolved, provisioner, migrationsStore: store);

            var scriptsByDb = new Dictionary<string, IReadOnlyList<MigrationScript>>(StringComparer.Ordinal)
            {
                [databaseName] = [
                    new("a.sql","newchecksum", "SQL")
                ]
            };

            var request = new MigrationStatusRequest(
                ConnectionString: "",
                ConnectionStringTemplate: "Server=localhost;Database={db};Uid=x;Pwd=y;",
                WarnIfDatabaseMissing: true,
                ScriptsByDatabase: scriptsByDb,
                Options: new StatusOptions()
            );

            var status = await tooling.GetStatusAsync(request);

            Assert.True(status.AnyDrift);
            Assert.Single(status.ByDatabase[databaseName].DriftFiles);
        }

        private static RelmMigrationTooling BuildTooling(
            ResolvedModelSet resolved,
            bool provisionerExists,
            FakeMigrationPlanner? planner = null,
            FakeMigrationSqlRenderer? renderer = null)
        {
            var prov = new FakeDatabaseProvisioner { Exists = provisionerExists };
            return BuildTooling(resolved, prov, planner, renderer, new FakeSchemaMigrationsStore());
        }

        private static RelmMigrationTooling BuildTooling(
            ResolvedModelSet resolved,
            FakeDatabaseProvisioner provisioner,
            FakeMigrationPlanner? planner = null,
            FakeMigrationSqlRenderer? renderer = null,
            FakeSchemaMigrationsStore? migrationsStore = null)
        {
            var resolver = new FakeModelSetResolver(resolved);
            var introspector = new FakeSchemaIntrospector();
            var desired = new FakeDesiredSchemaBuilder();
            planner ??= new FakeMigrationPlanner();
            renderer ??= new FakeMigrationSqlRenderer();
            migrationsStore ??= new FakeSchemaMigrationsStore();

            return new RelmMigrationTooling(
                resolver,
                introspector,
                desired,
                planner,
                renderer,
                provisioner,
                migrationsStore,
                log: null,
                informationSchemaContextFactory: connString => new InformationSchemaContext(connString, autoOpenConnection: false, autoInitializeDataSets: false, autoVerifyTables: false)
            );
        }

        private sealed class FakeOp : IMigrationOperation
        {
            public string Description => "fake";
        }
    }
}
