using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Introspection
{
    public class IndexSchema : RelmModel
    {
        [RelmColumn]
        public string? TableName { get; set; }

        [RelmColumn]
        public string? IndexName { get; set; }

        [RelmColumn]
        public string? ColumnName { get; set; }

        [RelmColumn]
        public int SeqInIndex { get; set; }

        [RelmColumn]
        public string? Collation { get; set; }

        [RelmColumn]
        public bool IsUnique { get; set; }

        [RelmColumn]
        public bool NonUnique { get; set; }

        public IReadOnlyList<IndexColumnSchema> Columns { get; set; }
    }
}
