using CoreRelm.Interfaces.Internal;
using CoreRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.RelmQuick
{
    public interface IRelmQuickContext : IDisposable
    {
        RelmContextOptionsBuilder ContextOptions { get; }

        void StartConnection(bool autoOpenTransaction = false);
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
        ICollection<T> Get<T>() where T : IRelmModel, new();
        ICollection<T> Get<T>(Expression<Func<T, bool>> predicate) where T : IRelmModel, new();
    }
}
