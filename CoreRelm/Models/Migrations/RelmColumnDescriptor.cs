using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations
{
    internal sealed record RelmColumnDescriptor(
        string? ColumnName,
        string? StoreType,
        bool IsNullable,
        string? DefaultSql,
        bool IsPrimaryKey,
        bool IsAutoIncrement,
        bool IsUnique,
        int Ordinal);
}
