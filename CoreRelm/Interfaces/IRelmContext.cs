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
    public interface IRelmContext
    {
        RelmContextOptionsBuilder ContextOptions { get; }

        void CommitTransaction();
        void RollbackTransaction();
        void RollbackTransactions();

        void SetDataLoader<T>(IRelmDataLoader<T> dataLoader) where T : RelmModel, new();

        void StartConnection(bool autoOpenTransaction = false, int lockWaitTimeoutSeconds = 0);
        void EndConnection(bool commitTransaction = true);
        bool HasDataSet<T>(bool throwException = true) where T : IRelmModel, new();
        bool HasDataSet(Type dataSetType, bool throwException = true);
        IRelmDataSet<T> GetDataSet<T>() where T : IRelmModel, new();
        IRelmDataSet<T> GetDataSet<T>(bool throwException) where T : IRelmModel, new();
        IRelmDataSetBase GetDataSet(Type dataSetType);
        IRelmDataSetBase GetDataSet(Type dataSetType, bool throwException);
        IRelmDataSet<T> GetDataSetType<T>() where T : IRelmModel, new();
        IRelmDataSet<T> GetDataSetType<T>(bool throwException) where T : IRelmModel, new();
        IRelmDataSetBase GetDataSetType(Type dataSetType);
        IRelmDataSetBase GetDataSetType(Type dataSetType, bool throwException);
        ICollection<T> Get<T>(bool loadDataLoaders = false) where T : IRelmModel, new();
        ICollection<T> Get<T>(Expression<Func<T, bool>> predicate, bool loadDataLoaders = false) where T : IRelmModel, new();
        T FirstOrDefault<T>(Expression<Func<T, bool>> predicate, bool loadDataLoaders = false) where T : IRelmModel, new();
        IRelmDataSet<T> Where<T>(Expression<Func<T, bool>> predicate) where T : IRelmModel, new();
        ICollection<T> Run<T>(string query, Dictionary<string, object> parameters = null) where T : IRelmModel, new();

        string GetLastInsertId();
        string GetIdFromInternalId(string Table, string InternalId);
        DataRow GetDataRow(string QueryString, Dictionary<string, object> Parameters = null, bool throwException = true);
        DataTable GetDataTable(string QueryString, Dictionary<string, object> Parameters = null, bool throwException = true);
        T GetDataObject<T>(string QueryString, Dictionary<string, object> Parameters = null, bool throwException = true) where T : IRelmModel, new();
        IEnumerable<T> GetDataObjects<T>(string QueryString, Dictionary<string, object> Parameters = null, bool throwException = true) where T : IRelmModel, new();
        IEnumerable<T> GetDataList<T>(string QueryString, Dictionary<string, object> Parameters = null, bool throwException = true);
        T GetScalar<T>(string query, Dictionary<string, object> parameters = null, bool throwException = true);

        BulkTableWriter<T> GetBulkTableWriter<T>(string InsertQuery = null, bool useTransaction = false, bool throwException = true, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false);
        int BulkTableWrite<T>(T SourceData, string TableName = null, MySqlTransaction sqlTransaction = null, Type ForceType = null, int BatchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false);

        void DoDatabaseWork(string QueryString, Dictionary<string, object> Parameters = null, bool throwException = true, bool useTransaction = false);
        T DoDatabaseWork<T>(string QueryString, Dictionary<string, object> Parameters = null, bool throwException = true, bool useTransaction = false);
        void DoDatabaseWork(string QueryString, Func<MySqlCommand, object> ActionCallback, bool throwException = true, bool useTransaction = false);
        T DoDatabaseWork<T>(string QueryString, Func<MySqlCommand, object> ActionCallback, bool throwException = true, bool useTransaction = false);

        int WriteToDatabase(IRelmModel relmModel, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false);
        int WriteToDatabase(IEnumerable<IRelmModel> relmModels, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false);
    }
}
