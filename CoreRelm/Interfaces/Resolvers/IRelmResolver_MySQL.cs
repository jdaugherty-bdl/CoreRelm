using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Resolvers
{
    public interface IRelmResolver_MySQL : IRelmResolverBase
    {
        MySqlConnectionStringBuilder GetConnectionBuilderFromType(Enum ConnectionType);
        MySqlConnectionStringBuilder GetConnectionBuilderFromName(string ConnectionString);
        MySqlConnectionStringBuilder GetConnectionBuilderFromConnectionString(string connectionString);
    }
}
