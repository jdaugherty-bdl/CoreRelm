using CoreRelm.Models;
using CoreRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Models.RelmContext_Tests
{
    public class RelmContextInitializationTests
    {
        [Fact]
        public void Should_Initialize_With_Valid_Named_Connection_String()
        {
            // Arrange
            string validConnectionString = "name=SimpleRelmMySql";

            // Act
            var dataSet = new RelmContext(validConnectionString, autoOpenConnection: false);

            // Assert
            Assert.NotNull(dataSet);
        }

        [Fact]
        public void Should_Throw_Exception_With_Empty_Connection_String()
        {
            // Arrange
            string invalidConnectionString = "";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RelmContext(invalidConnectionString));
        }

        /*
        [Fact]
        public void Should_Throw_Exception_With_Invalid_Named_Connection_String()
        {
            // Arrange
            string invalidConnectionString = "name=InvalidConnectionString";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new RelmContext(invalidConnectionString));
        }
        */

        [Fact]
        public void Should_Initialize_With_Valid_OptionsBuilder_ConnectionString()
        {
            // Arrange
            var validOptions = new RelmContextOptionsBuilder();

            validOptions.SetNamedConnection("SimpleRelmMySql");
            validOptions.SetDatabaseConnectionString(RelmHelper.GetConnectionBuilderFromName("SimpleRelmMySql").ConnectionString);

            // Act
            var dataSet = new RelmContext(validOptions, autoOpenConnection: false);

            // Assert
            Assert.NotNull(dataSet);
        }

        [Fact]
        public void Should_Throw_Exception_With_Null_ConnectionString()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RelmContext(default(string)));
        }

        [Fact]
        public void Should_Throw_Exception_With_Null_OptionsBuilder()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RelmContext(default(RelmContextOptionsBuilder)));
        }

        [Fact]
        public void Should_Throw_Exception_With_Empty_OptionsBuilder()
        {
            // Arrange
            var invalidOptions = new RelmContextOptionsBuilder();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RelmContext(invalidOptions));
        }
    }
}
