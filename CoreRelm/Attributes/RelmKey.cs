using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Attributes
{
    /// <summary>
    /// Specifies that the associated property or struct is a key used for identifying or referencing entities.
    /// </summary>
    /// <remarks>This attribute can be applied to properties or structs to indicate their role as a key in a
    /// relational or entity-based context. It is primarily used for metadata purposes and does not enforce any behavior
    /// at runtime.</remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Struct)]
    public sealed class RelmKey : Attribute
    {
    }
}
