using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Introspection
{
    public class FunctionSchema : RelmModel
    {
        [RelmColumn]
        public string? FunctionName { get; set; }

        [RelmColumn]
        public string? RoutineName { get; set; }

        [RelmColumn]
        public string? ReturnType { get; set; }

        [RelmColumn]
        public string? DtdIdentifier { get; set; }

        [RelmColumn]
        public string? RoutineDefinition { get; set; }
    }
}
