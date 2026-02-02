using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations
{
    public sealed record MigrationGenerateResult(string DatabaseName, bool HasChanges, string? Sql, string Message)
    {
        public static MigrationGenerateResult NoChanges(string dbName, string message) =>
            new(dbName, HasChanges: false, Sql: null, Message: message);

        public static MigrationGenerateResult Changes(string dbName, string sql, string message) =>
            new(dbName, HasChanges: true, Sql: sql, Message: message);
    }
}
