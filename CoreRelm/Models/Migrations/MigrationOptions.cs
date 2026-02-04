using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations
{
    public class MigrationOptions
    {
        public string? ConnectionString { get; set; }
        public string? ConnectionStringTemplate { get; set; }
        public string? SetName { get; set; }
        public string? ModelSetsPath { get; set; }
        public bool Quiet { get; set; }
        public bool JsonFlag { get; set; }
        public bool Destructive { get; set; }
        public bool Apply { get; set; }
        public CancellationToken CancelToken { get; set; }
    }
}
