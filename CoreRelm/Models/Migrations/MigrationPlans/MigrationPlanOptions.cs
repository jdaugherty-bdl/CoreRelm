using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.MigrationPlans
{
    public sealed record MigrationPlanOptions(
        bool DropFunctionsOnCreate,
        bool Destructive,
        string MigrationName,
        string MigrationFileName,
        string ModelSetName,
        ISet<string> ScopeTables,
        DateTime StampUtc
    );
}
