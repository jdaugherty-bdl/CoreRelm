using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace CoreRelm.Migrations.MigrationFiles
{
    /// <summary>
    /// Computes a stable SHA-256 checksum for a migration SQL file.
    /// Rules:
    ///  - Hash is computed over the entire file contents
    ///  - But the ChecksumSha256 header value is treated as a placeholder (64 zeroes) during hashing
    ///  - Newlines are normalized to '\n' before hashing
    ///  - Output hash is lowercase hex
    /// </summary>
    public static class MigrationChecksumHelper
    {
        /// <summary>
        /// 64 zeros. Used as placeholder in the header during checksum computation to avoid recursion.
        /// </summary>
        public const string PlaceholderChecksum64Zeros =
            "0000000000000000000000000000000000000000000000000000000000000000";

        // Matches a header line like:
        // -- ChecksumSha256: <value>
        // Captures:
        //  group 1 = the whole prefix ("-- ChecksumSha256:")
        //  group 2 = the value
        private static readonly Regex ChecksumLineRegex = new(
            pattern: @"(?im)^(?<prefix>\s*--\s*ChecksumSha256\s*:\s*)(?<value>\S*)\s*$",
            options: RegexOptions.Compiled);

        /// <summary>
        /// Normalize text for hashing:
        ///  - Convert CRLF and CR to LF
        /// </summary>
        public static string NormalizeNewlines(string sqlText)
        {
            if (sqlText is null) return string.Empty;
            // First normalize CRLF -> LF, then CR -> LF
            return sqlText.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        /// <summary>
        /// Returns a version of the SQL text where the checksum header value is replaced with 64 zeros.
        /// If no checksum header line exists, returns the input unchanged.
        /// </summary>
        public static string ReplaceChecksumWithPlaceholder(string sqlText)
        {
            if (string.IsNullOrEmpty(sqlText))
                return sqlText ?? string.Empty;

            return ChecksumLineRegex.Replace(sqlText, m =>
            {
                var prefix = m.Groups["prefix"].Value;
                return prefix + PlaceholderChecksum64Zeros;
            });
        }

        /// <summary>
        /// Computes the stable checksum for a migration SQL file:
        ///  - replace checksum value with placeholder
        ///  - normalize newlines
        ///  - SHA-256 over UTF-8 bytes (no BOM concept applies when encoding in-memory)
        /// </summary>
        public static string ComputeStableChecksumSha256(string sqlText)
        {
            var normalized = NormalizeNewlines(ReplaceChecksumWithPlaceholder(sqlText));

            var bytes = Encoding.UTF8.GetBytes(normalized);
            var hash = SHA256.HashData(bytes);

            // lowercase hex
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// Updates (or inserts) the checksum value in the SQL text header.
        /// If a ChecksumSha256 header line exists, it is replaced.
        /// If it does not exist, this method will NOT invent a header block; it throws.
        /// (Keeps behavior strict so you don't silently create partial headers.)
        /// </summary>
        public static string ApplyComputedChecksumToHeader(string sqlText)
        {
            if (sqlText is null) sqlText = string.Empty;

            if (!ChecksumLineRegex.IsMatch(sqlText))
                throw new InvalidOperationException("Cannot apply checksum: no '-- ChecksumSha256:' header line found.");

            var computed = ComputeStableChecksumSha256(sqlText);

            // Replace the checksum line with the computed value
            var updated = ChecksumLineRegex.Replace(sqlText, m =>
            {
                var prefix = m.Groups["prefix"].Value;
                return prefix + computed;
            }, count: 1);

            return updated;
        }

        /// <summary>
        /// Writes text as UTF-8 without BOM.
        /// This prevents BOM-related checksum differences across environments.
        /// </summary>
        public static void WriteUtf8NoBom(string path, string contents)
        {
            var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            File.WriteAllText(path, contents ?? string.Empty, utf8NoBom);
        }

        /// <summary>
        /// Convenience: computes checksum and writes final file (UTF-8 no BOM).
        /// </summary>
        public static void FinalizeAndWrite(string path, string sqlTextWithPlaceholder)
        {
            var finalized = ApplyComputedChecksumToHeader(sqlTextWithPlaceholder);
            WriteUtf8NoBom(path, finalized);
        }

        /// <summary>
        /// Verifies that the checksum in the SQL text matches the computed stable checksum.
        /// Returns false if no checksum header line exists.
        /// </summary>
        public static bool VerifyChecksum(string sqlText)
        {
            if (string.IsNullOrWhiteSpace(sqlText))
                return false;

            var m = ChecksumLineRegex.Match(sqlText);
            if (!m.Success)
                return false;

            var declared = (m.Groups["value"].Value ?? string.Empty).Trim();
            if (declared.Length != 64)
                return false;

            var computed = ComputeStableChecksumSha256(sqlText);
            return string.Equals(declared, computed, StringComparison.OrdinalIgnoreCase);
        }
    }
}