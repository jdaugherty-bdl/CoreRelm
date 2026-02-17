using CoreRelm.Attributes;
using CoreRelm.Attributes.BaseClasses;
using Org.BouncyCastle.Asn1.Cms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.ForeignKeys;
using static CoreRelm.Enums.Indexes;

namespace CoreRelm.Tests.Attributes.RelmIndex_Tests
{
    public class RelmIndexBase_Tester
    {
        [Fact]
        public void RelmIndexBase_Has_Correct_Base_Class()
        {
            var relmIndexBaseType = typeof(RelmIndexBase).BaseType;
            Assert.Equal(typeof(System.Attribute), relmIndexBaseType);
        }

        [Fact]
        public void RelmIndexBase_Can_Be_Instantiated()
        {
            var instance = Activator.CreateInstance(typeof(RelmIndexBase), null, null, IndexType.None, -1, null, null, Visibility.None, null, null, Algorithm.None, LockOption.None);
            Assert.NotNull(instance);
            Assert.IsType<RelmIndexBase>(instance);
        }

        [Fact]
        public void RelmIndexBase_Has_No_Properties()
        {
            var properties = typeof(RelmIndexBase).GetProperties();

            Assert.Contains(nameof(RelmIndexBase.IndexKeyHolder), properties.Select(p => p.Name));
            Assert.Contains(nameof(RelmIndexBase.Descending), properties.Select(p => p.Name));
            Assert.Contains(nameof(RelmIndexBase.IndexedProperties), properties.Select(p => p.Name));
            Assert.Contains(nameof(RelmIndexBase.IndexedPropertyNames), properties.Select(p => p.Name));
            Assert.Contains(nameof(RelmIndexBase.IndexName), properties.Select(p => p.Name));
            Assert.Contains(nameof(RelmIndexBase.IndexTypeValue), properties.Select(p => p.Name));
            Assert.Contains(nameof(RelmIndexBase.ParserName), properties.Select(p => p.Name));
            Assert.Contains(nameof(RelmIndexBase.Comment), properties.Select(p => p.Name));
            Assert.Contains(nameof(RelmIndexBase.IndexVisibility), properties.Select(p => p.Name));
            Assert.Contains(nameof(RelmIndexBase.AlgorithmType), properties.Select(p => p.Name));
            Assert.Contains(nameof(RelmIndexBase.IndexLockOption), properties.Select(p => p.Name));
        }

        [Fact]
        public void RelmIndexBase_Has_No_Methods()
        {
            var methods = typeof(RelmIndexBase).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);

            Assert.Contains(methods, m => m.Name == $"get_{nameof(RelmIndexBase.IndexKeyHolder)}");
            Assert.Contains(methods, m => m.Name == $"get_{nameof(RelmIndexBase.Descending)}");
            Assert.Contains(methods, m => m.Name == $"get_{nameof(RelmIndexBase.IndexedProperties)}");
            Assert.Contains(methods, m => m.Name == $"get_{nameof(RelmIndexBase.IndexedPropertyNames)}");
            Assert.Contains(methods, m => m.Name == $"get_{nameof(RelmIndexBase.IndexName)}");
            Assert.Contains(methods, m => m.Name == $"get_{nameof(RelmIndexBase.IndexTypeValue)}");
            Assert.Contains(methods, m => m.Name == $"get_{nameof(RelmIndexBase.ParserName)}");
            Assert.Contains(methods, m => m.Name == $"get_{nameof(RelmIndexBase.Comment)}");
            Assert.Contains(methods, m => m.Name == $"get_{nameof(RelmIndexBase.IndexVisibility)}");
            Assert.Contains(methods, m => m.Name == $"get_{nameof(RelmIndexBase.AlgorithmType)}");
            Assert.Contains(methods, m => m.Name == $"get_{nameof(RelmIndexBase.IndexLockOption)}");
        }

        [Fact]
        public void RelmIndexBase_Has_No_Attributes()
        {
            var attributes = typeof(RelmIndexBase).GetCustomAttributes(false);
            Assert.DoesNotContain(attributes, attr => attr.GetType() == typeof(AttributeUsageAttribute));
        }

        [Fact]
        public void RelmIndexBase_Is_Not_Abstract()
        {
            var isAbstract = typeof(RelmIndexBase).IsAbstract;
            Assert.False(isAbstract);
        }
    }
}
