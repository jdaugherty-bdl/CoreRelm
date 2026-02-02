using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Exceptions
{
    public sealed class MissingRelmDatabaseAttributeException : Exception
    {
        public MissingRelmDatabaseAttributeException(Type t)
            : base($"Type '{t.FullName}' is missing required [RelmDatabase] attribute.")
        {
        }
    }
}
