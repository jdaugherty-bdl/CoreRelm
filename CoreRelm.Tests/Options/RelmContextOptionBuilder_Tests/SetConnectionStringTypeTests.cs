using CoreRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Options.RelmContextOptions;

namespace CoreRelm.Tests.Options.RelmContextOptionBuilder_Tests
{
    public class SetConnectionStringTypeTests
    {
        enum ConnectionStringTypes
        {
            TestDatabase
        }
        [Fact]
        public void SetConnectionStringType_SetsConnectionStringTypePropertyCorrectly()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedConnectionStringType = ConnectionStringTypes.TestDatabase; // Replace with a valid enum value

            // Act
            builder.SetConnectionStringType(expectedConnectionStringType);
            var options = builder.BuildOptions();

            // Assert
            Assert.Equal(expectedConnectionStringType, options.ConnectionStringType);
        }

        [Fact]
        public void SetConnectionStringType_SetsDatabaseConnectionStringToStringOfEnum()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedConnectionStringType = ConnectionStringTypes.TestDatabase; // Replace with a valid enum value

            // Act
            builder.SetConnectionStringType(expectedConnectionStringType);
            var options = builder.BuildOptions();

            // Assert
            Assert.Equal(expectedConnectionStringType, options.ConnectionStringType);
        }

        [Fact]
        public void SetConnectionStringType_SetsOptionsBuilderTypeToNamedConnectionString()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedType = OptionsBuilderTypes.NamedConnectionString;
            var validConnectionStringType = ConnectionStringTypes.TestDatabase; // Replace with a valid enum value

            // Act
            builder.SetConnectionStringType(validConnectionStringType);
            var options = builder.BuildOptions();

            // Assert
            Assert.Equal(expectedType, options.OptionsBuilderType);
        }
    }
}
