using CoreRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Options.RelmContextOptions;

namespace CoreRelm.Tests.Options.RelmContextOptionBuilder_Tests
{
    public class SetDatabaseNameTests
    {
        [Fact]
        public void SetDatabaseName_SetsDatabaseNamePropertyCorrectly()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedDatabaseName = "TestDatabaseName";

            // Act
            builder.SetDatabaseName(expectedDatabaseName);
            var options = builder.BuildOptions(validateSettings: false);

            // Assert
            Assert.Equal(expectedDatabaseName, options.DatabaseName);
        }

        [Fact]
        public void SetDatabaseName_SetsOptionsBuilderTypeToConnectionString()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedType = OptionsBuilderTypes.ConnectionDetails;

            // Act
            builder.SetDatabaseName("TestDatabaseName");
            var options = builder.BuildOptions(validateSettings: false);

            // Assert
            Assert.Equal(expectedType, options.OptionsBuilderType);
        }

        [Fact]
        public void ValidateAllSettings_ThrowsException_ForMissingDatabaseName()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            builder.SetDatabaseServer("DBServer");
            builder.SetDatabaseUser("DBUser");
            builder.SetDatabasePassword("DBPassword");

            // Assert
            Assert.Throws<ArgumentNullException>(() => builder.ValidateAllSettings());
        }

        [Fact]
        public void ValidateAllSettings_ReturnsFalse_ForMissingDatabaseName()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            builder.SetDatabaseServer("DBServer");
            builder.SetDatabaseUser("DBUser");
            builder.SetDatabasePassword("DBPassword");

            // Act
            var result = builder.ValidateAllSettings(false);

            // Assert
            Assert.False(result);
        }
    }
}
