using CoreRelm.Interfaces.Migrations;
using CoreRelm.Interfaces.Migrations.Rollback;
using CoreRelm.RelmInternal.Models.Migrations.Rollback;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.MigrationEnums;

namespace CoreRelm.RelmInternal.Helpers.Migrations.Rollback
{
    internal sealed class RollbackMigrationPlanner(IMigrationOperationRollbackResolver resolver)
    {
        private readonly IMigrationOperationRollbackResolver _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));

        public RollbackPlan CreateRollbackPlan(IReadOnlyList<IMigrationOperation> upOperations)
        {
            ArgumentNullException.ThrowIfNull(upOperations);

            var reverseOperations = new List<IMigrationOperation>();
            var analysis = new List<RollbackAnalysisItem>();
            var blockers = new List<string>();
            var warnings = new List<string>();

            // Reverse iteration is the starting point for dependency-safe rollback ordering.
            // TODO:
            // Refine ordering rules later for FKs, triggers, functions, and more complex graph dependencies.
            for (var i = upOperations.Count - 1; i >= 0; i--)
            {
                var upOperation = upOperations[i];
                var resolution = _resolver.Resolve(upOperation);

                analysis.Add(new RollbackAnalysisItem
                {
                    OperationType = upOperation.GetType().Name,
                    Reversibility = resolution.Reversibility,
                    Reason = resolution.Reason
                });

                if (resolution.ReverseOperation is not null)
                {
                    reverseOperations.Add(resolution.ReverseOperation);
                }

                if (resolution.Blockers.Count > 0)
                {
                    blockers.AddRange(resolution.Blockers);
                }

                if (resolution.Warnings.Count > 0)
                {
                    warnings.AddRange(resolution.Warnings);
                }
            }

            var status = blockers.Count == 0
                ? RollbackPlanStatus.FullyReversible
                : RollbackPlanStatus.Blocked;

            return new RollbackPlan
            {
                Status = status,
                Operations = reverseOperations,
                Analysis = analysis,
                Blockers = blockers,
                Warnings = warnings
            };
        }

        public RollbackPlanPair CreatePlanPair(IReadOnlyList<IMigrationOperation> upOperations)
        {
            ArgumentNullException.ThrowIfNull(upOperations);

            return new RollbackPlanPair
            {
                UpOperations = upOperations,
                RollbackPlan = CreateRollbackPlan(upOperations)
            };
        }
    }
}
