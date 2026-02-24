using CoreRelm.Models.Migrations.Tooling.Apply;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreRelm.RelmInternal.Helpers.Migrations.MigrationFiles
{ 
    /// <summary>
    /// Generates the standard CoreRelm migration header block.
    ///
    /// Format:
    ///   -- CoreRelm-Migration:
    ///   -- HeaderVersion: 1
    ///   -- Tool: CoreRelm
    ///   -- ToolVersion: x.y.z
    ///   -- MigrationType: RelmMigration | SYSTEM_MIGRATION
    ///   -- Database: dbname
    ///   -- TimestampUtc: 2026-01-31T21:19:35Z
    ///   -- Slug: safe_slug
    ///   -- FileName: the_original_filename.sql
    ///   -- MigrationName: optional-friendly-name
    ///   -- ModelSet: optional-modelset-name
    ///   -- ChecksumSha256: 000..000 (placeholder)
    ///   -- Extras.<K>: <V>  (0..N)
    /// </summary>
    internal static class MigrationHeaderFormatter
    {
        public const int HeaderVersion = 1;

        /// <summary>
        /// 64 zeros placeholder for ChecksumSha256 to avoid recursion when hashing whole file.
        /// </summary>
        public const string ChecksumPlaceholder64Zeros =
            "0000000000000000000000000000000000000000000000000000000000000000";

        private const string Sentinel = "-- CoreRelm-Migration:";

        public static string BuildHeader(
            ParsedMigrationHeader header,
            string migrationType,          // "RelmMigration" or "SYSTEM_MIGRATION"
            string databaseName,
            DateTime generatedUtc,
            string slug,
            string fileName,
            IReadOnlyDictionary<string, string>? extraMetadata = null)
        {
            if (string.IsNullOrWhiteSpace(migrationType))
                throw new ArgumentException("migrationType is required.", nameof(migrationType));
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("databaseName is required.", nameof(databaseName));
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("slug is required.", nameof(slug));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("fileName is required.", nameof(fileName));

            // Normalize timestamp to UTC and emit ISO-8601 with Z
            var utc = generatedUtc.Kind == DateTimeKind.Utc
                ? generatedUtc
                : generatedUtc.ToUniversalTime();

            // Start building lines (use \n; writer can preserve or normalize later)
            var sb = new StringBuilder();
            sb.AppendLine(Sentinel);
            sb.AppendLine($"-- HeaderVersion: {HeaderVersion}");
            sb.AppendLine($"-- Tool: {header.Tool ?? "CoreRelm"}");

            if (!string.IsNullOrWhiteSpace(header.ToolVersion))
                sb.AppendLine($"-- ToolVersion: {header.ToolVersion}");

            sb.AppendLine($"-- MigrationType: {migrationType}");
            sb.AppendLine($"-- Database: {databaseName}");
            sb.AppendLine($"-- TimestampUtc: {utc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"-- Slug: {slug}");
            sb.AppendLine($"-- FileName: {fileName}");

            if (!string.IsNullOrWhiteSpace(header.MigrationName))
                sb.AppendLine($"-- MigrationName: {header.MigrationName}");

            if (!string.IsNullOrWhiteSpace(header.ModelSetName))
                sb.AppendLine($"-- ModelSet: {header.ModelSetName}");

            // Always include placeholder checksum line (finalized later)
            sb.AppendLine($"-- ChecksumSha256: {ChecksumPlaceholder64Zeros}");

            // Extras from ParsedMigrationHeader.Extras
            if (header.Extras is not null && header.Extras.Count > 0)
            {
                foreach (var kvp in header.Extras.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                    sb.AppendLine($"-- Extras.{kvp.Key}: {kvp.Value}");
            }

            // Any additional extra metadata passed by caller (doesn't overwrite header.Extras)
            if (extraMetadata is not null && extraMetadata.Count > 0)
            {
                foreach (var kvp in extraMetadata.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                    sb.AppendLine($"-- Extras.{kvp.Key}: {kvp.Value}");
            }

            // Blank line separator between header and SQL body
            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Convenience: builds header and concatenates with SQL body.
        /// Does NOT compute/finalize checksum. Use MigrationChecksumHelper.FinalizeAndWrite(...)
        /// or MigrationChecksumHelper.ApplyComputedChecksumToHeader(...) later.
        /// </summary>
        public static string PrependHeader(
            ParsedMigrationHeader header,
            string migrationType,
            string databaseName,
            DateTime generatedUtc,
            string slug,
            string fileName,
            string sqlBody,
            IReadOnlyDictionary<string, string>? extraMetadata = null)
        {
            var hdr = BuildHeader(header, migrationType, databaseName, generatedUtc, slug, fileName, extraMetadata);
            return hdr + (sqlBody ?? string.Empty);
        }
    }
}