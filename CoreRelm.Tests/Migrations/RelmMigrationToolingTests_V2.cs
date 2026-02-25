using CoreRelm.Attributes;
using CoreRelm.Interfaces.Metadata;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Interfaces.ModelSets;
using CoreRelm.Migrations;
using CoreRelm.Migrations.Contexts;
using CoreRelm.Models;
using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Execution;
using CoreRelm.Models.Migrations.Introspection;
using CoreRelm.Models.Migrations.MigrationPlans;
using CoreRelm.Models.Migrations.Rendering;
using CoreRelm.Models.Migrations.Tooling.Apply;
using CoreRelm.Models.Migrations.Tooling.Drift;
using CoreRelm.Models.Migrations.Tooling.Generation;
using CoreRelm.Models.Migrations.Tooling.Validation;
using CoreRelm.Options;
using CoreRelm.RelmInternal.Contexts;
using CoreRelm.RelmInternal.Helpers.Migrations.Introspection;
using CoreRelm.RelmInternal.Helpers.Migrations.MigrationPlans;
using Moq;
using System.Reflection;
using static CoreRelm.Enums.MigrationEnums;

namespace CoreRelm.Tests.Migrations;

[Collection("JsonConfiguration")]
public sealed class RelmMigrationToolingTests_V2 : IClassFixture<JsonConfigurationFixture>
{
    private static Assembly ThisAssembly => typeof(RelmMigrationToolingTests_V2).Assembly;

    public RelmMigrationToolingTests_V2(JsonConfigurationFixture fixture)
    {
        // Keep same pattern as your existing tests.
        RelmHelper.UseConfiguration(fixture.Configuration);
    }

    // --------------------------
    // Test models
    // --------------------------

    private readonly ModelSetsFile modelSetFile = new()
    {
        Version = 1,
        Sets = new Dictionary<string, ModelSetDefinition>(StringComparer.Ordinal)
        {
            ["set"] = new ModelSetDefinition
            {
                Types = [typeof(A_Table_Model).FullName, typeof(B_Table_Model).FullName]
            }
        }
    };

    [RelmDatabase("db1")]
    [RelmTable("a_table")]
    private sealed class A_Table_Model : RelmModel
    {
        [RelmColumn]
        public string CompanyName { get; set; } = string.Empty;

        [RelmColumn]
        public string GroupInternalId { get; set; } = string.Empty;
    }
    [RelmDatabase("db2")]
    [RelmTable("b_table")]
    private sealed class B_Table_Model : RelmModel
    {
        [RelmColumn]
        public string GroupName { get; set; } = string.Empty;
    }

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
    public void ValidateModelSet_AppliesDatabaseFilter_AndPreservesResolverWarnings()
    {
        var resolved = MakeResolvedSet(("db1", typeof(A_Table_Model)), ("db2", typeof(B_Table_Model)));

        var resolver = new Mock<IModelSetResolver>(MockBehavior.Strict);
        resolver
            .Setup(r => r.ResolveSet(It.IsAny<ModelSetsFile>(), "set", It.IsAny<Assembly>()))
            .Returns(resolved);

        resolver
            .Setup(r => r.ResolveSetWithDiagnostics(It.IsAny<ModelSetsFile>(), "set", It.IsAny<Assembly>()))
            .Returns((resolved, new ResolvedModelSetDiagnostics(
                SetName: "set",
                AssemblyTypeCount: 0,
                ExplicitTypeNameCount: 0,
                ExplicitTypesResolvedCount: 0,
                NamespacePrefixCount: 0,
                NamespaceMatchedCount: 0,
                CandidateCountBeforeFilter: 0,
                AbstractExcludedCount: 0,
                NotRelmModelExcludedCount: 0,
                IncludedCount: resolved.AllModels.Count,
                MissingRelmDatabaseCount: 0,
                MissingRelmTableCount: 0,
                AttributeValueErrorCount: 0,
                Errors: []
            )));

        var tooling = BuildTooling(resolver.Object);

        var report = tooling.ValidateModelSet(modelSetFile, "set", ThisAssembly,
            new ModelSetValidateOptions(DatabaseFilter: "db1"));

        Assert.Single(report.TypesByDatabase);
        Assert.True(report.TypesByDatabase.ContainsKey("db1"));
        Assert.Equal("resolver-warning", report.Warnings[0]);
        Assert.Empty(report.Errors);
    }

