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
        public ComplexTestContext(bool autoInitializeDataSets = true, bool autoVerifyTables = true) : base("name=SimpleRelmMySql", autoOpenConnection: false, autoInitializeDataSets: autoInitializeDataSets, autoVerifyTables: autoVerifyTables) { }
        public ComplexTestContext(string? connectionString, bool autoInitializeDataSets = true, bool autoVerifyTables = true) : base(connectionString, autoOpenConnection: false, autoInitializeDataSets: autoInitializeDataSets, autoVerifyTables: autoVerifyTables) { }
        public ComplexTestContext(RelmContextOptionsBuilder? options) : base(options) { }
        public ComplexTestContext(MySqlConnection connection, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool autoInitializeDataSets = true, bool autoVerifyTables = true) : base(connection, autoOpenConnection, autoOpenTransaction, autoInitializeDataSets: autoInitializeDataSets, autoVerifyTables: autoVerifyTables) { }

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
