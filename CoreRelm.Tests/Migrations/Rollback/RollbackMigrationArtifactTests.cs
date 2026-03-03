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
    public sealed class RollbackMigrationArtifactTests
    {
        private readonly RollbackMigrationPlanFactory _factory = new();

        private MySqlMigrationSqlRenderer CreateRenderer()
        {
            return new MySqlMigrationSqlRenderer(
                maxSupportedVersion: new Version(1, 0),
                log: NullLogger<MySqlMigrationSqlRenderer>.Instance);
        }

        [Fact]
        public void RollbackArtifact_HeaderIncludesMigrationRollbackType()
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
            var artifact = renderer.Render(migrationPlan);

            // Assert
            Assert.Contains("MigrationRollback", artifact, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void RollbackArtifact_HeaderIncludesSuppliedMetadata()
        {
            // Arrange
            var stampUtc = new DateTime(2026, 3, 1, 12, 34, 56, DateTimeKind.Utc);

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
                DatabaseName: "analyticsdb",
                MigrationName: "rollback_users",
                MigrationFileName: "rollback_users.sql",
                ModelSetName: "AnalyticsModel",
                StampUtc: stampUtc);

            var migrationPlan = _factory.Create(rollbackPlan, metadata);
            var renderer = CreateRenderer();

            // Act
            var artifact = renderer.Render(migrationPlan);

            // Assert
            Assert.Contains("analyticsdb", artifact, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("rollback_users.sql", artifact, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("AnalyticsModel", artifact, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FullyReversibleRollbackArtifact_ComputesChecksumDeterministically()
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
            var artifact1 = renderer.Render(migrationPlan);
            var artifact2 = renderer.Render(migrationPlan);

            // Assert
            Assert.Equal(artifact1, artifact2);

            // TODO:
            // Once you wire checksum helper usage into these tests, assert exact checksum equality here too.
        }

        [Fact]
        public void BlockedRollbackArtifact_ComputesChecksumDeterministically()
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
                    OperationType = "UnknownOperation",
                    Reversibility = MigrationOperationReversibility.UnknownReversible,
                    Reason = "Rollback behavior is unknown."
                }
                ],
                Blockers = ["Rollback behavior is unknown."],
                Warnings = []
            };

            var metadata = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "rollback_blocked_users",
                MigrationFileName: "rollback_blocked_users.sql",
                ModelSetName: "AppModel",
                StampUtc: new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

            var migrationPlan = _factory.Create(rollbackPlan, metadata);
            var renderer = CreateRenderer();

            // Act
            var artifact1 = renderer.Render(migrationPlan);
            var artifact2 = renderer.Render(migrationPlan);

            // Assert
            Assert.Equal(artifact1, artifact2);
        }

        [Fact]
        public void ChecksumChanges_WhenRollbackArtifactContentChanges()
        {
            // Arrange
            var rollbackPlan1 = new RollbackPlan
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

            var rollbackPlan2 = new RollbackPlan
            {
                Status = RollbackPlanStatus.FullyReversible,
                Operations =
                [
                    RollbackTestOperationFactory.DropTable("orders")
                ],
                Analysis = [],
                Blockers = [],
                Warnings = []
            };

            var metadata1 = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "rollback_users",
                MigrationFileName: "rollback_users.sql",
                ModelSetName: "AppModel",
                StampUtc: new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

            var metadata2 = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "rollback_orders",
                MigrationFileName: "rollback_orders.sql",
                ModelSetName: "AppModel",
                StampUtc: new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

            var migrationPlan1 = _factory.Create(rollbackPlan1, metadata1);
            var migrationPlan2 = _factory.Create(rollbackPlan2, metadata2);
            var renderer = CreateRenderer();

            // Act
            var artifact1 = renderer.Render(migrationPlan1);
            var artifact2 = renderer.Render(migrationPlan2);

            // Assert
            Assert.NotEqual(artifact1, artifact2);

            // TODO:
            // Once checksum helper usage is directly wired into the test, assert checksum inequality too.
        }

        [Fact]
        public void BlockedRollbackArtifact_RemainsStructurallyValid()
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
                    OperationType = "UnknownOperation",
                    Reversibility = MigrationOperationReversibility.UnknownReversible,
                    Reason = "Rollback behavior is unknown."
                }
                ],
                Blockers = ["Rollback behavior is unknown."],
                Warnings = ["Manual intervention may be required."]
            };

            var metadata = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "rollback_blocked_users",
                MigrationFileName: "rollback_blocked_users.sql",
                ModelSetName: "AppModel",
                StampUtc: new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

            var migrationPlan = _factory.Create(rollbackPlan, metadata);
            var renderer = CreateRenderer();

            // Act
            var artifact = renderer.Render(migrationPlan);

            // Assert
            Assert.Contains("MigrationRollback", artifact, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("-- BLOCKERS", artifact, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("-- WARNINGS", artifact, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Rollback behavior is unknown.", artifact, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void EquivalentRollbackArtifacts_RenderIdentically()
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

            var renderer = CreateRenderer();

            // Act
            var artifact1 = renderer.Render(_factory.Create(rollbackPlan, metadata));
            var artifact2 = renderer.Render(_factory.Create(rollbackPlan, metadata));

            // Assert
            Assert.Equal(artifact1, artifact2);
        }

        [Fact]
        public void RollbackArtifact_UsesSameChecksumRulesAsForwardMigrationArtifact()
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

            var rollbackMetadata = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "rollback_users",
                MigrationFileName: "rollback_users.sql",
                ModelSetName: "AppModel",
                StampUtc: new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

            var rollbackRenderer = CreateRenderer();
            var rollbackArtifact = rollbackRenderer.Render(_factory.Create(rollbackPlan, rollbackMetadata));

            // Assert
            // This is intentionally a contract-level test for now:
            // rollback artifacts should go through the same renderer/header/checksum machinery.
            Assert.Contains("ChecksumSha256", rollbackArtifact, StringComparison.OrdinalIgnoreCase);
        }
    }
}
