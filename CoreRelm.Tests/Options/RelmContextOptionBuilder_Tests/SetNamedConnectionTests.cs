using CoreRelm.Extensions;
using CoreRelm.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Options.RelmContextOptions;

namespace CoreRelm.Tests.Options.RelmContextOptionBuilder_Tests
{
    [Collection("JsonConfiguration")]
    public class SetNamedConnectionTests : IClassFixture<JsonConfigurationFixture>
    {
        private readonly IConfiguration _configuration;

        public SetNamedConnectionTests(JsonConfigurationFixture fixture)
        {
            _configuration = fixture.Configuration;
        }

        [Fact]
        public void SetNamedConnection_SetsNamedConnectionPropertyCorrectly()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedNamedConnection = "SimpleRelmMySql";

            // Act
            builder.SetNamedConnection(expectedNamedConnection);
            var options = builder.BuildOptions();

            // Assert
            Assert.Equal(expectedNamedConnection, options.NamedConnection);
        }

        [Fact]
        public void SetNamedConnection_SetsConnectionStringTypeCorrectly()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedConnectionStringType = OptionsBuilderTypes.NamedConnectionString; // Make sure this is valid

            // Act
            builder.SetNamedConnection("SimpleRelmMySql");
            var options = builder.BuildOptions();

            // Assert
            Assert.Equal(expectedConnectionStringType, options.OptionsBuilderType);
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
            var expectedType = OptionsBuilderTypes.NamedConnectionString;
            var validConnectionStringType = "SimpleRelmMySql"; // Make sure this is valid

            // Act
            builder.SetNamedConnection(validConnectionStringType);
            var options = builder.BuildOptions();

            // Assert
            Assert.Equal(expectedType, options.OptionsBuilderType);
        }

        [Fact]
        public void ValidateAllSettings_ReturnsTrue_ForValidNamedConnectionString()
        {
            // Arrange
            new ServiceCollection().AddCoreRelm(_configuration);
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
            new ServiceCollection().AddCoreRelm(_configuration);
            var builder = new RelmContextOptionsBuilder("name=SimpleRelmMySql");

            // Act
            var result = builder.ValidateAllSettings(false);

            // Assert
            Assert.True(result);
        }
    }
}
