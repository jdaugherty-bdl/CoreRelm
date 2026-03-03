using CoreRelm.Attributes;
using CoreRelm.Migrations;
using CoreRelm.Models;
using CoreRelm.Models.Migrations;
using CoreRelm.RelmInternal.Helpers.Migrations.Execution;
using CoreRelm.RelmInternal.Helpers.Migrations.Introspection;
using CoreRelm.RelmInternal.Helpers.Migrations.MigrationPlans;
using CoreRelm.RelmInternal.Helpers.Migrations.Provisioning;
using CoreRelm.RelmInternal.Helpers.Migrations.Rendering;
using CoreRelm.RelmInternal.Helpers.Migrations.Rollback;
using CoreRelm.RelmInternal.Models.Migrations.Rollback;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.MigrationEnums;

namespace CoreRelm.Tests.Integration.Migrations
{
    [Collection(MySql84Collection.Name)]
    public sealed class CoreRelmRollbackIntegrationTests_V2 : IClassFixture<JsonConfigurationFixture>
    {
        private readonly MySql84Fixture _db;
        private readonly JsonConfigurationFixture _config;

        private const string TestDatabaseName = "it_db";

        public CoreRelmRollbackIntegrationTests_V2(MySql84Fixture db, JsonConfigurationFixture config)
        {
            _db = db;
            _config = config;
            RelmHelper.UseConfiguration(_config.Configuration);
        }

        [RelmDatabase(TestDatabaseName)]
        [RelmTable("it_users")]
        private sealed class ItUser : RelmModel
        {
            [RelmColumn]
            public string Username { get; set; } = string.Empty;
        }

        [RelmDatabase(TestDatabaseName)]
        [RelmTable("it_users")]
        private sealed class ItUserWithNickname : RelmModel
        {
            [RelmColumn]
            public string Username { get; set; } = string.Empty;

            [RelmColumn]
            public string? Nickname { get; set; }
        }

        [Fact]
        public async Task CreateTable_Rollback_DropsTable()
        {
            // Arrange
            await RecreateDatabaseAsync();

            var ct = CancellationToken.None;
            var plumbing = BuildPlumbing();
            var options = BuildOptions(ct);

            var models = new List<ValidatedModelType>
            {
                new(typeof(ItUser), TestDatabaseName, "it_users")
            };

            var forwardFileName = $"RelmMigration_{DateTime.UtcNow:yyyyMMdd_HHmmss}_create_it_users__db-{TestDatabaseName}.sql";

            var provider = plumbing.ProviderFactory.CreateProvider(options);

            // Act
            var forwardArtifact = await provider.GenerateArtifactAsync(models, forwardFileName);

            Assert.True(forwardArtifact.HasChanges);
            Assert.False(string.IsNullOrWhiteSpace(forwardArtifact.Sql));

            var forwardApplied = await plumbing.Applier.ApplyAsync(
                options,
                forwardFileName,
                forwardArtifact.Sql!);

            Assert.True(forwardApplied);

            var rollbackPlan = plumbing.RollbackPlanner.CreateRollbackPlan(forwardArtifact.Plan.Operations);

            var rollbackMetadata = new RollbackMigrationPlanMetadata(
                DatabaseName: TestDatabaseName,
                MigrationName: $"rollback_{forwardArtifact.Plan.MigrationName}",
                MigrationFileName: $"rollback_{forwardArtifact.Plan.MigrationFileName}",
                ModelSetName: forwardArtifact.Plan.ModelSetName,
                StampUtc: DateTime.UtcNow);

            var rollbackMigrationPlan = plumbing.RollbackMigrationPlanFactory.Create(
                rollbackPlan,
                rollbackMetadata);

            var rollbackSql = plumbing.Renderer.Render(rollbackMigrationPlan);

            var rollbackApplied = await plumbing.Applier.ApplyAsync(
                options,
                rollbackMetadata.MigrationFileName,
                rollbackSql);

            Assert.True(rollbackApplied);

            // Assert
            await AssertTableDoesNotExistAsync("it_users");
        }
        [Fact]
        public async Task AddColumn_Rollback_DropsColumn()
        {
            // Arrange
            await RecreateDatabaseAsync();

            var ct = CancellationToken.None;
            var plumbing = BuildPlumbing();
            var options = BuildOptions(ct);

            var provider = plumbing.ProviderFactory.CreateProvider(options);

            // Step 1: Apply baseline table
            var baselineModels = new List<ValidatedModelType>
            {
                new(typeof(ItUser), TestDatabaseName, "it_users")
            };

            var baselineFileName = $"RelmMigration_{DateTime.UtcNow:yyyyMMdd_HHmmss}_baseline_it_users__db-{TestDatabaseName}.sql";
            var baseline = await provider.GenerateAsync(baselineModels, baselineFileName);

            Assert.True(baseline.HasChanges);
            Assert.True(await plumbing.Applier.ApplyAsync(options, baselineFileName, baseline.Sql!));

            await AssertColumnExistsAsync("it_users", "Username");

            // Step 2: Generate migration that adds Nickname
            var changedModels = new List<ValidatedModelType>
            {
                new(typeof(ItUserWithNickname), TestDatabaseName, "it_users")
            };

            var forwardFileName = $"RelmMigration_{DateTime.UtcNow:yyyyMMdd_HHmmss}_add_nickname__db-{TestDatabaseName}.sql";
            var forward = await provider.GenerateAsync(changedModels, forwardFileName);

            Assert.True(forward.HasChanges);
            Assert.True(await plumbing.Applier.ApplyAsync(options, forwardFileName, forward.Sql!));

            await AssertColumnExistsAsync("it_users", "Nickname");

            // Step 3: Apply rollback
            string rollbackSql = BuildRollbackSqlPlaceholder_ReplaceMe();

            var rollbackFileName = $"RelmMigration_{DateTime.UtcNow:yyyyMMdd_HHmmss}_rollback_add_nickname__db-{TestDatabaseName}.sql";
            var rollbackApplied = await plumbing.Applier.ApplyAsync(options, rollbackFileName, rollbackSql);
            Assert.True(rollbackApplied);

            // Assert
            await AssertColumnDoesNotExistAsync("it_users", "Nickname");
        }

