using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces
{
    /// <summary>
    /// Defines a contract for loading field data within a Relm context.
    /// </summary>
    public interface IRelmFieldLoader : IRelmFieldLoaderBase
    {
        /// <summary>
        /// Gets the current Relm context associated with this instance.
        /// </summary>
        IRelmContext RelmContext { get; }
    }
}
