using MySql.Data.MySqlClient;
using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.RelmInternal.Helpers.DataTransfer;
using CoreRelm.RelmInternal.Helpers.Utilities;
using CoreRelm.RelmInternal.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Triggers;

namespace CoreRelm.RelmInternal.Helpers.Operations
{
    internal class TableOperationsHelper
    {
        /// <summary>
        /// Truncates the specified database table, removing all rows while preserving the table structure.
        /// </summary>
        /// <remarks>This method removes all data from the specified table without deleting the table
        /// itself.  Use with caution, as this operation is irreversible.</remarks>
        /// <typeparam name="T">The type representing the table to truncate. This is used to infer the table schema if <paramref
        /// name="forceType"/> is not provided.</typeparam>
        /// <param name="connectionName">The connection identifier used to determine which database connection to use.</param>
        /// <param name="tableName">The name of the table to truncate. If null, the table name is inferred from the type <typeparamref
        /// name="T"/>.</param>
        /// <param name="forceType">An optional type used to explicitly specify the table schema. If null, the type <typeparamref name="T"/> is
        /// used.</param>
        /// <returns><see langword="true"/> if the table was successfully truncated; otherwise, <see langword="false"/>.</returns>
        internal static bool TruncateTable<T>(Enum connectionName, string tableName = null, Type forceType = null)
        {
            return TruncateTable(connectionName, tableName: tableName, forceType: forceType ?? typeof(T));
        }

        /// <summary>
        /// Truncates the specified database table associated with the given connection.
        /// </summary>
        /// <typeparam name="T">The type representing the table to truncate. This is used to infer the table schema or mapping.</typeparam>
        /// <param name="connectionName">The name of the database connection to use. Must be a valid enumeration value representing a configured
        /// connection.</param>
        /// <param name="tableName">The name of the table to truncate. If null, the table name is inferred from the type <typeparamref
        /// name="T"/>.</param>
        /// <returns><see langword="true"/> if the table was successfully truncated; otherwise, <see langword="false"/>.</returns>
        internal static bool TruncateTable<T>(Enum connectionName, string tableName = null)
        {
            return TruncateTable(connectionName, tableName: tableName, forceType: typeof(T));
        }

        /// <summary>
        /// Truncates the specified table in the database associated with the given connection.
        /// </summary>
        /// <remarks>This method establishes a database connection using the specified connection name and
        /// performs the truncation operation.  The operation may fail if the connection is invalid, the table does not
        /// exist, or the user lacks sufficient permissions.</remarks>
        /// <param name="connectionName">The name of the database connection, represented as an enumeration value.</param>
        /// <param name="tableName">The name of the table to truncate. If <see langword="null"/>, a default table name may be used based on the
        /// context.</param>
        /// <param name="forceType">An optional type parameter that can be used to enforce specific behavior during the truncation process. Can
        /// be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the table was successfully truncated; otherwise, <see langword="false"/>.</returns>
        internal static bool TruncateTable(Enum connectionName, string tableName = null, Type forceType = null)
        {
            using (var conn = ConnectionHelper.GetConnectionFromType(connectionName))
            {
                return TruncateTable(conn, tableName: tableName, forceType: forceType, sqlTransaction: null);
            }
        }

        /// <summary>
        /// Truncates the specified table in the database, removing all rows while preserving the table structure.
        /// </summary>
        /// <remarks>This method removes all rows from the specified table without logging individual row
        /// deletions, which can improve performance compared to deleting rows one by one. Ensure that the table does
        /// not have foreign key constraints that would prevent truncation.</remarks>
        /// <typeparam name="T">The type representing the table to truncate. If <paramref name="forceType"/> is provided, it overrides this
        /// type.</typeparam>
        /// <param name="existingConnection">An open <see cref="MySqlConnection"/> to the database. The connection must remain open for the duration of
        /// the operation.</param>
        /// <param name="tableName">The name of the table to truncate. If null, the table name is inferred from the type <typeparamref
        /// name="T"/> or <paramref name="forceType"/>.</param>
        /// <param name="forceType">An optional <see cref="Type"/> that explicitly specifies the table type to truncate, overriding
        /// <typeparamref name="T"/>.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the operation. If null, the operation is
        /// executed outside of a transaction.</param>
        /// <returns><see langword="true"/> if the table was successfully truncated; otherwise, <see langword="false"/>.</returns>
        internal static bool TruncateTable<T>(MySqlConnection existingConnection, string tableName = null, Type forceType = null, MySqlTransaction sqlTransaction = null)
        {
            return TruncateTable(existingConnection, tableName: tableName, forceType: forceType ?? typeof(T), sqlTransaction: sqlTransaction);
        }

