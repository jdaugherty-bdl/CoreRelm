using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models.Migrations.Rendering;
using CoreRelm.RelmInternal.Helpers.Migrations.Rollback;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.MigrationEnums;

namespace CoreRelm.Tests.Migrations.Rollback
{
    public sealed class RollbackMigrationPlannerTests
    {
        private readonly MigrationOperationRollbackResolver _resolver = new();
        private readonly RollbackMigrationPlanner _planner;

        public RollbackMigrationPlannerTests()
        {
            _planner = new RollbackMigrationPlanner(_resolver);
        }

        [Fact]
        public void GeneratesRollbackPlan_ForSingleReversibleOperation()
        {
            // Arrange
            var upOperations = new[]
            {
        RollbackTestOperationFactory.CreateTable("users")
    };

            // Act
            var rollbackPlan = _planner.CreateRollbackPlan(upOperations);

            // Assert
            Assert.NotNull(rollbackPlan);
            Assert.Equal(RollbackPlanStatus.FullyReversible, rollbackPlan.Status);
            Assert.Single(rollbackPlan.Operations);
            Assert.Empty(rollbackPlan.Blockers);
            Assert.Single(rollbackPlan.Analysis);
            Assert.Equal(MigrationOperationReversibility.Reversible, rollbackPlan.Analysis[0].Reversibility);
        }

        [Fact]
        public void GeneratesRollbackPlan_InReverseDependencySafeOrder_ForMultipleOperations()
        {
            // Arrange
            var upOperations = new IMigrationOperation[]
            {
        RollbackTestOperationFactory.CreateTable("users"),
        RollbackTestOperationFactory.AddColumn("users", "nickname", "varchar(100)"),
        RollbackTestOperationFactory.CreateIndex("users", "IX_users_nickname", ["nickname"])
            };

            // Act
            var rollbackPlan = _planner.CreateRollbackPlan(upOperations);

            // Assert
            Assert.Equal(RollbackPlanStatus.FullyReversible, rollbackPlan.Status);
            Assert.Equal(3, rollbackPlan.Operations.Count);
            Assert.Empty(rollbackPlan.Blockers);

            // TODO:
            // Once concrete operation types exist, assert exact reverse order:
            // 1. DropIndex
            // 2. DropColumn
            // 3. DropTable
        }

        [Fact]
        public void GeneratesBothUpAndDownPlans_ForMigration()
        {
            // Arrange
            var upOperations = new IMigrationOperation[]
            {
        RollbackTestOperationFactory.CreateTable("users")
            };

            // Act
            var result = _planner.CreatePlanPair(upOperations);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.UpOperations);
            Assert.NotNull(result.RollbackPlan);

            Assert.Single(result.UpOperations);
            Assert.Single(result.RollbackPlan.Operations);
        }

        [Fact]
        public void MarksRollbackPlan_AsFullyReversible_WhenAllOperationsAreReversible()
        {
            // Arrange
            var upOperations = new IMigrationOperation[]
            {
        RollbackTestOperationFactory.CreateTable("users"),
        RollbackTestOperationFactory.AddColumn("users", "nickname", "varchar(100)")
            };

            // Act
            var rollbackPlan = _planner.CreateRollbackPlan(upOperations);

            // Assert
            Assert.Equal(RollbackPlanStatus.FullyReversible, rollbackPlan.Status);
            Assert.Empty(rollbackPlan.Blockers);
            Assert.All(
                rollbackPlan.Analysis,
                item => Assert.Equal(MigrationOperationReversibility.Reversible, item.Reversibility));
        }

        [Fact]
        public void MarksRollbackPlan_AsBlocked_WhenUnknownReversibleOperationExists()
        {
            // Arrange
            var upOperations = new IMigrationOperation[]
            {
                RollbackTestOperationFactory.CreateTable("users"),
                RollbackTestOperationFactory.UnknownReversibilityOperation()
            };

            // Act
            var rollbackPlan = _planner.CreateRollbackPlan(upOperations);

            // Assert
            Assert.Equal(RollbackPlanStatus.Blocked, rollbackPlan.Status);
            Assert.NotEmpty(rollbackPlan.Blockers);
            Assert.Contains(
                rollbackPlan.Analysis,
                item => item.Reversibility == MigrationOperationReversibility.UnknownReversible);
        }

