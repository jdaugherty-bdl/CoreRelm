using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Resolvers
{
    internal class DefaultRelmResolver(IConfiguration? configuration = null) : DefaultRelmResolver_MySQL(configuration)
    {
    }
}
