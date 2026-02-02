using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Introspection
{

    public sealed record TriggerSchema(
        string TriggerName,
        string EventManipulation, // "INSERT", "UPDATE", "DELETE"
        string ActionTiming,      // "BEFORE" or "AFTER"
        string ActionStatement    // trigger body
    );
}
