using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Utilities
{
    internal class ForeignKeyComparer
    {
        public static bool Compare(object a, object b)
        {
            dynamic c = a;
            dynamic d = b;

            return c == d;
        }
    }
}