        [Fact]
        public void MarksRollbackPlan_AsBlocked_WhenNonReversibleOperationExists()
        {
            // Arrange
            var operations = new IMigrationOperation[]
            {
                RollbackTestOperationFactory.CreateTable("users"),
                RollbackTestOperationFactory.UnknownReversibilityOperation("not used semantically")
            };

            var resolver = new StubRollbackResolver(operation =>
            {
                var operationTypeName = operation.GetType().Name;

                if (operationTypeName.Contains("CreateTable", StringComparison.Ordinal))
                {
                    return RollbackResolution.Reversible(RollbackTestOperationFactory.DropTable("users"));
                }

                return RollbackResolution.NonReversible("This operation cannot be reversed.");
            });

            var planner = new RollbackMigrationPlanner(resolver);

            // Act
            var rollbackPlan = planner.CreateRollbackPlan(operations);

            // Assert
            Assert.Equal(RollbackPlanStatus.Blocked, rollbackPlan.Status);
            Assert.Contains(
                rollbackPlan.Analysis,
                item => item.Reversibility == MigrationOperationReversibility.NonReversible);
            Assert.NotEmpty(rollbackPlan.Blockers);
        }

        [Fact]
        public void PreservesPerOperationReversibilityAnnotations_InRollbackPlan()
        {
            // Arrange
            var upOperations = new IMigrationOperation[]
            {
                RollbackTestOperationFactory.CreateTable("users"),
                RollbackTestOperationFactory.UnknownReversibilityOperation("Unsupported operation"),
                RollbackTestOperationFactory.NonReversibleOperation("Drop table audit_log without snapshot")
            };

            var resolver = new StubRollbackResolver(operation =>
            {
                return operation switch
                {
                    CreateTableOperation
                        => RollbackResolution.Reversible(RollbackTestOperationFactory.DropTable("users")),

                    RollbackTestOperationFactory.UnknownReversibilityTestOperation unknown
                        => RollbackResolution.Unknown(unknown.Description),

                    RollbackTestOperationFactory.NonReversibleTestOperation nonReversible
                        => RollbackResolution.NonReversible(nonReversible.Description),

                    _ => RollbackResolution.Unknown("Unexpected operation in test")
                };
            });

            var planner = new RollbackMigrationPlanner(resolver);

            // Act
            var rollbackPlan = planner.CreateRollbackPlan(upOperations);

            // Assert
            Assert.Equal(3, rollbackPlan.Analysis.Count);
            Assert.Contains(rollbackPlan.Analysis, x => x.Reversibility == MigrationOperationReversibility.Reversible);
            Assert.Contains(rollbackPlan.Analysis, x => x.Reversibility == MigrationOperationReversibility.UnknownReversible);
            Assert.Contains(rollbackPlan.Analysis, x => x.Reversibility == MigrationOperationReversibility.NonReversible);
        }
        [Fact]
        public void IncludesBlockingReasons_InRollbackPlan()
        {
            // Arrange
            var upOperations = new IMigrationOperation[]
            {
                RollbackTestOperationFactory.UnknownReversibilityOperation(),
                RollbackTestOperationFactory.NonReversibleOperation("Drop table users without snapshot")
            };

            // Act
            var rollbackPlan = _planner.CreateRollbackPlan(upOperations);

            // Assert
            Assert.Equal(RollbackPlanStatus.Blocked, rollbackPlan.Status);
            Assert.NotEmpty(rollbackPlan.Blockers);

            Assert.All(
                rollbackPlan.Analysis.Where(x => x.Reversibility != MigrationOperationReversibility.Reversible),
                item => Assert.False(string.IsNullOrWhiteSpace(item.Reason)));
        }
    }
}
