using CoreRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Options.RelmContextOptions;
using static CoreRelm.Options.RelmContextOptionsBuilder;

namespace CoreRelm.Tests.Options.RelmContextOptionBuilder_Tests
{
    public class SetDatabaseServerTests
    {
        [Fact]
        public void SetDatabaseServer_SetsDatabaseServerPropertyCorrectly()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedDatabaseServer = "TestServer";

            // Act
            builder.SetDatabaseServer(expectedDatabaseServer);
            var options = builder.BuildOptions(validateSettings: false);

            // Assert
            Assert.Equal(expectedDatabaseServer, options.DatabaseServer);
        }

        [Fact]
        public void SetDatabaseServer_SetsOptionsBuilderTypeToConnectionString()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedDatabaseServer = "TestServer";

            // Act
            builder.SetDatabaseServer(expectedDatabaseServer);
            var options = builder.BuildOptions(validateSettings: false);

            // Assert
            Assert.Equal(OptionsBuilderTypes.ConnectionDetails, options.OptionsBuilderType);
        }

        [Fact]
        public void ValidateAllSettings_ThrowsException_ForMissingDatabaseServer()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            builder.SetDatabaseName("DBName");
            builder.SetDatabaseUser("DBUser");
            builder.SetDatabasePassword("DBPassword");

            // Assert
            Assert.Throws<ArgumentNullException>(() => builder.ValidateAllSettings());
        }

        [Fact]
        public void ValidateAllSettings_ReturnsFalse_ForMissingDatabaseServer()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            builder.SetDatabaseName("DBName");
            builder.SetDatabaseUser("DBUser");
            builder.SetDatabasePassword("DBPassword");

            // Act
            var result = builder.ValidateAllSettings(false);

            // Assert
            Assert.False(result);
        }
    }
}
