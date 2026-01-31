using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Resolvers
{
    /// <summary>
    /// Defines methods for obtaining a database connection string builder based on a specified connection type or
    /// connection string.
    /// </summary>
    public interface IRelmResolverBase
    {
        /// <summary>
        /// Creates and returns a new <see cref="DbConnectionStringBuilder"/> instance configured for the specified
        /// connection type.
        /// </summary>
        /// <param name="ConnectionType">An enumeration value that specifies the type of database connection for which to create the connection
        /// string builder. The value must correspond to a supported connection type.</param>
        /// <returns>A <see cref="DbConnectionStringBuilder"/> instance preconfigured for the specified connection type.</returns>
        DbConnectionStringBuilder GetConnectionBuilder(Enum ConnectionType);

        /// <summary>
        /// Creates and returns a new <see cref="DbConnectionStringBuilder"/> instance initialized with the specified connection
        /// string.
        /// </summary>
        /// <param name="ConnectionString">The connection string to use for initializing the DbConnectionStringBuilder. Cannot be null or empty.</param>
        /// <returns>A <see cref="DbConnectionStringBuilder"/> initialized with the provided connection string.</returns>
        DbConnectionStringBuilder GetConnectionBuilder(string ConnectionString);
    }
}
