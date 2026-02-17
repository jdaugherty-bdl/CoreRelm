using CoreRelm.Attributes.BaseClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Attributes.RelmIndex_Tests
{
    public class RelmIndexColumnBase_Tester
    {
        [Fact]
        public void RelmIndexColumnBase_Has_Correct_Base_Class()
        {
            var relmIndexColumnBaseType = typeof(RelmIndexColumnBase).BaseType;
            Assert.Equal(typeof(System.Attribute), relmIndexColumnBaseType);
        }

        [Fact]
        public void RelmIndexColumnBase_Can_Be_Instantiated()
        {
            var instance = Activator.CreateInstance(typeof(RelmIndexColumnBase), string.Empty, -1, null, false, 0);
            Assert.NotNull(instance);
            Assert.IsType<RelmIndexColumnBase>(instance);
        }

        [Fact]
        public void RelmIndexColumnBase_Exception_On_Null_Column_Name()
        {
            Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(typeof(RelmIndexColumnBase), null, -1, null, false, 0));
        }

        [Fact]
        public void RelmIndexColumnBase_Has_No_Properties()
        {
            var properties = typeof(RelmIndexColumnBase).GetProperties();

            Assert.Contains(nameof(RelmIndexColumnBase.IndexKeyHolder), properties.Select(p => p.Name));
            Assert.Contains(nameof(RelmIndexColumnBase.ColumnName), properties.Select(p => p.Name));
            Assert.Contains(nameof(RelmIndexColumnBase.Length), properties.Select(p => p.Name));
            Assert.Contains(nameof(RelmIndexColumnBase.Expression), properties.Select(p => p.Name));
            Assert.Contains(nameof(RelmIndexColumnBase.IsDescending), properties.Select(p => p.Name));
            Assert.Contains(nameof(RelmIndexColumnBase.Order), properties.Select(p => p.Name));
        }

        [Fact]
        public void RelmIndexColumnBase_Has_No_Methods()
        {
            var methods = typeof(RelmIndexColumnBase).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);

            Assert.Contains(methods, m => m.Name == $"get_{nameof(RelmIndexColumnBase.IndexKeyHolder)}");
            Assert.Contains(methods, m => m.Name == $"get_{nameof(RelmIndexColumnBase.ColumnName)}");
            Assert.Contains(methods, m => m.Name == $"get_{nameof(RelmIndexColumnBase.Length)}");
            Assert.Contains(methods, m => m.Name == $"get_{nameof(RelmIndexColumnBase.Expression)}");
            Assert.Contains(methods, m => m.Name == $"get_{nameof(RelmIndexColumnBase.IsDescending)}");
            Assert.Contains(methods, m => m.Name == $"get_{nameof(RelmIndexColumnBase.Order)}");
        }

        [Fact]
        public void RelmIndexColumnBase_Has_No_Attributes()
        {
            var attributes = typeof(RelmIndexColumnBase).GetCustomAttributes(false);

            Assert.Contains(attributes, attr => attr.GetType() == typeof(AttributeUsageAttribute));
        }

        [Fact]
        public void RelmIndexColumnBase_Is_Not_Abstract()
        {
            var isAbstract = typeof(RelmIndexColumnBase).IsAbstract;
            Assert.False(isAbstract);
        }

        [Fact]
        public void RelmIndexColumnBase_Has_No_Fields()
        {
            var fields = typeof(RelmIndexColumnBase).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);

            Assert.Contains(fields, f => f.Name == $"<{nameof(RelmIndexColumnBase.IndexKeyHolder)}>k__BackingField");
            Assert.Contains(fields, f => f.Name == $"<{nameof(RelmIndexColumnBase.ColumnName)}>k__BackingField");
            Assert.Contains(fields, f => f.Name == $"<{nameof(RelmIndexColumnBase.Length)}>k__BackingField");
            Assert.Contains(fields, f => f.Name == $"<{nameof(RelmIndexColumnBase.Expression)}>k__BackingField");
            Assert.Contains(fields, f => f.Name == $"<{nameof(RelmIndexColumnBase.IsDescending)}>k__BackingField");
            Assert.Contains(fields, f => f.Name == $"<{nameof(RelmIndexColumnBase.Order)}>k__BackingField");
        }
    }
}