        [Fact]
        public async Task BlockedRollback_GeneratesArtifactMarkedBlocked()
        {
            // Arrange
            await RecreateDatabaseAsync();

            var plumbing = BuildPlumbing();

            var rollbackPlan = new RollbackPlan
            {
                Status = RollbackPlanStatus.Blocked,
                Operations = [],
                Analysis =
                [
                    new RollbackAnalysisItem
                {
                    OperationType = "UnsupportedOperation",
                    Reversibility = MigrationOperationReversibility.UnknownReversible,
                    Reason = "Rollback behavior is unknown."
                }
                ],
                Blockers = ["Rollback behavior is unknown."],
                Warnings = ["Manual intervention may be required."]
            };

            var metadata = new RollbackMigrationPlanMetadata(
                DatabaseName: TestDatabaseName,
                MigrationName: "blocked_rollback_test",
                MigrationFileName: "blocked_rollback_test.sql",
                ModelSetName: "AppModel",
                StampUtc: DateTime.UtcNow);

            // Act
            var rollbackMigrationPlan = plumbing.RollbackMigrationPlanFactory.Create(rollbackPlan, metadata);
            var rollbackSql = plumbing.Renderer.Render(rollbackMigrationPlan);

            // Assert
            Assert.Contains("MigrationRollback", rollbackSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("-- BLOCKERS", rollbackSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Rollback behavior is unknown.", rollbackSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("-- WARNINGS", rollbackSql, StringComparison.OrdinalIgnoreCase);
        }

        private IntegrationPlumbing BuildPlumbing()
        {
            var connectionTemplate = _db.BuildConnectionStringTemplate();

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

            var rollbackResolver = new MigrationOperationRollbackResolver();
            var rollbackPlanner = new RollbackMigrationPlanner(rollbackResolver);
            var rollbackMigrationPlanFactory = new RollbackMigrationPlanFactory();

            return new IntegrationPlumbing(
                ProviderFactory: providerFactory,
                Applier: applier,
                Renderer: renderer,
                RollbackPlanner: rollbackPlanner,
                RollbackMigrationPlanFactory: rollbackMigrationPlanFactory,
                ConnectionTemplate: connectionTemplate);
        }

        private MigrationOptions BuildOptions(CancellationToken ct)
        {
            return new MigrationOptions
            {
                ConnectionStringTemplate = _db.BuildConnectionStringTemplate(),
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
        }

        private async Task RecreateDatabaseAsync()
        {
            var serverConn = _db.BuildServerConnectionString();

            await using var conn = new MySqlConnection(serverConn);
            await conn.OpenAsync();

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

        private async Task AssertTableExistsAsync(string tableName)
        {
            var exists = await TableExistsAsync(tableName);
            Assert.True(exists, $"Expected table `{tableName}` to exist.");
        }

        private async Task AssertTableDoesNotExistAsync(string tableName)
        {
            var exists = await TableExistsAsync(tableName);
            Assert.False(exists, $"Expected table `{tableName}` to not exist.");
        }

        private async Task<bool> TableExistsAsync(string tableName)
        {
            var connString = _db.BuildConnectionStringTemplate().Replace("{db}", TestDatabaseName, StringComparison.Ordinal);

            await using var conn = new MySqlConnection(connString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                          SELECT COUNT(*)
                          FROM information_schema.tables
                          WHERE table_schema = @db
                            AND table_name = @table;
                          """;
            cmd.Parameters.AddWithValue("@db", TestDatabaseName);
            cmd.Parameters.AddWithValue("@table", tableName);

            var result = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return result > 0;
        }

        private async Task AssertColumnExistsAsync(string tableName, string columnName)
        {
            var exists = await ColumnExistsAsync(tableName, columnName);
            Assert.True(exists, $"Expected column `{columnName}` on `{tableName}` to exist.");
        }

        private async Task AssertColumnDoesNotExistAsync(string tableName, string columnName)
        {
            var exists = await ColumnExistsAsync(tableName, columnName);
            Assert.False(exists, $"Expected column `{columnName}` on `{tableName}` to not exist.");
        }

        private async Task<bool> ColumnExistsAsync(string tableName, string columnName)
        {
            var connString = _db.BuildConnectionStringTemplate().Replace("{db}", TestDatabaseName, StringComparison.Ordinal);

            await using var conn = new MySqlConnection(connString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                          SELECT COUNT(*)
                          FROM information_schema.columns
                          WHERE table_schema = @db
                            AND table_name = @table
                            AND column_name = @column;
                          """;
            cmd.Parameters.AddWithValue("@db", TestDatabaseName);
            cmd.Parameters.AddWithValue("@table", tableName);
            cmd.Parameters.AddWithValue("@column", columnName);

            var result = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return result > 0;
        }

        private async Task<string> BuildRollbackSqlPlaceholder_ReplaceMe()
        {
            var forwardArtifact = await provider.GenerateArtifactAsync(models, forwardFileName);

            Assert.True(forwardArtifact.HasChanges);
            Assert.False(string.IsNullOrWhiteSpace(forwardArtifact.Sql));

            var forwardApplied = await plumbing.Applier.ApplyAsync(
                options,
                forwardFileName,
                forwardArtifact.Sql!);

            Assert.True(forwardApplied);

            var rollbackPlan = plumbing.RollbackPlanner.CreateRollbackPlan(forwardArtifact.Plan.Operations);

            var rollbackMetadata = new RollbackMigrationPlanMetadata(
                DatabaseName: TestDatabaseName,
                MigrationName: $"rollback_{forwardArtifact.Plan.MigrationName}",
                MigrationFileName: $"rollback_{forwardArtifact.Plan.MigrationFileName}",
                ModelSetName: forwardArtifact.Plan.ModelSetName,
                StampUtc: DateTime.UtcNow);

            var rollbackMigrationPlan = plumbing.RollbackMigrationPlanFactory.Create(
                rollbackPlan,
                rollbackMetadata);

            var rollbackSql = plumbing.Renderer.Render(rollbackMigrationPlan);

            var rollbackApplied = await plumbing.Applier.ApplyAsync(
                options,
                rollbackMetadata.MigrationFileName,
                rollbackSql);

            Assert.True(rollbackApplied);

            return rollbackSql;
        }

        private sealed record IntegrationPlumbing(
            MigrationSqlProviderFactory ProviderFactory,
            MigrationApplier Applier,
            MySqlMigrationSqlRenderer Renderer,
            RollbackMigrationPlanner RollbackPlanner,
            RollbackMigrationPlanFactory RollbackMigrationPlanFactory,
            string ConnectionTemplate);
    }
}
