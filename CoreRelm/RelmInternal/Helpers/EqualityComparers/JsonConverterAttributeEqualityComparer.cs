using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.EqualityComparers
{
    internal class JsonConverterAttributeEqualityComparer : IEqualityComparer<JsonConverterAttribute>
    {
        public bool Equals(JsonConverterAttribute x, JsonConverterAttribute y)
        {
            if (x == null && y == null)
                return true;
            else if (x == null || y == null)
                return false;
            else if (x.GetType() != y.GetType())
                return false;

            return x.ConverterType == y.ConverterType;
        }

        public int GetHashCode(JsonConverterAttribute obj)
        {
            return obj.GetHashCode();
        }
    }
}
