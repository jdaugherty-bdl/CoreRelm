using CoreRelm.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Interfaces
{
    public interface IRelmDataSet_TESTING<T> : IRelmDataSet<T> where T : IRelmModel, new()
    {
        ICollection<T> GetLoadData();
        IRelmDataLoader<T> GetDataLoader();
    }
}
