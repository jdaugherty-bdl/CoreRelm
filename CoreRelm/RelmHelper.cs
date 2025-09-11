using CoreRelm.Helpers.Operations;
using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.Options;
using CoreRelm.Persistence;
using CoreRelm.RelmInternal.Helpers.Connections;
using CoreRelm.RelmInternal.Helpers.DataCollection;
using CoreRelm.RelmInternal.Helpers.DataTransfer;
using CoreRelm.RelmInternal.Helpers.DataTransfer.Persistence;
using CoreRelm.RelmInternal.Helpers.Operations;
using CoreRelm.RelmInternal.Helpers.Utilities;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.RelmEnumHolder;

namespace CoreRelm
{
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

        public static IRelmContext CurrentContext { get; set; }

        public static string RootLoggingDirectory
        {
            get
            {
                return ConfigurationManager.AppSettings.AllKeys.Contains("EnergyStarQpx_LoggingDir")
                    ? ConfigurationManager.AppSettings["EnergyStarQpx_LoggingDir"]
                    : Path.GetDirectoryName((new Uri(AssemblyHelper.GetEntryAssembly().CodeBase)).AbsolutePath); // Assembly.GetExecutingAssembly()
            }
        }

        //***************** Connections *****************//

        /// <summary>
        /// Gets a MySQL connection builder that can then be used to establish a connection to the database, or to get connection details.
        /// </summary>
        /// <param name="connectionType">A properly formatted database connection string.</param>
        /// <returns>A connection string builder that can be used to establish connections.</returns>
        public static MySqlConnectionStringBuilder GetConnectionBuilderFromConnectionType(Enum connectionType)
            => ConnectionHelper.GetConnectionBuilderFromType(connectionType);

        /// <summary>
        /// Gets a MySQL connection builder that can then be used to establish a connection to the database, or to get connection details.
        /// </summary>
        /// <param name="connectionName">A properly formatted database connection string.</param>
        /// <returns>A connection string builder that can be used to establish connections.</returns>
        public static MySqlConnectionStringBuilder GetConnectionBuilderFromName(string connectionName)
            => ConnectionHelper.GetConnectionBuilderFromName(connectionName);

        /// <summary>
        /// Gets a MySQL connection builder that can then be used to establish a connection to the database, or to get connection details.
        /// </summary>
        /// <param name="connectionString">A properly formatted database connection string.</param>
        /// <returns>A connection string builder that can be used to establish connections.</returns>
        public static MySqlConnectionStringBuilder GetConnectionBuilderFromConnectionString(string connectionString)
            => ConnectionHelper.GetConnectionBuilderFromConnectionString(connectionString);

