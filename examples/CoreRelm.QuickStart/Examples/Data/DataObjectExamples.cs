using CoreRelm.Extensions;
using CoreRelm.Interfaces;
using CoreRelm.Quickstart.Contexts;
using CoreRelm.Quickstart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Quickstart.Examples.Data
{
    internal class DataObjectExamples
    {
        internal void RunExamples(ExampleContext exampleContext)
        {
            // Example usage to get a DataObject using query only
            var queryOnly = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()}
                LIMIT 1;";

            var dataObjectOnly = RelmHelper.GetDataObject<ExampleModel>(exampleContext, queryOnly, throwException: true);
            dataObjectOnly = exampleContext.GetDataObject<ExampleModel>(queryOnly, throwException: true);

            // Example usage to get a DataObject using query and parameters
            var parametersQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                WHERE {RelmHelper.GetColumnName<ExampleModel>(x => x.InternalId)} = @guid_value
                LIMIT 1;";
            
            var exampleParameters = new Dictionary<string, object>
            {
                { "@guid_value", "some-guid-value" }
            };

            var dataObject = RelmHelper.GetDataObject<ExampleModel>(exampleContext, parametersQuery, parameters: exampleParameters, throwException: true);
            dataObject = exampleContext.GetDataObject<ExampleModel>(parametersQuery, parameters: exampleParameters, throwException: true);

            // Example usage to get multiple DataObjects using query only
            var multipleQueryOnly = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()}
                LIMIT 10;";
            var dataObjectsOnly = RelmHelper.GetDataObjects<ExampleModel>(exampleContext, multipleQueryOnly, throwException: true);
            dataObjectsOnly = exampleContext.GetDataObjects<ExampleModel>(multipleQueryOnly, throwException: true);

            // Example usage to get multiple DataObjects using query and parameters
            var multipleParametersQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                WHERE {RelmHelper.GetColumnName<ExampleModel>(x => x.ModelIndex)} = @some_value
                LIMIT 10;";
            
            var multipleExampleParameters = new Dictionary<string, object>
            {
                { "@some_value", 12345 }
            };

            var dataObjects = RelmHelper.GetDataObjects<ExampleModel>(exampleContext, multipleParametersQuery, parameters: multipleExampleParameters, throwException: true);
            dataObjects = exampleContext.GetDataObjects<ExampleModel>(multipleParametersQuery, parameters: multipleExampleParameters, throwException: true);
        }
    }
}
