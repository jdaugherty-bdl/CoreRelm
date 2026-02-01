using CoreRelm.Attributes;
using CoreRelm.Extensions;
using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.Interfaces.Resolvers;
using CoreRelm.Models;
using CoreRelm.Options;
using CoreRelm.Persistence;
using CoreRelm.RelmInternal.Helpers.Connections;
using CoreRelm.RelmInternal.Helpers.DataTransfer;
using CoreRelm.RelmInternal.Helpers.DataTransfer.Persistence;
using CoreRelm.RelmInternal.Helpers.Operations;
using CoreRelm.RelmInternal.Helpers.Utilities;
using CoreRelm.RelmInternal.Resolvers;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm
{
    /// <summary>
    /// Provides a collection of helper methods and properties for interacting with relational databases.
    /// </summary>
    /// <remarks>The <see cref="RelmHelper"/> class offers a variety of static methods and properties to
    /// simplify database operations,  including connection management, query execution, and data retrieval. It also
    /// provides utilities for handling  execution errors, managing database contexts, and working with relational
    /// models.  This class is designed to abstract common database operations, making it easier to interact with MySQL
    /// databases  and manage data access layers in a consistent and reusable manner.</remarks>
    public class RelmHelper
    {
        /// <summary>
        /// Caches the last execution error encountered
        /// </summary>
        public static Exception LastExecutionException
            => DatabaseWorkHelper.LastExecutionException;

        /// <summary>
        /// Convenience function to get the last exception message
        /// </summary>
        public static string LastExecutionError
            => DatabaseWorkHelper.LastExecutionError;

        /// <summary>
        /// Convenience function to check if there's an error cached.
        /// </summary>
        public static bool HasError
            => DatabaseWorkHelper.HasError;

        /// <summary>
        /// Gets or sets the current context for the application.
        /// </summary>
        /// <remarks>This property holds the active context used throughout the application.  Ensure that
        /// the context is properly initialized before accessing this property.</remarks>
        public static IRelmContext? CurrentContext { get; set; }

        /// <summary>
        /// Gets the root directory where logging files are stored.
        /// </summary>
        public static string RootLoggingDirectory
        {
            get
            {
                /*
                return ConfigurationManager.AppSettings.AllKeys.Contains("SimpleRelm_LoggingDir")
                    ? ConfigurationManager.AppSettings["SimpleRelm_LoggingDir"]
                    : Path.GetDirectoryName((new Uri(AssemblyHelper.GetEntryAssembly().Location)).AbsolutePath); // Assembly.GetExecutingAssembly()
                */
                return _configuration != null && _configuration.GetSection("SimpleRelm_LoggingDir").Exists()
                    ? _configuration["SimpleRelm_LoggingDir"]!
                    : Path.GetDirectoryName((new Uri(AssemblyHelper.GetEntryAssembly().Location)).AbsolutePath)!; // Assembly.GetExecutingAssembly()
            }
        }

        internal static ConnectionHelper? ConnectionHelper { get; private set; }

        // holds the application's configuration, if provided
        private static IConfiguration? _configuration;

        static RelmHelper()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            ConnectionHelper = new ConnectionHelper(_configuration);
        }

        //***************** Connections *****************//

        /// <summary>
        /// Retrieves a <see cref="MySqlConnectionStringBuilder"/> instance configured for the specified connection
        /// type.
        /// </summary>
        /// <remarks>The method delegates the creation of the connection string builder to the <see
        /// cref="ConnectionHelper.GetConnectionBuilderFromType(Enum)"/> method. Ensure that the provided <paramref
        /// name="connectionName"/> corresponds to a supported connection type.</remarks>
        /// <param name="connectionName">An enumeration value representing the type of connection to configure.  This parameter determines the
        /// settings applied to the returned connection string builder.</param>
        /// <returns>A <see cref="MySqlConnectionStringBuilder"/> instance configured based on the specified <paramref
        /// name="connectionName"/>.</returns>
        public static MySqlConnectionStringBuilder? GetConnectionBuilderFromConnectionType(Enum connectionName)
            => ConnectionHelper?.GetConnectionBuilderFromType(connectionName);

        /// <summary>
        /// Retrieves a <see cref="MySqlConnectionStringBuilder"/> instance based on the specified connection name.
        /// </summary>
        /// <param name="connectionName">The name of the connection to retrieve. This value must correspond to a valid connection name
        /// in the application configuration.</param>
        /// <returns>A <see cref="MySqlConnectionStringBuilder"/> initialized with the connection string associated with the
        /// specified connection name.</returns>
        public static MySqlConnectionStringBuilder? GetConnectionBuilderFromName(string connectionName)
            => ConnectionHelper?.GetConnectionBuilderFromName(connectionName);

        /// <summary>
        /// Creates and returns a <see cref="MySqlConnectionStringBuilder"/> initialized with the specified connection
        /// string.
        /// </summary>
        /// <param name="connectionString">The connection string used to configure the <see cref="MySqlConnectionStringBuilder"/>.</param>
        /// <returns>A <see cref="MySqlConnectionStringBuilder"/> instance populated with the settings from the provided
        /// connection string.</returns>
        public static MySqlConnectionStringBuilder? GetConnectionBuilderFromConnectionString(string connectionString)
            => ConnectionHelper?.GetConnectionBuilderFromConnectionString(connectionString);

        /// <summary>
        /// Creates and returns a <see cref="MySqlConnection"/> based on the specified connection type and optional
        /// configuration settings.
        /// </summary>
        /// <remarks>This method simplifies the creation of MySQL connections. Ensure that
        /// the <paramref name="connectionName"/> corresponds to a valid configuration in the application's connection
        /// settings.</remarks>
        /// <param name="connectionName">An <see cref="Enum"/> representing the type of connection to establish. The specific values and their
        /// meanings depend on the application's connection configuration.</param>
        /// <param name="allowUserVariables">A boolean value indicating whether user-defined variables are allowed in SQL statements.  <see
        /// langword="true"/> to allow user variables; otherwise, <see langword="false"/>. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="convertZeroDateTime">A boolean value indicating whether zero date values (e.g., '0000-00-00') should be converted to <see
        /// langword="DateTime.MinValue"/>.  <see langword="true"/> to enable conversion; otherwise, <see
        /// langword="false"/>. Defaults to <see langword="false"/>.</param>
        /// <param name="lockWaitTimeoutSeconds">An integer specifying the lock wait timeout duration, in seconds, for the connection.  A value of 0
        /// indicates that the default timeout will be used. Defaults to 0.</param>
        /// <returns>A <see cref="MySqlConnection"/> object configured based on the specified parameters.</returns>
        public static MySqlConnection? GetConnectionFromType(Enum connectionName, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
            => ConnectionHelper?.GetConnectionFromType(connectionName, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);

        /// <summary>
        /// Retrieves a <see cref="MySqlConnection"/> instance based on the specified connection name.
        /// </summary>
        /// <param name="connectionName">The name of the connection to retrieve. This must correspond to a valid connection string defined in the
        /// application's configuration.</param>
        /// <param name="allowUserVariables">A value indicating whether user-defined variables are allowed in SQL queries. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="convertZeroDateTime">A value indicating whether zero date values (e.g., '0000-00-00') in the database should be converted to <see
        /// cref="DateTime.MinValue"/>. Defaults to <see langword="false"/>.</param>
        /// <param name="lockWaitTimeoutSeconds">The lock wait timeout, in seconds, to apply to the connection. A value of <c>0</c> indicates that the
        /// default timeout will be used.</param>
        /// <returns>A <see cref="MySqlConnection"/> instance configured with the specified options.</returns>
        public static MySqlConnection? GetConnectionFromName(string connectionName, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
            => ConnectionHelper?.GetConnectionFromName(connectionName, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);

        /// <summary>
        /// Creates and returns a new <see cref="MySqlConnection"/> instance based on the specified connection string
        /// and optional configuration parameters.
        /// </summary>
        /// <remarks>This method provides a convenient way to create a <see cref="MySqlConnection"/> with
        /// additional configuration options such as enabling user-defined variables, converting zero date values, or
        /// setting a custom lock wait timeout. Ensure that the connection string is valid and properly formatted for
        /// MySQL.</remarks>
        /// <param name="connectionString">The connection string used to establish the database connection. This parameter cannot be <see
        /// langword="null"/> or empty.</param>
        /// <param name="allowUserVariables">A value indicating whether user-defined variables are allowed in SQL statements. The default is <see
        /// langword="false"/>.</param>
        /// <param name="convertZeroDateTime">A value indicating whether zero date values (e.g., '0000-00-00') in the database should be converted to <see
        /// cref="DateTime.MinValue"/>. The default is <see langword="false"/>.</param>
        /// <param name="lockWaitTimeoutSeconds">The lock wait timeout duration, in seconds, to apply to the connection. A value of <c>0</c> indicates that
        /// the default timeout will be used.</param>
        /// <returns>A <see cref="MySqlConnection"/> instance configured with the specified connection string and options.</returns>
        public static MySqlConnection? GetConnectionFromConnectionString(string connectionString, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
            => ConnectionHelper?.GetConnectionFromConnectionString(connectionString, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);

        //***************** Identity functions *****************//

        /// <summary>
        /// Retrieves the identifier of the last inserted row in the database for the specified connection.
        /// </summary>
        /// <remarks>This method relies on the database's LAST_INSERT_ID() function, which typically
        /// returns the most recent auto-increment value generated during the current session.  Ensure that the database
        /// supports this function and that the session context is consistent with the operation that generated the
        /// ID.</remarks>
        /// <param name="connectionName">An enumeration value representing the configuration connection string to use for the database query.</param>
        /// <returns>A string containing the identifier of the last inserted row. The value is determined by the database's
        /// LAST_INSERT_ID() function.</returns>
        public static string GetLastInsertId(Enum connectionName)
            => RowIdentityHelper.GetLastInsertId(connectionName);

        /// <summary>
        /// Retrieves the identifier of the last inserted row in the current database session.
        /// </summary>
        /// <remarks>This method relies on the database's LAST_INSERT_ID() function, which typically
        /// returns the most recent auto-increment value generated during the current session.  Ensure that the database
        /// supports this function and that the session context is consistent with the operation that generated the
        /// ID.</remarks>
        /// <param name="relmContext">The database context used to execute the query. This context must be properly initialized and connected to
        /// the database.</param>
        /// <returns>A string containing the identifier of the last inserted row. The value is determined by the database's
        /// LAST_INSERT_ID() function.</returns>
        public static string GetLastInsertId(IRelmContext relmContext)
            => RowIdentityHelper.GetLastInsertId(relmContext);

        /// <summary>
        /// Retrieves the identifier of the last inserted row in the current database session.
        /// </summary>
        /// <remarks>This method relies on the database's LAST_INSERT_ID() function, which typically
        /// returns the most recent auto-increment value generated during the current session.  Ensure that the database
        /// supports this function and that the session context is consistent with the operation that generated the
        /// ID.</remarks>
        /// <param name="relmQuickContext">The database quick context used to execute the query. This context must be properly initialized and connected to
        /// the database.</param>
        /// <returns>A string representing the identifier of the last inserted row. The value is determined by the database's
        /// LAST_INSERT_ID() function.</returns>
        public static string GetLastInsertId(IRelmQuickContext relmQuickContext)
            => RowIdentityHelper.GetLastInsertId(relmQuickContext);

        /// <summary>
        /// Retrieves the ID associated with the specified internal ID from the given table.
        /// </summary>
        /// <remarks>This method executes a SQL query to retrieve the ID corresponding to the
        /// provided internal ID. Ensure that the <paramref name="connectionName"/> corresponds to a valid database
        /// connection and that the <paramref name="tableName"/> exists in the database schema.</remarks>
        /// <param name="connectionName">The database connection identifier, represented as an enumeration value.</param>
        /// <param name="tableName">The name of the database table to query. Must not be null or empty.</param>
        /// <param name="InternalId">The internal ID to search for. Must not be null or empty.</param>
        /// <returns>The ID as a string if a matching record is found; otherwise, <see langword="null"/>.</returns>
        public static string GetIdFromInternalId(Enum connectionName, string tableName, string InternalId)
            => RowIdentityHelper.GetIdFromInternalId(connectionName, tableName, InternalId);

        /// <summary>
        /// Retrieves the ID associated with the specified internal ID from the given table.
        /// </summary>
        /// <remarks>This method executes a SQL query to retrieve the ID corresponding to the
        /// provided internal ID. Ensure that the table specified by <paramref name="tableName"/> contains columns named
        /// "ID" and "InternalId".</remarks>
        /// <param name="relmContext">The database context used to execute the query.</param>
        /// <param name="tableName">The name of the table to query. Must not be null or empty.</param>
        /// <param name="InternalId">The internal ID to search for. Must not be null or empty.</param>
        /// <returns>The ID as a string if a matching record is found; otherwise, <see langword="null"/>.</returns>
        public static string GetIdFromInternalId(IRelmContext relmContext, string tableName, string InternalId)
            => RowIdentityHelper.GetIdFromInternalId(relmContext, tableName, InternalId);

        /// <summary>
        /// Retrieves the ID associated with the specified internal ID from the given table.
        /// </summary>
        /// <remarks>This method executes a SQL query to retrieve the external ID from the specified
        /// table. Ensure that the table contains a column named "InternalId" and that the query is executed in a secure
        /// and valid context.</remarks>
        /// <param name="relmQuickContext">The database quick context used to execute the query.</param>
        /// <param name="tableName">The name of the table to query. Must not be null or empty.</param>
        /// <param name="InternalId">The internal ID to search for. Must not be null or empty.</param>
        /// <returns>The ID corresponding to the specified internal ID, or <see langword="null"/> if no match is found.</returns>
        public static string GetIdFromInternalId(IRelmQuickContext relmQuickContext, string tableName, string InternalId)
            => RowIdentityHelper.GetIdFromInternalId(relmQuickContext, tableName, InternalId);

        //***************** Refined results *****************//

        /// <summary>
        /// Executes a scalar query and retrieves the first column of the first row in the result set, cast to the
        /// specified type.
        /// </summary>
        /// <remarks>This method is typically used to retrieve single values, such as counts, sums, or
        /// other aggregate results, from a database query.</remarks>
        /// <typeparam name="T">The type to which the scalar result will be cast.</typeparam>
        /// <param name="connectionName">The name of the database connection to use. Must be a valid enumeration value representing a configured
        /// connection.</param>
        /// <param name="query">The SQL query to execute. The query should return a single value.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. Can be <see langword="null"/>
        /// if no parameters are required.</param>
        /// <param name="throwException">Indicates whether an exception should be thrown if an error occurs during query execution. If <see
        /// langword="true"/>, exceptions will be propagated; otherwise, errors will be suppressed.</param>
        /// <param name="allowUserVariables">Indicates whether user-defined variables are allowed in the query. Set to <see langword="true"/> to enable
        /// user variables; otherwise, <see langword="false"/>.</param>
        /// <returns>The scalar result of the query, cast to the specified type <typeparamref name="T"/>. Returns the default
        /// value of <typeparamref name="T"/> if the query returns no result.</returns>
        public static T GetScalar<T>(Enum connectionName, string query, Dictionary<string, object> parameters = null, bool throwException = true, bool allowUserVariables = false)
            => RefinedResultsHelper.GetScalar<T>(connectionName, query, parameters: parameters, throwException: throwException, allowUserVariables: allowUserVariables);

        /// <summary>
        /// Executes a scalar query on the specified MySQL connection and returns the result as the specified type.
        /// </summary>
        /// <typeparam name="T">The type to which the scalar result will be converted.</typeparam>
        /// <param name="establishedConnection">An open <see cref="MySqlConnection"/> to execute the query on. The connection must be valid and open.</param>
        /// <param name="query">The SQL query string to execute. The query must return a single value.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. If null, no parameters are
        /// applied.</param>
        /// <param name="throwException">A boolean value indicating whether to throw an exception if an error occurs. If <see langword="true"/>,
        /// exceptions will be thrown; otherwise, errors will be suppressed.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the query. If null, the query will not be part
        /// of a transaction.</param>
        /// <returns>The scalar result of the query, converted to the specified type <typeparamref name="T"/>. Returns the
        /// default value of <typeparamref name="T"/> if the query result is null.</returns>
        public static T GetScalar<T>(MySqlConnection establishedConnection, string query, Dictionary<string, object> parameters = null, bool throwException = true, MySqlTransaction sqlTransaction = null)
            => RefinedResultsHelper.GetScalar<T>(establishedConnection, query, parameters: parameters, throwException: throwException, sqlTransaction: sqlTransaction);

        /// <summary>
        /// Executes a scalar query and retrieves the result as the specified type.
        /// </summary>
        /// <typeparam name="T">The type to which the scalar result will be converted.</typeparam>
        /// <param name="relmContext">The database context used to execute the query. Cannot be <see langword="null"/>.</param>
        /// <param name="query">The SQL query string to execute. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to include in the query. Can be <see langword="null"/>
        /// if no parameters are required.</param>
        /// <param name="throwException">Indicates whether an exception should be thrown if the query fails. If <see langword="true"/>, an exception
        /// will be thrown on failure; otherwise, the method will return the default value of <typeparamref name="T"/>.</param>
        /// <returns>The scalar result of the query converted to the specified type <typeparamref name="T"/>. Returns the default
        /// value of <typeparamref name="T"/> if the query produces no result or fails and <paramref
        /// name="throwException"/> is <see langword="false"/>.</returns>
        public static T GetScalar<T>(IRelmContext relmContext, string query, Dictionary<string, object> parameters = null, bool throwException = true)
            => RefinedResultsHelper.GetScalar<T>(relmContext, query, parameters: parameters, throwException: throwException);
        
        /// <summary>
        /// Executes a scalar query and retrieves the result as the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the result to be returned.</typeparam>
        /// <param name="relmQuickContext">The database context used to execute the query. Cannot be <see langword="null"/>.</param>
        /// <param name="query">The SQL query string to execute. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="parameters">An optional dictionary of parameters to include in the query. Keys represent parameter names, and values
        /// represent parameter values. Can be <see langword="null"/> if no parameters are needed.</param>
        /// <param name="throwException">Indicates whether an exception should be thrown if the query fails. If <see langword="true"/>, an exception
        /// will be thrown on failure; otherwise, the method will return the default value of <typeparamref name="T"/>.</param>
        /// <returns>The result of the scalar query, cast to the specified type <typeparamref name="T"/>. Returns the default
        /// value of <typeparamref name="T"/> if the query fails and <paramref name="throwException"/> is <see
        /// langword="false"/>.</returns>
        public static T GetScalar<T>(IRelmQuickContext relmQuickContext, string query, Dictionary<string, object> parameters = null, bool throwException = true)
            => RefinedResultsHelper.GetScalar<T>(relmQuickContext, query, parameters: parameters, throwException: throwException);

        /// <summary>
        /// Executes the specified query and retrieves the first row of the result set as a <see cref="DataRow"/>.
        /// </summary>
        /// <remarks>This method uses the connection associated with the specified <paramref
        /// name="connectionName"/> to execute  the query. If <paramref name="parameters"/> are provided, they are
        /// applied to the query as named parameters.</remarks>
        /// <param name="connectionName">An <see cref="Enum"/> representing the connection name to use for the database operation.</param>
        /// <param name="query">The SQL query to execute. This query must be a valid SQL statement.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. If null, no parameters are
        /// applied.</param>
        /// <param name="throwException">A boolean value indicating whether an exception should be thrown if the query fails.  <see langword="true"/>
        /// to throw an exception on failure; otherwise, <see langword="false"/>.</param>
        /// <param name="allowUserVariables">A boolean value indicating whether user-defined variables are allowed in the query.  <see langword="true"/>
        /// to allow user variables; otherwise, <see langword="false"/>.</param>
        /// <returns>A <see cref="DataRow"/> representing the first row of the result set.  Returns <see langword="null"/> if the
        /// query does not return any rows.</returns>
        public static DataRow GetDataRow(Enum connectionName, string query, Dictionary<string, object> parameters = null, bool throwException = true, bool allowUserVariables = false)
            => RefinedResultsHelper.GetDataRow(connectionName, query, parameters: parameters, throwException: throwException, allowUserVariables: allowUserVariables);

        /// <summary>
        /// Executes the specified SQL query and retrieves the first row of the result set as a <see cref="DataRow"/>.
        /// </summary>
        /// <remarks>This method is a convenience wrapper for retrieving a single row from the result set
        /// of a query. If the query returns multiple rows, only the first row is returned. If no rows are returned, the
        /// method returns <see langword="null"/>.</remarks>
        /// <param name="establishedConnection">An open <see cref="MySqlConnection"/> to the database. The connection must be in an open state before
        /// calling this method.</param>
        /// <param name="query">The SQL query to execute. The query must be a valid SQL statement that returns a result set.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. If null, no parameters are
        /// applied.</param>
        /// <param name="throwException">A boolean value indicating whether to throw an exception if the query fails.  <see langword="true"/> to
        /// throw an exception on failure; otherwise, <see langword="false"/>.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the query. If null, the query is executed
        /// without a transaction.</param>
        /// <returns>A <see cref="DataRow"/> representing the first row of the result set. Returns <see langword="null"/> if the
        /// query does not return any rows.</returns>
        public static DataRow GetDataRow(MySqlConnection establishedConnection, string query, Dictionary<string, object> parameters = null, bool throwException = true, MySqlTransaction sqlTransaction = null)
            => RefinedResultsHelper.GetDataRow(establishedConnection, query, parameters: parameters, throwException: throwException, sqlTransaction: sqlTransaction);

        /// <summary>
        /// Executes the specified query and retrieves the first row of the result set as a <see cref="DataRow"/>.
        /// </summary>
        /// <param name="relmContext">The database context used to execute the query. Cannot be <see langword="null"/>.</param>
        /// <param name="query">The SQL query to execute. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to include in the query. Can be <see langword="null"/>
        /// if no parameters are required.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the query does not return any rows. <see
        /// langword="true"/> to throw an exception when no rows are returned; otherwise, <see langword="false"/>.</param>
        /// <returns>A <see cref="DataRow"/> representing the first row of the result set. Returns <see langword="null"/> if no
        /// rows are found and <paramref name="throwException"/> is <see langword="false"/>.</returns>
        public static DataRow GetDataRow(IRelmContext relmContext, string query, Dictionary<string, object> parameters = null, bool throwException = true)
            => RefinedResultsHelper.GetDataRow(relmContext, query, parameters: parameters, throwException: throwException);

        /// <summary>
        /// Executes the specified query and retrieves the first row of the result set as a <see cref="DataRow"/>.
        /// </summary>
        /// <param name="relmQuickContext">The context used to execute the query. This must not be <see langword="null"/>.</param>
        /// <param name="query">The SQL query to execute. This must not be <see langword="null"/> or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. If <see langword="null"/>, no
        /// parameters are applied.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the query does not return any rows. If <see
        /// langword="true"/>, an exception is thrown when no rows are found; otherwise, <see langword="null"/> is
        /// returned.</param>
        /// <returns>The first row of the result set as a <see cref="DataRow"/>, or <see langword="null"/> if no rows are found
        /// and <paramref name="throwException"/> is <see langword="false"/>.</returns>
        public static DataRow GetDataRow(IRelmQuickContext relmQuickContext, string query, Dictionary<string, object> parameters = null, bool throwException = true)
            => RefinedResultsHelper.GetDataRow(relmQuickContext, query, parameters: parameters, throwException: throwException);

        /// <summary>
        /// Executes the specified SQL query and returns the results as a <see cref="DataTable"/>.
        /// </summary>
        /// <remarks>This method establishes a database connection based on the provided <paramref
        /// name="connectionName"/> and executes  the given query. If <paramref name="parameters"/> are provided, they
        /// are added to the query to prevent SQL injection.</remarks>
        /// <param name="connectionName">An <see cref="Enum"/> representing the name of the database connection to use.</param>
        /// <param name="query">The SQL query to execute. This query must be a valid SQL statement.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query.  If null, no parameters are
        /// applied.</param>
        /// <param name="throwException">A boolean value indicating whether to throw an exception if an error occurs.  If <see langword="true"/>,
        /// exceptions are thrown; otherwise, errors are suppressed.</param>
        /// <param name="allowUserVariables">A boolean value indicating whether user-defined variables are allowed in the query.  If <see
        /// langword="true"/>, user-defined variables are permitted; otherwise, they are not.</param>
        /// <returns>A <see cref="DataTable"/> containing the results of the query. The <see cref="DataTable"/> will be empty if
        /// the query returns no rows.</returns>
        public static DataTable GetDataTable(Enum connectionName, string query, Dictionary<string, object> parameters = null, bool throwException = true, bool allowUserVariables = false)
            => RefinedResultsHelper.GetDataTable(connectionName, query, parameters: parameters, throwException: throwException, allowUserVariables: allowUserVariables);

        /// <summary>
        /// Executes the specified SQL query and retrieves the results as a <see cref="DataTable"/>.
        /// </summary>
        /// <param name="establishedConnection">An open <see cref="MySqlConnection"/> to the database. The connection must be in an open state before
        /// calling this method.</param>
        /// <param name="query">The SQL query to execute. The query can include parameter placeholders.</param>
        /// <param name="parameters">An optional dictionary of parameter names and their corresponding values to be used in the query. If null,
        /// no parameters are applied.</param>
        /// <param name="throwException">A boolean value indicating whether to throw an exception if an error occurs. If <see langword="true"/>,
        /// exceptions are thrown; otherwise, errors are suppressed.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the query. If null, the query is executed
        /// without a transaction.</param>
        /// <returns>A <see cref="DataTable"/> containing the results of the query. If the query returns no rows, the <see
        /// cref="DataTable"/> will be empty.</returns>
        public static DataTable GetDataTable(MySqlConnection establishedConnection, string query, Dictionary<string, object> parameters = null, bool throwException = true, MySqlTransaction sqlTransaction = null)
            => RefinedResultsHelper.GetDataTable(establishedConnection, query, parameters: parameters, throwException: throwException, sqlTransaction: sqlTransaction);

        /// <summary>
        /// Executes the specified query against the provided Relm context and returns the results as a <see
        /// cref="DataTable"/>.
        /// </summary>
        /// <param name="relmContext">The Relm context used to execute the query. This cannot be <see langword="null"/>.</param>
        /// <param name="query">The SQL query to execute. This cannot be <see langword="null"/> or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. Can be <see langword="null"/>
        /// if no parameters are required.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if an error occurs during query execution.  If <see
        /// langword="true"/>, exceptions will be thrown; otherwise, errors will be suppressed.</param>
        /// <returns>A <see cref="DataTable"/> containing the results of the query. If the query returns no results, the <see
        /// cref="DataTable"/> will be empty.</returns>
        public static DataTable GetDataTable(IRelmContext relmContext, string query, Dictionary<string, object> parameters = null, bool throwException = true)
            => RefinedResultsHelper.GetDataTable(relmContext, query, parameters: parameters, throwException: throwException);
        
        /// <summary>
        /// Executes the specified query against the provided RelmQuick context and returns the results as a <see
        /// cref="DataTable"/>.
        /// </summary>
        /// <remarks>This method provides a convenient way to execute queries  and retrieve results in a tabular format. Ensure that the
        /// <paramref name="relmQuickContext"/> is properly initialized before calling this method.</remarks>
        /// <param name="relmQuickContext">The context used to execute the query. This must not be <see langword="null"/>.</param>
        /// <param name="query">The SQL query to execute. This must not be <see langword="null"/> or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. If <see langword="null"/>, no
        /// parameters are applied.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if an error occurs during query execution.  If <see
        /// langword="true"/>, exceptions are thrown; otherwise, errors are suppressed.</param>
        /// <returns>A <see cref="DataTable"/> containing the results of the query. The table will be empty if the query returns
        /// no rows.</returns>
        public static DataTable GetDataTable(IRelmQuickContext relmQuickContext, string query, Dictionary<string, object> parameters = null, bool throwException = true)
            => RefinedResultsHelper.GetDataTable(relmQuickContext, query, parameters: parameters, throwException: throwException);

        //***************** Object results *****************//

        /// <summary>
        /// Retrieves a single data object of the specified type based on the provided query and parameters.
        /// </summary>
        /// <remarks>This method is a convenience wrapper for retrieving a single data object.  If
        /// multiple objects match the query, only the first one is returned.</remarks>
        /// <typeparam name="T">The type of the data object to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="connectionName">The connection identifier used to determine the data source.</param>
        /// <param name="query">The query string used to retrieve the data object.</param>
        /// <param name="parameters">An optional dictionary of parameters to be used in the query. The keys represent parameter names, and the
        /// values represent their corresponding values.</param>
        /// <param name="throwException">A boolean value indicating whether an exception should be thrown if the query fails or no data is found.  If
        /// <see langword="true"/>, an exception is thrown; otherwise, <see langword="null"/> is returned.</param>
        /// <param name="allowUserVariables">A boolean value indicating whether user-defined variables are allowed in the query.  Set to <see
        /// langword="true"/> to enable user variables; otherwise, <see langword="false"/>.</param>
        /// <returns>An instance of the specified type <typeparamref name="T"/> populated with the data retrieved from the query,
        /// or <see langword="null"/> if no data is found and <paramref name="throwException"/> is <see
        /// langword="false"/>.</returns>
        public static T GetDataObject<T>(Enum connectionName, string query, Dictionary<string, object> parameters = null, bool throwException = true, bool allowUserVariables = false) where T : IRelmModel, new()
            => ObjectResultsHelper.GetDataObject<T>(connectionName, query, parameters: parameters, throwException: throwException, allowUserVariables: allowUserVariables);

        /// <summary>
        /// Retrieves a single data object of the specified type from the database based on the provided query and
        /// parameters.
        /// </summary>
        /// <typeparam name="T">The type of the data object to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="establishedConnection">An open <see cref="MySqlConnection"/> to the database. The connection must be valid and already established.</param>
        /// <param name="query">The SQL query to execute. The query should be structured to return a single row of data.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. Can be <see langword="null"/>
        /// if no parameters are required.</param>
        /// <param name="throwException">A boolean value indicating whether an exception should be thrown if the query does not return a result. If
        /// <see langword="true"/>, an exception is thrown when no data is found; otherwise, <see langword="default"/>
        /// is returned.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the query. Can be <see langword="null"/> if no
        /// transaction is needed.</param>
        /// <returns>An instance of type <typeparamref name="T"/> populated with the data retrieved from the database. Returns
        /// <see langword="default"/> if no data is found and <paramref name="throwException"/> is <see
        /// langword="false"/>.</returns>
        public static T GetDataObject<T>(MySqlConnection establishedConnection, string query, Dictionary<string, object> parameters = null, bool throwException = true, MySqlTransaction sqlTransaction = null) where T : IRelmModel, new()
            => ObjectResultsHelper.GetDataObject<T>(establishedConnection, query, parameters: parameters, throwException: throwException, sqlTransaction: sqlTransaction);

        /// <summary>
        /// Retrieves a single data object of the specified type based on the provided query and parameters.
        /// </summary>
        /// <remarks>This method provides a convenient way to retrieve a single data object. Ensure that the query and parameters are
        /// properly constructed to avoid unexpected results.</remarks>
        /// <typeparam name="T">The type of the data object to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="relmContext">The context used to execute the query. This provides the connection and configuration for the data source.</param>
        /// <param name="query">The query string used to retrieve the data object. The query must be valid for the underlying data source.</param>
        /// <param name="parameters">An optional dictionary of parameters to be used in the query. Keys represent parameter names, and values
        /// represent their corresponding values.</param>
        /// <param name="throwException">A boolean value indicating whether an exception should be thrown if the query does not return a result. If
        /// <see langword="true"/>, an exception is thrown when no data is found; otherwise, <see langword="null"/> is
        /// returned.</param>
        /// <returns>An instance of the specified type <typeparamref name="T"/> populated with the data retrieved by the query.
        /// Returns <see langword="null"/> if no data is found and <paramref name="throwException"/> is <see
        /// langword="false"/>.</returns>
        public static T GetDataObject<T>(IRelmContext relmContext, string query, Dictionary<string, object> parameters = null, bool throwException = true) where T : IRelmModel, new()
            => ObjectResultsHelper.GetDataObject<T>(relmContext, query, parameters: parameters, throwException: throwException);

        /// <summary>
        /// Retrieves a single data object of the specified type based on the provided query and parameters.
        /// </summary>
        /// <remarks>This method provides a convenient way to retrieve a single data object. Ensure that the query and parameters are
        /// properly constructed to avoid unexpected results.</remarks>
        /// <typeparam name="T">The type of the data object to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="relmQuickContext">The context used to execute the query. This provides the necessary connection and configuration for data
        /// retrieval.</param>
        /// <param name="query">The query string used to retrieve the data object. This should be a valid query supported by the context.</param>
        /// <param name="parameters">An optional dictionary of parameters to be used in the query. Keys represent parameter names, and values
        /// represent their corresponding values.</param>
        /// <param name="throwException">A boolean value indicating whether an exception should be thrown if the query does not return a result. If
        /// <see langword="true"/>, an exception is thrown when no result is found; otherwise, <see langword="null"/> is
        /// returned.</param>
        /// <returns>An instance of the specified type <typeparamref name="T"/> representing the retrieved data object. Returns
        /// <see langword="null"/> if no result is found and <paramref name="throwException"/> is <see
        /// langword="false"/>.</returns>
        public static T GetDataObject<T>(IRelmQuickContext relmQuickContext, string query, Dictionary<string, object> parameters = null, bool throwException = true) where T : IRelmModel, new()
            => ObjectResultsHelper.GetDataObject<T>(relmQuickContext, query, parameters: parameters, throwException: throwException);

        /// <summary>
        /// Retrieves a collection of data objects of the specified type from the database.
        /// </summary>
        /// <remarks>This method provides additional options for query execution.
        /// Ensure that the SQL query and parameters are properly constructed to avoid runtime errors.</remarks>
        /// <typeparam name="T">The type of data objects to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="connectionName">The name of the database connection to use. This must be an enumeration value representing a valid
        /// connection.</param>
        /// <param name="query">The SQL query to execute for retrieving the data objects.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to include in the query. Can be <see langword="null"/>
        /// if no parameters are needed.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if an error occurs during query execution. If <see
        /// langword="true"/>, exceptions will be thrown; otherwise, errors will be suppressed.</param>
        /// <param name="allowUserVariables">A value indicating whether user-defined variables are allowed in the query. Set to <see langword="true"/> to
        /// enable user variables; otherwise, <see langword="false"/>.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the data objects retrieved by the query. If no data is found, an
        /// empty collection is returned.</returns>
        public static IEnumerable<T> GetDataObjects<T>(Enum connectionName, string query, Dictionary<string, object> parameters = null, bool throwException = true, bool allowUserVariables = false) where T : IRelmModel, new()
            => ObjectResultsHelper.GetDataObjects<T>(connectionName, query, parameters: parameters, throwException: throwException, allowUserVariables: allowUserVariables);

        /// <summary>
        /// Retrieves a collection of data objects of the specified type from the database based on the provided query
        /// and parameters.
        /// </summary>
        /// <remarks>This method uses the specified SQL query and parameters to retrieve data objects from
        /// the database. Ensure that the query matches the structure of the type <typeparamref name="T"/>.</remarks>
        /// <typeparam name="T">The type of the data objects to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="establishedConnection">An open <see cref="MySqlConnection"/> to the database. The connection must be established and valid.</param>
        /// <param name="query">The SQL query to execute for retrieving the data objects.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. Can be <see langword="null"/>
        /// if no parameters are required.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if an error occurs during query execution. If <see
        /// langword="true"/>, exceptions will be propagated; otherwise, errors will be suppressed.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the query execution. Can be <see
        /// langword="null"/> if no transaction is required.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the data objects retrieved from the database. The collection will
        /// be empty if no matching records are found.</returns>
        public static IEnumerable<T> GetDataObjects<T>(MySqlConnection establishedConnection, string query, Dictionary<string, object> parameters = null, bool throwException = true, MySqlTransaction sqlTransaction = null) where T : IRelmModel, new()
            => ObjectResultsHelper.GetDataObjects<T>(establishedConnection, query, parameters: parameters, throwException: throwException, sqlTransaction);

        /// <summary>
        /// Executes a query against the specified Relm context and retrieves a collection of data objects of the
        /// specified type.
        /// </summary>
        /// <remarks>This method provides a convenient way to execute queries and
        /// retrieve data objects. Ensure that the query string and parameters are properly formatted to avoid runtime
        /// errors.</remarks>
        /// <typeparam name="T">The type of data objects to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="relmContext">The Relm context used to execute the query. This context provides the connection and configuration for the
        /// database.</param>
        /// <param name="query">The query string to execute. The query must be valid for the underlying database.</param>
        /// <param name="parameters">An optional dictionary of parameters to include in the query. Keys represent parameter names, and values
        /// represent their corresponding values.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if an error occurs during query execution. If <see
        /// langword="true"/>, exceptions will be thrown; otherwise, errors will be suppressed.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the data objects retrieved by the query. The collection will be
        /// empty if no matching data is found.</returns>
        public static IEnumerable<T> GetDataObjects<T>(IRelmQuickContext relmContext, string query, Dictionary<string, object> parameters = null, bool throwException = true) where T : IRelmModel, new()
            => ObjectResultsHelper.GetDataObjects<T>(relmContext, query, parameters: parameters, throwException: throwException);

        /// <summary>
        /// Executes a query against the specified Relm context and retrieves a collection of data objects of the
        /// specified type.
        /// </summary>
        /// <remarks>This method provides a convenient way to execute queries and
        /// retrieve data objects. Ensure that the query string and parameters are properly formatted to avoid runtime
        /// errors.</remarks>
        /// <typeparam name="T">The type of data objects to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="relmContext">The Relm context used to execute the query. This context provides the connection and configuration for the
        /// database.</param>
        /// <param name="query">The query string to execute. This should be a valid query supported by the Relm context.</param>
        /// <param name="parameters">An optional dictionary of parameters to include in the query. The keys represent parameter names, and the
        /// values represent their corresponding values.</param>
        /// <param name="throwException">A boolean value indicating whether an exception should be thrown if an error occurs during query execution. 
        /// If <see langword="true"/>, an exception will be thrown; otherwise, the method will handle the error
        /// silently.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the data objects retrieved by the query.  If no data is found,
        /// the collection will be empty.</returns>
        public static IEnumerable<T> GetDataObjects<T>(IRelmContext relmContext, string query, Dictionary<string, object> parameters = null, bool throwException = true) where T : IRelmModel, new()
            => ObjectResultsHelper.GetDataObjects<T>(relmContext, query, parameters: parameters, throwException: throwException);

        /// <summary>
        /// Converts the rows of the specified <see cref="DataTable"/> into a collection of objects of type
        /// <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>This method uses the <see cref="ObjectResultsHelper.GetDataObjects{T}(DataTable)"/>
        /// method to perform the conversion. Ensure that the <paramref name="existingData"/> table contains columns
        /// that match the properties of type <typeparamref name="T"/>.</remarks>
        /// <typeparam name="T">The type of objects to create from the data rows. Must implement <see cref="IRelmModel"/> and have a
        /// parameterless constructor.</typeparam>
        /// <param name="existingData">The <see cref="DataTable"/> containing the data to be converted. Each row in the table is mapped to an
        /// object of type <typeparamref name="T"/>.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the objects created from the rows of the <paramref
        /// name="existingData"/> table.</returns>
        public static IEnumerable<T> GetDataObjects<T>(DataTable existingData) where T : IRelmModel, new()
            => ObjectResultsHelper.GetDataObjects<T>(existingData);

        /// <summary>
        /// Executes the specified query and retrieves a collection of data mapped to the specified type.
        /// </summary>
        /// <remarks>This method uses the specified connection and query to retrieve data from the
        /// database. Ensure that the type <typeparamref name="T"/> has a structure compatible with the query
        /// results.</remarks>
        /// <typeparam name="T">The type to which the query results will be mapped.</typeparam>
        /// <param name="connectionName">The name of the connection to use for executing the query.</param>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="parameters">An optional dictionary of parameters to include in the query. Keys represent parameter names, and values
        /// represent their corresponding values.</param>
        /// <param name="throwException">A value indicating whether to throw an exception if an error occurs. If <see langword="false"/>, errors will
        /// be suppressed.</param>
        /// <param name="allowUserVariables">A value indicating whether user-defined variables are allowed in the query.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the results of the query mapped to the specified type. Returns an
        /// empty collection if no results are found.</returns>
        public static IEnumerable<T> GetDataList<T>(Enum connectionName, string query, Dictionary<string, object> parameters = null, bool throwException = true, bool allowUserVariables = false)
            => ObjectResultsHelper.GetDataList<T>(connectionName, query, parameters: parameters, throwException: throwException, allowUserVariables: allowUserVariables);

        /// <summary>
        /// Executes the specified SQL query and retrieves a list of objects of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of objects to map the query results to.</typeparam>
        /// <param name="establishedConnection">An open and valid <see cref="MySqlConnection"/> to use for the query.</param>
        /// <param name="query">The SQL query to execute. Must be a valid SQL statement.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to include in the query. Can be <see langword="null"/>
        /// if no parameters are needed.</param>
        /// <param name="throwException">A value indicating whether to throw an exception if an error occurs during query execution. If <see
        /// langword="true"/>, exceptions will be thrown; otherwise, errors will be suppressed.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the query. Can be <see langword="null"/> if no
        /// transaction is required.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the results of the query mapped to objects of type <typeparamref
        /// name="T"/>. Returns an empty collection if no results are found.</returns>
        public static IEnumerable<T> GetDataList<T>(MySqlConnection establishedConnection, string query, Dictionary<string, object> parameters = null, bool throwException = true, MySqlTransaction sqlTransaction = null)
            => ObjectResultsHelper.GetDataList<T>(establishedConnection, query, parameters: parameters, throwException: throwException, sqlTransaction: sqlTransaction);

        /// <summary>
        /// Executes a query against the specified Realm context and retrieves a collection of data items.
        /// </summary>
        /// <typeparam name="T">The type of the data items to retrieve.</typeparam>
        /// <param name="relmContext">The Realm context used to execute the query. Cannot be <see langword="null"/>.</param>
        /// <param name="query">The query string to execute. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="parameters">An optional dictionary of parameters to include in the query. Can be <see langword="null"/> if no parameters
        /// are needed.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the query fails.  If <see langword="true"/>, an
        /// exception is thrown on failure; otherwise, the method returns an empty collection.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the data items retrieved by the query.  Returns an empty
        /// collection if no items are found or if <paramref name="throwException"/> is <see langword="false"/> and the
        /// query fails.</returns>
        public static IEnumerable<T> GetDataList<T>(IRelmContext relmContext, string query, Dictionary<string, object> parameters = null, bool throwException = true)
            => ObjectResultsHelper.GetDataList<T>(relmContext, query, parameters: parameters, throwException: throwException);

        /// <summary>
        /// Executes a query against the specified context and returns a collection of results of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of objects to be returned in the result set.</typeparam>
        /// <param name="relmContext">The context used to execute the query. This parameter cannot be <see langword="null"/>.</param>
        /// <param name="query">The query string to execute. This parameter cannot be <see langword="null"/> or empty.</param>
        /// <param name="parameters">An optional dictionary of parameters to be used in the query. Keys represent parameter names, and values
        /// represent their corresponding values. Can be <see langword="null"/> if no parameters are needed.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if an error occurs during query execution. If <see
        /// langword="true"/>, an exception will be thrown on error; otherwise, the method will return an empty
        /// collection.</param>
        /// <returns>A collection of objects of type <typeparamref name="T"/> representing the results of the query. Returns an
        /// empty collection if no results are found or if <paramref name="throwException"/> is <see langword="false"/>
        /// and an error occurs.</returns>
        public static IEnumerable<T> GetDataList<T>(IRelmQuickContext relmContext, string query, Dictionary<string, object> parameters = null, bool throwException = true)
            => ObjectResultsHelper.GetDataList<T>(relmContext, query, parameters: parameters, throwException: throwException);

        //***************** Table write functions *****************//

        /// <summary>
        /// Creates and returns a <see cref="BulkTableWriter{T}"/> instance for performing bulk insert operations on a
        /// database table.
        /// </summary>
        /// <remarks>This method is designed for scenarios where large amounts of data need to be inserted
        /// into a database table efficiently. The behavior of the bulk operation can be customized using the provided
        /// parameters.</remarks>
        /// <typeparam name="T">The type of the objects to be written to the database. Each object represents a row in the table.</typeparam>
        /// <param name="connectionName">The name of the database connection, represented as an <see cref="Enum"/>. This specifies which database
        /// connection to use.</param>
        /// <param name="insertQuery">An optional SQL insert query to use for the bulk operation. If not provided, a default query is generated
        /// based on the type <typeparamref name="T"/>.</param>
        /// <param name="useTransaction">A value indicating whether the bulk operation should be performed within a database transaction. If <see
        /// langword="true"/>, the operation is transactional; otherwise, it is not.</param>
        /// <param name="throwException">A value indicating whether exceptions should be thrown if an error occurs during the operation. If <see
        /// langword="true"/>, exceptions are thrown; otherwise, errors are suppressed.</param>
        /// <param name="allowUserVariables">A value indicating whether user-defined variables are allowed in the SQL query. If <see langword="true"/>,
        /// user variables are permitted; otherwise, they are not.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns are allowed to be included in the bulk operation. If <see
        /// langword="true"/>, auto-increment columns are included; otherwise, they are excluded.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are allowed to be included in the bulk operation. If <see
        /// langword="true"/>, primary key columns are included; otherwise, they are excluded.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns are allowed to be included in the bulk operation. If <see
        /// langword="true"/>, unique columns are included; otherwise, they are excluded.</param>
        /// <returns>A <see cref="BulkTableWriter{T}"/> instance configured for the specified bulk operation.</returns>
        public static BulkTableWriter<T> GetBulkTableWriter<T>(Enum connectionName, string insertQuery = null, bool useTransaction = false, bool throwException = true, bool allowUserVariables = false, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false)
            => DataOutputOperations.GetBulkTableWriter<T>(connectionName, insertQuery: insertQuery, useTransaction: useTransaction, throwException: throwException, allowUserVariables: allowUserVariables, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns);
        
        /// <summary>
        /// Creates and returns a <see cref="BulkTableWriter{T}"/> instance for performing bulk insert operations on a
        /// MySQL database table.
        /// </summary>
        /// <remarks>This method provides a flexible way to perform high-performance bulk insert
        /// operations on a MySQL database table. The behavior of the operation can be customized using the provided
        /// parameters.</remarks>
        /// <typeparam name="T">The type of the objects to be written to the database. Each object represents a row in the target table.</typeparam>
        /// <param name="establishedConnection">An open and valid <see cref="MySqlConnection"/> to the target database. The connection must remain open for
        /// the duration of the bulk operation.</param>
        /// <param name="insertQuery">An optional custom SQL insert query to use for the bulk operation. If not provided, a default query will be
        /// generated based on the type <typeparamref name="T"/> and the target table schema.</param>
        /// <param name="throwException">A value indicating whether exceptions should be thrown if an error occurs during the bulk operation. If <see
        /// langword="true"/>, exceptions will be thrown; otherwise, errors will be suppressed.</param>
        /// <param name="useTransaction">A value indicating whether the bulk operation should be performed within a transaction. If <see
        /// langword="true"/>, a transaction will be used unless <paramref name="sqlTransaction"/> is provided.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to use for the bulk operation. If provided, the operation will
        /// use this transaction instead of creating a new one. This parameter is ignored if <paramref
        /// name="useTransaction"/> is <see langword="false"/>.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns in the target table are allowed to be explicitly written
        /// to. If <see langword="false"/>, auto-increment columns will be excluded from the operation.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns in the target table are allowed to be explicitly written to.
        /// If <see langword="false"/>, primary key columns will be excluded from the operation.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns in the target table are allowed to be explicitly written to. If
        /// <see langword="false"/>, unique columns will be excluded from the operation.</param>
        /// <returns>A <see cref="BulkTableWriter{T}"/> instance configured for the specified bulk operation.</returns>
        public static BulkTableWriter<T> GetBulkTableWriter<T>(MySqlConnection establishedConnection, string insertQuery = null, bool throwException = true, bool useTransaction = true, MySqlTransaction sqlTransaction = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false)
            => DataOutputOperations.GetBulkTableWriter<T>(establishedConnection, insertQuery: insertQuery, useTransaction: useTransaction, throwException: throwException, sqlTransaction: sqlTransaction, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns);
        
        /// <summary>
        /// Creates and returns a <see cref="BulkTableWriter{T}"/> instance for performing bulk insert operations on a
        /// database table.
        /// </summary>
        /// <remarks>This method provides a convenient way to configure and execute bulk insert operations
        /// on a database table. The behavior of the operation can be customized using the optional
        /// parameters.</remarks>
        /// <typeparam name="T">The type representing the table's data model. Each instance of <typeparamref name="T"/> corresponds to a row
        /// in the table.</typeparam>
        /// <param name="relmContext">The database context used to manage the connection and operations. This parameter cannot be <see
        /// langword="null"/>.</param>
        /// <param name="insertQuery">An optional SQL insert query to use for the bulk operation. If <see langword="null"/>, a default query is
        /// generated based on the type <typeparamref name="T"/>.</param>
        /// <param name="useTransaction">Indicates whether the bulk operation should be performed within a transaction. If <see langword="true"/>,
        /// the operation is transactional; otherwise, it is not.</param>
        /// <param name="throwException">Determines whether exceptions should be thrown during the operation. If <see langword="true"/>, exceptions
        /// are thrown; otherwise, errors are suppressed.</param>
        /// <param name="allowAutoIncrementColumns">Specifies whether auto-increment columns are allowed in the bulk operation. If <see langword="true"/>,
        /// auto-increment columns are included; otherwise, they are excluded.</param>
        /// <param name="allowPrimaryKeyColumns">Specifies whether primary key columns are allowed in the bulk operation. If <see langword="true"/>, primary
        /// key columns are included; otherwise, they are excluded.</param>
        /// <param name="allowUniqueColumns">Specifies whether unique columns are allowed in the bulk operation. If <see langword="true"/>, unique
        /// columns are included; otherwise, they are excluded.</param>
        /// <returns>A <see cref="BulkTableWriter{T}"/> instance configured for the specified bulk operation.</returns>
        public static BulkTableWriter<T> GetBulkTableWriter<T>(IRelmContext relmContext, string insertQuery = null, bool useTransaction = false, bool throwException = true, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false)
            => DataOutputOperations.GetBulkTableWriter<T>(relmContext, insertQuery: insertQuery, useTransaction: useTransaction, throwException: throwException, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns);
        
        /// <summary>
        /// Creates and returns a <see cref="BulkTableWriter{T}"/> instance for performing bulk insert operations on a
        /// database table.
        /// </summary>
        /// <remarks>This method is a wrapper around <see
        /// cref="DataOutputOperations.GetBulkTableWriter{T}"/> and provides additional configuration options for bulk
        /// insert operations. Use this method to efficiently insert large amounts of data into a database
        /// table.</remarks>
        /// <typeparam name="T">The type of the objects to be written to the database table.</typeparam>
        /// <param name="relmQuickContext">The database context used to manage the connection and operations. This parameter cannot be <see
        /// langword="null"/>.</param>
        /// <param name="insertQuery">An optional SQL insert query to use for the bulk operation. If <see langword="null"/>, a default query is
        /// generated based on the type <typeparamref name="T"/>.</param>
        /// <param name="useTransaction">A value indicating whether the bulk operation should be performed within a transaction. The default is <see
        /// langword="false"/>.</param>
        /// <param name="throwException">A value indicating whether exceptions should be thrown if an error occurs during the operation. The default
        /// is <see langword="true"/>.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns are allowed in the bulk operation. The default is <see
        /// langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are allowed in the bulk operation. The default is <see
        /// langword="false"/>.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns are allowed in the bulk operation. The default is <see
        /// langword="false"/>.</param>
        /// <returns>A <see cref="BulkTableWriter{T}"/> instance configured for the specified bulk operation.</returns>
        public static BulkTableWriter<T> GetBulkTableWriter<T>(IRelmQuickContext relmQuickContext, string insertQuery = null, bool useTransaction = false, bool throwException = true, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false)
            => DataOutputOperations.GetBulkTableWriter<T>(relmQuickContext, insertQuery: insertQuery, useTransaction: useTransaction, throwException: throwException, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns);
        
        /// <summary>
        /// Writes a collection of data to a database table in bulk, optimizing for high performance.
        /// </summary>
        /// <remarks>This method is designed for scenarios where large amounts of data need to be inserted
        /// into a database efficiently. It uses bulk operations to minimize database round-trips and improve
        /// performance. Ensure that the database schema matches the structure of the data being written to avoid
        /// runtime errors.</remarks>
        /// <typeparam name="T">The type of the data objects to be written to the table.</typeparam>
        /// <param name="connectionName">The name of the database connection to use. This must correspond to a valid connection configuration.</param>
        /// <param name="sourceData">The data to be written to the table. This can be a collection or a single object, depending on the
        /// implementation.</param>
        /// <param name="tableName">The name of the target database table. If <see langword="null"/>, the table name is inferred from the type
        /// <typeparamref name="T"/>.</param>
        /// <param name="forceType">An optional type to enforce when writing the data. If specified, the data will be treated as this type
        /// during the operation.</param>
        /// <param name="batchSize">The number of rows to write in each batch. Defaults to 100. Larger batch sizes may improve performance but
        /// require more memory.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether columns with auto-increment constraints are allowed to be written. Defaults to
        /// <see langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are allowed to be written. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns are allowed to be written. Defaults to <see langword="false"/>.</param>
        /// <returns>The number of rows successfully written to the database.</returns>
        public static int BulkTableWrite<T>(Enum connectionName, T sourceData, string tableName = null, Type forceType = null, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false)
            => DataOutputOperations.BulkTableWrite<T>(connectionName, sourceData, tableName, forceType, batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns);

        /// <summary>
        /// Writes a collection of data to a database table in bulk, optimizing for performance.
        /// </summary>
        /// <remarks>This method is designed for high-performance bulk data insertion. It is the caller's
        /// responsibility to ensure that the data in <paramref name="sourceData"/> matches the schema of the target
        /// table. <para> If <paramref name="allowAutoIncrementColumns"/>, <paramref name="allowPrimaryKeyColumns"/>, or
        /// <paramref name="allowUniqueColumns"/> are set to <see langword="true"/>, the caller must ensure that the
        /// data does not violate database constraints. </para> <para> The method does not automatically handle schema
        /// mismatches or data validation. Ensure that the data types and column mappings align with the target table.
        /// </para></remarks>
        /// <typeparam name="T">The type of the data source. Typically a collection of objects or a DataTable.</typeparam>
        /// <param name="establishedConnection">An open <see cref="MySqlConnection"/> to the database where the data will be written. The connection must
        /// remain open for the duration of the operation.</param>
        /// <param name="sourceData">The data to be written to the table. This can be a collection of objects or a DataTable.</param>
        /// <param name="tableName">The name of the target database table. If null, the table name is inferred from the type of <typeparamref
        /// name="T"/>.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the bulk write operation. If null, the
        /// operation is performed outside of a transaction.</param>
        /// <param name="forceType">An optional <see cref="Type"/> to enforce a specific type for the data being written. If null, the type is
        /// inferred from <typeparamref name="T"/>.</param>
        /// <param name="batchSize">The number of rows to write in each batch. Defaults to 100. Larger batch sizes may improve performance but
        /// require more memory.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns in the target table are allowed to be written. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns in the target table are allowed to be written. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns in the target table are allowed to be written. Defaults to <see
        /// langword="false"/>.</param>
        /// <returns>The number of rows successfully written to the database table.</returns>
        public static int BulkTableWrite<T>(MySqlConnection establishedConnection, T sourceData, string tableName = null, MySqlTransaction sqlTransaction = null, Type forceType = null, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false)
            => DataOutputOperations.BulkTableWrite<T>(establishedConnection, sourceData, tableName, sqlTransaction, forceType, batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns);
        
        /// <summary>
        /// Writes a collection of data to a database table in bulk, optimizing for performance.
        /// </summary>
        /// <remarks>This method is designed for high-performance bulk data insertion. It allows
        /// fine-grained control over which columns are included in the operation, such as auto-increment, primary key,
        /// and unique columns. Use the optional parameters to customize the behavior as needed.</remarks>
        /// <typeparam name="T">The type of the data to be written. This can be a collection or a single object.</typeparam>
        /// <param name="relmContext">The database context used to perform the bulk write operation. Cannot be <see langword="null"/>.</param>
        /// <param name="sourceData">The data to be written to the table. This can be a collection of objects or a single object. Cannot be <see
        /// langword="null"/>.</param>
        /// <param name="tableName">The name of the target database table. If <see langword="null"/>, the table name is inferred from the type
        /// <typeparamref name="T"/>.</param>
        /// <param name="forceType">An optional type to enforce for the operation. If <see langword="null"/>, the type is inferred from
        /// <typeparamref name="T"/>.</param>
        /// <param name="batchSize">The number of rows to write in each batch. Must be greater than 0. Defaults to 100.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns are allowed to be included in the bulk write operation. If
        /// <see langword="false"/>, auto-increment columns are excluded.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are allowed to be included in the bulk write operation. If
        /// <see langword="false"/>, primary key columns are excluded.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns are allowed to be included in the bulk write operation. If <see
        /// langword="false"/>, unique columns are excluded.</param>
        /// <returns>The total number of rows successfully written to the database.</returns>
        public static int BulkTableWrite<T>(IRelmContext relmContext, T sourceData, string tableName = null, Type forceType = null, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false)
            => DataOutputOperations.BulkTableWrite<T>(relmContext, sourceData, tableName, forceType, batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns);
        
        /// <summary>
        /// Writes data in bulk to a database table, with options to customize the operation.
        /// </summary>
        /// <remarks>This method provides a high-performance way to insert or update large amounts of data
        /// in a database table. The behavior of the operation can be customized using the optional parameters to
        /// control how specific columns (e.g., auto-increment, primary key, or unique columns) are handled.</remarks>
        /// <typeparam name="T">The type of the data source to be written to the table.</typeparam>
        /// <param name="relmQuickContext">The database context used to execute the bulk write operation. Cannot be <see langword="null"/>.</param>
        /// <param name="sourceData">The data to be written to the table. This can be a collection or a single object. Cannot be <see
        /// langword="null"/>.</param>
        /// <param name="tableName">The name of the target database table. If <see langword="null"/>, the table name is inferred from the type
        /// <typeparamref name="T"/>.</param>
        /// <param name="forceType">An optional type to enforce for the operation. If <see langword="null"/>, the type is inferred from
        /// <typeparamref name="T"/>.</param>
        /// <param name="batchSize">The number of rows to write in each batch. Must be greater than 0. Defaults to 100.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns in the target table are allowed to be explicitly written.
        /// If <see langword="false"/>, auto-increment columns are ignored during the write operation.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns in the target table are allowed to be explicitly written. If
        /// <see langword="false"/>, primary key columns are ignored during the write operation.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns in the target table are allowed to be explicitly written. If <see
        /// langword="false"/>, unique columns are ignored during the write operation.</param>
        /// <returns>The total number of rows successfully written to the database table.</returns>
        public static int BulkTableWrite<T>(IRelmQuickContext relmQuickContext, T sourceData, string tableName = null, Type forceType = null, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false)
            => DataOutputOperations.BulkTableWrite<T>(relmQuickContext, sourceData, tableName, forceType, batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns);
        
        /// <summary>
        /// Writes a collection of data to a database table in bulk, using the specified connection and options.
        /// </summary>
        /// <remarks>This method provides a high-performance way to insert large amounts of data into a
        /// database table. It supports optional configuration for handling auto-increment, primary key, and unique
        /// columns.</remarks>
        /// <typeparam name="T">The type of the objects in the <paramref name="sourceData"/> collection.</typeparam>
        /// <param name="connectionName">The name of the database connection to use. This must be an enumeration value representing a valid
        /// connection.</param>
        /// <param name="sourceData">The collection of data to write to the database table. Each item in the collection represents a row to be
        /// inserted.</param>
        /// <param name="tableName">The name of the target database table. If <see langword="null"/>, the table name is inferred from the type
        /// <typeparamref name="T"/>.</param>
        /// <param name="forceType">An optional type to enforce for the operation. If specified, the operation will treat the data as this type.</param>
        /// <param name="batchSize">The number of rows to write in each batch. Defaults to 100. Larger batch sizes may improve performance but
        /// require more memory.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns in the target table are allowed to be explicitly written.
        /// Defaults to <see langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns in the target table are allowed to be explicitly written.
        /// Defaults to <see langword="false"/>.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns in the target table are allowed to be explicitly written. Defaults
        /// to <see langword="false"/>.</param>
        /// <returns>The total number of rows successfully written to the database.</returns>
        public static int BulkTableWrite<T>(Enum connectionName, IEnumerable<T> sourceData, string tableName = null, Type forceType = null, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false)
            => DataOutputOperations.BulkTableWrite<T>(connectionName, sourceData, tableName, forceType, batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns);
        
        /// <summary>
        /// Writes a collection of data to a database table in bulk, optimizing for performance.
        /// </summary>
        /// <remarks>This method is designed for high-performance bulk data insertion and should be used
        /// when inserting large datasets. Ensure that the <paramref name="establishedConnection"/> is open and valid
        /// before calling this method.  If <paramref name="tableName"/> is not provided, the method attempts to infer
        /// the table name from the type <typeparamref name="T"/>.</remarks>
        /// <typeparam name="T">The type of the objects in the <paramref name="sourceData"/> collection.</typeparam>
        /// <param name="establishedConnection">An open <see cref="MySqlConnection"/> to the database where the data will be written. The connection must
        /// remain open for the duration of the operation.</param>
        /// <param name="sourceData">The collection of data to be written to the database table. Each item in the collection represents a row to
        /// be inserted.</param>
        /// <param name="tableName">The name of the target database table. If <c>null</c>, the table name is inferred from the type
        /// <typeparamref name="T"/>.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the bulk write operation. If <c>null</c>, the
        /// operation is performed without a transaction.</param>
        /// <param name="forceType">An optional <see cref="Type"/> to enforce a specific type mapping for the data being written. If
        /// <c>null</c>, the type is inferred from <typeparamref name="T"/>.</param>
        /// <param name="batchSize">The number of rows to write in each batch. Defaults to 100. Larger batch sizes may improve performance but
        /// require more memory.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether columns with auto-increment constraints are included in the bulk write operation.
        /// If <c>true</c>, auto-increment columns are included; otherwise, they are excluded.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are included in the bulk write operation.  If <c>true</c>,
        /// primary key columns are included; otherwise, they are excluded.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique constraint columns are included in the bulk write operation.  If
        /// <c>true</c>, unique columns are included; otherwise, they are excluded.</param>
        /// <returns>The total number of rows successfully written to the database table.</returns>
        public static int BulkTableWrite<T>(MySqlConnection establishedConnection, IEnumerable<T> sourceData, string tableName = null, MySqlTransaction sqlTransaction = null, Type forceType = null, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false)
            => DataOutputOperations.BulkTableWrite<T>(establishedConnection, sourceData, tableName, sqlTransaction, forceType, batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns);
        
        /// <summary>
        /// Writes a collection of data to a database table in bulk, optimizing for performance.
        /// </summary>
        /// <remarks>This method is designed for high-performance bulk data insertion and may bypass
        /// certain database constraints depending on the provided parameters. Use caution when enabling options such as
        /// <paramref name="allowAutoIncrementColumns"/>, <paramref name="allowPrimaryKeyColumns"/>, or <paramref
        /// name="allowUniqueColumns"/> to avoid violating database integrity.</remarks>
        /// <typeparam name="T">The type of the objects in the <paramref name="sourceData"/> collection.</typeparam>
        /// <param name="relmContext">The database context used to perform the bulk write operation. Cannot be <see langword="null"/>.</param>
        /// <param name="sourceData">The collection of data to be written to the table. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="tableName">The name of the target database table. If <see langword="null"/>, the table name is inferred from the type
        /// <typeparamref name="T"/>.</param>
        /// <param name="forceType">An optional type to enforce for the table schema. If <see langword="null"/>, the schema is inferred from
        /// <typeparamref name="T"/>.</param>
        /// <param name="batchSize">The number of records to write in each batch. Must be greater than 0. Defaults to 100.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns in the target table are allowed to be explicitly written
        /// to. If <see langword="false"/>, auto-increment columns are ignored during the write operation.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns in the target table are allowed to be explicitly written to.
        /// If <see langword="false"/>, primary key columns are ignored during the write operation.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns in the target table are allowed to be explicitly written to. If
        /// <see langword="false"/>, unique columns are ignored during the write operation.</param>
        /// <returns>The total number of rows successfully written to the database table.</returns>
        public static int BulkTableWrite<T>(IRelmContext relmContext, IEnumerable<T> sourceData, string tableName = null, Type forceType = null, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false)
            => DataOutputOperations.BulkTableWrite<T>(relmContext, sourceData, tableName, forceType, batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns);
        
        /// <summary>
        /// Writes a collection of data to a database table in bulk, optimizing for performance.
        /// </summary>
        /// <remarks>This method is designed for high-performance bulk insert operations. It is the
        /// caller's responsibility to ensure that the data in <paramref name="sourceData"/> conforms to the schema of
        /// the target table.</remarks>
        /// <typeparam name="T">The type of the objects in the <paramref name="sourceData"/> collection.</typeparam>
        /// <param name="relmQuickContext">The database context used to execute the bulk write operation. Cannot be <see langword="null"/>.</param>
        /// <param name="sourceData">The collection of data to be written to the table. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="tableName">The name of the target database table. If <see langword="null"/>, the table name is inferred from the type
        /// <typeparamref name="T"/>.</param>
        /// <param name="forceType">An optional type to enforce for the table schema. If <see langword="null"/>, the schema is inferred from
        /// <typeparamref name="T"/>.</param>
        /// <param name="batchSize">The number of rows to write in each batch. Must be greater than 0. Defaults to 100.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns in the target table are allowed to be written to. If <see
        /// langword="false"/>, auto-increment columns are excluded from the operation.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns in the target table are allowed to be written to. If <see
        /// langword="false"/>, primary key columns are excluded from the operation.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns in the target table are allowed to be written to. If <see
        /// langword="false"/>, unique columns are excluded from the operation.</param>
        /// <returns>The total number of rows successfully written to the database.</returns>
        public static int BulkTableWrite<T>(IRelmQuickContext relmQuickContext, IEnumerable<T> sourceData, string tableName = null, Type forceType = null, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false)
            => DataOutputOperations.BulkTableWrite<T>(relmQuickContext, sourceData, tableName, forceType, batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns);

        //***************** Core functions *****************//

        /// <summary>
        /// Executes a database operation using the specified connection, query, and parameters.
        /// </summary>
        /// <remarks>This method delegates the database operation to the
        /// <c>DatabaseWorkHelper.DoDatabaseWork</c> method.  Ensure that the <paramref name="connectionName"/>
        /// corresponds to a valid database connection and that the  <paramref name="query"/> is properly formatted for
        /// the target database.</remarks>
        /// <param name="connectionName">The name of the database connection to use. This must be a valid value from the specified enumeration.</param>
        /// <param name="query">The SQL query to execute. The query can include parameter placeholders.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query.  If null, no parameters will
        /// be passed to the query.</param>
        /// <param name="throwException">A boolean value indicating whether to throw an exception if an error occurs.  If <see langword="true"/>,
        /// exceptions will be thrown; otherwise, errors will be suppressed.</param>
        /// <param name="useTransaction">A boolean value indicating whether to execute the query within a transaction.  If <see langword="true"/>, a
        /// transaction will be used; otherwise, the query will execute without a transaction.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> object to use for the operation.  If provided, the operation will
        /// use this transaction; otherwise, a new transaction may be created if <paramref name="useTransaction"/> is
        /// <see langword="true"/>.</param>
        /// <param name="allowUserVariables">A boolean value indicating whether user-defined variables are allowed in the query.  If <see
        /// langword="true"/>, user variables are permitted; otherwise, they are not.</param>
        public static void DoDatabaseWork(Enum connectionName, string query, Dictionary<string, object> parameters = null, bool throwException = true, bool useTransaction = false, MySqlTransaction sqlTransaction = null, bool allowUserVariables = false)
            => DatabaseWorkHelper.DoDatabaseWork(connectionName, query, parameters, throwException, useTransaction, sqlTransaction, allowUserVariables);
        
        /// <summary>
        /// Executes a database query using the provided connection and optional parameters.
        /// </summary>
        /// <remarks>This method delegates the execution to <see
        /// cref="DatabaseWorkHelper.DoDatabaseWork"/>. Ensure that the connection is properly managed and disposed of
        /// after use.</remarks>
        /// <param name="establishedConnection">An open and established <see cref="MySqlConnection"/> to the database. The connection must be valid and open
        /// before calling this method.</param>
        /// <param name="query">The SQL query to execute. This must be a valid SQL statement supported by the database.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. If null, no parameters are
        /// applied.</param>
        /// <param name="throwException">A boolean value indicating whether to throw an exception if an error occurs. If <see langword="true"/>,
        /// exceptions are thrown; otherwise, errors are suppressed.</param>
        /// <param name="useTransaction">A boolean value indicating whether to execute the query within a transaction. If <see langword="true"/>, a
        /// transaction is used; otherwise, the query is executed without a transaction.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to use for the query. If provided, this transaction is used
        /// instead of creating a new one. Ignored if <paramref name="useTransaction"/> is <see langword="false"/>.</param>
        public static void DoDatabaseWork(MySqlConnection establishedConnection, string query, Dictionary<string, object> parameters = null, bool throwException = true, bool useTransaction = false, MySqlTransaction sqlTransaction = null)
            => DatabaseWorkHelper.DoDatabaseWork(establishedConnection, query, parameters, throwException, useTransaction, sqlTransaction);
        
        /// <summary>
        /// Executes a database operation using the specified query and parameters.
        /// </summary>
        /// <remarks>This method delegates the database operation to an internal helper. Ensure that the 
        /// <paramref name="relmContext"/> is properly configured and that the <paramref name="query"/>  is valid for
        /// the target database.</remarks>
        /// <param name="relmContext">The database context used to execute the operation. Cannot be <see langword="null"/>.</param>
        /// <param name="query">The SQL query to execute. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query.  If <see langword="null"/>, no
        /// parameters will be passed.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the operation fails.  The default value is <see
        /// langword="true"/>.</param>
        /// <param name="useTransaction">A value indicating whether the operation should be executed within a transaction.  The default value is <see
        /// langword="false"/>.</param>
        public static void DoDatabaseWork(IRelmContext relmContext, string query, Dictionary<string, object> parameters = null, bool throwException = true, bool useTransaction = false)
            => DatabaseWorkHelper.DoDatabaseWork(relmContext, query, parameters, throwException: throwException, useTransaction: useTransaction);
        
        /// <summary>
        /// Executes a database operation using the specified query and parameters.
        /// </summary>
        /// <remarks>This method delegates the database operation to an internal helper. Ensure that the
        /// provided query and parameters are valid for the target database.</remarks>
        /// <param name="relmQuickContext">The database context used to execute the operation. This cannot be <see langword="null"/>.</param>
        /// <param name="query">The SQL query to execute. This cannot be <see langword="null"/> or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. If <see langword="null"/>, no
        /// parameters are applied.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the operation fails. The default is <see
        /// langword="true"/>.</param>
        /// <param name="useTransaction">A value indicating whether the operation should be executed within a transaction. The default is <see
        /// langword="false"/>.</param>
        public static void DoDatabaseWork(IRelmQuickContext relmQuickContext, string query, Dictionary<string, object> parameters = null, bool throwException = true, bool useTransaction = false)
            => DatabaseWorkHelper.DoDatabaseWork(relmQuickContext, query, parameters, throwException: throwException, useTransaction: useTransaction);
        
        /// <summary>
        /// Executes a database operation and returns the result of the specified type.
        /// </summary>
        /// <remarks>This method provides a flexible way to execute database operations, supporting
        /// parameterized queries, transactions, and user-defined variables.  Ensure that the specified type
        /// <typeparamref name="T"/> matches the expected result of the query.</remarks>
        /// <typeparam name="T">The type of the result returned by the database operation.</typeparam>
        /// <param name="connectionName">The name of the database connection to use. Must be a valid enumeration value representing a configured
        /// connection.</param>
        /// <param name="query">The SQL query to execute. Cannot be null or empty.</param>
        /// <param name="parameters">An optional dictionary of parameters to include in the query. Keys represent parameter names, and values
        /// represent their corresponding values. Can be null if no parameters are needed.</param>
        /// <param name="throwException">Indicates whether an exception should be thrown if an error occurs during the operation. If <see
        /// langword="true"/>, exceptions will be propagated; otherwise, errors will be suppressed.</param>
        /// <param name="useTransaction">Specifies whether the operation should be executed within a transaction. If <see langword="true"/>, a
        /// transaction will be used unless <paramref name="sqlTransaction"/> is provided.</param>
        /// <param name="sqlTransaction">An optional existing <see cref="MySqlTransaction"/> to use for the operation. If provided, <paramref
        /// name="useTransaction"/> is ignored.</param>
        /// <param name="allowUserVariables">Indicates whether user-defined variables are allowed in the query. If <see langword="true"/>, user variables
        /// are permitted; otherwise, they are disallowed.</param>
        /// <returns>The result of the database operation, cast to the specified type <typeparamref name="T"/>.</returns>
        public static T DoDatabaseWork<T>(Enum connectionName, string query, Dictionary<string, object> parameters = null, bool throwException = true, bool useTransaction = false, MySqlTransaction sqlTransaction = null, bool allowUserVariables = false)
            => DatabaseWorkHelper.DoDatabaseWork<T>(connectionName, query, parameters, throwException, useTransaction, sqlTransaction, allowUserVariables);
        
        /// <summary>
        /// Executes a database query and returns the result as the specified type.
        /// </summary>
        /// <remarks>This method is a wrapper for executing database queries with optional
        /// parameterization,  transaction support, and error handling. Ensure the connection is open before calling
        /// this method.</remarks>
        /// <typeparam name="T">The type of the result expected from the query.</typeparam>
        /// <param name="establishedConnection">An open and valid <see cref="MySqlConnection"/> to the database.</param>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query.  If null, no parameters are
        /// added.</param>
        /// <param name="throwException">A boolean value indicating whether to throw an exception if an error occurs.  If <see langword="true"/>,
        /// exceptions are thrown; otherwise, errors are suppressed.</param>
        /// <param name="useTransaction">A boolean value indicating whether to execute the query within a transaction.  If <see langword="true"/>, a
        /// transaction is used.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to use for the query.  If null, a new transaction is created if
        /// <paramref name="useTransaction"/> is <see langword="true"/>.</param>
        /// <returns>The result of the query, cast to the specified type <typeparamref name="T"/>.</returns>
        public static T DoDatabaseWork<T>(MySqlConnection establishedConnection, string query, Dictionary<string, object> parameters = null, bool throwException = true, bool useTransaction = false, MySqlTransaction sqlTransaction = null)
            => DatabaseWorkHelper.DoDatabaseWork<T>(establishedConnection, query, parameters, throwException, useTransaction, sqlTransaction);
        
        /// <summary>
        /// Executes a database operation using the specified query and parameters, and optionally within a transaction.
        /// </summary>
        /// <remarks>This method provides a high-level abstraction for executing database operations.  The
        /// behavior of the operation, including error handling and transaction usage, can be customized using the
        /// <paramref name="throwException"/> and <paramref name="useTransaction"/> parameters.</remarks>
        /// <typeparam name="T">The type of the result returned by the database operation.</typeparam>
        /// <param name="relmContext">The database context used to execute the operation. Cannot be <see langword="null"/>.</param>
        /// <param name="query">The SQL query to execute. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. Can be <see langword="null"/>
        /// if no parameters are required.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the operation fails.  If <see langword="true"/>,
        /// exceptions will be propagated; otherwise, the method may handle errors internally.</param>
        /// <param name="useTransaction">A value indicating whether the operation should be executed within a transaction.  If <see
        /// langword="true"/>, the operation will be wrapped in a transaction.</param>
        /// <returns>The result of the database operation, of type <typeparamref name="T"/>.</returns>
        public static T DoDatabaseWork<T>(IRelmContext relmContext, string query, Dictionary<string, object> parameters = null, bool throwException = true, bool useTransaction = false)
            => DatabaseWorkHelper.DoDatabaseWork<T>(relmContext, query, parameters, throwException, useTransaction);
        
        /// <summary>
        /// Executes a database operation using the specified query and parameters, and optionally within a transaction.
        /// </summary>
        /// <typeparam name="T">The type of the result expected from the database operation.</typeparam>
        /// <param name="relmQuickContext">The database context used to execute the operation. Cannot be <see langword="null"/>.</param>
        /// <param name="query">The SQL query to execute. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="parameters">An optional dictionary of parameters to include in the query. Keys represent parameter names, and values
        /// represent their corresponding values. Defaults to <see langword="null"/>.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the operation fails. Defaults to <see
        /// langword="true"/>.</param>
        /// <param name="useTransaction">A value indicating whether the operation should be executed within a transaction. Defaults to <see
        /// langword="false"/>.</param>
        /// <returns>The result of the database operation, cast to the specified type <typeparamref name="T"/>.</returns>
        public static T DoDatabaseWork<T>(IRelmQuickContext relmQuickContext, string query, Dictionary<string, object> parameters = null, bool throwException = true, bool useTransaction = false)
            => DatabaseWorkHelper.DoDatabaseWork<T>(relmQuickContext, query, parameters, throwException, useTransaction);

        /// <summary>
        /// Executes a database operation using the specified query and callback function.
        /// </summary>
        /// <remarks>This method delegates the database operation to an internal helper and provides
        /// flexibility for executing queries with or without transactions.  Ensure that the <paramref
        /// name="connectionName"/> corresponds to a valid database connection and that the <paramref name="query"/> is
        /// properly formatted.</remarks>
        /// <param name="connectionName">The name of the database connection to use. This must be a valid connection identifier.</param>
        /// <param name="query">The SQL query to execute. Cannot be null or empty.</param>
        /// <param name="actionCallback">A callback function that processes the <see cref="MySqlCommand"/> object and returns a result. Cannot be
        /// null.</param>
        /// <param name="throwException">Specifies whether to throw an exception if an error occurs. If <see langword="true"/>, exceptions will be
        /// thrown; otherwise, errors will be suppressed.</param>
        /// <param name="useTransaction">Specifies whether the operation should be executed within a transaction. If <see langword="true"/>, a
        /// transaction will be used.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> object to use for the operation. If provided, this transaction
        /// will be used instead of creating a new one.</param>
        /// <param name="allowUserVariables">Specifies whether user-defined variables are allowed in the SQL query. If <see langword="true"/>, user
        /// variables are permitted.</param>
        public static void DoDatabaseWork(Enum connectionName, string query, Func<MySqlCommand, object> actionCallback, bool throwException = true, bool useTransaction = false, MySqlTransaction sqlTransaction = null, bool allowUserVariables = false)
            => DatabaseWorkHelper.DoDatabaseWork(connectionName, query, actionCallback, throwException, useTransaction, sqlTransaction, allowUserVariables);
        
        /// <summary>
        /// Executes a database operation using the provided connection, query, and callback function.
        /// </summary>
        /// <remarks>This method delegates the database operation to a helper method and provides
        /// flexibility for executing queries with or without transactions.  Ensure that the connection is open and
        /// valid before calling this method. If <paramref name="useTransaction"/> is <see langword="true"/> and 
        /// <paramref name="sqlTransaction"/> is not provided, a new transaction will be created and committed
        /// automatically.</remarks>
        /// <param name="establishedConnection">An open and valid <see cref="MySqlConnection"/> to the database. The connection must be established before
        /// calling this method.</param>
        /// <param name="query">The SQL query to execute. This query is passed to the callback function for processing.</param>
        /// <param name="actionCallback">A callback function that processes the <see cref="MySqlCommand"/> created from the query. The function
        /// should return an object representing the result of the operation.</param>
        /// <param name="throwException">A value indicating whether to throw exceptions if an error occurs during the operation.  If <see
        /// langword="true"/>, exceptions will be propagated; otherwise, errors will be suppressed. The default value is
        /// <see langword="true"/>.</param>
        /// <param name="useTransaction">A value indicating whether the operation should be executed within a transaction.  If <see
        /// langword="true"/>, a transaction will be used unless <paramref name="sqlTransaction"/> is provided. The
        /// default value is <see langword="false"/>.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to use for the operation. If provided, this transaction will be
        /// used instead of creating a new one.</param>
        public static void DoDatabaseWork(MySqlConnection establishedConnection, string query, Func<MySqlCommand, object> actionCallback, bool throwException = true, bool useTransaction = false, MySqlTransaction sqlTransaction = null)
            => DatabaseWorkHelper.DoDatabaseWork(establishedConnection, query, actionCallback, throwException, useTransaction, sqlTransaction);
        
        /// <summary>
        /// Executes a database operation using the specified query and callback function.
        /// </summary>
        /// <remarks>This method delegates the database operation to a helper class. Ensure that the
        /// provided <paramref name="relmContext"/> is properly configured and that the <paramref name="query"/> is
        /// valid for the target database.</remarks>
        /// <param name="relmContext">The database context used to establish the connection. Cannot be <see langword="null"/>.</param>
        /// <param name="query">The SQL query to be executed. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="actionCallback">A callback function that processes the <see cref="MySqlCommand"/> object and returns a result. The callback
        /// cannot be <see langword="null"/>.</param>
        /// <param name="throwException">A value indicating whether exceptions should be thrown if an error occurs during the operation. The default
        /// value is <see langword="true"/>.</param>
        /// <param name="useTransaction">A value indicating whether the operation should be executed within a database transaction. The default value
        /// is <see langword="false"/>.</param>
        public static void DoDatabaseWork(IRelmContext relmContext, string query, Func<MySqlCommand, object> actionCallback, bool throwException = true, bool useTransaction = false)
            => DatabaseWorkHelper.DoDatabaseWork(relmContext, query, actionCallback, throwException, useTransaction);
        
        /// <summary>
        /// Executes a database operation using the specified query and callback function.
        /// </summary>
        /// <remarks>This method delegates the database operation to <see
        /// cref="DatabaseWorkHelper.DoDatabaseWork"/>.  Ensure that the <paramref name="relmQuickContext"/> is properly
        /// configured before calling this method.</remarks>
        /// <param name="relmQuickContext">The database context used to establish the connection and manage the operation.</param>
        /// <param name="query">The SQL query to execute. Must be a valid SQL statement.</param>
        /// <param name="actionCallback">A callback function that processes the <see cref="MySqlCommand"/> object and returns a result.</param>
        /// <param name="throwException">A value indicating whether to throw an exception if an error occurs during the operation.  <see
        /// langword="true"/> to throw exceptions; otherwise, <see langword="false"/>.</param>
        /// <param name="useTransaction">A value indicating whether the operation should be executed within a database transaction.  <see
        /// langword="true"/> to use a transaction; otherwise, <see langword="false"/>.</param>
        public static void DoDatabaseWork(IRelmQuickContext relmQuickContext, string query, Func<MySqlCommand, object> actionCallback, bool throwException = true, bool useTransaction = false)
            => DatabaseWorkHelper.DoDatabaseWork(relmQuickContext, query, actionCallback, throwException, useTransaction);

        /// <summary>
        /// Executes a database operation using the specified query and callback function.
        /// </summary>
        /// <remarks>This method provides a flexible way to execute database operations by allowing the
        /// caller to specify a query and a callback function to process the database command. The operation can
        /// optionally be executed within a transaction, and user-defined variables in the query can be enabled if
        /// required.</remarks>
        /// <typeparam name="T">The type of the result returned by the callback function.</typeparam>
        /// <param name="connectionName">The name of the database connection to use. Must be a valid enumeration value representing a configured
        /// connection.</param>
        /// <param name="query">The SQL query to execute. Cannot be null or empty.</param>
        /// <param name="actionCallback">A callback function that processes the <see cref="MySqlCommand"/> object and returns a result of type
        /// <typeparamref name="T"/>.</param>
        /// <param name="throwException">Specifies whether to throw an exception if an error occurs. If <see langword="true"/>, exceptions will be
        /// thrown; otherwise, errors will be suppressed.</param>
        /// <param name="useTransaction">Specifies whether the operation should be executed within a database transaction. If <see langword="true"/>,
        /// a transaction will be used.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> object to use for the operation. If provided, the operation will
        /// be executed within this transaction.</param>
        /// <param name="allowUserVariables">Specifies whether user-defined variables are allowed in the SQL query. If <see langword="true"/>, user
        /// variables are permitted.</param>
        /// <returns>The result of the operation, as returned by the <paramref name="actionCallback"/> function.</returns>
        public static T DoDatabaseWork<T>(Enum connectionName, string query, Func<MySqlCommand, object> actionCallback, bool throwException = true, bool useTransaction = false, MySqlTransaction sqlTransaction = null, bool allowUserVariables = false)
            => DatabaseWorkHelper.DoDatabaseWork<T>(connectionName, query, actionCallback, throwException, useTransaction, sqlTransaction, allowUserVariables);
        
        /// <summary>
        /// Executes a database operation using the provided query and callback function.
        /// </summary>
        /// <remarks>This method provides a flexible way to execute database operations by allowing the
        /// caller to define the specific action to perform via the <paramref name="actionCallback"/>. If <paramref
        /// name="useTransaction"/> is <see langword="true"/> and no <paramref name="sqlTransaction"/> is provided, a
        /// new transaction is created for the operation.</remarks>
        /// <typeparam name="T">The type of the result returned by the callback function.</typeparam>
        /// <param name="establishedConnection">An open and valid <see cref="MySqlConnection"/> to the database. The connection must already be established.</param>
        /// <param name="query">The SQL query to be executed. This query is passed to the callback function for execution.</param>
        /// <param name="actionCallback">A callback function that defines the operation to perform using the <see cref="MySqlCommand"/> created from
        /// the query.</param>
        /// <param name="throwException">A value indicating whether exceptions should be thrown if an error occurs during the operation. If <see
        /// langword="true"/>, exceptions are propagated to the caller; otherwise, they are suppressed.</param>
        /// <param name="useTransaction">A value indicating whether the operation should be executed within a transaction. If <see langword="true"/>,
        /// a transaction is used unless <paramref name="sqlTransaction"/> is provided.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to use for the operation. If provided, this transaction is used
        /// regardless of the value of <paramref name="useTransaction"/>.</param>
        /// <returns>The result of type <typeparamref name="T"/> as returned by the <paramref name="actionCallback"/>.</returns>
        public static T DoDatabaseWork<T>(MySqlConnection establishedConnection, string query, Func<MySqlCommand, object> actionCallback, bool throwException = true, bool useTransaction = false, MySqlTransaction sqlTransaction = null)
            => DatabaseWorkHelper.DoDatabaseWork<T>(establishedConnection, query, actionCallback, throwException, useTransaction, sqlTransaction);
        
        /// <summary>
        /// Executes a database operation using the specified query and callback function.
        /// </summary>
        /// <remarks>This method provides a flexible way to execute database operations by allowing the
        /// caller to specify a query and a callback function that defines the operation to perform. The operation can
        /// optionally be executed within a transaction, and exceptions can be suppressed based on the <paramref
        /// name="throwException"/> parameter.</remarks>
        /// <typeparam name="T">The type of the result returned by the database operation.</typeparam>
        /// <param name="relmContext">The database context used to establish the connection and manage the operation. Cannot be <see
        /// langword="null"/>.</param>
        /// <param name="query">The SQL query to execute. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="actionCallback">A callback function that defines the operation to perform using the <see cref="MySqlCommand"/> object.
        /// Cannot be <see langword="null"/>.</param>
        /// <param name="throwException">A value indicating whether exceptions encountered during the operation should be thrown. If <see
        /// langword="true"/>, exceptions will be propagated to the caller; otherwise, they will be suppressed.</param>
        /// <param name="useTransaction">A value indicating whether the operation should be executed within a database transaction. If <see
        /// langword="true"/>, the operation will be wrapped in a transaction.</param>
        /// <returns>The result of the database operation, as defined by the <typeparamref name="T"/> type.</returns>
        public static T DoDatabaseWork<T>(IRelmContext relmContext, string query, Func<MySqlCommand, object> actionCallback, bool throwException = true, bool useTransaction = false)
            => DatabaseWorkHelper.DoDatabaseWork<T>(relmContext, query, actionCallback, throwException, useTransaction);
        
        /// <summary>
        /// Executes a database operation using the specified query and callback function.
        /// </summary>
        /// <remarks>This method provides a flexible way to execute database operations by allowing the
        /// caller to specify a query  and a custom callback function to process the database command. The caller can
        /// optionally enable transaction  support and control whether exceptions are thrown or suppressed.</remarks>
        /// <typeparam name="T">The type of the result returned by the callback function.</typeparam>
        /// <param name="relmQuickContext">The database context used to establish the connection and manage the operation. Cannot be <see
        /// langword="null"/>.</param>
        /// <param name="query">The SQL query to be executed. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="actionCallback">A callback function that defines the operation to perform using the <see cref="MySqlCommand"/> object.
        /// Cannot be <see langword="null"/>.</param>
        /// <param name="throwException">A value indicating whether exceptions should be thrown if an error occurs during the operation.  If <see
        /// langword="true"/>, exceptions will be propagated; otherwise, errors will be suppressed.</param>
        /// <param name="useTransaction">A value indicating whether the operation should be executed within a database transaction.  If <see
        /// langword="true"/>, the operation will be wrapped in a transaction.</param>
        /// <returns>The result of the operation, as returned by the <paramref name="actionCallback"/>.</returns>
        public static T DoDatabaseWork<T>(IRelmQuickContext relmQuickContext, string query, Func<MySqlCommand, object> actionCallback, bool throwException = true, bool useTransaction = false)
            => DatabaseWorkHelper.DoDatabaseWork<T>(relmQuickContext, query, actionCallback, throwException, useTransaction);

        //***************** Table operations *****************//
        /*
		public static bool TruncateTable<T>(Enum ConnectionStringType, string TableName = null, Type ForceType = null)
			=> TableOperationsHelper.TruncateTable<T>(ConnectionStringType, TableName, ForceType);

		public static bool TruncateTable<T>(MySqlConnection establishedConnection, string TableName = null, Type ForceType = null, MySqlTransaction sqlTransaction = null)
			=> TableOperationsHelper.TruncateTable<T>(establishedConnection, TableName, ForceType, sqlTransaction);

		public static bool CreateTable<T>(Enum ConnectionStringType)
			=> TableOperationsHelper.CreateTable<T>(ConnectionStringType);

		public static bool CreateTable<T>(MySqlConnection establishedConnection, MySqlTransaction sqlTransaction = null)
			=> TableOperationsHelper.CreateTable<T>(establishedConnection, sqlTransaction);
		*/

        /// <summary>
        /// Retrieves the name of the database table associated with the specified data model type.
        /// </summary>
        /// <remarks>This method is typically used to determine the table name for a given data model type
        /// in the context of database operations.</remarks>
        /// <typeparam name="T">The type of the data model. Must implement <see cref="IRelmModel"/> and have a parameterless constructor.</typeparam>
        /// <returns>A <see cref="string"/> representing the name of the database table associated with the specified data model
        /// type.</returns>
        public static string GetDalTable<T>() where T : IRelmModel, new() 
            => TableOperationsHelper.GetDalTable<T>();

        /// <summary>
        /// Retrieves the name of the database table associated with the specified Data Access Layer (DAL) object type.
        /// </summary>
        /// <param name="DalObjectType">The type of the DAL object for which the table name is being retrieved. This parameter cannot be <see
        /// langword="null"/>.</param>
        /// <returns>The name of the database table associated with the specified DAL object type.</returns>
        public static string GetDalTable(Type DalObjectType) 
            => TableOperationsHelper.GetDalTable(DalObjectType);

        /// <summary>
        /// Retrieves the name of the database column associated with the specified property or field.
        /// </summary>
        /// <typeparam name="T">The type of the model that implements <see cref="IRelmModel"/>.</typeparam>
        /// <param name="predicate">An expression that specifies the property or field for which to retrieve the column name. The expression
        /// should be in the form of a lambda, such as <c>x => x.PropertyName</c>.</param>
        /// <returns>The name of the database column corresponding to the specified property or field.</returns>
        public static string GetColumnName<T>(Expression<Func<T, object>> predicate) where T : IRelmModel
            => TableOperationsHelper.GetColumnName(predicate);

        /// <summary>
        /// Provides a standardized wrapper for executing database operations within a connection and transaction
        /// context.
        /// </summary>
        /// <remarks>This method ensures that the database connection and transaction are properly
        /// managed, including opening, committing, rolling back, and disposing of resources as necessary. Use this
        /// wrapper to simplify error handling and resource management for database operations.</remarks>
        /// <param name="connectionName">The type of database connection to use, represented as an enumeration value.</param>
        /// <param name="actionWrapper">A delegate that defines the operations to perform using the provided <see cref="MySqlConnection"/> and <see
        /// cref="MySqlTransaction"/>. The connection and transaction are managed by the wrapper.</param>
        /// <param name="exceptionHandler">An optional delegate to handle exceptions that occur during the execution of <paramref
        /// name="actionWrapper"/>. The delegate receives the exception and an error message as parameters. If not
        /// provided, exceptions will propagate to the caller.</param>
        public static void StandardConnectionWrapper(Enum connectionName, Action<MySqlConnection, MySqlTransaction> actionWrapper, Action<Exception, string> exceptionHandler = null)
            => StandardConnectionHelper.StandardConnectionWrapper(connectionName, actionWrapper, exceptionHandler);

        /// <summary>
        /// Executes a database operation within a standard connection and transaction context.
        /// </summary>
        /// <remarks>This method provides a standardized way to execute database operations, ensuring that
        /// the connection  and transaction are properly managed. If an exception occurs and an <paramref
        /// name="exceptionHandler"/>  is provided, the exception will be passed to the handler for custom
        /// processing.</remarks>
        /// <typeparam name="T">The type of the result returned by the operation.</typeparam>
        /// <param name="connectionName">The name of the database connection, represented as an <see cref="Enum"/>.</param>
        /// <param name="actionWrapper">A delegate that defines the operation to execute. The delegate receives a <see cref="MySqlConnection"/>  and
        /// a <see cref="MySqlTransaction"/> as parameters and returns a result of type <typeparamref name="T"/>.</param>
        /// <param name="exceptionHandler">An optional delegate to handle exceptions that occur during the operation. The delegate receives the 
        /// exception and a string message describing the context of the error.</param>
        /// <returns>The result of the operation, as defined by the <paramref name="actionWrapper"/> delegate.</returns>
        public static T StandardConnectionWrapper<T>(Enum connectionName, Func<MySqlConnection, MySqlTransaction, T> actionWrapper, Action<Exception, string> exceptionHandler = null)
            => StandardConnectionHelper.StandardConnectionWrapper<T>(connectionName, actionWrapper, exceptionHandler);

        /// <summary>
        /// Loads a foreign key field for the specified target model using the provided predicate and optional
        /// constraints.
        /// </summary>
        /// <remarks>This method is typically used to load a foreign key relationship for a model
        /// instance, optionally applying additional constraints or using a custom data loader to retrieve the related
        /// data.</remarks>
        /// <typeparam name="T">The type of the target model. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model representing the foreign key. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the database context.</param>
        /// <param name="target">The target model instance for which the foreign key field is being loaded.</param>
        /// <param name="predicate">An expression specifying the foreign key property on the target model.</param>
        /// <param name="customDataLoader">An optional custom data loader to retrieve the related data.</param>
        /// <param name="additionalConstraints">An optional expression specifying additional constraints to apply when loading the foreign key field.</param>
        /// <returns>The first related model that matches the specified predicate and constraints, or <see langword="null"/> if
        /// no match is found.</returns>
        public static T LoadForeignKeyField<T, R, S>(RelmContextOptionsBuilder relmContextOptionsBuilder, T target, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader, additionalConstraints).FirstOrDefault();

        /// <summary>
        /// Loads a foreign key field for the specified target model using the provided predicate and data loader.
        /// </summary>
        /// <remarks>This method uses a <see cref="ForeignKeyLoader{T}"/> to load the foreign key field
        /// for the specified target model. Ensure that the provided <paramref name="predicate"/> correctly identifies
        /// the foreign key property to be loaded.</remarks>
        /// <typeparam name="T">The type of the target model. Must implement <see cref="IRelmModel"/> and have a parameterless constructor.</typeparam>
        /// <typeparam name="R">The type of the related model referenced by the foreign key. Must implement <see cref="IRelmModel"/> and
        /// have a parameterless constructor.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader. Must implement <see cref="IRelmModel"/> and have a
        /// parameterless constructor.</typeparam>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the database context.</param>
        /// <param name="target">The target model instance for which the foreign key field is being loaded.</param>
        /// <param name="predicate">An expression specifying the foreign key property to load.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related data.</param>
        /// <returns>The first related model of type <typeparamref name="R"/> that matches the foreign key, or <see
        /// langword="null"/> if no match is found.</returns>
        public static T LoadForeignKeyField<T, R, S>(RelmContextOptionsBuilder relmContextOptionsBuilder, T target, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader).FirstOrDefault();

        /// <summary>
        /// Loads a foreign key field for the specified target model, applying the given predicate and additional
        /// constraints.
        /// </summary>
        /// <remarks>This method is typically used to load a related model for a given target model based
        /// on a foreign key relationship. The method applies the specified predicate and additional constraints to
        /// filter the related models.</remarks>
        /// <typeparam name="T">The type of the target model. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the database context.</param>
        /// <param name="target">The target model instance for which the foreign key field is being loaded.</param>
        /// <param name="predicate">An expression specifying the foreign key relationship between the target and related models.</param>
        /// <param name="additionalConstraints">An optional expression specifying additional constraints to apply when loading the related model.</param>
        /// <returns>The first related model that matches the specified predicate and additional constraints, or <see
        /// langword="null"/> if no match is found.</returns>
        public static T LoadForeignKeyField<T, R>(RelmContextOptionsBuilder relmContextOptionsBuilder, T target, Expression<Func<T, R>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, additionalConstraints).FirstOrDefault();

        /// <summary>
        /// Loads a foreign key field for the specified target entity using the provided predicate.
        /// </summary>
        /// <remarks>This method uses a <see cref="ForeignKeyLoader{T}"/> to load the foreign key field
        /// for the specified target entity.</remarks>
        /// <typeparam name="T">The type of the target entity. Must implement <see cref="IRelmModel"/> and have a parameterless constructor.</typeparam>
        /// <typeparam name="R">The type of the related entity. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the database context.</param>
        /// <param name="target">The target entity for which the foreign key field is to be loaded.</param>
        /// <param name="predicate">An expression specifying the foreign key field to load.</param>
        /// <returns>The first related entity of type <typeparamref name="R"/> that matches the foreign key field, or <see
        /// langword="null"/> if no match is found.</returns>
        public static T LoadForeignKeyField<T, R>(RelmContextOptionsBuilder relmContextOptionsBuilder, T target, Expression<Func<T, R>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate).FirstOrDefault();

        /// <summary>
        /// Loads a foreign key field for the specified target model, applying the given predicate, custom data loader,
        /// and additional constraints.
        /// </summary>
        /// <remarks>This method is typically used to load a foreign key field for a model in scenarios
        /// where additional constraints or a custom data loader are required.</remarks>
        /// <typeparam name="T">The type of the target model that contains the foreign key field. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model in the foreign key relationship. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the database context for the operation.</param>
        /// <param name="target">The target model instance for which the foreign key field will be loaded.</param>
        /// <param name="predicate">An expression specifying the foreign key field to load as a collection of related entities.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related entities.</param>
        /// <param name="additionalConstraints">An expression specifying additional constraints to apply when loading the related entities.</param>
        /// <returns>The first related entity that matches the specified predicate and constraints, or <see langword="null"/> if
        /// no match is found.</returns>
        public static T LoadForeignKeyField<T, R, S>(RelmContextOptionsBuilder relmContextOptionsBuilder, T target, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader, additionalConstraints).FirstOrDefault();

        /// <summary>
        /// Loads a foreign key field for the specified target entity using the provided predicate and custom data
        /// loader.
        /// </summary>
        /// <remarks>This method uses a custom data loader to retrieve the related entities for the
        /// specified foreign key field. The first entity in the resulting collection is returned. If the collection is
        /// empty, the method returns <see langword="null"/>.</remarks>
        /// <typeparam name="T">The type of the target entity. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related entity in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the entity used by the custom data loader. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the database context.</param>
        /// <param name="target">The target entity for which the foreign key field is being loaded.</param>
        /// <param name="predicate">An expression specifying the collection navigation property on the target entity.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related data.</param>
        /// <returns>The first related entity from the loaded foreign key collection, or <see langword="null"/> if no related
        /// entities are found.</returns>
        public static T LoadForeignKeyField<T, R, S>(RelmContextOptionsBuilder relmContextOptionsBuilder, T target, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader).FirstOrDefault();

        /// <summary>
        /// Loads a foreign key field for the specified target entity, applying the given predicate and additional
        /// constraints.
        /// </summary>
        /// <typeparam name="T">The type of the target entity, which must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related entity, which must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the database context.</param>
        /// <param name="target">The target entity for which the foreign key field is being loaded.</param>
        /// <param name="predicate">An expression specifying the collection navigation property on the target entity.</param>
        /// <param name="additionalConstraints">An expression specifying additional constraints to apply when loading the related entities.</param>
        /// <returns>The first related entity that matches the specified predicate and constraints, or <see langword="null"/> if
        /// no match is found.</returns>
        public static T LoadForeignKeyField<T, R>(RelmContextOptionsBuilder relmContextOptionsBuilder, T target, Expression<Func<T, ICollection<R>>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, additionalConstraints).FirstOrDefault();

        /// <summary>
        /// Loads a foreign key field for the specified target entity and retrieves the first related entity matching
        /// the given predicate.
        /// </summary>
        /// <remarks>This method is typically used to load and access a specific related entity in a
        /// foreign key relationship. Ensure that the <paramref name="relmContextOptionsBuilder"/> is properly
        /// configured to access the database context.</remarks>
        /// <typeparam name="T">The type of the target entity, which must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related entity, which must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the database context.</param>
        /// <param name="target">The target entity for which the foreign key field is being loaded.</param>
        /// <param name="predicate">An expression specifying the collection navigation property on the target entity that represents the foreign
        /// key relationship.</param>
        /// <returns>The first related entity matching the specified predicate, or <see langword="null"/> if no related entities
        /// are found.</returns>
        public static T LoadForeignKeyField<T, R>(RelmContextOptionsBuilder relmContextOptionsBuilder, T target, Expression<Func<T, ICollection<R>>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate).FirstOrDefault();

        /// <summary>
        /// Loads and resolves a foreign key field for a collection of entities, applying optional constraints and using
        /// a custom data loader.
        /// </summary>
        /// <remarks>This method is designed to simplify the process of resolving foreign key
        /// relationships for a collection of entities. It allows for the use of a custom data loader and additional
        /// constraints to tailor the loading process to specific requirements.</remarks>
        /// <typeparam name="T">The type of the primary entity in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related entity referenced by the foreign key. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the entity used by the custom data loader. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the database context for loading the foreign key field.</param>
        /// <param name="target">The collection of primary entities for which the foreign key field will be loaded.</param>
        /// <param name="predicate">An expression specifying the foreign key relationship between the primary entity and the related entity.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related entities.</param>
        /// <param name="additionalConstraints">An optional expression specifying additional constraints to apply when resolving the foreign key field.</param>
        /// <returns>A collection of the primary entities with the foreign key field resolved and loaded.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R, S>(RelmContextOptionsBuilder relmContextOptionsBuilder, ICollection<T> target, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader, additionalConstraints);

        /// <summary>
        /// Loads and resolves a foreign key field for a collection of entities, using the specified predicate and data
        /// loader.
        /// </summary>
        /// <remarks>This method uses a <see cref="ForeignKeyLoader{T}"/> to load and resolve the foreign
        /// key field for the specified collection of entities. The <paramref name="customDataLoader"/> allows for
        /// custom logic to be applied when fetching the related entities.</remarks>
        /// <typeparam name="T">The type of the primary entity in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related entity referenced by the foreign key. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the entity used by the custom data loader. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the database context for loading related data.</param>
        /// <param name="target">The collection of primary entities for which the foreign key field will be loaded.</param>
        /// <param name="predicate">An expression specifying the foreign key relationship to be resolved.</param>
        /// <param name="customDataLoader">A custom data loader used to fetch the related entities.</param>
        /// <returns>A collection of the primary entities with the foreign key field resolved.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R, S>(RelmContextOptionsBuilder relmContextOptionsBuilder, ICollection<T> target, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader);

        /// <summary>
        /// Loads and resolves a foreign key field for a collection of entities, applying the specified constraints.
        /// </summary>
        /// <remarks>This method uses a <see cref="ForeignKeyLoader{T}"/> to resolve and load the foreign
        /// key field for the specified collection of entities. The <paramref name="predicate"/> defines the
        /// relationship between the primary and related entities, while the  <paramref name="additionalConstraints"/>
        /// can be used to further filter or constrain the related entities.</remarks>
        /// <typeparam name="T">The type of the primary entity in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related entity referenced by the foreign key. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the database context for the operation.</param>
        /// <param name="target">The collection of primary entities for which the foreign key field will be loaded.</param>
        /// <param name="predicate">An expression specifying the foreign key relationship between the primary and related entities.</param>
        /// <param name="additionalConstraints">An optional expression specifying additional constraints to apply when resolving the foreign key field.</param>
        /// <returns>A collection of the primary entities with the foreign key field resolved and loaded.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R>(RelmContextOptionsBuilder relmContextOptionsBuilder, ICollection<T> target, Expression<Func<T, R>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, additionalConstraints);

        /// <summary>
        /// Loads and resolves a foreign key field for a collection of entities.
        /// </summary>
        /// <remarks>This method uses a <see cref="ForeignKeyLoader{T}"/> to load the foreign key field
        /// for the specified collection. The <paramref name="predicate"/> parameter determines which foreign key field
        /// is resolved.</remarks>
        /// <typeparam name="T">The type of the entity in the target collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related entity being loaded. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the database context.</param>
        /// <param name="target">The collection of entities for which the foreign key field will be loaded.</param>
        /// <param name="predicate">An expression specifying the foreign key field to load.</param>
        /// <returns>A collection of entities of type <typeparamref name="T"/> with the specified foreign key field resolved.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R>(RelmContextOptionsBuilder relmContextOptionsBuilder, ICollection<T> target, Expression<Func<T, R>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate);

        /// <summary>
        /// Loads and populates a foreign key field for a collection of entities, applying optional constraints and
        /// using a custom data loader.
        /// </summary>
        /// <remarks>This method is designed to simplify the process of loading related entities for a
        /// collection of primary entities. It supports applying additional constraints to filter the related entities
        /// and allows the use of a custom data loader for advanced scenarios. The method ensures that the foreign key
        /// field specified by <paramref name="predicate"/> is populated for each entity in the <paramref
        /// name="target"/> collection.</remarks>
        /// <typeparam name="T">The type of the primary entity in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related entity in the foreign key relationship. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the entity used by the custom data loader. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the database context for the operation.</param>
        /// <param name="target">The collection of primary entities for which the foreign key field will be loaded. Cannot be <see
        /// langword="null"/>.</param>
        /// <param name="predicate">An expression specifying the foreign key field to load for each entity in the collection.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related entities. Cannot be <see langword="null"/>.</param>
        /// <param name="additionalConstraints">An optional expression specifying additional constraints to apply when loading the related entities.</param>
        /// <returns>A collection of the primary entities with the specified foreign key field populated.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R, S>(RelmContextOptionsBuilder relmContextOptionsBuilder, ICollection<T> target, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader, additionalConstraints);

        /// <summary>
        /// Loads a foreign key field for a collection of entities, using the specified predicate and custom data
        /// loader.
        /// </summary>
        /// <remarks>This method facilitates the loading of related entities for a collection of primary
        /// entities by leveraging a custom data loader. The <paramref name="predicate"/> defines the navigation
        /// property representing the foreign key relationship, and the <paramref name="customDataLoader"/> provides the
        /// mechanism for retrieving the related data.</remarks>
        /// <typeparam name="T">The type of the primary entity in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related entity in the foreign key relationship. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the entity used by the custom data loader. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the database context.</param>
        /// <param name="target">The collection of primary entities for which the foreign key field will be loaded.</param>
        /// <param name="predicate">An expression specifying the foreign key field to load for each entity in the collection.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related entities.</param>
        /// <returns>A collection of the primary entities with the specified foreign key field loaded.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R, S>(RelmContextOptionsBuilder relmContextOptionsBuilder, ICollection<T> target, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader);

        /// <summary>
        /// Loads and populates a foreign key collection field for the specified target entities.
        /// </summary>
        /// <remarks>This method uses the provided <paramref name="relmContextOptionsBuilder"/> to
        /// configure the database context  and loads the related entities for the specified foreign key field. The
        /// <paramref name="additionalConstraints"/>  parameter can be used to filter or constrain the related entities
        /// being loaded.</remarks>
        /// <typeparam name="T">The type of the target entities that contain the foreign key collection.</typeparam>
        /// <typeparam name="R">The type of the related entities in the foreign key collection.</typeparam>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the database context.</param>
        /// <param name="target">The collection of target entities for which the foreign key field will be loaded.</param>
        /// <param name="predicate">An expression specifying the foreign key collection property to load.</param>
        /// <param name="additionalConstraints">An expression defining additional constraints to apply when loading the related entities.</param>
        /// <returns>A collection of the target entities with the specified foreign key field populated.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R>(RelmContextOptionsBuilder relmContextOptionsBuilder, ICollection<T> target, Expression<Func<T, ICollection<R>>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, additionalConstraints);

        /// <summary>
        /// Loads a foreign key field for a collection of entities, resolving the related entities based on the
        /// specified predicate.
        /// </summary>
        /// <remarks>This method uses the provided <paramref name="relmContextOptionsBuilder"/> to
        /// configure the database context and resolves the foreign key relationship defined by <paramref
        /// name="predicate"/> for the given <paramref name="target"/> collection.</remarks>
        /// <typeparam name="T">The type of the primary entity. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related entity. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the database context.</param>
        /// <param name="target">The collection of primary entities for which the foreign key field will be loaded.</param>
        /// <param name="predicate">An expression specifying the foreign key relationship to load.</param>
        /// <returns>A collection of the primary entities with the specified foreign key field loaded.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R>(RelmContextOptionsBuilder relmContextOptionsBuilder, ICollection<T> target, Expression<Func<T, ICollection<R>>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate);

        /// <summary>
        /// Loads a specified field for a collection of models using a DataLoader pattern.
        /// </summary>
        /// <remarks>This method uses a DataLoader pattern to efficiently load the specified field for all
        /// models in the collection. It is designed to minimize database queries by batching and caching
        /// operations.</remarks>
        /// <typeparam name="T">The type of the model in the collection. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <typeparam name="R">The type of the field to be loaded.</typeparam>
        /// <param name="relmContext">The context used to interact with the data source.</param>
        /// <param name="target">The collection of models for which the field will be loaded. Cannot be null.</param>
        /// <param name="predicate">An expression specifying the field to load for each model in the collection. Cannot be null.</param>
        /// <returns>The updated collection of models with the specified field loaded.</returns>
        public static ICollection<T> LoadDataLoaderField<T, R>(IRelmContext relmContext, ICollection<T> target, Expression<Func<T, R>> predicate) where T : IRelmModel, new()
            => new DataLoaderHelper<T>(relmContext, target).LoadField(predicate);

        /// <summary>
        /// Loads a specified field of a data model using a DataLoader and returns the first result.
        /// </summary>
        /// <remarks>This method uses a DataLoader to load the specified field of the target data model. 
        /// If multiple results are loaded, only the first result is returned.</remarks>
        /// <typeparam name="T">The type of the data model, which must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <typeparam name="R">The type of the field to be loaded.</typeparam>
        /// <param name="relmContext">The context used to interact with the data source.</param>
        /// <param name="target">The target data model instance whose field is to be loaded.</param>
        /// <param name="predicate">An expression specifying the field to load.</param>
        /// <returns>The first result of the loaded field, or the default value of <typeparamref name="R"/> if no results are
        /// found.</returns>
        public static T LoadDataLoaderField<T, R>(IRelmContext relmContext, T target, Expression<Func<T, R>> predicate) where T : IRelmModel, new()
            => new DataLoaderHelper<T>(relmContext, target).LoadField(predicate).FirstOrDefault();

        /// <summary>
        /// Loads a specified field of a data model using a DataLoader and returns the first result.
        /// </summary>
        /// <remarks>This method uses a DataLoader to load the specified field of the target data model. 
        /// If multiple results are loaded, only the first result is returned.</remarks>
        /// <typeparam name="T">The type of the data model, which must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <typeparam name="R">The type of the field to be loaded.</typeparam>
        /// <param name="relmQuickContext">The context used to interact with the data source.</param>
        /// <param name="target">The target data model instance whose field is to be loaded.</param>
        /// <param name="predicate">An expression specifying the field to load from the data model.</param>
        /// <returns>The first result of the loaded field, or the default value of <typeparamref name="R"/> if no results are
        /// found.</returns>
        public static T LoadDataLoaderField<T, R>(IRelmQuickContext relmQuickContext, T target, Expression<Func<T, R>> predicate) where T : IRelmModel, new()
            => new DataLoaderHelper<T>(relmQuickContext, target).LoadField(predicate).FirstOrDefault();

        /// <summary>
        /// Writes the specified model to the database using the provided context and configuration options.
        /// </summary>
        /// <remarks>This method delegates the write operation to the <paramref name="relmModel"/>
        /// implementation,  which performs the actual database interaction. The behavior of the write operation is
        /// influenced  by the provided configuration flags.</remarks>
        /// <param name="relmContext">The database context used to perform the write operation.</param>
        /// <param name="relmModel">The model to be written to the database.</param>
        /// <param name="batchSize">The number of records to process in a single batch. Defaults to 100.  Must be a positive integer.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether columns with auto-increment constraints are allowed to be written.  Defaults to
        /// <see langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are allowed to be written.  Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowUniqueColumns">A value indicating whether columns with unique constraints are allowed to be written.  Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether columns with automatic date generation are allowed to be written.  Defaults to
        /// <see langword="false"/>.</param>
        /// <returns>The number of records successfully written to the database.</returns>
        public static int WriteToDatabase(IRelmContext relmContext, IRelmModel relmModel, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
            => relmModel.WriteToDatabase(relmContext, batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
    
        /// <summary>
        /// Writes the specified model to the database using the provided context and configuration options.
        /// </summary>
        /// <remarks>This method provides fine-grained control over which types of columns are included in
        /// the database write operation. Use the configuration options to tailor the behavior to your specific
        /// requirements.</remarks>
        /// <param name="relmQuickContext">The database context used to manage the connection and transaction for the operation.</param>
        /// <param name="relmModel">The model to be written to the database. This model must conform to the expected schema.</param>
        /// <param name="batchSize">The number of records to process in a single batch. Defaults to 100. Must be a positive integer.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether columns with auto-increment constraints are allowed to be written. If <see
        /// langword="true"/>, auto-increment columns will be included in the operation; otherwise, they will be
        /// excluded.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are allowed to be written. If <see langword="true"/>, primary
        /// key columns will be included in the operation; otherwise, they will be excluded.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns are allowed to be written. If <see langword="true"/>, unique
        /// columns will be included in the operation; otherwise, they will be excluded.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether columns with automatic date generation constraints are allowed to be written. If
        /// <see langword="true"/>, auto-date columns will be included in the operation; otherwise, they will be
        /// excluded.</param>
        /// <returns>The number of records successfully written to the database.</returns>
        public static int WriteToDatabase(IRelmQuickContext relmQuickContext, IRelmModel relmModel, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
            => relmModel.WriteToDatabase(relmQuickContext, batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
    
        /// <summary>
        /// Writes a collection of models to the specified database context in batches.
        /// </summary>
        /// <remarks>This method writes the provided models to the database in batches to optimize
        /// performance.  The behavior of the write operation can be customized using the optional parameters to control
        /// whether specific column types (e.g., auto-increment, primary key, unique, or auto-generated date columns) 
        /// are included in the operation.</remarks>
        /// <param name="relmContext">The database context to which the models will be written. This parameter cannot be <see langword="null"/>.</param>
        /// <param name="relmModels">The collection of models to write to the database. This parameter cannot be <see langword="null"/> or empty.</param>
        /// <param name="batchSize">The number of models to include in each batch. Must be a positive integer. The default value is 100.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns are allowed to be written. If <see langword="true"/>,
        /// auto-increment columns will be included; otherwise, they will be excluded. The default value is <see
        /// langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are allowed to be written. If <see langword="true"/>, primary
        /// key columns will be included; otherwise, they will be excluded. The default value is <see
        /// langword="false"/>.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns are allowed to be written. If <see langword="true"/>, unique
        /// columns will be included; otherwise, they will be excluded. The default value is <see langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether auto-generated date columns are allowed to be written. If <see langword="true"/>,
        /// auto-generated date columns will be included; otherwise, they will be excluded. The default value is <see
        /// langword="false"/>.</param>
        /// <returns>The total number of models successfully written to the database.</returns>
        public static int WriteToDatabase(IRelmContext relmContext, IEnumerable<IRelmModel> relmModels, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
            => relmModels.WriteToDatabase(relmContext, batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
    
        /// <summary>
        /// Writes a collection of Relm models to the database using the specified context and configuration options.
        /// </summary>
        /// <remarks>This method provides fine-grained control over how data is written to the database by
        /// allowing the caller to specify whether certain types of columns (e.g., auto-increment, primary key, unique,
        /// or auto-date columns) are included in the operation. Use caution when enabling these options, as they may
        /// violate database constraints or lead to unexpected behavior.</remarks>
        /// <param name="relmQuickContext">The database context used to manage the connection and transaction for the operation.</param>
        /// <param name="relmModels">The collection of models to be written to the database. Each model represents a row to be inserted or
        /// updated.</param>
        /// <param name="batchSize">The number of models to process in a single batch. Defaults to 100. Larger batch sizes may improve
        /// performance but require more memory.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether columns marked as auto-increment are allowed to be written. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are allowed to be written. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowUniqueColumns">A value indicating whether columns with unique constraints are allowed to be written. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether columns with automatic date generation (e.g., timestamps) are allowed to be
        /// written. Defaults to <see langword="false"/>.</param>
        /// <returns>The total number of rows successfully written to the database.</returns>
        public static int WriteToDatabase(IRelmQuickContext relmQuickContext, IEnumerable<IRelmModel> relmModels, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
            => relmModels.WriteToDatabase(relmQuickContext, batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);

        /*
        /// <summary>
        /// Configures the library to use the specified <see cref="IConfiguration"/> instance for application settings
        /// and options.
        /// </summary>
        /// <remarks>Call this method at application startup to ensure the library uses the correct
        /// configuration source. This method should be invoked before performing any operations that depend on
        /// configuration values.</remarks>
        /// <param name="configuration">The <see cref="IConfiguration"/> instance that provides configuration values for the library. Cannot be
        /// null.</param>
        public static void UseConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
            ConnectionHelper.UseConfiguration(_configuration);
        }
        */
    }
}
