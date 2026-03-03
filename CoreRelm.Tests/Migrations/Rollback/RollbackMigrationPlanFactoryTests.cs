using CoreRelm.Interfaces.Migrations;
using CoreRelm.RelmInternal.Helpers.Migrations.Rollback;
using CoreRelm.RelmInternal.Models.Migrations.Rollback;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.MigrationEnums;

namespace CoreRelm.Tests.Migrations.Rollback
{
    public sealed class RollbackMigrationPlanFactoryTests
    {
        private readonly RollbackMigrationPlanFactory _factory = new();

        [Fact]
        public void CreatesMigrationPlan_FromFullyReversibleRollbackPlan()
        {
            // Arrange
            var rollbackPlan = new RollbackPlan
            {
                Status = RollbackPlanStatus.FullyReversible,
                Operations = new IMigrationOperation[]
                {
                RollbackTestOperationFactory.DropTable("users")
                },
                Analysis = new[]
                {
                new RollbackAnalysisItem
                {
                    OperationType = "DropTableOperation",
                    Reversibility = MigrationOperationReversibility.Reversible
                }
            },
                Blockers = Array.Empty<string>(),
                Warnings = Array.Empty<string>()
            };

            var metadata = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "20260301_120000_AddUsers_Rollback",
                MigrationFileName: "20260301_120000_AddUsers_Rollback.sql",
                ModelSetName: "AppModel",
                StampUtc: new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

            // Act
            var migrationPlan = _factory.Create(rollbackPlan, metadata);

            // Assert
            Assert.NotNull(migrationPlan);
            Assert.Equal("appdb", migrationPlan.DatabaseName);
            Assert.Equal("20260301_120000_AddUsers_Rollback", migrationPlan.MigrationName);
            Assert.Equal("20260301_120000_AddUsers_Rollback.sql", migrationPlan.MigrationFileName);
            Assert.Equal("AppModel", migrationPlan.ModelSetName);
        }

        [Fact]
        public void SetsMigrationType_ToMigrationRollback()
        {
            // Arrange
            var rollbackPlan = new RollbackPlan
            {
                Status = RollbackPlanStatus.FullyReversible,
                Operations = Array.Empty<IMigrationOperation>(),
                Analysis = Array.Empty<RollbackAnalysisItem>(),
                Blockers = Array.Empty<string>(),
                Warnings = Array.Empty<string>()
            };

            var metadata = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "rollback_test",
                MigrationFileName: "rollback_test.sql",
                ModelSetName: "AppModel",
                StampUtc: DateTime.UtcNow);

            // Act
            var migrationPlan = _factory.Create(rollbackPlan, metadata);

            // Assert
            Assert.Equal(RelmMigrationType.MigrationRollback, migrationPlan.MigrationType);
        }

        [Fact]
        public void CopiesRollbackOperations_ToMigrationPlan()
        {
            // Arrange
            var operations = new IMigrationOperation[]
            {
            RollbackTestOperationFactory.DropTable("users"),
            RollbackTestOperationFactory.DropColumn("users", "nickname")
            };

            var rollbackPlan = new RollbackPlan
            {
                Status = RollbackPlanStatus.FullyReversible,
                Operations = operations,
                Analysis = new[]
                {
                new RollbackAnalysisItem
                {
                    OperationType = "DropTableOperation",
                    Reversibility = MigrationOperationReversibility.Reversible
                },
                new RollbackAnalysisItem
                {
                    OperationType = "DropColumnOperation",
                    Reversibility = MigrationOperationReversibility.Reversible
                }
            },
                Blockers = Array.Empty<string>(),
                Warnings = Array.Empty<string>()
            };

            var metadata = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "rollback_test",
                MigrationFileName: "rollback_test.sql",
                ModelSetName: "AppModel",
                StampUtc: DateTime.UtcNow);

            // Act
            var migrationPlan = _factory.Create(rollbackPlan, metadata);

