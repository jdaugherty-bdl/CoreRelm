using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Triggers;

namespace CoreRelm.Models.Migrations.Introspection
{
    public class TriggerSchema : RelmModel
    {
        [RelmColumn]
        public string? TriggerName { get; set; }

        [RelmColumn]
        public TriggerEvent EventManipulation { get; set; } // "INSERT", "UPDATE", "DELETE"
        
        [RelmColumn]
        public TriggerTime ActionTiming { get; set; }      // "BEFORE" or "AFTER"
        
        [RelmColumn]
        public string? EventObjectTable { get; set; }
        
        [RelmColumn]
        public string? ActionStatement { get; set; }    // trigger body
    }
}
