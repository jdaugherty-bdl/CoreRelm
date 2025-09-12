using CoreRelm.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Interfaces
{
    public interface IRelmDataLoader_TESTING<T> : IRelmDataLoader<T> where T : IRelmModel, new()
    {
        ICollection<T> PullData(string selectQuery, Dictionary<string, object> findOptions);
        string GetSelectQuery(Dictionary<string, object> FindOptions);
        string GetUpdateQuery(Dictionary<string, object> FindOptions);
    }
}
