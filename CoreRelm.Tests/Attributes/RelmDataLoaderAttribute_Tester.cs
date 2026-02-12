using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Attributes
{
    public class RelmDataLoaderAttribute_Tester
    {
        [Fact]
        public void RelmDataLoaderAttribute_Can_Be_Instantiated()
        {
            var instance = Activator.CreateInstance(typeof(RelmDataLoader));
            Assert.NotNull(instance);
            Assert.IsType<RelmDataLoader>(instance);
        }
    
        [Fact]
        public void RelmDataLoaderAttribute_Has_Correct_AttributeUsage()
        {
            var attributeUsage = (AttributeUsageAttribute)typeof(RelmDataLoader).GetCustomAttributes(typeof(AttributeUsageAttribute), false).FirstOrDefault();
            Assert.NotNull(attributeUsage);
            Assert.Equal(AttributeTargets.Class, attributeUsage.ValidOn);
            Assert.False(attributeUsage.AllowMultiple);
            Assert.True(attributeUsage.Inherited);
        }

        [Fact]
        public void RelmDataLoaderAttribute_Has_No_Properties()
        {
            var properties = typeof(RelmDataLoader).GetProperties();
            Assert.Empty(properties);
        }

        [Fact]
        public void RelmDataLoaderAttribute_Has_No_Methods()
        {
            var methods = typeof(RelmDataLoader).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
            Assert.Empty(methods);
        }

        [Fact]
        public void RelmDataLoaderAttribute_Has_No_Fields()
        {
            var fields = typeof(RelmDataLoader).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
            Assert.Empty(fields);
        }

        [Fact]
        public void RelmDataLoaderAttribute_Has_No_Additional_Attributes()
        {
            var attributes = typeof(RelmDataLoader).GetCustomAttributes(false);
            Assert.Empty(attributes);
        }

        [Fact]
        public void RelmDataLoaderAttribute_Is_Not_Abstract()
        {
            var isAbstract = typeof(RelmDataLoader).IsAbstract;
            Assert.False(isAbstract);
        }
    }
}
