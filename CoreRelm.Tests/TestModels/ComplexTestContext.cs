using MySql.Data.MySqlClient;
using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.Options;
using CoreRelm.Tests.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.TestModels
{
    public class ComplexTestContext : RelmContext, IRelmContext_TESTING
    {
        public ComplexTestContext() : base("name=SimpleRelmMySql", autoOpenConnection: false) { }
        public ComplexTestContext(string connectionString) : base(connectionString, autoOpenConnection: false) { }
        public ComplexTestContext(RelmContextOptionsBuilder options) : base(options, autoOpenConnection: false) { }
        public ComplexTestContext(MySqlConnection connection, bool autoOpenConnection = true, bool autoOpenTransaction = false) : base(connection, autoOpenConnection, autoOpenTransaction) { }

        public virtual IRelmDataSet<ComplexTestModel>? ComplexTestModels { get; set; }
        public virtual IRelmDataSet<ComplexReferenceObject>? ComplexReferenceObjects { get; set; }
        public virtual IRelmDataSet<ComplexReferenceObject_NavigationProperty>? ComplexReferenceObject_NavigationProperties { get; set; }
        public virtual IRelmDataSet<ComplexReferenceObject_PrincipalEntity>? ComplexReferenceObject_PrincipalEntities { get; set; }
        public virtual IRelmDataSet<SimpleReferenceObject>? SimpleReferenceObjects { get; set; }
        public virtual IRelmDataSet<DataLoaderTestModel>? DataLoaderTestModels { get; set; }

        void IRelmContext_TESTING.SetDataSet<T>(IRelmDataSet<T> dataSet)
        {
            base.SetDataSet(dataSet);
        }

        public override void OnConfigure(RelmContextOptionsBuilder OptionsBuilder)
        {
            OptionsBuilder.CanOpenConnection = false;
        }
    }
}
