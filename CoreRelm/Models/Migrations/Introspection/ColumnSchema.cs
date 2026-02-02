using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Introspection
{
    public sealed record ColumnSchema(
        string ColumnName,
        string ColumnType,     // e.g. "varchar(45)", "bigint", "timestamp"
        bool IsNullable,
        bool IsPrimaryKey,
        bool IsForeignKey,
        bool IsReadOnly,
        bool IsUnique,
        string? DefaultValue,  // raw from INFORMATION_SCHEMA (may be null)
        string? DefaultValueSql,
        bool IsAutoIncrement,
        string? Extra,         // e.g. "on update CURRENT_TIMESTAMP"
        int OrdinalPosition
    );
}
