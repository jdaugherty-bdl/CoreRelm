using CoreRelm.Attributes;
using CoreRelm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.TestModels
{
    [RelmDatabase("test_database")]
    [RelmTable("nothing_table")]
    public class ComplexReferenceObject : RelmModel
    {
        [RelmColumn]
        [RelmForeignKey(localKey: nameof(TestModel))]
        public string? ComplexTestModelInternalId { get; set; }

        [RelmColumn]
        public ComplexTestModel? TestModel { get; set; }
    }
}
