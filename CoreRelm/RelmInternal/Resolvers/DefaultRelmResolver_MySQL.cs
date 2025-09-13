using CoreRelm.Interfaces.Resolvers;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Resolvers
{
    internal class DefaultRelmResolver_MySQL(IConfiguration? configuration = null) : IRelmResolver_MySQL
    {
        private IConfiguration? _configuration = configuration;

        // if no other DAL Resolvers are specified in the client program, this one is used
        public MySqlConnectionStringBuilder GetConnectionBuilderFromType(Enum ConnectionType)
        {
            // converts the enum name directly to string and then looks for that in the configuration file
            return GetConnectionBuilderFromName(ConnectionType.ToString());
        }

        public MySqlConnectionStringBuilder GetConnectionBuilderFromName(string ConfigConnectionString)
        {
            if (_configuration == null)
            {
                _configuration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                    .Build();
            }

            //return GetConnectionBuilderFromConnectionString(ConfigurationManager.ConnectionStrings[ConfigConnectionString].ConnectionString);
            var conn = _configuration?.GetConnectionString(ConfigConnectionString);
            if (string.IsNullOrEmpty(conn))
                throw new KeyNotFoundException($"Connection string '{ConfigConnectionString}' not found.");
            return GetConnectionBuilderFromConnectionString(conn);
        }

        public MySqlConnectionStringBuilder GetConnectionBuilderFromConnectionString(string connectionString)
        {
            return new MySqlConnectionStringBuilder(connectionString);
        }

        DbConnectionStringBuilder IRelmResolverBase.GetConnectionBuilder(Enum ConnectionType)
        {
            return GetConnectionBuilderFromType(ConnectionType);
        }

        DbConnectionStringBuilder IRelmResolverBase.GetConnectionBuilder(string ConnectionString)
        {
            return GetConnectionBuilderFromName(ConnectionString);
        }
    }
}
