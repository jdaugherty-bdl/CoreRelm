using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.Models.Migrations.Introspection;
using CoreRelm.Options;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Migrations.Contexts
{
    public class InformationSchemaContext(RelmContextOptions relmContextOptions) : RelmContext(relmContextOptions)
    {
        public virtual IRelmDataSet<ColumnSchema>? Columns { get; protected set; }
        public virtual IRelmDataSet<ForeignKeySchema>? ForeignKeys { get; protected set; }
        public virtual IRelmDataSet<FunctionParameterSchema>? FunctionParameters { get; protected set; }
        public virtual IRelmDataSet<FunctionSchema>? Functions { get; protected set; }
        public virtual IRelmDataSet<IndexSchema>? Indexes { get; protected set; }
        public virtual IRelmDataSet<TriggerSchema>? Triggers { get; protected set; }
    }
}
