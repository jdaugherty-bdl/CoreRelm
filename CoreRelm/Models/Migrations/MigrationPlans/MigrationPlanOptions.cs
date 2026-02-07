using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.MigrationPlans
{
    public sealed record MigrationPlanOptions(
        bool Destructive,
        ISet<string> ScopeTables,
        DateTime StampUtc
    );
}
