using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Exceptions
{
    public sealed class MissingRelmTableAttributeException : Exception
    {
        public MissingRelmTableAttributeException(Type t)
            : base($"Type '{t.FullName}' is missing required [RelmTable] attribute.")
        {
        }
    }
}
