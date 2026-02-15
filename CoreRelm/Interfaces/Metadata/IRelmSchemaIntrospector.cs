using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using CoreRelm.RelmInternal.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Metadata
{
    public interface IRelmSchemaIntrospector
    {
        Task<SchemaSnapshot> LoadSchemaAsync(
            InformationSchemaContext relmContext,
            SchemaIntrospectionOptions? options = null,
            CancellationToken ct = default);
    }
}
