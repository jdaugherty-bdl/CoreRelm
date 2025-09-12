using CoreRelm.Options;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Options.RelmContextOptionsBuilder;

namespace CoreRelm.Tests.Options.RelmContextOptionBuilder_Tests
{
    public class RelmContextOptionsBuilder_Tester
    {
        [Fact]
        public void RelmContextOptionsBuilder_Constructor_Empty()
        {
            // Arrange & Act
            var builder = new RelmContextOptionsBuilder();

            // Assert
            Assert.NotNull(builder);
        }

        [Fact]
        public void RelmContextOptionsBuilder_Constructor_InvalidConnectionString_ShouldThrowException()
        {
            // Arrange & Act
            var ex = Assert.Throws<ArgumentException>(() => new RelmContextOptionsBuilder("INVALID CONNECTION STRING"));

            // Assert
            Assert.Equal("Invalid connection details. Must be in the format of 'name=connectionString' or 'server=serverName;database=databaseName;user=userName;password=password'.", ex.Message);
        }

        [Fact]
        public void RelmContextOptionsBuilder_Constructor_NamedConnection()
        {
            // Arrange & Act
            var builder = new RelmContextOptionsBuilder("name=SimpleRelmMySql");

            // Assert
            Assert.Equal("SimpleRelmMySql", builder.NamedConnection);
            Assert.Equal(ConfigurationManager.ConnectionStrings["SimpleRelmMySql"].ConnectionString, builder.DatabaseConnectionString);
            Assert.Equal(OptionsBuilderTypes.NamedConnectionString, builder.OptionsBuilderType);
        }

        [Fact]
        public void RelmContextOptionsBuilder_Constructor_NamedConnection_InvalidConnectionString_ShouldThrowException()
        {
            // Arrange & Act
            var ex = Assert.Throws<ArgumentException>(() => new RelmContextOptionsBuilder("name=MyConnection;server=localhost;database=simple_relm;user=simplerelmuser;password=simplerelmpassword"));

            // Assert
            Assert.Equal("Invalid connection details. Must be in the format of 'name=connectionString' or 'server=serverName;database=databaseName;user=userName;password=password'.", ex.Message);
        }

        [Fact]
        public void RelmContextOptionsBuilder_Constructor_ConnectionString()
        {
            // Arrange & Act
            var builder = new RelmContextOptionsBuilder("server=localhost;database=simple_relm;user=simplerelmuser;password=simplerelmpassword");

            // Assert
            Assert.Equal(OptionsBuilderTypes.ConnectionString, builder.OptionsBuilderType);
            Assert.Equal("localhost", builder.DatabaseServer);
            Assert.Equal("simple_relm", builder.DatabaseName);
            Assert.Equal("simplerelmuser", builder.DatabaseUser);
            Assert.Equal("simplerelmpassword", builder.DatabasePassword);
        }

        [Fact]
        public void RelmContextOptionsBuilder_Constructor_ConnectionString_InvalidConnectionString_ShouldThrowException()
        {
            // Arrange & Act
            var ex = Assert.Throws<ArgumentException>(() => new RelmContextOptionsBuilder("server=localhost;database=simple_relm;user=simplerelmuser;password=simplerelmpassword;name=MyConnection"));

            // Assert
            Assert.Equal("Invalid connection details. Must be in the format of 'name=connectionString' or 'server=serverName;database=databaseName;user=userName;password=password'.", ex.Message);
        }


        [Fact]
        public void RelmContextOptionsBuilder_AddConnectionString_NamedConnection_InvalidConnectionString_ShouldThrowException()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();

            // Act
            var ex = Assert.Throws<ArgumentException>(() => builder.ParseConnectionDetails("name=MyConnection;server=localhost;database=simple_relm;user=simplerelmuser;password=simplerelmpassword"));

            // Assert
            Assert.Equal("Invalid connection details. Must be in the format of 'name=connectionString' or 'server=serverName;database=databaseName;user=userName;password=password'.", ex.Message);
        }

        [Fact]
        public void RelmContextOptionsBuilder_AddConnectionString_ConnectionString_InvalidConnectionString_ShouldThrowException()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();

            // Act
            var ex = Assert.Throws<ArgumentException>(() => builder.ParseConnectionDetails("server=localhost;database=simple_relm;user=simplerelmuser;password=simplerelmpassword;name=MyConnection"));

            // Assert
            Assert.Equal("Invalid connection details. Must be in the format of 'name=connectionString' or 'server=serverName;database=databaseName;user=userName;password=password'.", ex.Message);
        }

        [Fact]
        public void RelmContextOptionsBuilder_AddConnectionString_ConnectionString()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();

            // Act
            builder.ParseConnectionDetails("server=localhost;database=simple_relm;user=simplerelmuser;password=simplerelmpassword");

            // Assert
            Assert.Equal(OptionsBuilderTypes.ConnectionString, builder.OptionsBuilderType);
            Assert.Equal("localhost", builder.DatabaseServer);
            Assert.Equal("simple_relm", builder.DatabaseName);
            Assert.Equal("simplerelmuser", builder.DatabaseUser);
            Assert.Equal("simplerelmpassword", builder.DatabasePassword);
        }

        [Fact]
        public void RelmContextOptionsBuilder_AddDatabaseName()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            // Act
            builder.SetDatabaseName("simple_relm");
            // Assert
            Assert.Equal("simple_relm", builder.DatabaseName);
        }

        [Fact]
        public void RelmContextOptionsBuilder_AddDatabaseName_InvalidDatabaseName_ShouldThrowException()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            // Act
            var ex = Assert.Throws<ArgumentException>(() => builder.SetDatabaseName("invalid<>_database[]_name()"));
            // Assert
            Assert.Equal("DatabaseName (Parameter 'Invalid database name. Must be alphanumeric with underscores.')", ex.Message);
        }
        [Fact]
        public void RelmContextOptionsBuilder_AddDatabasePassword()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            // Act
            builder.SetDatabasePassword("simplerelmpassword");
            // Assert
            Assert.Equal("simplerelmpassword", builder.DatabasePassword);
        }
        [Fact]
        public void RelmContextOptionsBuilder_AddDatabaseServer()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            // Act
            builder.SetDatabaseServer("localhost");
            // Assert
            Assert.Equal("localhost", builder.DatabaseServer);
        }
        [Fact]
        public void RelmContextOptionsBuilder_AddDatabaseUser()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            // Act
            builder.SetDatabaseUser("simplerelmuser");
            // Assert
            Assert.Equal("simplerelmuser", builder.DatabaseUser);
        }
    }
}
