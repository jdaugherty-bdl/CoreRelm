using MySql.Data.MySqlClient;
using CoreRelm.Attributes;
using CoreRelm.Extensions;
using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.Options;
using CoreRelm.Persistence;
using CoreRelm.RelmInternal.Helpers.DataTransfer;
using CoreRelm.RelmInternal.Helpers.DataTransfer.Persistence;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

/*
namespace CoreRelm.Models
{
    /// <summary>
    /// Provides a quick-start context for working with Relm data models and MySQL database connections, supporting
    /// configuration, data set management, and transactional operations.
    /// </summary>
    /// <remarks>RelmQuickContext simplifies the setup and management of database contexts for Relm-based
    /// applications. It supports multiple initialization patterns, including connection strings, MySqlConnection
    /// objects, and configuration builders. The context manages the lifecycle of database connections and transactions,
    /// and provides methods for accessing, querying, and writing data sets. This class is not thread-safe; each
    /// instance should be used by a single thread at a time. Dispose the context when finished to ensure all resources
    /// are released.</remarks>
    [Obsolete("RelmQuickContext is deprecated and will be removed in future versions. Please use RelmContext(autoInitializeDataSets: false) instead.")]
    public class RelmQuickContext : IDisposable, IRelmQuickContext
    {
        /// <summary>
        /// Configures a triggerable event for after the database options are set, the connection is 
        /// open, and any transations have been started.
        /// </summary>
        /// <remarks>Override this method to customize how the context is configured, such as specifying
        /// database providers or additional options. This method is typically called by the framework during context
        /// initialization.</remarks>
        /// <param name="OptionsBuilder">A builder used to configure options for the context. Cannot be null.</param>
        public virtual void OnConfigure(RelmContextOptionsBuilder OptionsBuilder) { }

        /// <summary>
        /// Gets the options builder used to configure the context.
        /// </summary>
        /// <remarks>Use this property to customize context-specific settings before building or
        /// initializing the context. Changes to the options should be made prior to finalizing the context
        /// configuration.</remarks>
        public RelmContextOptionsBuilder ContextOptions { get; private set; }

        private List<PropertyInfo> _attachedProperties;
        private List<string> _currentDatabaseTables;

        private bool localOpenConnection = false;
        private bool localOpenTransaction = false;

        /// <summary>
        /// Initializes a new instance of the RelmQuickContext class with the specified context options and
        /// configuration settings.
        /// </summary>
        /// <remarks>All configuration settings are validated before the context is initialized. This
        /// constructor allows fine-grained control over connection and transaction behavior, as well as SQL
        /// compatibility options.</remarks>
        /// <param name="optionsBuilder">The options builder used to configure the context. Cannot be null.</param>
        /// <param name="autoOpenConnection">Specifies whether the database connection should be automatically opened when the context is created. The
        /// default is <see langword="true"/>.</param>
        /// <param name="autoOpenTransaction">Specifies whether a database transaction should be automatically started when the context is created. The
        /// default is <see langword="false"/>.</param>
        /// <param name="allowUserVariables">Specifies whether user-defined variables are allowed in SQL statements executed by the context. The default
        /// is <see langword="false"/>.</param>
        /// <param name="convertZeroDateTime">Specifies whether zero date/time values should be converted to null when reading from the database. The
        /// default is <see langword="false"/>.</param>
        /// <param name="lockWaitTimeoutSeconds">The number of seconds to wait for a database lock before timing out. The default is 0, which uses the
        /// server's default lock wait timeout.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionsBuilder"/> is null.</exception>
        public RelmQuickContext(RelmContextOptionsBuilder optionsBuilder, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
        {
            ContextOptions = optionsBuilder ?? throw new ArgumentNullException(nameof(optionsBuilder), "RelmContextOptionsBuilder cannot be null.");

            ContextOptions.ValidateAllSettings();

            InitializeContext(autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);
        }

        /// <summary>
        /// Initializes a new instance of the RelmQuickContext class with the specified connection options and behavior
        /// settings.
        /// </summary>
        /// <remarks>Use this constructor to customize context behavior such as connection management,
        /// transaction handling, and query options. These settings affect how the context interacts with the database
        /// and may impact performance or compatibility depending on the database provider.</remarks>
        /// <param name="connectionStringType">The type of connection string to use for configuring the database context. Determines how the context
        /// connects to the underlying data source.</param>
        /// <param name="autoOpenConnection">true to automatically open the database connection when the context is created; otherwise, false. The
        /// default is true.</param>
        /// <param name="autoOpenTransaction">true to automatically begin a transaction when the context is created; otherwise, false. The default is
        /// false.</param>
        /// <param name="allowUserVariables">true to allow user-defined variables in database queries; otherwise, false. The default is false.</param>
        /// <param name="convertZeroDateTime">true to convert zero date/time values from the database to DateTime.MinValue; otherwise, false. The default
        /// is false.</param>
        /// <param name="lockWaitTimeoutSeconds">The maximum number of seconds to wait for a database lock before timing out. Specify 0 to use the default
        /// timeout.</param>
        public RelmQuickContext(Enum connectionStringType, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
        {
            // set the options and allow user to override
            ContextOptions = new RelmContextOptionsBuilder(connectionStringType);

            InitializeContext(autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);
        }

        /// <summary>
        /// Initializes a new instance of the RelmQuickContext class with the specified connection details and
        /// configuration options.
        /// </summary>
        /// <param name="connectionDetails">The connection string or details used to establish a database connection.</param>
        /// <param name="autoOpenConnection">true to automatically open the database connection upon context initialization; otherwise, false.</param>
        /// <param name="autoOpenTransaction">true to automatically begin a transaction when the context is initialized; otherwise, false.</param>
        /// <param name="allowUserVariables">true to allow the use of user-defined variables in database queries; otherwise, false.</param>
        /// <param name="convertZeroDateTime">true to convert zero date/time values to null when reading from the database; otherwise, false.</param>
        /// <param name="lockWaitTimeoutSeconds">The number of seconds to wait for a database lock before timing out. Specify 0 to use the default timeout.</param>
        public RelmQuickContext(string connectionDetails, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
        {
            // set the options and allow user to override
            ContextOptions = new RelmContextOptionsBuilder(connectionDetails);

            InitializeContext(autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);
        }

        /// <summary>
        /// Initializes a new instance of the RelmQuickContext class using the specified MySQL connection and
        /// configuration options.
        /// </summary>
        /// <param name="connection">The MySqlConnection to use for database operations. Cannot be null.</param>
        /// <param name="autoOpenConnection">Specifies whether the database connection should be automatically opened when the context is created. If
        /// <see langword="true"/>, the connection is opened; otherwise, it remains closed until explicitly opened.</param>
        /// <param name="autoOpenTransaction">Specifies whether a database transaction should be automatically started when the context is created. If
        /// <see langword="true"/>, a transaction is started; otherwise, no transaction is started by default.</param>
        /// <param name="allowUserVariables">Specifies whether user-defined variables are allowed in SQL statements executed by this context. If <see
        /// langword="true"/>, user variables are permitted.</param>
        /// <param name="convertZeroDateTime">Specifies whether zero date/time values from the database should be converted to DateTime.MinValue. If <see
        /// langword="true"/>, zero date/time values are converted; otherwise, they are not.</param>
        /// <param name="lockWaitTimeoutSeconds">The number of seconds to wait for a database lock before timing out. Specify 0 to use the default server
        /// setting.</param>
        public RelmQuickContext(MySqlConnection connection, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
        {
            ContextOptions = new RelmContextOptionsBuilder(connection);

            InitializeContext(autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);
        }

        /// <summary>
        /// Initializes a new instance of the RelmQuickContext class using the specified MySQL connection, transaction,
        /// and context options.
        /// </summary>
        /// <param name="connection">The MySqlConnection to use for database operations. Must not be null and should be open or capable of being
        /// opened if <paramref name="autoOpenConnection"/> is <see langword="true"/>.</param>
        /// <param name="transaction">The MySqlTransaction to associate with the context. Can be null if no transaction is required.</param>
        /// <param name="autoOpenConnection">Specifies whether the context should automatically open the connection if it is not already open. If <see
        /// langword="true"/>, the connection will be opened as needed.</param>
        /// <param name="allowUserVariables">Indicates whether user-defined variables are allowed in SQL statements executed by the context. Set to <see
        /// langword="true"/> to enable support for user variables.</param>
        /// <param name="convertZeroDateTime">Specifies whether zero date/time values from the database should be converted to DateTime.MinValue instead
        /// of throwing an exception. Set to <see langword="true"/> to enable conversion.</param>
        /// <param name="lockWaitTimeoutSeconds">The maximum number of seconds to wait for a database lock before timing out. Specify 0 to use the default
        /// server setting.</param>
        public RelmQuickContext(MySqlConnection connection, MySqlTransaction transaction, bool autoOpenConnection = true, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
        {
            ContextOptions = new RelmContextOptionsBuilder(connection, transaction);

            InitializeContext(autoOpenConnection: autoOpenConnection, autoOpenTransaction: false, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);
        }

        private void InitializeContext(bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
        {
            if (ContextOptions.DatabaseConnection == null)
                ContextOptions.SetDatabaseConnection(RelmHelper.GetConnectionFromConnectionString(ContextOptions.DatabaseConnectionString, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds));

            if ((autoOpenConnection || autoOpenTransaction) && ContextOptions.DatabaseConnection != null)
                StartConnection(autoOpenTransaction);

            // call the user's OnConfigure method
            OnConfigure(ContextOptions);
        }

        /// <summary>
        /// Finalizes the RelmQuickContext instance and releases unmanaged resources before the object is reclaimed by
        /// garbage collection.
        /// </summary>
        /// <remarks>This destructor is called automatically by the garbage collector when the object is
        /// no longer accessible. It ensures that any unmanaged resources held by the RelmQuickContext are properly
        /// released. If you have already called Dispose, the finalizer will not release resources again.</remarks>
        ~RelmQuickContext()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases all resources used by the current instance of the class.
        /// </summary>
        /// <remarks>Call this method when you are finished using the object to free unmanaged resources
        /// and perform other cleanup operations. After calling Dispose, the object should not be used
        /// further.</remarks>
        public void Dispose()
        {
            // Implement full disposable pattern
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the object and optionally releases the managed resources.
        /// </summary>
        /// <remarks>This method is called by both the public Dispose() method and the finalizer. When
        /// disposing is true, this method disposes of managed resources such as attached properties that implement
        /// IDisposable. Override this method in a derived class to release additional resources.</remarks>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            EndConnection();

            if (disposing)
            {
                if ((_attachedProperties?.Count() ?? 0) > 0)
                {
                    foreach (var attachedProperty in _attachedProperties)
                    {
                        if (attachedProperty.GetValue(this) is IDisposable disposable)
                            disposable.Dispose();
                        else
                            attachedProperty.SetValue(this, default);
                    }
                }
            }
        }

        /// <summary>
        /// Begins a database transaction on the current MySQL connection and returns a transaction object for managing
        /// the transaction lifecycle.
        /// </summary>
        /// <remarks>If a transaction is already in progress, this method returns the existing transaction
        /// rather than starting a new one. The returned transaction must be committed or rolled back to complete the
        /// operation. Ensure that the underlying database connection is open before calling this method.</remarks>
        /// <returns>A <see cref="MySqlTransaction"/> object representing the started transaction. If a transaction is already
        /// active, returns the existing transaction.</returns>
        public MySqlTransaction BeginTransaction()
        {
            if (ContextOptions.DatabaseTransaction == null)
                ContextOptions.SetDatabaseTransaction(ContextOptions.DatabaseConnection?.BeginTransaction());

            return ContextOptions.DatabaseTransaction;
        }

        /// <summary>
        /// Commits the current database transaction, finalizing all changes made during the transaction.
        /// </summary>
        /// <remarks>If no active transaction exists, this method has no effect. After committing, the
        /// transaction is considered complete and cannot be rolled back. This method should be called only after all
        /// intended changes have been made within the transaction scope.</remarks>
        public void CommitTransaction()
        {
            ContextOptions.DatabaseTransaction?.Commit();

            localOpenTransaction = false;
            ContextOptions.SetDatabaseTransaction(null);
        }

        /// <summary>
        /// Rolls back the current database transaction, reverting all changes made during the transaction.
        /// </summary>
        /// <remarks>If no active transaction exists, this method has no effect. After calling this
        /// method, the transaction is considered closed and cannot be committed.</remarks>
        public void RollbackTransaction()
        {
            ContextOptions.DatabaseTransaction?.Rollback();

            localOpenTransaction = false;
            ContextOptions.SetDatabaseTransaction(null);
        }

        /// <summary>
        /// Rolls back the current database transaction, reverting all changes made during the transaction.
        /// </summary>
        /// <remarks>If no active transaction exists, this method has no effect. After calling this
        /// method, the transaction is considered closed and cannot be committed.</remarks>
        public void RollbackTransactions()
            => RollbackTransaction();

        /// <summary>
        /// Opens the database connection if it is not already open and optionally begins a new transaction.
        /// </summary>
        /// <remarks>If the connection is opened and lockWaitTimeoutSeconds is greater than zero, the
        /// session's lock wait timeout and transaction isolation level are set. If autoOpenTransaction is true and the
        /// connection is open, a new transaction is started automatically.</remarks>
        /// <param name="autoOpenTransaction">true to automatically begin a new transaction after opening the connection; otherwise, false.</param>
        /// <param name="lockWaitTimeoutSeconds">The lock wait timeout, in seconds, to set for the session. Specify a positive value to configure the
        /// timeout; otherwise, no change is made.</param>
        /// <exception cref="InvalidOperationException">Thrown if the database connection does not exist.</exception>
        public void StartConnection(bool autoOpenTransaction = false, int lockWaitTimeoutSeconds = 0)
        {
            if (ContextOptions.DatabaseConnection == null)
                throw new InvalidOperationException("Cannot open a non-existent database connection.");

            if (ContextOptions.DatabaseConnection.State == ConnectionState.Closed)
            {
                ContextOptions.DatabaseConnection.Open();

                localOpenConnection = true;

                if (lockWaitTimeoutSeconds > 0)
                {
                    // For true lock wait timeout, we need to execute a command immediately after opening
                    using (var cmd = ContextOptions.DatabaseConnection.CreateCommand())
                    {
                        cmd.CommandText = $"SET SESSION innodb_lock_wait_timeout = {lockWaitTimeoutSeconds}";
                        cmd.ExecuteNonQuery();

                        // Also set transaction isolation level to help with locks
                        cmd.CommandText = "SET SESSION transaction_isolation = 'READ-COMMITTED'";
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            if (autoOpenTransaction && ContextOptions.DatabaseConnection.State == ConnectionState.Open)
            {
                BeginTransaction();

                localOpenTransaction = true;
            }
        }

        /// <summary>
        /// Ends the current database connection and optionally commits the active transaction.
        /// </summary>
        /// <remarks>If a transaction is active and <paramref name="commitTransaction"/> is <see
        /// langword="true"/>, the transaction is committed before the connection is closed. If no connection is open,
        /// this method has no effect.</remarks>
        /// <param name="commitTransaction">Specifies whether to commit the active transaction before closing the connection. Set to <see
        /// langword="true"/> to commit; otherwise, the transaction will not be committed.</param>
        public void EndConnection(bool commitTransaction = true)
        {
            if ((ContextOptions?.DatabaseConnection?.State ?? System.Data.ConnectionState.Closed) != System.Data.ConnectionState.Closed)
            {
                if (commitTransaction && localOpenTransaction)
                {
                    ContextOptions.DatabaseTransaction?.Commit();

                    localOpenTransaction = false;
                }

                if (localOpenConnection)
                {
                    ContextOptions.DatabaseConnection.Close();

                    localOpenConnection = false;
                }
            }
        }

        /// <summary>
        /// Determines whether there is an active database transaction associated with the current context.
        /// </summary>
        /// <returns>true if a database transaction is currently active; otherwise, false.</returns>
        public bool HasTransaction()
        {
            return ContextOptions.DatabaseTransaction != null && ContextOptions.DatabaseTransaction.Connection != null;
        }

        /// <summary>
        /// Sets the data loader to be used for the specified data set type.
        /// </summary>
        /// <typeparam name="T">The type of model for which the data loader is being set. Must inherit from RelmModel and have a
        /// parameterless constructor.</typeparam>
        /// <param name="dataLoader">The data loader instance that will be associated with the data set of type <typeparamref name="T"/>.</param>
        /// <exception cref="InvalidOperationException">Thrown if a data set for type <typeparamref name="T"/> does not exist.</exception>
        public void SetDataLoader<T>(IRelmDataLoader<T> dataLoader) where T : RelmModel, new()
        {
            if (!HasDataSet<T>())
                throw new InvalidOperationException("No such data set exists");

            GetDataSetType<T>().SetDataLoader(dataLoader);
        }

        /// <summary>
        /// Retrieves a data set for the specified model type.
        /// </summary>
        /// <typeparam name="T">The type of model to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless constructor.</typeparam>
        /// <returns>An <see cref="IRelmDataSet{T}"/> instance for the specified model type <typeparamref name="T"/>.</returns>
        public IRelmDataSet<T> GetDataSet<T>() where T : IRelmModel, new()
        {
            return GetDataSet<T>(false); // auto-initialize
        }

        /// <summary>
        /// Retrieves a strongly typed data set for the specified model type.
        /// </summary>
        /// <typeparam name="T">The type of model for which to retrieve the data set. Must implement <see cref="IRelmModel"/> and have a
        /// parameterless constructor.</typeparam>
        /// <param name="throwException">Specifies whether to throw an exception if the data set cannot be retrieved. If <see langword="true"/>, an
        /// exception is thrown on failure; otherwise, <see langword="null"/> is returned.</param>
        /// <returns>An <see cref="IRelmDataSet{T}"/> instance representing the data set for the specified model type, or <see
        /// langword="null"/> if the data set cannot be retrieved and <paramref name="throwException"/> is <see
        /// langword="false"/>.</returns>
        public IRelmDataSet<T> GetDataSet<T>(bool throwException) where T : IRelmModel, new()
        {
            return GetDataSet(typeof(T), throwException) as IRelmDataSet<T>;
        }

        /// <summary>
        /// Retrieves an instance of a data set of the specified type.
        /// </summary>
        /// <param name="dataSetType">The type of the data set to retrieve. Must implement <see cref="IRelmDataSetBase"/>.</param>
        /// <returns>An instance of <see cref="IRelmDataSetBase"/> corresponding to the specified type.</returns>
        public IRelmDataSetBase GetDataSet(Type dataSetType)
        {
            return GetDataSet(dataSetType, false); // auto-initialize
        }

        /// <summary>
        /// Retrieves the data set instance associated with the specified entity type. If the data set is not
        /// initialized, it can be created automatically or an exception can be thrown based on the provided option.
        /// </summary>
        /// <remarks>If the data set for the specified type is not already initialized and <paramref
        /// name="throwException"/> is <see langword="false"/>, a default data loader is used to create and initialize
        /// the data set. This method only operates on types that are mapped to existing tables in the current
        /// database.</remarks>
        /// <param name="dataSetType">The type of the entity for which to retrieve the data set. Must correspond to a table that exists in the
        /// current database.</param>
        /// <param name="throwException">Specifies whether to throw an exception if the data set for the specified type is not initialized. If <see
        /// langword="true"/>, an exception is thrown; otherwise, a new data set is created if possible.</param>
        /// <returns>An instance of <see cref="IRelmDataSetBase"/> representing the data set for the specified entity type.
        /// Returns a newly created instance if the data set was not previously initialized and <paramref
        /// name="throwException"/> is <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the specified type does not correspond to a table in the current database, if no attached property
        /// is found for the type, or if the data set cannot be initialized and <paramref name="throwException"/> is
        /// <see langword="true"/>.</exception>
        public IRelmDataSetBase GetDataSet(Type dataSetType, bool throwException)
        {
            if ((_attachedProperties?.Count() ?? 0) == 0)
            {
                // cache attached properties to avoid reflection overhead on each call

                // find any properties that are IRelmDataSet<>
                _attachedProperties = this
                    .GetType()
                    .GetProperties()
                    .Where(x => x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(IRelmDataSet<>))
                    .ToList();

                var tableNames = _attachedProperties
                    .Select(prop => (prop.PropertyType.GetGenericArguments()[0].GetCustomAttribute<RelmTable>(false)?.TableName, prop))
                    .Where(x => !string.IsNullOrWhiteSpace(x.TableName))
                    .ToList();

                _currentDatabaseTables = RelmHelper.GetDataList<string>(this, "SHOW TABLES;")
                    .ToList();

                // don't initialize the data sets if the table name is not in the current database
                _attachedProperties = tableNames
                    .Where(x => _currentDatabaseTables.Contains(x.TableName))
                    .Select(x => x.prop)
                    .ToList();
            }

            /*
            if (!_currentDatabaseTables.Contains(RelmHelper.GetDalTable<T>()))
                throw new InvalidOperationException($"Table for type {typeof(T).Name} [{RelmHelper.GetDalTable<T>()}] does not exist in the current database.");

            var attachedProperty = _attachedProperties.FirstOrDefault(x => x.PropertyType.GetGenericArguments().Any(y => y == typeof(T)))
                ?? _attachedProperties.FirstOrDefault(x => x.PropertyType.GetGenericArguments().Any(y => y.IsAssignableFrom(typeof(T))))
                ?? throw new InvalidOperationException($"No attached property found for type {typeof(T).Name}.");

            //var dataSet = _attachedDataSets.FirstOrDefault(ds => ds.GetType().GetGenericArguments()[0] == typeof(T)) as IRelmDataSet<T>;
            var dataSet = attachedProperty.GetValue(this) as IRelmDataSet<T>;
            if (dataSet == null && throwException)
                throw new InvalidOperationException($"DataSet for type {typeof(T).Name} is not initialized.");
            else if (dataSet == null)
            {
                // create a default data loader for the generic type argument then create a dataset and pass the data loader
                object dalDataLoader = null;
                var classDataLoader = typeof(T).GetCustomAttribute<RelmDataLoader>(true);
                if (classDataLoader == null)
                    dalDataLoader = Activator.CreateInstance(typeof(RelmDefaultDataLoader<>).MakeGenericType(typeof(T)), new object[] { ContextOptions });
                else
                    dalDataLoader = Activator.CreateInstance(classDataLoader.LoaderType, new object[] { ContextOptions });

                // create a new instance of the DALDataSet<T> and pass the data loader
                dataSet = Activator.CreateInstance(typeof(RelmDataSet<>).MakeGenericType(typeof(T)), new object[] { this, dalDataLoader }) as IRelmDataSet<T>;
                if (dataSet == null)
                    throw new InvalidOperationException($"Failed to create DataSet for type {typeof(T).Name}.");

                attachedProperty.SetValue(this, dataSet);
            }
            * /
            if (!_currentDatabaseTables.Contains(RelmHelper.GetDalTable(dataSetType)))
                throw new InvalidOperationException($"Table for type {dataSetType.Name} [{RelmHelper.GetDalTable(dataSetType)}] does not exist in the current database.");

            var attachedProperty = _attachedProperties.FirstOrDefault(x => x.PropertyType.GetGenericArguments().Any(y => y == dataSetType))
                ?? _attachedProperties.FirstOrDefault(x => x.PropertyType.GetGenericArguments().Any(y => y.IsAssignableFrom(dataSetType)))
                ?? throw new InvalidOperationException($"No attached property found for type {dataSetType.Name}.");

            //var dataSet = Convert.ChangeType(attachedProperty.GetValue(this), typeof(IRelmDataSet<>).MakeGenericType(dataSetType)) as IRelmDataSetBase;
            var dataSet = attachedProperty.GetValue(this) as IRelmDataSetBase;
            if (dataSet == null)
            {
                if (throwException)
                    throw new InvalidOperationException($"DataSet for type {dataSetType.Name} is not initialized.");

                // create a default data loader for the generic type argument then create a dataset and pass the data loader
                object dalDataLoader = null;
                var classDataLoader = dataSetType.GetCustomAttribute<RelmDataLoader>(true);
                if (classDataLoader == null)
                    dalDataLoader = Activator.CreateInstance(typeof(RelmDefaultDataLoader<>).MakeGenericType(dataSetType), new object[] { ContextOptions });
                else
                    dalDataLoader = Activator.CreateInstance(classDataLoader.LoaderType, new object[] { ContextOptions });

                // create a new instance of the DALDataSet<T> and pass the data loader
                dataSet = Activator.CreateInstance(typeof(RelmDataSet<>).MakeGenericType(dataSetType), new object[] { this, dalDataLoader }) as IRelmDataSetBase;
                if (dataSet == null)
                    throw new InvalidOperationException($"Failed to create DataSet for type {dataSetType.Name}.");

                attachedProperty.SetValue(this, dataSet);
            }

            return dataSet;
        }

        /// <summary>
        /// Search through the list of attached data sets for a data set of the same type as "dataSet", if found replace it, otherwise add it.
        /// </summary>
        /// <typeparam name="T">A class that inherits from RelmModel.</typeparam>
        /// <param name="dataSet">The data set to add/replace with.</param>
        //internal void SetDataSet<T>(IRelmDataSet<T> dataSet) where T : RelmModel, new()
        internal void SetDataSet<T>(T dataSet)
        {
            /*
            if ((_attachedProperties?.Count() ?? 0) == 0)
            {
                // cache attached properties to avoid reflection overhead on each call

                // find any properties that are IRelmDataSet<>
                _attachedProperties = this.GetType().GetProperties().Where(x => x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(IRelmDataSet<>));

                var tableNames = _attachedProperties
                    .Select(prop => (prop.PropertyType.GetGenericArguments()[0].GetCustomAttribute<RelmTable>(false)?.TableName, prop))
                    .Where(x => !string.IsNullOrWhiteSpace(x.TableName))
                    .ToList();

                _currentDatabaseTables = RelmHelper.GetDataList<string>(this, "SHOW TABLES;")
                    .ToList();

                // don't initialize the data sets if the table name is not in the current database
                _attachedProperties = tableNames
                    .Where(x => _currentDatabaseTables.Contains(x.TableName))
                    .Select(x => x.prop)
                    .ToList();
            }
            * /
            if (_attachedProperties == null)
                _attachedProperties = new List<PropertyInfo>();

            var matchedProperties = this
                .GetType()
                .GetProperties()
                .Where(x => x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(IRelmDataSet<>) && x.PropertyType.GenericTypeArguments.Any(y => y == typeof(T)))
                .ToList();

            _attachedProperties.RemoveAll(x => matchedProperties.Any(y => y.Name == x.Name));

            _attachedProperties.AddRange(matchedProperties);

            // instantiate each item in the DALDataSet<T> properties
            foreach (var matchedProperty in matchedProperties)
            {
                var dalDataSetType = matchedProperty.PropertyType.GetGenericArguments()[0];

                // create a default data loader for the generic type argument then create a dataset and pass the data loader
                // check if dalDataSetType has a RelmDataLoader attribute defined at the class level, and create a new instance of the type indicated and save to dalDataLoader
                object dalDataLoader = null;
                var classDataLoader = dalDataSetType.GetCustomAttribute<RelmDataLoader>(true);

                try
                {
                    if (classDataLoader?.LoaderType == null)
                        dalDataLoader = Activator.CreateInstance(typeof(RelmDefaultDataLoader<>).MakeGenericType(dalDataSetType), new object[] { ContextOptions });
                    else
                        dalDataLoader = Activator.CreateInstance(classDataLoader.LoaderType, new object[] { ContextOptions });
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error creating data loader of type [{classDataLoader?.LoaderType?.FullName}] for dataset [{dalDataSetType.Name}]", ex);
                }

                // create a new instance of the DALDataSet<T> and pass the data loader
                var dalDataSet = Activator.CreateInstance(typeof(RelmDataSet<>).MakeGenericType(dalDataSetType), new object[] { this, dalDataLoader });

                matchedProperty.SetValue(this, dalDataSet);
                _attachedProperties.Add(matchedProperty);
            }
            /*
            // First, let's try to find an existing dataSet of the same type.
            var existingDataSet = _attachedDataSets
                .FirstOrDefault(ds => typeof(T).IsInstanceOfType(ds));

            if (existingDataSet != null)
            {
                // If we found it, we replace the property and existing attached data set with the new dataSet.
                this.GetType()
                    .GetProperties()
                    .FirstOrDefault(x => x.PropertyType.IsGenericType && typeof(T).IsInstanceOfType(x.GetValue(this)))
                    .SetValue(this, dataSet);

                var index = _attachedDataSets.IndexOf(existingDataSet);
                _attachedDataSets[index] = dataSet;
            }
            else
            {
                // If we didn't find it, we add the new dataSet to the list.
                _attachedDataSets.Add(dataSet);
            }
            * /
        }

        /// <summary>
        /// Checks if the DALDataSet of a specific type is attached to the current DALContext instance.
        /// </summary>
        /// <typeparam name="T">The type of the dataset, which should inherit from CS_DbModel.</typeparam>
        /// <returns>True if the dataset exists, otherwise false.</returns>
        public bool HasDataSet<T>(bool throwException = true) where T : IRelmModel, new()
        {
            return HasDataSet(typeof(T), throwException: throwException);
        }

        /// <summary>
        /// Checks if the DALDataSet of a specific type is attached to the current DALContext instance.
        /// </summary>
        /// <param name="dataSetType">The System.Type of the dataset to check.</param>
        /// <param name="throwException">Whether to throw an exception if the dataset is not found.</param>
        /// <returns>True if the dataset exists, otherwise false.</returns>
        public bool HasDataSet(Type dataSetType, bool throwException = true)
        {
            return GetDataSetType(dataSetType, throwException: throwException) != null;
        }

        /// <summary>
        /// Retrieves a data set instance for the specified model type.
        /// </summary>
        /// <typeparam name="T">The type of model for which to retrieve the data set. Must implement <see cref="IRelmModel"/> and have a
        /// parameterless constructor.</typeparam>
        /// <returns>An <see cref="IRelmDataSet{T}"/> instance associated with the specified model type.</returns>
        public IRelmDataSet<T> GetDataSetType<T>() where T : IRelmModel, new()
        {
            return GetDataSetType<T>(throwException: true);
        }

        /// <summary>
        /// Gets the dataset of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the dataset, which should inherit from CS_DbModel.</typeparam>
        /// <param name="throwException">Whether to throw an exception if the dataset is not found.</param>
        /// <returns>An instance of IDALDataSet of the specified type.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no matching dataset is found.</exception>
        public IRelmDataSet<T> GetDataSetType<T>(bool throwException) where T : IRelmModel, new()
        {
            return (IRelmDataSet<T>)GetDataSetType(typeof(T), throwException: throwException);
        }

        /// <summary>
        /// Retrieves an instance of a data set corresponding to the specified type.
        /// </summary>
        /// <param name="dataSetType">The type of the data set to retrieve. Must be a valid type that implements the required data set interface.</param>
        /// <returns>An object representing the data set of the specified type.</returns>
        public IRelmDataSetBase GetDataSetType(Type dataSetType)
        {
            return GetDataSetType(dataSetType, throwException: true);
        }

        /// <summary>
        /// Gets the dataset of the given type.
        /// </summary>
        /// <param name="dataSetType">Type of the dataset.</param>
        /// <param name="throwException">Whether to throw an exception if the dataset is not found.</param>
        /// <returns>An IDALDataSetBase instance of the given type.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no matching dataset is found.</exception>
        public IRelmDataSetBase GetDataSetType(Type dataSetType, bool throwException)
        {
            // Find the first property that is of type DALDataSet<> and has a generic type argument that matches DataSetType
            var dataSetProperty = this.GetType()
                .GetProperties()
                .FirstOrDefault(p => p.PropertyType.IsGenericType &&
                                     p.PropertyType.GetGenericTypeDefinition() == typeof(IRelmDataSet<>) &&
                                     p.PropertyType.GetGenericArguments()[0] == dataSetType);

            if (dataSetProperty == null && throwException)
            {
#if DEBUG
                var currentPropertyTypes_DEBUG = this.GetType().GetProperties().Select(x => x.PropertyType).ToList();
                var currentGenericTypes_DEBUG = currentPropertyTypes_DEBUG.Select(x => x.IsGenericType ? x.GetGenericTypeDefinition() : default).ToList();
                var currentGenericArguments_DEBUG = currentPropertyTypes_DEBUG.Select(x => x.IsGenericType ? x.GetGenericArguments() : default).ToList();
#endif

                throw new InvalidOperationException($"No RelmDataSet with generic type [{dataSetType.Name}] found in context [{this.GetType().Name}].");
            }

            return dataSetProperty?.GetValue(this) as IRelmDataSetBase;
        }

        /// <summary>
        /// Retrieves a collection of entities of type <typeparamref name="T"/> from the associated data set.
        /// </summary>
        /// <typeparam name="T">The type of model to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless constructor.</typeparam>
        /// <param name="loadDataLoaders">Specifies whether to load associated data loaders for each entity. Set to <see langword="true"/> to include
        /// related data loaders; otherwise, only the entities are loaded.</param>
        /// <returns>A collection containing all entities of type <typeparamref name="T"/> from the data set. The collection is
        /// empty if no entities are present.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the data set for type <typeparamref name="T"/> is not initialized.</exception>
        public ICollection<T> Get<T>(bool loadDataLoaders = false) where T : IRelmModel, new()
        {
            var dataSet = GetDataSet<T>()
                ?? throw new InvalidOperationException($"DataSet for type {typeof(T).Name} is not initialized.");

            return dataSet.Load(loadDataLoaders: loadDataLoaders);
        }

        /// <summary>
        /// Retrieves a collection of entities of type T that satisfy the specified predicate.
        /// </summary>
        /// <typeparam name="T">The type of entity to retrieve. Must implement IRelmModel and have a parameterless constructor.</typeparam>
        /// <param name="predicate">An expression that defines the conditions each entity must satisfy to be included in the result.</param>
        /// <param name="loadDataLoaders">true to load related data loaders for each entity; otherwise, false. The default is false.</param>
        /// <returns>A collection of entities of type T that match the specified predicate. The collection will be empty if no
        /// entities satisfy the predicate.</returns>
        public ICollection<T> Get<T>(Expression<Func<T, bool>> predicate, bool loadDataLoaders = false) where T : IRelmModel, new()
        {
            return Where(predicate).Load(loadDataLoaders: loadDataLoaders);
        }

        /// <summary>
        /// Returns the first element of type T that matches the specified predicate, or the default value if no such
        /// element is found.
        /// </summary>
        /// <typeparam name="T">The type of model to query. Must implement IRelmModel and have a parameterless constructor.</typeparam>
        /// <param name="predicate">An expression that defines the conditions the returned element must satisfy.</param>
        /// <param name="loadDataLoaders">true to load related data loaders for the returned element; otherwise, false. The default is false.</param>
        /// <returns>The first element of type T that matches the predicate, or default(T) if no match is found.</returns>
        public T FirstOrDefault<T>(Expression<Func<T, bool>> predicate, bool loadDataLoaders = false) where T : IRelmModel, new()
        {
            return Get(predicate, loadDataLoaders: loadDataLoaders).FirstOrDefault();
        }

        /// <summary>
        /// Filters the elements of the data set based on a specified predicate.
        /// </summary>
        /// <remarks>The returned data set is filtered according to the provided predicate and may be
        /// further queried or enumerated. This method does not modify the original data set.</remarks>
        /// <typeparam name="T">The type of model contained in the data set. Must implement <see cref="IRelmModel"/> and have a
        /// parameterless constructor.</typeparam>
        /// <param name="predicate">An expression that defines the conditions each element must satisfy to be included in the result. Cannot be
        /// null.</param>
        /// <returns>An <see cref="IRelmDataSet{T}"/> containing elements that match the specified predicate.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="predicate"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the data set for type <typeparamref name="T"/> is not initialized.</exception>
        public IRelmDataSet<T> Where<T>(Expression<Func<T, bool>> predicate) where T : IRelmModel, new()
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate), "Predicate cannot be null.");
            
            var dataSet = GetDataSet<T>()
                ?? throw new InvalidOperationException($"DataSet for type {typeof(T).Name} is not initialized.");

            return dataSet.Where(predicate);
        }

        /// <summary>
        /// Executes the specified query and returns a collection of data objects of type T that match the query
        /// criteria.
        /// </summary>
        /// <typeparam name="T">The type of data object to return. Must implement IRelmModel and have a parameterless constructor.</typeparam>
        /// <param name="query">The query string to execute. Cannot be null or empty.</param>
        /// <param name="parameters">An optional dictionary of parameters to be used with the query. If null, the query is executed without
        /// parameters.</param>
        /// <returns>A collection of objects of type T that satisfy the query. The collection will be empty if no matching
        /// objects are found.</returns>
        /// <exception cref="ArgumentNullException">Thrown if query is null or empty.</exception>
        public ICollection<T> Run<T>(string query, Dictionary<string, object> parameters = null) where T : IRelmModel, new()
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query), "Query cannot be null or empty.");

            var runResults = RelmHelper.GetDataObjects<T>(this, query, parameters)
                .ToList();

            return runResults;
        }

        /// <summary>
        /// Retrieves the identifier of the most recently inserted row for the current context.
        /// </summary>
        /// <returns>A string containing the last inserted row identifier. Returns an empty string if no row has been inserted.</returns>
        public string GetLastInsertId()
            => RowIdentityHelper.GetLastInsertId(this);

        /// <summary>
        /// Retrieves the external identifier associated with the specified internal identifier for a given table.
        /// </summary>
        /// <param name="table">The name of the table in which to look up the identifier. Cannot be null or empty.</param>
        /// <param name="InternalId">The internal identifier whose corresponding external identifier is to be retrieved. Cannot be null or empty.</param>
        /// <returns>A string containing the external identifier corresponding to the specified internal identifier. Returns null
        /// if no matching identifier is found.</returns>
        public string GetIdFromInternalId(string table, string InternalId)
            => RowIdentityHelper.GetIdFromInternalId(this, table, InternalId);

        /// <summary>
        /// Executes the specified SQL query and returns the first matching data row from the result set.
        /// </summary>
        /// <param name="query">The SQL query to execute. Must be a valid statement that returns at least one row.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be applied to the query. If null, the query is
        /// executed without parameters.</param>
        /// <param name="throwException">true to throw an exception if no matching row is found; otherwise, false to return null.</param>
        /// <returns>A DataRow containing the first result of the query if found; otherwise, null if no matching row exists and
        /// throwException is false.</returns>
        public DataRow GetDataRow(string query, Dictionary<string, object> parameters = null, bool throwException = true)
            => RefinedResultsHelper.GetDataRow(this, query, parameters, throwException: throwException);

        /// <summary>
        /// Executes the specified SQL query and returns the results as a <see cref="DataTable"/>.
        /// </summary>
        /// <remarks>The returned <see cref="DataTable"/> will contain all rows and columns produced by
        /// the query. If the query does not return any results, the <see cref="DataTable"/> will be empty. Ensure that
        /// the query and parameters are properly formatted to avoid runtime errors.</remarks>
        /// <param name="query">The SQL query to execute against the database. Must be a valid query string.</param>
        /// <param name="parameters">An optional dictionary containing parameter names and values to be used in the query. If <see
        /// langword="null"/>, no parameters are applied.</param>
        /// <param name="throwException">Specifies whether to throw an exception if the query fails. If <see langword="true"/>, an exception is
        /// thrown on error; otherwise, the method returns <see langword="null"/>.</param>
        /// <returns>A <see cref="DataTable"/> containing the results of the query, or <see langword="null"/> if the query fails
        /// and <paramref name="throwException"/> is <see langword="false"/>.</returns>
        public DataTable GetDataTable(string query, Dictionary<string, object> parameters = null, bool throwException = true)
            => RefinedResultsHelper.GetDataTable(this, query, parameters, throwException: throwException);

        /// <summary>
        /// Retrieves a data object of the specified type by executing the provided query string with optional
        /// parameters.
        /// </summary>
        /// <typeparam name="T">The type of data object to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="query">The query string used to select the data object. Cannot be null or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. If null, no parameters are
        /// applied.</param>
        /// <param name="throwException">Specifies whether to throw an exception if the query fails. If <see langword="true"/>, an exception is
        /// thrown on failure; otherwise, the method returns the default value of <typeparamref name="T"/>.</param>
        /// <returns>An instance of <typeparamref name="T"/> representing the retrieved data object. Returns the default value of
        /// <typeparamref name="T"/> if no data is found and <paramref name="throwException"/> is <see
        /// langword="false"/>.</returns>
        public T GetDataObject<T>(string query, Dictionary<string, object> parameters = null, bool throwException = true) where T : IRelmModel, new()
            => ObjectResultsHelper.GetDataObject<T>(this, query, parameters, throwException: throwException);

        /// <summary>
        /// Executes the specified query and returns a collection of data objects of type <typeparamref name="T"/> that
        /// match the query criteria.
        /// </summary>
        /// <typeparam name="T">The type of data object to return. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="query">The query string used to select data objects. Must be a valid query for the underlying data source.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used in the query. If <see langword="null"/>, no
        /// parameters are applied.</param>
        /// <param name="throwException">Specifies whether to throw an exception if the query fails. If <see langword="true"/>, an exception is
        /// thrown on error; otherwise, the method returns an empty collection.</param>
        /// <returns>An enumerable collection of data objects of type <typeparamref name="T"/> that satisfy the query. Returns an
        /// empty collection if no matching objects are found or if the query fails and <paramref
        /// name="throwException"/> is <see langword="false"/>.</returns>
        public IEnumerable<T> GetDataObjects<T>(string query, Dictionary<string, object> parameters = null, bool throwException = true) where T : IRelmModel, new()
            => ObjectResultsHelper.GetDataObjects<T>(this, query, parameters, throwException: throwException);

        /// <summary>
        /// Executes the specified query and returns a collection of results mapped to the specified type.
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
        public IEnumerable<T> GetDataList<T>(string query, Dictionary<string, object> parameters = null, bool throwException = true)
            => ObjectResultsHelper.GetDataList<T>(this, query, parameters: parameters, throwException: throwException);

        /// <summary>
        /// Executes the specified SQL query and returns the first column of the first row in the result set, cast to
        /// the specified type.
        /// </summary>
        /// <remarks>Use this method to efficiently retrieve a single value from the database, such as a
        /// count or aggregate. If throwException is set to true and the query fails or returns no result, an exception
        /// will be thrown.</remarks>
        /// <typeparam name="T">The type to which the scalar result will be cast.</typeparam>
        /// <param name="query">The SQL query to execute. Must be a valid statement that returns a single value.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be applied to the query. If null, the query is
        /// executed without parameters.</param>
        /// <param name="throwException">true to throw an exception if the query fails or returns no result; otherwise, false to return the default
        /// value of type T.</param>
        /// <returns>The value of the first column of the first row in the result set, cast to type T. Returns the default value
        /// of T if no result is found and throwException is false.</returns>
        public T GetScalar<T>(string query, Dictionary<string, object> parameters = null, bool throwException = true)
            => RefinedResultsHelper.GetScalar<T>(this, query, parameters: parameters, throwException: throwException);

        /// <summary>
        /// Creates and returns a bulk table writer for efficiently inserting multiple records of type T into the
        /// database.
        /// </summary>
        /// <remarks>Use this method to optimize large-scale data insertions. The returned 
        /// <see cref="BulkTableWriter{T}"/> provides methods for writing batches of data efficiently. Ensure that the configuration
        /// flags match your table schema and insertion requirements.</remarks>
        /// <typeparam name="T">The type of entities to be written to the database table.</typeparam>
        /// <param name="insertQuery">An optional custom SQL insert query to use for bulk operations. If null, a default query is generated based
        /// on the type T.</param>
        /// <param name="useTransaction">Specifies whether the bulk insert operation should be performed within a database transaction. Set to <see
        /// langword="true"/> to ensure atomicity; otherwise, <see langword="false"/>.</param>
        /// <param name="throwException">Specifies whether exceptions encountered during the bulk operation should be thrown. If <see
        /// langword="true"/>, exceptions are propagated; otherwise, errors are suppressed.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns are included in the insert operation. Set to <see langword="true"/>
        /// to allow explicit values for such columns.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns are included in the insert operation. Set to <see langword="true"/> to
        /// allow explicit values for primary keys.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns are included in the insert operation. Set to <see langword="true"/> to
        /// allow explicit values for unique columns.</param>
        /// <returns>A <see cref="BulkTableWriter{T}"/> instance configured for bulk insertion of entities of type T into the database.</returns>
        public BulkTableWriter<T> GetBulkTableWriter<T>(string insertQuery = null, bool useTransaction = false, bool throwException = true, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false)
            => DataOutputOperations.GetBulkTableWriter<T>(this, insertQuery: insertQuery, useTransaction: useTransaction, throwException: throwException, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns);

        /// <summary>
        /// Writes a collection of data to a database table in bulk, optionally using batching and transaction support.
        /// </summary>
        /// <remarks>This method is optimized for high-performance bulk inserts and can be used with or
        /// without an explicit transaction. Adjusting batch size may affect performance and resource usage. Column
        /// inclusion options allow fine-grained control over which table columns are written, which can be useful for
        /// tables with auto-increment, primary key, or unique constraints.</remarks>
        /// <typeparam name="T">The type of the data objects to be written to the table.</typeparam>
        /// <param name="source">The source data to write to the table. This can be a collection or a single object of type <typeparamref
        /// name="T"/>.</param>
        /// <param name="table">The name of the target database table. If <see langword="null"/>, the table name is inferred from the type
        /// <typeparamref name="T"/>.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to use for the bulk write operation. If <see langword="null"/>,
        /// the operation is executed without an explicit transaction.</param>
        /// <param name="forceType">An optional type to override the inferred type of <typeparamref name="T"/> when mapping columns. If <see
        /// langword="null"/>, the type of <typeparamref name="T"/> is used.</param>
        /// <param name="batchSize">The maximum number of rows to write in each batch. Must be greater than zero.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns are included in the write operation. If <see langword="true"/>,
        /// auto-increment columns are written; otherwise, they are excluded.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns are included in the write operation. If <see langword="true"/>,
        /// primary key columns are written; otherwise, they are excluded.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns are included in the write operation. If <see langword="true"/>, unique
        /// columns are written; otherwise, they are excluded.</param>
        /// <returns>The number of rows successfully written to the database table.</returns>
        public int BulkTableWrite<T>(T source, string table = null, MySqlTransaction sqlTransaction = null, Type forceType = null, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false)
            => DataOutputOperations.BulkTableWrite<T>(this, source, table, forceType, batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns);

        /// <summary>
        /// Executes a database operation using the specified SQL query and parameters, with optional exception handling
        /// and transaction support.
        /// </summary>
        /// <remarks>If <paramref name="useTransaction"/> is <see langword="true"/>, the operation is
        /// performed within a transaction, which is committed if successful or rolled back on failure. When <paramref
        /// name="throwException"/> is <see langword="false"/>, errors are suppressed and no exception is thrown, but
        /// the operation may not complete as expected.</remarks>
        /// <param name="query">The SQL query to execute against the database. Cannot be null or empty.</param>
        /// <param name="parameters">An optional dictionary containing parameter names and values to be used with the query. If null, the query
        /// is executed without parameters.</param>
        /// <param name="throwException">Specifies whether to throw an exception if the database operation fails. Set to <see langword="true"/> to
        /// throw exceptions; otherwise, errors are suppressed.</param>
        /// <param name="useTransaction">Specifies whether to execute the operation within a database transaction. Set to <see langword="true"/> to
        /// use a transaction; otherwise, the operation is executed without transactional support.</param>
        public void DoDatabaseWork(string query, Dictionary<string, object> parameters = null, bool throwException = true, bool useTransaction = false)
            => DatabaseWorkHelper.DoDatabaseWork(this, query, parameters, throwException: throwException, useTransaction: useTransaction);

        /// <summary>
        /// Executes a database query and returns the result as the specified type.
        /// </summary>
        /// <remarks>If <paramref name="useTransaction"/> is <see langword="true"/>, the query is executed
        /// within a transaction, which may impact performance and rollback behavior. Ensure that <paramref
        /// name="query"/> and <paramref name="parameters"/> are valid for the target database.</remarks>
        /// <typeparam name="T">The type of the result to return. Must be compatible with the query result.</typeparam>
        /// <param name="query">The SQL query to execute against the database. Cannot be null or empty.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be used with the query. If null, no parameters are
        /// applied.</param>
        /// <param name="throwException">Specifies whether to throw an exception if the query fails. If <see langword="true"/>, an exception is
        /// thrown on error; otherwise, the method returns the default value of <typeparamref name="T"/>.</param>
        /// <param name="useTransaction">Specifies whether to execute the query within a database transaction. If <see langword="true"/>, the query
        /// is executed in a transaction.</param>
        /// <returns>The result of the query cast to the specified type <typeparamref name="T"/>. Returns the default value of
        /// <typeparamref name="T"/> if the query fails and <paramref name="throwException"/> is <see
        /// langword="false"/>.</returns>
        public T DoDatabaseWork<T>(string query, Dictionary<string, object> parameters = null, bool throwException = true, bool useTransaction = false)
         => DatabaseWorkHelper.DoDatabaseWork<T>(this, query, parameters, throwException, useTransaction);

        /// <summary>
        /// Executes a database operation using the specified SQL query and callback, with optional exception handling
        /// and transaction support.
        /// </summary>
        /// <remarks>Use this method to perform custom database work, such as executing queries or
        /// commands, with control over error handling and transactional behavior. The <paramref name="actionCallback"/>
        /// allows you to define how the <see cref="MySqlCommand"/> is used, such as reading results or executing
        /// non-query commands.</remarks>
        /// <param name="query">The SQL query to execute against the database. Cannot be null or empty.</param>
        /// <param name="actionCallback">A callback function that receives the prepared <see cref="MySqlCommand"/> and performs custom logic. The
        /// function should return an object representing the result of the operation.</param>
        /// <param name="throwException">Specifies whether to throw an exception if the database operation fails. If <see langword="true"/>,
        /// exceptions are propagated; otherwise, errors are suppressed.</param>
        /// <param name="useTransaction">Specifies whether the database operation should be executed within a transaction. If <see langword="true"/>,
        /// the operation is wrapped in a transaction.</param>
        public void DoDatabaseWork(string query, Func<MySqlCommand, object> actionCallback, bool throwException = true, bool useTransaction = false)
            => DatabaseWorkHelper.DoDatabaseWork(this, query, actionCallback, throwException, useTransaction);

        /// <summary>
        /// Executes a database operation using the specified query and callback, optionally within a transaction, and
        /// returns the result as the specified type.
        /// </summary>
        /// <remarks>If <paramref name="useTransaction"/> is <see langword="true"/>, the operation is
        /// executed within a transaction, which is committed if successful or rolled back on failure. The behavior when
        /// an error occurs depends on the value of <paramref name="throwException"/>.</remarks>
        /// <typeparam name="T">The type of the result returned by the database operation.</typeparam>
        /// <param name="query">The SQL query to execute against the database. Cannot be null or empty.</param>
        /// <param name="actionCallback">A callback that receives the prepared <see cref="MySqlCommand"/> and performs the desired operation. The
        /// result of this callback is returned as type <typeparamref name="T"/>.</param>
        /// <param name="throwException">Specifies whether to throw an exception if the database operation fails. If <see langword="true"/>,
        /// exceptions are thrown; otherwise, failures are handled silently.</param>
        /// <param name="useTransaction">Specifies whether to execute the database operation within a transaction. If <see langword="true"/>, the
        /// operation is performed in a transaction; otherwise, no transaction is used.</param>
        /// <returns>The result of the database operation as type <typeparamref name="T"/>.</returns>
        public T DoDatabaseWork<T>(string query, Func<MySqlCommand, object> actionCallback, bool throwException = true, bool useTransaction = false)
            => DatabaseWorkHelper.DoDatabaseWork<T>(this, query, actionCallback, throwException, useTransaction);

        /// <summary>
        /// Writes the specified data model to the database using configurable options for batch size and column
        /// handling.
        /// </summary>
        /// <remarks>Adjusting the batch size can impact performance, especially for large data sets.
        /// Enabling writing to auto-increment, primary key, unique, or auto-date columns may result in constraint
        /// violations depending on the database schema.</remarks>
        /// <param name="relmModel">The data model to be written to the database. Cannot be null.</param>
        /// <param name="batchSize">The maximum number of records to write in each batch operation. Must be greater than zero. The default is
        /// 100.</param>
        /// <param name="allowAutoIncrementColumns">Specifies whether columns with auto-increment attributes are included in the write operation. Set to <see
        /// langword="true"/> to allow writing to auto-increment columns; otherwise, <see langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">Specifies whether primary key columns are included in the write operation. Set to <see langword="true"/> to
        /// allow writing to primary key columns; otherwise, <see langword="false"/>.</param>
        /// <param name="allowUniqueColumns">Specifies whether unique columns are included in the write operation. Set to <see langword="true"/> to allow
        /// writing to unique columns; otherwise, <see langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">Specifies whether columns with automatic date attributes are included in the write operation. Set to <see
        /// langword="true"/> to allow writing to auto-date columns; otherwise, <see langword="false"/>.</param>
        /// <returns>The number of records successfully written to the database.</returns>
        public int WriteToDatabase(IRelmModel relmModel, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
            => relmModel.WriteToDatabase(this, batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);

        /// <summary>
        /// Writes the specified collection of Relm models to the database in batches, with options to control how
        /// certain column types are handled during insertion.
        /// </summary>
        /// <remarks>If the collection contains more models than the specified batch size, the write
        /// operation is performed in multiple batches. The behavior of column inclusion is determined by the
        /// corresponding boolean parameters. This method does not guarantee transactional integrity across
        /// batches.</remarks>
        /// <param name="relmModels">The collection of Relm models to be written to the database. Cannot be null.</param>
        /// <param name="batchSize">The maximum number of models to include in each batch write operation. Must be greater than zero.</param>
        /// <param name="allowAutoIncrementColumns">Specifies whether columns marked as auto-increment are included in the write operation. If <see
        /// langword="true"/>, auto-increment columns are written; otherwise, they are excluded.</param>
        /// <param name="allowPrimaryKeyColumns">Specifies whether primary key columns are included in the write operation. If <see langword="true"/>,
        /// primary key columns are written; otherwise, they are excluded.</param>
        /// <param name="allowUniqueColumns">Specifies whether unique columns are included in the write operation. If <see langword="true"/>, unique
        /// columns are written; otherwise, they are excluded.</param>
        /// <param name="allowAutoDateColumns">Specifies whether columns with automatic date values are included in the write operation. If <see
        /// langword="true"/>, auto-date columns are written; otherwise, they are excluded.</param>
        /// <returns>The number of models successfully written to the database.</returns>
        public int WriteToDatabase(IEnumerable<IRelmModel> relmModels, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
            => relmModels.WriteToDatabase(this, batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
    }
}
*/
