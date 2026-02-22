using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations
{
    public record MigrationOptions
    {
        public string? DatabaseName { get; set; }
        public string? ConnectionStringTemplate { get; set; }
        public string? SetName { get; set; }
        public string? MigrationName { get; set; }
        public DateTime StampUtc { get; set; }
        public string? MigrationsPath { get; set; } = "./migrations";
        public string? ModelSetsPath { get; set; } = "./migrations/modelsets.json";
        public string MigrationErrorPath { get; set; } = "./migrations/_errors";
        public bool Quiet { get; set; } = false;
        public bool JsonFlag { get; set; } = false;
        public string? JsonPath { get; set; } = null;
        public bool DropFunctionsOnCreate { get; set; } = false;
        public bool Destructive { get; set; } = false;
        public bool Apply { get; set; } = false;
        public bool SaveSystemMigrations { get; set; } = false;
        public CancellationToken CancelToken { get; set; }
    }
}
