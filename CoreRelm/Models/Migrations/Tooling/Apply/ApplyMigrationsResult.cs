using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Tooling.Apply
{
    public sealed record ApplyMigrationsResult(
        IReadOnlyDictionary<string, PerDatabaseApplyResult> ByDatabase,
        bool AnyFailures
    );
}
