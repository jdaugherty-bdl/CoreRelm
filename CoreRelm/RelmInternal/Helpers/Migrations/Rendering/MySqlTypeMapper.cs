using CoreRelm.Attributes;
using CoreRelm.RelmInternal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Migrations.Rendering
{
    internal static class MySqlTypeMapper
    {
        public static string? ToMySqlType(Type clrType, RelmColumn column)
        {
            /*
            // unwrap Nullable<T>
            var t = Nullable.GetUnderlyingType(clrType) ?? clrType;

            if (t == typeof(string))
            {
                var size = col.ColumnSize > 0 ? col.ColumnSize : 255;
                return $"varchar({size})";
            }

            if (t == typeof(int)) return "int";
            if (t == typeof(long)) return "bigint";
            if (t == typeof(short)) return "smallint";
            if (t == typeof(byte)) return "tinyint unsigned";
            if (t == typeof(bool)) return "tinyint(1)";
            if (t == typeof(DateTime)) return "datetime";
            if (t == typeof(DateTimeOffset)) return "datetime";
            if (t == typeof(decimal))
            {
                var p = 18;
                var s = 2;
                if (col.CompoundColumnSize is { Length: >= 2 })
                {
                    p = col.CompoundColumnSize[0];
                    s = col.CompoundColumnSize[1];
                }
                return $"decimal({p},{s})";
            }
            if (t == typeof(double)) return "double";
            if (t == typeof(float)) return "float";
            if (t == typeof(Guid)) return "varchar(45)";
            if (t == typeof(byte[])) return "blob";

            // If you hit this, add a mapping or provide a custom mapper hook.
            throw new NotSupportedException($"No MySQL type mapping for CLR type {t.FullName} (property uses [RelmColumn]).");
            */
            var columnName = DALPropertyType_MySQL.PropertyTypeToColumnName(clrType);
            switch (columnName)
            {
                case "varchar":
                    return clrType switch
                    {
                        Type t when t == typeof(Guid) => $"{columnName}(45)",
                        _ => $"{columnName}({(column.ColumnSize > 0 ? column.ColumnSize : 255)})",
                    };
                case "tinyint":
                    return clrType switch
                    {
                        Type t when t == typeof(bool) => "tinyint(1)",
                        Type t when t == typeof(byte) => "tinyint unsigned",
                        _ => columnName,
                    };
                case "decimal":
                    {
                        var p = 18;
                        var s = 2;
                        if (column.CompoundColumnSize is { Length: >= 2 })
                        {
                            p = column.CompoundColumnSize[0];
                            s = column.CompoundColumnSize[1];
                        }
                        return $"{columnName}({p},{s})";
                    }
                default:
                    return columnName;
            }
        }
    }
}
