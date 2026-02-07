using MySql.Data.MySqlClient;
using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.Persistence;
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

namespace CoreRelm.RelmInternal.Helpers.DataTransfer.Persistence
{
    internal class DataOutputOperations
    {
        /// <summary>
        /// Creates and returns a new instance of <see cref="BulkTableWriter{T}"/> for performing bulk write operations
        /// on a database table.
        /// </summary>
        /// <remarks>This method is intended for scenarios where bulk data insertion is required, such as
        /// importing large datasets. The behavior of the bulk write operation can be customized using the provided
        /// parameters.</remarks>
        /// <typeparam name="T">The type representing the table schema or entity to be written to the database.</typeparam>
        /// <param name="connectionName">The name of the database connection, represented as an <see cref="Enum"/>.</param>
        /// <param name="insertQuery">An optional custom SQL insert query to use for the bulk write operation. If null, a default query is
        /// generated.</param>
        /// <param name="useTransaction">Indicates whether the bulk write operation should be performed within a transaction. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="throwException">Indicates whether exceptions should be thrown if an error occurs during the operation. Defaults to <see
        /// langword="true"/>.</param>
        /// <param name="allowUserVariables">Specifies whether user-defined variables are allowed in the bulk write operation. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowAutoIncrementColumns">Specifies whether auto-increment columns are allowed in the bulk write operation. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">Specifies whether primary key columns are allowed in the bulk write operation. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowUniqueColumns">Specifies whether unique columns are allowed in the bulk write operation. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">Specifies whether auto-generated date columns are allowed in the bulk write operation. Defaults to <see
        /// langword="false"/>.</param>
        /// <returns>A new instance of <see cref="BulkTableWriter{T}"/> configured with the specified options.</returns>
        internal static BulkTableWriter<T> GetBulkTableWriter<T>(Enum connectionName, string? insertQuery = null, bool throwException = true, bool allowUserVariables = false, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        {
            return new BulkTableWriter<T>(connectionName, insertQuery: insertQuery, throwException: throwException, allowUserVariables: allowUserVariables, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
        }

        /// <summary>
        /// Creates and returns a <see cref="BulkTableWriter{T}"/> instance for performing bulk insert operations on a
        /// database table.
        /// </summary>
        /// <typeparam name="T">The type of the entity to be written to the database. Each instance of <typeparamref name="T"/> represents a
        /// row in the target table.</typeparam>
        /// <param name="existingConnection">An existing <see cref="MySqlConnection"/> to be used for the database operations. The connection must be
        /// open before calling this method.</param>
        /// <param name="insertQuery">An optional custom SQL insert query to use for the bulk operation. If not provided, a default query will be
        /// generated based on the entity type <typeparamref name="T"/>.</param>
        /// <param name="throwException">A value indicating whether exceptions should be thrown if an error occurs during the bulk operation. If <see
        /// langword="true"/>, exceptions will be thrown; otherwise, errors will be suppressed.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns in the target table are allowed to be explicitly set
        /// during the bulk operation. If <see langword="true"/>, auto-increment columns can be set; otherwise, they
        /// will be ignored.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns in the target table are allowed to be explicitly set during
        /// the bulk operation. If <see langword="true"/>, primary key columns can be set; otherwise, they will be
        /// ignored.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns in the target table are allowed to be explicitly set during the
        /// bulk operation. If <see langword="true"/>, unique columns can be set; otherwise, they will be ignored.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether auto-generated date columns in the target table are allowed to be explicitly set
        /// during the bulk operation. If <see langword="true"/>, auto-date columns can be set; otherwise, they will be
        /// ignored.</param>
        /// <returns>A <see cref="BulkTableWriter{T}"/> instance configured for performing bulk insert operations on the target
        /// table.</returns>
        internal static BulkTableWriter<T> GetBulkTableWriter<T>(MySqlConnection existingConnection, string? insertQuery = null, bool throwException = true, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        {
            return GetBulkTableWriter<T>(new RelmContext(existingConnection), insertQuery: insertQuery, throwException: throwException, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
        }

        /// <summary>
        /// Creates and returns a <see cref="BulkTableWriter{T}"/> instance for performing bulk insert operations on a
        /// database table.
        /// </summary>
        /// <typeparam name="T">The type of the entity to be written to the database. Each instance of <typeparamref name="T"/> represents a
        /// row in the target table.</typeparam>
        /// <param name="existingConnection">An existing <see cref="MySqlConnection"/> to be used for the database operations. The connection must be
        /// open before calling this method.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to be used for the bulk operation. If provided, this transaction
        /// will be used instead of creating a new one.</param>
        /// <param name="insertQuery">An optional custom SQL insert query to use for the bulk operation. If not provided, a default query will be
        /// generated based on the entity type <typeparamref name="T"/>.</param>
        /// <param name="throwException">A value indicating whether exceptions should be thrown if an error occurs during the bulk operation. If <see
        /// langword="true"/>, exceptions will be thrown; otherwise, errors will be suppressed.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns in the target table are allowed to be explicitly set
        /// during the bulk operation. If <see langword="true"/>, auto-increment columns can be set; otherwise, they
        /// will be ignored.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns in the target table are allowed to be explicitly set during
        /// the bulk operation. If <see langword="true"/>, primary key columns can be set; otherwise, they will be
        /// ignored.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns in the target table are allowed to be explicitly set during the
        /// bulk operation. If <see langword="true"/>, unique columns can be set; otherwise, they will be ignored.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether auto-generated date columns in the target table are allowed to be explicitly set
        /// during the bulk operation. If <see langword="true"/>, auto-date columns can be set; otherwise, they will be
        /// ignored.</param>
        /// <returns>A <see cref="BulkTableWriter{T}"/> instance configured for performing bulk insert operations on the target
        /// table.</returns>
        internal static BulkTableWriter<T> GetBulkTableWriter<T>(MySqlConnection existingConnection, MySqlTransaction sqlTransaction, string? insertQuery = null, bool throwException = true, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        {
            return GetBulkTableWriter<T>(new RelmContext(existingConnection, sqlTransaction), insertQuery: insertQuery, throwException: throwException, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
        }

        /// <summary>
        /// Creates and returns a new instance of <see cref="BulkTableWriter{T}"/> for performing bulk write operations
        /// on a database table.
        /// </summary>
        /// <typeparam name="T">The type of the entity to be written to the database table.</typeparam>
        /// <param name="relmContext">The database context used to manage the connection and configuration for the bulk write operation. Cannot be
        /// <c>null</c>.</param>
        /// <param name="insertQuery">An optional custom SQL insert query to use for the bulk write operation. If <c>null</c>, a default query
        /// will be generated.</param>
        /// <param name="throwException">Indicates whether exceptions should be thrown during the bulk write operation. If <c>true</c>, exceptions
        /// will be thrown; otherwise, errors will be suppressed.</param>
        /// <param name="allowAutoIncrementColumns">Specifies whether auto-increment columns are allowed to be included in the bulk write operation. If
        /// <c>false</c>, such columns will be excluded.</param>
        /// <param name="allowPrimaryKeyColumns">Specifies whether primary key columns are allowed to be included in the bulk write operation. If
        /// <c>false</c>, such columns will be excluded.</param>
        /// <param name="allowUniqueColumns">Specifies whether unique columns are allowed to be included in the bulk write operation. If <c>false</c>,
        /// such columns will be excluded.</param>
        /// <param name="allowAutoDateColumns">Specifies whether auto-generated date columns are allowed to be included in the bulk write operation. If
        /// <c>false</c>, such columns will be excluded.</param>
        /// <returns>A new instance of <see cref="BulkTableWriter{T}"/> configured with the specified options for performing bulk
        /// write operations.</returns>
        internal static BulkTableWriter<T> GetBulkTableWriter<T>(IRelmContext relmContext, string? insertQuery = null, bool throwException = true, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        {
            return new BulkTableWriter<T>(relmContext, insertQuery: insertQuery, throwException: throwException, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
        }

        /// <summary>
        /// Performs a bulk write operation to a database table using the specified connection and source data.
        /// </summary>
        /// <remarks>This method provides a convenient way to perform bulk insert operations into a
        /// database table. It supports various configuration options, such as allowing or disallowing writes to
        /// specific types of columns (e.g., auto-increment, primary key, unique, or auto-generated date columns).  The
        /// method uses the specified connection, inferred or provided table name, and optional type enforcement to map
        /// the source data to the target table. If the table name is not provided, it is inferred from the type
        /// <typeparamref name="T"/>.  The operation is performed in batches, with the batch size specified by <paramref
        /// name="batchSize"/>. This can help optimize performance for large datasets.</remarks>
        /// <typeparam name="T">The type of the source data to be written to the table. This can be a collection of objects or a single
        /// object.</typeparam>
        /// <param name="connectionName">The name of the connection to use, represented as an <see cref="Enum"/>. This determines which database
        /// connection is used for the operation.</param>
        /// <param name="sourceData">The data to be written to the table. This can be a collection of objects or a single object. Cannot be <see
        /// langword="null"/>.</param>
        /// <param name="tableName">The name of the target database table. If <see langword="null"/>, the table name is inferred from the type
        /// <typeparamref name="T"/>.</param>
        /// <param name="forceType">An optional <see cref="Type"/> to enforce when mapping the source data to the table. If <see
        /// langword="null"/>, the type of <typeparamref name="T"/> is used.</param>
        /// <param name="allowUserVariables">Indicates whether user-defined variables are allowed in the database connection. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="batchSize">The number of rows to write in each batch. Defaults to 100. Must be greater than 0.</param>
        /// <param name="databaseName">The name of the database where the table resides. If <see langword="null"/>, the default database for the
        /// connection is used.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns in the target table are allowed to be written to. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns in the target table are allowed to be written to. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns in the target table are allowed to be written to. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">Indicates whether auto-generated date columns in the target table are allowed to be written to. Defaults to
        /// <see langword="false"/>.</param>
        /// <returns>The number of rows successfully written to the table.</returns>
        internal static int BulkTableWrite<T>(Enum connectionName, T sourceData, string? tableName = null, Type? forceType = null, bool allowUserVariables = false, int batchSize = 100, string? databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        {
            using var conn = (RelmHelper.ConnectionHelper?.GetConnectionFromType(connectionName, allowUserVariables: allowUserVariables))
                ?? throw new InvalidOperationException($"Could not get a valid connection for connection type '{connectionName}'.");

            return BulkTableWrite<T>(conn, sourceData, tableName, forceType, batchSize, databaseName, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
        }

        /// <summary>
        /// Performs a bulk write operation to a database table using the specified connection and source data.
        /// </summary>
        /// <remarks>This method provides a convenient way to perform bulk insert operations into a
        /// database table. It supports various configuration options, such as allowing or disallowing writes to
        /// specific types of columns (e.g., auto-increment, primary key, unique, or auto-generated date columns).  The
        /// method uses the specified connection, inferred or provided table name, and optional type enforcement to map
        /// the source data to the target table. If the table name is not provided, it is inferred from the type
        /// <typeparamref name="T"/>.  The operation is performed in batches, with the batch size specified by <paramref
        /// name="batchSize"/>. This can help optimize performance for large datasets.</remarks>
        /// <typeparam name="T">The type of the source data to be written to the table. This can be a collection of objects or a single
        /// object.</typeparam>
        /// <param name="connectionName">The name of the connection to use, represented as an <see cref="Enum"/>. This determines which database
        /// connection is used for the operation.</param>
        /// <param name="sourceData">The data to be written to the table. This can be a collection of objects or a single object. Cannot be <see
        /// langword="null"/>.</param>
        /// <param name="tableName">The name of the target database table. If <see langword="null"/>, the table name is inferred from the type
        /// <typeparamref name="T"/>.</param>
        /// <param name="forceType">An optional <see cref="Type"/> to enforce when mapping the source data to the table. If <see
        /// langword="null"/>, the type of <typeparamref name="T"/> is used.</param>
        /// <param name="allowUserVariables">Indicates whether user-defined variables are allowed in the database connection. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="batchSize">The number of rows to write in each batch. Defaults to 100. Must be greater than 0.</param>
        /// <param name="databaseName">The name of the database where the table resides. If <see langword="null"/>, the default database for the
        /// connection is used.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns in the target table are allowed to be written to. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns in the target table are allowed to be written to. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns in the target table are allowed to be written to. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">Indicates whether auto-generated date columns in the target table are allowed to be written to. Defaults to
        /// <see langword="false"/>.</param>
        /// <returns>The number of rows successfully written to the table.</returns>
        internal static async Task<int> BulkTableWriteAsync<T>(Enum connectionName, T sourceData, string? tableName = null, Type? forceType = null, bool allowUserVariables = false, int batchSize = 100, string? databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false, CancellationToken cancellationToken = default)
        {
            using var conn = (RelmHelper.ConnectionHelper?.GetConnectionFromType(connectionName, allowUserVariables: allowUserVariables))
                ?? throw new InvalidOperationException($"Could not get a valid connection for connection type '{connectionName}'.");

            return await BulkTableWriteAsync<T>(conn, sourceData, tableName, forceType, batchSize, databaseName, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Performs a bulk write operation to a database table using the specified connection and source data.
        /// </summary>
        /// <typeparam name="T">The type of the objects in the <paramref name="sourceData"/> collection.</typeparam>
        /// <param name="connectionName">The name of the connection to use, represented as an <see cref="Enum"/>.</param>
        /// <param name="sourceData">The collection of data to be written to the database table. Cannot be null or empty.</param>
        /// <param name="tableName">The name of the target database table. If null, the table name is inferred from the type <typeparamref
        /// name="T"/>.</param>
        /// <param name="forceType">An optional type to enforce for the database table schema. If null, the schema is inferred from
        /// <typeparamref name="T"/>.</param>
        /// <param name="allowUserVariables">Indicates whether user-defined variables are allowed in the database connection. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="batchSize">The number of rows to write in each batch. Must be greater than 0. Defaults to 100.</param>
        /// <param name="databaseName">The name of the database to use. If null, the default database for the connection is used.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns are included in the write operation. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns are included in the write operation. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns are included in the write operation. Defaults to <see langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">Indicates whether auto-generated date columns are included in the write operation. Defaults to <see
        /// langword="false"/>.</param>
        /// <returns>The total number of rows successfully written to the database table.</returns>
        internal static int BulkTableWrite<T>(Enum connectionName, IEnumerable<T> sourceData, string? tableName = null, Type? forceType = null, bool allowUserVariables = false, int batchSize = 100, string? databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        {
            using var conn = (RelmHelper.ConnectionHelper?.GetConnectionFromType(connectionName, allowUserVariables: allowUserVariables))
                ?? throw new InvalidOperationException($"Could not get a valid connection for connection type '{connectionName}'.");

            return BulkTableWrite<T>(conn, sourceData, tableName, forceType, batchSize, databaseName, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
        }

        /// <summary>
        /// Performs a bulk write operation to a database table using the specified connection and source data.
        /// </summary>
        /// <typeparam name="T">The type of the objects in the <paramref name="sourceData"/> collection.</typeparam>
        /// <param name="connectionName">The name of the connection to use, represented as an <see cref="Enum"/>.</param>
        /// <param name="sourceData">The collection of data to be written to the database table. Cannot be null or empty.</param>
        /// <param name="tableName">The name of the target database table. If null, the table name is inferred from the type <typeparamref
        /// name="T"/>.</param>
        /// <param name="forceType">An optional type to enforce for the database table schema. If null, the schema is inferred from
        /// <typeparamref name="T"/>.</param>
        /// <param name="allowUserVariables">Indicates whether user-defined variables are allowed in the database connection. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="batchSize">The number of rows to write in each batch. Must be greater than 0. Defaults to 100.</param>
        /// <param name="databaseName">The name of the database to use. If null, the default database for the connection is used.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns are included in the write operation. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns are included in the write operation. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns are included in the write operation. Defaults to <see langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">Indicates whether auto-generated date columns are included in the write operation. Defaults to <see
        /// langword="false"/>.</param>
        /// <returns>The total number of rows successfully written to the database table.</returns>
        internal static async Task<int> BulkTableWriteAsync<T>(Enum connectionName, IEnumerable<T> sourceData, string? tableName = null, Type? forceType = null, bool allowUserVariables = false, int batchSize = 100, string? databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false, CancellationToken cancellationToken = default)
        {
            using var conn = (RelmHelper.ConnectionHelper?.GetConnectionFromType(connectionName, allowUserVariables: allowUserVariables))
                ?? throw new InvalidOperationException($"Could not get a valid connection for connection type '{connectionName}'.");

            return await BulkTableWriteAsync<T>(conn, sourceData, tableName, forceType, batchSize, databaseName, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Performs a bulk write operation to a database table using the specified connection and source data.
        /// </summary>
        /// <remarks>This method provides a convenient way to perform bulk insert operations into a
        /// database table. It supports optional configurations such as batch size, table name inference, and column
        /// write permissions.  The caller is responsible for ensuring that the provided <paramref
        /// name="existingConnection"/> is open before invoking this method. If a transaction is provided via <paramref
        /// name="sqlTransaction"/>, the operation will be executed within the context of that transaction.  Use the
        /// optional parameters to customize the behavior of the bulk write operation, such as allowing specific column
        /// types to be written or specifying a target database.</remarks>
        /// <typeparam name="T">The type of the source data to be written to the table.</typeparam>
        /// <param name="existingConnection">An existing <see cref="MySqlConnection"/> to use for the operation. The connection must be open.</param>
        /// <param name="sourceData">The data to be written to the table. This can be a collection or a single object, depending on the
        /// implementation.</param>
        /// <param name="tableName">The name of the target database table. If null, the table name is inferred from the type <typeparamref
        /// name="T"/>.</param>
        /// <param name="forceType">An optional <see cref="Type"/> to explicitly specify the type of the source data. If null, the type is
        /// inferred from <typeparamref name="T"/>.</param>
        /// <param name="batchSize">The maximum number of rows to include in each batch during the bulk write operation. Must be greater than
        /// zero. Default is 100.</param>
        /// <param name="databaseName">The name of the target database. If null, the default database associated with the connection is used.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns in the target table are allowed to be written. Default is <see
        /// langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns in the target table are allowed to be written. Default is <see
        /// langword="false"/>.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns in the target table are allowed to be written. Default is <see
        /// langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">Indicates whether auto-generated date columns in the target table are allowed to be written. Default is <see
        /// langword="false"/>.</param>
        /// <returns>The number of rows successfully written to the target table.</returns>
        internal static int BulkTableWrite<T>(MySqlConnection existingConnection, T sourceData, string? tableName = null, Type? forceType = null, int batchSize = 100, string? databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        {
            return BulkTableWrite<T>(new RelmContext(existingConnection), sourceData, tableName: tableName, forceType: forceType, batchSize: batchSize, databaseName: databaseName, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
        }

        /// <summary>
        /// Performs a bulk write operation to a database table using the specified connection and source data.
        /// </summary>
        /// <remarks>This method provides a convenient way to perform bulk insert operations into a
        /// database table. It supports optional configurations such as batch size, table name inference, and column
        /// write permissions.  The caller is responsible for ensuring that the provided <paramref
        /// name="existingConnection"/> is open before invoking this method. If a transaction is provided via <paramref
        /// name="sqlTransaction"/>, the operation will be executed within the context of that transaction.  Use the
        /// optional parameters to customize the behavior of the bulk write operation, such as allowing specific column
        /// types to be written or specifying a target database.</remarks>
        /// <typeparam name="T">The type of the source data to be written to the table.</typeparam>
        /// <param name="existingConnection">An existing <see cref="MySqlConnection"/> to use for the operation. The connection must be open.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the operation. If null, no transaction is used.</param>
        /// <param name="sourceData">The data to be written to the table. This can be a collection or a single object, depending on the
        /// implementation.</param>
        /// <param name="tableName">The name of the target database table. If null, the table name is inferred from the type <typeparamref
        /// name="T"/>.</param>
        /// <param name="forceType">An optional <see cref="Type"/> to explicitly specify the type of the source data. If null, the type is
        /// inferred from <typeparamref name="T"/>.</param>
        /// <param name="batchSize">The maximum number of rows to include in each batch during the bulk write operation. Must be greater than
        /// zero. Default is 100.</param>
        /// <param name="databaseName">The name of the target database. If null, the default database associated with the connection is used.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns in the target table are allowed to be written. Default is <see
        /// langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns in the target table are allowed to be written. Default is <see
        /// langword="false"/>.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns in the target table are allowed to be written. Default is <see
        /// langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">Indicates whether auto-generated date columns in the target table are allowed to be written. Default is <see
        /// langword="false"/>.</param>
        /// <returns>The number of rows successfully written to the target table.</returns>
        internal static int BulkTableWrite<T>(MySqlConnection existingConnection, MySqlTransaction sqlTransaction, T sourceData, string? tableName = null, Type? forceType = null, int batchSize = 100, string? databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        {
            return BulkTableWrite<T>(new RelmContext(existingConnection, sqlTransaction), sourceData, tableName: tableName, forceType: forceType, batchSize: batchSize, databaseName: databaseName, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
        }

        /// <summary>
        /// Performs a bulk write operation to a database table using the specified connection and source data.
        /// </summary>
        /// <remarks>This method provides a convenient way to perform bulk insert operations into a
        /// database table. It supports optional configurations such as batch size, table name inference, and column
        /// write permissions.  The caller is responsible for ensuring that the provided <paramref
        /// name="existingConnection"/> is open before invoking this method. If a transaction is provided via <paramref
        /// name="sqlTransaction"/>, the operation will be executed within the context of that transaction.  Use the
        /// optional parameters to customize the behavior of the bulk write operation, such as allowing specific column
        /// types to be written or specifying a target database.</remarks>
        /// <typeparam name="T">The type of the source data to be written to the table.</typeparam>
        /// <param name="existingConnection">An existing <see cref="MySqlConnection"/> to use for the operation. The connection must be open.</param>
        /// <param name="sourceData">The data to be written to the table. This can be a collection or a single object, depending on the
        /// implementation.</param>
        /// <param name="tableName">The name of the target database table. If null, the table name is inferred from the type <typeparamref
        /// name="T"/>.</param>
        /// <param name="forceType">An optional <see cref="Type"/> to explicitly specify the type of the source data. If null, the type is
        /// inferred from <typeparamref name="T"/>.</param>
        /// <param name="batchSize">The maximum number of rows to include in each batch during the bulk write operation. Must be greater than
        /// zero. Default is 100.</param>
        /// <param name="databaseName">The name of the target database. If null, the default database associated with the connection is used.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns in the target table are allowed to be written. Default is <see
        /// langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns in the target table are allowed to be written. Default is <see
        /// langword="false"/>.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns in the target table are allowed to be written. Default is <see
        /// langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">Indicates whether auto-generated date columns in the target table are allowed to be written. Default is <see
        /// langword="false"/>.</param>
        /// <returns>The number of rows successfully written to the target table.</returns>
        internal static async Task<int> BulkTableWriteAsync<T>(MySqlConnection existingConnection, T sourceData, string? tableName = null, Type? forceType = null, int batchSize = 100, string? databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false, CancellationToken cancellationToken = default)
        {
            return await BulkTableWriteAsync<T>(new RelmContext(existingConnection), sourceData, tableName: tableName, forceType: forceType, batchSize: batchSize, databaseName: databaseName, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Performs a bulk write operation to a database table using the specified connection and source data.
        /// </summary>
        /// <remarks>This method provides a convenient way to perform bulk insert operations into a
        /// database table. It supports optional configurations such as batch size, table name inference, and column
        /// write permissions.  The caller is responsible for ensuring that the provided <paramref
        /// name="existingConnection"/> is open before invoking this method. If a transaction is provided via <paramref
        /// name="sqlTransaction"/>, the operation will be executed within the context of that transaction.  Use the
        /// optional parameters to customize the behavior of the bulk write operation, such as allowing specific column
        /// types to be written or specifying a target database.</remarks>
        /// <typeparam name="T">The type of the source data to be written to the table.</typeparam>
        /// <param name="existingConnection">An existing <see cref="MySqlConnection"/> to use for the operation. The connection must be open.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the operation. If null, no transaction is used.</param>
        /// <param name="sourceData">The data to be written to the table. This can be a collection or a single object, depending on the
        /// implementation.</param>
        /// <param name="tableName">The name of the target database table. If null, the table name is inferred from the type <typeparamref
        /// name="T"/>.</param>
        /// <param name="forceType">An optional <see cref="Type"/> to explicitly specify the type of the source data. If null, the type is
        /// inferred from <typeparamref name="T"/>.</param>
        /// <param name="batchSize">The maximum number of rows to include in each batch during the bulk write operation. Must be greater than
        /// zero. Default is 100.</param>
        /// <param name="databaseName">The name of the target database. If null, the default database associated with the connection is used.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns in the target table are allowed to be written. Default is <see
        /// langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns in the target table are allowed to be written. Default is <see
        /// langword="false"/>.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns in the target table are allowed to be written. Default is <see
        /// langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">Indicates whether auto-generated date columns in the target table are allowed to be written. Default is <see
        /// langword="false"/>.</param>
        /// <returns>The number of rows successfully written to the target table.</returns>
        internal static async Task<int> BulkTableWriteAsync<T>(MySqlConnection existingConnection, MySqlTransaction sqlTransaction, T sourceData, string? tableName = null, Type? forceType = null, int batchSize = 100, string? databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false, CancellationToken cancellationToken = default)
        {
            return await BulkTableWriteAsync<T>(new RelmContext(existingConnection, sqlTransaction), sourceData, tableName: tableName, forceType: forceType, batchSize: batchSize, databaseName: databaseName, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Performs a bulk write operation to a database table using the specified data source and configuration
        /// options.
        /// </summary>
        /// <remarks>This method provides a flexible way to perform bulk write operations with various
        /// configuration options. Use the optional parameters to customize the behavior of the operation, such as
        /// specifying the target table, enforcing a specific type, or allowing certain column types to be
        /// written.</remarks>
        /// <typeparam name="T">The type of the data source to be written to the table.</typeparam>
        /// <param name="relmContext">The database context used to manage the connection and transaction for the operation.</param>
        /// <param name="sourceData">The data to be written to the table. This can be a collection or a single entity of type <typeparamref
        /// name="T"/>.</param>
        /// <param name="tableName">The name of the target database table. If <see langword="null"/>, the table name is inferred from the type
        /// <typeparamref name="T"/>.</param>
        /// <param name="forceType">An optional type to enforce for the operation. If <see langword="null"/>, the type is inferred from
        /// <typeparamref name="T"/>.</param>
        /// <param name="batchSize">The number of records to process in each batch. Defaults to 100.</param>
        /// <param name="databaseName">The name of the target database. If <see langword="null"/>, the default database is used.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns are allowed to be written. Defaults to <see langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns are allowed to be written. Defaults to <see langword="false"/>.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns are allowed to be written. Defaults to <see langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">Indicates whether auto-generated date columns are allowed to be written. Defaults to <see
        /// langword="false"/>.</param>
        /// <returns>The number of rows successfully written to the database table.</returns>
        internal static int BulkTableWrite<T>(IRelmContext relmContext, T sourceData, string? tableName = null, Type? forceType = null, int batchSize = 100, string? databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        {
            return BulkTableWriteAsyncStatic<T>(GetBulkTableWriter<T>(relmContext), sourceData, tableName: tableName, forceType: forceType, batchSize: batchSize, databaseName: databaseName, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Performs a bulk write operation to a database table using the specified data source and configuration
        /// options.
        /// </summary>
        /// <remarks>This method provides a flexible way to perform bulk write operations with various
        /// configuration options. Use the optional parameters to customize the behavior of the operation, such as
        /// specifying the target table, enforcing a specific type, or allowing certain column types to be
        /// written.</remarks>
        /// <typeparam name="T">The type of the data source to be written to the table.</typeparam>
        /// <param name="relmContext">The database context used to manage the connection and transaction for the operation.</param>
        /// <param name="sourceData">The data to be written to the table. This can be a collection or a single entity of type <typeparamref
        /// name="T"/>.</param>
        /// <param name="tableName">The name of the target database table. If <see langword="null"/>, the table name is inferred from the type
        /// <typeparamref name="T"/>.</param>
        /// <param name="forceType">An optional type to enforce for the operation. If <see langword="null"/>, the type is inferred from
        /// <typeparamref name="T"/>.</param>
        /// <param name="batchSize">The number of records to process in each batch. Defaults to 100.</param>
        /// <param name="databaseName">The name of the target database. If <see langword="null"/>, the default database is used.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns are allowed to be written. Defaults to <see langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns are allowed to be written. Defaults to <see langword="false"/>.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns are allowed to be written. Defaults to <see langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">Indicates whether auto-generated date columns are allowed to be written. Defaults to <see
        /// langword="false"/>.</param>
        /// <returns>The number of rows successfully written to the database table.</returns>
        internal static async Task<int> BulkTableWriteAsync<T>(IRelmContext relmContext, T sourceData, string? tableName = null, Type? forceType = null, int batchSize = 100, string? databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false, CancellationToken cancellationToken = default)
        {
            return await BulkTableWriteAsyncStatic<T>(GetBulkTableWriter<T>(relmContext), sourceData, tableName: tableName, forceType: forceType, batchSize: batchSize, databaseName: databaseName, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Writes data to a database table in bulk using the specified <see cref="BulkTableWriter{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the data to be written to the table.</typeparam>
        /// <param name="tableWriter">The <see cref="BulkTableWriter{T}"/> instance used to perform the bulk write operation.</param>
        /// <param name="sourceData">The source data to be written to the table. This must match the structure of the target table.</param>
        /// <param name="tableName">The name of the target table. If not specified, the table name is inferred from the <see cref="RelmTable"/>
        /// attribute on the type <typeparamref name="T"/> or the <paramref name="forceType"/>.</param>
        /// <param name="forceType">An optional type used to override the type <typeparamref name="T"/> for attribute-based table and database
        /// name resolution.</param>
        /// <param name="batchSize">The number of rows to write in each batch. Defaults to 100.</param>
        /// <param name="databaseName">The name of the target database. If not specified, the database name is inferred from the <see
        /// cref="RelmDatabase"/> attribute on the type <typeparamref name="T"/> or the <paramref name="forceType"/>.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns are allowed to be written. Defaults to <see langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns are allowed to be written. Defaults to <see langword="false"/>.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns are allowed to be written. Defaults to <see langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">Indicates whether auto-generated date columns are allowed to be written. Defaults to <see
        /// langword="false"/>.</param>
        /// <returns>The number of rows successfully written to the table.</returns>
        /// <exception cref="CustomAttributeFormatException">Thrown if the table name cannot be resolved because the <see cref="RelmTable"/> attribute is missing on the
        /// type <typeparamref name="T"/> or the <paramref name="forceType"/>.</exception>
        private static async Task<int> BulkTableWriteAsyncStatic<T>(BulkTableWriter<T> tableWriter, T sourceData, string? tableName = null, Type? forceType = null, int batchSize = 100, string? databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false, CancellationToken cancellationToken = default)
        {
            var rowsUpdated = await tableWriter
                .SetTableName(tableName ?? (forceType ?? typeof(T)).GetCustomAttribute<RelmTable>()?.TableName ?? throw new CustomAttributeFormatException(CoreUtilities.NoDalTableAttributeError))
                .SetDatabaseName(databaseName ?? (forceType ?? typeof(T)).GetCustomAttribute<RelmDatabase>()?.DatabaseName)
                .SetSourceData(sourceData)
                .SetBatchSize(batchSize)
                .AllowAutoDateColumns(allowAutoDateColumns)
                .AllowAutoIncrementColumns(allowAutoIncrementColumns)
                .AllowPrimaryKeyColumns(allowPrimaryKeyColumns)
                .AllowUniqueColumns(allowUniqueColumns)
                .WriteAsync(cancellationToken: cancellationToken);

            return rowsUpdated;
        }

        /// <summary>
        /// Performs a bulk write operation to insert or update data in a database table.
        /// </summary>
        /// <remarks>This method performs a bulk write operation to efficiently insert or update multiple
        /// rows in a database table. It supports optional configuration for handling specific column types, such as
        /// auto-increment, primary key, unique, and auto-generated date columns. The operation can be performed within
        /// an existing transaction if <paramref name="sqlTransaction"/> is provided.</remarks>
        /// <typeparam name="T">The type of the objects in the <paramref name="sourceData"/> collection.</typeparam>
        /// <param name="existingConnection">An existing <see cref="MySqlConnection"/> to use for the operation. The connection must be open.</param>
        /// <param name="sourceData">The collection of data to be written to the database table. Each item represents a row to be inserted or
        /// updated.</param>
        /// <param name="tableName">The name of the target database table. If <c>null</c>, the table name is inferred from the type
        /// <typeparamref name="T"/>.</param>
        /// <param name="forceType">An optional <see cref="Type"/> to enforce a specific type mapping for the operation. If <c>null</c>, the
        /// type is inferred from <typeparamref name="T"/>.</param>
        /// <param name="batchSize">The maximum number of rows to include in each batch during the bulk write operation. Must be greater than
        /// zero. Default is 100.</param>
        /// <param name="databaseName">The name of the target database. If <c>null</c>, the default database associated with the connection is
        /// used.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns are included in the operation. If <see langword="true"/>,
        /// auto-increment columns are included; otherwise, they are excluded. Default is <see langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are included in the operation. If <see langword="true"/>,
        /// primary key columns are included; otherwise, they are excluded. Default is <see langword="false"/>.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns are included in the operation. If <see langword="true"/>, unique
        /// columns are included; otherwise, they are excluded. Default is <see langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether auto-generated date columns (e.g., timestamps) are included in the operation. If
        /// <see langword="true"/>, auto-generated date columns are included; otherwise, they are excluded. Default is
        /// <see langword="false"/>.</param>
        /// <returns>The total number of rows successfully written to the database.</returns>
        internal static int BulkTableWrite<T>(MySqlConnection existingConnection, IEnumerable<T> sourceData, string? tableName = null, Type? forceType = null, int batchSize = 100, string? databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        {
            return BulkTableWrite<T>(new RelmContext(existingConnection), sourceData, tableName: tableName, forceType: forceType, batchSize: batchSize, databaseName: databaseName, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
        }

        /// <summary>
        /// Performs a bulk write operation to insert or update data in a database table.
        /// </summary>
        /// <remarks>This method performs a bulk write operation to efficiently insert or update multiple
        /// rows in a database table. It supports optional configuration for handling specific column types, such as
        /// auto-increment, primary key, unique, and auto-generated date columns. The operation can be performed within
        /// an existing transaction if <paramref name="sqlTransaction"/> is provided.</remarks>
        /// <typeparam name="T">The type of the objects in the <paramref name="sourceData"/> collection.</typeparam>
        /// <param name="existingConnection">An existing <see cref="MySqlConnection"/> to use for the operation. The connection must be open.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the operation. If <c>null</c>, no transaction
        /// is used.</param>
        /// <param name="sourceData">The collection of data to be written to the database table. Each item represents a row to be inserted or
        /// updated.</param>
        /// <param name="tableName">The name of the target database table. If <c>null</c>, the table name is inferred from the type
        /// <typeparamref name="T"/>.</param>
        /// <param name="forceType">An optional <see cref="Type"/> to enforce a specific type mapping for the operation. If <c>null</c>, the
        /// type is inferred from <typeparamref name="T"/>.</param>
        /// <param name="batchSize">The maximum number of rows to include in each batch during the bulk write operation. Must be greater than
        /// zero. Default is 100.</param>
        /// <param name="databaseName">The name of the target database. If <c>null</c>, the default database associated with the connection is
        /// used.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns are included in the operation. If <see langword="true"/>,
        /// auto-increment columns are included; otherwise, they are excluded. Default is <see langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are included in the operation. If <see langword="true"/>,
        /// primary key columns are included; otherwise, they are excluded. Default is <see langword="false"/>.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns are included in the operation. If <see langword="true"/>, unique
        /// columns are included; otherwise, they are excluded. Default is <see langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether auto-generated date columns (e.g., timestamps) are included in the operation. If
        /// <see langword="true"/>, auto-generated date columns are included; otherwise, they are excluded. Default is
        /// <see langword="false"/>.</param>
        /// <returns>The total number of rows successfully written to the database.</returns>
        internal static int BulkTableWrite<T>(MySqlConnection existingConnection, MySqlTransaction sqlTransaction, IEnumerable<T> sourceData, string? tableName = null, Type? forceType = null, int batchSize = 100, string? databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        {
            return BulkTableWrite<T>(new RelmContext(existingConnection, sqlTransaction), sourceData, tableName: tableName, forceType: forceType, batchSize: batchSize, databaseName: databaseName, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
        }

        /// <summary>
        /// Performs a bulk write operation to insert or update data in a database table.
        /// </summary>
        /// <remarks>This method performs a bulk write operation to efficiently insert or update multiple
        /// rows in a database table. It supports optional configuration for handling specific column types, such as
        /// auto-increment, primary key, unique, and auto-generated date columns. The operation can be performed within
        /// an existing transaction if <paramref name="sqlTransaction"/> is provided.</remarks>
        /// <typeparam name="T">The type of the objects in the <paramref name="sourceData"/> collection.</typeparam>
        /// <param name="existingConnection">An existing <see cref="MySqlConnection"/> to use for the operation. The connection must be open.</param>
        /// <param name="sourceData">The collection of data to be written to the database table. Each item represents a row to be inserted or
        /// updated.</param>
        /// <param name="tableName">The name of the target database table. If <c>null</c>, the table name is inferred from the type
        /// <typeparamref name="T"/>.</param>
        /// <param name="forceType">An optional <see cref="Type"/> to enforce a specific type mapping for the operation. If <c>null</c>, the
        /// type is inferred from <typeparamref name="T"/>.</param>
        /// <param name="batchSize">The maximum number of rows to include in each batch during the bulk write operation. Must be greater than
        /// zero. Default is 100.</param>
        /// <param name="databaseName">The name of the target database. If <c>null</c>, the default database associated with the connection is
        /// used.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns are included in the operation. If <see langword="true"/>,
        /// auto-increment columns are included; otherwise, they are excluded. Default is <see langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are included in the operation. If <see langword="true"/>,
        /// primary key columns are included; otherwise, they are excluded. Default is <see langword="false"/>.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns are included in the operation. If <see langword="true"/>, unique
        /// columns are included; otherwise, they are excluded. Default is <see langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether auto-generated date columns (e.g., timestamps) are included in the operation. If
        /// <see langword="true"/>, auto-generated date columns are included; otherwise, they are excluded. Default is
        /// <see langword="false"/>.</param>
        /// <returns>The total number of rows successfully written to the database.</returns>
        internal static async Task<int> BulkTableWriteAsync<T>(MySqlConnection existingConnection, IEnumerable<T> sourceData, string? tableName = null, Type? forceType = null, int batchSize = 100, string? databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false, CancellationToken cancellationToken = default)
        {
            return await BulkTableWriteAsync<T>(new RelmContext(existingConnection), sourceData, tableName: tableName, forceType: forceType, batchSize: batchSize, databaseName: databaseName, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Performs a bulk write operation to insert or update data in a database table.
        /// </summary>
        /// <remarks>This method performs a bulk write operation to efficiently insert or update multiple
        /// rows in a database table. It supports optional configuration for handling specific column types, such as
        /// auto-increment, primary key, unique, and auto-generated date columns. The operation can be performed within
        /// an existing transaction if <paramref name="sqlTransaction"/> is provided.</remarks>
        /// <typeparam name="T">The type of the objects in the <paramref name="sourceData"/> collection.</typeparam>
        /// <param name="existingConnection">An existing <see cref="MySqlConnection"/> to use for the operation. The connection must be open.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the operation. If <c>null</c>, no transaction
        /// is used.</param>
        /// <param name="sourceData">The collection of data to be written to the database table. Each item represents a row to be inserted or
        /// updated.</param>
        /// <param name="tableName">The name of the target database table. If <c>null</c>, the table name is inferred from the type
        /// <typeparamref name="T"/>.</param>
        /// <param name="forceType">An optional <see cref="Type"/> to enforce a specific type mapping for the operation. If <c>null</c>, the
        /// type is inferred from <typeparamref name="T"/>.</param>
        /// <param name="batchSize">The maximum number of rows to include in each batch during the bulk write operation. Must be greater than
        /// zero. Default is 100.</param>
        /// <param name="databaseName">The name of the target database. If <c>null</c>, the default database associated with the connection is
        /// used.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns are included in the operation. If <see langword="true"/>,
        /// auto-increment columns are included; otherwise, they are excluded. Default is <see langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are included in the operation. If <see langword="true"/>,
        /// primary key columns are included; otherwise, they are excluded. Default is <see langword="false"/>.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns are included in the operation. If <see langword="true"/>, unique
        /// columns are included; otherwise, they are excluded. Default is <see langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether auto-generated date columns (e.g., timestamps) are included in the operation. If
        /// <see langword="true"/>, auto-generated date columns are included; otherwise, they are excluded. Default is
        /// <see langword="false"/>.</param>
        /// <returns>The total number of rows successfully written to the database.</returns>
        internal static async Task<int> BulkTableWriteAsync<T>(MySqlConnection existingConnection, MySqlTransaction sqlTransaction, IEnumerable<T> sourceData, string? tableName = null, Type? forceType = null, int batchSize = 100, string? databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false, CancellationToken cancellationToken = default)
        {
            return await BulkTableWriteAsync<T>(new RelmContext(existingConnection, sqlTransaction), sourceData, tableName: tableName, forceType: forceType, batchSize: batchSize, databaseName: databaseName, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Performs a bulk write operation to a database table using the specified data source and configuration
        /// options.
        /// </summary>
        /// <remarks>This method provides a high-performance mechanism for inserting large amounts of data
        /// into a database table. The behavior of the operation can be customized using the optional parameters to
        /// control schema enforcement and column constraints.</remarks>
        /// <typeparam name="T">The type of the objects in the data source.</typeparam>
        /// <param name="relmContext">The database context used to manage the connection and transaction for the operation. Cannot be <see
        /// langword="null"/>.</param>
        /// <param name="sourceData">The collection of data to be written to the table. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="tableName">The name of the target database table. If <see langword="null"/>, the table name is inferred from the type
        /// <typeparamref name="T"/>.</param>
        /// <param name="forceType">An optional type to enforce for the table schema. If <see langword="null"/>, the schema is inferred from
        /// <typeparamref name="T"/>.</param>
        /// <param name="batchSize">The number of records to write in each batch. Must be greater than 0. Defaults to 100.</param>
        /// <param name="databaseName">The name of the target database. If <see langword="null"/>, the default database is used.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns are allowed in the bulk write operation. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns are allowed in the bulk write operation. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns are allowed in the bulk write operation. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">Indicates whether auto-generated date columns are allowed in the bulk write operation. Defaults to <see
        /// langword="false"/>.</param>
        /// <returns>The total number of rows successfully written to the table.</returns>
        internal static int BulkTableWrite<T>(IRelmContext relmContext, IEnumerable<T> sourceData, string? tableName = null, Type? forceType = null, int batchSize = 100, string? databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        {
            return BulkTableWriteAsyncStatic(GetBulkTableWriter<T>(relmContext), sourceData, tableName: tableName, forceType: forceType, batchSize: batchSize, databaseName: databaseName, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Performs a bulk write operation to a database table using the specified data source and configuration
        /// options.
        /// </summary>
        /// <remarks>This method provides a high-performance mechanism for inserting large amounts of data
        /// into a database table. The behavior of the operation can be customized using the optional parameters to
        /// control schema enforcement and column constraints.</remarks>
        /// <typeparam name="T">The type of the objects in the data source.</typeparam>
        /// <param name="relmContext">The database context used to manage the connection and transaction for the operation. Cannot be <see
        /// langword="null"/>.</param>
        /// <param name="sourceData">The collection of data to be written to the table. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="tableName">The name of the target database table. If <see langword="null"/>, the table name is inferred from the type
        /// <typeparamref name="T"/>.</param>
        /// <param name="forceType">An optional type to enforce for the table schema. If <see langword="null"/>, the schema is inferred from
        /// <typeparamref name="T"/>.</param>
        /// <param name="batchSize">The number of records to write in each batch. Must be greater than 0. Defaults to 100.</param>
        /// <param name="databaseName">The name of the target database. If <see langword="null"/>, the default database is used.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns are allowed in the bulk write operation. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns are allowed in the bulk write operation. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns are allowed in the bulk write operation. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">Indicates whether auto-generated date columns are allowed in the bulk write operation. Defaults to <see
        /// langword="false"/>.</param>
        /// <returns>The total number of rows successfully written to the table.</returns>
        internal static async Task<int> BulkTableWriteAsync<T>(IRelmContext relmContext, IEnumerable<T> sourceData, string? tableName = null, Type? forceType = null, int batchSize = 100, string? databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false, CancellationToken cancellationToken = default)
        {
            return await BulkTableWriteAsyncStatic(GetBulkTableWriter<T>(relmContext), sourceData, tableName: tableName, forceType: forceType, batchSize: batchSize, databaseName: databaseName, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Writes a collection of data to a database table in bulk, using the specified table writer and configuration
        /// options.
        /// </summary>
        /// <remarks>This method performs a bulk write operation to the specified database table. The
        /// table and database names can be explicitly provided or inferred from attributes on the entity type. The
        /// operation is performed in batches, with the batch size controlled by the <paramref name="batchSize"/>
        /// parameter. Use the optional flags to control whether specific types of columns (e.g., auto-increment,
        /// primary key, unique, or auto-date columns) are included in the write operation.</remarks>
        /// <typeparam name="T">The type of the data entities to be written to the database table.</typeparam>
        /// <param name="tableWriter">The <see cref="BulkTableWriter{T}"/> instance used to perform the bulk write operation.</param>
        /// <param name="sourceData">The collection of data entities to be written to the database table. Cannot be null.</param>
        /// <param name="tableName">The name of the database table to write to. If null, the table name is inferred from the <see
        /// cref="RelmTable"/> attribute on the type <typeparamref name="T"/> or the <paramref name="forceType"/>.</param>
        /// <param name="forceType">An optional type used to override the type <typeparamref name="T"/> for attribute-based table and database
        /// name resolution.</param>
        /// <param name="batchSize">The number of rows to write in each batch. Defaults to 100. Must be greater than zero.</param>
        /// <param name="databaseName">The name of the database to write to. If null, the database name is inferred from the <see
        /// cref="RelmDatabase"/> attribute on the type <typeparamref name="T"/> or the <paramref name="forceType"/>.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns are allowed to be written. Defaults to <see langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns are allowed to be written. Defaults to <see langword="false"/>.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns are allowed to be written. Defaults to <see langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">Indicates whether auto-date columns are allowed to be written. Defaults to <see langword="false"/>.</param>
        /// <returns>The total number of rows successfully written to the database table.</returns>
        /// <exception cref="CustomAttributeFormatException">Thrown if the table name cannot be resolved because the <see cref="RelmTable"/> attribute is missing on the
        /// type <typeparamref name="T"/> or the <paramref name="forceType"/>.</exception>
        internal static async Task<int> BulkTableWriteAsyncStatic<T>(BulkTableWriter<T> tableWriter, IEnumerable<T> sourceData, string? tableName = null, Type? forceType = null, int batchSize = 100, string? databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false, CancellationToken cancellationToken = default)
        {
            var rowsUpdated = await tableWriter
                .SetTableName(tableName ?? (forceType ?? typeof(T)).GetCustomAttribute<RelmTable>()?.TableName ?? throw new CustomAttributeFormatException(CoreUtilities.NoDalTableAttributeError))
                .SetDatabaseName(databaseName ?? (forceType ?? typeof(T)).GetCustomAttribute<RelmDatabase>()?.DatabaseName)
                .SetSourceData(sourceData)
                .SetBatchSize(batchSize)
                .AllowAutoDateColumns(allowAutoDateColumns)
                .AllowAutoIncrementColumns(allowAutoIncrementColumns)
                .AllowPrimaryKeyColumns(allowPrimaryKeyColumns)
                .AllowUniqueColumns(allowUniqueColumns)
                .WriteAsync(cancellationToken: cancellationToken);

            return rowsUpdated;
        }
    }
}
