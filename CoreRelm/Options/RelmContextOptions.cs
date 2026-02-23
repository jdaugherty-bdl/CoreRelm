using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Options
{
    public class RelmContextOptions
    {
        internal RelmContextOptions() { }

        /// <summary>
        /// Specifies the types of options that can be used to configure a database context using an options builder.
        /// </summary>
        /// <remarks>Use this enumeration to indicate how the options builder should obtain or interpret
        /// connection information when configuring a database context. The selected value determines whether a raw
        /// connection string, a named connection string, or an existing open connection is used.</remarks>
        public enum OptionsBuilderTypes
        {
            /// <summary>
            /// Specifies that no options are set.
            /// </summary>
            None,
            /// <summary>
            /// Sets the option builder connection type to use a raw connection string.
            /// </summary>
            ConnectionString,
            /// <summary>
            /// Sets the option builder connection type to use a connection, including server address, port, and
            /// authentication credentials.
            /// </summary>
            ConnectionDetails,
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
        public string? DatabaseServer { get; init; }

        /// <summary>
        /// Gets the port number used to connect to the database as a string value.
        /// </summary>
        /// <remarks>The port value may be null if not specified. The format should be a valid port number
        /// as a string, such as "5432" for PostgreSQL or "1433" for SQL Server.</remarks>
        public string? DatabasePort { get; init; }

        /// <summary>
        /// Gets the name of the database associated with this instance.
        /// </summary>
        public string? DatabaseName { get; init; }

        /// <summary>
        /// Gets the user name used to connect to the database.
        /// </summary>
        public string? DatabaseUser { get; init; }

        /// <summary>
        /// Gets the password used to connect to the database.
        /// </summary>
        public string? DatabasePassword { get; init; }

        /// <summary>
        /// Gets the connection string used to connect to the database.
        /// </summary>
        public string? DatabaseConnectionString { get; init; }

        /// <summary>
        /// Gets or sets the name of the connection to use for database operations.
        /// </summary>
        public string? NamedConnection { get; set; }

        /// <summary>
        /// Gets the active MySQL database connection used by the application.
        /// </summary>
        private MySqlConnection? _databaseConnection;
        public MySqlConnection? DatabaseConnection { 
            get
            {
                return _databaseConnection;
            }
            init
            {
                _databaseConnection = value;
            }
        }

        /// <summary>
        /// Gets the current database transaction associated with the connection.
        /// </summary>
        /// <remarks>Use this property to access the active MySQL transaction for executing commands
        /// within a transactional context. The property is null if no transaction is in progress.</remarks>
        private MySqlTransaction? _databaseTransaction;
        public MySqlTransaction? DatabaseTransaction
        {
            get
            {
                   return _databaseTransaction;
            }
            init
            {
                _databaseTransaction = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the connection should be automatically opened when required.
        /// </summary>
        public bool AutoOpenConnection { get; init; } = true;

        /// <summary>
        /// Gets a value indicating whether a transaction is automatically opened when a database connection is
        /// established.
        /// </summary>
        public bool AutoOpenTransaction { get; init; } = false;

        /// <summary>
        /// Gets a value indicating whether user-defined variables are permitted in SQL statements.
        /// </summary>
        public bool AllowUserVariables { get; init; } = false;

        /// <summary>
        /// Gets a value indicating whether date and time values of zero are converted to DateTime.MinValue when
        /// retrieved from the database.
        /// </summary>
        /// <remarks>When enabled, date and time fields with a value of '0000-00-00' or '0000-00-00
        /// 00:00:00' are returned as DateTime.MinValue instead of causing an exception or being treated as invalid.
        /// This option is useful when working with databases that allow zero date values.</remarks>
        public bool ConvertZeroDateTime { get; init; } = false;

        /// <summary>
        /// Gets the maximum number of seconds to wait for a lock to be acquired before timing out.
        /// </summary>
        public int LockWaitTimeoutSeconds { get; init; } = 30;

        /// <summary>
        /// Gets a value indicating whether data sets are automatically initialized when the component is created.
        /// </summary>
        public bool AutoInitializeDataSets { get; init; } = true;

        /// <summary>
        /// Gets a value indicating whether table verification is performed automatically before database operations.
        /// </summary>
        public bool AutoVerifyTables { get; init; } = true;

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
        /// Sets the database connection to be used by the current instance.
        /// </summary>
        /// <remarks>After calling this method, the instance will use the provided connection for all
        /// subsequent database operations. The caller is responsible for managing the lifetime of the
        /// connection.</remarks>
        /// <param name="connection">The open <see cref="MySqlConnection"/> to associate with this instance. The connection must not be null.</param>
        /// <returns>The current instance of the <see cref="RelmContextOptionsBuilder"/> class.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="connection"/> is null.</exception>
        public RelmContextOptions SetDatabaseConnection(MySqlConnection? connection)
        {
            _databaseConnection = connection ?? throw new ArgumentNullException(nameof(connection), "Connection cannot be null.");

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
        public RelmContextOptions SetDatabaseTransaction(MySqlTransaction? transaction)
        {
            _databaseTransaction = transaction;

            _optionsBuilderType = OptionsBuilderTypes.OpenConnection;

            return this;
        }

        internal RelmContextOptions SetOptionsBuilderType(OptionsBuilderTypes optionsBuilderType)
        {
            _optionsBuilderType = optionsBuilderType;

            return this;
        }

        internal RelmContextOptions SetConnectionStringType(Enum connectionStringType)
        {
            _connectionStringType = connectionStringType;

            return this;
        }
    }
}
