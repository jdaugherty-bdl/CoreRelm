using MySql.Data.MySqlClient;
using CoreRelm.RelmInternal.Helpers.Operations;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CoreRelm.Options
{
    /// <summary>
    /// Provides a builder for configuring options required to establish a connection to a relational database context.
    /// Supports multiple initialization patterns, including connection strings, named connections, and open MySQL
    /// connections.
    /// </summary>
    /// <remarks>Use this class to specify database connection details such as server, database name, user
    /// credentials, or to provide an existing MySqlConnection or named connection. The builder validates configuration
    /// based on the selected connection method. Once configured, the options can be used to initialize a database
    /// context. This class is not thread-safe.</remarks>
    public class RelmContextOptionsBuilder
    {
        /// <summary>
        /// Specifies the types of options that can be used to configure a database context using an options builder.
        /// </summary>
        /// <remarks>Use this enumeration to indicate how the options builder should obtain or interpret
        /// connection information when configuring a database context. The selected value determines whether a raw
        /// connection string, a named connection string, or an existing open connection is used.</remarks>
        public enum OptionsBuilderTypes
        {
            /// <summary>
            /// Sets the option builder connection type to use a raw connection string.
            /// </summary>
            ConnectionString,
            /// <summary>
            /// Sets the option builder connection type to use a named connection string.
            /// </summary>
            NamedConnectionString,
            /// <summary>
            /// Sets the option builder connection type to use an open connection.
            /// </summary>
            OpenConnection
        }

        /// <summary>
        /// Gets the name or network address of the database server to which the application is connected.
        /// </summary>
        public string? DatabaseServer { get; private set; }
        
        /// <summary>
        /// Gets the port number used to connect to the database as a string value.
        /// </summary>
        /// <remarks>The port value may be null if not specified. The format should be a valid port number
        /// as a string, such as "5432" for PostgreSQL or "1433" for SQL Server.</remarks>
        public string? DatabasePort { get; private set; }

        /// <summary>
        /// Gets the name of the database associated with this instance.
        /// </summary>
        public string? DatabaseName { get; private set; }

        /// <summary>
        /// Gets the user name used to connect to the database.
        /// </summary>
        public string? DatabaseUser { get; private set; }

        /// <summary>
        /// Gets the password used to connect to the database.
        /// </summary>
        public string? DatabasePassword { get; private set; }

        /// <summary>
        /// Gets the connection string used to connect to the database.
        /// </summary>
        public string? DatabaseConnectionString { get; private set; }

        /// <summary>
        /// Gets or sets the name of the connection to use for database operations.
        /// </summary>
        public string? NamedConnection { get; set; }

        /// <summary>
        /// Gets the active MySQL database connection used by the application.
        /// </summary>
        public MySqlConnection? DatabaseConnection { get; private set; }

        /// <summary>
        /// Gets the current database transaction associated with the connection.
        /// </summary>
        /// <remarks>Use this property to access the active MySQL transaction for executing commands
        /// within a transactional context. The property is null if no transaction is in progress.</remarks>
        public MySqlTransaction? DatabaseTransaction { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the connection should be automatically opened when required.
        /// </summary>
        public bool AutoOpenConnection { get; private set; } = true;

        /// <summary>
        /// Gets a value indicating whether a transaction is automatically opened when a database connection is
        /// established.
        /// </summary>
        public bool AutoOpenTransaction { get; private set; } = false;

        /// <summary>
        /// Gets a value indicating whether user-defined variables are permitted in SQL statements.
        /// </summary>
        public bool AllowUserVariables { get; private set; } = false;

        /// <summary>
        /// Gets a value indicating whether date and time values of zero are converted to DateTime.MinValue when
        /// retrieved from the database.
        /// </summary>
        /// <remarks>When enabled, date and time fields with a value of '0000-00-00' or '0000-00-00
        /// 00:00:00' are returned as DateTime.MinValue instead of causing an exception or being treated as invalid.
        /// This option is useful when working with databases that allow zero date values.</remarks>
        public bool ConvertZeroDateTime { get; private set; } = false;

        /// <summary>
        /// Gets the maximum number of seconds to wait for a lock to be acquired before timing out.
        /// </summary>
        public int LockWaitTimeoutSeconds { get; private set; } = 30;

        /// <summary>
        /// Gets a value indicating whether data sets are automatically initialized when the component is created.
        /// </summary>
        public bool AutoInitializeDataSets { get; private set; } = true;

        /// <summary>
        /// Gets a value indicating whether table verification is performed automatically before database operations.
        /// </summary>
        public bool AutoVerifyTables { get; private set; } = true;

        /// <summary>
        /// Gets the type of options builder used to configure options for this instance.
        /// </summary>
        public OptionsBuilderTypes OptionsBuilderType => _optionsBuilderType;
        private OptionsBuilderTypes _optionsBuilderType;

        /// <summary>
        /// Gets the type of the connection string used by the data source.
        /// </summary>
        public Enum? ConnectionStringType => _connectionStringType;
        private Enum? _connectionStringType;

        internal bool CanOpenConnection { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of the RelmContextOptionsBuilder class.
        /// </summary>
        public RelmContextOptionsBuilder() { }

        /// <summary>
        /// Initializes a new instance of the RelmContextOptionsBuilder class using the specified connection string.
        /// </summary>
        /// <param name="connectionString">The connection string used to configure the context options. Cannot be null or empty.</param>
        /// <exception cref="ArgumentNullException">Thrown if the connectionString parameter is null or empty.</exception>
        public RelmContextOptionsBuilder(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");

            ParseConnectionDetails(connectionString);
        }

        /// <summary>
        /// Initializes a new instance of the RelmContextOptionsBuilder class with the specified database connection
        /// settings.
        /// </summary>
        /// <param name="databaseServer">The name or network address of the database server to connect to. Cannot be null or empty.</param>
        /// <param name="databaseName">The name of the database to use. Cannot be null or empty.</param>
        /// <param name="databaseUser">The username to use when connecting to the database. Cannot be null or empty.</param>
        /// <param name="databasePassword">The password associated with the specified database user. Cannot be null or empty.</param>
        public RelmContextOptionsBuilder(string databaseServer, string databasePort, string databaseName, string databaseUser, string databasePassword)
        {
            SetDatabaseServer(databaseServer);
            SetDatabasePort(databasePort);
            SetDatabaseName(databaseName);
            SetDatabaseUser(databaseUser);
            SetDatabasePassword(databasePassword);
        }

        /// <summary>
        /// Initializes a new instance of the RelmContextOptionsBuilder class using the specified connection string
        /// type.
        /// </summary>
        /// <param name="connectionStringType">An enumeration value that specifies the type of connection string to use. Must be a valid enum representing
        /// a supported connection string type.</param>
        public RelmContextOptionsBuilder(Enum connectionStringType)
        {
            SetConnectionStringType(connectionStringType.GetType(), connectionStringType);
        }

        /// <summary>
        /// Initializes a new instance of the RelmContextOptionsBuilder class using the specified MySQL database
        /// connection.
        /// </summary>
        /// <param name="connection">The MySqlConnection to use for configuring the context options. Cannot be null.</param>
        public RelmContextOptionsBuilder(MySqlConnection connection)
        {
            SetDatabaseConnection(connection);
        }

        /// <summary>
        /// Initializes a new instance of the RelmContextOptionsBuilder class using the specified MySqlConnection and
        /// MySqlTransaction.
        /// </summary>
        /// <remarks>Use this constructor to configure the context to operate within an existing MySQL
        /// connection and transaction. This is useful when managing connection and transaction lifetimes
        /// externally.</remarks>
        /// <param name="connection">The MySqlConnection to be used for database operations. Cannot be null.</param>
        /// <param name="transaction">The MySqlTransaction to associate with the context. Cannot be null.</param>
        public RelmContextOptionsBuilder(MySqlConnection connection, MySqlTransaction? transaction)
        {
            SetDatabaseConnection(connection);
            SetDatabaseTransaction(transaction);
        }

        /// <summary>
        /// Sets the database connection to be used by the current instance.
        /// </summary>
        /// <remarks>After calling this method, the instance will use the provided connection for all
        /// subsequent database operations. The caller is responsible for managing the lifetime of the
        /// connection.</remarks>
        /// <param name="connection">The open <see cref="MySqlConnection"/> to associate with this instance. The connection must not be null.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="connection"/> is null.</exception>
        public RelmContextOptionsBuilder SetDatabaseConnection(MySqlConnection? connection)
        {
            DatabaseConnection = connection ?? throw new ArgumentNullException(nameof(connection), "Connection cannot be null.");

            _optionsBuilderType = OptionsBuilderTypes.OpenConnection;

            return this;
        }

        /// <summary>
        /// Sets the database transaction to be used for subsequent database operations.
        /// </summary>
        /// <remarks>Use this method to specify an existing transaction for database commands. If a
        /// transaction is set, all subsequent operations will be executed within the context of that transaction until
        /// it is cleared or replaced.</remarks>
        /// <param name="transaction">The MySqlTransaction instance to associate with database operations. Can be null to clear the current
        /// transaction.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        public RelmContextOptionsBuilder SetDatabaseTransaction(MySqlTransaction? transaction)
        {
            DatabaseTransaction = transaction;

            _optionsBuilderType = OptionsBuilderTypes.OpenConnection;

            return this;
        }

        /// <summary>
        /// Sets the database server to use for establishing connections.
        /// </summary>
        /// <param name="databaseServer">The name or address of the database server. Cannot be null or empty.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="databaseServer"/> is null or empty.</exception>
        public RelmContextOptionsBuilder SetDatabaseServer(string databaseServer)
        {
            if (string.IsNullOrEmpty(databaseServer))
                throw new ArgumentNullException(nameof(databaseServer), "Database server cannot be null or empty.");

            this.DatabaseServer = databaseServer;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;

            return this;
        }

        public RelmContextOptionsBuilder SetDatabasePort(string databasePort)
        {
            this.DatabasePort = databasePort;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;

            return this;
        }

        /// <summary>
        /// Sets the name of the database to be used for the connection.
        /// </summary>
        /// <param name="databaseName">The name of the database. Must be a non-empty string containing only alphanumeric characters, underscores
        /// (_), dollar signs ($), or Unicode characters in the range U+0080 to U+FFFF.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the databaseName parameter is null or an empty string.</exception>
        /// <exception cref="ArgumentException">Thrown if databaseName contains invalid characters. The name must be alphanumeric and may include
        /// underscores (_), dollar signs ($), or Unicode characters in the range U+0080 to U+FFFF.</exception>
        public RelmContextOptionsBuilder SetDatabaseName(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentNullException(nameof(databaseName), "Database name cannot be null or empty.");

            string pattern = @"^[a-zA-Z0-9$_\u0080-\uFFFF]+$";

            if (!Regex.IsMatch(databaseName, pattern))
                throw new ArgumentException("Invalid database name. Must be alphanumeric with underscores.", nameof(databaseName));

            this.DatabaseName = databaseName;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;

            return this;
        }

        /// <summary>
        /// Sets the database user name to be used for the connection.
        /// </summary>
        /// <param name="databaseUser">The user name to associate with the database connection. Cannot be null or empty.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="databaseUser"/> is null or empty.</exception>
        public RelmContextOptionsBuilder SetDatabaseUser(string databaseUser)
        {
            if (string.IsNullOrEmpty(databaseUser))
                throw new ArgumentNullException(nameof(databaseUser), "Database user cannot be null or empty.");

            this.DatabaseUser = databaseUser;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;

            return this;
        }

        /// <summary>
        /// Sets the password used to connect to the database.
        /// </summary>
        /// <param name="databasePassword">The password to use for authenticating the database connection. Cannot be null or empty.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="databasePassword"/> is null or empty.</exception>
        public RelmContextOptionsBuilder SetDatabasePassword(string databasePassword)
        {
            if (string.IsNullOrEmpty(databasePassword))
                throw new ArgumentNullException(nameof(databasePassword), "Database password cannot be null or empty.");

            this.DatabasePassword = databasePassword;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;

            return this;
        }

        /// <summary>
        /// Sets the type of the connection string using the specified enumeration value.
        /// </summary>
        /// <param name="connectionStringType">An enumeration value that specifies the type of connection string to use. Must not be null.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        public RelmContextOptionsBuilder SetConnectionStringType(Enum connectionStringType)
        {
            SetConnectionStringType(connectionStringType.GetType(), connectionStringType);

            return this;
        }

        /// <summary>
        /// Sets the type of the connection string to use for database operations.
        /// </summary>
        /// <param name="enumType">The enumeration type that defines the valid connection string types. Must be an enum type.</param>
        /// <param name="connectionStringType">The specific connection string type to set. Must be a defined value of <paramref name="enumType"/>.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="connectionStringType"/> is not a defined value of <paramref name="enumType"/>.</exception>
        public RelmContextOptionsBuilder SetConnectionStringType(Type enumType, Enum connectionStringType)
        { 
            if (!Enum.IsDefined(enumType, connectionStringType))
                throw new ArgumentNullException(nameof(connectionStringType), "Invalid connection string type provided.");

            _connectionStringType = connectionStringType;

            NamedConnection = connectionStringType.ToString();

            _optionsBuilderType = OptionsBuilderTypes.NamedConnectionString;

            return this;
        }

        /// <summary>
        /// Sets the current database connection using the specified named connection string.
        /// </summary>
        /// <param name="namedConnection">The name of the connection string to use. Cannot be null or empty.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the namedConnection parameter is null or empty.</exception>
        public RelmContextOptionsBuilder SetNamedConnection(string namedConnection)
        {
            if (string.IsNullOrEmpty(namedConnection))
                throw new ArgumentNullException(nameof(namedConnection));

            NamedConnection = namedConnection;

            /*
            if (!Enum.TryParse(DatabaseConnectionString, out _connectionStringType))
                throw new ArgumentException($"Invalid connection string type '{DatabaseConnectionString}'.");
            ConnectionStringType = (DALHelper.ConnectionStringTypes)Enum.Parse(typeof(DALHelper.ConnectionStringTypes), DatabaseConnectionString);
            */

            _optionsBuilderType = OptionsBuilderTypes.NamedConnectionString;

            return this;
        }

        /// <summary>
        /// Sets the connection string used to connect to the database.
        /// </summary>
        /// <param name="DatabaseConnectionString">The connection string to use for database connections. Cannot be null or empty.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="DatabaseConnectionString"/> is null or empty.</exception>
        public RelmContextOptionsBuilder SetDatabaseConnectionString(string DatabaseConnectionString)
        {
            if (string.IsNullOrEmpty(DatabaseConnectionString))
                throw new ArgumentNullException(nameof(DatabaseConnectionString));

            this.DatabaseConnectionString = DatabaseConnectionString;

            return this;
        }

        /// <summary>
        /// Validates all required database connection settings based on the configured options builder type.
        /// </summary>
        /// <remarks>The required settings depend on the options builder type. For example, a named
        /// connection string requires DatabaseConnectionString, an open connection requires DatabaseConnection, and a
        /// standard connection string requires DatabaseServer, DatabaseName, DatabaseUser, and
        /// DatabasePassword.</remarks>
        /// <param name="throwExceptions">true to throw an exception if a required setting is missing; false to return false instead of throwing an
        /// exception. The default is true.</param>
        /// <returns>true if all required settings are valid; otherwise, false if a required setting is missing and
        /// throwExceptions is false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if a required setting is missing and throwExceptions is true.</exception>
        /// <exception cref="ArgumentException">Thrown if the configured connection string type is invalid.</exception>
        public bool ValidateAllSettings(bool throwExceptions = true)
        {
            if (_optionsBuilderType == OptionsBuilderTypes.NamedConnectionString)
            {
                if (string.IsNullOrEmpty(DatabaseConnectionString))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException(nameof(DatabaseConnectionString), "DatabaseConnectionString cannot be null or empty when using a named connection string.");
                    else
                        return false;
                }

                return true;
            }
            else if (_optionsBuilderType == OptionsBuilderTypes.OpenConnection)
            {
                if (DatabaseConnection == null)
                {
                    if (throwExceptions)
                        throw new ArgumentNullException(nameof(DatabaseConnection), "Database connection cannot be null.");
                    else
                        return false;
                }

                return true;
            }
            else if (_optionsBuilderType == OptionsBuilderTypes.ConnectionString)
            {
                if (string.IsNullOrEmpty(DatabaseServer))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException(nameof(DatabaseServer), "Database Server cannot be null or empty when using a connection string.");
                    else
                        return false;
                }

                if (string.IsNullOrEmpty(DatabaseName))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException(nameof(DatabaseName), "Database Name cannot be null or empty when using a connection string.");
                    else
                        return false;
                }

                if (string.IsNullOrEmpty(DatabaseUser))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException(nameof(DatabaseUser), "Username cannot be null or empty when using a connection string.");
                    else
                        return false;
                }

                if (string.IsNullOrEmpty(DatabasePassword))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException(nameof(DatabasePassword), "Password cannot be null or empty when using a connection string.");
                    else
                        return false;
                }

                return true;
            }
            else
            {
                throw new ArgumentException($"Invalid connection string type '{ConnectionStringType}'.");
            }
        }

        /// <summary>
        /// Parses the specified connection details string and configures the database connection settings accordingly.
        /// </summary>
        /// <remarks>The method supports two formats for specifying connection details: a named connection
        /// (e.g., 'name=MyConnection') or explicit connection parameters (e.g.,
        /// 'server=localhost;database=MyDb;user=admin;password=secret'). Only one format may be used at a time.
        /// Additional or missing parameters will result in an exception.</remarks>
        /// <param name="connectionDetails">A string containing the connection details. Must be in the format 'name=connectionString' to use a named
        /// connection, or 'server=serverName;database=databaseName;user=userName;password=password' to specify
        /// individual connection parameters.</param>
        /// <exception cref="ArgumentNullException">Thrown if the connectionDetails parameter is null, empty, or consists only of white-space characters.</exception>
        /// <exception cref="ArgumentException">Thrown if the connectionDetails parameter does not match the required format or is missing required
        /// connection parameters.</exception>
        public void ParseConnectionDetails(string connectionDetails)
        {
            if (string.IsNullOrWhiteSpace(connectionDetails))
                throw new ArgumentNullException(nameof(connectionDetails));

            if (!connectionDetails.Contains('='))
                throw new ArgumentException("Invalid connection details. Must be in the format of 'name=connectionString' or 'server=serverName;database=databaseName;user=userName;password=password'.");

            var connectionOptions = connectionDetails
                .Split([';'], StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(['='], StringSplitOptions.RemoveEmptyEntries))
                .ToDictionary(x => x[0].ToLower(), x => x[1]);

            // Check for either a 'name' key or all four individual connection parameters.
            if (!(connectionOptions.ContainsKey("name") ||
                  (connectionOptions.ContainsKey("server") &&
                   connectionOptions.ContainsKey("database") &&
                   (connectionOptions.ContainsKey("uid") || connectionOptions.ContainsKey("user") || connectionOptions.ContainsKey("user id")) &&
                   (connectionOptions.ContainsKey("pwd") || connectionOptions.ContainsKey("password")))))
            {
                throw new ArgumentException("Incomplete connection details. Must be in the format of 'name=connectionString' or 'server=serverName;database=databaseName;user=userName;password=password'.");
            }

            if ((connectionOptions.ContainsKey("name") 
                    && connectionOptions.Keys.Count > 1) 
                ||
                !(connectionOptions.ContainsKey("server") 
                    && connectionOptions.ContainsKey("database") 
                    && (connectionOptions.ContainsKey("uid") || connectionOptions.ContainsKey("user") || connectionOptions.ContainsKey("user id")) 
                    && (connectionOptions.ContainsKey("pwd") || connectionOptions.ContainsKey("password"))
                   //&& connectionOptions.Keys.Count > 4
                ))
            {
                throw new ArgumentException("Invalid connection details. Must be in the format of 'name=connectionString' or 'server=serverName;database=databaseName;user=userName;password=password'.");
            }

            if (connectionOptions.TryGetValue("name", out string? nameValue))
            {
                SetNamedConnection(nameValue);

                var connectionBuilder = RelmHelper.GetConnectionBuilderFromName(nameValue);
                var connectionString = connectionBuilder?.ConnectionString 
                    ?? throw new ArgumentNullException(nameof(MySqlConnectionStringBuilder.ConnectionString));

                SetDatabaseConnectionString(connectionString);
            }
            else
            {
                if (connectionOptions.TryGetValue("server", out string? serverValue))
                    SetDatabaseServer(serverValue);

                if (connectionOptions.TryGetValue("port", out string? portValue))
                    SetDatabasePort(portValue);

                if (connectionOptions.TryGetValue("database", out string? databaseValue))
                    SetDatabaseName(databaseValue);

                if (connectionOptions.TryGetValue("uid", out string? uidValue))
                    SetDatabaseUser(uidValue);
                else if (connectionOptions.TryGetValue("user", out string? userValue))
                    SetDatabaseUser(userValue);
                else if (connectionOptions.TryGetValue("user id", out string? userIdValue))
                    SetDatabaseUser(userIdValue);

                if (connectionOptions.TryGetValue("pwd", out string? pwdValue))
                    SetDatabasePassword(pwdValue);
                else if (connectionOptions.TryGetValue("password", out string? passwordValue))
                    SetDatabasePassword(passwordValue);

                DatabaseConnectionString = $"server={DatabaseServer};port={DatabasePort};database={DatabaseName};user id={DatabaseUser};password={DatabasePassword}";
            }
        }

        /// <summary>
        /// Enables or disables automatic opening of the connection when required by operations.
        /// </summary>
        /// <remarks>When automatic connection opening is enabled, the connection will be opened as needed
        /// by operations that require it. If disabled, the caller is responsible for ensuring the connection is open
        /// before performing such operations.</remarks>
        /// <param name="autoOpenConnection">true to enable automatic opening of the connection; false to require manual connection management.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        public RelmContextOptionsBuilder SetAutoOpenConnection(bool autoOpenConnection)
        {
            this.AutoOpenConnection = autoOpenConnection;

            return this;
        }

        /// <summary>
        /// Enables or disables automatic opening of a transaction when executing database operations.
        /// </summary>
        /// <param name="autoOpenTransaction">true to automatically open a transaction for each operation; otherwise, false.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        public RelmContextOptionsBuilder SetAutoOpenTransaction(bool autoOpenTransaction)
        {
            this.AutoOpenTransaction = autoOpenTransaction;

            return this;
        }

        /// <summary>
        /// Enables or disables the use of user-defined variables in SQL statements.
        /// </summary>
        /// <param name="allowUserVariables">true to allow user-defined variables in SQL statements; otherwise, false.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        public RelmContextOptionsBuilder SetAllowUserVariables(bool allowUserVariables)
        {
            this.AllowUserVariables = allowUserVariables;

            return this;
        }

        /// <summary>
        /// Specifies whether zero date values should be converted to DateTime.MinValue when retrieving data.
        /// </summary>
        /// <remarks>Zero date values are commonly used in some databases to represent an undefined or
        /// missing date. Enabling this option allows such values to be mapped to DateTime.MinValue in .NET, which may
        /// simplify handling of missing dates in application code.</remarks>
        /// <param name="convertZeroDateTime">true to convert zero date values to DateTime.MinValue; otherwise, false.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        public RelmContextOptionsBuilder SetConvertZeroDateTime(bool convertZeroDateTime)
        {
            this.ConvertZeroDateTime = convertZeroDateTime;

            return this;
        }

        /// <summary>
        /// Sets the lock wait timeout period, in seconds, for acquiring a lock.
        /// </summary>
        /// <remarks>If the timeout is set to zero, the method will not wait and will attempt to acquire
        /// the lock immediately. Setting an appropriate timeout can help prevent deadlocks in concurrent
        /// scenarios.</remarks>
        /// <param name="lockWaitTimeoutSeconds">The maximum number of seconds to wait for a lock to be acquired. Must be a non-negative value.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        public RelmContextOptionsBuilder SetLockWaitTimeoutSeconds(int lockWaitTimeoutSeconds)
        {
            this.LockWaitTimeoutSeconds = lockWaitTimeoutSeconds;

            return this;
        }

        /// <summary>
        /// Enables or disables automatic initialization of data sets.
        /// </summary>
        /// <param name="autoInitializeDataSets">true to enable automatic initialization of data sets; otherwise, false.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        public RelmContextOptionsBuilder SetAutoInitializeDataSets(bool autoInitializeDataSets)
        {
            this.AutoInitializeDataSets = autoInitializeDataSets;

            return this;
        }

        /// <summary>
        /// Enables or disables automatic verification of tables before performing operations.
        /// </summary>
        /// <param name="autoVerifyTables">true to enable automatic table verification; false to disable it.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        public RelmContextOptionsBuilder SetAutoVerifyTables(bool autoVerifyTables)
        {
            this.AutoVerifyTables = autoVerifyTables;

            return this;
        }
    }
}
