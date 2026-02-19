using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Tooling.Generation
{
    public sealed record MigrationGenerationResult(
        string SetName,
        DateTime TimestampUtc,
        IReadOnlyDictionary<string, PerDatabaseMigrationResult> ByDatabase,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Errors
    );
}
