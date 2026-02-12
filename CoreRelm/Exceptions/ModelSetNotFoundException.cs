using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Exceptions
{
    public sealed class ModelSetNotFoundException(string setName) : Exception($"Model set '{setName}' not found.")
    {
    }
}
