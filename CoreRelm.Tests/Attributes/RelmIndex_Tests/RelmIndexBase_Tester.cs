using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Attributes.RelmIndex_Tests
{
    public class RelmIndexBase_Tester
    {
        [Fact]
        public void RelmIndexBase_Has_Correct_Base_Class()
        {
            var relmIndexBaseType = typeof(RelmIndexBase_Tester).BaseType;
            Assert.Equal(typeof(object), relmIndexBaseType);
        }

        [Fact]
        public void RelmIndexBase_Can_Be_Instantiated()
        {
            var instance = Activator.CreateInstance(typeof(RelmIndexBase_Tester));
            Assert.NotNull(instance);
            Assert.IsType<RelmIndexBase_Tester>(instance);
        }

        [Fact]
        public void RelmIndexBase_Has_No_Properties()
        {
            var properties = typeof(RelmIndexBase_Tester).GetProperties();
            Assert.Empty(properties);
        }

        [Fact]
        public void RelmIndexBase_Has_No_Methods()
        {
            var methods = typeof(RelmIndexBase_Tester).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
            Assert.Empty(methods);
        }

        [Fact]
        public void RelmIndexBase_Has_No_Attributes()
        {
            var attributes = typeof(RelmIndexBase_Tester).GetCustomAttributes(false);
            Assert.Empty(attributes);
        }

        [Fact]
        public void RelmIndexBase_Is_Not_Abstract()
        {
            var isAbstract = typeof(RelmIndexBase_Tester).IsAbstract;
            Assert.False(isAbstract);
        }
    }
}
