using CoreRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Options.RelmContextOptionBuilder_Tests
{
    public class SetNamedConnectionTests
    {
        [Fact]
        public void SetNamedConnection_SetsNamedConnectionPropertyCorrectly()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedNamedConnection = "SimpleRelmMySql";

            // Act
            builder.SetNamedConnection(expectedNamedConnection);

            // Assert
            Assert.Equal(expectedNamedConnection, builder.NamedConnection);
        }

        [Fact]
        public void SetNamedConnection_SetsConnectionStringTypeCorrectly()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedConnectionStringType = RelmContextOptionsBuilder.OptionsBuilderTypes.NamedConnectionString; // Make sure this is valid

            // Act
            builder.SetNamedConnection("SimpleRelmMySql");

            // Assert
            Assert.Equal(expectedConnectionStringType, builder.OptionsBuilderType);
        }

        /*
        [Fact]
        public void SetNamedConnection_ThrowsExceptionForInvalidConnectionStringType()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var invalidConnectionStringType = "InvalidType";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.SetNamedConnection(invalidConnectionStringType));
        }
        */

        [Fact]
        public void SetNamedConnection_SetsOptionsBuilderTypeToNamedConnectionString()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedType = RelmContextOptionsBuilder.OptionsBuilderTypes.NamedConnectionString;
            var validConnectionStringType = "SimpleRelmMySql"; // Make sure this is valid

            // Act
            builder.SetNamedConnection(validConnectionStringType);

            // Assert
            Assert.Equal(expectedType, builder.OptionsBuilderType);
        }

        [Fact]
        public void ValidateAllSettings_ReturnsTrue_ForValidNamedConnectionString()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder("name=SimpleRelmMySql");

            // Act
            var result = builder.ValidateAllSettings();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateAllSettings_ReturnsTrue_ForValidNamedConnectionString_WithFalseParameter()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder("name=SimpleRelmMySql");

            // Act
            var result = builder.ValidateAllSettings(false);

            // Assert
            Assert.True(result);
        }
    }
}
