using CoreRelm.Attributes;
using CoreRelm.Models;
using CoreRelm.Tests.TestModels.MultipleKeys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.TestModels.NonDefaultForeignKeys
{
    [RelmDatabase("test_database")]
    [RelmTable("nothing_table")]
    public class NonDefaultForeignKeysReferenceObject_NavigationProperty : RelmModel
    {
        [RelmColumn]
        public string? ReferenceKey { get; set; }

        [RelmForeignKey(nameof(NonDefaultForeignKeysTestObject.NonDefaultForeignKeysReferenceObjectLocalKey), nameof(ReferenceKey))]
        public NonDefaultForeignKeysTestObject? NonDefaultForeignKeysTestObject_Reference { get; set; }
    }
}
