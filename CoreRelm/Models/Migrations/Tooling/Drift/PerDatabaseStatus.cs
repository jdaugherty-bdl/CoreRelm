using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Tooling.Drift
{
    public sealed record PerDatabaseStatus(
        string DatabaseName,
        bool DatabaseExists,
        int AppliedCount,
        int PendingCount,
        IReadOnlyList<string> DriftFiles,
        IReadOnlyList<string> Warnings
    );
}
