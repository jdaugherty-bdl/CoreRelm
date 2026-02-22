using CoreRelm.Interfaces.Migrations.MigrationFiles;
using CoreRelm.Models.Migrations.Tooling.Apply;
using System;
using System.Globalization;
using System.IO;

namespace CoreRelm.Migrations.MigrationFiles
{
    /// <summary>
    /// Parses migration file names with the formats:
    ///   RelmMigration_yyyyMMdd_HHmmss_{safeName}__db-{databaseName}.sql
    ///   SYSTEM_MIGRATION_yyyyMMdd_HHmmss_{safeName}__db-{databaseName}.sql
    ///
    /// Notes:
    /// - Accepts filename only; if a path is provided, Path.GetFileName() is used.
    /// - databaseName allowed chars: [A-Za-z0-9_]+ (underscores allowed; dashes/dots rejected).
    /// </summary>
    public sealed class MigrationFileNameParser : IMigrationFileNameParser
    {
        private const string RegularPrefix = "RelmMigration_";
        private const string SystemPrefix = "SYSTEM_MIGRATION_";
        private const string DbMarker = "__db-";
        private const string TimestampFormat = "yyyyMMdd_HHmmss";

        public bool TryParse(string fileName, out ParsedMigrationFileName parsed, out string? error)
        {
            parsed = new ParsedMigrationFileName(
                DatabaseName: "",
                FileName: "",
                TimestampUtc: null,
                MigrationSlug: null,
                SortKey: ""
            );
            error = null;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                error = "Filename is empty.";
                return false;
            }

            var nameOnly = Path.GetFileName(fileName);

            if (!nameOnly.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            {
                error = "Filename must end with .sql";
                return false;
            }

            var withoutExt = nameOnly.Substring(0, nameOnly.Length - 4);

            bool isSystem;
            string rest;
            if (withoutExt.StartsWith(SystemPrefix, StringComparison.Ordinal))
            {
                isSystem = true;
                rest = withoutExt.Substring(SystemPrefix.Length);
            }
            else if (withoutExt.StartsWith(RegularPrefix, StringComparison.Ordinal))
            {
                isSystem = false;
                rest = withoutExt.Substring(RegularPrefix.Length);
            }
            else
            {
                error = $"Filename must start with '{RegularPrefix}' or '{SystemPrefix}'.";
                return false;
            }

            // Find db marker
            var dbIdx = rest.LastIndexOf(DbMarker, StringComparison.Ordinal);
            if (dbIdx < 0)
            {
                error = $"Filename missing database marker '{DbMarker}'.";
                return false;
            }

            var left = rest.Substring(0, dbIdx);
            var dbName = rest.Substring(dbIdx + DbMarker.Length);

            if (string.IsNullOrWhiteSpace(dbName))
            {
                error = "Database name is empty.";
                return false;
            }

            // Validate dbName: underscores allowed; dashes/dots NOT allowed
            for (int i = 0; i < dbName.Length; i++)
            {
                var ch = dbName[i];
                var ok = char.IsLetterOrDigit(ch) || ch == '_';
                if (!ok)
                {
                    error = $"Database name '{dbName}' contains invalid character '{ch}'. Only letters, digits, and '_' are allowed.";
                    return false;
                }
            }

            // left should be: yyyyMMdd_HHmmss_{safeName}
            // Timestamp portion is 15 chars: 8 + 1 + 6
            if (left.Length < 16) // needs timestamp + '_' + at least 1 char slug
            {
                error = $"Filename missing timestamp and/or migration slug. Expected '{TimestampFormat}_<slug>' before '{DbMarker}'.";
                return false;
            }

            var ts = left.Substring(0, 15);
            if (left.Length == 15 || left[15] != '_')
            {
                error = $"Timestamp must be followed by '_' separator. Expected '{TimestampFormat}_<slug>'.";
                return false;
            }

            var slug = left.Substring(16);
            if (string.IsNullOrWhiteSpace(slug))
            {
                error = "Migration slug is empty.";
                return false;
            }

            if (!DateTime.TryParseExact(
                    ts,
                    TimestampFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var timestampUtc))
            {
                error = $"Timestamp '{ts}' is not a valid UTC timestamp in format {TimestampFormat}.";
                return false;
            }

            // SortKey: timestamp first, then system-vs-regular, then slug, then db
            // System migrations should normally run before regular migrations if timestamps collide
            var typeRank = isSystem ? "0" : "1";
            var sortKey = $"{ts}_{typeRank}_{slug}__db-{dbName}";

            parsed = new ParsedMigrationFileName(
                DatabaseName: dbName,
                FileName: nameOnly,
                TimestampUtc: timestampUtc,
                MigrationSlug: slug,
                SortKey: sortKey
            );

            return true;
        }
    }
}