    [Fact]
    public async Task GenerateMigrationsAsync_NoChanges_WhenPlannerReturnsZeroOps()
    {
        const string db = "db1";
        var resolved = MakeResolvedSet((db, typeof(A_Table_Model)));

        var planner = new Mock<IRelmMigrationPlanner>(MockBehavior.Strict);
        planner.Setup(p => p.Plan(It.IsAny<SchemaSnapshot>(), It.IsAny<SchemaSnapshot>(), It.IsAny<MigrationPlanOptions>()))
            .Returns((SchemaSnapshot desired, SchemaSnapshot actual, MigrationPlanOptions opt) =>
                new MigrationPlan(db, opt.MigrationName, opt.MigrationFileName, opt.ModelSetName, RelmMigrationType.Migration, [], [], [], opt.StampUtc));

        var renderer = new Mock<IRelmMigrationSqlRenderer>(MockBehavior.Strict);

        var resolver = new Mock<IModelSetResolver>(MockBehavior.Strict);
        resolver
            .Setup(r => r.ResolveSet(It.IsAny<ModelSetsFile>(), "set", It.IsAny<Assembly>()))
            .Returns(resolved);

        resolver
            .Setup(r => r.ResolveSetWithDiagnostics(It.IsAny<ModelSetsFile>(), "set", It.IsAny<Assembly>()))
            .Returns((resolved, new ResolvedModelSetDiagnostics(
                SetName: "set",
                AssemblyTypeCount: 0,
                ExplicitTypeNameCount: 0,
                ExplicitTypesResolvedCount: 0,
                NamespacePrefixCount: 0,
                NamespaceMatchedCount: 0,
                CandidateCountBeforeFilter: 0,
                AbstractExcludedCount: 0,
                NotRelmModelExcludedCount: 0,
                IncludedCount: resolved.AllModels.Count,
                MissingRelmDatabaseCount: 0,
                MissingRelmTableCount: 0,
                AttributeValueErrorCount: 0,
                Errors: []
            )));

        var tooling = BuildTooling(resolver.Object, planner: planner.Object, renderer: renderer.Object);

        var options = new GenerateMigrationsOptions(
            ConnectionStringTemplate: "Server=localhost;Database={db};Uid=x;Pwd=y;",
            IncludeUseDatabase: true,
            EnsureDatabaseExistsDuringGenerate: false,
            TreatMissingDatabaseAsEmpty: true
        );

        var result = await tooling.GenerateMigrationsAsync(modelSetFile, "set", ThisAssembly, options);

        Assert.True(result.ByDatabase.ContainsKey(db));
        var perDb = result.ByDatabase[db];
        Assert.False(perDb.HasChanges);
        Assert.Null(perDb.Sql);
        Assert.Equal(0, perDb.OperationCount);

        // Renderer should never be called if no operations.
        renderer.Verify(r => r.Render(It.IsAny<MigrationPlan>(), It.IsAny<MySqlRenderOptions>()), Times.Never);
    }


