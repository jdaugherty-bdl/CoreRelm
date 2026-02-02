using CoreRelm.Interfaces;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Migrations.Execution
{
    public sealed class MySqlScriptRunner
    {
        public async Task ExecuteScriptAsync(IRelmContext context, string sql, CancellationToken ct = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (sql is null) throw new ArgumentNullException(nameof(sql));

            // MySQL DDL causes implicit commits; do not wrap in a transaction.
            foreach (var stmt in SplitByDelimiter(sql))
            {
                var trimmed = stmt.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                /*
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = trimmed;
                await cmd.ExecuteNonQueryAsync(ct);
                */
                context.DoDatabaseWork(trimmed);
            }
        }

        private static IEnumerable<string> SplitByDelimiter(string script)
        {
            // Supports:
            //   DELIMITER $$
            //   ... statements ending with $$ ...
            //   DELIMITER ;
            var delimiter = ";";
            var sb = new System.Text.StringBuilder();

            using var reader = new System.IO.StringReader(script);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var trimmed = line.Trim();

                // Skip pure comment lines (keep comments inside statements if you want; here we drop them)
                if (trimmed.StartsWith("--")) continue;

                if (trimmed.StartsWith("DELIMITER ", StringComparison.OrdinalIgnoreCase))
                {
                    // Flush pending buffer as a statement (rare but safe)
                    var pending = sb.ToString().Trim();
                    if (pending.Length > 0)
                    {
                        yield return pending;
                        sb.Clear();
                    }

                    delimiter = trimmed.Substring("DELIMITER ".Length).Trim();
                    continue;
                }

                sb.AppendLine(line);

                // When delimiter is ";" we can end on semicolon line endings;
                // when delimiter is "$$" we end on "$$" suffix (commonly on its own line).
                var current = sb.ToString();

                if (EndsWithDelimiter(current, delimiter))
                {
                    // strip the delimiter from the end
                    var statement = StripTrailingDelimiter(current, delimiter);
                    yield return statement;
                    sb.Clear();
                }
            }

            var remaining = sb.ToString().Trim();
            if (remaining.Length > 0)
                yield return remaining;
        }

        private static bool EndsWithDelimiter(string text, string delimiter)
        {
            // Handle delimiter on its own line or after whitespace
            var t = text.TrimEnd();

            if (delimiter == ";")
                return t.EndsWith(";", StringComparison.Ordinal);

            return t.EndsWith(delimiter, StringComparison.Ordinal);
        }

        private static string StripTrailingDelimiter(string text, string delimiter)
        {
            var t = text.TrimEnd();
            if (delimiter == ";")
            {
                if (t.EndsWith(";", StringComparison.Ordinal))
                    return t.Substring(0, t.Length - 1);
                return t;
            }

            if (t.EndsWith(delimiter, StringComparison.Ordinal))
                return t.Substring(0, t.Length - delimiter.Length);

            return t;
        }
    }
}
