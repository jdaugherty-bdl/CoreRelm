using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Tooling.Drift
{
    public sealed record MigrationStatusResult(
        IReadOnlyDictionary<string, PerDatabaseStatus> ByDatabase,
        bool AnyDrift
    );
}
