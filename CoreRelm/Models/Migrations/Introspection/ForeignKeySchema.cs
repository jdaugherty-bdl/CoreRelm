using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Introspection
{
    public class ForeignKeySchema : RelmModel
    {
        [RelmColumn]
        public string? ConstraintName { get; set; }

        [RelmColumn]
        public string? TableName { get; set; }

        [RelmColumn]
        public string? ColumnName { get; set; }

        [RelmColumn]
        public IReadOnlyList<string?>? ColumnNames { get; set; }

        [RelmColumn]
        public string? ReferencedTableName { get; set; }

        [RelmColumn]
        public string? ReferencedColumnName { get; set; }

        [RelmColumn]
        public IReadOnlyList<string?>? ReferencedColumnNames { get; set; }

        [RelmColumn]
        public string? UpdateRule { get; set; }

        [RelmColumn]
        public string? DeleteRule { get; set; }

        [RelmColumn]
        public int OrdinalPosition { get; set; }
    }
}
