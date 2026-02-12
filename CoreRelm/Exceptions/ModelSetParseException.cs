using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Exceptions
{
    public sealed class ModelSetParseException(string message, Exception? inner = null) : Exception(message, inner)
    {
    }
}
