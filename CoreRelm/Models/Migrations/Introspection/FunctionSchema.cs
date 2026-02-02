using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Introspection
{
    public sealed record FunctionSchema(
        string FunctionName,
        string ReturnType,
        string RoutineDefinition
    );
}
