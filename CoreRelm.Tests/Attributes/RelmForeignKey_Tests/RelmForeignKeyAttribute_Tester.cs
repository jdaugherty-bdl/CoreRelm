using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.ForeignKeys;

namespace CoreRelm.Tests.Attributes.RelmForeignKey_Tests
{
    public class RelmForeignKeyAttribute_Tester
    {
        [Fact]
        public void RelmForeignKeyAttribute_Can_Be_Instantiated_SingleKey()
        {

            var instance = Activator.CreateInstance(typeof(RelmForeignKey), string.Empty, null, null, ReferentialAction.Cascade);
            Assert.NotNull(instance);
            Assert.IsType<RelmForeignKey>(instance);
        }

        [Fact]
        public void RelmForeignKeyAttribute_Can_Be_Instantiated_MultipleKeys()
        {

            var instance = Activator.CreateInstance(typeof(RelmForeignKey), args: [new[] { string.Empty }, null, null, ReferentialAction.Cascade]);
            Assert.NotNull(instance);
            Assert.IsType<RelmForeignKey>(instance);
        }

        [Fact]
        public void RelmForeignKeyAttribute_Has_Correct_AttributeUsage()
        {
            var attributeUsage = (AttributeUsageAttribute)typeof(RelmForeignKey)
                .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
                .FirstOrDefault();

            Assert.NotNull(attributeUsage);
            Assert.Equal(AttributeTargets.Property | AttributeTargets.Struct, attributeUsage.ValidOn);
            Assert.False(attributeUsage.AllowMultiple);
            Assert.True(attributeUsage.Inherited);
        }

        [Fact]
        public void RelmForeignKeyAttribute_Has_No_Properties()
        {
            var properties = typeof(RelmForeignKey).GetProperties();

            Assert.Contains(nameof(RelmForeignKey.ForeignKeys), properties.Select(p => p.Name));
            Assert.Contains(nameof(RelmForeignKey.LocalKeys), properties.Select(p => p.Name));
            Assert.Contains(nameof(RelmForeignKey.OrderBy), properties.Select(p => p.Name));
            Assert.Contains(nameof(RelmForeignKey.OnDelete), properties.Select(p => p.Name));
        }

        [Fact]
        public void RelmForeignKeyAttribute_Has_No_Methods()
        {
            var methods = typeof(RelmForeignKey).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);

            Assert.Contains(methods, m => m.Name == "get_ForeignKeys");
            Assert.Contains(methods, m => m.Name == "get_LocalKeys");
            Assert.Contains(methods, m => m.Name == "get_OrderBy");
            Assert.Contains(methods, m => m.Name == "get_OnDelete");
        }

        [Fact]
        public void RelmForeignKeyAttribute_Has_No_Fields()
        {
            var fields = typeof(RelmForeignKey).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);

            Assert.Contains(fields, f => f.Name == "<ForeignKeys>k__BackingField");
            Assert.Contains(fields, f => f.Name == "<LocalKeys>k__BackingField");
            Assert.Contains(fields, f => f.Name == "<OrderBy>k__BackingField");
            Assert.Contains(fields, f => f.Name == "<OnDelete>k__BackingField");
        }

        [Fact]
        public void RelmForeignKeyAttribute_Has_No_Additional_Attributes()
        {
            var attributes = typeof(RelmForeignKey).GetCustomAttributes(false);
            Assert.Contains(attributes, attr => attr.GetType() == typeof(AttributeUsageAttribute));
        }

        [Fact]
        public void RelmForeignKeyAttribute_Is_Not_Abstract()
        {
            var isAbstract = typeof(RelmForeignKey).IsAbstract;
            Assert.False(isAbstract);
        }















        [Fact]
        public void RelmForeignKeyAttribute_Is_Sealed_Attribute()
        {
            Assert.True(typeof(RelmForeignKey).IsSealed);
            Assert.True(typeof(RelmForeignKey).IsSubclassOf(typeof(Attribute)));
        }

        [Fact]
        public void Default_Ctor_Sets_Nulls_And_NoAction()
        {
            var attribute = new RelmForeignKey(string.Empty);

            Assert.Null(attribute.ForeignKeys);
            Assert.Null(attribute.LocalKeys);
            Assert.Null(attribute.OrderBy);
            Assert.Equal(ReferentialAction.NoAction, attribute.OnDelete);
        }

        [Fact]
        public void Single_Value_Ctor_Populates_Single_Element_Arrays()
        {
            var attribute = new RelmForeignKey("fk_id", "id", "name ASC", ReferentialAction.Cascade);

            Assert.Equal(new[] { "fk_id" }, attribute.ForeignKeys);
            Assert.Equal(new[] { "id" }, attribute.LocalKeys);
            Assert.Equal(new[] { "name ASC" }, attribute.OrderBy);
            Assert.Equal(ReferentialAction.Cascade, attribute.OnDelete);
        }

        [Fact]
        public void Array_Ctor_Assigns_Arrays_Directly()
        {
            var foreignKeys = new[] { "fk1", "fk2" };
            var localKeys = new[] { "id1", "id2" };
            var orderBy = new[] { "col1 DESC", "col2 ASC" };

            var attribute = new RelmForeignKey(foreignKeys, localKeys, orderBy, ReferentialAction.SetNull);

            Assert.Same(foreignKeys, attribute.ForeignKeys);
            Assert.Same(localKeys, attribute.LocalKeys);
            Assert.Same(orderBy, attribute.OrderBy);
            Assert.Equal(ReferentialAction.SetNull, attribute.OnDelete);
        }

        [Fact]
        public void Array_Ctor_Allows_Nulls()
        {
            var attribute = new RelmForeignKey((string?)null, null, null, ReferentialAction.Restrict);

            Assert.Null(attribute.ForeignKeys);
            Assert.Null(attribute.LocalKeys);
            Assert.Null(attribute.OrderBy);
            Assert.Equal(ReferentialAction.Restrict, attribute.OnDelete);
        }
    }
}