    [Fact]
    public async Task GenerateMigrationsAsync_Blockers_PreventSql()
    {
        const string db = "db1";
        var resolved = MakeResolvedSet((db, typeof(A_Table_Model)));

        var planner = new Mock<IRelmMigrationPlanner>(MockBehavior.Strict);
        planner.Setup(p => p.Plan(It.IsAny<SchemaSnapshot>(), It.IsAny<SchemaSnapshot>(), It.IsAny<MigrationPlanOptions>()))
            .Returns((SchemaSnapshot desired, SchemaSnapshot actual, MigrationPlanOptions opt) =>
                new MigrationPlan(db, opt.MigrationName, opt.MigrationFileName, opt.ModelSetName, RelmMigrationType.Migration, [Mock.Of<IMigrationOperation>()], [], ["blocker"], opt.StampUtc));

        var renderer = new Mock<IRelmMigrationSqlRenderer>(MockBehavior.Strict);

        var resolver = new Mock<IModelSetResolver>(MockBehavior.Strict);
        resolver
            .Setup(r => r.ResolveSet(It.IsAny<ModelSetsFile>(), "set", It.IsAny<Assembly>()))
            .Returns(resolved);

        resolver
            .Setup(r => r.ResolveSetWithDiagnostics(It.IsAny<ModelSetsFile>(), "set", It.IsAny<Assembly>()))
            .Returns((resolved, new ResolvedModelSetDiagnostics(
                SetName: "set",
                AssemblyTypeCount: 0,
                ExplicitTypeNameCount: 0,
                ExplicitTypesResolvedCount: 0,
                NamespacePrefixCount: 0,
                NamespaceMatchedCount: 0,
                CandidateCountBeforeFilter: 0,
                AbstractExcludedCount: 0,
                NotRelmModelExcludedCount: 0,
                IncludedCount: resolved.AllModels.Count,
                MissingRelmDatabaseCount: 0,
                MissingRelmTableCount: 0,
                AttributeValueErrorCount: 0,
                Errors: []
            )));

        var tooling = BuildTooling(resolver.Object, planner: planner.Object, renderer: renderer.Object);

        var options = new GenerateMigrationsOptions(
            ConnectionStringTemplate: "Server=localhost;Database={db};Uid=x;Pwd=y;",
            EnsureDatabaseExistsDuringGenerate: false,
            TreatMissingDatabaseAsEmpty: true
        );

        var result = await tooling.GenerateMigrationsAsync(new ModelSetsFile(), "set", ThisAssembly, options);

        Assert.True(result.ByDatabase.ContainsKey(db));
        var perDb = result.ByDatabase[db];
        Assert.True(perDb.HasChanges);
        Assert.Null(perDb.Sql);
        Assert.Single(perDb.Blockers);
        Assert.Contains("Blockers detected", perDb.Messages[0]);

        // Renderer should never be called if blockers exist.
        renderer.Verify(r => r.Render(It.IsAny<MigrationPlan>(), It.IsAny<MySqlRenderOptions>()), Times.Never);
    }

