using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Introspection
{
    public sealed record IndexColumnSchema(
        string ColumnName,
        int SeqInIndex,
        string? Collation // "A" or "D" or null
    );
}
