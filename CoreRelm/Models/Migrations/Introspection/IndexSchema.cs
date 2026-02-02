using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Introspection
{
    public sealed record IndexSchema(
        string IndexName,
        bool IsUnique,
        IReadOnlyList<IndexColumnSchema> Columns
    );
}
