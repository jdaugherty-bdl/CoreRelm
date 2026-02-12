using CoreRelm.Exceptions;
using CoreRelm.Migrations;
using CoreRelm.Models.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Migrations
{
    public class ModelSetResolverTests
    {
        [Fact]
        public void Resolve_MissingSet_Throws()
        {
            var file = new ModelSetsFile { Version = 1 };
            var resolver = new ModelSetResolver(Assembly.GetExecutingAssembly());
            Assert.Throws<ModelSetNotFoundException>(() => resolver.ResolveSet(file, "nope"));
        }
    }
}
