using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Models.Migrations.Rollback
{
    internal sealed record RollbackMigrationPlanMetadata(
        string DatabaseName,
        string MigrationName,
        string MigrationFileName,
        string ModelSetName,
        DateTime StampUtc);
}
