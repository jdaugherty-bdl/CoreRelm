using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.MigrationEnums;

namespace CoreRelm.Models.Migrations.Execution
{
    [RelmDatabase("ledgerlite")]
    [RelmTable("schema_migrations")]
    public class AppliedMigration : RelmModel
    {
        [RelmColumn(columnSize: 255, isNullable: false, unique: true)]
        public string FileName { get; init; }

        [RelmIndex]
        [RelmColumn(columnSize: 128, isNullable: false, defaultValue: "Migration")]
        public RelmMigrationType MigrationType { get; init; } = RelmMigrationType.Migration;

        [RelmColumn(columnType: "char", columnSize: 64, isNullable: false)]
        public string ChecksumSha256 { get; init; }

        [RelmColumn(isNullable: false)]
        public DateTime AppliedUtc { get; init; }

        public AppliedMigration() 
        { 
            FileName = string.Empty;
            MigrationType = RelmMigrationType.Migration;
            ChecksumSha256 = string.Empty;
            AppliedUtc = DateTime.UtcNow;
        }

        public AppliedMigration(
            string fileName, 
            RelmMigrationType migrationType,
            string checksumSha256, 
            DateTime appliedUtc
        )
        {
            FileName = fileName;
            MigrationType = migrationType;
            ChecksumSha256 = checksumSha256;
            AppliedUtc = appliedUtc;
        }
    }
}
