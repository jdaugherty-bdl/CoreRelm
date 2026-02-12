using CoreRelm.Exceptions;
using CoreRelm.Interfaces.ModelSets;
using CoreRelm.Models.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoreRelm.Migrations
{
    public sealed class ModelSetParser : IModelSetParser
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public ModelSetsFile Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ModelSetParseException("modelsets.json content is empty.");

            try
            {
                var file = JsonSerializer.Deserialize<ModelSetsFile>(json, Options) 
                    ?? throw new ModelSetParseException("Failed to parse modelsets.json (deserialized null).");

                if (file.Version != 1)
                    throw new UnsupportedModelSetsVersionException(file.Version);

                file.Sets ??= new(StringComparer.Ordinal);
                return file;
            }
            catch (UnsupportedModelSetsVersionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ModelSetParseException("Failed to parse modelsets.json.", ex);
            }
        }
    }
}
