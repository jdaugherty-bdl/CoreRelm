using CoreRelm.Models.Migrations;
using CoreRelm.RelmInternal.Helpers.Migrations.Provisioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Migrations
{
    public interface IMigrationSqlProvider
    {
        Task<MigrationGenerateResult> Generate(
            string migrationName,
            string stampUtc,
            string setName,
            string dbName,
            List<ValidatedModelType> modelsForDb,
            bool destructive,
            CancellationToken cancellationToken,
            bool quiet,
            bool apply,
            MySqlDatabaseProvisioner provisioner);
    }
}
