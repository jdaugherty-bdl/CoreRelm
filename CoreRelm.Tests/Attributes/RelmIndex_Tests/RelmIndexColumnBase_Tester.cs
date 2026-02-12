using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Attributes.RelmIndex_Tests
{
    public class RelmIndexColumnBase_Tester
    {
        [Fact]
        public void RelmIndexColumnBase_Has_Correct_Base_Class()
        {
            var relmIndexColumnBaseType = typeof(RelmIndexColumnBase_Tester).BaseType;
            Assert.Equal(typeof(object), relmIndexColumnBaseType);
        }
        [Fact]
        public void RelmIndexColumnBase_Can_Be_Instantiated()
        {
            var instance = Activator.CreateInstance(typeof(RelmIndexColumnBase_Tester));
            Assert.NotNull(instance);
            Assert.IsType<RelmIndexColumnBase_Tester>(instance);
        }
        [Fact]
        public void RelmIndexColumnBase_Has_No_Properties()
        {
            var properties = typeof(RelmIndexColumnBase_Tester).GetProperties();
            Assert.Empty(properties);
        }
        [Fact]
        public void RelmIndexColumnBase_Has_No_Methods()
        {
            var methods = typeof(RelmIndexColumnBase_Tester).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
            Assert.Empty(methods);
        }
        [Fact]
        public void RelmIndexColumnBase_Has_No_Attributes()
        {
            var attributes = typeof(RelmIndexColumnBase_Tester).GetCustomAttributes(false);
            Assert.Empty(attributes);
        }
        [Fact]
        public void RelmIndexColumnBase_Is_Not_Abstract()
        {
            var isAbstract = typeof(RelmIndexColumnBase_Tester).IsAbstract;
            Assert.False(isAbstract);
        }

        [Fact]
        public void RelmIndexColumnBase_Has_No_Fields()
        {
            var fields = typeof(RelmIndexColumnBase_Tester).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
            Assert.Empty(fields);
        }
    }
}
