using CoreRelm.Interfaces.Migrations.MigrationFiles;
using CoreRelm.Models.Migrations.Tooling.Apply;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace CoreRelm.Migrations.MigrationFiles
{
    /// <summary>
    /// Parses an optional header block from SQL text.
    /// Expected header format (example):
    ///   -- CoreRelm-Migration:
    ///   -- Tool: CoreRelm
    ///   -- ToolVersion: 1.2.3
    ///   -- MigrationType: RelmMigration
    ///   -- Database: ledgerlite
    ///   -- TimestampUtc: 2026-01-31T21:19:35Z
    ///   -- Slug: add_users
    ///   -- ChecksumSha256: --hash--
    ///
    /// If no header block is found, returns false with error=null.
    /// </summary>
    public sealed class MigrationScriptHeaderParser([FromKeyedServices("MaxSupportedMigrationFileVersion")] Version maxSupportedVersion) : IMigrationScriptHeaderParser
    {
        private const string HeaderStart = "-- CoreRelm-Migration:";

        public void ValidateHeader(string headerVersion)
        {
            if (Version.TryParse(headerVersion, out var fileVersion) && fileVersion > maxSupportedVersion)
            {
                throw new NotSupportedException($"Unsupported migration script version: {fileVersion}");
            }
        }

        public bool TryParseHeader(string sqlText, out ParsedMigrationHeader header, out string? error)
        {
            header = new ParsedMigrationHeader(
                Tool: null,
                ToolVersion: null,
                MigrationName: null,
                ModelSetName: null,
                DatabaseName: null,
                GeneratedUtc: null,
                ChecksumSha256: null,
                Extras: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            );
            error = null;

            if (string.IsNullOrWhiteSpace(sqlText))
                return false;

            var lines = sqlText.Replace("\r\n", "\n").Split('\n');

            // Find header start
            var startIdx = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().Equals(HeaderStart, StringComparison.Ordinal))
                {
                    startIdx = i;
                    break;
                }
            }

            if (startIdx < 0)
            {
                // No header present
                return false;
            }

            var extras = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string? tool = null, toolVer = null, migType = null, migName = null, slug = null, db = null, checksum = null;
            DateTime? generatedUtc = null;

            // Read subsequent comment lines until a non-comment line or blank after leaving header area
            for (int i = startIdx + 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (!line.StartsWith("--"))
                    break;

                // remove leading '--'
                line = line.Substring(2).Trim();
                if (line.Length == 0)
                    continue;

                var colon = line.IndexOf(':');
                if (colon <= 0)
                    continue;

                var key = line.Substring(0, colon).Trim();
                var val = line.Substring(colon + 1).Trim();

                if (key.Equals("Tool", StringComparison.OrdinalIgnoreCase)) tool = val;
                else if (key.Equals("ToolVersion", StringComparison.OrdinalIgnoreCase)) toolVer = val;
                else if (key.Equals("MigrationType", StringComparison.OrdinalIgnoreCase)) migType = val;
                else if (key.Equals("MigrationName", StringComparison.OrdinalIgnoreCase)) migName = val;
                else if (key.Equals("Slug", StringComparison.OrdinalIgnoreCase)) slug = val;
                else if (key.Equals("Database", StringComparison.OrdinalIgnoreCase)) db = val;
                else if (key.Equals("ChecksumSha256", StringComparison.OrdinalIgnoreCase)) checksum = val;
                else if (key.Equals("TimestampUtc", StringComparison.OrdinalIgnoreCase) || key.Equals("GeneratedUtc", StringComparison.OrdinalIgnoreCase))
                {
                    if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
                        generatedUtc = dt;
                    else
                        extras[key] = val; // keep it as extra if it doesn't parse
                }
                else
                {
                    extras[key] = val;
                }
            }

            ValidateHeader(toolVer ?? new Version(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue).ToString());

            header = new ParsedMigrationHeader(
                Tool: tool,
                ToolVersion: toolVer,
                MigrationName: migName,
                ModelSetName: slug,
                DatabaseName: db,
                GeneratedUtc: generatedUtc,
                ChecksumSha256: checksum,
                Extras: extras
            );

            return true;
        }
    }
}