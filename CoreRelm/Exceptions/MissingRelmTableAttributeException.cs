using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Exceptions
{
    public sealed class MissingRelmTableAttributeException(Type t, Type? referencedType = null) : Exception($"Missing required [RelmTable] attribute on principal type {t.FullName}{(referencedType == null ? null : $" referenced by {referencedType!.DeclaringType?.FullName}.{referencedType.Name}")}")
    {
    }
}
