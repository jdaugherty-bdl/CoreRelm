using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.Interfaces.Internal;
using CoreRelm.Options;
using CoreRelm.RelmInternal.Helpers.Operations;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models
{
    public class RelmContext : IDisposable, IAsyncDisposable, IRelmContext
    {
        public virtual void OnConfigure(RelmContextOptionsBuilder OptionsBuilder) { }

        public RelmContextOptionsBuilder ContextOptions { get; private set; }

        private IEnumerable<PropertyInfo>? _attachedProperties;
        private List<object> _attachedDataSets = [];

        private bool localOpenConnection = false;
        private bool localOpenTransaction = false;

        public RelmContext(IRelmContext relmContext, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
        {
            if (relmContext == null)
                throw new ArgumentNullException(nameof(relmContext), "RelmContext cannot be null.");
            ContextOptions = relmContext.ContextOptions ?? throw new ArgumentNullException(nameof(relmContext.ContextOptions), "RelmContextOptionsBuilder cannot be null.");
            ContextOptions.ValidateAllSettings();
            InitializeContext(autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);
        }

        public RelmContext(RelmContextOptionsBuilder optionsBuilder, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
        {
            ContextOptions = optionsBuilder ?? throw new ArgumentNullException(nameof(optionsBuilder), "RelmContextOptionsBuilder cannot be null.");
            ContextOptions.ValidateAllSettings();
            InitializeContext(autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);
        }

        public RelmContext(Enum connectionStringType, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
        {
            // set the options and allow user to override
            ContextOptions = new RelmContextOptionsBuilder(connectionStringType);
            InitializeContext(autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);
        }

        public RelmContext(string connectionDetails, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
        {
            // set the options and allow user to override
            ContextOptions = new RelmContextOptionsBuilder(connectionDetails);
            InitializeContext(autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);
        }

        public RelmContext(MySqlConnection connection, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
        {
            ContextOptions = new RelmContextOptionsBuilder(connection);
            InitializeContext(autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);
        }

        public RelmContext(MySqlConnection connection, MySqlTransaction transaction, bool autoOpenConnection = true, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
        {
            ContextOptions = new RelmContextOptionsBuilder(connection, transaction);
            InitializeContext(autoOpenConnection: autoOpenConnection, autoOpenTransaction: false, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);
        }

        private void InitializeContext(bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
        {
            if (ContextOptions.DatabaseConnection == null)
                ContextOptions.SetDatabaseConnection(RelmHelper.GetConnectionFromConnectionString(ContextOptions.DatabaseConnectionString, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds));

            if ((autoOpenConnection || autoOpenTransaction) && ContextOptions.DatabaseConnection != null)
                StartConnection(autoOpenTransaction: autoOpenTransaction, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);

            _attachedDataSets = [];

            // call the user's OnConfigure method
            OnConfigure(ContextOptions);

            InitializeDataSets();
        }

        private void InitializeDataSets()
        {
            // find any properties that are DALDataSet<T>
            _attachedProperties = this.GetType().GetProperties().Where(x => x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(IRelmDataSet<>));

            var tableNames = _attachedProperties
                .Select(attachedProperty => (attachedProperty.PropertyType.GetGenericArguments()[0].GetCustomAttribute<RelmTable>(false)?.TableName, attachedProperty))
                .Where(x => !string.IsNullOrWhiteSpace(x.TableName))
                .ToList();

            var currentDatabaseTables = RelmHelper.GetDataList<string>(this, "SHOW TABLES;")
                .ToList();

            // don't initialize the data sets if the table name is not in the current database
            _attachedProperties = [.. tableNames
                .Where(x => currentDatabaseTables.Contains(x.TableName))
                .Select(x => x.attachedProperty)];

            // instantiate each item in the DALDataSet<T> properties
            foreach (var attachedProperty in _attachedProperties)
            {
                var dalDataSetType = attachedProperty.PropertyType.GetGenericArguments()[0];

                // create a default data loader for the generic type argument then create a dataset and pass the data loader
                // check if dalDataSetType has a RelmDataLoader attribute defined at the class level, and create a new instance of the type indicated and save to dalDataLoader
                object? dalDataLoader = null;
                var classDataLoader = dalDataSetType.GetCustomAttribute<RelmDataLoader>(true);

                try
                {
                    if (classDataLoader?.LoaderType == null)
                        dalDataLoader = Activator.CreateInstance(typeof(RelmDefaultDataLoader<>).MakeGenericType(dalDataSetType), [ContextOptions]);
                    else
                        dalDataLoader = Activator.CreateInstance(classDataLoader.LoaderType, [ContextOptions]);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error creating data loader of type [{classDataLoader?.LoaderType?.FullName}] for dataset [{dalDataSetType.Name}]", ex);
                }

                // create a new instance of the DALDataSet<T> and pass the data loader
                var dalDataSet = Activator.CreateInstance(typeof(RelmDataSet<>).MakeGenericType(dalDataSetType), [this, dalDataLoader])
                    ?? throw new InvalidOperationException($"Error creating dataset of type [{dalDataSetType.Name}]");

                attachedProperty.SetValue(this, dalDataSet);

                _attachedDataSets.Add(dalDataSet);
            }
        }

        public void SetDataLoader<T>(IRelmDataLoader<T> dataLoader) where T : RelmModel, new()
        {
            if (!HasDataSet<T>())
                throw new InvalidOperationException("No such data set exists");

            GetDataSetType<T>().SetDataLoader(dataLoader);
        }

        /// <summary>
        /// Search through the list of attached data sets for a data set of the same type as "dataSet", if found replace it, otherwise add it.
        /// </summary>
        /// <typeparam name="T">A class that inherits from RelmModel.</typeparam>
        /// <param name="dataSet">The data set to add/replace with.</param>
        //internal void SetDataSet<T>(IRelmDataSet<T> dataSet) where T : RelmModel, new()
        internal void SetDataSet<T>(T dataSet)
        {
            if (dataSet == null)
                throw new ArgumentNullException(nameof(dataSet), "DataSet cannot be null.");

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
        }

        public void StartConnection(bool autoOpenTransaction = false, int lockWaitTimeoutSeconds = 0)
        {
            if (ContextOptions.DatabaseConnection == null)
                throw new InvalidOperationException("Cannot open a non-existent database connection.");

            if (ContextOptions.DatabaseConnection.State == System.Data.ConnectionState.Closed)
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

            if (autoOpenTransaction && ContextOptions.DatabaseConnection.State == System.Data.ConnectionState.Open)
            {
                ContextOptions.SetDatabaseTransaction(ContextOptions.DatabaseConnection.BeginTransaction());

                localOpenTransaction = true;
            }
        }

        public void EndConnection(bool commitTransaction = true)
        {
            if ((ContextOptions?.DatabaseConnection?.State ?? System.Data.ConnectionState.Closed) == System.Data.ConnectionState.Closed)
                return;

            if (commitTransaction && localOpenTransaction)
            {
                ContextOptions?.DatabaseTransaction?.Commit();

                localOpenTransaction = false;
            }

            if (localOpenConnection)
            {
                ContextOptions?.DatabaseConnection.Close();

                localOpenConnection = false;
            }
        }

        public bool HasTransaction()
        {
            return ContextOptions.DatabaseTransaction != null && ContextOptions.DatabaseTransaction.Connection != null;
        }

        public MySqlTransaction? BeginTransaction()
        {
            if (ContextOptions == null || ContextOptions.DatabaseConnection == null)
                throw new InvalidOperationException("Cannot begin a transaction without a valid database connection.");

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

        public void RollbackTransactions()
        {
            ContextOptions.DatabaseTransaction?.Rollback();

            localOpenTransaction = false;
            ContextOptions.SetDatabaseTransaction(null);
        }

        public ValueTask DisposeAsync()
        {
            // Implement full disposable pattern
            Dispose();
            return ValueTask.CompletedTask;
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
                if (_attachedProperties != null)
                {
                    foreach (var attachedProperty in _attachedProperties)
                    {
                        if (attachedProperty.GetValue(this) is IDisposable disposable)
                            disposable.Dispose();
                        else
                            attachedProperty.SetValue(this, default);
                    }
                }

                _attachedDataSets.Clear();
            }
        }

        ~RelmContext()
        {
            Dispose(false);
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

        public void SaveAll()
        {
            // loop through each _attachedDataSet and call Save()
            foreach (var attachedDataSet in _attachedDataSets)
            {
                var saveMethod = attachedDataSet.GetType().GetMethod(nameof(RelmDataSet<RelmModel>.Save)) 
                    ?? throw new InvalidOperationException($"No Save method found on dataset of type [{attachedDataSet.GetType().Name}]");

                saveMethod.Invoke(attachedDataSet, null);
            }
        }
    }
}
