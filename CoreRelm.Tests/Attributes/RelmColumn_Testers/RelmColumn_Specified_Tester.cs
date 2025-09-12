using CoreRelm.Attributes;
using CoreRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Attributes.RelmColumn_Testers
{
    public class RelmColumn_Specified_Tester
    {
        readonly RelmColumn? relmColumnAttribute;

        public RelmColumn_Specified_Tester()
        {
            var complexTestModel = new ComplexTestModel();
            var complexTestModelType = complexTestModel.GetType();
            var complexTestModelProperties = complexTestModelType.GetProperties();

            relmColumnAttribute = complexTestModelProperties
                .Where(x => x.Name == nameof(ComplexTestModel.TestColumnInternalId))
                .SelectMany(x => x.GetCustomAttributes(typeof(RelmColumn), true))
                .Cast<RelmColumn>()
                .FirstOrDefault();
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_ColumnName()
        {
            Assert.Equal("test_column_InternalId", relmColumnAttribute?.ColumnName);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_ColumnSize_255()
        {
            Assert.Equal(255, relmColumnAttribute?.ColumnSize);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_IsNullable_False()
        {
            Assert.False(relmColumnAttribute?.IsNullable);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_PrimaryKey_False()
        {
            Assert.False(relmColumnAttribute?.PrimaryKey);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_Autonumber_True()
        {
            Assert.True(relmColumnAttribute?.Autonumber);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_Unique_True()
        {
            Assert.True(relmColumnAttribute?.Unique);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_DefaultValue_DEFAULTVALUE()
        {
            Assert.Equal("DEFAULTVALUE", relmColumnAttribute?.DefaultValue);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_Index_INDEX()
        {
            Assert.Equal("INDEX", relmColumnAttribute?.Index);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_IndexDescending_True()
        {
            Assert.True(relmColumnAttribute?.IndexDescending);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_AllowDataTruncation_True()
        {
            Assert.True(relmColumnAttribute?.AllowDataTruncation);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_Virtual_True()
        {
            Assert.True(relmColumnAttribute?.Virtual);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_CompoundColumnSize_Specified()
        {
            Assert.Equal([100, 200], relmColumnAttribute?.CompoundColumnSize);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_Present()
        {
            Assert.NotNull(relmColumnAttribute);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_Absent_On_Other_Property()
        {
            var complexTestModel = new ComplexTestModel();
            var complexTestModelType = complexTestModel.GetType();
            var otherProperty = complexTestModelType.GetProperty(nameof(ComplexTestModel.TestFieldBoolean));

            Assert.NotNull(otherProperty);

            var otherPropertyAttribute = otherProperty
                .GetCustomAttributes(typeof(RelmColumn), true)
                .Cast<RelmColumn>()
                .FirstOrDefault();

            Assert.Null(otherPropertyAttribute);
        }
    }
}
