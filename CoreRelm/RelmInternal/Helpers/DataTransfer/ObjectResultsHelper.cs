using MySql.Data.MySqlClient;
using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.Models;
using CoreRelm.RelmInternal.Helpers.Operations;
using CoreRelm.RelmInternal.Helpers.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.DataTransfer
{
    internal class ObjectResultsHelper
    {
        /// <summary>
        /// Executes a query on the specified database connection and retrieves a collection of results.
        /// </summary>
        /// <typeparam name="T">The type of objects to map the query results to.</typeparam>
        /// <param name="connectionName">The name of the database connection, represented as an <see cref="Enum"/>.</param>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to include in the query. Defaults to <see
        /// langword="null"/>.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if an error occurs during query execution.  If <see
        /// langword="true"/>, exceptions will be thrown; otherwise, errors will be suppressed. Defaults to <see
        /// langword="true"/>.</param>
        /// <param name="allowUserVariables">A value indicating whether user-defined variables are allowed in the database connection.  Defaults to <see
        /// langword="false"/>.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the results of the query, mapped to the specified type
        /// <typeparamref name="T"/>.</returns>
        internal static IEnumerable<T> GetDataList<T>(Enum connectionName, string query, Dictionary<string, object> parameters = null, bool throwException = true, bool allowUserVariables = false)
        {
            using (var conn = ConnectionHelper.GetConnectionFromType(connectionName, allowUserVariables))
            {
                return GetDataList<T>(conn, query, parameters: parameters, throwException: throwException);
            }
        }

        /// <summary>
        /// Executes the specified SQL query and retrieves a collection of data mapped to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to which each row in the result set will be mapped.</typeparam>
        /// <param name="existingConnection">An existing <see cref="MySqlConnection"/> to use for the query. The connection must be open.</param>
        /// <param name="query">The SQL query to execute. Must be a valid SQL statement.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to include in the query. Can be <see langword="null"/>
        /// if no parameters are needed.</param>
        /// <param name="throwException">A value indicating whether to throw an exception if an error occurs. If <see langword="true"/>, exceptions
        /// will be thrown; otherwise, errors will be suppressed.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the query. Can be <see langword="null"/> if no
        /// transaction is required.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the data mapped to the specified type <typeparamref name="T"/>.
        /// The collection will be empty if the query returns no results.</returns>
        internal static IEnumerable<T> GetDataList<T>(MySqlConnection existingConnection, string query, Dictionary<string, object> parameters = null, bool throwException = true, MySqlTransaction sqlTransaction = null)
        {
            var relmContext = new RelmContext(existingConnection, sqlTransaction);

            return GetDataList<T>(relmContext, query, parameters: parameters, throwException: throwException);
        }

        /// <summary>
        /// Executes a query against the specified context and returns the results as a sequence of the specified
        /// type.
        /// </summary>
        /// <typeparam name="T">The type to which the query results will be converted.</typeparam>
        /// <param name="relmContext">The Realm context used to execute the query. Cannot be <see langword="null"/>.</param>
        /// <param name="query">The SQL-like query string to execute. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="parameters">An optional dictionary of parameters to include in the query. Can be <see langword="null"/> if no parameters
        /// are needed.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the query fails.  If <see langword="true"/>, an
        /// exception will be thrown on failure; otherwise, the method will return an empty sequence.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the query results converted to the specified type.  Returns an
        /// empty sequence if the query produces no results or if <paramref name="throwException"/> is <see
        /// langword="false"/> and the query fails.</returns>
        internal static IEnumerable<T> GetDataList<T>(IRelmContext relmContext, string query, Dictionary<string, object> parameters = null, bool throwException = true)
        {
            return RefinedResultsHelper.GetDataTable(relmContext, query, parameters: parameters, throwException: throwException)
                .AsEnumerable()
                .Select(x => (T)CoreUtilities.ConvertScalar<T>(x[0]));
        }

        /// <summary>
        /// Executes a query against the specified context and returns the results as a sequence of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to which the query results will be converted.</typeparam>
        /// <param name="relmContext">The context used to execute the query. Must not be <see langword="null"/>.</param>
        /// <param name="query">The SQL query to execute. Must not be <see langword="null"/> or empty.</param>
        /// <param name="parameters">An optional dictionary of parameters to include in the query. Can be <see langword="null"/> if no parameters
        /// are needed.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the query fails.  If <see langword="true"/>, an
        /// exception will be thrown on failure; otherwise, the method will return an empty sequence.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the results of the query, with each result converted to the
        /// specified type. If the query returns no results, an empty sequence is returned.</returns>
        internal static IEnumerable<T> GetDataList<T>(IRelmQuickContext relmContext, string query, Dictionary<string, object> parameters = null, bool throwException = true)
        {
            return RefinedResultsHelper.GetDataTable(relmContext, query, parameters: parameters, throwException: throwException)
                .AsEnumerable()
                .Select(x => (T)CoreUtilities.ConvertScalar<T>(x[0]));
        }

        /// <summary>
        /// Retrieves a single data object of the specified type based on the provided query and parameters.
        /// </summary>
        /// <remarks>This method is a convenience wrapper for retrieving a single data object.  If
        /// multiple objects match the query, only the first one is returned.</remarks>
        /// <typeparam name="T">The type of the data object to retrieve. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="connectionName">The connection identifier used to determine the data source.</param>
        /// <param name="query">The query string used to retrieve the data object.</param>
        /// <param name="parameters">An optional dictionary of parameters to be used in the query. Can be <see langword="null"/> if no parameters
        /// are required.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if an error occurs.  The default value is <see
        /// langword="true"/>.</param>
        /// <param name="allowUserVariables">A value indicating whether user-defined variables are allowed in the query.  The default value is <see
        /// langword="false"/>.</param>
        /// <returns>The first data object of type <typeparamref name="T"/> that matches the query, or <see langword="null"/> if
        /// no matching object is found.</returns>
        internal static T GetDataObject<T>(Enum connectionName, string query, Dictionary<string, object> parameters = null, bool throwException = true, bool allowUserVariables = false) where T : IRelmModel
        {
            return GetDataObjects<T>(connectionName, query, parameters: parameters, throwException: throwException, allowUserVariables: allowUserVariables)
                .FirstOrDefault();
        }

        /// <summary>
        /// Executes the specified query and retrieves the first result as an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to retrieve. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="existingConnection">An existing <see cref="MySqlConnection"/> to use for the query. Cannot be null.</param>
        /// <param name="query">The SQL query to execute. Cannot be null or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to include in the query. Can be null if no parameters
        /// are needed.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if an error occurs during query execution. <see
        /// langword="true"/> to throw exceptions; otherwise, <see langword="false"/>.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the query. Can be null if no transaction is
        /// used.</param>
        /// <returns>The first result of the query as an object of type <typeparamref name="T"/>, or <see langword="null"/> if no
        /// results are found.</returns>
        internal static T GetDataObject<T>(MySqlConnection existingConnection, string query, Dictionary<string, object> parameters = null, bool throwException = true, MySqlTransaction sqlTransaction = null) where T : IRelmModel
        {
            return GetDataObjects<T>(existingConnection, query, parameters: parameters, throwException: throwException, sqlTransaction: sqlTransaction)
                .FirstOrDefault();
        }

        /// <summary>
        /// Retrieves a single data object of the specified type that matches the given query and parameters.
        /// </summary>
        /// <remarks>This method is a convenience wrapper around <c>GetDataObjects</c>, returning only the
        /// first matching object. If no objects match the query, the method returns <see langword="null"/> unless
        /// <paramref name="throwException"/> is set to <see langword="true"/>.</remarks>
        /// <typeparam name="T">The type of the data object to retrieve. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="relmContext">The context used to execute the query. This provides access to the data source.</param>
        /// <param name="query">The query string used to filter the data objects.</param>
        /// <param name="parameters">An optional dictionary of parameters to be used in the query. Can be <see langword="null"/>.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the query fails. Defaults to <see
        /// langword="true"/>.</param>
        /// <returns>The first data object of type <typeparamref name="T"/> that matches the query, or <see langword="null"/> if
        /// no matching object is found.</returns>
        internal static T GetDataObject<T>(IRelmContext relmContext, string query, Dictionary<string, object> parameters = null, bool throwException = true) where T : IRelmModel
        {
            return GetDataObjects<T>(relmContext, query, parameters: parameters, throwException: throwException)
                .FirstOrDefault();
        }

        /// <summary>
        /// Retrieves a single data object of the specified type that matches the given query and parameters.
        /// </summary>
        /// <typeparam name="T">The type of the data object to retrieve. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="relmQuickContext">The context used to execute the query.</param>
        /// <param name="query">The query string used to retrieve the data object.</param>
        /// <param name="parameters">An optional dictionary of parameters to be used in the query. Can be <see langword="null"/>.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the query fails or no data object is found. <see
        /// langword="true"/> to throw an exception; otherwise, <see langword="false"/>.</param>
        /// <returns>The first data object of type <typeparamref name="T"/> that matches the query, or <see langword="null"/> if
        /// no match is found and <paramref name="throwException"/> is <see langword="false"/>.</returns>
        internal static T GetDataObject<T>(IRelmQuickContext relmQuickContext, string query, Dictionary<string, object> parameters = null, bool throwException = true) where T : IRelmModel
        {
            return GetDataObjects<T>(relmQuickContext, query, parameters: parameters, throwException: throwException)
                .FirstOrDefault();
        }

        /// <summary>
        /// Executes a query against the specified database connection and retrieves a collection of data objects of the
        /// specified type.
        /// </summary>
        /// <remarks>This method uses the specified database connection to execute the provided query and
        /// map the results to objects of type <typeparamref name="T"/>. The caller is responsible for ensuring that the
        /// query and parameters are valid for the target database.</remarks>
        /// <typeparam name="T">The type of data objects to retrieve. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="connectionName">An <see cref="Enum"/> value representing the database connection to use.</param>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to include in the query. Defaults to <see
        /// langword="null"/>.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if an error occurs. Defaults to <see
        /// langword="true"/>.</param>
        /// <param name="allowUserVariables">A value indicating whether user-defined variables are allowed in the database connection. Defaults to <see
        /// langword="false"/>.</param>
        /// <returns>A collection of objects of type <typeparamref name="T"/> retrieved from the database. The collection will be
        /// empty if no matching data is found.</returns>
        internal static IEnumerable<T> GetDataObjects<T>(Enum connectionName, string query, Dictionary<string, object> parameters = null, bool throwException = true, bool allowUserVariables = false) where T : IRelmModel
        {
            using (var conn = ConnectionHelper.GetConnectionFromType(connectionName, allowUserVariables))
            {
                return GetDataObjects<T>(conn, query, parameters: parameters, throwException: throwException);
            }
        }

        /// <summary>
        /// Retrieves a collection of data objects of the specified type by executing the provided SQL query.
        /// </summary>
        /// <typeparam name="T">The type of data objects to retrieve. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="existingConnection">An open <see cref="MySqlConnection"/> to use for executing the query.</param>
        /// <param name="query">The SQL query to execute. The query should be a valid SQL statement that returns data.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to include in the query. Can be <see langword="null"/>
        /// if no parameters are required.</param>
        /// <param name="throwException">A value indicating whether to throw an exception if an error occurs during query execution. If <see
        /// langword="true"/>, exceptions will be thrown; otherwise, errors will be suppressed.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the query execution. Can be <see
        /// langword="null"/> if no transaction is required.</param>
        /// <returns>A collection of objects of type <typeparamref name="T"/> populated with the data returned by the query.</returns>
        internal static IEnumerable<T> GetDataObjects<T>(MySqlConnection existingConnection, string query, Dictionary<string, object> parameters = null, bool throwException = true, MySqlTransaction sqlTransaction = null) where T : IRelmModel
        {
            return GetDataObjects<T>(RefinedResultsHelper.GetDataTable(existingConnection, query, parameters: parameters, throwException: throwException, sqlTransaction: sqlTransaction));
        }

        /// <summary>
        /// Retrieves a collection of data objects of the specified type based on the provided query and parameters.
        /// </summary>
        /// <remarks>This method internally executes the query using the specified <paramref
        /// name="relmContext"/> and maps the results to objects of type <typeparamref name="T"/>. Ensure that the query
        /// and parameters are properly constructed to avoid runtime errors.</remarks>
        /// <typeparam name="T">The type of data objects to retrieve. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="relmContext">The context used to execute the query. Cannot be <c>null</c>.</param>
        /// <param name="query">The SQL query or command text used to retrieve the data. Cannot be <c>null</c> or empty.</param>
        /// <param name="parameters">An optional dictionary of parameters to be used in the query. Keys represent parameter names, and values
        /// represent parameter values. Can be <c>null</c>.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if an error occurs during query execution. <see
        /// langword="true"/> to throw an exception; otherwise, <see langword="false"/>.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the data objects of type <typeparamref name="T"/> retrieved by
        /// the query. If no data is found, an empty collection is returned.</returns>
        internal static IEnumerable<T> GetDataObjects<T>(IRelmContext relmContext, string query, Dictionary<string, object> parameters = null, bool throwException = true) where T : IRelmModel
        {
            return GetDataObjects<T>(RefinedResultsHelper.GetDataTable(relmContext, query, parameters: parameters, throwException: throwException));
        }

        /// <summary>
        /// Retrieves a collection of data objects of the specified type based on the provided query and parameters.
        /// </summary>
        /// <typeparam name="T">The type of data objects to retrieve. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="relmQuickContext">The context used to execute the query.</param>
        /// <param name="query">The SQL query string used to retrieve the data.</param>
        /// <param name="parameters">An optional dictionary of parameters to be used in the query. The keys represent parameter names, and the
        /// values represent parameter values.</param>
        /// <param name="throwException">A boolean value indicating whether an exception should be thrown if an error occurs during query execution.
        /// <see langword="true"/> to throw an exception; otherwise, <see langword="false"/>.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the data objects of type <typeparamref name="T"/> retrieved by
        /// the query.</returns>
        internal static IEnumerable<T> GetDataObjects<T>(IRelmQuickContext relmQuickContext, string query, Dictionary<string, object> parameters = null, bool throwException = true) where T : IRelmModel
        {
            return GetDataObjects<T>(RefinedResultsHelper.GetDataTable(relmQuickContext, query, parameters: parameters, throwException: throwException));
        }

        /// <summary>
        /// Converts the rows of the specified <see cref="DataTable"/> into a collection of objects of type
        /// <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of objects to create, which must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="existingData">The <see cref="DataTable"/> containing the data to convert. Each row is used to create an object of type
        /// <typeparamref name="T"/>.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing objects of type <typeparamref name="T"/> created from the rows of
        /// the <paramref name="existingData"/>. If a row is null, the corresponding object in the collection will be
        /// the default value of <typeparamref name="T"/>.</returns>
        internal static IEnumerable<T> GetDataObjects<T>(DataTable existingData) where T : IRelmModel
        {
            return existingData
                .AsEnumerable()
                .Select(x => x == null
                    ? default
                    : (T)CoreUtilities.CreateCreatorExpression<T>()().ResetWithData(x, null));
        }
    }
}
