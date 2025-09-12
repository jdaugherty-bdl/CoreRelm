using SimpleRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Options.RelmContextOptionBuilder_Tests
{
    public class SetDatabaseConnectionStringTests
    {
        [Fact]
        public void SetDatabaseConnectionString_SetsDatabaseConnectionStringConstructorCorrectly()
        {
            // Arrange
            var expectedConnectionStringType = RelmContextOptionsBuilder.OptionsBuilderTypes.ConnectionString; // Make sure this is valid
            var expectedDatabaseConnectionString = "server=localhost;database=simple_relm;user id=simplerelmuser;password=simplerelmpassword";

            // Act
            var builder = new RelmContextOptionsBuilder(expectedDatabaseConnectionString);

            // Assert
            Assert.Equal(expectedConnectionStringType, builder.OptionsBuilderType);
            Assert.Equal(expectedDatabaseConnectionString, builder.DatabaseConnectionString);
        }

        [Fact]
        public void SetDatabaseConnectionString_SetsDatabaseConnectionStringPropertyCorrectly()
        {
            // Arrange
            var expectedConnectionStringType = RelmContextOptionsBuilder.OptionsBuilderTypes.ConnectionString; // Make sure this is valid
            var expectedDatabaseConnectionString = "server=localhost;database=simple_relm;user id=simplerelmuser;password=simplerelmpassword";
            
            var builder = new RelmContextOptionsBuilder();

            // Act
            builder.SetDatabaseConnectionString(expectedDatabaseConnectionString);

            // Assert
            Assert.Equal(expectedConnectionStringType, builder.OptionsBuilderType);
            Assert.Equal(expectedDatabaseConnectionString, builder.DatabaseConnectionString);
        }

        [Fact]
        public void SetDatabaseConnectionString_ThrowsExceptionForInvalidConnectionStringType()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();

            // Act 
            builder.SetDatabaseConnectionString("INVALID CONNECTION STRING");

            // Assert
            Assert.Throws<ArgumentNullException>(() => builder.ValidateAllSettings());
        }

        [Fact]
        public void SetDatabaseConnectionString_SetsOptionsBuilderTypeToNamedConnectionString()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedType = RelmContextOptionsBuilder.OptionsBuilderTypes.ConnectionString;
            var validConnectionStringType = "server=localhost;database=simple_relm;user id=simplerelmuser;password=simplerelmpassword"; // Make sure this is valid

            // Act
            builder.SetDatabaseConnectionString(validConnectionStringType);

            // Assert
            Assert.Equal(expectedType, builder.OptionsBuilderType);
        }

        [Fact]
        public void ValidateAllSettings_ReturnsTrue_ForValidNamedConnectionString()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder("server=localhost;database=simple_relm;user id=simplerelmuser;password=simplerelmpassword");

            // Act
            var result = builder.ValidateAllSettings();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateAllSettings_ReturnsTrue_ForValidNamedConnectionString_WithFalseParameter()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder("server=localhost;database=simple_relm;user id=simplerelmuser;password=simplerelmpassword");

            // Act
            var result = builder.ValidateAllSettings(false);

            // Assert
            Assert.True(result);
        }
    }
}
