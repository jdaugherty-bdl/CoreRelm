using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Introspection
{
    public sealed record IndexColumnSchema(
        string ColumnName,
        string? SubPart,
        string? Collation,
        string? Expression,
        int SeqInIndex
    );
}
