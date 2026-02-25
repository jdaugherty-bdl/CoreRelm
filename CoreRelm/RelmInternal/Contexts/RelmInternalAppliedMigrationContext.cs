using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.Models.Migrations.Execution;
using CoreRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Contexts
{
    internal class RelmInternalAppliedMigrationContext(RelmContextOptions contextOptions) : RelmContext(contextOptions)
    {
        public IRelmDataSet<AppliedMigration>? AppliedMigrations { get; set; }
    }
}
