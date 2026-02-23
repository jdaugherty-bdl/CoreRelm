using CoreRelm.Models;
using CoreRelm.Options;
using CoreRelm.Tests.TestModels;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Models.RelmContext_Tests
{
    [Collection("JsonConfiguration")]
    public class HasDataSetTests : IClassFixture<JsonConfigurationFixture>
    {
        private readonly IConfiguration _configuration;

        public HasDataSetTests(JsonConfigurationFixture fixture)
        {
            _configuration = fixture.Configuration;

            RelmHelper.UseConfiguration(fixture.Configuration);
        }

        [Fact]
        public void HasDataSet_ReturnsTrue_WhenDataSetTypeIsAttached()
        {
            // Arrange
            var context = new RelmContextOptionsBuilder("x", "x", "x", "x")
                .SetAutoOpenConnection(false)
                .SetAutoVerifyTables(false)
                .Build<ComplexTestContext>()
                ?? throw new InvalidOperationException("Failed to build ComplexTestContext");

            // Act
            context.GetDataSet<ComplexTestModel>();
            bool result = context.HasDataSet<ComplexTestModel>(); // Ensure the dataset is attached

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasDataSet_ReturnsFalse_WhenDataSetTypeIsNotAttached()
        {
            // Arrange
            var context = new RelmContextOptionsBuilder("x", "x", "x", "x")
                .SetAutoOpenConnection(false)
                .SetAutoInitializeDataSets(false)
                .SetAutoVerifyTables(false)
                .Build<ComplexTestContext>()
                ?? throw new InvalidOperationException("Failed to build ComplexTestContext");

            // Act
            bool result = context.HasDataSet<ComplexTestModel>(throwException: false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasDataSet_TypeOverload_ReturnsTrue_WhenDataSetTypeIsAttached()
        {
            // Arrange
            var context = new RelmContextOptionsBuilder("x", "x", "x", "x")
                .SetAutoOpenConnection(false)
                .SetAutoVerifyTables(false)
                .Build<ComplexTestContext>()
                ?? throw new InvalidOperationException("Failed to build ComplexTestContext");
            // Assuming you have a way to attach a dataset of type "YourDataSetType"

            // Act
            context.GetDataSet<ComplexTestModel>(); // Ensure the dataset is attached
            bool result = context.HasDataSet(typeof(ComplexTestModel));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasDataSet_TypeOverload_ReturnsFalse_WhenDataSetTypeIsNotAttached()
        {
            // Arrange
            var context = new RelmContextOptionsBuilder("x", "x", "x", "x")
                .SetAutoOpenConnection(false)
                .SetAutoInitializeDataSets(false)
                .SetAutoVerifyTables(false)
                .Build<ComplexTestContext>()
                ?? throw new InvalidOperationException("Failed to build ComplexTestContext");

            // Act
            bool result = context.HasDataSet(typeof(ComplexTestModel), throwException: false);

            // Assert
            Assert.False(result);
        }
    }
}
