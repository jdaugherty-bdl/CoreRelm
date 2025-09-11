using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.RelmQuick
{
    public interface IRelmQuickFieldLoader : IRelmFieldLoaderBase
    {
        IRelmQuickContext RelmContext { get; }
    }
}
