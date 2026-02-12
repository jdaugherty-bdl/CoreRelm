using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Attributes.RelmUnique_Tests
{
    [RelmUnique([nameof(BaseWithUnique.Id)])]
    public class BaseWithUnique
    {
        public string Id { get; set; } = "";
    }

    public class DerivedWithoutUnique : BaseWithUnique
    {
    }

    public class RelmUnique_ReflectionInheritFlag_Tests
    {
        [Fact]
        public void GetCustomAttributes_With_Inherit_False_Does_Not_Include_Base_Attributes()
        {
            var attrsDerivedNoInherit = (RelmUnique[])typeof(DerivedWithoutUnique)
                .GetCustomAttributes(typeof(RelmUnique), inherit: false);

            Assert.Empty(attrsDerivedNoInherit);
        }

        [Fact]
        public void GetCustomAttributes_With_Inherit_True_Includes_Base_Attributes()
        {
            var attrsDerivedInherit = (RelmUnique[])typeof(DerivedWithoutUnique)
                .GetCustomAttributes(typeof(RelmUnique), inherit: true);

            Assert.Single(attrsDerivedInherit);
            Assert.Equal([nameof(BaseWithUnique.Id)], attrsDerivedInherit[0].ConstraintProperties);
        }
    }
}
