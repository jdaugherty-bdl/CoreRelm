using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Enums
{
    public class MigrationEnums
    {
        public enum RelmMigrationType
        {
            None,
            Migration,
            MigrationRollback,
            SystemMigration
        }

        public enum MigrationOperationReversibility
        {
            Reversible = 0,
            UnknownReversible = 1,
            NonReversible = 2
        }

        public enum RollbackPlanStatus
        {
            FullyReversible = 0,
            Blocked = 1
        }
    }
}
