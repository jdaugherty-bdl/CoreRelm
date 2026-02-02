using CoreRelm.Models.Migrations.Introspection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Migrations.Introspection
{
    public static class SchemaSnapshotFactory
    {
        public static SchemaSnapshot Empty(string dbName) =>
            new(
                DatabaseName: dbName,
                Tables: new Dictionary<string, TableSchema>(System.StringComparer.Ordinal),
                Functions: new Dictionary<string, FunctionSchema>(System.StringComparer.OrdinalIgnoreCase)
            );
    }
}
