using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Exceptions
{
    public sealed class UnsupportedModelSetsVersionException(int version) : Exception($"Unsupported modelsets.json version: {version}. Supported versions: 1.")
    {
    }
}
