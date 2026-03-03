using CoreRelm.Interfaces.Migrations;
using CoreRelm.RelmInternal.Helpers.Migrations.Rendering;
using CoreRelm.RelmInternal.Helpers.Migrations.Rollback;
using CoreRelm.RelmInternal.Models.Migrations.Rollback;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.MigrationEnums;

namespace CoreRelm.Tests.Migrations.Rollback
{
    public sealed class RollbackMigrationRenderingTests
    {
        private readonly RollbackMigrationPlanFactory _factory = new();

        private MySqlMigrationSqlRenderer CreateRenderer()
        {
            // If your renderer constructor uses DI for maxSupportedVersion, replace this with the
            // exact way you already instantiate it in your existing renderer tests.
            return new MySqlMigrationSqlRenderer(
                maxSupportedVersion: new Version(1, 0),
                log: NullLogger<MySqlMigrationSqlRenderer>.Instance);
        }

        [Fact]
        public void FullyReversibleRollbackPlan_RendersExecutableSql()
        {
            // Arrange
            var rollbackPlan = new RollbackPlan
            {
                Status = RollbackPlanStatus.FullyReversible,
                Operations =
                [
                    RollbackTestOperationFactory.DropTable("users")
                ],
                Analysis = [],
                Blockers = [],
                Warnings = []
            };

            var metadata = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "rollback_users",
                MigrationFileName: "rollback_users.sql",
                ModelSetName: "AppModel",
                StampUtc: new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

            var migrationPlan = _factory.Create(rollbackPlan, metadata);
            var renderer = CreateRenderer();

            // Act
            var sql = renderer.Render(migrationPlan);

            // Assert
            Assert.Contains("DROP TABLE", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("users", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("-- BLOCKERS", sql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BlockedRollbackPlan_RendersBlockerSection()
        {
            // Arrange
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
                Warnings = []
            };

            var metadata = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "blocked_rollback_users",
                MigrationFileName: "blocked_rollback_users.sql",
                ModelSetName: "AppModel",
                StampUtc: new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

            var migrationPlan = _factory.Create(rollbackPlan, metadata);
            var renderer = CreateRenderer();

            // Act
            var sql = renderer.Render(migrationPlan);

            // Assert
            Assert.Contains("-- BLOCKERS", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Rollback behavior is unknown.", sql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void RollbackMigrationType_IsIncludedInRenderedArtifact()
        {
            // Arrange
            var rollbackPlan = new RollbackPlan
            {
                Status = RollbackPlanStatus.FullyReversible,
                Operations =
                [
                    RollbackTestOperationFactory.DropTable("users")
                ],
                Analysis = [],
                Blockers = [],
                Warnings = []
            };

            var metadata = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "rollback_users",
                MigrationFileName: "rollback_users.sql",
                ModelSetName: "AppModel",
                StampUtc: new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

            var migrationPlan = _factory.Create(rollbackPlan, metadata);
            var renderer = CreateRenderer();

            // Act
            var sql = renderer.Render(migrationPlan);

            // Assert
            Assert.Contains("MigrationRollback", sql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void RollbackWarnings_AreRendered()
        {
            // Arrange
            var rollbackPlan = new RollbackPlan
            {
                Status = RollbackPlanStatus.Blocked,
                Operations = [],
                Analysis = [],
                Blockers = ["Rollback is blocked."],
                Warnings = ["Manual intervention may be required."]
            };

            var metadata = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "rollback_users",
                MigrationFileName: "rollback_users.sql",
                ModelSetName: "AppModel",
                StampUtc: new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

            var migrationPlan = _factory.Create(rollbackPlan, metadata);
            var renderer = CreateRenderer();

            // Act
            var sql = renderer.Render(migrationPlan);

            // Assert
            Assert.Contains("-- WARNINGS", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Manual intervention may be required.", sql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void EquivalentRollbackMigrationPlans_RenderDeterministically()
        {
            // Arrange
            var rollbackPlan = new RollbackPlan
            {
                Status = RollbackPlanStatus.FullyReversible,
                Operations =
                [
                    RollbackTestOperationFactory.DropTable("users")
                ],
                Analysis = [],
                Blockers = [],
                Warnings = []
            };

            var metadata = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "rollback_users",
                MigrationFileName: "rollback_users.sql",
                ModelSetName: "AppModel",
                StampUtc: new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

            var migrationPlan1 = _factory.Create(rollbackPlan, metadata);
            var migrationPlan2 = _factory.Create(rollbackPlan, metadata);
            var renderer = CreateRenderer();

            // Act
            var sql1 = renderer.Render(migrationPlan1);
            var sql2 = renderer.Render(migrationPlan2);

            // Assert
            Assert.Equal(sql1, sql2);
        }

        [Fact]
        public void FullyReversibleRollbackPlan_RendersDropForeignKeySql()
        {
            // Arrange
            var rollbackPlan = new RollbackPlan
            {
                Status = RollbackPlanStatus.FullyReversible,
                Operations = [
                RollbackTestOperationFactory.DropForeignKeyWithOriginalDefinition(
                    tableName: "orders",
                    foreignKeyName: "FK_orders_users_user_id",
                    columnName: "user_id",
                    principalTable: "users",
                    principalColumn: "id")
                ],
                Analysis = [],
                Blockers = [],
                Warnings = []
            };

            var metadata = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "rollback_fk",
                MigrationFileName: "rollback_fk.sql",
                ModelSetName: "AppModel",
                StampUtc: new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

            var migrationPlan = _factory.Create(rollbackPlan, metadata);
            var renderer = CreateRenderer();

            // Act
            var sql = renderer.Render(migrationPlan);

            // Assert
            Assert.Contains("ALTER TABLE", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("DROP FOREIGN KEY", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("FK_orders_users_user_id", sql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FullyReversibleRollbackPlan_RendersDropColumnSql()
        {
            // Arrange
            var rollbackPlan = new RollbackPlan
            {
                Status = RollbackPlanStatus.FullyReversible,
                Operations = new IMigrationOperation[]
                {
            RollbackTestOperationFactory.DropColumn("users", "nickname")
                },
                Analysis = Array.Empty<RollbackAnalysisItem>(),
                Blockers = Array.Empty<string>(),
                Warnings = Array.Empty<string>()
            };

            var metadata = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "rollback_users_column",
                MigrationFileName: "rollback_users_column.sql",
                ModelSetName: "AppModel",
                StampUtc: new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

            var migrationPlan = _factory.Create(rollbackPlan, metadata);
            var renderer = CreateRenderer();

            // Act
            var sql = renderer.Render(migrationPlan);

            // Assert
            Assert.Contains("ALTER TABLE", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("DROP COLUMN", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("nickname", sql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FullyReversibleRollbackPlan_RendersDropIndexSql()
        {
            // Arrange
            var rollbackPlan = new RollbackPlan
            {
                Status = RollbackPlanStatus.FullyReversible,
                Operations = new IMigrationOperation[]
                {
            RollbackTestOperationFactory.DropIndexWithOriginalDefinition(
                tableName: "users",
                indexName: "IX_users_email",
                columns: ["email"])
                },
                Analysis = Array.Empty<RollbackAnalysisItem>(),
                Blockers = Array.Empty<string>(),
                Warnings = Array.Empty<string>()
            };

            var metadata = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "rollback_users_index",
                MigrationFileName: "rollback_users_index.sql",
                ModelSetName: "AppModel",
                StampUtc: new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

            var migrationPlan = _factory.Create(rollbackPlan, metadata);
            var renderer = CreateRenderer();

            // Act
            var sql = renderer.Render(migrationPlan);

            // Assert
            Assert.Contains("DROP INDEX", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("IX_users_email", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("users", sql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FullyReversibleRollbackPlan_RendersDropTriggerSql()
        {
            // Arrange
            var rollbackPlan = new RollbackPlan
            {
                Status = RollbackPlanStatus.FullyReversible,
                Operations = new IMigrationOperation[]
                {
            RollbackTestOperationFactory.DropTriggerWithOriginalDefinition(
                triggerName: "trg_users_before_insert",
                tableName: "users",
                bodySql: """
                         BEGIN
                             SET NEW.created_utc = UTC_TIMESTAMP();
                         END
                         """)
                },
                Analysis = Array.Empty<RollbackAnalysisItem>(),
                Blockers = Array.Empty<string>(),
                Warnings = Array.Empty<string>()
            };

            var metadata = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "rollback_users_trigger",
                MigrationFileName: "rollback_users_trigger.sql",
                ModelSetName: "AppModel",
                StampUtc: new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

            var migrationPlan = _factory.Create(rollbackPlan, metadata);
            var renderer = CreateRenderer();

            // Act
            var sql = renderer.Render(migrationPlan);

            // Assert
            Assert.Contains("DROP TRIGGER", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("trg_users_before_insert", sql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FullyReversibleRollbackPlan_RendersDropFunctionSql()
        {
            // Arrange
            var rollbackPlan = new RollbackPlan
            {
                Status = RollbackPlanStatus.FullyReversible,
                Operations = new IMigrationOperation[]
                {
            RollbackTestOperationFactory.DropFunctionWithOriginalDefinition(
                functionName: "fn_normalize_email",
                bodySql: """
                         RETURNS VARCHAR(255)
                         DETERMINISTIC
                         BEGIN
                             RETURN LOWER(TRIM('test@example.com'));
                         END
                         """)
                },
                Analysis = Array.Empty<RollbackAnalysisItem>(),
                Blockers = Array.Empty<string>(),
                Warnings = Array.Empty<string>()
            };

            var metadata = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "rollback_users_function",
                MigrationFileName: "rollback_users_function.sql",
                ModelSetName: "AppModel",
                StampUtc: new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

            var migrationPlan = _factory.Create(rollbackPlan, metadata);
            var renderer = CreateRenderer();

            // Act
            var sql = renderer.Render(migrationPlan);

            // Assert
            Assert.Contains("DROP FUNCTION", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("fn_normalize_email", sql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FullyReversibleRollbackPlan_RendersCreateTriggerSql_WhenReversingDroppedTrigger()
        {
            // Arrange
            var rollbackPlan = new RollbackPlan
            {
                Status = RollbackPlanStatus.FullyReversible,
                Operations = new IMigrationOperation[]
                {
            RollbackTestOperationFactory.CreateTrigger(
                triggerName: "trg_users_before_insert",
                tableName: "users",
                bodySql: """
                         BEGIN
                             SET NEW.created_utc = UTC_TIMESTAMP();
                         END
                         """)
                },
                Analysis = Array.Empty<RollbackAnalysisItem>(),
                Blockers = Array.Empty<string>(),
                Warnings = Array.Empty<string>()
            };

            var metadata = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "rollback_recreate_trigger",
                MigrationFileName: "rollback_recreate_trigger.sql",
                ModelSetName: "AppModel",
                StampUtc: new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

            var migrationPlan = _factory.Create(rollbackPlan, metadata);
            var renderer = CreateRenderer();

            // Act
            var sql = renderer.Render(migrationPlan);

            // Assert
            Assert.Contains("CREATE TRIGGER", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("trg_users_before_insert", sql, StringComparison.OrdinalIgnoreCase);

            // Keep this assertion flexible until you confirm exact renderer formatting.
            Assert.Contains("DELIMITER", sql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FullyReversibleRollbackPlan_RendersCreateFunctionSql_WhenReversingDroppedFunction()
        {
            // Arrange
            var rollbackPlan = new RollbackPlan
            {
                Status = RollbackPlanStatus.FullyReversible,
                Operations = new IMigrationOperation[]
                {
            RollbackTestOperationFactory.CreateFunction(
                functionName: "fn_normalize_email",
                bodySql: """
                         RETURNS VARCHAR(255)
                         DETERMINISTIC
                         BEGIN
                             RETURN LOWER(TRIM('test@example.com'));
                         END
                         """)
                },
                Analysis = Array.Empty<RollbackAnalysisItem>(),
                Blockers = Array.Empty<string>(),
                Warnings = Array.Empty<string>()
            };

            var metadata = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "rollback_recreate_function",
                MigrationFileName: "rollback_recreate_function.sql",
                ModelSetName: "AppModel",
                StampUtc: new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

            var migrationPlan = _factory.Create(rollbackPlan, metadata);
            var renderer = CreateRenderer();

            // Act
            var sql = renderer.Render(migrationPlan);

            // Assert
            Assert.Contains("CREATE FUNCTION", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("fn_normalize_email", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("DELIMITER", sql, StringComparison.OrdinalIgnoreCase);
        }
    }
}
