using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("CoreRelm.Tests")]
// Required by Moq (Castle DynamicProxy) so it can access internal members for creating proxies
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]