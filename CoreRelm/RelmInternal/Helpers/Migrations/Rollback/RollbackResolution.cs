using CoreRelm.Interfaces.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.MigrationEnums;

namespace CoreRelm.RelmInternal.Helpers.Migrations.Rollback
{
    internal sealed class RollbackResolution
    {
        public required MigrationOperationReversibility Reversibility { get; init; }

        public IMigrationOperation? ReverseOperation { get; init; }

        public IReadOnlyList<string> Blockers { get; init; } = Array.Empty<string>();

        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

        public string? Reason { get; init; }

        public static RollbackResolution Reversible(IMigrationOperation reverseOperation)
        {
            ArgumentNullException.ThrowIfNull(reverseOperation);

            return new RollbackResolution
            {
                Reversibility = MigrationOperationReversibility.Reversible,
                ReverseOperation = reverseOperation
            };
        }

        public static RollbackResolution Unknown(string reason)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(reason);

            return new RollbackResolution
            {
                Reversibility = MigrationOperationReversibility.UnknownReversible,
                Reason = reason,
                Blockers = new[] { reason }
            };
        }

        public static RollbackResolution NonReversible(string reason)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(reason);

            return new RollbackResolution
            {
                Reversibility = MigrationOperationReversibility.NonReversible,
                Reason = reason,
                Blockers = new[] { reason }
            };
        }
    }
}
