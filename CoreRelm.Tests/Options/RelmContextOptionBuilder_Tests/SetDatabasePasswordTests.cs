using SimpleRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Options.RelmContextOptionBuilder_Tests
{
    public class SetDatabasePasswordTests
    {
        [Fact]
        public void SetDatabasePassword_SetsDatabasePasswordPropertyCorrectly()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedDatabasePassword = "TestPassword";

            // Act
            builder.SetDatabasePassword(expectedDatabasePassword);

            // Assert
            Assert.Equal(expectedDatabasePassword, builder.DatabasePassword);
        }

        [Fact]
        public void SetDatabasePassword_SetsOptionsBuilderTypeToConnectionString()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedType = RelmContextOptionsBuilder.OptionsBuilderTypes.ConnectionString;

            // Act
            builder.SetDatabasePassword("TestPassword");

            // Assert
            Assert.Equal(expectedType, builder.OptionsBuilderType);
        }

        [Fact]
        public void ValidateAllSettings_ThrowsException_ForMissingPassword()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            builder.SetDatabaseName("DBName");
            builder.SetDatabaseServer("DBServer");
            builder.SetDatabaseUser("DBUser");

            // Assert
            Assert.Throws<ArgumentNullException>(() => builder.ValidateAllSettings());
        }

        [Fact]
        public void ValidateAllSettings_ReturnsFalse_ForMissingPassword()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            builder.SetDatabaseName("DBName");
            builder.SetDatabaseServer("DBServer");
            builder.SetDatabaseUser("DBUser");

            // Act
            var result = builder.ValidateAllSettings(false);

            // Assert
            Assert.False(result);
        }
    }
}