        /// <summary>
        /// Gets an unopened MySQL connection given a resolvable connection string type.
        /// </summary>
        /// <param name="connectionType">An enum type to reference a connection string defined in web.config.</param>
        /// <param name="allowUserVariables">Will allow special user variables (variables start with "@") to be defined in the query that will be eventually executed.</param>
        /// <returns>An unopened MySQL connection.</returns>
        public static MySqlConnection GetConnectionFromType(Enum connectionType, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
            => ConnectionHelper.GetConnectionFromType(connectionType, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);

        /// <summary>
        /// Gets an unopened MySQL connection given a resolvable connection string type.
        /// </summary>
        /// <param name="connectionName">An enum type to reference a connection string defined in web.config.</param>
        /// <param name="allowUserVariables">Will allow special user variables (variables start with "@") to be defined in the query that will be eventually executed.</param>
        /// <returns>An unopened MySQL connection.</returns>
        public static MySqlConnection GetConnectionFromName(string connectionName, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
            => ConnectionHelper.GetConnectionFromName(connectionName, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);

        /// <summary>
        /// Gets an unopened MySQL connection given a resolvable connection string type.
        /// </summary>
        /// <param name="connectionString">An enum type to reference a connection string defined in web.config.</param>
        /// <param name="allowUserVariables">Will allow special user variables (variables start with "@") to be defined in the query that will be eventually executed.</param>
        /// <returns>An unopened MySQL connection.</returns>
        public static MySqlConnection GetConnectionFromConnectionString(string connectionString, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
            => ConnectionHelper.GetConnectionFromConnectionString(connectionString, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);

        //***************** Identity functions *****************//

        /// <summary>
        /// Use the MySql built in function to get the ID of the last row inserted.
        /// </summary>
        /// <param name="ConfigConnectionString">An enum type to reference a connection string defined in web.config.</param>
        /// <returns>A string representation of the ID.</returns>
        public static string GetLastInsertId(Enum ConfigConnectionString)
            => RowIdentityHelper.GetLastInsertId(ConfigConnectionString);

        /// <summary>
        /// Converts an InternalId to an autonumbered row ID.
        /// </summary>
        /// <param name="ConfigConnectionString">An enum type to reference a connection string defined in web.config.</param>
        /// <param name="Table">Table name to use for the conversion.</param>
        /// <param name="InternalId">The GUID of the InternalId to convert.</param>
        /// <returns>ID of the row matching the InternalId.</returns>
        public static string GetIdFromInternalId(Enum ConfigConnectionString, string Table, string InternalId)
            => RowIdentityHelper.GetIdFromInternalId(ConfigConnectionString, Table, InternalId);

        //***************** Refined results *****************//

        /// <summary>
        /// Query the database to get a single value.
        /// </summary>
        /// <typeparam name="T">The data return type.</typeparam>
        /// <param name="ConfigConnectionString">An enum type to reference a connection string defined in web.config.</param>
        /// <param name="QueryString">The full SQL query string to be used to retrieve the value requested.</param>
        /// <param name="Parameters">Named parameters for the query.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="AllowUserVariables">Will allow special user variables (variables start with "@") to be defined in the query that will be eventually executed.</param>
        /// <returns>Single value returned as the specified type.</returns>
        public static T GetScalar<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool AllowUserVariables = false)
            => RefinedResultsHelper.GetScalar<T>(ConfigConnectionString, QueryString, Parameters: Parameters, ThrowException: ThrowException, AllowUserVariables: AllowUserVariables);

        /// <summary>
        /// Query the database to get a single value.
        /// </summary>
        /// <typeparam name="T">The data return type.</typeparam>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database.</param>
        /// <param name="QueryString">The full SQL query string to be used to retrieve the value requested.</param>
        /// <param name="Parameters">Named parameters for the query.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        /// <returns>Single value returned as the specified type.</returns>
        public static T GetScalar<T>(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null)
            => RefinedResultsHelper.GetScalar<T>(EstablishedConnection, QueryString, Parameters: Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction);

        /// <summary>
        /// Query the database to get a single value.
        /// </summary>
        /// <typeparam name="T">The data return type.</typeparam>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database.</param>
        /// <param name="QueryString">The full SQL query string to be used to retrieve the value requested.</param>
        /// <param name="Parameters">Named parameters for the query.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        /// <returns>Single value returned as the specified type.</returns>
        public static T GetScalar<T>(IRelmContext relmContext, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null)
            => RefinedResultsHelper.GetScalar<T>(relmContext, QueryString, Parameters: Parameters, ThrowException: ThrowException);

        /// <summary>
        /// Query the database to get a single row. If multiple rows are returned by the query, only the first is returned.
        /// </summary>
        /// <param name="ConfigConnectionString">An enum type to reference a connection string defined in web.config.</param>
        /// <param name="QueryString">The full SQL query string to be used to retrieve the value requested.</param>
        /// <param name="Parameters">Named parameters for the query.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="AllowUserVariables">Will allow special user variables (variables start with "@") to be defined in the query that will be eventually executed.</param>
        /// <returns>One row of data.</returns>
        public static DataRow GetDataRow(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool AllowUserVariables = false)
            => RefinedResultsHelper.GetDataRow(ConfigConnectionString, QueryString, Parameters: Parameters, ThrowException: ThrowException, AllowUserVariables: AllowUserVariables);

        /// <summary>
        /// Query the database to get a single row. If multiple rows are returned by the query, only the first is returned.
        /// </summary>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database.</param>
        /// <param name="QueryString">The full SQL query string to be used to retrieve the value requested.</param>
        /// <param name="Parameters">Named parameters for the query.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        /// <returns>One row of data.</returns>
        public static DataRow GetDataRow(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null)
            => RefinedResultsHelper.GetDataRow(EstablishedConnection, QueryString, Parameters: Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction);

        /// <summary>
        /// Query the database for a full table.
        /// </summary>
        /// <param name="ConfigConnectionString">An enum type to reference a connection string defined in web.config.</param>
        /// <param name="QueryString">The full SQL query string to be used to retrieve the value requested.</param>
        /// <param name="Parameters">Named parameters for the query.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="AllowUserVariables">Will allow special user variables (variables start with "@") to be defined in the query that will be eventually executed.</param>
        /// <returns>A full DataTable with requested data.</returns>
        public static DataTable GetDataTable(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool AllowUserVariables = false)
            => RefinedResultsHelper.GetDataTable(ConfigConnectionString, QueryString, Parameters: Parameters, ThrowException: ThrowException, AllowUserVariables: AllowUserVariables);

        /// <summary>
        /// Query the database for a full table.
        /// </summary>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database.</param>
        /// <param name="QueryString">The full SQL query string to be used to retrieve the value requested.</param>
        /// <param name="Parameters">Named parameters for the query.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        /// <returns>A full DataTable with requested data.</returns>
        public static DataTable GetDataTable(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null)
            => RefinedResultsHelper.GetDataTable(EstablishedConnection, QueryString, Parameters: Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction);

        /// <summary>
        /// Query the database for a full table.
        /// </summary>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database.</param>
        /// <param name="QueryString">The full SQL query string to be used to retrieve the value requested.</param>
        /// <param name="Parameters">Named parameters for the query.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        /// <returns>A full DataTable with requested data.</returns>
        public static DataTable GetDataTable(IRelmContext relmContext, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true)
            => RefinedResultsHelper.GetDataTable(relmContext, QueryString, Parameters: Parameters, ThrowException: ThrowException);

        //***************** Object results *****************//

        /// <summary>
        /// Query the database for a single row that returns as a single object of the supplied type.
        /// </summary>
        /// <typeparam name="T">The data return type.</typeparam>
        /// <param name="ConfigConnectionString">An enum type to reference a connection string defined in web.config.</param>
        /// <param name="QueryString">The full SQL query string to be used to retrieve the value requested.</param>
        /// <param name="Parameters">Named parameters for the query.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="AllowUserVariables">Will allow special user variables (variables start with "@") to be defined in the query that will be eventually executed.</param>
        /// <returns>An object populated with the requested data.</returns>
        public static T GetDataObject<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool AllowUserVariables = false) where T : IRelmModel, new()
            => ObjectResultsHelper.GetDataObject<T>(ConfigConnectionString, QueryString, Parameters: Parameters, ThrowException: ThrowException, AllowUserVariables: AllowUserVariables);

        public static T GetDataObject<T>(IRelmContext CurrentContext, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool AllowUserVariables = false) where T : IRelmModel, new()
            => ObjectResultsHelper.GetDataObject<T>(CurrentContext, QueryString, Parameters: Parameters, ThrowException: ThrowException, AllowUserVariables: AllowUserVariables);

        /// <summary>
        /// Query the database for a single row that returns as a single object of the supplied type.
        /// </summary>
        /// <typeparam name="T">The data return type.</typeparam>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database.</param>
        /// <param name="QueryString">The full SQL query string to be used to retrieve the value requested.</param>
        /// <param name="Parameters">Named parameters for the query.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        /// <returns>An object populated with the requested data.</returns>
        public static T GetDataObject<T>(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null) where T : IRelmModel, new()
            => ObjectResultsHelper.GetDataObject<T>(EstablishedConnection, QueryString, Parameters: Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction);

        /// <summary>
        /// Query the database and return the table as a list of objects of the supplied type.
        /// </summary>
        /// <typeparam name="T">The data return type.</typeparam>
        /// <param name="ConfigConnectionString">An enum type to reference a connection string defined in web.config.</param>
        /// <param name="QueryString">The full SQL query string to be used to retrieve the value requested.</param>
        /// <param name="Parameters">Named parameters for the query.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="AllowUserVariables">Will allow special user variables (variables start with "@") to be defined in the query that will be eventually executed.</param>
        /// <returns>A list of objects populated with the requested data.</returns>
        public static IEnumerable<T> GetDataObjects<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool AllowUserVariables = false) where T : IRelmModel, new()
            => ObjectResultsHelper.GetDataObjects<T>(ConfigConnectionString, QueryString, Parameters: Parameters, ThrowException: ThrowException, AllowUserVariables: AllowUserVariables);

