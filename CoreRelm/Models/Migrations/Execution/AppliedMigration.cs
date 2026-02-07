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
        [RelmColumn(columnSize: 255, isNullable: false, unique: true)]
        public string FileName { get; init; }

        [RelmColumn(columnType: "char", columnSize: 64, isNullable: false)]
        public string ChecksumSha256 { get; init; }

        [RelmColumn(isNullable: false)]
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
