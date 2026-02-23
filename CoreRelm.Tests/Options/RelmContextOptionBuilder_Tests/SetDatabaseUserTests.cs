using CoreRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Options.RelmContextOptions;

namespace CoreRelm.Tests.Options.RelmContextOptionBuilder_Tests
{
    public class SetDatabaseUserTests
    {
        [Fact]
        public void SetDatabaseUser_SetsDatabaseUserPropertyCorrectly()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedDatabaseUser = "TestUser";

            // Act
            builder.SetDatabaseUser(expectedDatabaseUser);
            var options = builder.BuildOptions(validateSettings: false);

            // Assert
            Assert.Equal(expectedDatabaseUser, options.DatabaseUser);
        }

        [Fact]
        public void SetDatabaseUser_SetsOptionsBuilderTypeToConnectionString()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedType = OptionsBuilderTypes.ConnectionDetails;

            // Act
            builder.SetDatabaseUser("TestUser");
            var options = builder.BuildOptions(validateSettings: false);

            // Assert
            Assert.Equal(expectedType, options.OptionsBuilderType);
        }

        [Fact]
        public void ValidateAllSettings_ThrowsException_ForMissingUsername()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            builder.SetDatabaseName("DBName");
            builder.SetDatabaseServer("DBServer");
            builder.SetDatabasePassword("DBPassword");

            // Assert
            Assert.Throws<ArgumentNullException>(() => builder.ValidateAllSettings());
        }

        [Fact]
        public void ValidateAllSettings_ReturnsFalse_ForMissingUsername()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            builder.SetDatabaseName("DBName");
            builder.SetDatabaseServer("DBServer");
            builder.SetDatabasePassword("DBPassword");

            // Act
            var result = builder.ValidateAllSettings(false);

            // Assert
            Assert.False(result);
        }
    }
}
