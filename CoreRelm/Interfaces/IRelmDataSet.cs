using CoreRelm.Interfaces.Internal;
using CoreRelm.Interfaces.RelmQuick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces
{
    public interface IRelmDataSet<T> : ICollection<T>, IRelmDataSetBase where T : IRelmModel, new()
    {
        IRelmFieldLoader? SetFieldLoader(string fieldName, IRelmFieldLoader dataLoader);
        IRelmQuickFieldLoader? SetFieldLoader(string fieldName, IRelmQuickFieldLoader dataLoader);
        IRelmDataLoader<T> SetDataLoader(IRelmDataLoader<T> dataLoader);
        IRelmDataSet<T> Where(Expression<Func<T, bool>> predicate);
        IRelmDataSet<T> Reference<S>(Expression<Func<T, S>> predicate);
        IRelmDataSet<T> Reference<S>(Expression<Func<T, S>> predicate, Expression<Func<S, object>> additionalConstraints);
        IRelmDataSet<T> Reference<S>(Expression<Func<T, ICollection<S>>> predicate, Expression<Func<S, object>> additionalConstraints);
        IRelmDataSet<T> Reference<S>(Expression<Func<T, S>> predicate, ICollection<Expression<Func<S, object>>> additionalConstraints);
        IRelmDataSet<T> Reference<S>(Expression<Func<T, ICollection<S>>> predicate, ICollection<Expression<Func<S, object>>> additionalConstraints);
        T? Find(int ItemId);
        T? Find(string ItemInternalId);
        T? FirstOrDefault();
        T? FirstOrDefault(bool LoadItems);
        T? FirstOrDefault(Expression<Func<T, bool>> predicate);
        T? FirstOrDefault(Expression<Func<T, bool>> predicate, bool LoadItems);
        ICollection<T> Load();
        ICollection<T> Load(bool loadDataLoaders);
        IRelmDataSet<T> LoadAsDataSet();
        int Write();
        IRelmDataSet<T> Entry(T Item);
        IRelmDataSet<T> Entry(T Item, bool Persist = true);
        IRelmDataSet<T> OrderBy(Expression<Func<T, object>> predicate);
        IRelmDataSet<T> OrderByDescending(Expression<Func<T, object>> predicate);
        IRelmDataSet<T> Set(Expression<Func<T, T>> predicate);
        IRelmDataSet<T> GroupBy(Expression<Func<T, object>> predicate);
        IRelmDataSet<T> Limit(int LimitCount);
        IRelmDataSet<T> DistinctBy(Expression<Func<T, object>> predicate);
        int Save(T Item);
        int Save();
        T New();
        T New(dynamic NewObjectParameters, bool Persist = true);
        new int Add(T item);
        int Add(T item, bool Persist);
        int Add(ICollection<T> items);
        int Add(ICollection<T> items, bool Persist);
    }
}
