using CoreRelm.Models.Migrations.Tooling.Apply;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Migrations.MigrationFiles
{
    public interface IMigrationScriptHeaderParser
    {
        void ValidateHeader(string headerVersion);
        bool TryParseHeader(string sqlText, out ParsedMigrationHeader header, out string? error);
    }
}
