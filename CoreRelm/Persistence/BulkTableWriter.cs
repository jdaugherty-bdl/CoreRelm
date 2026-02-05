using MySql.Data.MySqlClient;
using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.RelmInternal.Extensions;
using CoreRelm.RelmInternal.Helpers.DataTransfer;
using CoreRelm.RelmInternal.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoreRelm.Persistence
{
    /// <summary>
    /// Provides functionality for performing high-performance bulk insert operations into a database table with
    /// configurable options for batching, transactions, error handling, and column mapping.
    /// </summary>
    /// <remarks>The <see cref="BulkTableWriter{T}"/> class is designed for efficient bulk data insertion
    /// scenarios, such as importing large datasets or synchronizing data between sources. It supports customization of
    /// batch size, transaction usage, and column inclusion rules, and allows mapping between object properties and
    /// database columns. This class is intended for internal use and is typically instantiated via factory methods or
    /// helper classes within the data access layer. Thread safety is not guaranteed; use separate instances for
    /// concurrent operations.</remarks>
    /// <typeparam name="T">The type of the data items to be written to the database table. Each instance of <typeparamref name="T"/>
    /// represents a row in the target table.</typeparam>
    public class BulkTableWriter<T>
    {
        // config
        private readonly int DEFAULT_BATCH_SIZE = 100; // default size of the write batches

        // exposed by functional methods
        private string? _insertQuery;
        private string? _tableName;
        private string? _databaseName;

        // awkward names here so we can use the nice name for the set method below
        private bool _throwException;
        private bool _allowUserVariables;
        private bool _allowAutoIncrementColumns;
        private bool _allowPrimaryKeyColumns;
        private bool _allowUniqueColumns;
        private bool _allowAutoDateColumns;

        private Dictionary<string, Tuple<MySqlDbType, int, string?, string?>?>? _tableColumns;
        private Dictionary<string, Tuple<MySqlDbType, int, string?, string?>?>? TableColumns => _tableColumns = _tableColumns ?? [];

        private int _batchSize;
        private IEnumerable<T>? _sourceData;
        private MySqlTransaction? _sqlTransaction;
        private readonly IRelmContext? _existingContext;

        // local objects
        private readonly MySqlConnection? _existingConnection;
        private readonly Enum? _connectionName = default;
        private DataTable? _outputTable;
        private static readonly string[] _autoDateColumnNames = new string[] { "create_date", "last_updated" };

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkTableWriter{T}"/> class.
        /// </summary>
        /// <remarks>This constructor is internal and is not intended to be used directly by consumers of
        /// the API.  Instances of <see cref="BulkTableWriter{T}"/> should be created using the factory pattern provided 
        /// by the <c>RelmHelper</c> class.</remarks>
        internal BulkTableWriter() 
        {
            SetupBatchSize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkTableWriter{T}"/> class, which facilitates bulk data insertion
        /// into a database table.
        /// </summary>
        /// <remarks>This constructor provides fine-grained control over the behavior of the bulk data
        /// insertion process, including options for transaction usage, error handling, and column
        /// constraints.</remarks>
        /// <param name="connectionName">The name of the database connection to use. This must be a valid connection identifier.</param>
        /// <param name="insertQuery">An optional SQL insert query to use for the bulk operation. If not provided, a default query will be
        /// generated.</param>
        /// <param name="throwException">Indicates whether exceptions should be thrown when an error occurs. If <see langword="true"/>, exceptions
        /// will be thrown; otherwise, errors will be suppressed.</param>
        /// <param name="allowUserVariables">Indicates whether user-defined variables are allowed in the bulk operation. If <see langword="true"/>, user
        /// variables are permitted; otherwise, they are not.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns are allowed in the bulk operation. If <see langword="true"/>,
        /// auto-increment columns are permitted; otherwise, they are not.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns are allowed in the bulk operation. If <see langword="true"/>, primary
        /// key columns are permitted; otherwise, they are not.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns are allowed in the bulk operation. If <see langword="true"/>, unique
        /// columns are permitted; otherwise, they are not.</param>
        /// <param name="allowAutoDateColumns">Indicates whether auto-generated date columns are allowed in the bulk operation. If <see langword="true"/>,
        /// auto date columns are permitted; otherwise, they are not.</param>
        internal BulkTableWriter(Enum connectionName, string? insertQuery = null, bool throwException = true, bool allowUserVariables = false, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        {
            _connectionName = connectionName;

            CommonSetup(insertQuery, throwException, null, allowUserVariables, allowAutoIncrementColumns, allowPrimaryKeyColumns, allowUniqueColumns, allowAutoDateColumns);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkTableWriter{T}"/> class, which facilitates bulk data insertion
        /// into a MySQL database.
        /// </summary>
        /// <remarks>This constructor is intended for internal use and provides advanced configuration
        /// options for bulk data insertion.  Ensure that the provided <paramref name="existingConnection"/> is open and
        /// valid before using this class.</remarks>
        /// <param name="existingConnection">An existing <see cref="MySqlConnection"/> to use for database operations. This connection must be open
        /// before calling this constructor.</param>
        /// <param name="insertQuery">An optional SQL insert query to use for bulk insertion. If not provided, a default query will be generated
        /// based on the table schema.</param>
        /// <param name="throwException">A value indicating whether exceptions should be thrown when an error occurs. If <see langword="true"/>,
        /// exceptions will be thrown; otherwise, errors will be suppressed.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to use for the bulk operation. If provided, this transaction will
        /// be used instead of creating a new one.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns are allowed in the bulk operation. If <see
        /// langword="true"/>, auto-increment columns will be included.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are allowed in the bulk operation. If <see langword="true"/>,
        /// primary key columns will be included.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns are allowed in the bulk operation. If <see langword="true"/>,
        /// unique columns will be included.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether auto-generated date columns (e.g., timestamp columns) are allowed in the bulk
        /// operation. If <see langword="true"/>, such columns will be included.</param>
        internal BulkTableWriter(MySqlConnection existingConnection, string? insertQuery = null, bool throwException = true, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        {
            _existingConnection = existingConnection;
            _existingContext = new RelmContext(existingConnection, autoInitializeDataSets: false, autoVerifyTables: false);

            CommonSetup(insertQuery, throwException, null, false, allowAutoIncrementColumns, allowPrimaryKeyColumns, allowUniqueColumns, allowAutoDateColumns);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkTableWriter{T}"/> class, which facilitates bulk data insertion
        /// into a MySQL database.
        /// </summary>
        /// <remarks>This constructor is intended for internal use and provides advanced configuration
        /// options for bulk data insertion.  Ensure that the provided <paramref name="existingConnection"/> is open and
        /// valid before using this class.</remarks>
        /// <param name="existingConnection">An existing <see cref="MySqlConnection"/> to use for database operations. This connection must be open
        /// before calling this constructor.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to use for the bulk operation. If provided, this transaction will
        /// be used instead of creating a new one.</param>
        /// <param name="insertQuery">An optional SQL insert query to use for bulk insertion. If not provided, a default query will be generated
        /// based on the table schema.</param>
        /// <param name="throwException">A value indicating whether exceptions should be thrown when an error occurs. If <see langword="true"/>,
        /// exceptions will be thrown; otherwise, errors will be suppressed.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns are allowed in the bulk operation. If <see
        /// langword="true"/>, auto-increment columns will be included.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are allowed in the bulk operation. If <see langword="true"/>,
        /// primary key columns will be included.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns are allowed in the bulk operation. If <see langword="true"/>,
        /// unique columns will be included.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether auto-generated date columns (e.g., timestamp columns) are allowed in the bulk
        /// operation. If <see langword="true"/>, such columns will be included.</param>
        internal BulkTableWriter(MySqlConnection existingConnection, MySqlTransaction sqlTransaction, string? insertQuery = null, bool throwException = true, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        {
            _existingConnection = existingConnection;
            _existingContext = new RelmContext(existingConnection, autoInitializeDataSets: false, autoVerifyTables: false);

            CommonSetup(insertQuery, throwException, sqlTransaction, false, allowAutoIncrementColumns, allowPrimaryKeyColumns, allowUniqueColumns, allowAutoDateColumns);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkTableWriter{T}"/> class, which provides functionality  for
        /// performing bulk insert operations into a database table with configurable options.
        /// </summary>
        /// <remarks>This constructor is intended for internal use and provides fine-grained control over
        /// the behavior of bulk insert operations. It allows customization of the SQL query, transaction usage, and
        /// column inclusion rules.</remarks>
        /// <param name="relmContext">The database context that provides the connection and transaction settings for the bulk operation.</param>
        /// <param name="insertQuery">An optional SQL insert query to use for the bulk operation. If not provided, a default query will be
        /// generated.</param>
        /// <param name="throwException">A value indicating whether exceptions should be thrown during the operation.  If <see langword="true"/>,
        /// exceptions will be thrown; otherwise, they will be suppressed.</param>
        /// <param name="useTransaction">A value indicating whether the bulk operation should be executed within a transaction.  If <see
        /// langword="true"/>, the operation will use a transaction; otherwise, it will not.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns are allowed in the bulk operation.  If <see
        /// langword="true"/>, auto-increment columns will be included; otherwise, they will be excluded.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are allowed in the bulk operation.  If <see
        /// langword="true"/>, primary key columns will be included; otherwise, they will be excluded.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns are allowed in the bulk operation.  If <see langword="true"/>,
        /// unique columns will be included; otherwise, they will be excluded.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether auto-generated date columns are allowed in the bulk operation.  If <see
        /// langword="true"/>, auto-generated date columns will be included; otherwise, they will be excluded.</param>
        internal BulkTableWriter(IRelmContext relmContext, string? insertQuery = null, bool throwException = true, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        {
            _existingConnection = relmContext.ContextOptions.DatabaseConnection;
            _sqlTransaction = relmContext.ContextOptions.DatabaseTransaction;
            _existingContext = relmContext;

            CommonSetup(insertQuery, throwException, _sqlTransaction, false, allowAutoIncrementColumns, allowPrimaryKeyColumns, allowUniqueColumns, allowAutoDateColumns);
        }

        private void CommonSetup(string? insertQuery, bool throwException, MySqlTransaction? sqlTransaction, bool allowUserVariables, bool allowAutoIncrementColumns, bool allowPrimaryKeyColumns, bool allowUniqueColumns, bool allowAutoDateColumns = false)
        {
            _insertQuery = insertQuery;
            _throwException = throwException;
            _allowUserVariables = allowUserVariables;
            _allowAutoIncrementColumns = allowAutoIncrementColumns;
            _allowPrimaryKeyColumns = allowPrimaryKeyColumns;
            _allowUniqueColumns = allowUniqueColumns;
            _allowAutoDateColumns = allowAutoDateColumns;

            _sqlTransaction = sqlTransaction;

            SetupBatchSize();
        }

        /// <summary>
        /// Configures the batch size for bulk writing operations based on the application settings.
        /// </summary>
        /// <remarks>The batch size is determined by the "BulkWriterBatchSize" value in the application
        /// configuration file. If the configuration value is not set or is invalid, a default batch size is
        /// used.</remarks>
        private void SetupBatchSize()
        {
            _batchSize = int.TryParse(ConfigurationManager.AppSettings.AllKeys.Contains("BulkWriterBatchSize") ? ConfigurationManager.AppSettings["BulkWriterBatchSize"] : null, out int bulkWriterBatchSize) ? bulkWriterBatchSize : DEFAULT_BATCH_SIZE;
        }

        /// <summary>
        /// Writes data to the underlying output stream.
        /// </summary>
        /// <remarks>This overload calls the <see cref="Write(Func{string, T, object})"/> method with a <see
        /// langword="null"/> argument.</remarks>
        /// <returns>The result of the write operation, as returned by the <see cref="Write(Func{string, T, object})"/> method.</returns>
        public int Write()
        {
            return Write(null);
        }

        /// <summary>
        /// Executes a batch operation to process and insert data into a database using the specified function to
        /// populate a data table.
        /// </summary>
        /// <remarks>This method processes the source data in batches, determined by the configured batch
        /// size, and inserts the data into the database. The method supports multiple database contexts and
        /// connections, and it uses the appropriate one based on the current configuration.</remarks>
        /// <param name="DataTableFunction">A function that takes a column name and a data item of type <typeparamref name="T"/> and returns the value
        /// to be inserted into the data table.</param>
        /// <returns>The total number of records successfully inserted into the database. Returns 0 if there is no source data to
        /// process.</returns>
        public int Write(Func<string, T, object>? DataTableFunction)
        {
            var sourceDataCount = _sourceData?.Count() ?? 0;

            if (sourceDataCount == 0)
                return 0;

            PopulateColumnDetails();

            if (string.IsNullOrWhiteSpace(_insertQuery))
                throw new ArgumentNullException("Error executing Bulk Table Writer: insert query not defined");

            var outputIterations = sourceDataCount <= _batchSize
                ? 1
                : sourceDataCount / _batchSize;

            if (outputIterations * _batchSize < sourceDataCount)
                outputIterations++;

            var recordsInserted = 0;
            for (var i = 0; i < outputIterations; i++)
            {
                CreateOutputDataTable(DataTableFunction, i);

                if (_existingContext != null)
                    recordsInserted += DatabaseWorkHelper.DoDatabaseWork<int>(_existingContext, _insertQuery, CommonDatabaseWork, throwException: _throwException);
                else if (_existingConnection != null)
                {
                    if (_sqlTransaction == null)
                        recordsInserted += DatabaseWorkHelper.DoDatabaseWork<int>(_existingConnection, _insertQuery, CommonDatabaseWork, throwException: _throwException);
                    else
                        recordsInserted += DatabaseWorkHelper.DoDatabaseWork<int>(_existingConnection, _sqlTransaction, _insertQuery, CommonDatabaseWork, throwException: _throwException);
                }
                else
                    recordsInserted += DatabaseWorkHelper.DoDatabaseWork<int>(_connectionName!, _insertQuery, CommonDatabaseWork, throwException: _throwException, allowUserVariables: _allowUserVariables);
            }

            return recordsInserted;
        }

        /// <summary>
        /// Executes a batch insert or update operation on the database using the specified <see cref="MySqlCommand"/>.
        /// </summary>
        /// <remarks>This method configures the provided <see cref="MySqlCommand"/> to use batch
        /// processing for inserting or updating records in the database. The batch size is determined by the internal
        /// configuration of the class.</remarks>
        /// <param name="CommandObject">The <see cref="MySqlCommand"/> object that defines the database operation, including the command text and
        /// parameters.</param>
        /// <returns>The number of rows affected by the batch operation.</returns>
        private object CommonDatabaseWork(MySqlCommand CommandObject)
        {
            CommandObject.UpdatedRowSource = UpdateRowSource.None;

            // add all the parameters and point to the source table
            CommandObject
                .Parameters
                .AddRange(TableColumns
                    ?.Select(x => new MySqlParameter($"@{x.Key}", x.Value.Item1, x.Value.Item2, x.Key))
                    .ToArray());

            // Specify the number of records to be Inserted/Updated in one go. Default is 1.
            var adpt = new MySqlDataAdapter
            {
                InsertCommand = CommandObject,
                UpdateBatchSize = _batchSize
            };

            // output the data using batch output
            return adpt.Update(_outputTable!);
        }

        /// <summary>
        /// Populates the column details for the current table, including generating the insert query and column
        /// definitions if they are not already defined.
        /// </summary>
        /// <remarks>This method automatically retrieves the table schema from the database and constructs
        /// the necessary insert query and column definitions. It excludes certain columns, such as auto-increment
        /// fields and boilerplate columns (e.g., "create_date" and "last_updated"), based on the configuration. If the
        /// table name is not specified, an <see cref="ArgumentNullException"/> is thrown.</remarks>
        /// <exception cref="ArgumentNullException">Thrown if the table name is not defined.</exception>
        /// <exception cref="KeyNotFoundException">Thrown if an invalid field type is encountered while converting column definitions.</exception>
        private void PopulateColumnDetails()
        {
            // if we have no table columns or insert query, build them automatically
            if ((TableColumns?.Count ?? 0) == 0 || string.IsNullOrWhiteSpace(_insertQuery))
            {
                // we need a table name at minimum to autobuild the rest
                if (string.IsNullOrWhiteSpace(_tableName))
                    throw new ArgumentNullException("Error auto-populating Bulk Table Writer call: table name not defined");

                // pull the table details from the database
                List<DALTableRowDescriptor?>? currentTableDetails = null;
                if (_existingContext != null)
                    currentTableDetails = [.. RelmHelper.GetDataObjects<DALTableRowDescriptor>(_existingContext, $"DESCRIBE {(string.IsNullOrWhiteSpace(_databaseName) ? string.Empty : $"{_databaseName}.")}{_tableName}")];
                else if (_existingConnection != null)
                {
                    if (_sqlTransaction == null)
                        currentTableDetails = [.. RelmHelper.GetDataObjects<DALTableRowDescriptor>(_existingConnection, $"DESCRIBE {(string.IsNullOrWhiteSpace(_databaseName) ? string.Empty : $"{_databaseName}.")}{_tableName}")];
                    else
                        currentTableDetails = [.. RelmHelper.GetDataObjects<DALTableRowDescriptor>(_existingConnection, _sqlTransaction, $"DESCRIBE {(string.IsNullOrWhiteSpace(_databaseName) ? string.Empty : $"{_databaseName}.")}{_tableName}")];
                }
                else
                    currentTableDetails = [.. RelmHelper.GetDataObjects<DALTableRowDescriptor>(_connectionName!, $"DESCRIBE {(string.IsNullOrWhiteSpace(_databaseName) ? string.Empty : $"{_databaseName}.")}{_tableName}")];

                // use all column for insert EXCEPT autonumber fields and the boilerplate create_date and last_updated columns
                var insertColumns = currentTableDetails
                    .Where(x => (_allowAutoIncrementColumns || (!_allowAutoIncrementColumns && !(x?.Extra.Contains("auto_increment") ?? false))) && (_allowAutoDateColumns || (!_allowAutoDateColumns && !_autoDateColumnNames.Contains(x?.Field))));

                // don't update primary key or unique columns on duplicate key as it's unnecessary
                var updateColumns = insertColumns
                    .Where(x => (_allowPrimaryKeyColumns || (!_allowPrimaryKeyColumns && !(x?.Key.Contains("PRI") ?? false))) && (_allowUniqueColumns || (!_allowUniqueColumns && !(x?.Key.Contains("UNI") ?? false))));

                // if we don't have an insert query, make one
                if (string.IsNullOrWhiteSpace(_insertQuery))
                {
                    var newQuery = new StringBuilder();

                    newQuery.Append("INSERT INTO ");
                    newQuery.Append(string.IsNullOrWhiteSpace(_databaseName) ? string.Empty : $"{_databaseName}.");
                    newQuery.Append(_tableName);
                    newQuery.Append(" (`");
                    newQuery.Append(string.Join("`,`", insertColumns.Select(x => x?.Field)));
                    newQuery.Append("`) VALUES (");
                    newQuery.Append(string.Join(",", insertColumns.Select(x => $"@{x?.Field}")));
                    newQuery.Append(") ");
                    newQuery.Append("ON DUPLICATE KEY UPDATE ");
                    newQuery.Append(string.Join(",", updateColumns.Select(x => $"`{x?.Field}` = VALUES(`{x?.Field}`)"))); // don't update primary key or unique columns on duplicate key as it's unnecessary
                    newQuery.Append(';');

                    SetInsertQuery(newQuery.ToString());
                }

                // if we don't have a table columns list, make it
                if ((TableColumns?.Count ?? 0) <= 0)
                {
                    // use all of the insert columns because it's the larger set
                    var columnDefinitions = insertColumns
                        .Select((x) =>
                        {
                            if (x == null)
                                return default;

                            var fieldType = x.Type;
                            var fieldSize = -1;

                            // try to pull the type and size from the column name
                            if (fieldType.Contains('('))
                            {
                                var typeParts = fieldType.Split(['(', ')'], StringSplitOptions.RemoveEmptyEntries);

                                fieldType = typeParts[0];

                                // if there's no size or the size is unparseable, just use -1 when inserting data
                                fieldSize = int.TryParse(typeParts[1], out int sizeField) ? sizeField : -1;
                            }

                            var convertedType = new DALPropertyType_MySQL(fieldType);

                            // we don't already have this conversion defined, throw exception
                            if (convertedType.PropertyType == null)
                                throw new KeyNotFoundException($"Error auto-populating columns for [`{_tableName}`.`{x.Field}`]: Invalid field type [{fieldType}]");

                            return new Tuple<string, MySqlDbType, int, string?, string?>(x.Field, convertedType.PropertyMySqlDbType, fieldSize, null, x.Default);
                        });

                    // add all those columns to the output table
                    AddColumns(columnDefinitions);
                }
            }
        }

        /// <summary>
        /// Creates and populates a <see cref="DataTable"/> with a subset of data from the source collection.
        /// </summary>
        /// <remarks>This method creates a new <see cref="DataTable"/> instance and populates it with rows
        /// corresponding to a batch of data from the source collection. The size of the batch is determined by the
        /// value of <c>_batchSize</c>, and the starting point for the batch is calculated based on <paramref
        /// name="IterationCount"/>. The <paramref name="DataTableFunction"/> is used to generate the values for each
        /// column in the table.</remarks>
        /// <param name="DataTableFunction">A function that maps a column name and a data item of type <typeparamref name="T"/> to a value to be
        /// inserted into the table.</param>
        /// <param name="IterationCount">The zero-based index of the current batch iteration, used to determine which subset of the source data to
        /// include.</param>
        /// <returns>A <see cref="DataTable"/> containing the specified subset of data, with columns defined by <see
        /// cref="TableColumns"/>.</returns>
        private DataTable CreateOutputDataTable(Func<string, T, object>? DataTableFunction, int IterationCount)
        {
            // make a new table
            _outputTable = new DataTable();
            _outputTable.Clear();

            // add the columns
            if (TableColumns != null)
                _outputTable.Columns.AddRange([.. TableColumns.Select(x => new DataColumn(x.Key))]);

            // add the rows
            foreach (var data in _sourceData?.Skip(IterationCount * _batchSize).Take(_batchSize) ?? [])
            {
                _outputTable.Rows.Add(CreateOutputDataRow(_outputTable, data, DataTableFunction));
            }

            return _outputTable;
        }

        /// <summary>
        /// Creates a new <see cref="DataRow"/> in the specified <see cref="DataTable"/> and populates it with data
        /// derived from the provided object and column definitions.
        /// </summary>
        /// <remarks>If <paramref name="DataTableFunction"/> is null, the method attempts to map the
        /// object's properties to the columns of the <paramref name="formData"/> automatically. This includes handling
        /// type conversions, formatting, and truncation based on column definitions. If a property cannot be mapped to
        /// a column, the column is assigned a default value.  If <paramref name="DataTableFunction"/> is provided, it
        /// is used to determine the value for each column based on the column name and the provided object.  Exceptions
        /// may be thrown if data truncation is not allowed and a value exceeds the column's size limit.</remarks>
        /// <param name="formData">The <see cref="DataTable"/> to which the new row will belong.</param>
        /// <param name="RowData">The object containing the data to populate the row. The properties of this object are mapped to the columns
        /// of the <paramref name="formData"/>.</param>
        /// <param name="DataTableFunction">An optional function that provides custom logic for mapping column names to values. If null, default mapping
        /// logic is applied based on the object's properties and column definitions.</param>
        /// <returns>A <see cref="DataRow"/> populated with data from the specified object and mapping logic.</returns>
        /// <exception cref="ArgumentException">Thrown if a string value exceeds the column size limit and truncation is not allowed.</exception>
        private DataRow CreateOutputDataRow(DataTable formData, T RowData, Func<string, T, object>? DataTableFunction)
        {
            var newRow = formData.NewRow();

            // if there is no data conversion function specified, auto generate
            if (DataTableFunction == null && TableColumns != null)
            {
                var underscoreProperties = RowData.ConvertPropertiesToUnderscoreNames();

                // autoresolve object properties here

                // run through each column of table
                foreach (var tableColumn in TableColumns)
                {
                    // check if AlternatePropertyName is a property on this object
                    var alternateUnderscoreName = tableColumn.Value?.Item3 == null ? null : Regex.Replace(tableColumn.Value.Item3, UnderscoreNamesHelper.UppercaseSearchPattern, UnderscoreNamesHelper.ReplacePattern);

                    var underscoreProperty = (KeyValuePair<string, Tuple<string, PropertyInfo>>?)null;

                    // if AlternatePropertyName is null or not on the object, convert column name to property name
                    // check to see if there is an alternate name converted to an underscore name with that key, if there isn't then check the underscoreName 
                    if (underscoreProperties.Any(x => x.Key.Equals(tableColumn.Key, StringComparison.InvariantCultureIgnoreCase) || x.Key.Equals(alternateUnderscoreName, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        underscoreProperty = underscoreProperties
                            .Where(x => x.Key.Equals(alternateUnderscoreName, StringComparison.InvariantCultureIgnoreCase))
                            .FirstOrDefault();

                        if (string.IsNullOrWhiteSpace(underscoreProperty.Value.Key))
                        {
                            underscoreProperty = underscoreProperties
                                .Where(x => x.Key.Equals(tableColumn.Key, StringComparison.InvariantCultureIgnoreCase))
                                .FirstOrDefault();
                        }
                    }

                    // if we found the underscore name then grab the value
                    if (underscoreProperty.HasValue)
                    {
                        var currentProperty = underscoreProperty.Value.Value.Item2;
                        var resolvedObject = currentProperty.GetValue(RowData, null);

                        // get value from property name, perform any type conversions as necessary
                        switch (tableColumn.Value?.Item1)
                        {
                            case MySqlDbType.Bit:
                            case MySqlDbType.Int16:
                            case MySqlDbType.Int24:
                            case MySqlDbType.Int32:
                            case MySqlDbType.Int64:
                                /*
                                if (currentProperty.PropertyType == typeof(bool) || currentProperty.PropertyType.GenericTypeArguments?.FirstOrDefault() == typeof(bool))
                                    resolvedObject = (bool)currentProperty.GetValue(RowData, null) ? 1 : 0;
                                */
                                if (currentProperty.PropertyType == typeof(bool) || currentProperty.PropertyType.GenericTypeArguments?.FirstOrDefault() == typeof(bool))
                                {
                                    var fieldValue = currentProperty.GetValue(RowData, null);
                                    resolvedObject = fieldValue is bool boolValue ? 1 : 0;
                                }
                                break;
                            case MySqlDbType.Timestamp:
                            case MySqlDbType.DateTime:
                                if (currentProperty.PropertyType == typeof(DateTime) || currentProperty.PropertyType.GenericTypeArguments?.FirstOrDefault() == typeof(DateTime))
                                    resolvedObject = ((DateTime?)currentProperty.GetValue(RowData, null))?.ToString("yyyy-MM-dd HH:mm:ss") ?? ((currentProperty.GetCustomAttribute<RelmColumn>()?.IsNullable ?? true) ? null : "0000-00-00 00:00:00");
                                break;
                            case MySqlDbType.Date:
                                if (currentProperty.PropertyType == typeof(DateTime) || currentProperty.PropertyType.GenericTypeArguments?.FirstOrDefault() == typeof(DateTime))
                                    resolvedObject = ((DateTime?)currentProperty.GetValue(RowData, null))?.ToString("yyyy-MM-dd") ?? ((currentProperty.GetCustomAttribute<RelmColumn>()?.IsNullable ?? true) ? null : "0000-00-00");
                                break;
                            case MySqlDbType.Time:
                                if (currentProperty.PropertyType == typeof(DateTime) || currentProperty.PropertyType.GenericTypeArguments?.FirstOrDefault() == typeof(DateTime))
                                    resolvedObject = ((DateTime?)currentProperty.GetValue(RowData, null))?.ToString("HH:mm:ss") ?? ((currentProperty.GetCustomAttribute<RelmColumn>()?.IsNullable ?? true) ? null : "00:00:00");
                                break;
                            case MySqlDbType.JSON:
                                resolvedObject = Newtonsoft.Json.JsonConvert.SerializeObject(resolvedObject);
                                break;
                            case MySqlDbType.VarChar:
                                if (currentProperty.PropertyType == typeof(Enum) || currentProperty.PropertyType.GenericTypeArguments?.FirstOrDefault() == typeof(Enum))
                                {
                                    var enumValue = currentProperty.GetValue(RowData, null);
                                    resolvedObject = enumValue?.ToString();
                                }
                                break;
                        }

                        if (resolvedObject?.GetType().GenericTypeArguments?.Contains(typeof(string)) ?? false)
                        {
                            var columnResolvable = currentProperty.GetCustomAttribute<RelmColumn>();
                            var columnSize = columnResolvable?.ColumnSize ?? tableColumn.Value?.Item2 ?? -1; // get DBModel specified, or get caller specified, or default to -1

                            if (columnSize != -1 && (resolvedObject?.ToString()?.Length ?? -1) > columnSize)
                            {
                                var shouldTruncate = columnResolvable?.AllowDataTruncation ?? false;

                                if (_throwException && !shouldTruncate)
                                    throw new ArgumentException($"String length [{resolvedObject?.ToString()?.Length}] is too long for column [{underscoreProperty?.Key}]. Expected [{columnSize}].");
                                else
                                    resolvedObject = resolvedObject?.ToString()?[..columnSize];
                            }
                        }

                        // assign the data to the row
                        newRow[tableColumn.Key] = resolvedObject;
                    }
                    else
                    {
                        // if can't find property, just return null
                        newRow[tableColumn.Key] = tableColumn.Value?.Item4;
                    }
                }
            }
            else if (TableColumns != null && DataTableFunction != null) 
            {
                // data conversion function is provided
                // go through each column and use the data conversion function to populate the row
                foreach (var column in TableColumns)
                {
                    newRow[column.Key] = DataTableFunction(column.Key, RowData);
                }
            }

            return newRow;
        }

        /// <summary>
        /// Sets the transaction to be used for the bulk table write operation.
        /// </summary>
        /// <param name="sqlTransaction">The <see cref="MySqlTransaction"/> instance to associate with the bulk table writer. This transaction will
        /// be used to execute the database operations.</param>
        /// <returns>The current <see cref="BulkTableWriter{T}"/> instance, allowing for method chaining.</returns>
        public BulkTableWriter<T> SetTransaction(MySqlTransaction sqlTransaction)
        {
            this._sqlTransaction = sqlTransaction;

            return this;
        }

        /// <summary>
        /// Configures whether the writer should throw an exception when an error occurs during bulk writing.
        /// </summary>
        /// <param name="throwException">A value indicating whether exceptions should be thrown.  <see langword="true"/> to throw exceptions on
        /// errors; otherwise, <see langword="false"/>.</param>
        /// <returns>The current instance of <see cref="BulkTableWriter{T}"/>, allowing for method chaining.</returns>
        public BulkTableWriter<T> ThrowException(bool throwException)
        {
            this._throwException = throwException;

            return this;
        }

        /// <summary>
        /// Enables or disables the use of user-defined variables in the bulk table writer.
        /// </summary>
        /// <param name="allowUserVariables">A value indicating whether user-defined variables are allowed.  <see langword="true"/> to allow user-defined
        /// variables; otherwise, <see langword="false"/>.</param>
        /// <returns>The current instance of <see cref="BulkTableWriter{T}"/>, allowing for method chaining.</returns>
        public BulkTableWriter<T> AllowUserVariables(bool allowUserVariables)
        {
            this._allowUserVariables = allowUserVariables;

            return this;
        }

        /// <summary>
        /// Enables or disables the automatic inclusion of date columns in the bulk table writer.
        /// </summary>
        /// <param name="allowAutoDateColumns">A value indicating whether automatic date columns should be included.  <see langword="true"/> to enable
        /// automatic date columns; otherwise, <see langword="false"/>.</param>
        /// <returns>The current instance of <see cref="BulkTableWriter{T}"/>, allowing for method chaining.</returns>
        public BulkTableWriter<T> AllowAutoDateColumns(bool allowAutoDateColumns)
        {
            this._allowAutoDateColumns = allowAutoDateColumns;

            return this;
        }

        /// <summary>
        /// Configures whether auto-increment columns are allowed during bulk table writing.
        /// </summary>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns should be included.  Specify <see langword="true"/> to
        /// allow auto-increment columns; otherwise, <see langword="false"/>.</param>
        /// <returns>The current <see cref="BulkTableWriter{T}"/> instance, enabling method chaining.</returns>
        public BulkTableWriter<T> AllowAutoIncrementColumns(bool allowAutoIncrementColumns)
        {
            this._allowAutoIncrementColumns = allowAutoIncrementColumns;

            return this;
        }

        /// <summary>
        /// Configures whether primary key columns are allowed to be included in bulk write operations.
        /// </summary>
        /// <remarks>By default, primary key columns are typically excluded from bulk write operations to
        /// prevent  unintended modifications to primary keys. Use this method to explicitly allow their inclusion  if
        /// required.</remarks>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns should be included.  <see langword="true"/> to allow primary
        /// key columns; otherwise, <see langword="false"/>.</param>
        /// <returns>The current <see cref="BulkTableWriter{T}"/> instance, enabling method chaining.</returns>
        public BulkTableWriter<T> AllowPrimaryKeyColumns(bool allowPrimaryKeyColumns)
        {
            this._allowPrimaryKeyColumns = allowPrimaryKeyColumns;

            return this;
        }

        /// <summary>
        /// Configures whether unique columns are allowed during bulk table writing.
        /// </summary>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns should be allowed.  <see langword="true"/> to allow unique
        /// columns; otherwise, <see langword="false"/>.</param>
        /// <returns>The current <see cref="BulkTableWriter{T}"/> instance, enabling method chaining.</returns>
        public BulkTableWriter<T> AllowUniqueColumns(bool allowUniqueColumns)
        {
            this._allowUniqueColumns = allowUniqueColumns;

            return this;
        }

        /// <summary>
        /// Sets the SQL insert query to be used for bulk data operations.
        /// </summary>
        /// <param name="insertQuery">The SQL insert query string to execute during bulk operations.  This query should be properly formatted and
        /// compatible with the target database.</param>
        /// <returns>The current instance of <see cref="BulkTableWriter{T}"/>, allowing for method chaining.</returns>
        public BulkTableWriter<T> SetInsertQuery(string insertQuery)
        {
            this._insertQuery = insertQuery;

            return this;
        }

        /// <summary>
        /// Sets the source data to be written in bulk operations.
        /// </summary>
        /// <param name="sourceData">The collection of data items to be used as the source for bulk writing. Cannot be null.</param>
        /// <returns>The current instance of <see cref="BulkTableWriter{T}"/> to allow method chaining.</returns>
        public BulkTableWriter<T> SetSourceData(IEnumerable<T> sourceData)
        {
            this._sourceData = sourceData;

            return this;
        }

        /// <summary>
        /// Sets the source data for the bulk table writer.
        /// </summary>
        /// <param name="sourceData">The source data to be written, represented as an instance of type <typeparamref name="T"/>.</param>
        /// <returns>The current instance of <see cref="BulkTableWriter{T}"/>, allowing for method chaining.</returns>
        public BulkTableWriter<T> SetSourceData(T sourceData)
        {
            this._sourceData = [sourceData];

            return this;
        }

        /// <summary>
        /// Adds a column definition to the bulk table writer.
        /// </summary>
        /// <param name="columnName">The name of the column to add. This value cannot be null or empty.</param>
        /// <param name="dbType">The MySQL data type of the column.</param>
        /// <param name="size">The maximum size of the column, in bytes. Must be a non-negative value.</param>
        /// <param name="alternatePropertyName">An optional alternate property name to map to this column. If null, the column will use the default property
        /// mapping.</param>
        /// <param name="columnDefault">An optional default value for the column. If null, no default value will be applied.</param>
        /// <returns>The current <see cref="BulkTableWriter{T}"/> instance, allowing for method chaining.</returns>
        public BulkTableWriter<T> AddColumn(string columnName, MySqlDbType dbType, int size, string? alternatePropertyName = null, string? columnDefault = null)
        {
            TableColumns?.Add(columnName, new Tuple<MySqlDbType, int, string?, string?>(dbType, size, alternatePropertyName, columnDefault));

            return this;
        }

        /// <summary>
        /// Adds the specified columns to the current table schema for bulk writing operations.
        /// </summary>
        /// <remarks>Columns with a null data type (<see cref="MySqlDbType"/>) are ignored. If the
        /// <paramref name="columns"/> parameter is null, no columns are added.</remarks>
        /// <param name="columns">A collection of tuples representing the columns to add. Each tuple contains the following elements: <list
        /// type="bullet"> <item><description>The column name (<see cref="string"/>).</description></item>
        /// <item><description>The MySQL data type of the column (<see cref="MySqlDbType"/>).</description></item>
        /// <item><description>The column size (<see cref="int"/>).</description></item> <item><description>The column
        /// collation (<see cref="string"/>).</description></item> <item><description>The column default value (<see
        /// cref="string"/>).</description></item> </list></param>
        /// <returns>The current instance of <see cref="BulkTableWriter{T}"/>, allowing for method chaining.</returns>
        public BulkTableWriter<T> AddColumns(IEnumerable<Tuple<string, MySqlDbType, int, string?, string?>?>? columns)
        {
            _tableColumns = TableColumns
                ?.Concat(columns?
                    .Where(x => x?.Item2 != null)
                    .ToDictionary(x => x!.Item1, x => new Tuple<MySqlDbType, int, string?, string?>(x!.Item2, x.Item3, x.Item4, x.Item5))
                    ??
                    [])
                .ToDictionary(x => x.Key, x => x.Value);

            return this;
        }

        /// <summary>
        /// Adds a collection of columns to the bulk table writer.
        /// </summary>
        /// <remarks>This method allows you to define columns for the bulk table writer by providing their
        /// names, data types, and sizes. The additional metadata for each column, such as default values or
        /// constraints, is not specified in this overload.</remarks>
        /// <param name="columns">A collection of tuples, where each tuple specifies the column name, the MySQL data type, and the column
        /// size.</param>
        /// <returns>The current instance of <see cref="BulkTableWriter{T}"/> with the specified columns added.</returns>
        public BulkTableWriter<T> AddColumns(IEnumerable<Tuple<string, MySqlDbType, int>> columns)
        {
            return AddColumns(columns.Select(x => new Tuple<string, MySqlDbType, int, string?, string?>(x.Item1, x.Item2, x.Item3, null, null)));
        }

        /// <summary>
        /// Sets the name of the database table to which data will be written.
        /// </summary>
        /// <param name="tableName">The name of the table. Cannot be null or empty.</param>
        /// <returns>The current <see cref="BulkTableWriter{T}"/> instance, allowing for method chaining.</returns>
        public BulkTableWriter<T> SetTableName(string tableName)
        {
            this._tableName = tableName;

            return this;
        }

        /// <summary>
        /// Sets the name of the database to be used for bulk table operations.
        /// </summary>
        /// <param name="databaseName">The name of the database. Cannot be null or empty.</param>
        /// <returns>The current instance of <see cref="BulkTableWriter{T}"/> to allow method chaining.</returns>
        public BulkTableWriter<T> SetDatabaseName(string databaseName)
        {
            this._databaseName = databaseName;

            return this;
        }

        /// <summary>
        /// Removes the specified column from the table writer.
        /// </summary>
        /// <remarks>If the specified column does not exist in the table, the method performs no
        /// action.</remarks>
        /// <param name="columnName">The name of the column to remove. This parameter is case-sensitive.</param>
        /// <returns>The current <see cref="BulkTableWriter{T}"/> instance, allowing for method chaining.</returns>
        public BulkTableWriter<T> RemoveColumn(string columnName)
        {
            if (TableColumns?.ContainsKey(columnName) ?? false)
                TableColumns.Remove(columnName);

            return this;
        }

        /// <summary>
        /// Sets the batch size for bulk write operations.
        /// </summary>
        /// <remarks>The batch size determines how many items are processed together during a bulk write
        /// operation.  Setting an appropriate batch size can help optimize performance based on the workload and system
        /// resources.</remarks>
        /// <param name="batchSize">The number of items to include in each batch. Must be a positive integer.</param>
        /// <returns>The current <see cref="BulkTableWriter{T}"/> instance, allowing for method chaining.</returns>
        public BulkTableWriter<T> SetBatchSize(int batchSize)
        {
            this._batchSize = batchSize;

            return this;
        }

        /// <summary>
        /// Resets the batch size to its default value.
        /// </summary>
        /// <remarks>This method sets the batch size to the default value defined by the implementation.
        /// It can be used to revert any custom batch size previously set.</remarks>
        /// <returns>The current <see cref="BulkTableWriter{T}"/> instance with the batch size reset.</returns>
        public BulkTableWriter<T> ResetBatchSize()
        {
            this._batchSize = DEFAULT_BATCH_SIZE;

            return this;
        }
    }
}