        /// <summary>
        /// Query the database and return the table as a list of objects of the supplied type.
        /// </summary>
        /// <typeparam name="T">The data return type.</typeparam>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database.</param>
        /// <param name="QueryString">The full SQL query string to be used to retrieve the value requested.</param>
        /// <param name="Parameters">Named parameters for the query.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        /// <returns>A list of objects populated with the requested data.</returns>
        public static IEnumerable<T> GetDataObjects<T>(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null) where T : IRelmModel, new()
            => ObjectResultsHelper.GetDataObjects<T>(EstablishedConnection, QueryString, Parameters: Parameters, ThrowException: ThrowException, SqlTransaction);

        public static IEnumerable<T> GetDataObjects<T>(IRelmQuickContext relmContext, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null) where T : IRelmModel, new()
            => ObjectResultsHelper.GetDataObjects<T>(relmContext, QueryString, Parameters: Parameters, ThrowException: ThrowException);

        public static IEnumerable<T> GetDataObjects<T>(IRelmContext relmContext, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null) where T : IRelmModel, new()
            => ObjectResultsHelper.GetDataObjects<T>(relmContext, QueryString, Parameters: Parameters, ThrowException: ThrowException);

        public static IEnumerable<T> GetDataObjects<T>(DataTable existingData) where T : IRelmModel, new()
            => ObjectResultsHelper.GetDataObjects<T>(existingData);

