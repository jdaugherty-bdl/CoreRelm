using CoreRelm.Models.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.ModelSets
{
    public interface IModelSetResolver
    {
        ResolvedModelSet ResolveSet(ModelSetsFile file, string setName);
        (ResolvedModelSet Resolved, ResolvedModelSetDiagnostics Diagnostics) ResolveSetWithDiagnostics(ModelSetsFile file, string setName);
    }
}
