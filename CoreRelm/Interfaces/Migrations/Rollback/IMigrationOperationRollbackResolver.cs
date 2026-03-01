using CoreRelm.RelmInternal.Helpers.Migrations.Rollback;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Migrations.Rollback
{
    internal interface IMigrationOperationRollbackResolver
    {
        RollbackResolution Resolve(IMigrationOperation operation);
    }
}
