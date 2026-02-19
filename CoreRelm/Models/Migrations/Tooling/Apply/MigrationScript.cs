using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Tooling.Apply
{
    public sealed record MigrationScript(
        string FileName,
        string SqlText,
        string ChecksumSha256 // computed by host or tooling
    );
}
