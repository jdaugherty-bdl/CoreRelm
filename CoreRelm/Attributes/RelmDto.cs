using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Struct)]
    public sealed class RelmDto : Attribute
    {
    }
}
