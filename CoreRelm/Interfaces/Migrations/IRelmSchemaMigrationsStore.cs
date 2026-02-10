using CoreRelm.Models;
using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Migrations
{
    public interface IRelmSchemaMigrationsStore
    {
        Task<int> EnsureSchemaMigrationTableAsync(IRelmContext context, MigrationOptions migrationOptions);
        Task<Dictionary<string, AppliedMigration>?> GetAppliedMigrationsAsync(RelmContext context, CancellationToken ct = default);
        Task<int> RecordAppliedMigrationAsync(IRelmContext context, string migrationFile, string checksumSha256, CancellationToken ct = default);
    }
}
