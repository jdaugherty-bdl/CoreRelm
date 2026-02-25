using CoreRelm.Enums;
using CoreRelm.Interfaces;
using CoreRelm.Interfaces.Metadata;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Interfaces.ModelSets;
using CoreRelm.Migrations.Contexts;
using CoreRelm.Models;
using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Execution;
using CoreRelm.Models.Migrations.Introspection;
using CoreRelm.Models.Migrations.MigrationPlans;
using CoreRelm.Models.Migrations.Rendering;
using System.Reflection;
using static CoreRelm.Enums.MigrationEnums;

namespace CoreRelm.Tests
{

    public sealed class FakeModelSetResolver : IModelSetResolver
    {
        private readonly ResolvedModelSet _resolved;

        public FakeModelSetResolver(ResolvedModelSet resolved) => _resolved = resolved;

        public ModelSetsFile LoadModelSets(string? modelSetsPath) => throw new NotImplementedException();

        public ResolvedModelSet ResolveSet(ModelSetsFile file, string setName, Assembly modelsAssembly) => _resolved;

        public (ResolvedModelSet Resolved, ResolvedModelSetDiagnostics Diagnostics) ResolveSetWithDiagnostics(ModelSetsFile file, string setName, Assembly modelsAssembly)
            => (_resolved, new ResolvedModelSetDiagnostics(
            SetName: setName,
            AssemblyTypeCount: 0,
            ExplicitTypeNameCount: 0,
            ExplicitTypesResolvedCount: 0,
            NamespacePrefixCount: 0,
            NamespaceMatchedCount: 0,
            CandidateCountBeforeFilter: 0,
            AbstractExcludedCount: 0,
            NotRelmModelExcludedCount: 0,
            IncludedCount: _resolved.AllModels.Count,
            MissingRelmDatabaseCount: 0,
            MissingRelmTableCount: 0,
            AttributeValueErrorCount: 0,
            Errors: []
        ));
    }

    public sealed class FakeSchemaIntrospector : IRelmSchemaIntrospector
    {
        public Func<InformationSchemaContext, SchemaIntrospectionOptions?, CancellationToken, Task<SchemaSnapshot>> Handler { get; set; } 
            = (_, _, _) => Task.FromResult(SchemaSnapshotFactory.Empty("test"));

        public Task<SchemaSnapshot> LoadSchemaAsync(InformationSchemaContext ctx, SchemaIntrospectionOptions? options = null, CancellationToken ct = default) 
            => Handler(ctx, options, ct);
    }

    public sealed class FakeDesiredSchemaBuilder : IRelmDesiredSchemaBuilder
    {
        public Func<string, IReadOnlyList<ValidatedModelType>, SchemaSnapshot> Handler { get; set; } = (db, types) => SchemaSnapshotFactory.Empty(db);
        public Func<string, IReadOnlyList<ValidatedModelType>, Task<SchemaSnapshot>> HandlerAsync { get; set; } = (db, types) => Task.FromResult(SchemaSnapshotFactory.Empty(db));

        public SchemaSnapshot Build(string databaseFilter, IReadOnlyList<ValidatedModelType> modelsForDb)
            => Handler(databaseFilter, modelsForDb);

        public Task<SchemaSnapshot> BuildAsync(string databaseFilter, List<ValidatedModelType> modelsForDb)
            => HandlerAsync(databaseFilter, modelsForDb);
    }

    public sealed class FakeMigrationPlanner : IRelmMigrationPlanner
    {
        public Func<SchemaSnapshot, SchemaSnapshot, MigrationPlanOptions, MigrationPlan> Handler { get; set; }
            = (desired, actual, options) => new MigrationPlan(desired.DatabaseName, options.MigrationName, options.MigrationFileName, options.ModelSetName, RelmMigrationType.Migration, [], [], [], options.StampUtc);

        public MigrationPlan Plan(SchemaSnapshot desired, SchemaSnapshot actual, MigrationPlanOptions options) => Handler(desired, actual, options);
    }

    public sealed class FakeMigrationSqlRenderer : IRelmMigrationSqlRenderer
    {
        public MySqlRenderOptions? LastOptions { get; private set; }
        public Func<MigrationPlan, MySqlRenderOptions?, string> Handler { get; set; } = (_, __) => "USE `db`; -- sql";

        public string Render(MigrationPlan plan, MySqlRenderOptions? options = null)
        {
            LastOptions = options;
            return Handler(plan, options);
        }
    }

    public sealed class FakeDatabaseProvisioner : IRelmDatabaseProvisioner
    {
        public bool Exists { get; set; } = true;
        public bool InitializeCalled { get; private set; }

        public Task InitializeEmptyDatabaseAsync(MigrationOptions migrationOptions, CharsetEnums.DatabaseCharset? charset = null, CharsetEnums.DatabaseCollation? collation = null)
        {
            InitializeCalled = true;
            return Task.CompletedTask;
        }

        public Task<bool> DatabaseExistsAsync(MigrationOptions options) 
            => Task.FromResult(Exists);

        public Task<bool> EnsureForApplyOrMigrateAsync(MigrationOptions migrationOptions, Action<string, object[]?> logInfo, Action<string, object[]?> logWarn)
        {
            throw new NotImplementedException();
        }

        public Task<bool> WarnIfMissingAsync(MigrationOptions migrationOptions, Action<string> logWarn)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class FakeSchemaMigrationsStore : IRelmSchemaMigrationsStore
    {
        public Dictionary<string, AppliedMigration>? Applied { get; set; } = new(StringComparer.Ordinal);

        public Task<int> EnsureSchemaMigrationTableAsync(IRelmContext context, MigrationOptions migrationOptions)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, AppliedMigration>?> GetAppliedMigrationsAsync(IRelmContext context, CancellationToken ct = default)
            => Task.FromResult(Applied);


        public Task<int> RecordAppliedMigrationAsync(IRelmContext context, string migrationFile, MigrationEnums.RelmMigrationType migrationType, string checksumSha256, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }

    // Minimal factory for empty snapshot used by tests. Replace with your real factory if it exists.
    public static class SchemaSnapshotFactory
    {
        public static SchemaSnapshot Empty(string dbName)
        {
            return new SchemaSnapshot(dbName,
                Tables: new Dictionary<string, TableSchema>(StringComparer.Ordinal),
                Functions: new Dictionary<string, FunctionSchema>(StringComparer.OrdinalIgnoreCase));
        }
    }
}
