using CoreRelm.Models.Migrations.Tooling.Apply;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Migrations.MigrationFiles
{
    public interface IMigrationFileNameParser
    {
        bool TryParse(string fileName, out ParsedMigrationFileName parsed, out string? error);
    }
}
