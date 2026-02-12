using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Attributes.RelmUnique_Tests
{
    public class RelmUnique_OrderAndCase_Tests
    {
        [Fact]
        public void ConstraintProperties_Preserves_Order()
        {
            var instance = new RelmUnique(["A", "B", "C"]);
            Assert.Equal(["A", "B", "C"], instance.ConstraintProperties);
        }

        [Fact]
        public void ConstraintProperties_Preserves_Case()
        {
            var instance = new RelmUnique(["Name", "email"]);
            Assert.Equal(["Name", "email"], instance.ConstraintProperties);
        }
    }
}
