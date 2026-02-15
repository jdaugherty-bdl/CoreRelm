using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.Models.Migrations.Introspection;
using CoreRelm.Options;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Contexts
{
    public class InformationSchemaContext : RelmContext
    {
        public InformationSchemaContext(RelmContextOptionsBuilder optionsBuilder) : base(optionsBuilder)
        {
        }

        public InformationSchemaContext(IRelmContext relmContext, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0, bool autoInitializeDataSets = true, bool autoVerifyTables = true) : base(relmContext, autoOpenConnection, autoOpenTransaction, allowUserVariables, convertZeroDateTime, lockWaitTimeoutSeconds, autoInitializeDataSets, autoVerifyTables)
        {
        }

        public InformationSchemaContext(Enum connectionStringType, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0, bool autoInitializeDataSets = true, bool autoVerifyTables = true) : base(connectionStringType, autoOpenConnection, autoOpenTransaction, allowUserVariables, convertZeroDateTime, lockWaitTimeoutSeconds, autoInitializeDataSets, autoVerifyTables)
        {
        }

        public InformationSchemaContext(string connectionDetails, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0, bool autoInitializeDataSets = true, bool autoVerifyTables = true) : base(connectionDetails, autoOpenConnection, autoOpenTransaction, allowUserVariables, convertZeroDateTime, lockWaitTimeoutSeconds, autoInitializeDataSets, autoVerifyTables)
        {
        }

        public InformationSchemaContext(MySqlConnection connection, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0, bool autoInitializeDataSets = true, bool autoVerifyTables = true) : base(connection, autoOpenConnection, autoOpenTransaction, allowUserVariables, convertZeroDateTime, lockWaitTimeoutSeconds, autoInitializeDataSets, autoVerifyTables)
        {
        }

        public InformationSchemaContext(MySqlConnection connection, MySqlTransaction? transaction, bool autoOpenConnection = true, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0, bool autoInitializeDataSets = true, bool autoVerifyTables = true) : base(connection, transaction, autoOpenConnection, allowUserVariables, convertZeroDateTime, lockWaitTimeoutSeconds, autoInitializeDataSets, autoVerifyTables)
        {
        }

        public virtual IRelmDataSet<ColumnSchema>? Columns { get; protected set; }
        public virtual IRelmDataSet<ForeignKeySchema>? ForeignKeys { get; protected set; }
        public virtual IRelmDataSet<FunctionParameterSchema>? FunctionParameters { get; protected set; }
        public virtual IRelmDataSet<FunctionSchema>? Functions { get; protected set; }
        //public virtual IRelmDataSet<IndexColumnSchema>? IndexColumns { get; protected set; }
        public virtual IRelmDataSet<IndexSchema>? Indexes { get; protected set; }
        //public virtual IRelmDataSet<TableSchema>? Tables { get; protected set; }
        public virtual IRelmDataSet<TriggerSchema>? Triggers { get; protected set; }
    }
}
