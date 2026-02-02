using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations
{
    // TODO: remove when CoreRelm implements migrations properly.
    public sealed class ModelSetsFile
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("sets")]
        public Dictionary<string, ModelSetDefinition> Sets { get; set; } = [];
    }
}
