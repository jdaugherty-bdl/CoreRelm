using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Tooling.Apply
{
    public sealed record ParsedMigrationHeader(
        string? Tool,                 // e.g. "CoreRelm"
        string? ToolVersion,
        string? MigrationName,
        string? ModelSetName,
        string? DatabaseName,
        DateTime? GeneratedUtc,
        string? ChecksumSha256,
        IReadOnlyDictionary<string, string> Extras
    );
}
