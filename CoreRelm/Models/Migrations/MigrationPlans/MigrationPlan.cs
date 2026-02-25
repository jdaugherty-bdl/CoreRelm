using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.MigrationEnums;

namespace CoreRelm.Models.Migrations.MigrationPlans
{
    public sealed record MigrationPlan(
        string DatabaseName,
        string MigrationName,
        string MigrationFileName,
        string ModelSetName,
        RelmMigrationType MigrationType,
        IReadOnlyList<IMigrationOperation> Operations,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Blockers,
        DateTime StampUtc
    )
    {
        public bool HasChanges => Operations.Count > 0 || Blockers.Count > 0;
    }
}
