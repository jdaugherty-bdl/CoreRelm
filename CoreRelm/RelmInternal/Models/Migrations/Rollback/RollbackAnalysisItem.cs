using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.MigrationEnums;

namespace CoreRelm.RelmInternal.Models.Migrations.Rollback
{
    internal sealed class RollbackAnalysisItem
    {
        public required string OperationType { get; init; }
        public required MigrationOperationReversibility Reversibility { get; init; }
        public string? Reason { get; init; }
    }
}
