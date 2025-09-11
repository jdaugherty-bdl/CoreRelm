using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.Options;
using CoreRelm.RelmInternal.Helpers.DataTransfer;
using CoreRelm.RelmInternal.Helpers.DataTransfer.Persistence;
using CoreRelm.RelmInternal.Helpers.Utilities;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Extensions
{
    public static class RelmListExtensions
    {
        public static int WriteToDatabase<T>(this IEnumerable<T> DbModelData, Enum ConnectionStringType, string TableName = null, Type ForceType = null, bool AllowUserVariables = false, int BatchSize = 100, string DatabaseName = null, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false) where T : IRelmModel
        {
            return DataOutputOperations.BulkTableWrite<T>(ConnectionStringType, DbModelData, TableName, ForceType, AllowUserVariables: AllowUserVariables, BatchSize, DatabaseName, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns, AllowAutoDateColumns: AllowAutoDateColumns);
        }

        public static int WriteToDatabase<T>(this IEnumerable<T> DbModelData, MySqlConnection ExistingConnection, MySqlTransaction SqlTransaction = null, string TableName = null, Type ForceType = null, int BatchSize = 100, string DatabaseName = null, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false) where T : IRelmModel
        {
            return DataOutputOperations.BulkTableWrite<T>(ExistingConnection, DbModelData, TableName, SqlTransaction: SqlTransaction, ForceType, BatchSize, DatabaseName, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns, AllowAutoDateColumns: AllowAutoDateColumns);
        }

        public static int WriteToDatabase<T>(this IEnumerable<T> DbModelData, IRelmQuickContext relmContext, string TableName = null, Type ForceType = null, int BatchSize = 100, string DatabaseName = null, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false)
        {
            return DataOutputOperations.BulkTableWrite<T>(relmContext, DbModelData, TableName, ForceType, BatchSize, DatabaseName, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns, AllowAutoDateColumns: AllowAutoDateColumns);
        }

        public static int WriteToDatabase<T>(this IEnumerable<T> DbModelData, IRelmContext relmContext, string TableName = null, Type ForceType = null, int BatchSize = 100, string DatabaseName = null, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false)
        {
            return DataOutputOperations.BulkTableWrite<T>(relmContext, DbModelData, TableName, ForceType, BatchSize, DatabaseName, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns, AllowAutoDateColumns: AllowAutoDateColumns);
        }

        public static ICollection<T> LoadForeignKeyField<T, R, S>(this ICollection<T> DbModelData, IRelmContext relmContext, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
        {
            return new ForeignKeyLoader<T>(DbModelData, relmContext.ContextOptions).LoadForeignKey(predicate, customDataLoader, additionalConstraints);
        }

        public static ICollection<T> LoadForeignKeyField<T, R, S>(this ICollection<T> DbModelData, IRelmContext relmContext, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
        {
            return new ForeignKeyLoader<T>(DbModelData, relmContext.ContextOptions).LoadForeignKey(predicate, customDataLoader);
        }

        public static ICollection<T> LoadForeignKeyField<T, R>(this ICollection<T> DbModelData, IRelmContext relmContext, Expression<Func<T, R>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
        {
            return new ForeignKeyLoader<T>(DbModelData, relmContext.ContextOptions).LoadForeignKey(predicate, additionalConstraints);
        }

        public static ICollection<T> LoadForeignKeyField<T, R, S>(this ICollection<T> DbModelData, IRelmContext relmContext, Expression<Func<T, R>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
        {
            return new ForeignKeyLoader<T>(DbModelData, relmContext.ContextOptions).LoadForeignKey(predicate);
        }

        public static ICollection<T> LoadForeignKeyField<T, R, S>(this ICollection<T> DbModelData, IRelmContext relmContext, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
        {
            return new ForeignKeyLoader<T>(DbModelData, relmContext.ContextOptions).LoadForeignKey(predicate, customDataLoader, additionalConstraints);
        }

        public static ICollection<T> LoadForeignKeyField<T, R, S>(this ICollection<T> DbModelData, IRelmContext relmContext, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
        {
            return new ForeignKeyLoader<T>(DbModelData, relmContext.ContextOptions).LoadForeignKey(predicate, customDataLoader);
        }

        public static ICollection<T> LoadForeignKeyField<T, R>(this ICollection<T> DbModelData, IRelmContext relmContext, Expression<Func<T, ICollection<R>>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
        {
            return new ForeignKeyLoader<T>(DbModelData, relmContext.ContextOptions).LoadForeignKey(predicate, additionalConstraints);
        }

        public static ICollection<T> LoadForeignKeyField<T, R>(this ICollection<T> target, IRelmContext relmContext, Expression<Func<T, ICollection<R>>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
        {
            return new ForeignKeyLoader<T>(target, relmContext.ContextOptions).LoadForeignKey(predicate);
        }

        public static ICollection<T> LoadForeignKeyField<T, R, S>(this ICollection<T> DbModelData, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
        {
            return new ForeignKeyLoader<T>(DbModelData, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader, additionalConstraints);
        }

        public static ICollection<T> LoadForeignKeyField<T, R, S>(this ICollection<T> DbModelData, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
        {
            return new ForeignKeyLoader<T>(DbModelData, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader);
        }

        public static ICollection<T> LoadForeignKeyField<T, R>(this ICollection<T> DbModelData, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, ICollection<R>>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
        {
            return new ForeignKeyLoader<T>(DbModelData, relmContextOptionsBuilder).LoadForeignKey(predicate, additionalConstraints);
        }

        public static ICollection<T> LoadForeignKeyField<T, R>(this ICollection<T> DbModelData, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, R>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
        {
            return new ForeignKeyLoader<T>(DbModelData, relmContextOptionsBuilder).LoadForeignKey(predicate, additionalConstraints);
        }

        public static ICollection<T> LoadForeignKeyField<T, R, S>(this ICollection<T> DbModelData, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, R>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
        {
            return new ForeignKeyLoader<T>(DbModelData, relmContextOptionsBuilder).LoadForeignKey(predicate);
        }

        public static ICollection<T> LoadForeignKeyField<T, R>(this ICollection<T> target, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, ICollection<R>>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate);

        public static ICollection<T> LoadDataLoaderField<T, R>(this ICollection<T> DbModelData, IRelmContext relmContext, Expression<Func<T, R>> predicate) where T : IRelmModel, new()
        {
            return new DataLoaderHelper<T>(relmContext, DbModelData).LoadField(predicate);
        }

        public static ICollection<T> FlattenTreeObject<T>(this IEnumerable<T> EnumerableList, Func<T, ICollection<T>> GetChildrenFunction)
        {
            return EnumerableList
                .SelectMany(enumerableItem =>
                    Enumerable
                    .Repeat(enumerableItem, 1)
                    .Concat(GetChildrenFunction(enumerableItem)
                        ?.FlattenTreeObject(GetChildrenFunction)
                        ??
                        Enumerable.Empty<T>()))
                .ToList();
        }

        public static ICollection<dynamic> GenerateDTO<T>(this IEnumerable<T> BaseObjects, ICollection<string> IncludeProperties = null, ICollection<string> ExcludeProperties = null, string SourceObjectName = null, Func<IRelmModel, Dictionary<string, object>> GetAdditionalObjectProperties = null) where T : IRelmModel
        {
            return BaseObjects.Select(x => x.GenerateDTO(IncludeProperties: IncludeProperties, ExcludeProperties: ExcludeProperties, SourceObjectName: SourceObjectName, GetAdditionalObjectProperties: GetAdditionalObjectProperties)).ToList();
        }

        public static KeyValuePair<TKey, TValue> GetEntry<TKey, TValue>
            (this IDictionary<TKey, TValue> dictionary,
             TKey key)
        {
            return new KeyValuePair<TKey, TValue>(key, dictionary[key]);
        }
    }
}
