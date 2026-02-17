using CoreRelm.Attributes;
using Org.BouncyCastle.Asn1.Cms;
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
        public void RelmDataLoaderAttribute_Can_Be_Instantiated_One_Parameter()
        {
            var instance = Activator.CreateInstance(typeof(RelmDataLoader), typeof(object));
            Assert.NotNull(instance);
            Assert.IsType<RelmDataLoader>(instance);
        }

        [Fact]
        public void RelmDataLoaderAttribute_Can_Be_Instantiated_Two_Parameters_Single()
        {
            var instance = Activator.CreateInstance(typeof(RelmDataLoader), typeof(object), string.Empty);
            Assert.NotNull(instance);
            Assert.IsType<RelmDataLoader>(instance);
        }

        [Fact]
        public void RelmDataLoaderAttribute_Can_Be_Instantiated_Two_Parameters_Multiple()
        {
            var instance = Activator.CreateInstance(typeof(RelmDataLoader), args: [typeof(object), new[] { string.Empty }]);
            Assert.NotNull(instance);
            Assert.IsType<RelmDataLoader>(instance);
        }

        [Fact]
        public void RelmDataLoaderAttribute_Has_Correct_AttributeUsage()
        {
            var attributeUsage = (AttributeUsageAttribute)typeof(RelmDataLoader).GetCustomAttributes(typeof(AttributeUsageAttribute), false).FirstOrDefault();
            Assert.NotNull(attributeUsage);
            Assert.Equal(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property, attributeUsage.ValidOn);
            Assert.True(attributeUsage.AllowMultiple);
            Assert.True(attributeUsage.Inherited);
        }

        [Fact]
        public void RelmDataLoaderAttribute_Has_No_Properties()
        {
            var properties = typeof(RelmDataLoader).GetProperties();

            Assert.Contains(nameof(RelmDataLoader.LoaderType), properties.Select(p => p.Name));
            Assert.Contains(nameof(RelmDataLoader.KeyFields), properties.Select(p => p.Name));
        }

        [Fact]
        public void RelmDataLoaderAttribute_Has_No_Methods()
        {
            var methods = typeof(RelmDataLoader).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);

            Assert.Contains(methods, m => m.Name == $"get_{nameof(RelmDataLoader.LoaderType)}");
            Assert.Contains(methods, m => m.Name == $"get_{nameof(RelmDataLoader.KeyFields)}");
        }

        [Fact]
        public void RelmDataLoaderAttribute_Has_No_Fields()
        {
            var fields = typeof(RelmDataLoader).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);

            Assert.Contains(fields, f => f.Name == $"<{nameof(RelmDataLoader.LoaderType)}>k__BackingField");
            Assert.Contains(fields, f => f.Name == $"<{nameof(RelmDataLoader.KeyFields)}>k__BackingField");
        }

        [Fact]
        public void RelmDataLoaderAttribute_Has_No_Additional_Attributes()
        {
            var attributes = typeof(RelmDataLoader).GetCustomAttributes(false);
            Assert.Contains(attributes, attr => attr.GetType() == typeof(AttributeUsageAttribute));
        }

        [Fact]
        public void RelmDataLoaderAttribute_Is_Not_Abstract()
        {
            var isAbstract = typeof(RelmDataLoader).IsAbstract;
            Assert.False(isAbstract);
        }
    }
}
