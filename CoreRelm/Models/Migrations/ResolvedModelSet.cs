using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations
{
    public sealed record ResolvedModelSet(
        string SetName,
        IReadOnlyList<ValidatedModelType> AllModels,
        IReadOnlyDictionary<string, List<ValidatedModelType>> ModelsByDatabase,
        IReadOnlyList<string> Warnings
    );
}
