using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Introspection
{
    internal sealed record SchemaIntrospectionOptions(
        bool IncludeViews = false
    );
}
