using CoreRelm.Models.Migrations.MigrationPlans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Models.Migrations.MigrationPlans
{
    internal sealed record GeneratedMigrationArtifact(
        MigrationPlan Plan,
        string? Sql,
        bool HasChanges,
        string Message);
}