    [Fact]
    public async Task GenerateMigrationsAsync_PassesIncludeUseDatabaseToRenderer()
    {
        const string db = "db1";
        var resolved = MakeResolvedSet((db, typeof(string)));

        var planner = new Mock<IRelmMigrationPlanner>(MockBehavior.Strict);
        planner.Setup(p => p.Plan(It.IsAny<SchemaSnapshot>(), It.IsAny<SchemaSnapshot>(), It.IsAny<MigrationPlanOptions>()))
            .Returns((SchemaSnapshot desired, SchemaSnapshot actual, MigrationPlanOptions opt) =>
                new MigrationPlan(db, opt.MigrationName, opt.MigrationFileName, opt.ModelSetName, RelmMigrationType.Migration, [Mock.Of<IMigrationOperation>()], [], [], opt.StampUtc));

        MySqlRenderOptions? captured = null;
        var renderer = new Mock<IRelmMigrationSqlRenderer>(MockBehavior.Strict);
        renderer.Setup(r => r.Render(It.IsAny<MigrationPlan>(), It.IsAny<MySqlRenderOptions>()))
            .Callback<MigrationPlan, MySqlRenderOptions>((p, opts) => captured = opts)
            .Returns("SQL");

        var resolver = new Mock<IModelSetResolver>(MockBehavior.Strict);
        resolver
            .Setup(r => r.ResolveSet(It.IsAny<ModelSetsFile>(), "set", It.IsAny<Assembly>()))
            .Returns(resolved);

        resolver
            .Setup(r => r.ResolveSetWithDiagnostics(It.IsAny<ModelSetsFile>(), "set", It.IsAny<Assembly>()))
            .Returns((resolved, new ResolvedModelSetDiagnostics(
                SetName: "set",
                AssemblyTypeCount: 0,
                ExplicitTypeNameCount: 0,
                ExplicitTypesResolvedCount: 0,
                NamespacePrefixCount: 0,
                NamespaceMatchedCount: 0,
                CandidateCountBeforeFilter: 0,
                AbstractExcludedCount: 0,
                NotRelmModelExcludedCount: 0,
                IncludedCount: resolved.AllModels.Count,
                MissingRelmDatabaseCount: 0,
                MissingRelmTableCount: 0,
                AttributeValueErrorCount: 0,
                Errors: []
            )));

        var tooling = BuildTooling(resolver.Object, planner: planner.Object, renderer: renderer.Object);

        var options = new GenerateMigrationsOptions(
            ConnectionStringTemplate: "Server=localhost;Database={db};Uid=x;Pwd=y;",
            IncludeUseDatabase: false,
            EnsureDatabaseExistsDuringGenerate: false,
            TreatMissingDatabaseAsEmpty: true
        );

        _ = await tooling.GenerateMigrationsAsync(new ModelSetsFile(), "set", ThisAssembly, options);

        Assert.NotNull(captured);
        Assert.False(captured!.IncludeUseDatabase);
    }

