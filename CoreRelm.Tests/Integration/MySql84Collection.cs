using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Integration
{

    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class MySql84Collection : ICollectionFixture<MySql84Fixture>
    {
        public const string Name = "MySql84";
    }
}
