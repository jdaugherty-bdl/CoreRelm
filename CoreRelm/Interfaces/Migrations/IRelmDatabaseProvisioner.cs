using CoreRelm.Models.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Migrations
{

    public interface IRelmDatabaseProvisioner
    {
        Task<bool> DatabaseExistsAsync(MigrationOptions migrationOptions);
        Task InitializeEmptyDatabaseAsync(
            MigrationOptions migrationOptions,
            string? charset = null,
            string? collation = null);
    }
}
