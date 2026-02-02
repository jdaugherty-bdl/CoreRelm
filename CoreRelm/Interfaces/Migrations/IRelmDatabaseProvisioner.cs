using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Migrations
{

    public interface IRelmDatabaseProvisioner
    {
        Task<bool> DatabaseExistsAsync(string serverConnectionString, string databaseName, CancellationToken ct = default);
        Task EnsureDatabaseExistsAsync(
            string serverConnectionString,
            string databaseName,
            string? charset = null,
            string? collation = null,
            CancellationToken ct = default);
    }
}
