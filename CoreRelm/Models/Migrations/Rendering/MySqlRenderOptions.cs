using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Rendering
{
    public sealed record MySqlRenderOptions(
        bool IncludeUseDatabase = true,
        bool WrapTriggersWithDelimiter = true,
        string TriggerDelimiter = "$$"
    );
}
