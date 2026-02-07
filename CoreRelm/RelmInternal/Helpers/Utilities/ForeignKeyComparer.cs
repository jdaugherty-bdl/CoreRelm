using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Utilities
{
    internal class ForeignKeyComparer
    {
        public static bool Compare(object? a, object? b)
        {
            if (a == null && b == null)
                return true;
            else if (a == null || b == null)
                return false;

            dynamic c = a;
            dynamic d = b;

            return c == d;
        }
    }
}
