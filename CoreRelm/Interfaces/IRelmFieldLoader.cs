using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces
{
    public interface IRelmFieldLoader : IRelmFieldLoaderBase
    {
        IRelmContext RelmContext { get; }
    }
}
