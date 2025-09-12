using CoreRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            // Assert
            Assert.Equal(expectedConnectionStringType, builder.ConnectionStringType);
        }

        [Fact]
        public void SetConnectionStringType_SetsDatabaseConnectionStringToStringOfEnum()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedConnectionStringType = ConnectionStringTypes.TestDatabase; // Replace with a valid enum value

            // Act
            builder.SetConnectionStringType(expectedConnectionStringType);

            // Assert
            Assert.Equal(expectedConnectionStringType, builder.ConnectionStringType);
        }

        [Fact]
        public void SetConnectionStringType_SetsOptionsBuilderTypeToNamedConnectionString()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedType = RelmContextOptionsBuilder.OptionsBuilderTypes.NamedConnectionString;
            var validConnectionStringType = ConnectionStringTypes.TestDatabase; // Replace with a valid enum value

            // Act
            builder.SetConnectionStringType(validConnectionStringType);

            // Assert
            Assert.Equal(expectedType, builder.OptionsBuilderType);
        }
    }
}
