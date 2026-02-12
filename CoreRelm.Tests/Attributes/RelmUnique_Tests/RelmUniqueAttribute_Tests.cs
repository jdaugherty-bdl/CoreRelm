using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Attributes.RelmUnique_Tests
{
    public class RelmUniqueAttribute_Tests
    {
        [RelmUnique([nameof(UniqueUserModel.Name), nameof(UniqueUserModel.Email)])]
        [RelmUnique([nameof(UniqueUserModel.TenantId), nameof(UniqueUserModel.Username)])]
        public class UniqueUserModel
        {
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
            public int TenantId { get; set; }
            public string Username { get; set; } = "";
        }

        [RelmUnique([nameof(UniqueCodeStruct.Code)])]
        public struct UniqueCodeStruct
        {
            public string Code { get; set; }
        }

        [Fact]
        public void RelmUniqueAttribute_Cannot_Be_Instantiated_With_Parameterless_Constructor()
        {
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(RelmUnique)));
        }

        [Fact]
        public void RelmUniqueAttribute_Has_No_Properties()
        {
            var properties = typeof(RelmUnique).GetProperties();
            Assert.NotEmpty(properties);
            Assert.Equal(2, properties.Length); // get/set for ConstraintProperties
            Assert.Contains(properties, m => m.Name == nameof(RelmUnique.ConstraintProperties));
            Assert.Contains(properties, m => m.Name == nameof(Attribute.TypeId));
        }

        [Fact]
        public void RelmUniqueAttribute_Has_Two_Backing_Methods()
        {
            var methods = typeof(RelmUnique).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
            Assert.NotEmpty(methods);
            Assert.Equal(2, methods.Length); // get/set for ConstraintProperties
            Assert.Contains(methods, m => m.Name == "get_ConstraintProperties");
            Assert.Contains(methods, m => m.Name == "set_ConstraintProperties");
        }

        [Fact]
        public void RelmUniqueAttribute_Has_One_Field()
        {
            var fields = typeof(RelmUnique).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
            Assert.NotEmpty(fields);
            Assert.Single(fields);
            Assert.Contains(fields, f => f.Name == "<ConstraintProperties>k__BackingField");
        }

        [Fact]
        public void RelmUniqueAttribute_Has_One_Additional_Attribute()
        {
            var attributes = typeof(RelmUnique).GetCustomAttributes(false);
            Assert.NotEmpty(attributes);
            Assert.Single(attributes);
            Assert.Contains(attributes!, a => a is AttributeUsageAttribute);
        }

        [Fact]
        public void RelmUniqueAttribute_Is_Not_Abstract()
        {
            var isAbstract = typeof(RelmUnique).IsAbstract;
            Assert.False(isAbstract);
        }












        [Fact]
        public void RelmUniqueAttribute_Can_Be_Instantiated_With_Valid_Properties()
        {
            var props = new[] { "Name", "Email" };
            var instance = new RelmUnique(props);

            Assert.NotNull(instance);
            Assert.IsType<RelmUnique>(instance);
            Assert.Equal(props, instance.ConstraintProperties);
        }

        [Fact]
        public void RelmUniqueAttribute_Constructor_Throws_On_Null()
        {
            Assert.Throws<ArgumentException>(() => new RelmUnique(null!));
        }

        [Fact]
        public void RelmUniqueAttribute_Constructor_Throws_On_Empty_Array()
        {
            Assert.Throws<ArgumentException>(() => new RelmUnique([]));
        }

        [Fact]
        public void RelmUniqueAttribute_Has_Correct_AttributeUsage()
        {
            var attributeUsage = (AttributeUsageAttribute)typeof(RelmUnique)
                .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
                .FirstOrDefault();

            Assert.NotNull(attributeUsage);
            Assert.True(attributeUsage.ValidOn.HasFlag(AttributeTargets.Class));
            Assert.True(attributeUsage.ValidOn.HasFlag(AttributeTargets.Struct));
            Assert.True(attributeUsage.AllowMultiple);
            Assert.True(attributeUsage.Inherited); // default is true
        }

        [Fact]
        public void RelmUniqueAttribute_Has_ConstraintProperties_Property()
        {
            var properties = typeof(RelmUnique).GetProperties();
            Assert.Contains(properties, p => p.Name == nameof(RelmUnique.ConstraintProperties) && p.CanRead && p.CanWrite);
        }

        [Fact]
        public void RelmUniqueAttribute_Has_Public_Constructor_Taking_StringArray()
        {
            var ctor = typeof(RelmUnique)
                .GetConstructors()
                .FirstOrDefault(c =>
                {
                    var parms = c.GetParameters();
                    return parms.Length == 1 && parms[0].ParameterType == typeof(string[]);
                });

            Assert.NotNull(ctor);
        }

        [Fact]
        public void RelmUniqueAttribute_Has_AttributeUsageAttribute()
        {
            var attributes = typeof(RelmUnique).GetCustomAttributes(false);
            Assert.Contains(attributes, a => a is AttributeUsageAttribute);
        }

        [Fact]
        public void RelmUniqueAttribute_Is_Sealed_And_Not_Abstract()
        {
            var type = typeof(RelmUnique);
            Assert.True(type.IsSealed);
            Assert.False(type.IsAbstract);
        }

        [Fact]
        public void Multiple_RelmUnique_On_Class_Are_Present_With_Correct_Constraints()
        {
            var attrs = (RelmUnique[])typeof(UniqueUserModel)
                .GetCustomAttributes(typeof(RelmUnique), inherit: true);

            Assert.Equal(2, attrs.Length);
            Assert.Contains(attrs, a => a.ConstraintProperties!.SequenceEqual([nameof(UniqueUserModel.Name), nameof(UniqueUserModel.Email)]));
            Assert.Contains(attrs, a => a.ConstraintProperties!.SequenceEqual([nameof(UniqueUserModel.TenantId), nameof(UniqueUserModel.Username)]));
        }

        [Fact]
        public void RelmUnique_Can_Be_Applied_To_Struct()
        {
            var attrs = (RelmUnique[])typeof(UniqueCodeStruct)
                .GetCustomAttributes(typeof(RelmUnique), inherit: true);

            Assert.Single(attrs);
            Assert.Equal([nameof(UniqueCodeStruct.Code)], attrs[0].ConstraintProperties);
        }
    }
}
