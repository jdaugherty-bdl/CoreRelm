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

namespace CoreRelm.Models
{
    public class RelmQuickContext : IDisposable, IRelmQuickContext
    {
        public RelmContextOptionsBuilder ContextOptions { get; private set; }

        private IEnumerable<PropertyInfo> _attachedProperties;
        private List<string> _currentDatabaseTables;

        private bool localOpenConnection = false;
        private bool localOpenTransaction = false;

        public RelmQuickContext(RelmContextOptionsBuilder optionsBuilder, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
        {
            ContextOptions = optionsBuilder ?? throw new ArgumentNullException(nameof(optionsBuilder), "RelmContextOptionsBuilder cannot be null.");

            ContextOptions.ValidateAllSettings();

            InitializeContext(autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);
        }

        public RelmQuickContext(Enum connectionStringType, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
        {
            // set the options and allow user to override
            ContextOptions = new RelmContextOptionsBuilder(connectionStringType);

            InitializeContext(autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);
        }

        public RelmQuickContext(string connectionDetails, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
        {
            // set the options and allow user to override
            ContextOptions = new RelmContextOptionsBuilder(connectionDetails);

            InitializeContext(autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);
        }

        public RelmQuickContext(MySqlConnection connection, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
        {
            ContextOptions = new RelmContextOptionsBuilder(connection);

            InitializeContext(autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);
        }

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
        }

        ~RelmQuickContext()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            // Implement full disposable pattern
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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

        public MySqlTransaction BeginTransaction()
        {
            if (ContextOptions.DatabaseTransaction == null)
                ContextOptions.SetDatabaseTransaction(ContextOptions.DatabaseConnection?.BeginTransaction());

            return ContextOptions.DatabaseTransaction;
        }

        public void CommitTransaction()
        {
            ContextOptions.DatabaseTransaction?.Commit();

            localOpenTransaction = false;
            ContextOptions.SetDatabaseTransaction(null);
        }

        public void RollbackTransaction()
        {
            ContextOptions.DatabaseTransaction?.Rollback();

            localOpenTransaction = false;
            ContextOptions.SetDatabaseTransaction(null);
        }

        public void RollbackTransactions()
            => RollbackTransaction();

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

        public void SetDataLoader<T>(IRelmDataLoader<T> dataLoader) where T : RelmModel, new()
        {
            if (!HasDataSet<T>())
                throw new InvalidOperationException("No such data set exists");

            GetDataSetType<T>().SetDataLoader(dataLoader);
        }

        public IRelmDataSet<T> GetDataSet<T>() where T : IRelmModel, new()
        {
            return GetDataSet<T>(false); // auto-initialize
        }

        public IRelmDataSet<T> GetDataSet<T>(bool throwException) where T : IRelmModel, new()
        {
            return GetDataSet(typeof(T), throwException) as IRelmDataSet<T>;
        }

        public IRelmDataSetBase GetDataSet(Type dataSetType)
        {
            return GetDataSet(dataSetType, false); // auto-initialize
        }

        public IRelmDataSetBase GetDataSet(Type dataSetType, bool throwException)
        {
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
            */
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
        /// <returns>True if the dataset exists, otherwise false.</returns>
        public bool HasDataSet(Type dataSetType, bool throwException = true)
        {
            return GetDataSetType(dataSetType, throwException: throwException) != null;
        }

        public IRelmDataSet<T> GetDataSetType<T>() where T : IRelmModel, new()
        {
            return GetDataSetType<T>(throwException: true);
        }

        /// <summary>
        /// Gets the dataset of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the dataset, which should inherit from CS_DbModel.</typeparam>
        /// <returns>An instance of IDALDataSet of the specified type.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no matching dataset is found.</exception>
        public IRelmDataSet<T> GetDataSetType<T>(bool throwException) where T : IRelmModel, new()
        {
            return (IRelmDataSet<T>)GetDataSetType(typeof(T), throwException: throwException);
        }

        public IRelmDataSetBase GetDataSetType(Type dataSetType)
        {
            return GetDataSetType(dataSetType, throwException: true);
        }

        /// <summary>
        /// Gets the dataset of the given type.
        /// </summary>
        /// <param name="dataSetType">Type of the dataset.</param>
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

        public ICollection<T> Get<T>(bool loadDataLoaders = false) where T : IRelmModel, new()
        {
            var dataSet = GetDataSet<T>()
                ?? throw new InvalidOperationException($"DataSet for type {typeof(T).Name} is not initialized.");

            return dataSet.Load(loadDataLoaders: loadDataLoaders);
        }

        public ICollection<T> Get<T>(Expression<Func<T, bool>> predicate, bool loadDataLoaders = false) where T : IRelmModel, new()
        {
            return Where(predicate).Load(loadDataLoaders: loadDataLoaders);
        }

        public T FirstOrDefault<T>(Expression<Func<T, bool>> predicate, bool loadDataLoaders = false) where T : IRelmModel, new()
        {
            return Get(predicate, loadDataLoaders: loadDataLoaders).FirstOrDefault();
        }

        public IRelmDataSet<T> Where<T>(Expression<Func<T, bool>> predicate) where T : IRelmModel, new()
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate), "Predicate cannot be null.");
            
            var dataSet = GetDataSet<T>()
                ?? throw new InvalidOperationException($"DataSet for type {typeof(T).Name} is not initialized.");

            return dataSet.Where(predicate);
        }

        public ICollection<T> Run<T>(string query, Dictionary<string, object> parameters = null) where T : IRelmModel, new()
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query), "Query cannot be null or empty.");

