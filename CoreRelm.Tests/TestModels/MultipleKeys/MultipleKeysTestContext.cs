using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.Options;
using CoreRelm.Tests.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.TestModels.MultipleKeys
{
    public class MultipleKeysTestContext(RelmContextOptions? contextOptions) : RelmContext(contextOptions ?? throw new ArgumentNullException(nameof(contextOptions))), IRelmContext_TESTING
    {
        public virtual IRelmDataSet<MultipleKeysTestObject>? MultipleKeysTestObjects { get; set; }
        public virtual IRelmDataSet<MultipleKeysReferenceObject_ForeignKey>? MultipleKeysReferenceObject_ForeignKeys { get; set; }
        public virtual IRelmDataSet<MultipleKeysReferenceObject_NavigationProperty>? MultipleKeysReferenceObject_NavigationProperties { get; set; }
        public virtual IRelmDataSet<MultipleKeysReferenceObject_PrincipalEntity>? MultipleKeysReferenceObject_PrincipalEntities { get; set; }

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
