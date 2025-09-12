using CoreRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Options.RelmContextOptionBuilder_Tests
{
    public class ValidateAllSettings_Tester
    {
        [Fact]
        public void ValidateAllSettings_ReturnsTrue_ForValidConnectionString_WithFalseParameter()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            builder.SetDatabaseServer("DBServer");
            builder.SetDatabaseName("DBName");
            builder.SetDatabaseUser("DBUser");
            builder.SetDatabasePassword("DBPassword");

            // Act
            var result = builder.ValidateAllSettings(false);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateAllSettings_ReturnsTrue_ForValidConnectionString()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            builder.SetDatabaseServer("DBServer");
            builder.SetDatabaseName("DBName");
            builder.SetDatabaseUser("DBUser");
            builder.SetDatabasePassword("DBPassword");

            // Act
            var result = builder.ValidateAllSettings();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateAllSettings_ThrowsException_ForInvalidOptionsBuilder()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.ValidateAllSettings());
        }

        [Fact]
        public void ValidateAllSettings_ReturnsFalse_ForInvalidOptionsBuilder_WithFalseParameter()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();

            // Act
            var result = builder.ValidateAllSettings(false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateAllSettings_ReturnsTrue_ForValidNamedConnectionStringSettings()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder("name=SimpleRelmMySql");

            // Act
            var result = builder.ValidateAllSettings();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateAllSettings_ReturnsTrue_ForValidConnectionStringSettings()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            builder.SetDatabaseServer("YourServer");
            builder.SetDatabaseName("YourDatabase");
            builder.SetDatabaseUser("YourUser");
            builder.SetDatabasePassword("YourPassword");

            // Act
            var result = builder.ValidateAllSettings();

            // Assert
            Assert.True(result);
        }
    }
}
