using CoreRelm.Interfaces.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Models.Migrations.Rollback
{
    internal sealed class RollbackPlanPair
    {
        public required IReadOnlyList<IMigrationOperation> UpOperations { get; init; }

        public required RollbackPlan RollbackPlan { get; init; }
    }
}
