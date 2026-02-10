using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations
{
    public class MigrationOptions
    {
        public string? ConnectionStringTemplate { get; set; }
        public string? SetName { get; set; }
        public string? MigrationsPath { get; set; } = "./migrations";
        public string? ModelSetsPath { get; set; } = "./migrations/modelsets.json";
        public bool Quiet { get; set; } = false;
        public bool JsonFlag { get; set; } = false;
        public string? JsonPath { get; set; } = null;
        public bool Destructive { get; set; } = false;
        public bool Apply { get; set; } = false;
        public bool SaveSystemMigrations { get; set; } = false;
        public CancellationToken CancelToken { get; set; }
    }
}
