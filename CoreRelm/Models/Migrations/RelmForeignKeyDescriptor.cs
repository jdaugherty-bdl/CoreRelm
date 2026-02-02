using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations
{
    internal sealed record RelmForeignKeyDescriptor(
        string Name,
        IReadOnlyList<string> LocalColumns,
        string PrincipalTable,
        IReadOnlyList<string> PrincipalColumns,
        string OnDelete);
}
