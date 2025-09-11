using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Resolvers
{
    public interface IRelmResolverBase
    {
        DbConnectionStringBuilder GetConnectionBuilder(Enum ConnectionType);
        DbConnectionStringBuilder GetConnectionBuilder(string ConnectionString);
    }
}
