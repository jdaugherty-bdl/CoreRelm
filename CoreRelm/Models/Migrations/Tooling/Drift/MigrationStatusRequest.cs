using CoreRelm.Models.Migrations.Tooling.Apply;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Tooling.Drift
{
    public sealed record MigrationStatusRequest(
        string ConnectionString,
        string ConnectionStringTemplate,
        bool WarnIfDatabaseMissing,
        IReadOnlyDictionary<string, IReadOnlyList<MigrationScript>> ScriptsByDatabase,
        StatusOptions Options
    );
}
