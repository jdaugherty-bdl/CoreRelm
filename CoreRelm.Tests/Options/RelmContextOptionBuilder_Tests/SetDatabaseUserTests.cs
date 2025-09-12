using SimpleRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            // Assert
            Assert.Equal(expectedDatabaseUser, builder.DatabaseUser);
        }

        [Fact]
        public void SetDatabaseUser_SetsOptionsBuilderTypeToConnectionString()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedType = RelmContextOptionsBuilder.OptionsBuilderTypes.ConnectionString;

            // Act
            builder.SetDatabaseUser("TestUser");

            // Assert
            Assert.Equal(expectedType, builder.OptionsBuilderType);
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
