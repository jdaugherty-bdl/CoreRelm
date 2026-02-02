using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations
{

    public sealed class ModelSetDefinition
    {
        [JsonPropertyName("types")]
        public List<string> Types { get; set; } = [];

        [JsonPropertyName("namespacePrefixes")]
        public List<string> NamespacePrefixes { get; set; } = [];
    }
}
