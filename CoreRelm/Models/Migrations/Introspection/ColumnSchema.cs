using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Introspection
{
    public class ColumnSchema : RelmModel
    {
        [RelmColumn]
        public string? TableName { get; set; }

        [RelmColumn]
        public string? ColumnName { get; set; }

        [RelmColumn]
        public string? ColumnType { get; set; }     // e.g. "varchar(45)", "bigint", "timestamp"

        [RelmColumn]
        public string? ColumnKey { get; set; }

        [RelmColumn]
        public bool IsNullable { get; set; }

        [RelmColumn]
        public bool IsPrimaryKey { get; set; }

        [RelmColumn]
        public bool IsForeignKey { get; set; }

        [RelmColumn]
        public bool IsReadOnly { get; set; }

        [RelmColumn]
        public bool IsUnique { get; set; }

        [RelmColumn(columnName: "COLUMN_DEFAULT")]
        public string? DefaultValue { get; set; }  // raw from INFORMATION_SCHEMA (may be null)

        //public string? DefaultValueSql { get; set; }

        [RelmColumn]
        public bool IsAutoIncrement { get; set; }

        [RelmColumn]
        public string? Extra { get; set; }         // e.g. "on update CURRENT_TIMESTAMP"
        
        [RelmColumn]
        public int OrdinalPosition { get; set; }

        public ColumnSchema Clone() => (ColumnSchema)this.MemberwiseClone();
    }
}
