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
        MigrationGenerateResult Generate(List<ValidatedModelType> modelsForDb);

        Task<MigrationGenerateResult> GenerateAsync(List<ValidatedModelType> modelsForDb);
    }
}
