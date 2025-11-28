using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Attributes
{
    /// <summary>
    /// Specifies that the associated property or struct is part of a Data Transfer Object (DTO)  used for communication
    /// between application layers or services.
    /// </summary>
    /// <remarks>This attribute is intended to annotate properties or structs that are part of a DTO, 
    /// providing metadata for tools or frameworks that process or validate DTOs. It can be  applied to properties or
    /// structs to indicate their role in data transfer scenarios.</remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Struct)]
    public sealed class RelmDto : Attribute
    {
    }
}