    [Fact]
    public async Task GetStatusAsync_MissingDatabase_MarksAllPending_AndWarns()
    {
        const string db = "db1";

        var provisioner = new Mock<IRelmDatabaseProvisioner>(MockBehavior.Strict);
        provisioner.Setup(p => p.DatabaseExistsAsync(It.IsAny<MigrationOptions>())).ReturnsAsync(false);

        var store = new Mock<IRelmSchemaMigrationsStore>(MockBehavior.Loose);

        var tooling = BuildTooling(provisioner: provisioner.Object, migrationsStore: store.Object);

        // IMPORTANT: correct argument order = (FileName, SqlText, ChecksumSha256)
        var scriptsByDb = new Dictionary<string, IReadOnlyList<MigrationScript>>(StringComparer.Ordinal)
        {
            [db] =
            [
                new MigrationScript("a.sql", "SQL", "c1"),
                new MigrationScript("b.sql", "SQL", "c2")
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

        Assert.True(status.ByDatabase.ContainsKey(db));
        var perDb = status.ByDatabase[db];
        Assert.False(perDb.DatabaseExists);
        Assert.Equal(0, perDb.AppliedCount);
        Assert.Equal(2, perDb.PendingCount);
        Assert.NotEmpty(perDb.Warnings);
    }

    [Fact]
    public async Task GetStatusAsync_DetectsDrift_WhenChecksumMismatch()
    {
        const string db = "db1";

        var provisioner = new Mock<IRelmDatabaseProvisioner>(MockBehavior.Strict);
        provisioner.Setup(p => p.DatabaseExistsAsync(It.IsAny<MigrationOptions>())).ReturnsAsync(true);

        var applied = new Dictionary<string, AppliedMigration>(StringComparer.Ordinal)
        {
            ["a.sql"] = new AppliedMigration("a.sql", RelmMigrationType.Migration, "oldchecksum", DateTime.UtcNow)
        };

        var store = new Mock<IRelmSchemaMigrationsStore>(MockBehavior.Strict);
        // InformationSchemaContext derives from RelmContext in your codebase (since tooling passes it).
        store.Setup(s => s.GetAppliedMigrationsAsync(It.IsAny<RelmContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applied);

        var tooling = BuildTooling(provisioner: provisioner.Object, migrationsStore: store.Object);

        // IMPORTANT: correct argument order = (FileName, SqlText, ChecksumSha256)
        var scriptsByDb = new Dictionary<string, IReadOnlyList<MigrationScript>>(StringComparer.Ordinal)
        {
            [db] = [new MigrationScript("a.sql", "SQL", "newchecksum")]
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
        Assert.Single(status.ByDatabase[db].DriftFiles);
    }

    private static RelmMigrationTooling BuildTooling(
        IModelSetResolver? resolver = null,
        ResolvedModelSet? resolved = null,
        IRelmSchemaIntrospector? introspector = null,
        IRelmDesiredSchemaBuilder? desiredBuilder = null,
        IRelmMigrationPlanner? planner = null,
        IRelmMigrationSqlRenderer? renderer = null,
        IRelmDatabaseProvisioner? provisioner = null,
        IRelmSchemaMigrationsStore? migrationsStore = null)
    {
        // Resolver defaults
        if (resolver is null)
        {
            resolved ??= MakeResolvedSet(("db1", typeof(string)));
            var mockResolver = new Mock<IModelSetResolver>(MockBehavior.Strict);
            mockResolver
                .Setup(r => r.ResolveSet(It.IsAny<ModelSetsFile>(), It.IsAny<string>(), It.IsAny<Assembly>()))
                .Returns(resolved);
            resolver = mockResolver.Object;
        }

        // Dependencies we often avoid hitting in tests
        introspector ??= Mock.Of<IRelmSchemaIntrospector>(MockBehavior.Loose);
        desiredBuilder ??= Mock.Of<IRelmDesiredSchemaBuilder>(b =>
            b.Build(It.IsAny<string>(), It.IsAny<List<ValidatedModelType>>()) == SchemaSnapshotFactory.Empty("db1"), MockBehavior.Loose);

        planner ??= Mock.Of<IRelmMigrationPlanner>(MockBehavior.Loose);
        renderer ??= Mock.Of<IRelmMigrationSqlRenderer>(MockBehavior.Loose);

        provisioner ??= Mock.Of<IRelmDatabaseProvisioner>(p =>
            p.DatabaseExistsAsync(It.IsAny<MigrationOptions>()) == Task.FromResult(false), MockBehavior.Loose);

        migrationsStore ??= Mock.Of<IRelmSchemaMigrationsStore>(MockBehavior.Loose);

        // Use the internal ctor so we can supply a safe InformationSchemaContext
        return new RelmMigrationTooling(
            resolver,
            introspector,
            desiredBuilder,
            planner,
            renderer,
            provisioner,
            migrationsStore,
            log: null,
            informationSchemaContextFactory: cs => new RelmContextOptionsBuilder(cs).SetAutoOpenConnection(false).SetAutoInitializeDataSets(false).SetAutoVerifyTables(false).Build<InformationSchemaContext>() ?? throw new InvalidOperationException("Failed to build InformationSchemaContext"),
            appliedMigrationContextFactory: cs => new RelmContextOptionsBuilder(cs).SetAutoOpenConnection(false).SetAutoInitializeDataSets(false).SetAutoVerifyTables(false).Build<RelmInternalAppliedMigrationContext>() ?? throw new InvalidOperationException("Failed to build RelmInternalAppliedMigrationContext")
        );
    }

    [Fact]
    public async Task GenerateMigrationsAsync_PassesMigrationMetadataToPlanner()
    {
        // Arrange
        const string db = "db1";
        var resolved = MakeResolvedSet((db, typeof(A_Table_Model)));
        MigrationPlanOptions? captured = null;

        var planner = new Mock<IRelmMigrationPlanner>(MockBehavior.Strict);
        planner.Setup(p => p.Plan(It.IsAny<SchemaSnapshot>(), It.IsAny<SchemaSnapshot>(), It.IsAny<MigrationPlanOptions>()))
            .Returns((SchemaSnapshot desired, SchemaSnapshot actual, MigrationPlanOptions opt) =>
            {
                captured = opt;
                return new MigrationPlan(db, opt.MigrationName, opt.MigrationFileName, opt.ModelSetName, RelmMigrationType.Migration, [], [], [], opt.StampUtc);
            });

        var renderer = new Mock<IRelmMigrationSqlRenderer>(MockBehavior.Strict);

        var resolver = new Mock<IModelSetResolver>(MockBehavior.Strict);
        resolver
            .Setup(r => r.ResolveSet(It.IsAny<ModelSetsFile>(), "set", It.IsAny<Assembly>()))
            .Returns(resolved);

        resolver
            .Setup(r => r.ResolveSetWithDiagnostics(It.IsAny<ModelSetsFile>(), "set", It.IsAny<Assembly>()))
            .Returns((resolved, new ResolvedModelSetDiagnostics(
                SetName: "set",
                AssemblyTypeCount: 0,
                ExplicitTypeNameCount: 0,
                ExplicitTypesResolvedCount: 0,
                NamespacePrefixCount: 0,
                NamespaceMatchedCount: 0,
                CandidateCountBeforeFilter: 0,
                AbstractExcludedCount: 0,
                NotRelmModelExcludedCount: 0,
                IncludedCount: resolved.AllModels.Count,
                MissingRelmDatabaseCount: 0,
                MissingRelmTableCount: 0,
                AttributeValueErrorCount: 0,
                Errors: []
            )));

        var tooling = BuildTooling(resolver.Object, planner: planner.Object, renderer: renderer.Object);

        var options = new GenerateMigrationsOptions(
            ConnectionStringTemplate: "Server=localhost;Database={db};Uid=x;Pwd=y;",
            EnsureDatabaseExistsDuringGenerate: false,
            TreatMissingDatabaseAsEmpty: true,
            MigrationName: "custom-migration"
        );

        // Act
        _ = await tooling.GenerateMigrationsAsync(new ModelSetsFile(), "set", ThisAssembly, options);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal("custom-migration", captured!.MigrationName);
        Assert.Equal("set", captured.ModelSetName);
    }

    [Fact]
    public void Plan_SetsMigrationNameAndModelSetName_FromOptions()
    {
        // Arrange
        var planner = new RelmMigrationPlanner();
        var desired = new SchemaSnapshot(
            "db1",
            new Dictionary<string, TableSchema>(StringComparer.Ordinal),
            new Dictionary<string, FunctionSchema>(StringComparer.OrdinalIgnoreCase));
        var actual = new SchemaSnapshot(
            "db1",
            new Dictionary<string, TableSchema>(StringComparer.Ordinal),
            new Dictionary<string, FunctionSchema>(StringComparer.OrdinalIgnoreCase));
        var options = new MigrationPlanOptions(
            DropFunctionsOnCreate: false,
            Destructive: false,
            MigrationName: "migration-1",
            MigrationFileName: "migration-1.sql",
            ModelSetName: "set-1",
            ScopeTables: new HashSet<string>(StringComparer.Ordinal),
            StampUtc: DateTime.UtcNow
        );

        // Act
        var plan = planner.Plan(desired, actual, options);

        // Assert
        Assert.Equal("migration-1", plan.MigrationName);
        Assert.Equal("set-1", plan.ModelSetName);
    }

    [Fact]
    public void MigrationPlan_HasChanges_WhenBlockersPresent()
    {
        // Arrange
        var plan = new MigrationPlan(
            DatabaseName: "db1",
            MigrationName: "migration-1",
            MigrationFileName: "migration-1.sql",
            ModelSetName: "set-1",
            MigrationType: RelmMigrationType.Migration,
            Operations: [],
            Warnings: [],
            Blockers: ["blocker"],
            StampUtc: DateTime.UtcNow);

        // Act
        var hasChanges = plan.HasChanges;

        // Assert
        Assert.True(hasChanges);
    }
}