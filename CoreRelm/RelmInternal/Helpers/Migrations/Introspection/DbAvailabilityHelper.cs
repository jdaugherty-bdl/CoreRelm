using CoreRelm.Interfaces.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Migrations.Introspection
{
    public static class DbAvailabilityHelper
    {
        public static async Task<bool> EnsureForApplyOrMigrateAsync(
            IRelmDatabaseProvisioner provisioner,
            string serverConnectionString,
            string databaseName,
            Action<string> logInfo,
            Action<string> logWarn,
            CancellationToken ct)
        {
            try
            {
                await provisioner.EnsureDatabaseExistsAsync(
                    serverConnectionString: serverConnectionString,
                    databaseName: databaseName,
                    charset: "utf8mb4",
                    collation: "utf8mb4_0900_ai_ci",
                    ct: ct);

                logInfo($"Database ensured: `{databaseName}`");
                return true;
            }
            catch (Exception ex)
            {
                // Apply/migrate path: this is a real failure (permissions, connectivity, etc.)
                // You can either throw or return false and let caller set exit code.
                logWarn($"FAILED to ensure database `{databaseName}`: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> WarnIfMissingAsync(
            IRelmDatabaseProvisioner provisioner,
            string serverConnectionString,
            string databaseName,
            Action<string> logWarn,
            CancellationToken ct)
        {
            try
            {
                var exists = await provisioner.DatabaseExistsAsync(serverConnectionString, databaseName, ct);
                if (!exists)
                {
                    logWarn($"Database `{databaseName}` does not exist. It will be created during apply/db migrate.");
                }
                return exists;
            }
            catch (Exception ex)
            {
                // Non-apply path: never error out; just warn.
                logWarn($"Could not verify existence of database `{databaseName}` (will be created during apply/db migrate): {ex.Message}");
                return false;
            }
        }
    }

}
