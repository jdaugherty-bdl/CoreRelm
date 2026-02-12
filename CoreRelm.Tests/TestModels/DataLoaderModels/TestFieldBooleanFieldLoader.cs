using CoreRelm.Interfaces;
using CoreRelm.RelmInternal.Helpers.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.TestModels.DataLoaderModels
{
    public class TestFieldBooleanFieldLoader : IRelmFieldLoader
    {
        private readonly string? _fieldName;
        public string? FieldName => _fieldName;
        private readonly string[]? _keyFields;
        public string[]? KeyFields => _keyFields;

        public IRelmContext RelmContext { get; }

        public TestFieldBooleanFieldLoader(ComplexTestContext relmContext, string fieldName, string[]? keyFields = null)
        {
            RelmContext = relmContext;
            _fieldName = fieldName;
            _keyFields = keyFields;
        }

        public Dictionary<S[], object>? GetFieldData<S>(ICollection<S[]>? keyData) where S : notnull
        {
            return GetFieldDataAsync(keyData)
                .GetAwaiter()
                .GetResult();
        }

        public async Task<Dictionary<S[], object>?> GetFieldDataAsync<S>(ICollection<S[]>? keyData, CancellationToken cancellationToken = default) where S : notnull
        {
            return keyData
                ?.Select((x, i) => new { Key = x, Value = i })
                .ToDictionary(x => x.Key, x => (object)(x.Value % 2 == 0));
        }
    }
}
