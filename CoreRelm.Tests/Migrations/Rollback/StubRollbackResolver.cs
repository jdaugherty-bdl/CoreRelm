using CoreRelm.Interfaces.Migrations;
using CoreRelm.Interfaces.Migrations.Rollback;
using CoreRelm.RelmInternal.Helpers.Migrations.Rollback;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Migrations.Rollback
{
    internal sealed class StubRollbackResolver : IMigrationOperationRollbackResolver
    {
        private readonly Func<IMigrationOperation, RollbackResolution> _resolve;

        public StubRollbackResolver(Func<IMigrationOperation, RollbackResolution> resolve)
        {
            _resolve = resolve;
        }

        public RollbackResolution Resolve(IMigrationOperation operation) => _resolve(operation);
    }
}
