using CoreRelm.Models.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Migrations.Execution
{
    public interface IMigrationApplier
    {
        Task<bool> ApplyAsync(MigrationOptions migrationOptions, string migrationFileName, string sql);
    }
}
