using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.SecurityEnums;

namespace CoreRelm.Models.Migrations.Introspection
{
    public class FunctionSchema : RelmModel
    {
        public string? Delimiter { get; set; } = "$$"; // delimiter used in the function definition (e.g. $$) is local so no RelmColumn attribute needed

        [RelmColumn]
        public string? RoutineName { get; set; }

        [RelmColumn]
        public string? RoutineComment { get; set; }

        [RelmColumn]
        public string? DtdIdentifier { get; set; }

        [RelmColumn]
        public string? RoutineDefinition { get; set; }

        [RelmColumn]
        public string? SqlDataAccess { get; set; } // NO SQL, CONTAINS SQL, READS SQL DATA, MODIFIES SQL DATA

        [RelmColumn]
        public SqlSecurityLevel SecurityType { get; set; }

        [RelmColumn]
        public string? IsDeterministic { get; set; }

        [RelmColumn]
        public bool IsDeterministicBool => IsDeterministic == "YES";
    }
}
