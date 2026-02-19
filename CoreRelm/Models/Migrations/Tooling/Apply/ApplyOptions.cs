using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Tooling.Apply
{
    public sealed record ApplyOptions(
        bool EnsureDatabaseExists = true,
        bool StopOnFirstFailurePerDatabase = true,
        bool ContinueOtherDatabasesOnFailure = true
    );
}
