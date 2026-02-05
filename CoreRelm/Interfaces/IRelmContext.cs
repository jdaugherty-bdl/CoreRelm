using MySql.Data.MySqlClient;
using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.Options;
using CoreRelm.Persistence;
using CoreRelm.RelmInternal.Helpers.DataTransfer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces
{
    /// <summary>
    /// Defines a context for interacting with a Relm database, providing methods for managing connections, 
    /// transactions, and data operations. This interface supports querying, data manipulation, and bulk operations 
    /// while ensuring proper resource management.
    /// </summary>
    /// <remarks>Implementations of this interface are designed to facilitate database operations in a
    /// structured and  transactional manner. The context provides methods for starting and ending connections, managing
    /// transactions,  and performing CRUD operations on data sets. It also includes support for executing raw queries
    /// and bulk  operations. The interface extends <see cref="IDisposable"/> to ensure that resources are properly
    /// released  when the context is no longer needed.  Thread safety is not guaranteed unless explicitly stated by the
    /// implementation. Users should ensure proper  synchronization when accessing the context from multiple
    /// threads.</remarks>
    public interface IRelmContext
    {
        /// <summary>
        /// Gets the builder used to configure options for the current Relm context.
        /// </summary>
        RelmContextOptionsBuilder ContextOptions { get; }

        /// <summary>
        /// Commits the current transaction, making all changes permanent in the database.
        /// </summary>
        void CommitTransaction();

        /// <summary>
        /// Rolls back the current transaction, undoing any changes made since the transaction began.
        /// </summary>
        /// <remarks>This method should be called to revert changes if an error occurs or if the
        /// transaction cannot be completed successfully.  Ensure that a transaction is active before calling this
        /// method; otherwise, an exception may be thrown.</remarks>
        void RollbackTransaction();

        /// <summary>
        /// Rolls back the current transaction, undoing any changes made since the transaction began.
        /// </summary>
        /// <remarks>This method should be called to revert changes if an error occurs or if the
        /// transaction cannot be completed successfully.  Ensure that a transaction is active before calling this
        /// method; otherwise, an exception may be thrown.</remarks>
        void RollbackTransactions();

        /// <summary>
        /// Configures the data loader for the specified model type.
        /// </summary>
        /// <typeparam name="T">The type of the model that the data loader will handle. Must inherit from <see cref="RelmModel"/> and have a
        /// parameterless constructor.</typeparam>
        /// <param name="dataLoader">The data loader instance to associate with the specified model type. Cannot be <see langword="null"/>.</param>
        void SetDataLoader<T>(IRelmDataLoader<T> dataLoader) where T : RelmModel, new();

        /// <summary>
        /// Starts a connection to the database.
        /// </summary>
        /// <remarks>If <paramref name="autoOpenTransaction"/> is set to <see langword="true"/>, ensure
        /// that the transaction  is committed or rolled back to avoid leaving it open. This method must be called
        /// before performing any  database operations.</remarks>
        /// <param name="autoOpenTransaction">Specifies whether a transaction should be automatically opened after the connection is established.  Pass
        /// <see langword="true"/> to open a transaction automatically; otherwise, <see langword="false"/>.</param>
        /// <param name="lockWaitTimeoutSeconds">The lock wait timeout in seconds. A value of 0 indicates the default timeout for the database.  Specify a positive integer to set a
        /// custom timeout duration.</param>
        void StartConnection(bool autoOpenTransaction = false, int lockWaitTimeoutSeconds = 0);

        /// <summary>
        /// Ends the current connection and optionally commits any active transaction.
        /// </summary>
        /// <remarks>Use this method to cleanly terminate a connection. If a transaction is active, you
        /// can  specify whether to commit or roll it back before the connection is closed. Ensure that  any necessary
        /// operations are completed before calling this method, as the connection  will no longer be available
        /// afterward.</remarks>
        /// <param name="commitTransaction">A value indicating whether to commit the active transaction before ending the connection.  <see
        /// langword="true"/> to commit the transaction; <see langword="false"/> to roll it back.  The default is <see
        /// langword="true"/>.</param>
        void EndConnection(bool commitTransaction = true);

        /// <summary>
        /// Determines whether a dataset of the specified type exists in the current context.
        /// </summary>
        /// <typeparam name="T">The type of the dataset to check for. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="throwException">A value indicating whether to throw an exception if the dataset does not exist.  <see langword="true"/> to
        /// throw an exception; <see langword="false"/> to return <see langword="false"/> instead.</param>
        /// <returns><see langword="true"/> if the dataset of the specified type exists; otherwise, <see langword="false"/>.</returns>
        bool HasDataSet<T>(bool throwException = true) where T : IRelmModel, new();

        /// <summary>
        /// Determines whether a dataset of the specified type is available.
        /// </summary>
        /// <param name="dataSetType">The <see cref="Type"/> of the dataset to check for availability. This parameter cannot be <see
        /// langword="null"/>.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the dataset is not available. If <see
        /// langword="true"/>, an exception is thrown when the dataset is not found; otherwise, the method returns <see
        /// langword="false"/>.</param>
        /// <returns><see langword="true"/> if the dataset of the specified type is available; otherwise, <see
        /// langword="false"/>.</returns>
        bool HasDataSet(Type dataSetType, bool throwException = true);

        /// <summary>
        /// Retrieves an initialized instance of a dataset of the specified type.
        /// </summary>
        /// <remarks>Use this method to access a dataset for a specific model type. The type parameter
        /// <typeparamref name="T"/> must represent a model that conforms to the <see cref="IRelmModel"/>
        /// interface.</remarks>
        /// <typeparam name="T">The type of the dataset to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <returns>An instance of <see cref="IRelmDataSet{T}"/> containing the data for the specified type.</returns>
        IRelmDataSet<T>? GetDataSet<T>() where T : IRelmModel, new();

        /// <summary>
        /// Retrieves an initialized instance of a dataset of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the dataset to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the dataset cannot be retrieved. If <see
        /// langword="true"/>, an exception is thrown on failure; otherwise, <see langword="null"/> is returned.</param>
        /// <returns>An instance of <see cref="IRelmDataSet{T}"/> containing the dataset of type <typeparamref name="T"/>.
        /// Returns <see langword="null"/> if the dataset cannot be retrieved and <paramref name="throwException"/> is
        /// <see langword="false"/>.</returns>
        IRelmDataSet<T>? GetDataSet<T>(bool throwException) where T : IRelmModel, new();

        /// <summary>
        /// Retrieves an initialized instance of a dataset based on the specified type.
        /// </summary>
        /// <param name="dataSetType">The <see cref="Type"/> of the dataset to retrieve. This must be a type that implements <see
        /// cref="IRelmDataSetBase"/>.</param>
        /// <returns>An instance of the dataset that matches the specified type. Returns <see langword="null"/> if no matching
        /// dataset is found.</returns>
        IRelmDataSetBase? GetDataSet(Type dataSetType);

        /// <summary>
        /// Retrieves an initialized instance of a dataset of the specified type.
        /// </summary>
        /// <param name="dataSetType">The <see cref="Type"/> of the dataset to retrieve. This must implement <see cref="IRelmDataSetBase"/>.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the dataset cannot be found.  If <see
        /// langword="true"/>, an exception is thrown when the dataset is not found; otherwise, <see langword="null"/>
        /// is returned.</param>
        /// <returns>An instance of <see cref="IRelmDataSetBase"/> representing the requested dataset, or <see langword="null"/>
        /// if the dataset is not found and <paramref name="throwException"/> is <see langword="false"/>.</returns>
        IRelmDataSetBase? GetDataSet(Type dataSetType, bool throwException);

        /// <summary>
        /// Retrieves an uninitialized dataset of the specified type.
        /// </summary>
        /// <remarks>This method is generic and allows retrieval of datasets for any type that satisfies
        /// the constraints.</remarks>
        /// <typeparam name="T">The type of the dataset to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <returns>An instance of <see cref="IRelmDataSet{T}"/> representing the dataset of the specified type.</returns>
        IRelmDataSet<T>? GetDataSetType<T>() where T : IRelmModel, new();

        /// <summary>
        /// Retrieves an uninitialized dataset of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the dataset to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the dataset cannot be retrieved. If <see
        /// langword="true"/>, an exception is thrown; otherwise, <see langword="null"/> may be returned.</param>
        /// <returns>An instance of <see cref="IRelmDataSet{T}"/> representing the dataset of the specified type, or <see
        /// langword="null"/> if the dataset cannot be retrieved and <paramref name="throwException"/> is <see
        /// langword="false"/>.</returns>
        IRelmDataSet<T>? GetDataSetType<T>(bool throwException) where T : IRelmModel, new();

        /// <summary>
        /// Retrieves an uninitialized dataset of type specified by <paramref name="dataSetType"/>.
        /// </summary>
        /// <param name="dataSetType">The <see cref="Type"/> of the dataset to retrieve. This must implement <see cref="IRelmDataSetBase"/>.</param>
        /// <returns>An instance of <see cref="IRelmDataSetBase"/> that corresponds to the specified <paramref
        /// name="dataSetType"/>.</returns>
        IRelmDataSetBase? GetDataSetType(Type dataSetType);

        /// <summary>
        /// Retrieves an uninitialized dataset of type specified by <paramref name="dataSetType"/>.
        /// </summary>
        /// <param name="dataSetType">The <see cref="Type"/> of the dataset to retrieve. Must implement <see cref="IRelmDataSetBase"/>.</param>
        /// <param name="throwException">A boolean value indicating whether to throw an exception if the specified <paramref name="dataSetType"/> is
        /// invalid. <see langword="true"/> to throw an exception; otherwise, <see langword="false"/>.</param>
        /// <returns>An instance of <see cref="IRelmDataSetBase"/> corresponding to the specified <paramref name="dataSetType"/>.
        /// Returns <see langword="null"/> if <paramref name="throwException"/> is <see langword="false"/> and the
        /// dataset type is invalid.</returns>
        IRelmDataSetBase? GetDataSetType(Type dataSetType, bool throwException);

        /// <summary>
        /// Retrieves a collection of entities of type <typeparamref name="T"/> from the data source.
        /// </summary>
        /// <typeparam name="T">The type of objects to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="loadDataLoaders">A boolean value indicating whether to load associated data loaders. If <see langword="true"/>, data loaders
        /// will be initialized; otherwise, they will not.</param>
        /// <returns>A collection of objects of type <typeparamref name="T"/>. The collection may be empty if no objects are
        /// found.</returns>
        ICollection<T>? Get<T>(bool loadDataLoaders = false) where T : IRelmModel, new();

        /// <summary>
        /// Retrieves a collection of entities of type <typeparamref name="T"/> from the data source that satisfy the specified predicate.
        /// </summary>
        /// <typeparam name="T">The type of the entities to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="predicate">An expression that defines the conditions the entities must satisfy.</param>
        /// <param name="loadDataLoaders">A boolean value indicating whether to load associated data loaders for the retrieved entities. <see
        /// langword="true"/> to load data loaders; otherwise, <see langword="false"/>.</param>
        /// <returns>A collection of entities of type <typeparamref name="T"/> that match the specified predicate.  Returns an
        /// empty collection if no entities match.</returns>
        ICollection<T>? Get<T>(Expression<Func<T, bool>> predicate, bool loadDataLoaders = false) where T : IRelmModel, new();

        /// <summary>
        /// Retrieves the first element of a sequence from the data source that satisfies the specified condition, or a default value if no
        /// such element is found.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the sequence. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="predicate">An expression that defines the condition to test each element for.</param>
        /// <param name="loadDataLoaders">A boolean value indicating whether to load associated data loaders for the retrieved element.  If <see
        /// langword="true"/>, data loaders will be initialized; otherwise, they will not be loaded.</param>
        /// <returns>The first element of type <typeparamref name="T"/> that satisfies the specified condition,  or the default
        /// value of <typeparamref name="T"/> if no such element is found.</returns>
        T? FirstOrDefault<T>(Expression<Func<T, bool>> predicate, bool loadDataLoaders = false) where T : IRelmModel, new();

        /// <summary>
        /// Retrieves data from the data source, filtered based on the specified predicate.
        /// </summary>
        /// <remarks>The predicate is evaluated for each element in the dataset, and only those elements
        /// for which the predicate returns <see langword="true"/> are included in the result.</remarks>
        /// <typeparam name="T">The type of the model in the dataset. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="predicate">An expression that defines the conditions for filtering the dataset.</param>
        /// <returns>A new dataset containing only the elements that satisfy the specified predicate.</returns>
        IRelmDataSet<T> Where<T>(Expression<Func<T, bool>> predicate) where T : IRelmModel, new();

        /// <summary>
        /// Executes the specified query and returns a collection of results mapped to the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the objects in the result collection. Must implement <see cref="IRelmModel"/> and have a
        /// parameterless constructor.</typeparam>
        /// <param name="query">The query string to execute. This string must be a valid query in the context of the underlying data source.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. If null, no parameters are
        /// applied.</param>
        /// <returns>A collection of objects of type <typeparamref name="T"/> representing the query results. The collection will
        /// be empty if no results are found.</returns>
        ICollection<T?> Run<T>(string query, Dictionary<string, object>? parameters = null) where T : IRelmModel, new();

        /// <summary>
        /// Retrieves the row identifier of the most recently inserted record in the database.
        /// </summary>
        /// <remarks>This method is typically used after an insert operation to obtain the unique
        /// identifier generated for the new record. Ensure that the database connection and context are properly
        /// configured before calling this method.</remarks>
        /// <returns>The identifier of the last inserted record as a string. The format and value depend on the database
        /// implementation.</returns>
        string GetLastInsertId();

        /// <summary>
        /// Converts an internal identifier to its corresponding row identifier for a specified table.
        /// </summary>
        /// <param name="table">The name of the table containing the internal identifier. Cannot be null or empty.</param>
        /// <param name="InternalId">The internal identifier to be converted. Cannot be null or empty.</param>
        /// <returns>The row identifier corresponding to the specified internal identifier and table.</returns>
        string GetIdFromInternalId(string table, string InternalId);

        /// <summary>
        /// Executes the specified query and retrieves the first row of the result set as a <see cref="DataRow"/>.
        /// </summary>
        /// <param name="query">The SQL query to execute. Must be a valid SQL statement.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query.  If null, no parameters are
        /// applied.</param>
        /// <param name="throwException">A boolean value indicating whether an exception should be thrown if the query does not return any rows.  If
        /// <see langword="true"/>, an exception is thrown when no rows are found; otherwise, <see langword="null"/> is
        /// returned.</param>
        /// <returns>A <see cref="DataRow"/> representing the first row of the result set, or <see langword="null"/> if no rows
        /// are found  and <paramref name="throwException"/> is set to <see langword="false"/>.</returns>
        DataRow? GetDataRow(string query, Dictionary<string, object>? parameters = null, bool throwException = true);

        /// <summary>
        /// Executes the specified SQL query and returns the results as a <see cref="DataTable"/>.
        /// </summary>
        /// <remarks>This method is designed to execute read-only queries, such as SELECT statements. It
        /// is not intended for executing queries that modify data (e.g., INSERT, UPDATE, DELETE).</remarks>
        /// <param name="query">The SQL query to execute. This must be a valid SQL statement.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. If provided, the parameters
        /// will be added to the query to prevent SQL injection. Defaults to <see langword="null"/>.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if an error occurs during query execution. If <see
        /// langword="true"/>, exceptions will be propagated to the caller. If <see langword="false"/>, the method will
        /// suppress exceptions and return <see langword="null"/> in case of an error. Defaults to <see
        /// langword="true"/>.</param>
        /// <returns>A <see cref="DataTable"/> containing the results of the query. Returns <see langword="null"/> if <paramref
        /// name="throwException"/> is <see langword="false"/> and an error occurs during execution.</returns>
        DataTable? GetDataTable(string query, Dictionary<string, object>? parameters = null, bool throwException = true);

        /// <summary>
        /// Executes the specified query and retrieves a data object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the data object to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="query">The query string used to retrieve the data object. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="parameters">An optional dictionary of parameters to be used in the query. Keys represent parameter names, and values
        /// represent their corresponding values.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the query fails or no data is found.  If <see
        /// langword="true"/>, an exception is thrown; otherwise, <see langword="default"/> is returned.</param>
        /// <returns>An instance of the specified type <typeparamref name="T"/> populated with the data retrieved by the query. 
        /// Returns <see langword="default"/> if no data is found and <paramref name="throwException"/> is <see
        /// langword="false"/>.</returns>
        T? GetDataObject<T>(string query, Dictionary<string, object>? parameters = null, bool throwException = true) where T : IRelmModel, new();

        /// <summary>
        /// Executes a query and retrieves a collection of data objects of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of data objects to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="query">The query string used to retrieve the data objects. Cannot be null or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. If null, no parameters are
        /// applied.</param>
        /// <param name="throwException">A boolean value indicating whether an exception should be thrown if the query fails. If <see
        /// langword="true"/>, an exception is thrown on failure; otherwise, the method returns an empty collection.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the data objects retrieved by the query. Returns an empty
        /// collection if no data is found or if <paramref name="throwException"/> is <see langword="false"/> and the
        /// query fails.</returns>
        IEnumerable<T?>? GetDataObjects<T>(string query, Dictionary<string, object>? parameters = null, bool throwException = true) where T : IRelmModel, new();

        /// <summary>
        /// Executes the specified query and retrieves a collection of data mapped to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to which the query results will be mapped.</typeparam>
        /// <param name="query">The SQL query to execute. Must not be null or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. Can be null if no parameters
        /// are required.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the query fails.  If <see langword="true"/>, an
        /// exception will be thrown on failure; otherwise, the method will return an empty collection.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the results of the query mapped to the specified type.  Returns
        /// an empty collection if no results are found or if <paramref name="throwException"/> is <see
        /// langword="false"/> and the query fails.</returns>
        IEnumerable<T>? GetDataList<T>(string query, Dictionary<string, object>? parameters = null, bool throwException = true);

        /// <summary>
        /// Executes the specified query asynchronously and returns a collection of results mapped to the specified type.
        /// </summary>
        /// <remarks>The method maps each row in the result set to an instance of type T. If
        /// throwException is false and an error occurs during query execution, the method returns an empty collection
        /// instead of throwing an exception.</remarks>
        /// <typeparam name="T">The type to which each result row will be mapped.</typeparam>
        /// <param name="query">The SQL query to execute against the data source.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be applied to the query. If null, the query is
        /// executed without parameters.</param>
        /// <param name="throwException">true to throw an exception if the query fails; otherwise, false to suppress exceptions and return an empty
        /// collection.</param>
        /// <returns>An enumerable collection of objects of type T representing the query results. Returns an empty collection if
        /// no results are found or if an error occurs and throwException is false.</returns>
        Task<IEnumerable<T>?> GetDataListAsync<T>(string query, Dictionary<string, object>? parameters = null, bool throwException = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the specified SQL query and retrieves a single scalar value of the specified type.
        /// </summary>
        /// <remarks>Use this method to retrieve a single value, such as a count or aggregate result, from
        /// a database query. Ensure that the query is written to return only one value; otherwise, an exception may
        /// occur.</remarks>
        /// <typeparam name="T">The type of the scalar value to return.</typeparam>
        /// <param name="query">The SQL query to execute. Must not be null or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to include in the query.  If null, no parameters are
        /// added.</param>
        /// <param name="throwException">A boolean value indicating whether an exception should be thrown if the query fails.  If <see
        /// langword="true"/>, an exception is thrown on failure; otherwise, the default value of <typeparamref
        /// name="T"/> is returned.</param>
        /// <returns>The scalar value of type <typeparamref name="T"/> returned by the query.  If the query does not return a
        /// result and <paramref name="throwException"/> is <see langword="false"/>, the default value of <typeparamref
        /// name="T"/> is returned.</returns>
        T? GetScalar<T>(string query, Dictionary<string, object>? parameters = null, bool throwException = true);

        /// <summary>
        /// Asynchronously executes a SQL query and returns the first column of the first row in the result set as a
        /// scalar value of the specified type.
        /// </summary>
        /// <remarks>Use this method to efficiently retrieve a single value from the database, such as a
        /// count or aggregate. If throwException is set to false and the query yields no result, the method returns
        /// default(T) instead of throwing an exception.</remarks>
        /// <typeparam name="T">The type to which the scalar result is cast. Must be compatible with the value returned by the query.</typeparam>
        /// <param name="query">The SQL query to execute. The query should return a single value.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be applied to the query. Can be null if the query
        /// does not require parameters.</param>
        /// <param name="throwException">true to throw an exception if the query returns no result or if an error occurs; otherwise, false to return
        /// the default value of type T.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the scalar value of type T if
        /// the query succeeds; otherwise, the default value of type T if throwException is false and no result is
        /// found.</returns>
        Task<T?> GetScalarAsync<T>(string query, Dictionary<string, object>? parameters = null, bool throwException = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates and returns a <see cref="BulkTableWriter{T}"/> instance for performing bulk insert operations on a
        /// database table.
        /// </summary>
        /// <remarks>The <see cref="BulkTableWriter{T}"/> provides an efficient way to insert large
        /// amounts of data into a database table.  Use the optional parameters to customize the behavior of the bulk
        /// operation, such as enabling transactions or allowing specific column types.</remarks>
        /// <typeparam name="T">The type of the objects to be written to the database table. Each object represents a row in the table.</typeparam>
        /// <param name="insertQuery">An optional SQL insert query to use for the bulk operation. If <see langword="null"/>, a default query is
        /// generated based on the type <typeparamref name="T"/>.</param>
        /// <param name="throwException">Specifies whether exceptions should be thrown if an error occurs during the bulk operation. If <see
        /// langword="true"/>, exceptions are thrown; otherwise, errors are suppressed.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns in the database table are allowed to be included in the bulk
        /// operation. If <see langword="true"/>, these columns are included; otherwise, they are excluded.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns in the database table are allowed to be included in the bulk
        /// operation. If <see langword="true"/>, these columns are included; otherwise, they are excluded.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns in the database table are allowed to be included in the bulk operation. If
        /// <see langword="true"/>, these columns are included; otherwise, they are excluded.</param>
        /// <returns>A <see cref="BulkTableWriter{T}"/> instance configured for the specified bulk operation settings.</returns>
        BulkTableWriter<T> GetBulkTableWriter<T>(string? insertQuery = null, bool throwException = true, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false);

        /// <summary>
        /// Performs a bulk write operation to insert or update data in the specified database table.
        /// </summary>
        /// <remarks>This method is optimized for high-performance bulk operations and supports optional
        /// batching to handle large datasets efficiently.  Ensure that the source data aligns with the schema of the
        /// target table to avoid runtime errors.</remarks>
        /// <typeparam name="T">The type of the source data. This can be a collection or a single object representing the data to be
        /// written.</typeparam>
        /// <param name="sourceData">The data to be written to the table. Must not be null. If a collection is provided, each item will be
        /// processed in the bulk operation.</param>
        /// <param name="tableName">The name of the target database table. If null, the table name will be inferred from the type <typeparamref
        /// name="T"/>.</param>
        /// <param name="forceType">An optional <see cref="Type"/> to explicitly specify the type of the data being written. If null, the type
        /// will be inferred from <typeparamref name="T"/>.</param>
        /// <param name="batchSize">The maximum number of rows to include in each batch during the bulk write operation. Must be greater than
        /// zero. Defaults to 100.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns in the target table are allowed to be explicitly written. Defaults
        /// to <see langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns in the target table are allowed to be explicitly written. Defaults to
        /// <see langword="false"/>.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns in the target table are allowed to be explicitly written. Defaults to <see
        /// langword="false"/>.</param>
        /// <returns>The number of rows successfully written to the database table.</returns>
        int BulkTableWrite<T>(T sourceData, string? tableName = null, Type? forceType = null, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false);

        /// <summary>
        /// Executes a database operation using the specified query and parameters.
        /// </summary>
        /// <remarks>This method allows for executing parameterized SQL queries with optional transaction
        /// support.  Use the <paramref name="throwException"/> parameter to control error handling behavior.</remarks>
        /// <param name="query">The SQL query to execute. Cannot be null or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query.  If null, no parameters are
        /// applied.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the operation fails.  If <see langword="true"/>,
        /// an exception is thrown on failure; otherwise, the failure is silently handled.</param>
        void DoDatabaseWork(string query, Dictionary<string, object>? parameters = null, bool throwException = true);

        /// <summary>
        /// Executes a database query and returns the result as the specified type.
        /// </summary>
        /// <remarks>Use this method to execute queries that return a single result, such as scalar values
        /// or objects.  Ensure that the type <typeparamref name="T"/> matches the expected result of the query to avoid
        /// runtime errors.</remarks>
        /// <typeparam name="T">The type of the result expected from the query.</typeparam>
        /// <param name="query">The SQL query to execute. Cannot be null or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to include in the query. Can be null if no parameters
        /// are required.</param>
        /// <param name="throwException">Specifies whether an exception should be thrown if the query fails. If <see langword="true"/>, an exception
        /// is thrown on failure; otherwise, the method returns the default value of <typeparamref name="T"/>.</param>
        /// <returns>The result of the query as an instance of type <typeparamref name="T"/>. Returns the default value of
        /// <typeparamref name="T"/> if <paramref name="throwException"/> is <see langword="false"/> and the query
        /// fails.</returns>
        T? DoDatabaseWork<T>(string query, Dictionary<string, object>? parameters = null, bool throwException = true);

        /// <summary>
        /// Executes a database operation using the specified query and callback function.
        /// </summary>
        /// <remarks>This method provides a flexible way to execute database operations by allowing the
        /// caller to  define custom logic through the <paramref name="actionCallback"/> parameter. Ensure that the 
        /// <paramref name="query"/> is properly sanitized to prevent SQL injection attacks.</remarks>
        /// <param name="query">The SQL query to execute. Must not be null or empty.</param>
        /// <param name="actionCallback">A callback function that processes the <see cref="MySqlCommand"/> object and returns a result.  The callback
        /// must not be null.</param>
        /// <param name="throwException">A value indicating whether to throw an exception if an error occurs during the operation.  If <see
        /// langword="true"/>, exceptions will be thrown; otherwise, errors will be suppressed.</param>
        void DoDatabaseWork(string query, Func<MySqlCommand, object> actionCallback, bool throwException = true);

        /// <summary>
        /// Executes a database operation using the specified query and callback function.
        /// </summary>
        /// <remarks>This method provides a flexible way to execute database operations by allowing the
        /// caller to define the specific action to perform using the provided <see cref="MySqlCommand"/> object. If
        /// <paramref name="useTransaction"/> is <see langword="true"/>, the operation will be executed within a
        /// transaction, which will be committed or rolled back based on the success or failure of the
        /// operation.</remarks>
        /// <typeparam name="T">The type of the result returned by the callback function.</typeparam>
        /// <param name="query">The SQL query to be executed. Cannot be null or empty.</param>
        /// <param name="actionCallback">A callback function that defines the operation to perform with the <see cref="MySqlCommand"/> object. The
        /// function should return an object of type <typeparamref name="T"/>.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if an error occurs during the operation. The
        /// default value is <see langword="true"/>.</param>
        /// <returns>The result of the operation, as returned by the <paramref name="actionCallback"/> function.</returns>
        T? DoDatabaseWork<T>(string query, Func<MySqlCommand, object> actionCallback, bool throwException = true);

        /// <summary>
        /// Writes the specified <see cref="IRelmModel"/> to the database in batches.
        /// </summary>
        /// <remarks>This method writes data to the database in batches to optimize performance. The
        /// behavior of the write operation can be customized using the boolean parameters to control the inclusion of
        /// specific column types.</remarks>
        /// <param name="relmModel">The model containing the data to be written to the database. Cannot be <see langword="null"/>.</param>
        /// <param name="batchSize">The number of records to write in each batch. Must be greater than 0. The default value is 100.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether columns with auto-increment constraints are allowed to be written. If <see
        /// langword="true"/>, auto-increment columns will be included in the write operation; otherwise, they will be
        /// excluded.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are allowed to be written. If <see langword="true"/>, primary
        /// key columns will be included in the write operation; otherwise, they will be excluded.</param>
        /// <param name="allowUniqueColumns">A value indicating whether columns with unique constraints are allowed to be written. If <see
        /// langword="true"/>, unique columns will be included in the write operation; otherwise, they will be excluded.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether columns with automatic date generation constraints are allowed to be written. If
        /// <see langword="true"/>, auto-date columns will be included in the write operation; otherwise, they will be
        /// excluded.</param>
        /// <returns>The total number of records successfully written to the database.</returns>
        int WriteToDatabase(IRelmModel relmModel, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false);

        /// <summary>
        /// Writes a collection of Relm models to the database in batches.
        /// </summary>
        /// <remarks>This method processes the provided models in batches to optimize database writes. The
        /// behavior of the write operation can be customized using the optional parameters to control which types of
        /// columns are included.</remarks>
        /// <param name="relmModels">The collection of models to be written to the database. Each model must implement the <see
        /// cref="IRelmModel"/> interface.</param>
        /// <param name="batchSize">The number of models to include in each batch. Must be a positive integer. The default value is 100.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether columns with auto-increment constraints are allowed to be written. If <see
        /// langword="true"/>, auto-increment columns will be included; otherwise, they will be excluded. The default
        /// value is <see langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are allowed to be written. If <see langword="true"/>, primary
        /// key columns will be included; otherwise, they will be excluded. The default value is <see
        /// langword="false"/>.</param>
        /// <param name="allowUniqueColumns">A value indicating whether columns with unique constraints are allowed to be written. If <see
        /// langword="true"/>, unique columns will be included; otherwise, they will be excluded. The default value is
        /// <see langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether columns with automatic date generation constraints are allowed to be written. If
        /// <see langword="true"/>, auto-date columns will be included; otherwise, they will be excluded. The default
        /// value is <see langword="false"/>.</param>
        /// <returns>The total number of models successfully written to the database.</returns>
        int WriteToDatabase(IEnumerable<IRelmModel> relmModels, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false);
    }
}