        /// <summary>
        /// Truncates the specified table in the database, removing all rows without logging individual deletions.
        /// </summary>
        /// <remarks>This method performs a SQL TRUNCATE operation, which is typically faster than
        /// deleting rows individually but may have restrictions depending on the database schema. Ensure that the user
        /// has the necessary permissions to truncate the specified table.</remarks>
        /// <typeparam name="T">The type used to infer the table schema or mapping, if applicable.</typeparam>
        /// <param name="existingConnection">An open <see cref="MySqlConnection"/> to the database. The connection must remain open for the duration of
        /// the operation.</param>
        /// <param name="tableName">The name of the table to truncate. If null, the table name is inferred based on the type <typeparamref
        /// name="T"/>.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the operation. If null, the operation is
        /// executed outside of a transaction.</param>
        /// <returns><see langword="true"/> if the table was successfully truncated; otherwise, <see langword="false"/>.</returns>
        internal static bool TruncateTable<T>(MySqlConnection existingConnection, string tableName = null, MySqlTransaction sqlTransaction = null)
        {
            return TruncateTable(existingConnection, tableName: tableName, forceType: typeof(T), sqlTransaction: sqlTransaction);
        }

        /// <summary>
        /// Truncates the specified table in the database.
        /// </summary>
        /// <remarks>This method executes a SQL `TRUNCATE` statement on the specified table. The operation
        /// is performed within a transaction if <paramref name="sqlTransaction"/> is provided or if the default
        /// transaction behavior is enabled.</remarks>
        /// <param name="existingConnection">An open <see cref="MySqlConnection"/> to the database where the table resides. The connection must remain
        /// open for the duration of the operation.</param>
        /// <param name="tableName">The name of the table to truncate. If <paramref name="tableName"/> is <see langword="null"/>, the table name
        /// is determined from the <see cref="RelmTable"/> attribute of the type specified in <paramref
        /// name="forceType"/>.</param>
        /// <param name="forceType">A <see cref="Type"/> that specifies the table to truncate, based on its <see cref="RelmTable"/> attribute.
        /// This parameter is used only if <paramref name="tableName"/> is <see langword="null"/>.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to use for the operation. If provided, the operation will be
        /// executed within the context of this transaction.</param>
        /// <returns><see langword="true"/> if the table was successfully truncated; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="CustomAttributeFormatException">Thrown if <paramref name="tableName"/> is <see langword="null"/> and the <paramref name="forceType"/> does
        /// not have a <see cref="RelmTable"/> attribute.</exception>
        internal static bool TruncateTable(MySqlConnection existingConnection, string tableName = null, Type forceType = null, MySqlTransaction sqlTransaction = null)
        {
            var localTableName = tableName ?? forceType.GetCustomAttribute<RelmTable>()?.TableName ?? throw new CustomAttributeFormatException(CoreUtilities.NoDalTableAttributeError);

            var truncateQuery = $"TRUNCATE {localTableName};";

            var rowsUpdated = DatabaseWorkHelper.DoDatabaseWork<int>(existingConnection, truncateQuery, useTransaction: true, sqlTransaction: sqlTransaction);

            var success = rowsUpdated > 0;

            return success;
        }

        /// <summary>
        /// Retrieves a writable table definition for the specified connection.
        /// </summary>
        /// <typeparam name="T">The type of the table entity.</typeparam>
        /// <param name="connectionName">The name of the connection as an enumeration value.</param>
        /// <returns>A <see cref="WritableTableDefinition{T}"/> representing the writable table for the specified connection.</returns>
        internal static WritableTableDefinition<T> GetWritableTableObject<T>(Enum connectionName)
        {
            return GetWritableTableObject<T>(connectionName: connectionName);
        }

        /// <summary>
        /// Retrieves a writable table definition for the specified type, using the provided MySQL connection.
        /// </summary>
        /// <typeparam name="T">The type representing the table structure.</typeparam>
        /// <param name="existingConnection">An existing <see cref="MySqlConnection"/> to be used for database operations. Cannot be <see
        /// langword="null"/>.</param>
        /// <returns>A <see cref="WritableTableDefinition{T}"/> instance that allows operations on the writable table.</returns>
        internal static WritableTableDefinition<T> GetWritableTableObject<T>(MySqlConnection existingConnection)
        {
            return GetWritableTableObject<T>(existingConnection: existingConnection);
        }

        /// <summary>
        /// Retrieves a writable table definition for the specified data access layer (DAL) model type.
        /// </summary>
        /// <remarks>This method automatically includes standard triggers in the table
        /// definition.</remarks>
        /// <typeparam name="T">The type of the DAL model for which the table definition is retrieved.</typeparam>
        /// <param name="connectionName">The name of the database connection as an enumeration value.</param>
        /// <returns>A <see cref="WritableTableDefinition{T}"/> representing the writable table definition for the specified DAL
        /// model type.</returns>
        internal static WritableTableDefinition<T> GetDalModelTableObject<T>(Enum connectionName)
        {
            return GetWritableTableObject<T>(connectionName: connectionName, addStandardTriggers: true);
        }

