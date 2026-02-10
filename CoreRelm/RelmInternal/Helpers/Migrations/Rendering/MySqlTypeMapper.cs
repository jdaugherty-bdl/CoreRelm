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
            var columnName = DALPropertyType_MySQL.PropertyTypeToColumnName(clrType);
            switch (columnName)
            {
                case "varchar":
                    return clrType switch
                    {
                        Type t when t == typeof(Guid) => $"{columnName}(45)",
                        Type t when t == typeof(Enum) => $"{columnName}({(column.ColumnSize > 0 ? column.ColumnSize : 512)})",
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
