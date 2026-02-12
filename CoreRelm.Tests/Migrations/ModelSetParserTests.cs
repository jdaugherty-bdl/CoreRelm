using CoreRelm.Exceptions;
using CoreRelm.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Migrations
{
    public class ModelSetParserTests
    {
        [Fact]
        public void Parse_ValidJson_Works()
        {
            var json = @"{
          ""version"": 1,
          ""sets"": { ""core"": { ""types"": [], ""namespacePrefixes"": [""X""] } }
        }";

            var parser = new ModelSetParser();
            var file = parser.Parse(json);

            Assert.Equal(1, file.Version);
            Assert.True(file.Sets.ContainsKey("core"));
        }

        [Fact]
        public void Parse_UnsupportedVersion_Throws()
        {
            var json = @"{ ""version"": 2, ""sets"": {} }";
            var parser = new ModelSetParser();
            Assert.Throws<UnsupportedModelSetsVersionException>(() => parser.Parse(json));
        }
    }
}
