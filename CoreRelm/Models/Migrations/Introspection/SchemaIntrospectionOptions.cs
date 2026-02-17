using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Introspection
{
    public sealed record SchemaIntrospectionOptions(
        string? DatabaseName = null,
        bool IncludeViews = false
    );
}
