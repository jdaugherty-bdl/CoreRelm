using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Resolvers
{
    /// <summary>
    /// Defines methods for obtaining MySQL connection string builders based on connection type, name, or raw connection
    /// string.
    /// </summary>
    /// <remarks>This interface extends IRelmResolverBase to provide MySQL-specific connection string
    /// resolution. Implementations are expected to return configured MySQLConnectionStringBuilder instances for use in
    /// establishing MySQL database connections.</remarks>
    public interface IRelmResolver_MySQL : IRelmResolverBase
    {
        /// <summary>
        /// Creates a new <see cref="MySqlConnectionStringBuilder"/> instance configured for the specified connection type.
        /// </summary>
        /// <param name="ConnectionType">An enumeration value that specifies the type of connection for which to create the connection string
        /// builder. Must be a valid value recognized by the method.</param>
        /// <returns>A <see cref="MySqlConnectionStringBuilder"/> instance configured according to the specified connection type.</returns>
        MySqlConnectionStringBuilder GetConnectionBuilderFromType(Enum ConnectionType);

        /// <summary>
        /// Retrieves a new <see cref="MySqlConnectionStringBuilder"/> instance initialized with the specified connection string.
        /// </summary>
        /// <param name="ConnectionString">The name or value of the connection string to use for initializing the connection string builder. Cannot be
        /// null or empty.</param>
        /// <returns>A <see cref="MySqlConnectionStringBuilder"/> configured with the provided connection string.</returns>
        MySqlConnectionStringBuilder GetConnectionBuilderFromName(string ConnectionString);

        /// <summary>
        /// Creates a new <see cref="MySqlConnectionStringBuilder"/> instance based on the specified connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to parse. Must be a valid MySQL connection string.</param>
        /// <returns>A <see cref="MySqlConnectionStringBuilder"/> initialized with the values from the specified connection string.</returns>
        MySqlConnectionStringBuilder GetConnectionBuilderFromConnectionString(string connectionString);
    }
}
