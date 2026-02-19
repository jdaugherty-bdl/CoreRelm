using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Tooling.Validation
{
    public sealed record ModelSetValidationReport(
        string SetName,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Errors,
        IReadOnlyDictionary<string, IReadOnlyList<ValidatedModelType>> TypesByDatabase
    );
}
