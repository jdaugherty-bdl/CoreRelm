using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.MySql;

namespace CoreRelm.Tests.Integration
{
    public sealed class MySql84Fixture : IAsyncLifetime
    {
        private const string _username = "root";
        private const string _password = "testpw";

        public MySqlContainer Container { get; }

        public MySql84Fixture()
        {
            // Modules follow the module-specific builder pattern.
            // [3](https://dotnet.testcontainers.org/modules/)
            // [4](https://github.com/testcontainers/testcontainers-dotnet/discussions/856)
            // Use mysql:8.4 (or mysql:8.4.8) as Docker Hub supports 8.4 tags.
            // [9](https://hub.docker.com/_/mysql/)
            // [10](https://hub.docker.com/_/mysql/tags)
            var builder = new MySqlBuilder("mysql:8.4")
                // If your version supports config methods, use them; otherwise defaults will work.
                // Use root password for simplicity in tests:
                .WithPassword(_password);

#if DEBUG
            builder = builder.WithPortBinding(61152, 3306);
#endif

            Container = builder.Build();
        }

        public Task InitializeAsync() => Container.StartAsync();

        public Task DisposeAsync() => Container.DisposeAsync().AsTask();

        public string Hostname => Container.Hostname;

        public ushort Port => Container.GetMappedPublicPort(3306);

        public string Username => _username;

        public string Password => _password;

        public string BuildConnectionStringTemplate()
        {
            // Note: CoreRelm expects ConnectionStringTemplate to contain "{db}" for apply/generate paths. [2](https://bostondatalabs-my.sharepoint.com/personal/jdaugherty_bostondatalabs_com/Documents/Microsoft%20Copilot%20Chat%20Files/DefaultRelmMigrationSqlProvider.cs)[2](https://bostondatalabs-my.sharepoint.com/personal/jdaugherty_bostondatalabs_com/Documents/Microsoft%20Copilot%20Chat%20Files/DefaultRelmMigrationSqlProvider.cs)
            return $"Server={Hostname};Port={Port};Uid={Username};Pwd={Password};Database={{db}};SslMode=None;AllowPublicKeyRetrieval=true;";
        }

        public string BuildServerConnectionString()
        {
            // No database selected (used for DROP/CREATE DATABASE).
            return $"Server={Hostname};Port={Port};Uid={Username};Pwd={Password};SslMode=None;AllowPublicKeyRetrieval=true;";
        }
    }
}
