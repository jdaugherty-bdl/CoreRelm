using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.CharsetEnums;

namespace CoreRelm.RelmInternal.Helpers.Migrations.Introspection
{
    public static class DbAvailabilityHelper
    {
        public static async Task<bool> EnsureForApplyOrMigrateAsync(
            MigrationOptions migrationOptions,
            IRelmDatabaseProvisioner provisioner,
            Action<string, object[]?> logInfo,
            Action<string, object[]?> logWarn)
        {
            try
            {
                await provisioner.InitializeEmptyDatabaseAsync(
                    migrationOptions: migrationOptions,
                    charset: DatabaseCharset.Utf8mb4,
                    collation: DatabaseCollation.Utf8mb40900AiCi);

                logInfo("Database ensured: `{DatabaseName}`", [migrationOptions.DatabaseName]);
                return true;
            }
            catch (Exception ex)
            {
                // Apply/migrate path: this is a real failure (permissions, connectivity, etc.)
                // You can either throw or return false and let caller set exit code.
                logWarn("FAILED to ensure database `{DatabaseName}`: {ErrorMessage}", [migrationOptions.DatabaseName, ex.Message]);
                return false;
            }
        }

        public static async Task<bool> WarnIfMissingAsync(
            MigrationOptions migrationOptions,
            IRelmDatabaseProvisioner provisioner,
            Action<string> logWarn)
        {
            try
            {
                var exists = await provisioner.DatabaseExistsAsync(migrationOptions);
                if (!exists)
                {
                    logWarn($"Database `{migrationOptions.DatabaseName}` does not exist. It will be created during apply/db migrate.");
                }
                return exists;
            }
            catch (Exception ex)
            {
                // Non-apply path: never error out; just warn.
                logWarn($"Could not verify existence of database `{migrationOptions.DatabaseName}` (will be created during apply/db migrate): {ex.Message}");
                return false;
            }
        }
    }

}
