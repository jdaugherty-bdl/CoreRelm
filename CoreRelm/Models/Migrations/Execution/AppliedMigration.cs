using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Execution
{
    [RelmDatabase("ledgerlite")]
    [RelmTable("schema_migrations")]
    public class AppliedMigration : RelmModel
    {
        [RelmColumn]
        public string FileName { get; init; }

        [RelmColumn]
        public string ChecksumSha256 { get; init; }

        [RelmColumn]
        public DateTime AppliedUtc { get; init; }

        public AppliedMigration() 
        { 
            FileName = string.Empty;
            ChecksumSha256 = string.Empty;
            AppliedUtc = DateTime.UtcNow;
        }

        public AppliedMigration(
            string fileName, 
            string checksumSha256, 
            DateTime appliedUtc
        )
        {
            FileName = fileName;
            ChecksumSha256 = checksumSha256;
            AppliedUtc = appliedUtc;
        }
    }
}
