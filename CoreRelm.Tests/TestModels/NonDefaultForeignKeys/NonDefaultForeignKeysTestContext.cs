using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.Options;
using CoreRelm.Tests.Interfaces;
using CoreRelm.Tests.TestModels.MultipleKeys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.TestModels.NonDefaultForeignKeys
{
    public class NonDefaultForeignKeysTestContext : RelmContext, IRelmContext_TESTING
    {
        public NonDefaultForeignKeysTestContext(string? connectionString, bool autoInitializeDataSets = true, bool autoVerifyTables = true) : base(connectionString, autoOpenConnection: false, autoInitializeDataSets: autoInitializeDataSets, autoVerifyTables: autoVerifyTables) { }
        public NonDefaultForeignKeysTestContext(RelmContextOptionsBuilder? options) : base(options?.SetAutoOpenConnection(false)) { }

        public virtual IRelmDataSet<NonDefaultForeignKeysTestObject>? NonDefaultForeignKeysTestObjects { get; set; }
        public virtual IRelmDataSet<NonDefaultForeignKeysReferenceObject_ForeignKey>? NonDefaultForeignKeysReferenceObject_ForeignKeys { get; set; }
        public virtual IRelmDataSet<NonDefaultForeignKeysReferenceObject_NavigationProperty>? NonDefaultForeignKeysReferenceObject_NavigationProperties { get; set; }
        public virtual IRelmDataSet<NonDefaultForeignKeysReferenceObject_PrincipalEntity>? NonDefaultForeignKeysReferenceObject_PrincipalEntities { get; set; }

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
