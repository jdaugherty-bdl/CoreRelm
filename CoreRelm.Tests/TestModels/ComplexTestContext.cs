using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.Options;
using CoreRelm.Tests.Interfaces;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Options.RelmContextOptions;

namespace CoreRelm.Tests.TestModels
{
    public class ComplexTestContext(RelmContextOptions? contextOptions) : RelmContext(contextOptions, "name=SimpleRelmMySql", OptionsBuilderTypes.NamedConnectionString), IRelmContext_TESTING
    {
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

        public override void OnConfigure(RelmContextOptions contextOptions)
        {
            contextOptions.CanOpenConnection = false;
        }
    }
}
