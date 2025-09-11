using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.Models;
using CoreRelm.Persistence;
using CoreRelm.RelmInternal.Helpers.Operations;
using CoreRelm.RelmInternal.Helpers.Utilities;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.DataTransfer.Persistence
{
    internal class DataOutputOperations
    {
        /// <summary>
        /// Factory to retrieve a new bulk table writer instance. Caller can then run bulk inserts as per BulkTableWriter class.
        /// </summary>
        /// <param name="ConfigConnectionString">The DALHelper connection string type</param>
        /// <param name="InsertQuery">The full SQL query to be run on each row insert</param>
        /// <param name="UseTransaction">Indicate whether to write all the data in a single transaction</param>
        /// <param name="ThrowException">Indicate whether to throw exception or record and proceed</param>
        /// <returns>An object to add data to and write that data to the database</returns>
        internal static BulkTableWriter<T> GetBulkTableWriter<T>(Enum ConfigConnectionString, string InsertQuery = null, bool UseTransaction = false, bool ThrowException = true, bool AllowUserVariables = false, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false)
        {
            return new BulkTableWriter<T>(ConfigConnectionString, InsertQuery: InsertQuery, ThrowException: ThrowException, UseTransaction: UseTransaction, AllowUserVariables: AllowUserVariables, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns, AllowAutoDateColumns: AllowAutoDateColumns);
        }

        internal static BulkTableWriter<T> GetBulkTableWriter<T>(MySqlConnection ExistingConnection, string InsertQuery = null, bool UseTransaction = false, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false)
        {
            return GetBulkTableWriter<T>(new RelmContext(ExistingConnection, SqlTransaction), InsertQuery: InsertQuery, UseTransaction: UseTransaction, ThrowException: ThrowException, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns, AllowAutoDateColumns: AllowAutoDateColumns);
        }

        internal static BulkTableWriter<T> GetBulkTableWriter<T>(IRelmQuickContext relmContext, string InsertQuery = null, bool UseTransaction = false, bool ThrowException = true, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false)
        {
            return new BulkTableWriter<T>(relmContext, InsertQuery: InsertQuery, ThrowException: ThrowException, UseTransaction: UseTransaction || relmContext.ContextOptions.DatabaseTransaction != null, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns, AllowAutoDateColumns: AllowAutoDateColumns);
        }

        internal static BulkTableWriter<T> GetBulkTableWriter<T>(IRelmContext relmContext, string InsertQuery = null, bool UseTransaction = false, bool ThrowException = true, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false)
        {
            return new BulkTableWriter<T>(relmContext, InsertQuery: InsertQuery, ThrowException: ThrowException, UseTransaction: UseTransaction || relmContext.ContextOptions.DatabaseTransaction != null, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns, AllowAutoDateColumns: AllowAutoDateColumns);
        }

        internal static int BulkTableWrite<T>(Enum ConfigConnectionString, T SourceData, string TableName = null, Type ForceType = null, bool AllowUserVariables = false, int BatchSize = 100, string DatabaseName = null, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false)
        {
            using (var conn = ConnectionHelper.GetConnectionFromType(ConfigConnectionString, allowUserVariables: AllowUserVariables))
            {
                return BulkTableWrite<T>(conn, SourceData, TableName, null, ForceType, BatchSize, DatabaseName, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns, AllowAutoDateColumns: AllowAutoDateColumns);
            }
        }

        internal static int BulkTableWrite<T>(Enum ConfigConnectionString, IEnumerable<T> SourceData, string TableName = null, Type ForceType = null, bool AllowUserVariables = false, int BatchSize = 100, string DatabaseName = null, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false)
        {
            using (var conn = ConnectionHelper.GetConnectionFromType(ConfigConnectionString, allowUserVariables: AllowUserVariables))
            {
                return BulkTableWrite<T>(conn, SourceData, TableName, null, ForceType, BatchSize, DatabaseName, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns, AllowAutoDateColumns: AllowAutoDateColumns);
            }
        }

        //TODO: make each BulkTableWrite below chain up to a single BulkTableWrite that then calls GetBulkTableWriter
        internal static int BulkTableWrite<T>(MySqlConnection ExistingConnection, T SourceData, string TableName = null, MySqlTransaction SqlTransaction = null, Type ForceType = null, int BatchSize = 100, string DatabaseName = null, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false)
        {
            return BulkTableWrite<T>(new RelmContext(ExistingConnection, SqlTransaction), SourceData, TableName: TableName, ForceType: ForceType, BatchSize: BatchSize, DatabaseName: DatabaseName, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns, AllowAutoDateColumns: AllowAutoDateColumns);
        }

        internal static int BulkTableWrite<T>(IRelmQuickContext relmContext, T SourceData, string TableName = null, Type ForceType = null, int BatchSize = 100, string DatabaseName = null, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false)
        {
            return BulkTableWriteStatic<T>(GetBulkTableWriter<T>(relmContext), SourceData, TableName: TableName, ForceType: ForceType, BatchSize: BatchSize, DatabaseName: DatabaseName, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns, AllowAutoDateColumns: AllowAutoDateColumns);
        }

        internal static int BulkTableWrite<T>(IRelmContext relmContext, T SourceData, string TableName = null, Type ForceType = null, int BatchSize = 100, string DatabaseName = null, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false)
        {
            return BulkTableWriteStatic<T>(GetBulkTableWriter<T>(relmContext), SourceData, TableName: TableName, ForceType: ForceType, BatchSize: BatchSize, DatabaseName: DatabaseName, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns, AllowAutoDateColumns: AllowAutoDateColumns);
        }

        private static int BulkTableWriteStatic<T>(BulkTableWriter<T> tableWriter, T SourceData, string TableName = null, Type ForceType = null, int BatchSize = 100, string DatabaseName = null, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false)
        {
            var rowsUpdated = tableWriter
                .SetTableName(TableName ?? (ForceType ?? typeof(T)).GetCustomAttribute<RelmTable>()?.TableName ?? throw new CustomAttributeFormatException(CoreUtilities.NoDalTableAttributeError))
                .SetDatabaseName(DatabaseName ?? (ForceType ?? typeof(T)).GetCustomAttribute<RelmDatabase>()?.DatabaseName)
                .SetSourceData(SourceData)
                .UseTransaction(true)
                //.SetTransaction(SqlTransaction)
                .SetBatchSize(BatchSize)
                .AllowAutoDateColumns(AllowAutoDateColumns)
                .AllowAutoIncrementColumns(AllowAutoIncrementColumns)
                .AllowPrimaryKeyColumns(AllowPrimaryKeyColumns)
                .AllowUniqueColumns(AllowUniqueColumns)
                .Write();

            return rowsUpdated;
        }

        internal static int BulkTableWrite<T>(MySqlConnection ExistingConnection, IEnumerable<T> SourceData, string TableName = null, MySqlTransaction SqlTransaction = null, Type ForceType = null, int BatchSize = 100, string DatabaseName = null, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false)
        {
            return BulkTableWrite<T>(new RelmContext(ExistingConnection, SqlTransaction), SourceData, TableName: TableName, ForceType: ForceType, BatchSize: BatchSize, DatabaseName: DatabaseName, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns, AllowAutoDateColumns: AllowAutoDateColumns);
        }

        internal static int BulkTableWrite<T>(IRelmQuickContext relmContext, IEnumerable<T> SourceData, string TableName = null, Type ForceType = null, int BatchSize = 100, string DatabaseName = null, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false)
        {
            return BulkTableWriteStatic<T>(GetBulkTableWriter<T>(relmContext), SourceData, TableName: TableName, ForceType: ForceType, BatchSize: BatchSize, DatabaseName: DatabaseName, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns, AllowAutoDateColumns: AllowAutoDateColumns);
        }

        internal static int BulkTableWrite<T>(IRelmContext relmContext, IEnumerable<T> SourceData, string TableName = null, Type ForceType = null, int BatchSize = 100, string DatabaseName = null, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false)
        {
            return BulkTableWriteStatic(GetBulkTableWriter<T>(relmContext), SourceData, TableName: TableName, ForceType: ForceType, BatchSize: BatchSize, DatabaseName: DatabaseName, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns, AllowAutoDateColumns: AllowAutoDateColumns);
        }

        internal static int BulkTableWriteStatic<T>(BulkTableWriter<T> tableWriter, IEnumerable<T> SourceData, string TableName = null, Type ForceType = null, int BatchSize = 100, string DatabaseName = null, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false)
        {
            var rowsUpdated = tableWriter
                .SetTableName(TableName ?? (ForceType ?? typeof(T)).GetCustomAttribute<RelmTable>()?.TableName ?? throw new CustomAttributeFormatException(CoreUtilities.NoDalTableAttributeError))
                .SetDatabaseName(DatabaseName ?? (ForceType ?? typeof(T)).GetCustomAttribute<RelmDatabase>()?.DatabaseName)
                .SetSourceData(SourceData)
                .UseTransaction(true)
                //.SetTransaction(SqlTransaction)
                .SetBatchSize(BatchSize)
                .AllowAutoDateColumns(AllowAutoDateColumns)
                .AllowAutoIncrementColumns(AllowAutoIncrementColumns)
                .AllowPrimaryKeyColumns(AllowPrimaryKeyColumns)
                .AllowUniqueColumns(AllowUniqueColumns)
                .Write();

            return rowsUpdated;
        }
    }
}
