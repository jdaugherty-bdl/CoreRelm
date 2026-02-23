using MySql.Data.MySqlClient;
using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.Options;
using CoreRelm.Quickstart.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Quickstart.Contexts
{
    internal class ExampleContext(RelmContextOptions contextOptions) : RelmContext(contextOptions)
    {
        public IRelmDataSet<ExampleModel>? ExampleModels { get; set; }
        public IRelmDataSet<ExampleGroup>? ExampleGroups { get; set; }
    }
}
