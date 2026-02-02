using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Execution
{
    public class AppliedMigration : RelmModel
    {
        public string FileName { get; init; }
        public string ChecksumSha256 { get; init; }
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
