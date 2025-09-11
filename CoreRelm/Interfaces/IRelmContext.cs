using CoreRelm.Interfaces.Internal;
using CoreRelm.Models;
using CoreRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces
{
    public interface IRelmContext : IDisposable, IAsyncDisposable
    {
        RelmContextOptionsBuilder ContextOptions { get; }

        void SetDataLoader<T>(IRelmDataLoader<T> dataLoader) where T : RelmModel, new();

        IRelmDataSet<T> GetDataSetType<T>() where T : IRelmModel, new();
        IRelmDataSet<T> GetDataSetType<T>(bool throwException) where T : IRelmModel, new();
        IRelmDataSetBase GetDataSetType(Type dataSetType);
        IRelmDataSetBase GetDataSetType(Type dataSetType, bool throwException);

        void CommitTransaction();
    }
}
