using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Migrations
{
    public interface IRelmSqlScriptRunner
    {
        Task ExecuteScriptAsync(IRelmContext context, string sql, CancellationToken ct = default);
    }
}
