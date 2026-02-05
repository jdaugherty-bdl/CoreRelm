using MySql.Data.MySqlClient;
using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.Models;
using CoreRelm.Options;
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
    internal class DatabaseWorkHelper
    {
        /// <summary>
        /// Caches the last execution error encountered
        /// </summary>
        internal static Exception? LastExecutionException;
        
        /// <summary>
        /// Convenience function to get the last exception message
        /// </summary>
        internal static string? LastExecutionError => LastExecutionException?.Message;
        
        /// <summary>
        /// Convenience function to check if there's an error cached
        /// </summary>
        internal static bool HasError => LastExecutionException != null;

        /// <summary>
        /// Executes a database operation using the specified connection, query, and parameters.
        /// </summary>
        /// <remarks>This method establishes a database connection based on the specified <paramref
        /// name="connectionName"/> and delegates the execution of the query to an internal helper method.</remarks>
        /// <param name="connectionName">The name of the database connection to use, represented as an enumeration value.</param>
        /// <param name="query">The SQL query to execute. This must be a valid SQL statement.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. Can be <see langword="null"/>
        /// if no parameters are required.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if an error occurs during the operation. If <see
        /// langword="true"/>, exceptions will be propagated; otherwise, errors will be suppressed.</param>
        /// <param name="allowUserVariables">A value indicating whether user-defined variables are allowed in the SQL query. If <see langword="true"/>,
        /// user variables are permitted; otherwise, they are not.</param>
        internal static void DoDatabaseWork(Enum connectionName, string query, Dictionary<string, object>? parameters = null, bool throwException = true, bool allowUserVariables = false)
        {
            using var conn = (RelmHelper.ConnectionHelper?.GetConnectionFromType(connectionName, allowUserVariables)) 
                ?? throw new InvalidOperationException($"Could not get a valid connection for connection type '{connectionName}'.");

            DoDatabaseWork(conn, query, parameters: parameters, throwException: throwException);
        }

        /// <summary>
        /// Executes a database operation using the specified connection, query, and parameters.
        /// </summary>
        /// <remarks>This method delegates the database operation to an internal context. Ensure that the
        /// provided connection is properly managed and disposed of by the caller.</remarks>
        /// <param name="establishedConnection">An established <see cref="MySqlConnection"/> to the database. The connection must be open before calling
        /// this method.</param>
        /// <param name="query">The SQL query to execute. This query can include parameter placeholders.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. If null, no parameters are
        /// applied.</param>
        /// <param name="throwException">A value indicating whether to throw an exception if an error occurs. If <see langword="true"/>, exceptions
        /// are thrown; otherwise, errors are suppressed.</param>
        internal static void DoDatabaseWork(MySqlConnection establishedConnection, string query, Dictionary<string, object>? parameters = null, bool throwException = true)
        {
            DoDatabaseWork(new RelmContext(establishedConnection), query, parameters: parameters, throwException: throwException);
        }

        /// <summary>
        /// Executes a database operation using the specified connection, query, and parameters.
        /// </summary>
        /// <remarks>This method delegates the database operation to an internal context. Ensure that the
        /// provided connection is properly managed and disposed of by the caller.</remarks>
        /// <param name="establishedConnection">An established <see cref="MySqlConnection"/> to the database. The connection must be open before calling
        /// this method.</param>
        /// <param name="sqlTransaction">A <see cref="MySqlTransaction"/> to use for the operation.</param>
        /// <param name="query">The SQL query to execute. This query can include parameter placeholders.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. If null, no parameters are
        /// applied.</param>
        /// <param name="throwException">A value indicating whether to throw an exception if an error occurs. If <see langword="true"/>, exceptions
        /// are thrown; otherwise, errors are suppressed.</param>
        internal static void DoDatabaseWork(MySqlConnection establishedConnection, MySqlTransaction sqlTransaction, string query, Dictionary<string, object>? parameters = null, bool throwException = true)
        {
            DoDatabaseWork(new RelmContext(establishedConnection, sqlTransaction), query, parameters: parameters, throwException: throwException);
        }

        /// <summary>
        /// Executes a database operation using the specified query and parameters.
        /// </summary>
        /// <remarks>This method is a wrapper for executing database operations that do not return a
        /// result set.  It supports parameterized queries and optional transaction handling. If the database context 
        /// already has a transaction configured, the operation will use it.</remarks>
        /// <param name="relmContext">The database context used to execute the operation. Cannot be <see langword="null"/>.</param>
        /// <param name="query">The SQL query to execute. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="parameters">An optional dictionary of parameters to bind to the query. Keys represent parameter names, and values
        /// represent parameter values. Can be <see langword="null"/> if no parameters are required.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the operation fails.  If <see langword="true"/>,
        /// exceptions will be propagated; otherwise, failures will be suppressed.</param>
        internal static void DoDatabaseWork(IRelmContext relmContext, string query, Dictionary<string, object>? parameters = null, bool throwException = true)
        {
            DoDatabaseWork<int>(relmContext, query,
                (cmd) =>
                {
                    if (parameters != null)
                        cmd.Parameters.AddAllParameters(parameters);

                    return cmd.ExecuteNonQuery();
                },
                throwException: throwException);
        }

        /// <summary>
        /// Executes a non-query SQL command asynchronously against the specified database context.
        /// </summary>
        /// <remarks>This method is intended for executing SQL commands that do not return result sets. If
        /// throwException is set to false, errors during execution will be suppressed.</remarks>
        /// <param name="relmContext">The database context to use for executing the command. Cannot be null.</param>
        /// <param name="query">The SQL query to execute. Must be a valid non-query command such as INSERT, UPDATE, or DELETE.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be added to the command. If null, no parameters are
        /// added.</param>
        /// <param name="throwException">true to throw an exception if the command fails; otherwise, false to suppress exceptions.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        internal async static Task DoDatabaseWorkAsync(IRelmContext relmContext, string query, Dictionary<string, object>? parameters = null, bool throwException = true, CancellationToken cancellationToken = default)
        {
            await DoDatabaseWorkAsync<int>(relmContext, query,
                async (cmd, cancellationToken) =>
                {
                    if (parameters != null)
                        cmd.Parameters.AddAllParameters(parameters);

                    return await cmd.ExecuteNonQueryAsync(cancellationToken);
                },
                throwException: throwException, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Executes a database operation using the specified connection, query, and parameters, and returns the result.
        /// </summary>
        /// <remarks>This method provides a high-level abstraction for executing database operations. It
        /// supports parameterized queries and error handling. The operation will be executed within a transaction.</remarks>
        /// <typeparam name="T">The type of the result returned by the database operation.</typeparam>
        /// <param name="connectionName">The name of the database connection to use. This must correspond to a valid connection type.</param>
        /// <param name="query">The SQL query to execute. This query can include parameter placeholders.</param>
        /// <param name="parameters">An optional dictionary of parameters to bind to the query. Keys represent parameter names, and values
        /// represent their corresponding values. Can be <see langword="null"/> if no parameters are required.</param>
        /// <param name="throwException">A value indicating whether to throw an exception if an error occurs. If <see langword="true"/>, exceptions
        /// will be propagated; otherwise, errors will be suppressed.</param>
        /// <param name="allowUserVariables">A value indicating whether user-defined variables are allowed in the query. If <see langword="true"/>, user
        /// variables are permitted.</param>
        /// <returns>The result of the database operation, cast to the specified type <typeparamref name="T"/>.</returns>
        internal static T? DoDatabaseWork<T>(Enum connectionName, string query, Dictionary<string, object>? parameters = null, bool throwException = true, bool allowUserVariables = false)
        {
            using var conn = (RelmHelper.ConnectionHelper?.GetConnectionFromType(connectionName, allowUserVariables)) 
                ?? throw new InvalidOperationException($"Could not get a valid connection for connection type '{connectionName}'.");

            return DoDatabaseWork<T>(conn, query, parameters: parameters, throwException: throwException);
        }

        /// <summary>
        /// Executes a database operation using the specified query and parameters, and returns the result.
        /// </summary>
        /// <remarks>This method delegates the database operation to an internal context and supports
        /// optional transaction handling. Ensure that the provided connection remains valid and open for the duration
        /// of the operation.</remarks>
        /// <typeparam name="T">The type of the result returned by the database operation.</typeparam>
        /// <param name="establishedConnection">An established <see cref="MySqlConnection"/> to the database. This connection must be open before calling
        /// the method.</param>
        /// <param name="query">The SQL query to execute. The query can include parameter placeholders.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. If null, no parameters are
        /// applied.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if an error occurs. If <see langword="true"/>,
        /// exceptions are thrown; otherwise, errors are suppressed.</param>
        /// <returns>The result of the database operation, cast to the specified type <typeparamref name="T"/>.</returns>
        internal static T? DoDatabaseWork<T>(MySqlConnection establishedConnection, string query, Dictionary<string, object>? parameters = null, bool throwException = true)
        {
            return DoDatabaseWork<T>(new RelmContext(establishedConnection), query, parameters: parameters, throwException: throwException);
        }

        /// <summary>
        /// Executes a database operation using the specified query and parameters, and returns the result.
        /// </summary>
        /// <remarks>This method delegates the database operation to an internal context and supports
        /// optional transaction handling. Ensure that the provided connection remains valid and open for the duration
        /// of the operation.</remarks>
        /// <typeparam name="T">The type of the result returned by the database operation.</typeparam>
        /// <param name="establishedConnection">An established <see cref="MySqlConnection"/> to the database. This connection must be open before calling
        /// the method.</param>
        /// <param name="sqlTransaction">A <see cref="MySqlTransaction"/> to use for the operation.</param>
        /// <param name="query">The SQL query to execute. The query can include parameter placeholders.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. If null, no parameters are
        /// applied.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if an error occurs. If <see langword="true"/>,
        /// exceptions are thrown; otherwise, errors are suppressed.</param>
        /// <returns>The result of the database operation, cast to the specified type <typeparamref name="T"/>.</returns>
        internal static T? DoDatabaseWork<T>(MySqlConnection establishedConnection, MySqlTransaction sqlTransaction, string query, Dictionary<string, object>? parameters = null, bool throwException = true)
        {
            return DoDatabaseWork<T>(new RelmContext(establishedConnection, sqlTransaction), query, parameters: parameters, throwException: throwException);
        }

        /// <summary>
        /// Executes a database operation using the specified query and parameters, and returns a result of the
        /// specified type.
        /// </summary>
        /// <remarks>This method supports basic database operations and allows for flexible result types.
        /// Use the <paramref name="parameters"/> dictionary to bind query parameters securely and avoid SQL injection.
        /// The operation will be executed within a transaction, which may improve data consistency in certain scenarios.</remarks>
        /// <typeparam name="T">The type of the result to return. Supported types are <see cref="string"/>, <see cref="bool"/>, and <see
        /// cref="int"/>. For unsupported types, the method returns the default value of <typeparamref name="T"/>.</typeparam>
        /// <param name="relmContext">The database context used to execute the operation. This must implement <see cref="IRelmContext"/>.</param>
        /// <param name="query">The SQL query to execute. This cannot be <see langword="null"/> or empty.</param>
        /// <param name="parameters">An optional dictionary of parameters to bind to the query. If <see langword="null"/>, no parameters are
        /// added.</param>
        /// <param name="throwException">A value indicating whether to throw an exception if the operation fails. If <see langword="true"/>,
        /// exceptions are thrown on failure; otherwise, they are suppressed.</param>
        /// <returns>The result of the database operation, converted to the specified type <typeparamref name="T"/>. Returns the
        /// default value of <typeparamref name="T"/> if the type is unsupported or if the operation produces no result.</returns>
        internal static T? DoDatabaseWork<T>(IRelmContext relmContext, string query, Dictionary<string, object>? parameters = null, bool throwException = true)
        {
            return DoDatabaseWork<T>(relmContext, query,
                (cmd) =>
                {
                    if (parameters != null)
                        cmd.Parameters.AddAllParameters(parameters);

                    var executionWork = cmd.ExecuteNonQuery();

                    if (typeof(T) == typeof(string))
                        return executionWork.ToString();
                    else if (typeof(T) == typeof(bool))
                        return executionWork > 0;
                    else if (typeof(T) == typeof(int))
                        return executionWork;
                    else
                        return default;
                },
                throwException: throwException);
        }

        /// <summary>
        /// Executes a database command asynchronously using the specified query and parameters, and returns the result
        /// as the specified type.
        /// </summary>
        /// <remarks>If T is string, the number of affected rows is returned as a string. If T is bool,
        /// returns true if one or more rows were affected; otherwise, false. If T is int, returns the number of
        /// affected rows. For other types, the default value of T is returned.</remarks>
        /// <typeparam name="T">The type of the result to return. Supported types are string, bool, and int. For other types, the default
        /// value is returned.</typeparam>
        /// <param name="relmContext">The database context to use for executing the command. Cannot be null.</param>
        /// <param name="query">The SQL query to execute against the database. Cannot be null or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be added to the command. If null, no parameters are
        /// added.</param>
        /// <param name="throwException">true to throw an exception if the operation fails; otherwise, false to suppress exceptions.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the outcome of the command,
        /// converted to the specified type T. Returns the default value of T if the type is not supported.</returns>
        internal async static Task<T?> DoDatabaseWorkAsync<T>(IRelmContext relmContext, string query, Dictionary<string, object>? parameters = null, bool throwException = true, CancellationToken cancellationToken = default)
        {
            return await DoDatabaseWorkAsync<T>(relmContext, query,
                async (cmd, cancellationToken) =>
                {
                    if (parameters != null)
                        cmd.Parameters.AddAllParameters(parameters);

                    var executionWork = await cmd.ExecuteNonQueryAsync(cancellationToken);

                    if (typeof(T) == typeof(string))
                        return executionWork.ToString();
                    else if (typeof(T) == typeof(bool))
                        return executionWork > 0;
                    else if (typeof(T) == typeof(int))
                        return executionWork;
                    else
                        return default;
                },
                throwException: throwException, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Executes a database operation using the specified query and callback function.
        /// </summary>
        /// <remarks>This method delegates the operation to a generic implementation, ensuring that the
        /// specified query and callback are executed within the context of the provided database connection and
        /// transaction settings.</remarks>
        /// <param name="connectionName">The name of the database connection to use. Must be a valid connection identifier.</param>
        /// <param name="query">The SQL query to execute. Cannot be null or empty.</param>
        /// <param name="actionCallback">A callback function that processes the <see cref="MySqlCommand"/> object and returns a result. Cannot be
        /// null.</param>
        /// <param name="throwException">A value indicating whether to throw an exception if an error occurs during the operation. If <see
        /// langword="true"/>, exceptions will be thrown; otherwise, errors will be suppressed.</param>
        /// <param name="allowUserVariables">A value indicating whether user-defined variables are allowed in the SQL query. If <see langword="true"/>,
        /// user variables are permitted; otherwise, they are not.</param>
        internal static void DoDatabaseWork(Enum connectionName, string query, Func<MySqlCommand, object> actionCallback, bool throwException = true, bool allowUserVariables = false)
        {
            DoDatabaseWork<object>(connectionName, query, actionCallback, throwException: throwException, allowUserVariables: allowUserVariables);
        }

        /// <summary>
        /// Executes a database operation using the specified query and callback function.
        /// </summary>
        /// <remarks>This method delegates to a generic overload to perform the database operation. The
        /// caller is responsible for ensuring that the connection is properly managed and disposed.</remarks>
        /// <param name="establishedConnection">An established <see cref="MySqlConnection"/> to the database. The connection must be open before calling
        /// this method.</param>
        /// <param name="query">The SQL query to execute against the database.</param>
        /// <param name="actionCallback">A callback function that processes the <see cref="MySqlCommand"/> created for the query. The result of the
        /// callback is returned by the generic overload.</param>
        /// <param name="throwException">A value indicating whether exceptions should be thrown if an error occurs. If <see langword="false"/>,
        /// errors are suppressed.</param>
        internal static void DoDatabaseWork(MySqlConnection establishedConnection, string query, Func<MySqlCommand, object> actionCallback, bool throwException = true)
        {
            DoDatabaseWork<object>(establishedConnection, query, actionCallback, throwException: throwException);
        }

        /// <summary>
        /// Executes a database operation using the specified query and callback function.
        /// </summary>
        /// <remarks>This method delegates to a generic overload to perform the database operation. The
        /// caller is responsible for ensuring that the connection is properly managed and disposed.</remarks>
        /// <param name="establishedConnection">An established <see cref="MySqlConnection"/> to the database. The connection must be open before calling
        /// this method.</param>
        /// <param name="sqlTransaction">A <see cref="MySqlTransaction"/> to use for the operation.</param>
        /// <param name="query">The SQL query to execute against the database.</param>
        /// <param name="actionCallback">A callback function that processes the <see cref="MySqlCommand"/> created for the query. The result of the
        /// callback is returned by the generic overload.</param>
        /// <param name="throwException">A value indicating whether exceptions should be thrown if an error occurs. If <see langword="false"/>,
        /// errors are suppressed.</param>
        internal static void DoDatabaseWork(MySqlConnection establishedConnection, MySqlTransaction sqlTransaction, string query, Func<MySqlCommand, object> actionCallback, bool throwException = true)
        {
            DoDatabaseWork<object>(establishedConnection, sqlTransaction, query, actionCallback, throwException: throwException);
        }

        /// <summary>
        /// Executes a database operation using the specified query and callback function.
        /// </summary>
        /// <remarks>This method delegates the operation to a generic overload of <c>DoDatabaseWork</c>, 
        /// allowing for additional customization of the database operation. The transaction behavior  is influenced by
        /// the  <c>DatabaseTransaction</c> property of the <paramref name="relmContext"/>.</remarks>
        /// <param name="relmContext">The database context used to manage the connection and configuration.</param>
        /// <param name="query">The SQL query to be executed.</param>
        /// <param name="actionCallback">A callback function that processes the <see cref="MySqlCommand"/> and returns a result.</param>
        /// <param name="throwException">A value indicating whether to throw an exception if an error occurs.  If <see langword="true"/>, exceptions
        /// will be propagated; otherwise, errors will be suppressed.</param>
        internal static void DoDatabaseWork(IRelmContext relmContext, string query, Func<MySqlCommand, object> actionCallback, bool throwException = true)
        {
            DoDatabaseWork<object>(relmContext, query, actionCallback, throwException: throwException);
        }

        /// <summary>
        /// Executes a database operation using the specified connection, query, and callback function.
        /// </summary>
        /// <remarks>This method establishes a database connection based on the specified <paramref
        /// name="connectionName"/>  and executes the provided <paramref name="query"/> using the <paramref
        /// name="actionCallback"/> function.  The operation will be executed within a transaction.</remarks>
        /// <typeparam name="T">The type of the result returned by the database operation.</typeparam>
        /// <param name="connectionName">The name of the database connection, represented as an <see cref="Enum"/>.</param>
        /// <param name="query">The SQL query to execute. This must be a valid SQL statement.</param>
        /// <param name="actionCallback">A callback function that defines the operation to perform using the <see cref="MySqlCommand"/> object.</param>
        /// <param name="throwException">A value indicating whether to throw an exception if an error occurs.  If <see langword="true"/>, exceptions
        /// will be propagated; otherwise, errors will be suppressed.</param>
        /// <param name="allowUserVariables">A value indicating whether user-defined variables are allowed in the SQL query.  If <see langword="true"/>,
        /// user variables are permitted.</param>
        /// <returns>The result of the database operation, as defined by the <typeparamref name="T"/> type.</returns>
        internal static T? DoDatabaseWork<T>(Enum connectionName, string query, Func<MySqlCommand, object> actionCallback, bool throwException = true, bool allowUserVariables = false)
        {
            using var conn = (RelmHelper.ConnectionHelper?.GetConnectionFromType(connectionName, allowUserVariables))
                ?? throw new InvalidOperationException($"Could not get a valid connection for connection type '{connectionName}'.");

            return DoDatabaseWork<T>(conn, query, actionCallback, throwException: throwException);
        }

        /// <summary>
        /// Executes a database operation using the specified connection, query, and callback function.
        /// </summary>
        /// <remarks>This method provides a flexible way to execute database operations by allowing the
        /// caller to specify a query  and a callback function to process the <see cref="MySqlCommand"/>. The operation
        /// can optionally be executed  within a transaction, either provided by the caller or created
        /// internally.</remarks>
        /// <typeparam name="T">The type of the result returned by the operation.</typeparam>
        /// <param name="establishedConnection">An established <see cref="MySqlConnection"/> to the database. Must not be null.</param>
        /// <param name="query">The SQL query to execute. Must not be null or empty.</param>
        /// <param name="actionCallback">A callback function that defines the operation to perform using the <see cref="MySqlCommand"/>. Must not be
        /// null.</param>
        /// <param name="throwException">A value indicating whether to throw an exception if an error occurs during the operation.  If <see
        /// langword="true"/>, exceptions will be propagated; otherwise, errors will be suppressed.</param>
        /// <returns>The result of the operation, as defined by the <paramref name="actionCallback"/>.</returns>
        internal static T? DoDatabaseWork<T>(MySqlConnection establishedConnection, string query, Func<MySqlCommand, object> actionCallback, bool throwException = true)
        {
            return DoDatabaseWork<T>(new RelmContext(establishedConnection), query, actionCallback, throwException: throwException);
        }

        /// <summary>
        /// Executes a database operation using the specified connection, query, and callback function.
        /// </summary>
        /// <remarks>This method provides a flexible way to execute database operations by allowing the
        /// caller to specify a query  and a callback function to process the <see cref="MySqlCommand"/>. The operation
        /// can optionally be executed  within a transaction, either provided by the caller or created
        /// internally.</remarks>
        /// <typeparam name="T">The type of the result returned by the operation.</typeparam>
        /// <param name="establishedConnection">An established <see cref="MySqlConnection"/> to the database. Must not be null.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to use for the operation. If provided, the operation will be
        /// executed within this transaction.</param>
        /// <param name="query">The SQL query to execute. Must not be null or empty.</param>
        /// <param name="actionCallback">A callback function that defines the operation to perform using the <see cref="MySqlCommand"/>. Must not be
        /// null.</param>
        /// <param name="throwException">A value indicating whether to throw an exception if an error occurs during the operation.  If <see
        /// langword="true"/>, exceptions will be propagated; otherwise, errors will be suppressed.</param>
        /// <returns>The result of the operation, as defined by the <paramref name="actionCallback"/>.</returns>
        internal static T? DoDatabaseWork<T>(MySqlConnection establishedConnection, MySqlTransaction sqlTransaction, string query, Func<MySqlCommand, object> actionCallback, bool throwException = true)
        {
            return DoDatabaseWork<T>(new RelmContext(establishedConnection, sqlTransaction), query, actionCallback, throwException: throwException);
        }

        /// <summary>
        /// Executes a database operation using the specified query and callback function.
        /// </summary>
        /// <remarks>This method provides a flexible way to execute database operations by allowing the
        /// caller to specify a query and a callback function that defines the operation to perform. The operation can
        /// optionally be executed within a transaction, and error handling behavior can be customized using the
        /// <paramref name="throwException"/> parameter.</remarks>
        /// <typeparam name="T">The type of the result returned by the database operation.</typeparam>
        /// <param name="relmContext">The database context used to configure and execute the operation. Cannot be <see langword="null"/>.</param>
        /// <param name="query">The SQL query to execute. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="actionCallback">A callback function that defines the operation to perform with the <see cref="MySqlCommand"/> object. Cannot
        /// be <see langword="null"/>.</param>
        /// <param name="throwException">Specifies whether to throw an exception if an error occurs. If <see langword="true"/>, exceptions will be
        /// propagated; otherwise, errors will be suppressed.</param>
        /// <returns>The result of the database operation, as defined by the <typeparamref name="T"/> type.</returns>
        internal static T? DoDatabaseWork<T>(IRelmContext relmContext, string query, Func<MySqlCommand, object> actionCallback, bool throwException = true)
        {
            return DoDatabaseWork<T>(relmContext.ContextOptions, query, actionCallback, throwException: throwException);
        }

        /// <summary>
        /// Executes an asynchronous database operation using the specified query and callback within the provided Relm
        /// context.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the database operation.</typeparam>
        /// <param name="relmContext">The Relm context that provides database connection and configuration information. Cannot be null.</param>
        /// <param name="query">The SQL query to execute as part of the database operation. Cannot be null or empty.</param>
        /// <param name="actionCallback">A callback function that receives a configured MySqlCommand and a cancellation token, and performs the
        /// desired asynchronous operation. The callback must return a Task representing the result of the operation.</param>
        /// <param name="throwException">true to throw an exception if the operation fails; otherwise, false to suppress exceptions and return a
        /// default value. The default is true.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the value returned by the
        /// callback, cast to type T.</returns>
        internal async static Task<T?> DoDatabaseWorkAsync<T>(IRelmContext relmContext, string query, Func<MySqlCommand, CancellationToken, Task<object>> actionCallback, bool throwException = true, CancellationToken cancellationToken = default)
        {
            return await DoDatabaseWorkAsync<T>(relmContext.ContextOptions, query, actionCallback, throwException: throwException, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Executes a database operation using the provided SQL query and callback function, with optional transaction
        /// support.
        /// </summary>
        /// <remarks>This method manages the database connection and transaction lifecycle. If the
        /// connection is not already open, it will be opened for the duration of the operation and closed afterward. If
        /// no transaction is provided in <paramref name="contextOptions"/>, a new transaction will be created and committed 
        /// upon successful execution or rolled back in case of an error.</remarks>
        /// <typeparam name="T">The type of the result returned by the operation.</typeparam>
        /// <param name="contextOptions">The database context options, including the connection and transaction settings.</param>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="actionCallback">A callback function that defines the operation to perform using the <see cref="MySqlCommand"/> object.</param>
        /// <param name="throwException">A value indicating whether exceptions should be thrown. If <see langword="true"/>, exceptions are thrown;
        /// otherwise, they are recorded in <c>LastExecutionException</c>.</param>
        /// <returns>The result of the operation, as defined by the <typeparamref name="T"/> type. Returns the default value of
        /// <typeparamref name="T"/> if an exception occurs and <paramref name="throwException"/> is <see
        /// langword="false"/>.</returns>
        /// <exception cref="Exception">Thrown if a general error occurs during the execution of the query and <paramref name="throwException"/> is
        /// <see langword="true"/>.</exception>
        internal static T? DoDatabaseWork<T>(RelmContextOptionsBuilder contextOptions, string query, Func<MySqlCommand, object> actionCallback, bool throwException = true)
        {
            return DoDatabaseWorkAsync<T>(
                contextOptions,
                query,
                async (cmd, ct) => actionCallback(cmd),
                throwException: throwException,
                useTransaction: contextOptions.DatabaseTransaction != null,
                cancellationToken: CancellationToken.None
            ).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Executes an asynchronous database operation using the specified query and callback, returning the result as
        /// the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the database operation.</typeparam>
        /// <param name="contextOptions">The options used to configure the database context, including connection and transaction settings.</param>
        /// <param name="query">The SQL query to execute against the database.</param>
        /// <param name="actionCallback">A callback function that receives a configured <see cref="MySqlCommand"/> and performs the desired
        /// asynchronous operation. The result of this callback is cast to the specified type.</param>
        /// <param name="throwException">Indicates whether to throw an exception if the database operation fails. If <see langword="true"/>,
        /// exceptions are propagated; otherwise, failures may be handled silently.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the value returned by the
        /// callback, cast to the specified type.</returns>
        internal async static Task<T?> DoDatabaseWorkAsync<T>(RelmContextOptionsBuilder contextOptions, string query, Func<MySqlCommand, Task<object>> actionCallback, bool throwException = true, CancellationToken cancellationToken = default)
        {
            return DoDatabaseWorkAsync<T>(
                contextOptions,
                query,
                async (cmd, ct) => await actionCallback(cmd),
                throwException: throwException,
                useTransaction: contextOptions.DatabaseTransaction != null,
                cancellationToken: cancellationToken
            ).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Executes a database operation using the provided SQL query and callback function, with optional transaction
        /// support.
        /// </summary>
        /// <remarks>This method manages the database connection and transaction lifecycle. If the
        /// connection is not already open, it will be opened for the duration of the operation and closed afterward. If
        /// <paramref name="useTransaction"/> is <see langword="true"/> and no transaction is provided in <paramref
        /// name="contextOptions"/>, a new transaction will be created and committed upon successful execution or rolled
        /// back in case of an error.</remarks>
        /// <typeparam name="T">The type of the result returned by the operation.</typeparam>
        /// <param name="contextOptions">The database context options, including the connection and transaction settings.</param>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="actionCallback">A callback function that defines the operation to perform using the <see cref="MySqlCommand"/> object.</param>
        /// <param name="throwException">A value indicating whether exceptions should be thrown. If <see langword="true"/>, exceptions are thrown;
        /// otherwise, they are recorded in <c>LastExecutionException</c>.</param>
        /// <param name="useTransaction">A value indicating whether the operation should be executed within a transaction. If <see langword="true"/>,
        /// a transaction is used; otherwise, the operation is executed without a transaction.</param>
        /// <returns>The result of the operation, as defined by the <typeparamref name="T"/> type. Returns the default value of
        /// <typeparamref name="T"/> if an exception occurs and <paramref name="throwException"/> is <see
        /// langword="false"/>.</returns>
        /// <exception cref="Exception">Thrown if a general error occurs during the execution of the query and <paramref name="throwException"/> is
        /// <see langword="true"/>.</exception>
        private async static Task<T?> DoDatabaseWorkAsync<T>(
            RelmContextOptionsBuilder contextOptions, 
            string query, 
            Func<MySqlCommand, CancellationToken, Task<object>> actionCallback, 
            bool throwException = true, 
            bool useTransaction = true,
            CancellationToken cancellationToken = default)
        {
            var internalOpen = false; // indicates whether the connection was already open or not
            var openedNewTransaction = false; // indicates whether a new transaction was created here or not

            // reset the last execution error
            LastExecutionException = null;

            try
            {
                // if the connection isn't open, then open it and record that we did that
                if ((contextOptions.DatabaseConnection?.State ?? ConnectionState.Broken) != ConnectionState.Open)
                {
                    //EstablishedConnection.Open();
                    if (contextOptions.DatabaseConnection != null)
                    {
                        await contextOptions.DatabaseConnection.OpenAsync(cancellationToken);
                        internalOpen = true;
                    }
                }

                // if the caller wants to use transactions but they didn't provide one, create a new one
                if (useTransaction && contextOptions.DatabaseTransaction == null)
                {
                    //currentTransaction = EstablishedConnection.BeginTransaction();
                    //var newTransaction = await contextOptions.DatabaseConnection?.BeginTransactionAsync(cancellationToken);
                    var newTransaction = contextOptions.DatabaseConnection != null
                        ? await contextOptions.DatabaseConnection.BeginTransactionAsync(cancellationToken)
                        : null;
                    contextOptions.SetDatabaseTransaction(newTransaction);
                    openedNewTransaction = true;
                }

                // execute the SQL
                using (var cmd = new MySqlCommand(query, contextOptions.DatabaseConnection, contextOptions.DatabaseTransaction))
                {
                    cmd.CommandTimeout = int.MaxValue;

                    // execute whatever code the caller provided
                    var result = await actionCallback(cmd, cancellationToken).ConfigureAwait(false);

                    // if we opened the transaction here, just commit it because we're going to be closing it right away
                    if (openedNewTransaction)
                        contextOptions.DatabaseTransaction?.Commit();

                    return (T)result;
                }
            }
            catch (MySqlException mysqlEx) // use special handling for MySQL exceptions
            {
                // there was an error, roll back the transaction
                if (useTransaction)
                    contextOptions.DatabaseTransaction?.Rollback();

                // if we want exceptions to be thrown, rethrow the current one, otherwise just record the error
                if (throwException)
                    throw new Exception(mysqlEx.Message, mysqlEx);
                else
                {
                    LastExecutionException = mysqlEx;

                    return default;
                }
            }
            catch (Exception ex) // handle all other unhandled exceptions
            {
                // there was an error, roll back the transaction
                if (useTransaction)
                    contextOptions.DatabaseTransaction?.Rollback();

                // if we want exceptions to be thrown, rethrow the current one, otherwise just record the error
                if (throwException)
                    throw new Exception(ex.Message, ex);
                else
                {
                    LastExecutionException = ex;

                    return default;
                }
            }
            finally
            {
                // if we opened the connection, close it back up before it's disposed
                if (internalOpen 
                    && (contextOptions.DatabaseConnection?.State ?? ConnectionState.Broken) == ConnectionState.Open
                    && contextOptions.DatabaseConnection != null)
                {
                    await contextOptions.DatabaseConnection.CloseAsync();
                }
            }
        }
    }
}
