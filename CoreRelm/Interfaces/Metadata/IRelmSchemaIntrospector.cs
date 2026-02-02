using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Metadata
{
    internal interface IRelmSchemaIntrospector
    {
        Task<SchemaSnapshot> LoadSchemaAsync(
            string connectionString,
            SchemaIntrospectionOptions? options = null,
            CancellationToken ct = default);
    }
}
