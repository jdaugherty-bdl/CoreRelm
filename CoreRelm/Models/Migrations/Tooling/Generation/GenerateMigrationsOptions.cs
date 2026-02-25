using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Tooling.Generation
{
    public sealed record GenerateMigrationsOptions(
        string? ConnectionString = null,
        string? ConnectionStringTemplate = null,
        string? DatabaseName = null,
        bool Destructive = false,
        bool IncludeUseDatabase = true,
        bool EnsureDatabaseExistsDuringGenerate = true,
        bool DropFunctionsOnCreate = false,
        string MigrationName = "migration",
        string MigrationFileName = "migration.sql",
        DateTime? TimestampUtc = null,
        // Host policy: if DB missing and not applying, treat actual schema as empty and warn
        bool TreatMissingDatabaseAsEmpty = true,
        CancellationToken CancelToken = default
    );
}
