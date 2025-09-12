using CoreRelm.Attributes;
using CoreRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Attributes.RelmColumn_Testers
{
    public class RelmColumn_NotSpecified_Tester
    {
        readonly RelmColumn? relmColumnAttribute;

        public RelmColumn_NotSpecified_Tester()
        {
            var complexTestModel = new ComplexTestModel();
            var complexTestModelType = complexTestModel.GetType();
            var complexTestModelProperties = complexTestModelType.GetProperties();

            relmColumnAttribute = complexTestModelProperties
                .Where(x => x.Name == nameof(ComplexTestModel.TestColumnNoAttributeArguments))
                .SelectMany(x => x.GetCustomAttributes(typeof(RelmColumn), true))
                .Cast<RelmColumn>()
                .FirstOrDefault();
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_AllowDataTruncation_False_When_Not_Specified()
        {
            Assert.False(relmColumnAttribute?.AllowDataTruncation);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_ColumnName_Null_When_Not_Specified()
        {
            Assert.Null(relmColumnAttribute?.ColumnName);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_ColumnSize_Negative1_When_Not_Specified()
        {
            Assert.Equal(-1, relmColumnAttribute?.ColumnSize);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_DefaultValue_Null_When_Not_Specified()
        {
            Assert.Null(relmColumnAttribute?.DefaultValue);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_Index_Null_When_Not_Specified()
        {
            Assert.Null(relmColumnAttribute?.Index);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_IndexDescending_False_When_Not_Specified()
        {
            Assert.False(relmColumnAttribute?.IndexDescending);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_IsNullable_True_When_Not_Specified()
        {
            Assert.True(relmColumnAttribute?.IsNullable);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_Autonumber_False_When_Not_Specified()
        {
            Assert.False(relmColumnAttribute?.Autonumber);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_PrimaryKey_False_When_Not_Specified()
        {
            Assert.False(relmColumnAttribute?.PrimaryKey);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_Unique_False_When_Not_Specified()
        {
            Assert.False(relmColumnAttribute?.Unique);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_Virtual_False_When_Not_Specified()
        {
            Assert.False(relmColumnAttribute?.Virtual);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Attribute_CompoundColumnSize_Null_When_Not_Specified()
        {
            Assert.Null(relmColumnAttribute?.CompoundColumnSize);
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
