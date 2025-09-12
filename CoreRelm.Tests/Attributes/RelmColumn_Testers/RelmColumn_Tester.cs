using CoreRelm.Attributes;
using CoreRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Attributes.RelmColumn_Testers
{
    public class RelmColumn_Tester
    {
        [Fact]
        public void RelmColumn_ComplexTestModel_HasAtLeastOne_Attribute()
        {
            var complexTestModel = new ComplexTestModel();
            var complexTestModelType = complexTestModel.GetType();
            var complexTestModelProperties = complexTestModelType.GetProperties();

            var relmColumnAttributes = complexTestModelProperties
                .SelectMany(x => x.GetCustomAttributes(typeof(RelmColumn), true))
                .Cast<RelmColumn>();

            Assert.True(relmColumnAttributes.Any());
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_TestColumnInternalId_Has_Attribute()
        {
            var complexTestModel = new ComplexTestModel();
            var complexTestModelType = complexTestModel.GetType();
            var testColumnInternalIdProperty = complexTestModelType.GetProperty(nameof(ComplexTestModel.TestColumnInternalId));

            Assert.NotNull(testColumnInternalIdProperty);

            var relmColumnAttribute = testColumnInternalIdProperty
                .GetCustomAttributes(typeof(RelmColumn), true)
                .Cast<RelmColumn>()
                .FirstOrDefault();

            Assert.NotNull(relmColumnAttribute);
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_Has_Correct_Attribute_Count()
        {
            var complexTestModel = new ComplexTestModel();
            var complexTestModelType = complexTestModel.GetType();
            var complexTestModelProperties = complexTestModelType.GetProperties();

            var relmColumnAttributes = complexTestModelProperties
                .SelectMany(x => x.GetCustomAttributes(typeof(RelmColumn), true))
                .Cast<RelmColumn>();

            Assert.Equal(9, relmColumnAttributes.Count()); // Assuming there are 3 properties with RelmColumn attribute
        }

        [Fact]
        public void RelmColumn_ComplexTestModel_TestColumnInternalId_Has_Correct_Attribute_Values()
        {
            var complexTestModel = new ComplexTestModel();
            var complexTestModelType = complexTestModel.GetType();
            var testColumnInternalIdProperty = complexTestModelType.GetProperty(nameof(ComplexTestModel.TestColumnInternalId));

            Assert.NotNull(testColumnInternalIdProperty);

            var relmColumnAttribute = testColumnInternalIdProperty
                .GetCustomAttributes(typeof(RelmColumn), true)
                .Cast<RelmColumn>()
                .FirstOrDefault();

            Assert.NotNull(relmColumnAttribute);
            Assert.Equal("test_column_InternalId", relmColumnAttribute?.ColumnName);
            Assert.Equal(255, relmColumnAttribute?.ColumnSize);
            Assert.False(relmColumnAttribute?.IsNullable);
            Assert.False(relmColumnAttribute?.PrimaryKey);
            Assert.True(relmColumnAttribute?.Autonumber);
            Assert.True(relmColumnAttribute?.Unique);
            Assert.Equal("DEFAULTVALUE", relmColumnAttribute?.DefaultValue);
            Assert.Equal("INDEX", relmColumnAttribute?.Index);
            Assert.True(relmColumnAttribute?.IndexDescending);
            Assert.True(relmColumnAttribute?.AllowDataTruncation);
            Assert.True(relmColumnAttribute?.Virtual);
        }
    }
}
