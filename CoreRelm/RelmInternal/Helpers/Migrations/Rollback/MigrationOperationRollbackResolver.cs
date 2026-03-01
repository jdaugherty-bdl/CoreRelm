using CoreRelm.Interfaces.Migrations;
using CoreRelm.Interfaces.Migrations.Rollback;
using CoreRelm.Models.Migrations.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Migrations.Rollback
{
    internal sealed class MigrationOperationRollbackResolver : IMigrationOperationRollbackResolver
    {
        public RollbackResolution Resolve(IMigrationOperation operation)
        {
            ArgumentNullException.ThrowIfNull(operation);

            // TODO:
            // Replace this name-based placeholder logic with real strongly-typed operation handling.
            // The purpose of this starter skeleton is to compile and give the TDD cycle a place to start.
            var operationTypeName = operation.GetType().Name;

            if (operationTypeName.Contains("CreateTable", StringComparison.Ordinal))
            {
                return RollbackResolution.Reversible(CreatePlaceholderReverseOperation("DropTable"));
            }

            if (operationTypeName.Contains("AddColumn", StringComparison.Ordinal))
            {
                return RollbackResolution.Reversible(CreatePlaceholderReverseOperation("DropColumn"));
            }

            if (operationTypeName.Contains("CreateIndex", StringComparison.Ordinal))
            {
                return RollbackResolution.Reversible(CreatePlaceholderReverseOperation("DropIndex"));
            }

            if (operationTypeName.Contains("AddForeignKey", StringComparison.Ordinal))
            {
                return RollbackResolution.Reversible(CreatePlaceholderReverseOperation("DropForeignKey"));
            }

            if (operationTypeName.Contains("CreateTrigger", StringComparison.Ordinal))
            {
                return RollbackResolution.Reversible(CreatePlaceholderReverseOperation("DropTrigger"));
            }

            if (operationTypeName.Contains("CreateFunction", StringComparison.Ordinal))
            {
                return RollbackResolution.Reversible(CreatePlaceholderReverseOperation("DropFunction"));
            }

            if (operationTypeName.Contains("DropIndex", StringComparison.Ordinal))
            {
                if (operation is DropIndexOperation dropIndexOperation && dropIndexOperation.Description is not null)
                {
                    return RollbackResolution.Reversible(
                        CreatePlaceholderReverseOperation("CreateIndex"));
                }

                return RollbackResolution.Unknown(
                    $"Rollback for operation type '{operationTypeName}' requires original definition metadata.");
            }

            if (operationTypeName.Contains("DropColumn", StringComparison.Ordinal))
            {
                return RollbackResolution.Unknown(
                    $"Rollback for operation type '{operationTypeName}' requires original column definition metadata.");
            }

            if (operationTypeName.Contains("AlterColumn", StringComparison.Ordinal))
            {
                return RollbackResolution.Unknown(
                    $"Rollback for operation type '{operationTypeName}' requires prior full column definition metadata.");
            }

            if (operationTypeName.Contains("DropTable", StringComparison.Ordinal))
            {
                return RollbackResolution.NonReversible(
                    $"Rollback for operation type '{operationTypeName}' cannot be safely generated without original schema snapshot metadata.");
            }
            if (operationTypeName.Contains("DropForeignKey", StringComparison.Ordinal))
            {
                if (operation.Description is not null)
                {
                    return RollbackResolution.Reversible(
                        CreatePlaceholderReverseOperation("AddForeignKey"));
                }

                return RollbackResolution.Unknown(
                    $"Rollback for operation type '{operationTypeName}' requires original foreign key definition metadata.");
            }

            if (operationTypeName.Contains("DropTrigger", StringComparison.Ordinal))
            {
                if (operation.Description is not null)
                {
                    return RollbackResolution.Reversible(
                        CreatePlaceholderReverseOperation("CreateTrigger"));
                }

                return RollbackResolution.Unknown(
                    $"Rollback for operation type '{operationTypeName}' requires original trigger definition metadata.");
            }

            if (operationTypeName.Contains("DropFunction", StringComparison.Ordinal))
            {
                if (operation.Description is not null)
                {
                    return RollbackResolution.Reversible(
                        CreatePlaceholderReverseOperation("CreateFunction"));
                }

                return RollbackResolution.Unknown(
                    $"Rollback for operation type '{operationTypeName}' requires original function definition metadata.");
            }

            return RollbackResolution.Unknown(
                $"Rollback behavior for operation type '{operationTypeName}' has not been implemented yet.");
        }

        private static IMigrationOperation CreatePlaceholderReverseOperation(string reverseOperationName)
        {
            return new PlaceholderMigrationOperation(reverseOperationName);
        }

        /// <summary>
        /// Temporary compile-time placeholder so the resolver can return something
        /// before real reverse operation construction is implemented.
        /// </summary>
        private sealed class PlaceholderMigrationOperation : IMigrationOperation
        {
            public string Description { get; private set; }

            public PlaceholderMigrationOperation(string operationTypeName)
            {
                OperationTypeName = operationTypeName;
            }

            public string OperationTypeName { get; }
        }
    }
}
