using CoreRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            // Assert
            Assert.Equal(expectedDatabaseServer, builder.DatabaseServer);
        }

        [Fact]
        public void SetDatabaseServer_SetsOptionsBuilderTypeToConnectionString()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedDatabaseServer = "TestServer";

            // Act
            builder.SetDatabaseServer(expectedDatabaseServer);

            // Assert
            Assert.Equal(OptionsBuilderTypes.ConnectionString, builder.OptionsBuilderType);
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
