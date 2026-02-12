using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Triggers;

namespace CoreRelm.Tests.Attributes.RelmTable_Tests
{
    public class RelmTableAttribute_Tester
    {
        [RelmTable("Users")]
        private class UsersTable { }

        [RelmTable("AuditEvents")]
        private struct AuditEventsTable { }

        [RelmTable("BaseTable")]
        private class BaseTable { }

        private class DerivedTable : BaseTable { }

        [Fact]
        public void RelmTableAttribute_Parameterless_Constructor_Throws()
        {
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(RelmTable)));
        }

        [Fact]
        public void RelmTableAttribute_Can_Be_Instantiated_With_Activator()
        {
            var instance = Activator.CreateInstance(typeof(RelmTable), ["test_table"]);
            Assert.NotNull(instance);
            Assert.IsType<RelmTable>(instance);
        }

        [Fact]
        public void RelmTableAttribute_Can_Be_Instantiated()
        {
            var instance = new RelmTable("Test");
            Assert.NotNull(instance);
            Assert.IsType<RelmTable>(instance);
        }

        [Fact]
        public void RelmTableAttribute_Has_Correct_AttributeUsage()
        {
            var usage = (AttributeUsageAttribute)typeof(RelmTable)
                .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
                .First();

            Assert.NotNull(usage);
            Assert.Equal(AttributeTargets.Class | AttributeTargets.Struct, usage.ValidOn);
            Assert.False(usage.AllowMultiple);
            Assert.True(usage.Inherited);
        }

        [Fact]
        public void RelmTableAttribute_Has_Expected_Properties()
        {
            // TableName,TypeId
            var properties = typeof(RelmTable).GetProperties();
            Assert.Equal(2, properties.Length);
            Assert.Contains(properties, p => p.Name == nameof(RelmTable.TableName) && p.PropertyType == typeof(string));
            Assert.Contains(properties, p => p.Name == nameof(Attribute.TypeId) && p.PropertyType == typeof(object));
        }

        [Fact]
        public void RelmTableAttribute_Has_Expected_Methods()
        {
            var methods = typeof(RelmTable).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            Assert.Single(methods);
            Assert.Contains(methods, m => m.Name == "get_TableName");
        }

        [Fact]
        public void RelmTableAttribute_Has_Expected_Fields()
        {
            var fields = typeof(RelmTable).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            Assert.Single(fields);
            Assert.Contains(fields, f => f.Name == "<TableName>k__BackingField" && f.FieldType == typeof(string));
        }

        [Fact]
        public void RelmTriggerAttribute_Has_Attribute_Usage()
        {
            var attributes = typeof(RelmTrigger).GetCustomAttributes(false);
            Assert.Contains(attributes, a => a.GetType() == typeof(AttributeUsageAttribute));
        }

        [Fact]
        public void RelmTableAttribute_Sets_TableName()
        {
            var instance = new RelmTable("MyTable");
            Assert.Equal("MyTable", instance.TableName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void RelmTableAttribute_Throws_On_NullOrWhitespace_TableName(string? tableName)
        {
            Assert.Throws<ArgumentNullException>(() => new RelmTable(tableName!));
        }

        [Fact]
        public void RelmTableAttribute_Has_Single_Public_Instance_Property_TableName_With_Private_Setter()
        {
            var properties = typeof(RelmTable).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Assert.Equal(2, properties.Length);

            var tableNameProp = properties.First();
            Assert.Equal(nameof(RelmTable.TableName), tableNameProp.Name);
            Assert.True(tableNameProp.CanRead);
            Assert.True(tableNameProp.CanWrite);
            Assert.True(tableNameProp.SetMethod?.IsPrivate);
        }

        [Fact]
        public void RelmTableAttribute_Has_Single_Public_Constructor_With_String_Parameter()
        {
            var constructors = typeof(RelmTable).GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            Assert.Single(constructors);

            var ctor = constructors.Single();
            var parameters = ctor.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(string), parameters[0].ParameterType);
        }

        [Fact]
        public void RelmTableAttribute_Applies_To_Class_And_Stores_Name()
        {
            var attr = (RelmTable)typeof(UsersTable).GetCustomAttributes(typeof(RelmTable), false).Single();
            Assert.Equal("Users", attr.TableName);
        }

        [Fact]
        public void RelmTableAttribute_Applies_To_Struct_And_Stores_Name()
        {
            var attr = (RelmTable)typeof(AuditEventsTable).GetCustomAttributes(typeof(RelmTable), false).Single();
            Assert.Equal("AuditEvents", attr.TableName);
        }

        [Fact]
        public void RelmTableAttribute_Is_Inherited_By_Derived_Types()
        {
            var attr = (RelmTable?)Attribute.GetCustomAttribute(typeof(DerivedTable), typeof(RelmTable), inherit: true);
            Assert.NotNull(attr);
            Assert.Equal("BaseTable", attr!.TableName);
        }

        [Fact]
        public void RelmTableAttribute_Is_Not_Abstract()
        {
            Assert.False(typeof(RelmTable).IsAbstract);
        }














        [Fact]
        public void RelmTable_Constructor_Preserves_Inner_And_Outer_Whitespace()
        {
            var name = "  spaced  name  ";
            var attr = new RelmTable(name);
            Assert.Equal(name, attr.TableName); // no trimming occurs
        }

        [Fact]
        public void RelmTable_Constructor_Allows_NonEmpty_Whitespace_Inside()
        {
            var name = "a  b  c";
            var attr = new RelmTable(name);
            Assert.Equal("a  b  c", attr.TableName);
        }

        [Fact]
        public void RelmTable_Inherit_False_Does_Not_Resolve_Base_Attribute()
        {
            var attrs = typeof(DerivedTable).GetCustomAttributes(typeof(RelmTable), inherit: false);
            Assert.Empty(attrs);
        }

        [Fact]
        public void RelmTable_Inherit_True_Resolves_Base_Attribute()
        {
            var attrs = typeof(DerivedTable).GetCustomAttributes(typeof(RelmTable), inherit: true);
            Assert.Single(attrs);
            var attr = (RelmTable)attrs[0];
            Assert.Equal("BaseTable", attr.TableName);
        }

        [Fact]
        public void RelmTable_TypeId_Is_Accessible()
        {
            var attr = new RelmTable("name");
            Assert.NotNull(attr.TypeId);
        }
    }
}
