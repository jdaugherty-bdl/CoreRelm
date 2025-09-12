using MySql.Data.MySqlClient;
using CoreRelm.RelmInternal.Resolvers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.RelmInternal.Resolvers
{
    public class DefaultRelmResolver_Tester
    {
        enum ConnectionType
        {
            InvalidConnectionString,
            SimpleRelmMySql,
            SimpleRelmSqlExpress,
            SimpleRelmSqlServerCe
        }

        [Fact]
        public void GetConnectionBuilder_ByString_ThrowsException()
        {
            // Arrange
            var resolver = new DefaultRelmResolver();

            // Act
            var ex = Assert.Throws<ArgumentException>(() => resolver.GetConnectionBuilderFromConnectionString("INVALID CONNECTION STRING"));

            // Assert
            Assert.Equal("Format of the initialization string does not conform to specification starting at index 0.", ex.Message);
        }

        [Fact]
        public void GetConnectionBuilder_ByType_ThrowsException()
        {
            // Arrange
            var resolver = new DefaultRelmResolver();

            // Act
            var ex = Assert.Throws<ArgumentException>(() => resolver.GetConnectionBuilderFromType(ConnectionType.InvalidConnectionString));

            // Assert
            Assert.Equal("Format of the initialization string does not conform to specification starting at index 0.", ex.Message);
        }

        [Fact]
        public void GetConnectionBuilder_ByString_MySql()
        {
            // Arrange
            var resolver = new DefaultRelmResolver();
            var expected = new MySqlConnectionStringBuilder("server=localhost;database=simple_relm;uid=simplerelmuser;pwd=simplerelmpassword");

            // Act
            var actual = resolver.GetConnectionBuilderFromConnectionString("server=localhost;database=simple_relm;uid=simplerelmuser;pwd=simplerelmpassword");

            // Assert
            Assert.Equal(expected.ConnectionString, actual.ConnectionString);
        }

        [Fact]
        public void GetConnectionBuilder_ByName_MySql()
        {
            // Arrange
            var resolver = new DefaultRelmResolver();
            var expected = new MySqlConnectionStringBuilder("server=localhost;database=simple_relm;user id=simplerelmuser;password=simplerelmpassword");

            // Act
            var actual = resolver.GetConnectionBuilderFromName("SimpleRelmMySql");

            // Assert
            Assert.Equal(expected.ConnectionString, actual.ConnectionString);
        }

        [Fact]
        public void GetConnectionBuilder_ByType_MySql()
        {
            // Arrange
            var resolver = new DefaultRelmResolver();
            var expected = new MySqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["SimpleRelmMySql"].ConnectionString);

            // Act
            var actual = resolver.GetConnectionBuilderFromType(ConnectionType.SimpleRelmMySql);

            // Assert
            Assert.Equal(expected.ConnectionString, actual.ConnectionString);
        }
    }
}