        /// <summary>
        /// Retrieves a writable table definition for the specified data model type.
        /// </summary>
        /// <typeparam name="T">The type of the data model associated with the table.</typeparam>
        /// <param name="existingConnection">An existing <see cref="MySqlConnection"/> to use for database operations. Cannot be null.</param>
        /// <returns>A <see cref="WritableTableDefinition{T}"/> representing the writable table definition for the specified data
        /// model type.</returns>
        internal static WritableTableDefinition<T> GetDalModelTableObject<T>(MySqlConnection existingConnection)
        {
            return GetWritableTableObject<T>(existingConnection: existingConnection, addStandardTriggers: true);
        }

        /// <summary>
        /// Creates and returns a writable table definition for the specified type, with optional configuration for
        /// triggers and database connection.
        /// </summary>
        /// <typeparam name="T">The type representing the table schema.</typeparam>
        /// <param name="connectionName">An optional enumeration value specifying the connection name. If null, the default connection is used.</param>
        /// <param name="existingConnection">An optional <see cref="MySqlConnection"/> instance to use for determining the database name. If null, the
        /// database name is resolved from the connection name.</param>
        /// <param name="addStandardTriggers">A value indicating whether to add standard triggers to the table definition.  If <see langword="true"/>,
        /// triggers for setting the last updated timestamp and generating a UUID for the internal ID are added.</param>
        /// <returns>A <see cref="WritableTableDefinition{T}"/> instance configured with the specified database name and optional
        /// triggers.</returns>
        private static WritableTableDefinition<T> GetWritableTableObject<T>(Enum connectionName = null, MySqlConnection existingConnection = null, bool addStandardTriggers = false)
        {
            var tableDef = new WritableTableDefinition<T>
            {
                DatabaseName = existingConnection?.Database ?? RelmHelper.GetConnectionBuilderFromConnectionType(connectionName)?.Database
            };

            if (addStandardTriggers)
                tableDef
                    .SetTrigger(TriggerTypes.BeforeUpdate, "set NEW.last_updated = CURRENT_TIMESTAMP;")
                    .SetTrigger(TriggerTypes.BeforeInsert, "set new.InternalId = IFNULL(new.InternalId, uuid());\r\nset NEW.last_updated = CURRENT_TIMESTAMP;");

            return tableDef;
        }

        /// <summary>
        /// Creates a database table for the specified type, optionally truncating the table if it already exists.
        /// </summary>
        /// <remarks>This method uses the specified database connection to create the table. If the table
        /// already exists  and <paramref name="truncateIfExists"/> is set to <see langword="true"/>, the table will be
        /// truncated  before creation.</remarks>
        /// <typeparam name="T">The type representing the structure of the table to be created.</typeparam>
        /// <param name="connectionName">An enumeration value representing the database connection to use.</param>
        /// <param name="truncateIfExists">A value indicating whether to truncate the table if it already exists.  <see langword="true"/> to truncate
        /// the table; otherwise, <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if the table was successfully created; otherwise, <see langword="false"/>.</returns>
        internal static bool CreateTable<T>(Enum connectionName, bool truncateIfExists = false)
        {
            using (var conn = ConnectionHelper.GetConnectionFromType(connectionName))
            {
                return CreateTable<T>(conn, null, truncateIfExists: truncateIfExists);
            }
        }

        /// <summary>
        /// Creates a database table for the specified type, with optional behavior for truncating or dropping the table
        /// if it already exists.
        /// </summary>
        /// <remarks>This method uses the specified connection and optional transaction to create the
        /// table. The behavior of the method can be customized using the <paramref name="truncateIfExists"/> and
        /// <paramref name="dropIfExists"/> parameters, but these options are mutually exclusive.</remarks>
        /// <typeparam name="T">The type representing the table schema to be created.</typeparam>
        /// <param name="existingConnection">An open <see cref="MySqlConnection"/> to the database where the table will be created.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to associate with the table creation operation. Defaults to <see
        /// langword="null"/>.</param>
        /// <param name="truncateIfExists">A value indicating whether to truncate the table if it already exists. If <see langword="true"/>, the
        /// table's data will be cleared.</param>
        /// <param name="dropIfExists">A value indicating whether to drop the table if it already exists. If <see langword="true"/>, the table will
        /// be dropped and recreated.</param>
        /// <returns><see langword="true"/> if the table was successfully created; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if both <paramref name="truncateIfExists"/> and <paramref name="dropIfExists"/> are set to <see
        /// langword="true"/>.</exception>
        internal static bool CreateTable<T>(MySqlConnection existingConnection, MySqlTransaction sqlTransaction = null, bool truncateIfExists = false, bool dropIfExists = false)
        {
            if (truncateIfExists && dropIfExists)
                throw new ArgumentException("Cannot both truncate and drop table on create.");

            var createdTable = GetDalModelTableObject<T>(existingConnection: existingConnection);

            var rowsUpdated = RelmHelper.DoDatabaseWork<int>(existingConnection, createdTable.ToString(), useTransaction: false, sqlTransaction: sqlTransaction);

            return true;
        }

