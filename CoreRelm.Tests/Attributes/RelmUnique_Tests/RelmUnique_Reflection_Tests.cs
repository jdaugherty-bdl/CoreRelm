using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Attributes.RelmUnique_Tests
{
    public class RelmUnique_Reflection_Tests
    {
        [Fact]
        public void RelmUnique_Has_Single_Public_StringArray_Ctor()
        {
            var ctors = typeof(RelmUnique).GetConstructors();
            Assert.Single(ctors);
            var parms = ctors[0].GetParameters();
            Assert.Single(parms);
            Assert.Equal(typeof(string[]), parms[0].ParameterType);
        }

        [Fact]
        public void RelmUnique_Has_ReadWrite_ConstraintProperties()
        {
            var prop = typeof(RelmUnique).GetProperty(nameof(RelmUnique.ConstraintProperties));
            Assert.NotNull(prop);
            Assert.True(prop!.CanRead);
            Assert.True(prop!.CanWrite);
        }
    }
}
