using CoreRelm.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.TestModels.DataLoaderModels
{
    internal class TestFieldStringFieldLoader : IRelmFieldLoader
    {
        public string FieldName { get; private set; }
        public string[] KeyFields { get; private set; }

        public IRelmContext RelmContext { get; }

        public TestFieldStringFieldLoader(ComplexTestContext relmContext, string fieldName, string[] keyFields)
        {
            RelmContext = relmContext;
            FieldName = fieldName;
            KeyFields = keyFields;
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
            {
                return null;
            }

            var testContext = (ComplexTestContext)RelmContext;

            var applications = testContext
                .ComplexTestModels
                ?.ToList()
                .Where(x => x.Active == true && keyData.Any(y => y.First().ToString() == x.TestFieldString))
                .ToDictionary(x => keyData.First(y => y.All(z => z.ToString() == x.TestFieldString)), x => (object)x)
                ?? [];

            return applications;
        }
    }
}
