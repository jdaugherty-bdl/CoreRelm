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
        /*
        Task<MigrationGenerateResult> Generate(
            string migrationName,
            string stampUtc,
            string dbName,
            List<ValidatedModelType> modelsForDb,
            MySqlDatabaseProvisioner provisioner);
        */
        MigrationGenerateResult Generate(
            MigrationOptions migrationOptions,
            string migrationName,
            DateTime stampUtc,
            string dbName,
            List<ValidatedModelType> modelsForDb);

        Task<MigrationGenerateResult> GenerateAsync(
            MigrationOptions migrationOptions,
            string migrationName,
            DateTime stampUtc,
            string dbName,
            List<ValidatedModelType> modelsForDb);
    }
}
