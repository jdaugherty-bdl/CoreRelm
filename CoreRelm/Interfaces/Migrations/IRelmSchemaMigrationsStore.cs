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
        Task<int> EnsureTableAsync(IRelmContext context, CancellationToken ct = default);
        Task<Dictionary<string, AppliedMigration>> GetAppliedAsync(IRelmContext context, CancellationToken ct = default);
        Task<int> RecordAppliedAsync(IRelmContext context, string migrationFile, string checksumSha256, CancellationToken ct = default);
    }
}
