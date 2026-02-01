using MySql.Data.MySqlClient;
using CoreRelm.Interfaces.Resolvers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CoreRelm.RelmInternal.Resolvers
{
    internal class DefaultRelmResolver_MySQL : IRelmResolver_MySQL
    {
        readonly IConfiguration config;

        public DefaultRelmResolver_MySQL(IConfiguration configuration)
        {
            config = configuration;
        }

        // if no other DAL Resolvers are specified in the client program, this one is used
        public MySqlConnectionStringBuilder GetConnectionBuilderFromType(Enum ConnectionType)
        {
            // converts the enum name directly to string and then looks for that in the configuration file
            return GetConnectionBuilderFromName(ConnectionType.ToString());
        }

        public MySqlConnectionStringBuilder GetConnectionBuilderFromName(string ConfigConnectionString)
        {
            return GetConnectionBuilderFromConnectionString(config.GetConnectionString(ConfigConnectionString));
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
