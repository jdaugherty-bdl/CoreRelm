using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations
{
    internal sealed record RelmIndexDescriptor(
        string Name,
        bool IsUnique,
        IReadOnlyList<string> Columns);
}
