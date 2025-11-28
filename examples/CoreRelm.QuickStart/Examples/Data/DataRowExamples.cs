using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.Quickstart.Contexts;
using CoreRelm.Quickstart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Quickstart.Examples.Data
{
    internal class DataRowExamples
    {
        internal void RunExamples(ExampleContext exampleContext)
        {
            // Example usage to get a DataRow using query only
            var queryOnly = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                LIMIT 1;";

            var dataRowOnly = RelmHelper.GetDataRow(exampleContext, queryOnly, throwException: true);
            dataRowOnly = exampleContext.GetDataRow(queryOnly, throwException: true);

            // Example usage to get a DataRow using query and parameters
            var parametersQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                WHERE {RelmHelper.GetColumnName<ExampleModel>(x => x.InternalId)} = @guid_value
                LIMIT 1;";
            
            var exampleParameters = new Dictionary<string, object>
            {
                { "@guid_value", "some-guid-value" }
            };

            var dataRow = RelmHelper.GetDataRow(exampleContext, parametersQuery, parameters: exampleParameters, throwException: true);
            dataRow = exampleContext.GetDataRow(parametersQuery, parameters: exampleParameters, throwException: true);
        }

        internal void RunExamples(ExampleQuickContext exampleQuickContext)
        {
            // Example usage to get a DataRow using query only
            var queryOnly = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                LIMIT 1;";

            var dataRowOnly = RelmHelper.GetDataRow(exampleQuickContext, queryOnly, throwException: true);
            dataRowOnly = exampleQuickContext.GetDataRow(queryOnly, throwException: true);

            // Example usage to get a DataRow using query and parameters
            var parametersQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                WHERE {RelmHelper.GetColumnName<ExampleModel>(x => x.InternalId)} = @guid_value
                LIMIT 1;";
            
            var exampleParameters = new Dictionary<string, object>
            {
                { "@guid_value", "some-guid-value" }
            };

            var dataRow = RelmHelper.GetDataRow(exampleQuickContext, parametersQuery, parameters: exampleParameters, throwException: true);
            dataRow = exampleQuickContext.GetDataRow(parametersQuery, parameters: exampleParameters, throwException: true);
        }
    }
}
