using CoreRelm.Interfaces;
using CoreRelm.Quickstart.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Quickstart.FieldLoaders
{
    internal class IsModificationFieldLoader : IRelmFieldLoader
    {
        public string FieldName { get; private set; }
        public string[] KeyFields { get; private set; }
        public IRelmContext RelmContext { get; private set; }

        private ExampleContext? _exampleContext => RelmContext as ExampleContext;

        public IsModificationFieldLoader(IRelmContext relmContext, string fieldName, string[] keyFields)
        {
            FieldName = fieldName;
            KeyFields = keyFields;
            RelmContext = relmContext ?? throw new ArgumentNullException(nameof(relmContext), "RelmContext cannot be null.");

            if (_exampleContext == null)
                RelmContext = new ExampleContext(relmContext.ContextOptions);
        }

        public Dictionary<S[], object>? GetFieldData<S>(ICollection<S[]>? keyData) where S : notnull
        {
            return GetFieldDataAsync(keyData)
                .GetAwaiter()
                .GetResult();
        }

        public async Task<Dictionary<S[], object>?> GetFieldDataAsync<S>(ICollection<S[]>? keyData, CancellationToken cancellationToken = default) where S : notnull
        {
            if (keyData == null || keyData.Count == 0)
                return null;

            var sourceInternalIds = keyData.Select(x => x.Select(y => y.ToString()).ToArray()).ToList();
            if ((sourceInternalIds?.Count ?? 0) <= 0)
                return null;

            var data = keyData
                .ToDictionary(x => x, x => (object)(_exampleContext
                    ?.ExampleModels
                    .Where(y => sourceInternalIds!.Any(z => z.Contains(y.SuperceededByInternalId)) && y.Active == true)
                    .Load()
                    ?.Count > 0));

            return data;
        }
    }
}
