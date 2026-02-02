using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations
{

    public sealed record ResolvedModelSetDiagnostics(
        string SetName,
        int AssemblyTypeCount,
        int ExplicitTypeNameCount,
        int ExplicitTypesResolvedCount,
        int NamespacePrefixCount,
        int NamespaceMatchedCount,
        int CandidateCountBeforeFilter,
        int AbstractExcludedCount,
        int NotRelmModelExcludedCount,
        int IncludedCount,
        int MissingRelmDatabaseCount,
        int MissingRelmTableCount,
        int AttributeValueErrorCount,
        IReadOnlyList<string> Errors
    );
}
