using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Attributes.RelmUnique_Tests
{
    public class RelmUnique_Validation_Tests
    {
        [Fact]
        public void RelmUnique_Constructor_Allows_Valid_NonEmpty_Strings()
        {
            var instance = new RelmUnique(new[] { "A", "B", "C" });
            Assert.Equal(["A", "B", "C"], instance.ConstraintProperties);
        }

        [Fact]
        public void RelmUnique_Does_Not_Allow_Empty_Array()
        {
            Assert.Throws<ArgumentException>(() => new RelmUnique([]));
        }

        [Fact]
        public void RelmUnique_Does_Not_Allow_Null_Array()
        {
            Assert.Throws<ArgumentException>(() => new RelmUnique(null!));
        }
    }
}
