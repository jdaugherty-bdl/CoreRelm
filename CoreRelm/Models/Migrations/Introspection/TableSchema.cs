using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Introspection
{
    public sealed record TableSchema(
        string TableName,
        IReadOnlyDictionary<string, ColumnSchema> Columns,
        IReadOnlyDictionary<string, IndexSchema> Indexes,
        IReadOnlyDictionary<string, ForeignKeySchema> ForeignKeys,
        IReadOnlyDictionary<string, TriggerSchema> Triggers
    );
}
