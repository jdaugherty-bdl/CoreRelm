using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Attributes.RelmUnique_Tests
{
    [RelmUnique(new[] { "A", "B" })]
    [RelmUnique(new[] { "A", "B" })]
    public class DuplicateUniqueModel
    {
        public string A { get; set; } = "";
        public string B { get; set; } = "";
    }

    public class RelmUnique_DuplicateAttribute_Tests
    {
        [Fact]
        public void Duplicate_RelmUnique_Attributes_Are_Both_Present()
        {
            var attrs = (RelmUnique[])typeof(DuplicateUniqueModel)
                .GetCustomAttributes(typeof(RelmUnique), inherit: true);

            Assert.Equal(2, attrs.Length);
            Assert.All(attrs, a => Assert.True(a.ConstraintProperties!.SequenceEqual(new[] { "A", "B" })));
        }
    }
}
