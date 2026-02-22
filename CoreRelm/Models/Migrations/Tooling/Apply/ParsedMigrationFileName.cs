using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Tooling.Apply
{
    public sealed record ParsedMigrationFileName(
        string DatabaseName,
        string FileName,
        DateTime? TimestampUtc,
        string? MigrationSlug,
        string SortKey
    );
}
