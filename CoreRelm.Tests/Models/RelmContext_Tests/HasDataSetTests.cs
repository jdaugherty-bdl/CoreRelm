using CoreRelm.Models;
using CoreRelm.Options;
using CoreRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Models.RelmContext_Tests
{
    public class HasDataSetTests
    {
        [Fact]
        public void HasDataSet_ReturnsTrue_WhenDataSetTypeIsAttached()
        {
            // Arrange
            var optionsBuilder = new RelmContextOptionsBuilder("x", "x", "x", "x")
                .SetAutoOpenConnection(false)
                .SetAutoVerifyTables(false);
            var context = new ComplexTestContext(optionsBuilder);

            // Act
            //context.GetDataSet<ComplexTestModel>(); // Ensure the dataset is attached
            bool result = context.HasDataSet<ComplexTestModel>();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasDataSet_ReturnsFalse_WhenDataSetTypeIsNotAttached()
        {
            // Arrange
            var optionsBuilder = new RelmContextOptionsBuilder("x", "x", "x", "x")
                .SetAutoOpenConnection(false)
                .SetAutoInitializeDataSets(false)
                .SetAutoVerifyTables(false);
            var context = new RelmContext(optionsBuilder);

            // Act
            bool result = context.HasDataSet<ComplexTestModel>(throwException: false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasDataSet_TypeOverload_ReturnsTrue_WhenDataSetTypeIsAttached()
        {
            // Arrange
            var optionsBuilder = new RelmContextOptionsBuilder("x", "x", "x", "x")
                .SetAutoOpenConnection(false)
                .SetAutoVerifyTables(false);
            var context = new ComplexTestContext(optionsBuilder);
            // Assuming you have a way to attach a dataset of type "YourDataSetType"

            // Act
            bool result = context.HasDataSet(typeof(ComplexTestModel));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasDataSet_TypeOverload_ReturnsFalse_WhenDataSetTypeIsNotAttached()
        {
            // Arrange
            var optionsBuilder = new RelmContextOptionsBuilder("x", "x", "x", "x")
                .SetAutoOpenConnection(false)
                .SetAutoInitializeDataSets(false)
                .SetAutoVerifyTables(false);
            var context = new RelmContext(optionsBuilder);

            // Act
            bool result = context.HasDataSet(typeof(ComplexTestModel), throwException: false);

            // Assert
            Assert.False(result);
        }
    }
}
