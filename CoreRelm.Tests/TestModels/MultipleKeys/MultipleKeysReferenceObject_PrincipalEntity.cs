using CoreRelm.Attributes;
using CoreRelm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.TestModels.MultipleKeys
{
    [RelmDatabase("test_database")]
    [RelmTable("nothing_table")]
    public class MultipleKeysReferenceObject_PrincipalEntity : RelmModel
    {
        [RelmColumn]
        public string? ReferenceKey1 { get; set; }
        [RelmColumn]
        public string? ReferenceKey2 { get; set; }

        public MultipleKeysTestObject? MultipleKeysTestObject_Reference { get; set; }
    }
}
