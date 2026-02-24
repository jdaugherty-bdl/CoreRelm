using CoreRelm.Models.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.CharsetEnums;

namespace CoreRelm.Interfaces.Migrations
{

    public interface IRelmDatabaseProvisioner
    {
        Task<bool> DatabaseExistsAsync(MigrationOptions migrationOptions);

        Task InitializeEmptyDatabaseAsync(
            MigrationOptions migrationOptions,
            DatabaseCharset? charset = null,
            DatabaseCollation? collation = null);

        Task<bool> EnsureForApplyOrMigrateAsync(
            MigrationOptions migrationOptions,
            Action<string, object[]?> logInfo,
            Action<string, object[]?> logWarn);

        Task<bool> WarnIfMissingAsync(
            MigrationOptions migrationOptions,
            Action<string> logWarn);
    }
}
