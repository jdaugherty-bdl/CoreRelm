using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Migrations
{

    public interface IRelmMigrationExecutor
    {
        Task ExecuteSqlAsync(string connectionString, string sql, CancellationToken ct = default);
    }
}
