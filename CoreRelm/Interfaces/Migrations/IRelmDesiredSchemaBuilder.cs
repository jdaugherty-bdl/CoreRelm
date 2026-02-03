using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Migrations
{
    public interface IRelmDesiredSchemaBuilder
    {
        Task<SchemaSnapshot> BuildAsync(string dbName, List<ValidatedModelType> modelsForDb);
        SchemaSnapshot Build(string databaseName, IReadOnlyList<ValidatedModelType> modelsForDb);
    }
}
