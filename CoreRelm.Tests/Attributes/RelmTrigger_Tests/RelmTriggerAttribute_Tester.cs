using CoreRelm.Attributes;
using CoreRelm.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Triggers;

namespace CoreRelm.Tests.Attributes.RelmTrigger_Tests
{
    public class RelmTriggerAttribute_Tester
    {
        [RelmTrigger(TriggerTime.BEFORE, TriggerEvent.DELETE, "base body")]
        private class BaseWithTrigger { }

        private class DerivedNoTrigger : BaseWithTrigger { }

        private class SampleClassWithTrigger
        {
        }

        [RelmTrigger(Triggers.TriggerTime.BEFORE, Triggers.TriggerEvent.INSERT, "body", "classTrigger")]
        private sealed class DecoratedSampleClassWithTrigger : SampleClassWithTrigger
        {
        }

        [RelmTrigger(Triggers.TriggerTime.AFTER, Triggers.TriggerEvent.UPDATE, "body", "structTrigger")]
        private struct SampleStructWithTrigger
        {
        }

        private class DerivedFromBaseWithTrigger : BaseWithTrigger
        {
        }

        [Fact]
        public void RelmTriggerAttribute_Parameterless_Constructor_Throws_Exception()
        {
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(RelmTrigger)));
        }

        [Fact]
        public void RelmTriggerAttribute_Can_Be_Instantiated()
        {
            var instance = Activator.CreateInstance(typeof(RelmTrigger), [TriggerTime.BEFORE, TriggerEvent.INSERT, "body", "my_trigger", TriggerOrdering.PRECEDES, "other_trigger"]);
            Assert.NotNull(instance);
            Assert.IsType<RelmTrigger>(instance);
        }

        [Fact]
        public void RelmTriggerAttribute_Has_Correct_AttributeUsage()
        {
            var attributeUsage = (AttributeUsageAttribute)typeof(RelmTrigger).GetCustomAttributes(typeof(AttributeUsageAttribute), false).FirstOrDefault();
            Assert.NotNull(attributeUsage);
            Assert.Equal(AttributeTargets.Class | AttributeTargets.Struct, attributeUsage.ValidOn);
            Assert.False(attributeUsage.AllowMultiple);
            Assert.True(attributeUsage.Inherited);
        }

        [Fact]
        public void RelmTriggerAttribute_Has_Expected_Properties()
        {
            // TriggerTime,TriggerEvent,TriggerBody,TriggerName,TriggerOrder,OtherTriggerName,TypeId
            var properties = typeof(RelmTrigger).GetProperties();
            Assert.Equal(7, properties.Length);
            Assert.Contains(properties, p => p.Name == "TriggerTime");
            Assert.Contains(properties, p => p.Name == "TriggerEvent");
            Assert.Contains(properties, p => p.Name == "TriggerBody");
            Assert.Contains(properties, p => p.Name == "TriggerName");
            Assert.Contains(properties, p => p.Name == "TriggerOrder");
            Assert.Contains(properties, p => p.Name == "OtherTriggerName");
            Assert.Contains(properties, p => p.Name == "TypeId");
        }

        [Fact]
        public void RelmTriggerAttribute_Has_Expected_Methods()
        {
            var methods = typeof(RelmTrigger).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
            Assert.Equal(12, methods.Length);
            Assert.Contains(methods, p => p.Name == "get_TriggerTime");
            Assert.Contains(methods, p => p.Name == "get_TriggerEvent");
            Assert.Contains(methods, p => p.Name == "get_TriggerBody");
            Assert.Contains(methods, p => p.Name == "get_TriggerName");
            Assert.Contains(methods, p => p.Name == "get_TriggerOrder");
            Assert.Contains(methods, p => p.Name == "get_OtherTriggerName");
            Assert.Contains(methods, p => p.Name == "set_TriggerTime");
            Assert.Contains(methods, p => p.Name == "set_TriggerEvent");
            Assert.Contains(methods, p => p.Name == "set_TriggerBody");
            Assert.Contains(methods, p => p.Name == "set_TriggerName");
            Assert.Contains(methods, p => p.Name == "set_TriggerOrder");
            Assert.Contains(methods, p => p.Name == "set_OtherTriggerName");
        }

        [Fact]
        public void RelmTriggerAttribute_Has_Expected_Fields()
        {
            var fields = typeof(RelmTrigger).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
            Assert.Equal(6, fields.Length);
            Assert.Contains(fields, f => f.Name == "<TriggerTime>k__BackingField");
            Assert.Contains(fields, f => f.Name == "<TriggerEvent>k__BackingField");
            Assert.Contains(fields, f => f.Name == "<TriggerBody>k__BackingField");
            Assert.Contains(fields, f => f.Name == "<TriggerName>k__BackingField");
            Assert.Contains(fields, f => f.Name == "<TriggerOrder>k__BackingField");
            Assert.Contains(fields, f => f.Name == "<OtherTriggerName>k__BackingField");
        }

        [Fact]
        public void RelmTriggerAttribute_Has_Attribute_Usage()
        {
            var attributes = typeof(RelmTrigger).GetCustomAttributes(false);
            Assert.Contains(attributes, a => a.GetType() == typeof(AttributeUsageAttribute));
        }

        [Fact]
        public void RelmTriggerAttribute_Is_Not_Abstract()
        {
            var isAbstract = typeof(RelmTrigger).IsAbstract;
            Assert.False(isAbstract);
        }

        [Fact]
        public void RelmTrigger_Ctor_Assigns_All_Properties()
        {
            var attr = new RelmTrigger(
                TriggerTime.BEFORE,
                TriggerEvent.INSERT,
                "body",
                "my_trigger",
                TriggerOrdering.PRECEDES,
                "other_trigger");

            Assert.Equal(TriggerTime.BEFORE, attr.TriggerTime);
            Assert.Equal(TriggerEvent.INSERT, attr.TriggerEvent);
            Assert.Equal("body", attr.TriggerBody);
            Assert.Equal("my_trigger", attr.TriggerName);
            Assert.Equal(TriggerOrdering.PRECEDES, attr.TriggerOrder);
            Assert.Equal("other_trigger", attr.OtherTriggerName);
        }

        [Fact]
        public void RelmTrigger_TriggerBody_Is_Trimmed()
        {
            var attr = new RelmTrigger(
                TriggerTime.AFTER,
                TriggerEvent.UPDATE,
                "  body  ");

            Assert.Equal("body", attr.TriggerBody);
        }

        [Fact]
        public void RelmTrigger_Defaults_Are_Set()
        {
            var attr = new RelmTrigger(
                TriggerTime.AFTER,
                TriggerEvent.DELETE,
                "do stuff");

            Assert.Null(attr.TriggerName);
            Assert.Equal(TriggerOrdering.FOLLOWS, attr.TriggerOrder);
            Assert.Null(attr.OtherTriggerName);
        }

        [Fact]
        public void RelmTrigger_Properties_Are_Settable()
        {
            var attr = new RelmTrigger(
                TriggerTime.AFTER,
                TriggerEvent.DELETE,
                "body");

            attr.TriggerTime = TriggerTime.BEFORE;
            attr.TriggerEvent = TriggerEvent.INSERT;
            attr.TriggerBody = "new";
            attr.TriggerName = "name";
            attr.TriggerOrder = TriggerOrdering.PRECEDES;
            attr.OtherTriggerName = "other";

            Assert.Equal(TriggerTime.BEFORE, attr.TriggerTime);
            Assert.Equal(TriggerEvent.INSERT, attr.TriggerEvent);
            Assert.Equal("new", attr.TriggerBody);
            Assert.Equal("name", attr.TriggerName);
            Assert.Equal(TriggerOrdering.PRECEDES, attr.TriggerOrder);
            Assert.Equal("other", attr.OtherTriggerName);
        }

        [Fact]
        public void RelmTrigger_Has_Correct_AttributeUsage()
        {
            var usage = (AttributeUsageAttribute)typeof(RelmTrigger)
                .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
                .First();

            Assert.Equal(AttributeTargets.Class | AttributeTargets.Struct, usage.ValidOn);
            Assert.False(usage.AllowMultiple);
            Assert.True(usage.Inherited);
        }

        [Fact]
        public void RelmTrigger_Is_Attribute_And_Not_Abstract()
        {
            Assert.True(typeof(Attribute).IsAssignableFrom(typeof(RelmTrigger)));
            Assert.False(typeof(RelmTrigger).IsAbstract);
        }

        [Fact]
        public void RelmTrigger_TriggerBody_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new RelmTrigger(
                    TriggerTime.BEFORE,
                    TriggerEvent.INSERT,
                    triggerBody: null!));
        }

        [Fact]
        public void RelmTrigger_TriggerBody_WhitespaceOnly_Becomes_Empty()
        {
            var attr = new RelmTrigger(
                Triggers.TriggerTime.BEFORE,
                Triggers.TriggerEvent.INSERT,
                "   ");

            Assert.Equal(string.Empty, attr.TriggerBody);
        }

        [Fact]
        public void RelmTrigger_TriggerBody_Tabs_And_Newlines_Are_Trimmed()
        {
            var attr = new RelmTrigger(
                Triggers.TriggerTime.AFTER,
                Triggers.TriggerEvent.UPDATE,
                " \tbody\r\n ");

            Assert.Equal("body", attr.TriggerBody);
        }

        [Fact]
        public void RelmTrigger_TriggerName_Allows_Empty_String()
        {
            var attr = new RelmTrigger(
                Triggers.TriggerTime.BEFORE,
                Triggers.TriggerEvent.INSERT,
                "body",
                triggerName: string.Empty);

            Assert.Equal(string.Empty, attr.TriggerName);
        }

        [Fact]
        public void RelmTrigger_OtherTriggerName_Allows_Empty_String()
        {
            var attr = new RelmTrigger(
                Triggers.TriggerTime.BEFORE,
                Triggers.TriggerEvent.INSERT,
                "body",
                triggerName: null,
                triggerOrder: TriggerOrdering.FOLLOWS,
                otherTriggerName: string.Empty);

            Assert.Equal(string.Empty, attr.OtherTriggerName);
        }

        [Fact]
        public void RelmTrigger_TriggerOrder_Property_Allows_Null()
        {
            var attr = new RelmTrigger(
                Triggers.TriggerTime.AFTER,
                Triggers.TriggerEvent.DELETE,
                "body");

            attr.TriggerOrder = null;

            Assert.Null(attr.TriggerOrder);
        }

        [Fact]
        public void RelmTrigger_TriggerBody_Set_To_Null_Allows_Null()
        {
            var attr = new RelmTrigger(
                Triggers.TriggerTime.AFTER,
                Triggers.TriggerEvent.DELETE,
                "body");

            attr.TriggerBody = null;

            Assert.Null(attr.TriggerBody);
        }

        [Fact]
        public void RelmTrigger_Can_Be_Retrieved_From_Class_And_Struct()
        {
            var classAttr = (RelmTrigger?)Attribute.GetCustomAttribute(typeof(DecoratedSampleClassWithTrigger), typeof(RelmTrigger));
            var structAttr = (RelmTrigger?)Attribute.GetCustomAttribute(typeof(SampleStructWithTrigger), typeof(RelmTrigger));

            Assert.NotNull(classAttr);
            Assert.Equal(Triggers.TriggerEvent.INSERT, classAttr!.TriggerEvent);
            Assert.NotNull(structAttr);
            Assert.Equal(Triggers.TriggerEvent.UPDATE, structAttr!.TriggerEvent);
        }

        [Fact]
        public void RelmTrigger_Is_Inherited_By_Derived_Class()
        {
            var attrs = typeof(DerivedFromBaseWithTrigger).GetCustomAttributes(typeof(RelmTrigger), inherit: true);
            Assert.Single(attrs);
            var attr = (RelmTrigger)attrs[0];
            Assert.Equal(Triggers.TriggerEvent.DELETE, attr.TriggerEvent);
        }
















        [Fact]
        public void RelmTrigger_Constructor_Allows_Empty_String_Body_And_Trims_Outer_Whitespace()
        {
            var attr1 = new RelmTrigger(TriggerTime.BEFORE, TriggerEvent.INSERT, string.Empty);
            Assert.Equal(string.Empty, attr1.TriggerBody);

            var attr2 = new RelmTrigger(TriggerTime.AFTER, TriggerEvent.UPDATE, "  body text  ");
            Assert.Equal("body text", attr2.TriggerBody);
        }

        [Fact]
        public void RelmTrigger_Constructor_Preserves_Inner_Whitespace_When_Trimming()
        {
            var attr = new RelmTrigger(TriggerTime.BEFORE, TriggerEvent.INSERT, "  a  b  c  ");
            Assert.Equal("a  b  c", attr.TriggerBody); // only outer whitespace trimmed
        }

        [Fact]
        public void RelmTrigger_Constructor_Respects_Custom_Order_And_OtherTriggerName()
        {
            var attr = new RelmTrigger(
                TriggerTime.AFTER,
                TriggerEvent.DELETE,
                "do stuff",
                triggerName: "cleanup_trigger",
                triggerOrder: TriggerOrdering.PRECEDES,
                otherTriggerName: "audit_trigger");

            Assert.Equal(TriggerOrdering.PRECEDES, attr.TriggerOrder);
            Assert.Equal("audit_trigger", attr.OtherTriggerName);
            Assert.Equal("cleanup_trigger", attr.TriggerName);
        }

        [Fact]
        public void RelmTrigger_Setters_Can_Clear_Names_And_Order()
        {
            var attr = new RelmTrigger(TriggerTime.AFTER, TriggerEvent.UPDATE, "body", "name", TriggerOrdering.FOLLOWS, "other");

            attr.TriggerName = null;
            attr.OtherTriggerName = null;
            attr.TriggerOrder = null;

            Assert.Null(attr.TriggerName);
            Assert.Null(attr.OtherTriggerName);
            Assert.Null(attr.TriggerOrder);
        }

        [Fact]
        public void RelmTrigger_Inherit_False_Does_Not_Resolve_Base_Attribute()
        {
            var attrs = typeof(DerivedNoTrigger).GetCustomAttributes(typeof(RelmTrigger), inherit: false);
            Assert.Empty(attrs);
        }

        [Fact]
        public void RelmTrigger_Inherit_True_Resolves_Base_Attribute()
        {
            var attrs = typeof(DerivedNoTrigger).GetCustomAttributes(typeof(RelmTrigger), inherit: true);
            Assert.Single(attrs);
            var attr = (RelmTrigger)attrs[0];
            Assert.Equal(TriggerEvent.DELETE, attr.TriggerEvent);
            Assert.Equal(TriggerTime.BEFORE, attr.TriggerTime);
        }
    }
}
