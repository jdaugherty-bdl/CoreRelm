using CoreRelm.Interfaces.Migrations;
using CoreRelm.RelmInternal.Helpers.Migrations.Rollback;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.MigrationEnums;

namespace CoreRelm.Tests.Migrations.Rollback
{
    public sealed class MigrationOperationRollbackResolverTests
    {
        private readonly MigrationOperationRollbackResolver _resolver = new();

        [Fact]
        public void CreateTable_IsReversible_ToDropTable()
        {
            // Arrange
            var operation = RollbackTestOperationFactory.CreateTable("users");

            // Act
            var result = _resolver.Resolve(operation);

            // Assert
            Assert.Equal(MigrationOperationReversibility.Reversible, result.Reversibility);
            Assert.NotNull(result.ReverseOperation);
            Assert.Empty(result.Blockers);

            // TODO:
            // Assert exact reverse operation type once the concrete migration operation types are finalized.
            // Example:
            // var reverse = Assert.IsType<DropTableOperation>(result.ReverseOperation);
            // Assert.Equal("users", reverse.TableName);
        }

        [Fact]
        public void AddColumn_IsReversible_ToDropColumn()
        {
            // Arrange
            var operation = RollbackTestOperationFactory.AddColumn(
                tableName: "users",
                columnName: "nickname",
                storeType: "varchar(100)");

            // Act
            var result = _resolver.Resolve(operation);

            // Assert
            Assert.Equal(MigrationOperationReversibility.Reversible, result.Reversibility);
            Assert.NotNull(result.ReverseOperation);
            Assert.Empty(result.Blockers);

            // TODO:
            // Assert exact reverse op details.

        }

        [Fact]
        public void CreateIndex_IsReversible_ToDropIndex()
        {
            // Arrange
            var operation = RollbackTestOperationFactory.CreateIndex(
                tableName: "users",
                indexName: "IX_users_email",
                columns: ["email"]);

            // Act
            var result = _resolver.Resolve(operation);

            // Assert
            Assert.Equal(MigrationOperationReversibility.Reversible, result.Reversibility);
            Assert.NotNull(result.ReverseOperation);
            Assert.Empty(result.Blockers);
        }

        [Fact]
        public void AddForeignKey_IsReversible_ToDropForeignKey()
        {
            // Arrange
            var operation = RollbackTestOperationFactory.AddForeignKey(
                tableName: "orders",
                foreignKeyName: "FK_orders_users_user_id",
                columnName: "user_id",
                principalTable: "users",
                principalColumn: "id");

            // Act
            var result = _resolver.Resolve(operation);

            // Assert
            Assert.Equal(MigrationOperationReversibility.Reversible, result.Reversibility);
            Assert.NotNull(result.ReverseOperation);
            Assert.Empty(result.Blockers);
        }

        [Fact]
        public void UnsupportedOperation_IsBlocked_AsUnknownReversible()
        {
            // Arrange
            var operation = RollbackTestOperationFactory.UnknownReversibilityOperation();

            // Act
            var result = _resolver.Resolve(operation);

            // Assert
            Assert.Equal(MigrationOperationReversibility.UnknownReversible, result.Reversibility);
            Assert.Null(result.ReverseOperation);
            Assert.NotEmpty(result.Blockers);
        }

        [Fact]
        public void DropIndex_WithOriginalDefinition_IsReversible_ToCreateIndex()
        {
            // Arrange
            var operation = RollbackTestOperationFactory.DropIndexWithOriginalDefinition(
                tableName: "users",
                indexName: "IX_users_email",
                columns: ["email"]);

            // Act
            var result = _resolver.Resolve(operation);

            // Assert
            Assert.Equal(MigrationOperationReversibility.Reversible, result.Reversibility);
            Assert.NotNull(result.ReverseOperation);
            Assert.Empty(result.Blockers);

            // TODO:
            // Assert exact recreated index definition once concrete operation types are wired in.
        }

        [Fact]
        public void DropForeignKey_WithOriginalDefinition_IsReversible_ToAddForeignKey()
        {
            // Arrange
            var operation = RollbackTestOperationFactory.DropForeignKeyWithOriginalDefinition(
                tableName: "orders",
                foreignKeyName: "FK_orders_users_user_id",
                columnName: "user_id",
                principalTable: "users",
                principalColumn: "id");

            // Act
            var result = _resolver.Resolve(operation);

            // Assert
            Assert.Equal(MigrationOperationReversibility.Reversible, result.Reversibility);
            Assert.NotNull(result.ReverseOperation);
            Assert.Empty(result.Blockers);

            // TODO:
            // Assert exact reverse operation type/details once real reverse operation construction exists.
            // Example:
            // var reverse = Assert.IsType<AddForeignKeyOperation>(result.ReverseOperation);
            // Assert.Equal("orders", reverse.TableName);
        }

        [Fact]
        public void DropTrigger_WithOriginalDefinition_IsReversible_ToCreateTrigger()
        {
            // Arrange
            var operation = RollbackTestOperationFactory.DropTriggerWithOriginalDefinition(
                triggerName: "trg_users_before_insert",
                tableName: "users",
                bodySql: """
                 BEGIN
                     SET NEW.created_utc = UTC_TIMESTAMP();
                 END
                 """);

            // Act
            var result = _resolver.Resolve(operation);

            // Assert
            Assert.Equal(MigrationOperationReversibility.Reversible, result.Reversibility);
            Assert.NotNull(result.ReverseOperation);
            Assert.Empty(result.Blockers);

            // TODO:
            // Assert exact reverse operation type/details once real reverse operation construction exists.
        }

        [Fact]
        public void DropFunction_WithOriginalDefinition_IsReversible_ToCreateFunction()
        {
            // Arrange
            var operation = RollbackTestOperationFactory.DropFunctionWithOriginalDefinition(
                functionName: "fn_normalize_email",
                bodySql: """
                 RETURNS VARCHAR(255)
                 DETERMINISTIC
                 BEGIN
                     RETURN LOWER(TRIM('test@example.com'));
                 END
                 """);

            // Act
            var result = _resolver.Resolve(operation);

            // Assert
            Assert.Equal(MigrationOperationReversibility.Reversible, result.Reversibility);
            Assert.NotNull(result.ReverseOperation);
            Assert.Empty(result.Blockers);

            // TODO:
            // Assert exact reverse operation type/details once real reverse operation construction exists.
        }
    }
}
