using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Tooling.Apply
{
    public sealed record PerDatabaseApplyResult(
        string DatabaseName,
        int AppliedCount,
        int SkippedAlreadyAppliedCount,
        IReadOnlyList<string> Errors
    );
}
