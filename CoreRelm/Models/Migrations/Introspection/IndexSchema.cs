using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Indexes;

namespace CoreRelm.Models.Migrations.Introspection
{
    public class IndexSchema : RelmModel
    {
        public IReadOnlyList<IndexColumnSchema>? Columns { get; set; }

        public bool IsVisibleBool => IsVisible == "YES";

        [RelmColumn]
        public string? TableName { get; set; }

        [RelmColumn]
        public string? IndexName { get; set; }

        [RelmColumn]
        public string? ColumnName { get; set; }

        [RelmColumn]
        public string? SubPart { get; set; }

        [RelmColumn]
        public int SeqInIndex { get; set; }

        [RelmColumn]
        public string? Collation { get; set; }

        [RelmColumn]
        public bool NonUnique { get; set; }

        [RelmColumn]
        public string? Expression { get; set; }

        [RelmColumn]
        public string? Comment { get; set; }

        [RelmColumn]
        public string? IndexComment { get; set; }

        [RelmColumn]
        public string? IndexType { get; set; }

        [RelmColumn]
        public IndexType IndexTypeValue { get; set; }

        [RelmColumn]
        public string? IsVisible { get; set; }
    }
}
