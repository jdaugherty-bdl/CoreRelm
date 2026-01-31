using CoreRelm.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.TestModels.DataLoaderModels
{
    public class TestFieldBooleansFieldLoader : IRelmFieldLoader
    {
        private readonly string? _fieldName;
        public string? FieldName => _fieldName;
        private readonly string[]? _keyFields;
        public string[]? KeyFields => _keyFields;

        public IRelmContext RelmContext { get; }

        public TestFieldBooleansFieldLoader(ComplexTestContext relmContext, string fieldName, string[]? keyFields = null)
        {
            RelmContext = relmContext;
            _fieldName = fieldName;
            _keyFields = keyFields;
        }

        public virtual Dictionary<S[], object> GetFieldData<S>(ICollection<S[]> keyData) where S : notnull
        {
            return keyData
                .Select((x, i) => new { Key = x, Value = i })
                .ToDictionary(x => x.Key, x => (object)Enumerable.Range(0, 4).Select(y => y % 2 == 0));
        }
    }
}
