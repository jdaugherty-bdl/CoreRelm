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
            // Arrange
            var file = new ModelSetsFile 
            { 
                Version = 1,
                Sets = new Dictionary<string, ModelSetDefinition>
                {
                    ["validSet"] = new ModelSetDefinition 
                    { 
                        Types = new List<string> { "CoreRelm.Tests.Migrations.ModelSetResolverTests" },
                        NamespacePrefixes = new List<string> { "CoreRelm.Tests.Migrations" }
                    } 
                }
            };

            // Act
            var resolver = new ModelSetResolver(Assembly.GetExecutingAssembly());

            // Assert
            Assert.Throws<ModelSetNotFoundException>(() => resolver.ResolveSet(file, "nope"));
        }

        [Fact]
        public void Resolve_SetResolution_Throws()
        {
            // Arrange
            var file = new ModelSetsFile 
            { 
                Version = 1 
            };

            // Act
            var resolver = new ModelSetResolver(Assembly.GetExecutingAssembly());
            
            // Assert
            Assert.Throws<ModelSetResolutionException>(() => resolver.ResolveSet(file, "nope"));
        }
    }
}
