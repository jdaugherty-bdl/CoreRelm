using SimpleRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            // Assert
            Assert.Equal(expectedDatabaseName, builder.DatabaseName);
        }

        [Fact]
        public void SetDatabaseName_SetsOptionsBuilderTypeToConnectionString()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedType = RelmContextOptionsBuilder.OptionsBuilderTypes.ConnectionString;

            // Act
            builder.SetDatabaseName("TestDatabaseName");

            // Assert
            Assert.Equal(expectedType, builder.OptionsBuilderType);
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
