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
        public IRelmContext RelmContext { get; } = new ComplexTestContext();

        public string FieldName { get; private set; }
        public string[] KeyFields { get; private set; }

        public TestFieldStringFieldLoader(string fieldName, string[] keyFields)
        {
            FieldName = fieldName;
            KeyFields = keyFields;
        }

        public Dictionary<S[], object> GetFieldData<S>(ICollection<S[]> keyData) where S : notnull
        {
            var testContext = new ComplexTestContext();

            var applications = testContext
                .ComplexTestModels!
                .Where(x => x.Active == true && keyData.Any(y => y.First().ToString() == x.TestFieldString))
                .Load()
                .ToDictionary(x => keyData.First(y => y.All(z => z.ToString() == x.TestFieldString)), x => (object)x);

            return applications;
        }
    }
}
