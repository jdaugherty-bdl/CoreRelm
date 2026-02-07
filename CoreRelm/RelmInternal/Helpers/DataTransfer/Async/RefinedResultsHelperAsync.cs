using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.RelmInternal.Helpers.Utilities;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.DataTransfer.Async
{
    internal class RefinedResultsHelperAsync
    {
        /// <summary>
        /// Executes a query asynchronously and retrieves a single scalar value of the specified type.
        /// </summary>
        /// <remarks>This method establishes a database connection based on the specified <paramref
        /// name="connectionName"/>  and executes the provided query to retrieve a single scalar value. Ensure that the
        /// query is designed  to return exactly one value; otherwise, an exception may occur.</remarks>
        /// <typeparam name="T">The type of the scalar value to retrieve.</typeparam>
        /// <param name="connectionName">The name of the connection to use, represented as an <see cref="Enum"/>.</param>
        /// <param name="query">The SQL query to execute. Must be a valid SQL statement that returns a single scalar value.</param>
        /// <param name="parameters">An optional dictionary of parameters to include in the query. Keys represent parameter names, and values
        /// represent parameter values. Can be <see langword="null"/> if no parameters are needed.</param>
        /// <param name="throwException">A value indicating whether to throw an exception if an error occurs. If <see langword="true"/>, exceptions
        /// will be thrown; otherwise, errors will be suppressed.</param>
        /// <param name="allowUserVariables">A value indicating whether user-defined variables are allowed in the connection. Defaults to <see
        /// langword="false"/>.</param>
        /// <returns>The scalar value of type <typeparamref name="T"/> returned by the query.</returns>
        internal static async Task<T?> GetScalarAsync<T>(Enum connectionName, string query, Dictionary<string, object>? parameters = null, bool throwException = true, bool allowUserVariables = false, CancellationToken cancellationToken = default)
        {
            using var conn = (RelmHelper.ConnectionHelper?.GetConnectionFromType(connectionName, allowUserVariables))
                ?? throw new InvalidOperationException($"Could not get a valid connection for connection type '{connectionName}'.");

            return await GetScalarAsync<T>(conn, query, parameters, throwException: throwException, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Executes a scalar query and retrieves the result as the specified type.
        /// </summary>
        /// <remarks>This method is a wrapper for executing scalar queries using a provided database
        /// connection and optional transaction.  Ensure that the connection is open before calling this method. If
        /// <paramref name="throwException"/> is set to <see langword="false"/>,  any exceptions encountered during
        /// query execution will be suppressed, and the default value of <typeparamref name="T"/> will be
        /// returned.</remarks>
        /// <typeparam name="T">The type to which the scalar result will be converted.</typeparam>
        /// <param name="establishedConnection">An established <see cref="MySqlConnection"/> to the database. Must not be null.</param>
        /// <param name="query">The SQL query string to execute. Must not be null or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to include in the query. Can be null if no parameters
        /// are required.</param>
        /// <param name="throwException">Indicates whether an exception should be thrown if the query fails. If <see langword="true"/>, exceptions
        /// will be propagated; otherwise, the method will suppress exceptions and return the default value of
        /// <typeparamref name="T"/>.</param>
        /// <returns>The scalar result of the query, converted to the specified type <typeparamref name="T"/>. Returns the
        /// default value of <typeparamref name="T"/> if the query produces no result or if <paramref
        /// name="throwException"/> is <see langword="false"/> and an error occurs.</returns>
        internal static async Task<T?> GetScalarAsync<T>(MySqlConnection establishedConnection, string query, Dictionary<string, object>? parameters = null, bool throwException = true, CancellationToken cancellationToken = default)
        {
            return await GetScalarAsync<T>(new RelmContext(establishedConnection), query, parameters, throwException: throwException, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Executes a scalar query and retrieves the result as the specified type.
        /// </summary>
        /// <remarks>This method is a wrapper for executing scalar queries using a provided database
        /// connection and optional transaction.  Ensure that the connection is open before calling this method. If
        /// <paramref name="throwException"/> is set to <see langword="false"/>,  any exceptions encountered during
        /// query execution will be suppressed, and the default value of <typeparamref name="T"/> will be
        /// returned.</remarks>
        /// <typeparam name="T">The type to which the scalar result will be converted.</typeparam>
        /// <param name="establishedConnection">An established <see cref="MySqlConnection"/> to the database. Must not be null.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the query. Can be null if no transaction is
        /// used.</param>
        /// <param name="query">The SQL query string to execute. Must not be null or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to include in the query. Can be null if no parameters
        /// are required.</param>
        /// <param name="throwException">Indicates whether an exception should be thrown if the query fails. If <see langword="true"/>, exceptions
        /// will be propagated; otherwise, the method will suppress exceptions and return the default value of
        /// <typeparamref name="T"/>.</param>
        /// <returns>The scalar result of the query, converted to the specified type <typeparamref name="T"/>. Returns the
        /// default value of <typeparamref name="T"/> if the query produces no result or if <paramref
        /// name="throwException"/> is <see langword="false"/> and an error occurs.</returns>
        internal static async Task<T?> GetScalarAsync<T>(MySqlConnection establishedConnection, MySqlTransaction sqlTransaction, string query, Dictionary<string, object>? parameters = null, bool throwException = true, CancellationToken cancellationToken = default)
        {
            return await GetScalarAsync<T>(new RelmContext(establishedConnection, sqlTransaction), query, parameters, throwException: throwException, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Executes a SQL query asynchronously and returns the first column of the first row in the result set, cast to
        /// the specified type.
        /// </summary>
        /// <typeparam name="T">The type to which the result is cast. Must be compatible with the value returned by the query.</typeparam>
        /// <param name="relmContext">The database context used to execute the query. Cannot be null.</param>
        /// <param name="query">The SQL query to execute. Must be a valid scalar query.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be applied to the query. Can be null if the query
        /// does not require parameters.</param>
        /// <param name="throwException">true to throw an exception if the query fails; otherwise, false to suppress exceptions and return the
        /// default value of T.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the value of the first column of
        /// the first row in the result set, cast to type T. Returns the default value of T if no result is found or if
        /// throwException is false and an error occurs.</returns>
        internal async static Task<T?> GetScalarAsync<T>(IRelmContext relmContext, string query, Dictionary<string, object>? parameters = null, bool throwException = true, CancellationToken cancellationToken = default)
        {
            return await DatabaseWorkHelper.DoDatabaseWorkAsync(relmContext, query,
                async (cmd, cancellationToken) =>
                {
                    return await RunScalarCommandAsync<T?>(cmd, parameters: parameters, cancellationToken: cancellationToken);
                },
                throwException: throwException, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Executes the specified MySqlCommand asynchronously and returns the result of the first column in the first
        /// row, converted to the specified type.
        /// </summary>
        /// <remarks>The command must be associated with an open MySQL connection. This method is
        /// typically used for queries that return a single value, such as aggregate functions or identity values after
        /// an insert.</remarks>
        /// <typeparam name="T">The type to which the scalar result will be converted before being returned.</typeparam>
        /// <param name="cmd">The MySqlCommand to execute. Must be configured with a valid SQL statement and connection.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to add to the command before execution. Can be null if
        /// no parameters are required.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the scalar value returned by the
        /// command, converted to type T. If the result is DBNull or cannot be converted, the default value for type T
        /// is returned.</returns>
        private async static Task<T?> RunScalarCommandAsync<T>(MySqlCommand cmd, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
        {
            if (parameters != null)
                cmd.Parameters.AddAllParameters(parameters);

            var scalarResult = await cmd.ExecuteScalarAsync(cancellationToken);

            return (T?)CoreUtilities.ConvertScalar<T>(scalarResult);
        }

        /// <summary>
        /// Asynchronously executes the specified query on the database connection associated with the given connection name  and
        /// retrieves the first row of the result set.
        /// </summary>
        /// <remarks>This method uses the connection associated with the specified <paramref
        /// name="connectionName"/> to execute  the query. If <paramref name="parameters"/> are provided, they are
        /// applied to the query as named parameters.</remarks>
        /// <param name="connectionName">The name of the database connection, represented as an <see cref="Enum"/>.</param>
        /// <param name="query">The SQL query to execute. Must be a valid SQL statement.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query.  If null, no parameters are
        /// applied.</param>
        /// <param name="throwException">A boolean value indicating whether an exception should be thrown if the query  fails. If <see
        /// langword="true"/>, an exception is thrown on failure; otherwise, the method returns null.</param>
        /// <param name="allowUserVariables">A boolean value indicating whether user-defined variables are allowed  in the database connection. Defaults
        /// to <see langword="false"/>.</param>
        /// <returns>A <see cref="DataRow"/> representing the first row of the result set, or <see langword="null"/> if no rows 
        /// are returned or if <paramref name="throwException"/> is <see langword="false"/> and an error occurs.</returns>
        internal static async Task<DataRow?> GetDataRowAsync(Enum connectionName, string query, Dictionary<string, object>? parameters = null, bool throwException = true, bool allowUserVariables = false, CancellationToken cancellationToken = default)
        {
            using var conn = (RelmHelper.ConnectionHelper?.GetConnectionFromType(connectionName, allowUserVariables))
                ?? throw new InvalidOperationException($"Could not get a valid connection for connection type '{connectionName}'.");

            return await GetDataRowAsync(conn, query, parameters, throwException: throwException, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Asynchronously executes the specified query and retrieves the first row of the result set.
        /// </summary>
        /// <remarks>This method is a convenience wrapper for retrieving a single row from the result set
        /// of a query. If the query returns multiple rows, only the first row is returned. If no rows are returned, the
        /// method returns <see langword="null"/>.</remarks>
        /// <param name="establishedConnection">An established <see cref="MySqlConnection"/> to the database.</param>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. Can be <see langword="null"/>
        /// if no parameters are required.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if an error occurs during query execution. If <see
        /// langword="false"/>, the method will suppress exceptions and return <see langword="null"/> in case of an
        /// error.</param>
        /// <returns>The first <see cref="DataRow"/> from the result set if the query returns any rows; otherwise, <see
        /// langword="null"/>.</returns>
        internal static async Task<DataRow?> GetDataRowAsync(MySqlConnection establishedConnection, string query, Dictionary<string, object>? parameters = null, bool throwException = true, CancellationToken cancellationToken = default)
        {
            var intermediate = await GetDataTableAsync(establishedConnection, query, parameters: parameters, throwException: throwException, cancellationToken: cancellationToken);

            return (intermediate?.Rows.Count ?? 0) > 0 ? intermediate!.Rows[0] : null;
        }

        /// <summary>
        /// Executes the specified query and retrieves the first row of the result set.
        /// </summary>
        /// <remarks>This method is a convenience wrapper for retrieving a single row from the result set
        /// of a query. If the query returns multiple rows, only the first row is returned. If no rows are returned, the
        /// method returns <see langword="null"/>.</remarks>
        /// <param name="establishedConnection">An established <see cref="MySqlConnection"/> to the database.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the query. Can be <see langword="null"/> if no
        /// transaction is required.</param>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. Can be <see langword="null"/>
        /// if no parameters are required.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if an error occurs during query execution. If <see
        /// langword="false"/>, the method will suppress exceptions and return <see langword="null"/> in case of an
        /// error.</param>
        /// <returns>The first <see cref="DataRow"/> from the result set if the query returns any rows; otherwise, <see
        /// langword="null"/>.</returns>
        internal static async Task<DataRow?> GetDataRowAsync(MySqlConnection establishedConnection, MySqlTransaction sqlTransaction, string query, Dictionary<string, object>? parameters = null, bool throwException = true, CancellationToken cancellationToken = default)
        {
            var intermediate = await GetDataTableAsync(establishedConnection, sqlTransaction, query, parameters: parameters, throwException: throwException, cancellationToken: cancellationToken);

            return (intermediate?.Rows.Count ?? 0) > 0 ? intermediate!.Rows[0] : null;
        }

        /// <summary>
        /// Executes the specified query and retrieves the first row of the result set as a <see cref="DataRow"/>.
        /// </summary>
        /// <remarks>This method internally calls <c>GetDataTable</c> to execute the query and retrieve
        /// the result set. If the result set contains no rows, the method returns <see langword="null"/>.</remarks>
        /// <param name="relmContext">The database context used to execute the query.</param>
        /// <param name="query">The SQL query to execute. Must not be <see langword="null"/> or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to include in the query. Can be <see langword="null"/>
        /// if no parameters are required.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if an error occurs during query execution. <see
        /// langword="true"/> to throw an exception; otherwise, <see langword="false"/>.</param>
        /// <returns>The first <see cref="DataRow"/> from the result set if the query returns any rows; otherwise, <see
        /// langword="null"/>.</returns>
        internal static async Task<DataRow?> GetDataRowAsync(IRelmContext relmContext, string query, Dictionary<string, object>? parameters = null, bool throwException = true, CancellationToken cancellationToken = default)
        {
            var intermediate = await GetDataTableAsync(relmContext, query, parameters: parameters, throwException: throwException, cancellationToken: cancellationToken);

            return (intermediate?.Rows.Count ?? 0) > 0 ? intermediate!.Rows[0] : null;
        }

        /// <summary>
        /// Executes the specified query on the database connection associated with the given connection name  and
        /// returns the results as a <see cref="DataTable"/>.
        /// </summary>
        /// <remarks>This method establishes a database connection based on the provided <paramref
        /// name="connectionName"/> and executes  the given query. If <paramref name="parameters"/> are provided, they
        /// are added to the query to prevent SQL injection.</remarks>
        /// <param name="connectionName">An <see cref="Enum"/> representing the name or type of the database connection to use.</param>
        /// <param name="query">The SQL query to execute. Must be a valid SQL statement.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to include in the query.  If null, no parameters are
        /// used.</param>
        /// <param name="throwException">A boolean value indicating whether to throw an exception if an error occurs.  If <see langword="true"/>,
        /// exceptions are thrown; otherwise, errors are suppressed.</param>
        /// <param name="allowUserVariables">A boolean value indicating whether user-defined variables are allowed in the connection.  Defaults to <see
        /// langword="false"/>.</param>
        /// <returns>A <see cref="DataTable"/> containing the results of the query. The table will be empty if the query  returns
        /// no rows.</returns>
        internal static async Task<DataTable?> GetDataTableAsync(Enum connectionName, string query, Dictionary<string, object>? parameters = null, bool throwException = true, bool allowUserVariables = false, CancellationToken cancellationToken = default)
        {
            using var conn = (RelmHelper.ConnectionHelper?.GetConnectionFromType(connectionName, allowUserVariables))
                ?? throw new InvalidOperationException($"Could not get a valid connection for connection type '{connectionName}'.");

            return await GetDataTableAsync(conn, query, parameters, throwException: throwException, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Executes the specified SQL query and returns the results as a <see cref="DataTable"/>.
        /// </summary>
        /// <remarks>This method is a convenience wrapper for executing SQL queries and retrieving their
        /// results as a <see cref="DataTable"/>. Ensure that the provided connection is open and valid before calling
        /// this method. If <paramref name="throwException"/> is set to <see langword="false"/>, any errors during
        /// execution will be suppressed, and an empty <see cref="DataTable"/> will be returned instead.</remarks>
        /// <param name="establishedConnection">An established <see cref="MySqlConnection"/> to the database. The connection must be open before calling
        /// this method.</param>
        /// <param name="query">The SQL query to execute. The query can include parameter placeholders.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. If null, no parameters are
        /// applied.</param>
        /// <param name="throwException">A boolean value indicating whether to throw an exception if an error occurs. If <see langword="false"/>, the
        /// method suppresses exceptions and returns an empty <see cref="DataTable"/> on failure.</param>
        /// <returns>A <see cref="DataTable"/> containing the results of the query. If the query returns no rows, the <see
        /// cref="DataTable"/> will be empty.</returns>
        internal static async Task<DataTable?> GetDataTableAsync(MySqlConnection establishedConnection, string query, Dictionary<string, object>? parameters = null, bool throwException = true, CancellationToken cancellationToken = default)
        {
            return await GetDataTableAsync(new RelmContext(establishedConnection, autoInitializeDataSets: false, autoVerifyTables: false), query, parameters, throwException: throwException, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Executes the specified SQL query asynchronously and returns the results as a <see cref="DataTable"/>.
        /// </summary>
        /// <remarks>This method is a convenience wrapper for executing SQL queries and retrieving their
        /// results as a <see cref="DataTable"/>. Ensure that the provided connection is open and valid before calling
        /// this method. If <paramref name="throwException"/> is set to <see langword="false"/>, any errors during
        /// execution will be suppressed, and an empty <see cref="DataTable"/> will be returned instead.</remarks>
        /// <param name="establishedConnection">An established <see cref="MySqlConnection"/> to the database. The connection must be open before calling
        /// this method.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the query. If null, the query is executed
        /// without a transaction.</param>
        /// <param name="query">The SQL query to execute. The query can include parameter placeholders.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. If null, no parameters are
        /// applied.</param>
        /// <param name="throwException">A boolean value indicating whether to throw an exception if an error occurs. If <see langword="false"/>, the
        /// method suppresses exceptions and returns an empty <see cref="DataTable"/> on failure.</param>
        /// <returns>A <see cref="DataTable"/> containing the results of the query. If the query returns no rows, the <see
        /// cref="DataTable"/> will be empty.</returns>
        internal static async Task<DataTable?> GetDataTableAsync(MySqlConnection establishedConnection, MySqlTransaction sqlTransaction, string query, Dictionary<string, object>? parameters = null, bool throwException = true, CancellationToken cancellationToken = default)
        {
            return await GetDataTableAsync(new RelmContext(establishedConnection, sqlTransaction, autoInitializeDataSets: false, autoVerifyTables: false), query, parameters, throwException: throwException, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Asynchronously executes a SQL query against the specified Relm database context and returns the results as a
        /// <see cref="DataTable"/>.
        /// </summary>
        /// <remarks>If throwException is set to false and an error occurs during query execution, the
        /// method returns null instead of throwing an exception. The caller is responsible for disposing of the
        /// returned DataTable if it is not null.</remarks>
        /// <param name="relmContext">The database context used to execute the query. Cannot be null.</param>
        /// <param name="query">The SQL query to execute. Must be a valid SQL statement supported by the underlying database.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be applied to the query. If null, the query is
        /// executed without parameters.</param>
        /// <param name="throwException">true to throw an exception if an error occurs during execution; otherwise, false to suppress exceptions and
        /// return null.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a DataTable with the query
        /// results, or null if an error occurs and throwException is false.</returns>
        internal async static Task<DataTable?> GetDataTableAsync(IRelmContext relmContext, string query, Dictionary<string, object>? parameters = null, bool throwException = true, CancellationToken cancellationToken = default)
        {
            return await DatabaseWorkHelper.DoDatabaseWorkAsync<DataTable>(relmContext, query,
                async (cmd, cancellationToken) =>
                {
                    if (parameters != null)
                        cmd.Parameters.AddAllParameters(parameters);

                    using (var tableAdapter = new MySqlDataAdapter())
                    {
                        tableAdapter.SelectCommand = cmd;
                        tableAdapter.SelectCommand.CommandType = CommandType.Text;

                        var outputTable = new DataTable();
                        await tableAdapter.FillAsync(outputTable, cancellationToken);

                        return outputTable;
                    }
                }, throwException: throwException, cancellationToken: cancellationToken);
        }
    }
}
