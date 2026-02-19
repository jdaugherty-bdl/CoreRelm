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
        ModelSetsFile LoadModelSets(string? modelSetsPath);
        ResolvedModelSet ResolveSet(ModelSetsFile file, string setName, Assembly modelsAssembly);
        (ResolvedModelSet Resolved, ResolvedModelSetDiagnostics Diagnostics) ResolveSetWithDiagnostics(ModelSetsFile file, string setName, Assembly modelsAssembly);
    }
}