            // Assert
            Assert.Equal(2, migrationPlan.Operations.Count);
            Assert.Same(operations, migrationPlan.Operations);
        }

        [Fact]
        public void CopiesWarnings_AndBlockers_ToMigrationPlan()
        {
            // Arrange
            var rollbackPlan = new RollbackPlan
            {
                Status = RollbackPlanStatus.Blocked,
                Operations = Array.Empty<IMigrationOperation>(),
                Analysis = Array.Empty<RollbackAnalysisItem>(),
                Blockers = new[] { "Rollback is blocked because original metadata is missing." },
                Warnings = new[] { "Manual intervention may be required." }
            };

            var metadata = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "rollback_test",
                MigrationFileName: "rollback_test.sql",
                ModelSetName: "AppModel",
                StampUtc: DateTime.UtcNow);

            // Act
            var migrationPlan = _factory.Create(rollbackPlan, metadata);

            // Assert
            Assert.Single(migrationPlan.Blockers);
            Assert.Single(migrationPlan.Warnings);
            Assert.Contains("original metadata is missing", migrationPlan.Blockers[0], StringComparison.OrdinalIgnoreCase);
            Assert.Contains("manual intervention", migrationPlan.Warnings[0], StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void PreservesSuppliedMetadata_WhenCreatingMigrationPlan()
        {
            // Arrange
            var stampUtc = new DateTime(2026, 3, 1, 15, 30, 45, DateTimeKind.Utc);

            var rollbackPlan = new RollbackPlan
            {
                Status = RollbackPlanStatus.FullyReversible,
                Operations = Array.Empty<IMigrationOperation>(),
                Analysis = Array.Empty<RollbackAnalysisItem>(),
                Blockers = Array.Empty<string>(),
                Warnings = Array.Empty<string>()
            };

            var metadata = new RollbackMigrationPlanMetadata(
                DatabaseName: "analyticsdb",
                MigrationName: "rollback_analytics_001",
                MigrationFileName: "rollback_analytics_001.sql",
                ModelSetName: "AnalyticsModel",
                StampUtc: stampUtc);

            // Act
            var migrationPlan = _factory.Create(rollbackPlan, metadata);

            // Assert
            Assert.Equal("analyticsdb", migrationPlan.DatabaseName);
            Assert.Equal("rollback_analytics_001", migrationPlan.MigrationName);
            Assert.Equal("rollback_analytics_001.sql", migrationPlan.MigrationFileName);
            Assert.Equal("AnalyticsModel", migrationPlan.ModelSetName);
            Assert.Equal(stampUtc, migrationPlan.StampUtc);
        }

        [Fact]
        public void CreatesBlockedRollbackMigrationPlan_WhenRollbackPlanIsBlocked()
        {
            // Arrange
            var rollbackPlan = new RollbackPlan
            {
                Status = RollbackPlanStatus.Blocked,
                Operations = Array.Empty<IMigrationOperation>(),
                Analysis = new[]
                {
                new RollbackAnalysisItem
                {
                    OperationType = "UnknownOperation",
                    Reversibility = MigrationOperationReversibility.UnknownReversible,
                    Reason = "Rollback behavior is unknown."
                }
            },
                Blockers = new[] { "Rollback behavior is unknown." },
                Warnings = Array.Empty<string>()
            };

            var metadata = new RollbackMigrationPlanMetadata(
                DatabaseName: "appdb",
                MigrationName: "blocked_rollback_test",
                MigrationFileName: "blocked_rollback_test.sql",
                ModelSetName: "AppModel",
                StampUtc: DateTime.UtcNow);

            // Act
            var migrationPlan = _factory.Create(rollbackPlan, metadata);

            // Assert
            Assert.Equal(RelmMigrationType.MigrationRollback, migrationPlan.MigrationType);
            Assert.NotEmpty(migrationPlan.Blockers);
            Assert.Empty(migrationPlan.Operations);
        }
    }
}
