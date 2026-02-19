using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Tooling.Generation
{
    public sealed record PerDatabaseMigrationResult(
        string DatabaseName,
        bool HasChanges,
        string? Sql,                     // null if no changes
        IReadOnlyList<string> Messages,   // includes "No changes detected..." etc.
        int OperationCount,
        IReadOnlyList<string> Blockers
    );
}