        /// <summary>
        /// Query the database for a single column of data and return as a list of the supplied type.
        /// </summary>
        /// <typeparam name="T">The data return type.</typeparam>
        /// <param name="ConfigConnectionString">An enum type to reference a connection string defined in web.config.</param>
        /// <param name="QueryString">The full SQL query string to be used to retrieve the value requested.</param>
        /// <param name="Parameters">Named parameters for the query.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="AllowUserVariables">Will allow special user variables (variables start with "@") to be defined in the query that will be eventually executed.</param>
        /// <returns>A list of data as the specified return type.</returns>
        public static IEnumerable<T> GetDataList<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool AllowUserVariables = false) //where T : DALBaseModel
            => ObjectResultsHelper.GetDataList<T>(ConfigConnectionString, QueryString, Parameters: Parameters, ThrowException: ThrowException, AllowUserVariables: AllowUserVariables);

        /// <summary>
        /// Query the database for a single column of data and return as a list of the supplied type.
        /// </summary>
        /// <typeparam name="T">The data return type.</typeparam>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database.</param>
        /// <param name="QueryString">The full SQL query string to be used to retrieve the value requested.</param>
        /// <param name="Parameters">Named parameters for the query.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        /// <returns>A list of data as the specified return type.</returns>
        public static IEnumerable<T> GetDataList<T>(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null) //where T : DALBaseModel
            => ObjectResultsHelper.GetDataList<T>(EstablishedConnection, QueryString, Parameters: Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction);

        public static IEnumerable<T> GetDataList<T>(IRelmContext relmContext, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true) //where T : DALBaseModel
            => ObjectResultsHelper.GetDataList<T>(relmContext, QueryString, Parameters: Parameters, ThrowException: ThrowException);

        public static IEnumerable<T> GetDataList<T>(IRelmQuickContext relmContext, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true) //where T : DALBaseModel
            => ObjectResultsHelper.GetDataList<T>(relmContext, QueryString, Parameters: Parameters, ThrowException: ThrowException);

        //***************** Table write functions *****************//

        /// <summary>
        /// Gets a partially configured BulkTableWriter objects which can then be used to write data to the database.
        /// </summary>
        /// <typeparam name="T">The type of object to write.</typeparam>
        /// <param name="ConfigConnectionString">An enum type to reference a connection string defined in web.config.</param>
        /// <param name="InsertQuery">The full SQL query to be run on each row insert.</param>
        /// <param name="UseTransaction">Indicate whether to write all the data in a single transaction.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="AllowUserVariables">Will allow special user variables (variables start with "@") to be defined in the query that will be eventually executed.</param>
        /// <returns>An object to used to write data to the database.</returns>
        public static BulkTableWriter<T> GetBulkTableWriter<T>(Enum ConfigConnectionString, string InsertQuery = null, bool UseTransaction = false, bool ThrowException = true, bool AllowUserVariables = false, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false)
            => DataOutputOperations.GetBulkTableWriter<T>(ConfigConnectionString, InsertQuery: InsertQuery, UseTransaction: UseTransaction, ThrowException: ThrowException, AllowUserVariables: AllowUserVariables, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns);

        /// <summary>
        /// Gets a partially configured BulkTableWriter objects which can then be used to write data to the database.
        /// </summary>
        /// <typeparam name="T">The type of object to write.</typeparam>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database.</param>
        /// <param name="InsertQuery">The full SQL query to be run on each row insert.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="UseTransaction">Indicate whether to write all the data in a single transaction.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        /// <returns>An object to used to write data to the database.</returns>
        public static BulkTableWriter<T> GetBulkTableWriter<T>(MySqlConnection EstablishedConnection, string InsertQuery = null, bool ThrowException = true, bool UseTransaction = true, MySqlTransaction SqlTransaction = null, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false)
            => DataOutputOperations.GetBulkTableWriter<T>(EstablishedConnection, InsertQuery: InsertQuery, UseTransaction: UseTransaction, ThrowException: ThrowException, SqlTransaction: SqlTransaction, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns);

        /// <summary>
        /// Gets a partially configured BulkTableWriter objects which can then be used to write data to the database.
        /// </summary>
        /// <typeparam name="T">The type of object to write.</typeparam>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database.</param>
        /// <param name="InsertQuery">The full SQL query to be run on each row insert.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="UseTransaction">Indicate whether to write all the data in a single transaction.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        /// <returns>An object to used to write data to the database.</returns>
        internal static BulkTableWriter<T> GetBulkTableWriter<T>(IRelmContext relmContext, string InsertQuery = null, bool UseTransaction = false, bool ThrowException = true, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false)
            => DataOutputOperations.GetBulkTableWriter<T>(relmContext, InsertQuery: InsertQuery, UseTransaction: UseTransaction, ThrowException: ThrowException, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns);

        /// <summary>
        /// Writes out a single object to the database using a combination of supplied parameters and class attributes.
        /// </summary>
        /// <typeparam name="T">The type of object to write.</typeparam>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database.</param>
        /// <param name="SourceData">The object to write out to the database.</param>
        /// <param name="TableName">The name of the table to write to. If none is supplied, DALHelper attempts to get it from the DALTable attribute.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        /// <param name="ForceType">Force a type other than the specified one to be used when auto-retrieving the table name from the DALTable attribute.</param>
        /// <returns>The total number of rows written to the database.</returns>
        public static int BulkTableWrite<T>(MySqlConnection EstablishedConnection, T SourceData, string TableName = null, MySqlTransaction SqlTransaction = null, Type ForceType = null, int BatchSize = 100, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false)
            => DataOutputOperations.BulkTableWrite<T>(EstablishedConnection, SourceData, TableName, SqlTransaction, ForceType, BatchSize: BatchSize, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns);

        /// <summary>
        /// Writes out a single object to the database using a combination of supplied parameters and class attributes.
        /// </summary>
        /// <typeparam name="T">The type of object to write.</typeparam>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database.</param>
        /// <param name="SourceData">The object to write out to the database.</param>
        /// <param name="TableName">The name of the table to write to. If none is supplied, DALHelper attempts to get it from the DALTable attribute.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        /// <param name="ForceType">Force a type other than the specified one to be used when auto-retrieving the table name from the DALTable attribute.</param>
        /// <returns>The total number of rows written to the database.</returns>
        public static int BulkTableWrite<T>(IRelmContext relmContext, T SourceData, string TableName = null, MySqlTransaction SqlTransaction = null, Type ForceType = null, int BatchSize = 100, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false)
            => DataOutputOperations.BulkTableWrite<T>(relmContext, SourceData, TableName, ForceType, BatchSize: BatchSize, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns);

        /// <summary>
        /// Writes out a list of objects to the database using a combination of supplied parameters and class attributes.
        /// </summary>
        /// <typeparam name="T">The type of object to write.</typeparam>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database.</param>
        /// <param name="SourceData">The list of objects to write out to the database.</param>
        /// <param name="TableName">The name of the table to write to. If none is supplied, DALHelper attempts to get it from the DALTable attribute.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        /// <param name="ForceType">Force a type other than the specified one to be used when auto-retrieving the table name from the DALTable attribute.</param>
        /// <returns>The total number of rows written to the database.</returns>
        public static int BulkTableWrite<T>(MySqlConnection EstablishedConnection, IEnumerable<T> SourceData, string TableName = null, MySqlTransaction SqlTransaction = null, Type ForceType = null, int BatchSize = 100, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false)
            => DataOutputOperations.BulkTableWrite<T>(EstablishedConnection, SourceData, TableName, SqlTransaction, ForceType, BatchSize: BatchSize, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns);

        /// <summary>
        /// Writes out a list of objects to the database using a combination of supplied parameters and class attributes.
        /// </summary>
        /// <typeparam name="T">The type of object to write.</typeparam>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database.</param>
        /// <param name="SourceData">The list of objects to write out to the database.</param>
        /// <param name="TableName">The name of the table to write to. If none is supplied, DALHelper attempts to get it from the DALTable attribute.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        /// <param name="ForceType">Force a type other than the specified one to be used when auto-retrieving the table name from the DALTable attribute.</param>
        /// <returns>The total number of rows written to the database.</returns>
        public static int BulkTableWrite<T>(IRelmContext relmContext, IEnumerable<T> SourceData, string TableName = null, MySqlTransaction SqlTransaction = null, Type ForceType = null, int BatchSize = 100, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false)
            => DataOutputOperations.BulkTableWrite<T>(relmContext, SourceData, TableName, ForceType, BatchSize: BatchSize, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns);

        /// <summary>
        /// Writes out a single object to the database using a combination of supplied parameters and class attributes.
        /// </summary>
        /// <typeparam name="T">The type of object to write.</typeparam>
        /// <param name="ConfigConnectionString">An enum type to reference a connection string defined in web.config.</param>
        /// <param name="SourceData">The object to write out to the database.</param>
        /// <param name="TableName">The name of the table to write to. If none is supplied, DALHelper attempts to get it from the DALTable attribute.</param>
        /// <param name="ForceType">Force a type other than the specified one to be used when auto-retrieving the table name from the DALTable attribute.</param>
        /// <returns>The total number of rows written to the database.</returns>
        public static int BulkTableWrite<T>(Enum ConfigConnectionString, T SourceData, string TableName = null, Type ForceType = null, int BatchSize = 100, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false)
            => DataOutputOperations.BulkTableWrite<T>(ConfigConnectionString, SourceData, TableName, ForceType, BatchSize: BatchSize, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns);

        /// <summary>
        /// Writes out a list of objects to the database using a combination of supplied parameters and class attributes.
        /// </summary>
        /// <typeparam name="T">The type of object to write.</typeparam>
        /// <param name="ConfigConnectionString">An enum type to reference a connection string defined in web.config.</param>
        /// <param name="SourceData">The list of objects to write out to the database.</param>
        /// <param name="TableName">The name of the table to write to. If none is supplied, DALHelper attempts to get it from the DALTable attribute.</param>
        /// <param name="ForceType">Force a type other than the specified one to be used when auto-retrieving the table name from the DALTable attribute.</param>
        /// <returns>The total number of rows written to the database.</returns>
        public static int BulkTableWrite<T>(Enum ConfigConnectionString, IEnumerable<T> SourceData, string TableName = null, Type ForceType = null, int BatchSize = 100, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false)
            => DataOutputOperations.BulkTableWrite<T>(ConfigConnectionString, SourceData, TableName, ForceType, BatchSize: BatchSize, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns);

        //***************** Core functions *****************//

        /// <summary>
        /// Execute a non-returning query on the database with the specified parameters.
        /// </summary>
        /// <param name="ConfigConnectionString">An enum type to reference a connection string defined in web.config.</param>
        /// <param name="QueryString">The full SQL query string to be used when executing the command.</param>
        /// <param name="Parameters">Named parameters for the query.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="UseTransaction">Indicate whether to write all the data in a single transaction.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        /// <param name="AllowUserVariables">Will allow special user variables (variables start with "@") to be defined in the query that will be eventually executed.</param>
        public static void DoDatabaseWork(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
            => DatabaseWorkHelper.DoDatabaseWork(ConfigConnectionString, QueryString, Parameters, ThrowException, UseTransaction, SqlTransaction, AllowUserVariables);

        /// <summary>
        /// Execute a non-returning query on the database with the specified parameters.
        /// </summary>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database.</param>
        /// <param name="QueryString">The full SQL query string to be used when executing the command.</param>
        /// <param name="Parameters">Named parameters for the query.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="UseTransaction">Indicate whether to write all the data in a single transaction.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        public static void DoDatabaseWork(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
            => DatabaseWorkHelper.DoDatabaseWork(EstablishedConnection, QueryString, Parameters, ThrowException, UseTransaction, SqlTransaction);

        /// <summary>
        /// Execute a non-returning query on the database with the specified parameters.
        /// </summary>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database.</param>
        /// <param name="QueryString">The full SQL query string to be used when executing the command.</param>
        /// <param name="Parameters">Named parameters for the query.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="UseTransaction">Indicate whether to write all the data in a single transaction.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        public static void DoDatabaseWork(IRelmContext relmContext, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
            => DatabaseWorkHelper.DoDatabaseWork(relmContext, QueryString, Parameters, ThrowException, UseTransaction);

        /// <summary>
        /// Execute a query on the database and return the number of rows affected.
        /// </summary>
        /// <typeparam name="T">Return type - only accepts String or Int</typeparam>
        /// <param name="ConfigConnectionString">A ConnectionStringTypes type to reference a connection string defined in web.config</param>
        /// <param name="QueryString">The full SQL query string to be used when executing the command.</param>
        /// <param name="Parameters">Dictionary of named parameters</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="UseTransaction">Indicate whether to write all the data in a single transaction.</param>
        /// <returns>Number of rows affected.</returns>
        public static T DoDatabaseWork<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
            => DatabaseWorkHelper.DoDatabaseWork<T>(ConfigConnectionString, QueryString, Parameters, ThrowException, UseTransaction, SqlTransaction, AllowUserVariables);

        /// <summary>
        /// Execute a query on the database and return the number of rows affected.
        /// </summary>
        /// <typeparam name="T">Return type - only accepts String or Int</typeparam>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database.</param>
        /// <param name="QueryString">The full SQL query string to be used when executing the command.</param>
        /// <param name="Parameters">Dictionary of named parameters</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="UseTransaction">Indicate whether to write all the data in a single transaction.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        /// <returns>Number of rows affected.</returns>
        public static T DoDatabaseWork<T>(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
         => DatabaseWorkHelper.DoDatabaseWork<T>(EstablishedConnection, QueryString, Parameters, ThrowException, UseTransaction, SqlTransaction);

        /// <summary>
        /// Execute a query on the database and return the number of rows affected.
        /// </summary>
        /// <typeparam name="T">Return type - only accepts String or Int</typeparam>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database.</param>
        /// <param name="QueryString">The full SQL query string to be used when executing the command.</param>
        /// <param name="Parameters">Dictionary of named parameters</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="UseTransaction">Indicate whether to write all the data in a single transaction.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        /// <returns>Number of rows affected.</returns>
        public static T DoDatabaseWork<T>(IRelmContext relmContext, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
         => DatabaseWorkHelper.DoDatabaseWork<T>(relmContext, QueryString, Parameters, ThrowException, UseTransaction);

        /// <summary>
        /// Execute a query on the database using the provided function without returning a value.
        /// </summary>
        /// <param name="ConfigConnectionString">A ConnectionStringTypes type to reference a connection string defined in web.config</param>
        /// <param name="QueryString">The full SQL query string to be used when executing the command.</param>
        /// <param name="ActionCallback">Custom function to execute when connected to the database.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="UseTransaction">Indicate whether to write all the data in a single transaction.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        /// <param name="AllowUserVariables">Will allow special user variables (variables start with "@") to be defined in the query that will be eventually executed.</param>
        public static void DoDatabaseWork(Enum ConfigConnectionString, string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
            => DatabaseWorkHelper.DoDatabaseWork(ConfigConnectionString, QueryString, ActionCallback, ThrowException, UseTransaction, SqlTransaction, AllowUserVariables);

        /// <summary>
        /// Execute a query on the database using the provided function without returning a value.
        /// </summary>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database.</param>
        /// <param name="QueryString">The full SQL query string to be used when executing the command.</param>
        /// <param name="ActionCallback">Custom function to execute when connected to the database.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="UseTransaction">Indicate whether to write all the data in a single transaction.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        public static void DoDatabaseWork(MySqlConnection EstablishedConnection, string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
            => DatabaseWorkHelper.DoDatabaseWork(EstablishedConnection, QueryString, ActionCallback, ThrowException, UseTransaction, SqlTransaction);

        /// <summary>
        /// Execute a query on the database using the provided function, returning value of of the specified type.
        /// </summary>
        /// <typeparam name="T">The data return type.</typeparam>
        /// <param name="ConfigConnectionString">A ConnectionStringTypes type to reference a connection string defined in web.config</param>
        /// <param name="QueryString">The full SQL query string to be used when executing the command.</param>
        /// <param name="ActionCallback">Custom function to execute when connected to the database.</param>
        /// <param name="ThrowException">Throw exception or cache in LastExecutionException and continue.</param>
        /// <param name="UseTransaction">Indicate whether to write all the data in a single transaction.</param>
        /// <param name="SqlTransaction">Supply an existing transaction for use in this operation.</param>
        /// <param name="AllowUserVariables">Will allow special user variables (variables start with "@") to be defined in the query that will be eventually executed.</param>
        /// <returns>Data of any of the specified type.</returns>
        public static T DoDatabaseWork<T>(Enum ConfigConnectionString, string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
            => DatabaseWorkHelper.DoDatabaseWork<T>(ConfigConnectionString, QueryString, ActionCallback, ThrowException, UseTransaction, SqlTransaction, AllowUserVariables);

        /// <summary>
        /// Execute a query on the database using the provided function, returning value of of the specified type.
        /// </summary>
        /// <typeparam name="T">The data return type.</typeparam>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database</param>
        /// <param name="QueryString">SQL query to retrieve the data requested</param>
        /// <param name="ActionCallback">Customized function to execute when connected to the database</param>
        /// <param name="ThrowException">Throw exception or swallow and return default(T)</param>
        /// <param name="UseTransaction">Specify whether to use a transaction for this call</param>
        /// <returns>Data of any of the specified type.</returns>
        public static T DoDatabaseWork<T>(MySqlConnection EstablishedConnection, string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
            => DatabaseWorkHelper.DoDatabaseWork<T>(EstablishedConnection, QueryString, ActionCallback, ThrowException, UseTransaction, SqlTransaction);

        //***************** Table operations *****************//
        /*
		public static bool TruncateTable<T>(Enum ConnectionStringType, string TableName = null, Type ForceType = null)
			=> TableOperationsHelper.TruncateTable<T>(ConnectionStringType, TableName, ForceType);

		public static bool TruncateTable<T>(MySqlConnection EstablishedConnection, string TableName = null, Type ForceType = null, MySqlTransaction SqlTransaction = null)
			=> TableOperationsHelper.TruncateTable<T>(EstablishedConnection, TableName, ForceType, SqlTransaction);

		public static bool CreateTable<T>(Enum ConnectionStringType)
			=> TableOperationsHelper.CreateTable<T>(ConnectionStringType);

		public static bool CreateTable<T>(MySqlConnection EstablishedConnection, MySqlTransaction SqlTransaction = null)
			=> TableOperationsHelper.CreateTable<T>(EstablishedConnection, SqlTransaction);
		*/
        /// <summary>
        /// Gets the DALTable attribute from any standard DALHelper object, assuming one has been specified.
        /// </summary>
        /// <typeparam name="T">The standard DALHelper object type to get the table name from.</typeparam>
        /// <returns>The table name, if found</returns>
        public static string GetDalTable<T>() where T : IRelmModel, new() => TableOperationsHelper.GetDalTable<T>();

        /// <summary>
        /// Gets the DALTable attribute from any standard DALHelper object, assuming the attribute has been specified.
        /// </summary>
        /// <param name="DalObjectType">The standard DALHelper object type to get the table name from.</param>
        /// <returns>The table name, if found</returns>
        public static string GetDalTable(Type DalObjectType) => TableOperationsHelper.GetDalTable(DalObjectType);

        /// <summary>
        /// Performs a supplied action as wrapped in an auto-generated connection & transaction
        /// </summary>
        /// <param name="ConnectionType"></param>
        /// <param name="ActionWrapper">A function that takes in a connection and transaction, and returns a type <typeparamref name="T"/></param>
        /// <param name="ExceptionHandler"></param>
        public static void StandardConnectionWrapper(Enum ConnectionType, Action<MySqlConnection, MySqlTransaction> ActionWrapper, Action<Exception, string> ExceptionHandler = null)
            => StandardConnectionHelper.StandardConnectionWrapper(ConnectionType, ActionWrapper, ExceptionHandler);

        ///// <summary>
        ///// Performs a supplied action as wrapped in an auto-generated connection & transaction
        ///// </summary>
        ///// <param name="ActionWrapper">A function that takes in a connection and transaction, and returns a type <typeparamref name="T"/></param>
        ///// <param name="ExceptionHandler"></param>
        //public static void StandardConnectionWrapper(Action<MySqlConnection, MySqlTransaction> ActionWrapper, Action<Exception, string> ExceptionHandler = null)
        //    => StandardConnectionHelper.StandardConnectionWrapper(ActionWrapper, ExceptionHandler);

        ///// <summary>
        ///// Performs a supplied action as wrapped in an auto-generated connection & transaction
        ///// </summary>
        ///// <typeparam name="T">Return type of the action</typeparam>
        ///// <param name="ActionWrapper">A function that takes in a connection and transaction, and returns a type <typeparamref name="T"/></param>
        ///// <returns>An object with a type of <typeparamref name="T"/></returns>
        //public static T StandardConnectionWrapper<T>(Func<MySqlConnection, MySqlTransaction, T> ActionWrapper, Action<Exception, string> ExceptionHandler = null)
        //    => StandardConnectionHelper.StandardConnectionWrapper<T>(ActionWrapper, ExceptionHandler);

        /// <summary>
        /// Performs a supplied action as wrapped in an auto-generated connection & transaction
        /// </summary>
        /// <typeparam name="T">Return type of the action</typeparam>
        /// <param name="ConnectionType">The connection type to use for this wrapper</param>
        /// <param name="ActionWrapper">A function that takes in a connection and transaction, and returns a type <typeparamref name="T"/></param>
        /// <returns>An object with a type of <typeparamref name="T"/></returns>
        public static T StandardConnectionWrapper<T>(Enum ConnectionType, Func<MySqlConnection, MySqlTransaction, T> ActionWrapper, Action<Exception, string> ExceptionHandler = null)
            => StandardConnectionHelper.StandardConnectionWrapper<T>(ConnectionType, ActionWrapper, ExceptionHandler);

        /// <summary>
        /// Loads the results of a foreign key or data loader field into the relevant properties of the supplied target.
        /// </summary>
        /// <typeparam name="T">A RelmModel object type to load the data for.</typeparam>
        /// <typeparam name="R">The field type of the target property.</typeparam>
        /// <typeparam name="S"></typeparam>
        /// <param name="relmContextOptionsBuilder">The connection options to use when retrieving data.</param>
        /// <param name="target">The object to load the data onto.</param>
        /// <param name="predicate">A member expression indicating which field to load independently.</param>
        /// <param name="customDataLoader"></param>
        /// <param name="additionalConstraints"></param>
        /// <returns>The target object with the relevant data loaded.</returns>
        public static T LoadForeignKeyField<T, R, S>(RelmContextOptionsBuilder relmContextOptionsBuilder, T target, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader, additionalConstraints).FirstOrDefault();

        public static T LoadForeignKeyField<T, R, S>(RelmContextOptionsBuilder relmContextOptionsBuilder, T target, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader).FirstOrDefault();

        public static T LoadForeignKeyField<T, R>(RelmContextOptionsBuilder relmContextOptionsBuilder, T target, Expression<Func<T, R>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, additionalConstraints).FirstOrDefault();

        public static T LoadForeignKeyField<T, R>(RelmContextOptionsBuilder relmContextOptionsBuilder, T target, Expression<Func<T, R>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate).FirstOrDefault();

        public static T LoadForeignKeyField<T, R, S>(RelmContextOptionsBuilder relmContextOptionsBuilder, T target, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader, additionalConstraints).FirstOrDefault();

        public static T LoadForeignKeyField<T, R, S>(RelmContextOptionsBuilder relmContextOptionsBuilder, T target, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader).FirstOrDefault();

        public static T LoadForeignKeyField<T, R>(RelmContextOptionsBuilder relmContextOptionsBuilder, T target, Expression<Func<T, ICollection<R>>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, additionalConstraints).FirstOrDefault();

        public static T LoadForeignKeyField<T, R>(RelmContextOptionsBuilder relmContextOptionsBuilder, T target, Expression<Func<T, ICollection<R>>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate).FirstOrDefault();

        /// <summary>
        /// Loads the results of a foreign key or data loader field into the relevant properties of the supplied target.
        /// </summary>
        /// <typeparam name="T">A RelmModel object type to load the data for.</typeparam>
        /// <typeparam name="R">The field type of the target property.</typeparam>
        /// <typeparam name="S"></typeparam>
        /// <param name="relmContextOptionsBuilder">The connection options to use when retrieving data.</param>
        /// <param name="target">An ICollection of objects to load the data onto.</param>
        /// <param name="predicate">A member expression indicating which field to load independently.</param>
        /// <param name="customDataLoader"></param>
        /// <param name="additionalConstraints"></param>
        /// <returns>The ICollection of target objects with the relevant data loaded.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R, S>(RelmContextOptionsBuilder relmContextOptionsBuilder, ICollection<T> target, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader, additionalConstraints);

        public static ICollection<T> LoadForeignKeyField<T, R, S>(RelmContextOptionsBuilder relmContextOptionsBuilder, ICollection<T> target, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader);

        public static ICollection<T> LoadForeignKeyField<T, R>(RelmContextOptionsBuilder relmContextOptionsBuilder, ICollection<T> target, Expression<Func<T, R>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, additionalConstraints);

        public static ICollection<T> LoadForeignKeyField<T, R>(RelmContextOptionsBuilder relmContextOptionsBuilder, ICollection<T> target, Expression<Func<T, R>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate);

        public static ICollection<T> LoadForeignKeyField<T, R, S>(RelmContextOptionsBuilder relmContextOptionsBuilder, ICollection<T> target, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader, additionalConstraints);

        public static ICollection<T> LoadForeignKeyField<T, R, S>(RelmContextOptionsBuilder relmContextOptionsBuilder, ICollection<T> target, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader);

        public static ICollection<T> LoadForeignKeyField<T, R>(RelmContextOptionsBuilder relmContextOptionsBuilder, ICollection<T> target, Expression<Func<T, ICollection<R>>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, additionalConstraints);

        public static ICollection<T> LoadForeignKeyField<T, R>(RelmContextOptionsBuilder relmContextOptionsBuilder, ICollection<T> target, Expression<Func<T, ICollection<R>>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate);

        /// <summary>
        /// Loads the results of a data loader to the target object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="target"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static T LoadDataLoaderField<T, R>(IRelmContext relmContext, T target, Expression<Func<T, R>> predicate) where T : IRelmModel, new()
            => new DataLoaderHelper<T>(relmContext, target).LoadField(predicate).FirstOrDefault();

        public static ICollection<T> LoadDataLoaderField<T, R>(IRelmContext relmContext, ICollection<T> target, Expression<Func<T, R>> predicate) where T : IRelmModel, new()
            => new DataLoaderHelper<T>(relmContext, target).LoadField(predicate);
    }
}
