using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Attributes.RelmUnique_Tests
{
    [RelmUnique([nameof(BaseEntity.ExternalId)])]
    public class BaseEntity
    {
        public string ExternalId { get; set; } = "";
    }

    public class DerivedEntity : BaseEntity
    {
        public string Name { get; set; } = "";
    }


    public class RelmUnique_Inheritance_Tests
    {
        [Fact]
        public void RelmUnique_Is_Inherited_From_Base_Class()
        {
            var attrsBase = (RelmUnique[])typeof(BaseEntity).GetCustomAttributes(typeof(RelmUnique), inherit: true);
            var attrsDerived = (RelmUnique[])typeof(DerivedEntity).GetCustomAttributes(typeof(RelmUnique), inherit: true);

            Assert.Single(attrsBase);
            Assert.Single(attrsDerived);
            Assert.Equal(attrsBase[0].ConstraintProperties, attrsDerived[0].ConstraintProperties);
        }
    }
}
