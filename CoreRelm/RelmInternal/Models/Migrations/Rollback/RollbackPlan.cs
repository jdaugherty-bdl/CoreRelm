using CoreRelm.Interfaces.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.MigrationEnums;

namespace CoreRelm.RelmInternal.Models.Migrations.Rollback
{
    internal sealed class RollbackPlan
    {
        public required RollbackPlanStatus Status { get; init; }
        public required IReadOnlyList<IMigrationOperation> Operations { get; init; }
        public required IReadOnlyList<RollbackAnalysisItem> Analysis { get; init; }
        public required IReadOnlyList<string> Blockers { get; init; }
        public required IReadOnlyList<string> Warnings { get; init; }
    }
}
