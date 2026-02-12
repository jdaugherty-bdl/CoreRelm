using CoreRelm.Quickstart.Contexts;
using CoreRelm.Quickstart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Quickstart.Examples.Context
{
    internal class ContextDataExamples
    {
        internal void RunExamples(ExampleContext exampleContext)
        {
            // Example usage to get all ExampleModels
            var filteredModels = exampleContext.Get<ExampleModel>();

            // Example usage to find all models by ModelIndex
            filteredModels = exampleContext.Get<ExampleModel>(x => x.ModelIndex > 1000);

            // Example usage to find a model by ModelIndex
            var model = exampleContext.FirstOrDefault<ExampleModel>(x => x.ModelIndex > 1000);

            // Example usage to get multiple DataObjects using query only
            var query = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()};";

            filteredModels = exampleContext.Run<ExampleModel>(query);

            // Example usage to get multiple DataObjects using query and parameters
            query = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()}
                WHERE {RelmHelper.GetColumnName<ExampleModel>(x => x.ModelIndex)} = @some_value;";

            var exampleParameters = new Dictionary<string, object>
            {
                { "@some_value", 12345 }
            };

            filteredModels = exampleContext.Run<ExampleModel>(query, exampleParameters);
        }
    }
}
