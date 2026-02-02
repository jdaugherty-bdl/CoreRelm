using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations
{
    public sealed record ModelSetValidateReport(
        string Tool,
        string Command,
        string GeneratedUtc,
        string SetName,
        string? DatabaseFilter,
        ResolvedModelSetDiagnostics Diagnostics,
        IReadOnlyDictionary<string, List<ModelSetValidatedModel>> ModelsByDatabase
    );
}
