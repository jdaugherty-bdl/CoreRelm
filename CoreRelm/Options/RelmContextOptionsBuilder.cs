using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.RelmInternal.Helpers.Operations;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static CoreRelm.Options.RelmContextOptions;

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
        private string? _databaseServer;
        private string? _databasePort;
        private string? _databaseName;
        private string? _databaseUser;
        private string? _databasePassword;
        private string? _databaseConnectionString;
        private string? _namedConnection;
        private MySqlConnection? _databaseConnection;
        private MySqlTransaction? _databaseTransaction;
        private bool _autoOpenConnection = true;
        private bool _autoOpenTransaction = false;
        private bool _allowUserVariables = false;
        private bool _convertZeroDateTime = true;
        private int _lockWaitTimeoutSeconds = 0;
        private bool _autoInitializeDataSets = true;
        private bool _autoVerifyTables = true;
        private OptionsBuilderTypes _optionsBuilderType = OptionsBuilderTypes.None;
        private Enum? _connectionStringType;
        private bool _useInternalTransaction = true;

        public OptionsBuilderTypes OptionsBuilderType => _optionsBuilderType;

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
        public RelmContextOptionsBuilder(string databaseServer, string databaseName, string databaseUser, string databasePassword)
        {
            SetDatabaseServer(databaseServer);
            SetDatabaseName(databaseName);
            SetDatabaseUser(databaseUser);
            SetDatabasePassword(databasePassword);
        }

        /// <summary>
        /// Initializes a new instance of the RelmContextOptionsBuilder class with the specified database connection
        /// settings.
        /// </summary>
        /// <param name="databaseServer">The name or network address of the database server to connect to. Cannot be null or empty.</param>
        /// <param name="databasePort">The port number to use for the database connection. Cannot be null or empty.</param>
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

            _useInternalTransaction = transaction == null;

            SetDatabaseTransaction(transaction);
        }

        public RelmContextOptionsBuilder(IRelmContext? relmContext) : this(relmContext?.ContextOptions)
        {
            ArgumentNullException.ThrowIfNull(relmContext, nameof(relmContext));

            if (relmContext.ContextOptions == null)
                throw new ArgumentException("The provided IRelmContext does not have options configured.", nameof(relmContext));
        }

        public RelmContextOptionsBuilder(RelmContextOptions? contextOptions)
        {
            ArgumentNullException.ThrowIfNull(contextOptions, nameof(contextOptions));

            _databaseConnection = contextOptions.DatabaseConnection;
            _databaseTransaction = contextOptions.DatabaseTransaction;
            _databaseServer = contextOptions.DatabaseServer;
            _databasePort = contextOptions.DatabasePort;
            _databaseName = contextOptions.DatabaseName;
            _databaseUser = contextOptions.DatabaseUser;
            _databasePassword = contextOptions.DatabasePassword;
            _namedConnection = contextOptions.NamedConnection;
            _databaseConnectionString = contextOptions.DatabaseConnectionString;
            _autoOpenConnection = contextOptions.AutoOpenConnection;
            _autoOpenTransaction = contextOptions.AutoOpenTransaction;
            _allowUserVariables = contextOptions.AllowUserVariables;
            _convertZeroDateTime = contextOptions.ConvertZeroDateTime;
            _lockWaitTimeoutSeconds = contextOptions.LockWaitTimeoutSeconds;
            _autoInitializeDataSets = contextOptions.AutoInitializeDataSets;
            _autoVerifyTables = contextOptions.AutoVerifyTables;
            _optionsBuilderType = contextOptions.OptionsBuilderType;
            _connectionStringType = contextOptions.ConnectionStringType;
        }

        public RelmContextOptions BuildOptions()
        {
            return BuildOptions(validateSettings: true);
        }

        internal RelmContextOptions BuildOptions(bool validateSettings = true)
        {
            if (validateSettings && !ValidateAllSettings())
                throw new InvalidOperationException("Invalid configuration. Ensure all required settings are provided based on the selected connection method.");

            var newOptions = new RelmContextOptions
            {
                DatabaseConnection = _databaseConnection,
                DatabaseTransaction = _databaseTransaction,
                DatabaseServer = _databaseServer,
                DatabasePort = _databasePort,
                DatabaseName = _databaseName,
                DatabaseUser = _databaseUser,
                DatabasePassword = _databasePassword,
                NamedConnection = _namedConnection,
                DatabaseConnectionString = _databaseConnectionString,
                AutoOpenConnection = _autoOpenConnection,
                AutoOpenTransaction = _autoOpenTransaction,
                AllowUserVariables = _allowUserVariables,
                ConvertZeroDateTime = _convertZeroDateTime,
                LockWaitTimeoutSeconds = _lockWaitTimeoutSeconds,
                AutoInitializeDataSets = _autoInitializeDataSets,
                AutoVerifyTables = _autoVerifyTables,
            };

            // this is not normally allowed to be set outside of the RelmContextOptions class so we make it internal and only set it here in the builder
            newOptions.SetOptionsBuilderType(_optionsBuilderType);
            newOptions.SetConnectionStringType(_connectionStringType);

            return newOptions;
        }

        public T? Build<T>() where T : IRelmContext
        {
            var options = this.BuildOptions();

            return (T?)Activator.CreateInstance(typeof(T), [options]) 
                ?? throw new InvalidOperationException($"Failed to create an instance of type '{typeof(T).FullName}'. Ensure that the type has a constructor that accepts a RelmContextOptionsBuilder parameter.");
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
            ArgumentNullException.ThrowIfNull(connection, nameof(connection));

            _databaseConnection = connection;

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
            if (transaction != null && _databaseConnection == null)
                throw new InvalidOperationException("Cannot set a transaction without an associated database connection. Set the database connection before setting a transaction.");

            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            if (transaction != null && !_useInternalTransaction)
                throw new InvalidOperationException("Cannot set a transaction when using an open connection with external transaction management. Ensure that the connection and transaction settings are compatible.");

            _databaseTransaction = transaction;

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

            _databaseServer = databaseServer;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionDetails;

            return this;
        }

        public RelmContextOptionsBuilder SetDatabasePort(string databasePort)
        {
            if (string.IsNullOrEmpty(databasePort))
                throw new ArgumentNullException(nameof(databasePort), "Database port cannot be null or empty.");

            // convert to int and validate that it's a valid port number (1-65535)
            if (!int.TryParse(databasePort, out int port) || port <= 0 || port > 65535)
                throw new ArgumentException("Invalid database port. Must be a valid integer between 1 and 65535.", nameof(databasePort));

            // store the parsed port number as this is the most likely valid number
            _databasePort = port.ToString();

            _optionsBuilderType = OptionsBuilderTypes.ConnectionDetails;

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

            _databaseName = databaseName;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionDetails;

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

            _databaseUser = databaseUser;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionDetails;

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

            _databasePassword = databasePassword;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionDetails;

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

            _namedConnection = connectionStringType.ToString();

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

            _namedConnection = namedConnection;

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
        public RelmContextOptionsBuilder SetDatabaseConnectionString(string? DatabaseConnectionString)
        {
            if (string.IsNullOrEmpty(DatabaseConnectionString))
                throw new ArgumentNullException(nameof(DatabaseConnectionString));

            _databaseConnectionString = DatabaseConnectionString;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;

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
            if (_optionsBuilderType == OptionsBuilderTypes.None)
                return true;
            else if (_optionsBuilderType == OptionsBuilderTypes.NamedConnectionString)
            {
                if (string.IsNullOrEmpty(_namedConnection))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException(nameof(_namedConnection), "NamedConnection cannot be null or empty when using a named connection string.");
                    else
                        return false;
                }

                return true;
            }
            else if (_optionsBuilderType == OptionsBuilderTypes.OpenConnection)
            {
                if (_databaseConnection == null)
                {
                    if (throwExceptions)
                        throw new ArgumentNullException(nameof(_databaseConnection), "Database connection cannot be null.");
                    else
                        return false;
                }

                return true;
            }
            else if (_optionsBuilderType == OptionsBuilderTypes.ConnectionString)
            {
                if (string.IsNullOrEmpty(_databaseConnectionString))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException(nameof(_databaseConnectionString), "Database connection string cannot be null or empty when using a connection string.");
                    else
                        return false;
                }

                return true;
            }
            else if (_optionsBuilderType == OptionsBuilderTypes.ConnectionDetails)
            {
                if (string.IsNullOrEmpty(_databaseServer))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException(nameof(_databaseServer), "Database Server cannot be null or empty when using connection details.");
                    else
                        return false;
                }

                if (string.IsNullOrEmpty(_databaseName))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException(nameof(_databaseName), "Database Name cannot be null or empty when using connection details.");
                    else
                        return false;
                }

                if (string.IsNullOrEmpty(_databaseUser))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException(nameof(_databaseUser), "Username cannot be null or empty when using connection details.");
                    else
                        return false;
                }

                if (string.IsNullOrEmpty(_databasePassword))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException(nameof(_databasePassword), "Password cannot be null or empty when using connection details.");
                    else
                        return false;
                }

                return true;
            }
            else
            {
                throw new ArgumentException($"Invalid connection string type '{_connectionStringType}'.");
            }
        }

        /// <summary>
        /// Parses the specified connection details string and configures the database connection settings accordingly.
        /// </summary>
        /// <remarks>The method supports two formats for specifying connection details: a named connection
        /// (e.g., 'name=MyConnection') or explicit connection parameters (e.g.,
        /// 'server=localhost;database=MyDb;user=admin;password=secret'). Only one format may be used at a time.
        /// Additional or missing parameters will result in an exception.</remarks>
        /// <param name="connectionDetails">A string containing the connection details. Must be in the format 'name=connectionName' to use a named
        /// connection, or 'server=serverName;database=databaseName;user=userName;password=password' to specify
        /// individual connection parameters.</param>
        /// <exception cref="ArgumentNullException">Thrown if the connectionDetails parameter is null, empty, or consists only of white-space characters.</exception>
        /// <exception cref="ArgumentException">Thrown if the connectionDetails parameter does not match the required format or is missing required
        /// connection parameters.</exception>
        public void ParseConnectionDetails()
        {
            ParseConnectionDetails(_databaseConnectionString);
        }

        public void ParseConnectionDetails(string? connectionDetails)
        {
            if (string.IsNullOrWhiteSpace(connectionDetails))
                throw new ArgumentNullException(nameof(connectionDetails));

            if (!connectionDetails.Contains('='))
                throw new ArgumentException("Invalid connection details. Must be in the format of 'name=connectionName' or 'server=serverName;database=databaseName;user=userName;password=password'.");

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
                throw new ArgumentException("Incomplete connection details. Must be in the format of 'name=connectionName' or 'server=serverName;database=databaseName;user=userName;password=password'.");
            }

            if (connectionOptions.ContainsKey("name") 
                    && connectionOptions.Keys.Count > 1) 
            {
                throw new ArgumentException("Invalid connection details. Must be in the format of 'name=connectionName' or 'server=serverName;database=databaseName;user=userName;password=password'.");
            }
               
            if(!connectionOptions.ContainsKey("name")
                && !(connectionOptions.ContainsKey("server") 
                    && connectionOptions.ContainsKey("database") 
                    && (connectionOptions.ContainsKey("uid") || connectionOptions.ContainsKey("user") || connectionOptions.ContainsKey("user id")) 
                    && (connectionOptions.ContainsKey("pwd") || connectionOptions.ContainsKey("password"))
                    //&& connectionOptions.Keys.Count > 4
            ))
            {
                throw new ArgumentException("Invalid connection details. Must be in the format of 'name=connectionName' or 'server=serverName;database=databaseName;user=userName;password=password'.");
            }

            if (connectionOptions.TryGetValue("name", out string? nameValue))
            {
                var connectionBuilder = RelmHelper.GetConnectionBuilderFromName(nameValue);
                var connectionString = connectionBuilder?.ConnectionString 
                    ?? throw new ArgumentNullException(nameof(MySqlConnectionStringBuilder.ConnectionString));

                SetDatabaseConnectionString(connectionString);
                SetNamedConnection(nameValue);
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

                SetDatabaseConnectionString($"server={_databaseServer}{(string.IsNullOrWhiteSpace(_databasePort) ? null : $";port={_databasePort}")};database={_databaseName};user id={_databaseUser};password={_databasePassword}");
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
            if (_optionsBuilderType == OptionsBuilderTypes.OpenConnection && autoOpenConnection)
                throw new InvalidOperationException("Cannot enable automatic connection opening when using an open connection. The connection must be managed manually.");

            _autoOpenConnection = autoOpenConnection;

            return this;
        }

        /// <summary>
        /// Enables or disables automatic opening of a transaction when executing database operations.
        /// </summary>
        /// <param name="autoOpenTransaction">true to automatically open a transaction for each operation; otherwise, false.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        public RelmContextOptionsBuilder SetAutoOpenTransaction(bool autoOpenTransaction)
        {
            _autoOpenTransaction = autoOpenTransaction;

            return this;
        }

        /// <summary>
        /// Enables or disables the use of user-defined variables in SQL statements.
        /// </summary>
        /// <param name="allowUserVariables">true to allow user-defined variables in SQL statements; otherwise, false.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        public RelmContextOptionsBuilder SetAllowUserVariables(bool allowUserVariables)
        {
            _allowUserVariables = allowUserVariables;

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
            _convertZeroDateTime = convertZeroDateTime;

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
            _lockWaitTimeoutSeconds = lockWaitTimeoutSeconds;

            return this;
        }

        /// <summary>
        /// Enables or disables automatic initialization of data sets.
        /// </summary>
        /// <param name="autoInitializeDataSets">true to enable automatic initialization of data sets; otherwise, false.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        public RelmContextOptionsBuilder SetAutoInitializeDataSets(bool autoInitializeDataSets)
        {
            _autoInitializeDataSets = autoInitializeDataSets;

            return this;
        }

        /// <summary>
        /// Enables or disables automatic verification of tables before performing operations.
        /// </summary>
        /// <param name="autoVerifyTables">true to enable automatic table verification; false to disable it.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        public RelmContextOptionsBuilder SetAutoVerifyTables(bool autoVerifyTables)
        {
            _autoVerifyTables = autoVerifyTables;

            return this;
        }
    }
}
