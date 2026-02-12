using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Attributes.RelmDto_Tests
{
    public class RelmDtoAttribute_Test
    {
        [Fact]
        public void RelmDtoAttribute_Can_Be_Instantiated()
        {
            var instance = Activator.CreateInstance(typeof(RelmDto));
            Assert.NotNull(instance);
            Assert.IsType<RelmDto>(instance);
        }

        [Fact]
        public void RelmDtoAttribute_Has_Correct_AttributeUsage()
        {
            var attributeUsage = (AttributeUsageAttribute)typeof(RelmDto).GetCustomAttributes(typeof(AttributeUsageAttribute), false).FirstOrDefault();
            Assert.NotNull(attributeUsage);
            Assert.Equal(AttributeTargets.Class, attributeUsage.ValidOn);
            Assert.False(attributeUsage.AllowMultiple);
            Assert.True(attributeUsage.Inherited);
        }

        [Fact]
        public void RelmDtoAttribute_Has_No_Properties()
        {
            var properties = typeof(RelmDto).GetProperties();
            Assert.Empty(properties);
        }

        [Fact]
        public void RelmDtoAttribute_Has_No_Methods()
        {
            var methods = typeof(RelmDto).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
            Assert.Empty(methods);
        }

        [Fact]
        public void RelmDtoAttribute_Has_No_Fields()
        {
            var fields = typeof(RelmDto).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
            Assert.Empty(fields);
        }

        [Fact]
        public void RelmDtoAttribute_Has_No_Additional_Attributes()
        {
            var attributes = typeof(RelmDto).GetCustomAttributes(false);
            Assert.Empty(attributes);
        }

        [Fact]
        public void RelmDtoAttribute_Is_Not_Abstract()
        {
            var isAbstract = typeof(RelmDto).IsAbstract;
            Assert.False(isAbstract);
        }

        [Fact]
        public void RelmDtoAttribute_Has_Correct_Name()
        {
            Assert.Equal("RelmDto", typeof(RelmDto).Name);
        }

        [Fact]
        public void RelmDtoAttribute_Has_Correct_Namespace()
        {
            Assert.Equal("CoreRelm.Attributes", typeof(RelmDto).Namespace);
        }

        [Fact]
        public void RelmDtoAttribute_Has_Correct_Assembly()
        {
            var assemblyName = typeof(RelmDto).Assembly.GetName().Name;
            Assert.Equal("CoreRelm", assemblyName);
        }

        [Fact]
        public void RelmDtoAttribute_Has_Correct_FullName()
        {
            Assert.Equal("CoreRelm.Attributes.RelmDto", typeof(RelmDto).FullName);
        }

        [Fact]
        public void RelmDtoAttribute_Has_Correct_BaseType()
        {
            var baseType = typeof(RelmDto).BaseType;
            Assert.Equal(typeof(Attribute), baseType);
        }

        [Fact]
        public void RelmDtoAttribute_Has_Correct_AssemblyQualifiedName()
        {
            var assemblyQualifiedName = typeof(RelmDto).AssemblyQualifiedName;
            Assert.Equal("CoreRelm.Attributes.RelmDto, CoreRelm, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", assemblyQualifiedName);
        }
    }
}
