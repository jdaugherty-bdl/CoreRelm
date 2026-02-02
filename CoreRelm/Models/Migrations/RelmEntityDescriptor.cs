using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations
{
    internal sealed record RelmEntityDescriptor(
        string DatabaseName,
        string TableName,
        IReadOnlyList<RelmColumnDescriptor> Columns,
        IReadOnlyList<RelmIndexDescriptor> Indexes,
        IReadOnlyList<RelmForeignKeyDescriptor> ForeignKeys,
        Type ClrType,
        string? Notes,
        Dictionary<string, string> Hints);
}