        /// <summary>
        /// Determines whether a table with the specified name exists in the database associated with the given
        /// connection type.
        /// </summary>
        /// <remarks>This method establishes a database connection based on the specified connection
        /// string type and checks for the existence of the table.</remarks>
        /// <param name="connectionName">The type of connection string to use for establishing the database connection.</param>
        /// <param name="tableName">The name of the table to check for existence. Cannot be null or empty.</param>
        /// <returns><see langword="true"/> if the table exists; otherwise, <see langword="false"/>.</returns>
        internal static bool TableExists(Enum connectionName, string tableName)
        {
            using (var conn = ConnectionHelper.GetConnectionFromType(connectionName))
            {
                return TableExists(conn, tableName, null);
            }
        }

        /// <summary>
        /// Determines whether a table with the specified name exists in the current database.
        /// </summary>
        /// <remarks>This method queries the database's information schema to determine the existence of
        /// the specified table. Ensure that the provided connection is open and points to the correct database before
        /// calling this method.</remarks>
        /// <param name="existingConnection">An open <see cref="MySqlConnection"/> to the database where the table existence will be checked.</param>
        /// <param name="tableName">The name of the table to check for existence. This value is case-sensitive and cannot be null or empty.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to use for the query. If null, the query will execute outside of
        /// a transaction.</param>
        /// <returns><see langword="true"/> if a table with the specified name exists in the current database; otherwise, <see
        /// langword="false"/>.</returns>
        internal static bool TableExists(MySqlConnection existingConnection, string tableName, MySqlTransaction sqlTransaction = null)
        {
            // pull the table details from the database
            var existsQuery = @"SELECT TABLE_NAME
                FROM information_schema.tables
                WHERE table_schema = @table_schema
                    AND table_name = @table_name
                LIMIT 1;";

            var localTableName = RefinedResultsHelper.GetScalar<string>(existingConnection, existsQuery, new Dictionary<string, object>
            {
                ["@table_schema"] = existingConnection.Database,
                ["@table_name"] = tableName
            }, sqlTransaction: sqlTransaction);

            return !string.IsNullOrWhiteSpace(localTableName);
        }

        /// <summary>
        /// Retrieves the name of the data access layer (DAL) table associated with the specified model type.
        /// </summary>
        /// <typeparam name="T">The type of the model, which must implement <see cref="IRelmModel"/> and have a parameterless constructor.</typeparam>
        /// <returns>A string representing the name of the DAL table associated with the specified model type.</returns>
        internal static string GetDalTable<T>() where T : IRelmModel, new()
        {
            return GetDalTable(typeof(T));
        }

        /// <summary>
        /// Retrieves the table name associated with the specified data access layer (DAL) object type.
        /// </summary>
        /// <param name="dalObjectType">The type of the DAL object. This type must have a <see cref="RelmTable"/> attribute applied.</param>
        /// <returns>The table name specified in the <see cref="RelmTable"/> attribute of the provided type,  or <see
        /// langword="null"/> if the attribute is not present.</returns>
        internal static string GetDalTable(Type dalObjectType)
        {
            return dalObjectType.GetCustomAttribute<RelmTable>()?.TableName;
        }

        /// <summary>
        /// Retrieves the column name associated with the specified property of a model.
        /// </summary>
        /// <typeparam name="T">The type of the model, which must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="predicate">An expression that specifies the property for which to retrieve the column name. The expression must be a
        /// member access expression targeting a property.</param>
        /// <returns>The column name defined by the <see cref="RelmColumn"/> attribute applied to the specified property.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="predicate"/> is not a member access expression or does not target a property.</exception>
        /// <exception cref="CustomAttributeFormatException">Thrown if the specified property does not have a <see cref="RelmColumn"/> attribute.</exception>
        internal static string GetColumnName<T>(Expression<Func<T, object>> predicate) where T : IRelmModel
        {
            var member = predicate.Body as MemberExpression 
                ?? throw new ArgumentException("Predicate must be a member expression");

            var propertyInfo = member.Member as PropertyInfo 
                ?? throw new ArgumentException("Predicate must be a property expression");

            var relmColumnAttribute = propertyInfo.GetCustomAttribute<RelmColumn>();
            return relmColumnAttribute == null
                ? throw new CustomAttributeFormatException(CoreUtilities.NoDalPropertyAttributeError)
                : relmColumnAttribute.ColumnName;
        }
    }
}
