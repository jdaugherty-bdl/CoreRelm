using MySql.Data.MySqlClient;
using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.Models;
using CoreRelm.RelmInternal.Helpers.Operations;
using CoreRelm.RelmInternal.Helpers.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.DataTransfer
{
    internal class RefinedResultsHelper
    {
        /// <summary>
        /// Executes a query and retrieves a single scalar value of the specified type.
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
        internal static T GetScalar<T>(Enum connectionName, string query, Dictionary<string, object> parameters = null, bool throwException = true, bool allowUserVariables = false)
        {
            using (var conn = RelmHelper.ConnectionHelper?.GetConnectionFromType(connectionName, allowUserVariables))
            {
                return GetScalar<T>(conn, query, parameters, throwException: throwException);
            }
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
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the query. Can be null if no transaction is
        /// used.</param>
        /// <returns>The scalar result of the query, converted to the specified type <typeparamref name="T"/>. Returns the
        /// default value of <typeparamref name="T"/> if the query produces no result or if <paramref
        /// name="throwException"/> is <see langword="false"/> and an error occurs.</returns>
        internal static T GetScalar<T>(MySqlConnection establishedConnection, string query, Dictionary<string, object> parameters = null, bool throwException = true, MySqlTransaction sqlTransaction = null)
        {
            return GetScalar<T>(new RelmContext(establishedConnection, sqlTransaction), query, parameters, throwException: throwException);
        }

        /// <summary>
        /// Executes a scalar database query and returns the result as the specified type.
        /// </summary>
        /// <typeparam name="T">The type to which the scalar result will be converted.</typeparam>
        /// <param name="relmContext">The database context used to execute the query. Cannot be <see langword="null"/>.</param>
        /// <param name="query">The SQL query string to execute. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. Can be <see langword="null"/>
        /// if no parameters are required.</param>
        /// <param name="throwException">Indicates whether an exception should be thrown if an error occurs. If <see langword="true"/>, exceptions
        /// will be propagated; otherwise, errors will be suppressed.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to use for the query. Can be <see langword="null"/> if no
        /// transaction is required.</param>
        /// <returns>The scalar result of the query, converted to the specified type <typeparamref name="T"/>.</returns>
        internal static T GetScalar<T>(IRelmContext relmContext, string query, Dictionary<string, object> parameters = null, bool throwException = true, MySqlTransaction sqlTransaction = null)
        {
            return DatabaseWorkHelper.DoDatabaseWork<T>(relmContext, query,
                (cmd) =>
                {
                    return RunScalarCommand<T>(cmd, parameters);
                },
                throwException: throwException, useTransaction: sqlTransaction != null);
        }

        /// <summary>
        /// Executes a scalar database query and returns the result as the specified type.
        /// </summary>
        /// <typeparam name="T">The type to which the scalar result will be converted.</typeparam>
        /// <param name="relmQuickContext">The database context used to execute the query.</param>
        /// <param name="query">The SQL query string to execute. Must be a valid scalar query.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to include in the query. Can be <see langword="null"/>
        /// if no parameters are required.</param>
        /// <param name="throwException">Indicates whether an exception should be thrown if an error occurs. If <see langword="true"/>, exceptions
        /// will be propagated; otherwise, errors will be suppressed.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the query. Can be <see langword="null"/> if no
        /// transaction is used.</param>
        /// <returns>The scalar result of the query, converted to the specified type <typeparamref name="T"/>.</returns>
        internal static T GetScalar<T>(IRelmQuickContext relmQuickContext, string query, Dictionary<string, object> parameters = null, bool throwException = true, MySqlTransaction sqlTransaction = null)
        {
            return DatabaseWorkHelper.DoDatabaseWork<T>(relmQuickContext, query,
                (cmd) =>
                {
                    return RunScalarCommand<T>(cmd, parameters);
                },
                throwException: throwException, useTransaction: sqlTransaction != null);
        }

        /// <summary>
        /// Executes the specified MySQL command and returns the result as a scalar value of the specified type.
        /// </summary>
        /// <remarks>This method adds the provided parameters to the command before execution. Ensure that
        /// the command text and parameters are properly configured to avoid SQL errors.</remarks>
        /// <typeparam name="T">The type to which the scalar result will be converted.</typeparam>
        /// <param name="cmd">The <see cref="MySqlCommand"/> to execute. Must not be <c>null</c>.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to add to the command. Can be <c>null</c>.</param>
        /// <returns>The scalar result of the command, converted to the specified type <typeparamref name="T"/>. If the result is
        /// <c>null</c>, the default value of <typeparamref name="T"/> is returned.</returns>
        private static object RunScalarCommand<T>(MySqlCommand cmd, Dictionary<string, object> parameters = null)
        {
            cmd.Parameters.AddAllParameters(parameters);

            var scalarResult = cmd.ExecuteScalar();

            return CoreUtilities.ConvertScalar<T>(scalarResult);
        }

        /// <summary>
        /// Executes the specified query on the database connection associated with the given connection name  and
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
        internal static DataRow GetDataRow(Enum connectionName, string query, Dictionary<string, object> parameters = null, bool throwException = true, bool allowUserVariables = false)
        {
            using (var conn = RelmHelper.ConnectionHelper?.GetConnectionFromType(connectionName, allowUserVariables))
            {
                return GetDataRow(conn, query, parameters, throwException: throwException);
            }
        }

        /// <summary>
        /// Executes the specified query and retrieves the first row of the result set.
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
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the query. Can be <see langword="null"/> if no
        /// transaction is required.</param>
        /// <returns>The first <see cref="DataRow"/> from the result set if the query returns any rows; otherwise, <see
        /// langword="null"/>.</returns>
        internal static DataRow GetDataRow(MySqlConnection establishedConnection, string query, Dictionary<string, object> parameters = null, bool throwException = true, MySqlTransaction sqlTransaction = null)
        {
            var intermediate = GetDataTable(establishedConnection, query, parameters: parameters, throwException: throwException, sqlTransaction: sqlTransaction);

            return intermediate.Rows.Count > 0 ? intermediate.Rows[0] : null;
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
        internal static DataRow GetDataRow(IRelmContext relmContext, string query, Dictionary<string, object> parameters = null, bool throwException = true)
        {
            var intermediate = GetDataTable(relmContext, query, parameters: parameters, throwException: throwException);

            return intermediate.Rows.Count > 0 ? intermediate.Rows[0] : null;
        }

        /// <summary>
        /// Executes the specified query and retrieves the first row of the result set as a <see cref="DataRow"/>.
        /// </summary>
        /// <remarks>This method is a convenience wrapper for retrieving a single row from a query result.
        /// If multiple rows are returned by the query, only the first row is returned.  Use this method when you expect
        /// at most one row in the result set.</remarks>
        /// <param name="relmQuickContext">The database context used to execute the query.</param>
        /// <param name="query">The SQL query to execute. Must not be null or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query.  If null, no parameters are
        /// applied.</param>
        /// <param name="throwException">A boolean value indicating whether an exception should be thrown if an error occurs during query execution. 
        /// If <see langword="true"/>, an exception is thrown; otherwise, the method returns null in case of an error.</param>
        /// <returns>The first <see cref="DataRow"/> from the result set if the query returns any rows; otherwise, <see
        /// langword="null"/>.</returns>
        internal static DataRow GetDataRow(IRelmQuickContext relmQuickContext, string query, Dictionary<string, object> parameters = null, bool throwException = true)
        {
            var intermediate = GetDataTable(relmQuickContext, query, parameters: parameters, throwException: throwException);

            return intermediate.Rows.Count > 0 ? intermediate.Rows[0] : null;
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
        internal static DataTable GetDataTable(Enum connectionName, string query, Dictionary<string, object> parameters = null, bool throwException = true, bool allowUserVariables = false)
        {
            using (var conn = RelmHelper.ConnectionHelper?.GetConnectionFromType(connectionName, allowUserVariables))
            {
                return GetDataTable(conn, query, parameters, throwException: throwException);
            }
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
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the query. If null, the query is executed
        /// without a transaction.</param>
        /// <returns>A <see cref="DataTable"/> containing the results of the query. If the query returns no rows, the <see
        /// cref="DataTable"/> will be empty.</returns>
        internal static DataTable GetDataTable(MySqlConnection establishedConnection, string query, Dictionary<string, object> parameters = null, bool throwException = true, MySqlTransaction sqlTransaction = null)
        {
            return GetDataTable(new RelmQuickContext(establishedConnection, sqlTransaction), query, parameters, throwException: throwException);
        }

        /// <summary>
        /// Executes the specified SQL query and returns the results as a <see cref="DataTable"/>.
        /// </summary>
        /// <remarks>This method uses a <see cref="MySqlDataAdapter"/> to execute the query and populate
        /// the <see cref="DataTable"/>. Ensure that the query is properly parameterized to avoid SQL injection
        /// vulnerabilities.</remarks>
        /// <param name="relmQuickContext">The database context used to execute the query. This provides the connection and transaction settings.</param>
        /// <param name="query">The SQL query to execute. The query must be a valid SQL statement.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be added to the query. If null, no parameters are
        /// added.</param>
        /// <param name="throwException">A value indicating whether to throw an exception if an error occurs during query execution.  If <see
        /// langword="true"/>, exceptions are thrown; otherwise, errors are suppressed.</param>
        /// <returns>A <see cref="DataTable"/> containing the results of the query. The table will be empty if the query returns
        /// no rows.</returns>
        internal static DataTable GetDataTable(IRelmQuickContext relmQuickContext, string query, Dictionary<string, object> parameters = null, bool throwException = true)
        {
            return DatabaseWorkHelper.DoDatabaseWork<DataTable>(relmQuickContext, query,
                (cmd) =>
                {
                    cmd.Parameters.AddAllParameters(parameters);

                    using (var tableAdapter = new MySqlDataAdapter())
                    {
                        tableAdapter.SelectCommand = cmd;
                        tableAdapter.SelectCommand.CommandType = CommandType.Text;

                        var outputTable = new DataTable();
                        tableAdapter.Fill(outputTable);

                        return outputTable;
                    }
                }, throwException: throwException, useTransaction: relmQuickContext.ContextOptions.DatabaseTransaction != null);
        }

        /// <summary>
        /// Executes the specified SQL query and returns the results as a <see cref="DataTable"/>.
        /// </summary>
        /// <remarks>This method uses a <see cref="MySqlDataAdapter"/> to execute the query and populate
        /// the <see cref="DataTable"/>. If a database transaction is active in the provided <paramref
        /// name="relmContext"/>, the query will participate in that transaction.</remarks>
        /// <param name="relmContext">The database context used to execute the query. Must not be <c>null</c>.</param>
        /// <param name="query">The SQL query to execute. Must not be <c>null</c> or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be added to the query. If <c>null</c>, no parameters
        /// are added.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if an error occurs during query execution. If <see
        /// langword="true"/>, exceptions are propagated; otherwise, errors are suppressed.</param>
        /// <returns>A <see cref="DataTable"/> containing the results of the query. The table will be empty if the query returns
        /// no rows.</returns>
        internal static DataTable GetDataTable(IRelmContext relmContext, string query, Dictionary<string, object> parameters = null, bool throwException = true)
        {
            return DatabaseWorkHelper.DoDatabaseWork<DataTable>(relmContext, query,
                (cmd) =>
                {
                    cmd.Parameters.AddAllParameters(parameters);

                    using (var tableAdapter = new MySqlDataAdapter())
                    {
                        tableAdapter.SelectCommand = cmd;
                        tableAdapter.SelectCommand.CommandType = CommandType.Text;

                        var outputTable = new DataTable();
                        tableAdapter.Fill(outputTable);

                        return outputTable;
                    }
                }, throwException: throwException, useTransaction: relmContext.ContextOptions.DatabaseTransaction != null);
        }
    }
}