using CoreRelm.Models.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Migrations
{
    public interface IRelmMigrationSqlProviderFactory
    {
        IMigrationSqlProvider CreateProvider(MigrationOptions migrationOptions);
    }
}
