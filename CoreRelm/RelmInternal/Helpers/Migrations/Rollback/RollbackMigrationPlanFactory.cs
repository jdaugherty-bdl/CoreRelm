using CoreRelm.Models.Migrations.MigrationPlans;
using CoreRelm.RelmInternal.Models.Migrations.Rollback;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.MigrationEnums;

namespace CoreRelm.RelmInternal.Helpers.Migrations.Rollback
{
    internal sealed class RollbackMigrationPlanFactory
    {
        public MigrationPlan Create(
            RollbackPlan rollbackPlan,
            RollbackMigrationPlanMetadata metadata)
        {
            ArgumentNullException.ThrowIfNull(rollbackPlan);
            ArgumentNullException.ThrowIfNull(metadata);

            return new MigrationPlan(
                DatabaseName: metadata.DatabaseName,
                MigrationName: metadata.MigrationName,
                MigrationFileName: metadata.MigrationFileName,
                ModelSetName: metadata.ModelSetName,
                MigrationType: RelmMigrationType.MigrationRollback,
                Operations: rollbackPlan.Operations,
                Warnings: rollbackPlan.Warnings,
                Blockers: rollbackPlan.Blockers,
                StampUtc: metadata.StampUtc
            );
        }
    }
}
