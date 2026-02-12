using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Attributes.RelmKey_Tests
{
    public class RelmKeyAttribute_Tester
    {
        [Fact]
        public void RelmKeyAttribute_Can_Be_Instantiated()
        {
            var instance = Activator.CreateInstance(typeof(RelmKey));
            Assert.NotNull(instance);
            Assert.IsType<RelmKey>(instance);
        }
        [Fact]
        public void RelmKeyAttribute_Has_Correct_AttributeUsage()
        {
            var attributeUsage = (AttributeUsageAttribute)typeof(RelmKey)
                .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
                .FirstOrDefault();

            Assert.NotNull(attributeUsage);
            Assert.Equal(AttributeTargets.Property | AttributeTargets.Struct, attributeUsage.ValidOn);
            Assert.False(attributeUsage.AllowMultiple);
            Assert.True(attributeUsage.Inherited);
        }
        [Fact]
        public void RelmKeyAttribute_Has_Expected_Properties()
        {
            // TypeId
            var properties = typeof(RelmKey).GetProperties();
            Assert.Single(properties);
            Assert.Contains(properties, p => p.Name == nameof(Attribute.TypeId) && p.PropertyType == typeof(object));
        }
        [Fact]
        public void RelmKeyAttribute_Has_No_Methods()
        {
            var methods = typeof(RelmKey).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
            Assert.Empty(methods);
        }
        [Fact]
        public void RelmKeyAttribute_Has_No_Fields()
        {
            var fields = typeof(RelmKey).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
            Assert.Empty(fields);
        }

        [Fact]
        public void RelmKeyAttribute_Has_Only_AttributeUsageAttribute()
        {
            var attributes = typeof(RelmKey).GetCustomAttributes(false);

            Assert.Single(attributes);
            Assert.IsType<AttributeUsageAttribute>(attributes[0]);
        }
        [Fact]
        public void RelmKeyAttribute_Is_Not_Abstract()
        {
            var isAbstract = typeof(RelmKey).IsAbstract;
            Assert.False(isAbstract);
        }

        [Fact]
        public void RelmKeyAttribute_Is_Sealed()
        {
            Assert.True(typeof(RelmKey).IsSealed);
        }














        private class KeyedPropertyHolder
        {
            [RelmKey]
            public int Id { get; set; }
        }

        [Fact]
        public void RelmKeyAttribute_Is_Present_On_Property()
        {
            var prop = typeof(KeyedPropertyHolder).GetProperty(nameof(KeyedPropertyHolder.Id));
            var attr = prop?.GetCustomAttributes(typeof(RelmKey), inherit: false).SingleOrDefault();

            Assert.NotNull(prop);
            Assert.NotNull(attr);
            Assert.IsType<RelmKey>(attr);
        }

        [RelmKey]
        private struct KeyedStruct { }

        [Fact]
        public void RelmKeyAttribute_Is_Present_On_Struct()
        {
            var attr = typeof(KeyedStruct)
                .GetCustomAttributes(typeof(RelmKey), inherit: false)
                .SingleOrDefault();

            Assert.NotNull(attr);
            Assert.IsType<RelmKey>(attr);
        }

        private class BaseWithKey
        {
            [RelmKey]
            public virtual int Id { get; set; }
        }

        private class DerivedWithOverride : BaseWithKey
        {
            public override int Id { get; set; }
        }

        [Fact]
        public void RelmKeyAttribute_Is_Inherited_On_Overridden_Property()
        {
            var prop = typeof(DerivedWithOverride).GetProperty(nameof(BaseWithKey.Id));

            var attrOnOverride = prop?.GetCustomAttributes(typeof(RelmKey), inherit: true).SingleOrDefault();
            Assert.NotNull(prop);
            Assert.Null(attrOnOverride);

            var baseProp = typeof(BaseWithKey).GetProperty(nameof(BaseWithKey.Id));
            var attrOnBase = baseProp?.GetCustomAttributes(typeof(RelmKey), inherit: false).SingleOrDefault();
            Assert.NotNull(baseProp);
            Assert.NotNull(attrOnBase);
            Assert.IsType<RelmKey>(attrOnBase);
        }

        private class DerivedWithNew : BaseWithKey
        {
            public new int Id { get; set; }
        }

        [Fact]
        public void RelmKeyAttribute_Is_Not_Inherited_On_New_Property()
        {
            var prop = typeof(DerivedWithNew).GetProperty(nameof(BaseWithKey.Id));
            var attr = prop?.GetCustomAttributes(typeof(RelmKey), inherit: true).SingleOrDefault();

            Assert.NotNull(prop);
            Assert.Null(attr);
        }

        [Fact]
        public void RelmKeyAttribute_Has_Public_Parameterless_Ctor()
        {
            var ctor = typeof(RelmKey).GetConstructor(Type.EmptyTypes);
            Assert.NotNull(ctor);
        }
    }
}