            var runResults = RelmHelper.GetDataObjects<T>(this, query, parameters)
                .ToList();

            return runResults;
        }

        public string GetLastInsertId()
            => RowIdentityHelper.GetLastInsertId(this);

        public string GetIdFromInternalId(string Table, string InternalId)
            => RowIdentityHelper.GetIdFromInternalId(this, Table, InternalId);

        public DataRow GetDataRow(string query, Dictionary<string, object> parameters = null, bool throwException = true)
            => RefinedResultsHelper.GetDataRow(this, query, parameters, throwException: throwException);

        public DataTable GetDataTable(string query, Dictionary<string, object> parameters = null, bool throwException = true)
            => RefinedResultsHelper.GetDataTable(this, query, parameters, throwException: throwException);

        public T GetDataObject<T>(string QueryString, Dictionary<string, object> Parameters = null, bool throwException = true) where T : IRelmModel, new()
            => ObjectResultsHelper.GetDataObject<T>(this, QueryString, Parameters, throwException: throwException);

        public IEnumerable<T> GetDataObjects<T>(string QueryString, Dictionary<string, object> Parameters = null, bool throwException = true) where T : IRelmModel, new()
            => ObjectResultsHelper.GetDataObjects<T>(this, QueryString, Parameters, throwException: throwException);

        public IEnumerable<T> GetDataList<T>(string QueryString, Dictionary<string, object> Parameters = null, bool throwException = true)
            => ObjectResultsHelper.GetDataList<T>(this, QueryString, parameters: Parameters, throwException: throwException);

        public T GetScalar<T>(string query, Dictionary<string, object> parameters = null, bool throwException = true)
            => RefinedResultsHelper.GetScalar<T>(this, query, parameters: parameters, throwException: throwException);

        public BulkTableWriter<T> GetBulkTableWriter<T>(string InsertQuery = null, bool useTransaction = false, bool throwException = true, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false)
            => DataOutputOperations.GetBulkTableWriter<T>(this, insertQuery: InsertQuery, useTransaction: useTransaction, throwException: throwException, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns);

        public int BulkTableWrite<T>(T SourceData, string TableName = null, MySqlTransaction sqlTransaction = null, Type ForceType = null, int BatchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false)
            => DataOutputOperations.BulkTableWrite<T>(this, SourceData, TableName, ForceType, batchSize: BatchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns);

        public void DoDatabaseWork(string QueryString, Dictionary<string, object> Parameters = null, bool throwException = true, bool useTransaction = false)
            => DatabaseWorkHelper.DoDatabaseWork(this, QueryString, Parameters, throwException: throwException, useTransaction: useTransaction);

        public T DoDatabaseWork<T>(string QueryString, Dictionary<string, object> Parameters = null, bool throwException = true, bool useTransaction = false)
         => DatabaseWorkHelper.DoDatabaseWork<T>(this, QueryString, Parameters, throwException, useTransaction);

        public void DoDatabaseWork(string QueryString, Func<MySqlCommand, object> ActionCallback, bool throwException = true, bool useTransaction = false)
            => DatabaseWorkHelper.DoDatabaseWork(this, QueryString, ActionCallback, throwException, useTransaction);

        public T DoDatabaseWork<T>(string QueryString, Func<MySqlCommand, object> ActionCallback, bool throwException = true, bool useTransaction = false)
            => DatabaseWorkHelper.DoDatabaseWork<T>(this, QueryString, ActionCallback, throwException, useTransaction);

        public int WriteToDatabase(IRelmModel relmModel, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
            => relmModel.WriteToDatabase(this, batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);

        public int WriteToDatabase(IEnumerable<IRelmModel> relmModels, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
            => relmModels.WriteToDatabase(this, batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
    }
}
