using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Tooling.Validation
{
    public sealed record ModelSetValidateOptions(
        string? DatabaseFilter = null,
        bool IncludeNamespaceMatchBreakdown = true,
        bool FailOnWarnings = false
    );
}
