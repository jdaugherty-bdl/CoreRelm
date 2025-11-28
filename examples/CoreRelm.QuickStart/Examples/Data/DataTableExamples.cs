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
    internal class DataTableExamples
    {
        internal void RunExamples(ExampleContext exampleContext)
        {
            // Example usage to get a DataTable using query only
            var queryOnly = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()};";

            var dataTable = RelmHelper.GetDataTable(exampleContext, queryOnly, throwException: true);
            dataTable = exampleContext.GetDataTable(queryOnly, throwException: true);

            // Example usage to get a DataTable using query and parameters
            var parametersQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                WHERE {RelmHelper.GetColumnName<ExampleModel>(x => x.ModelIndex)} = @some_value;";
            
            var exampleParameters = new Dictionary<string, object>
            {
                { "@some_value", 12345 }
            };

            dataTable = RelmHelper.GetDataTable(exampleContext, parametersQuery, parameters: exampleParameters, throwException: true);
            dataTable = exampleContext.GetDataTable(parametersQuery, parameters: exampleParameters, throwException: true);
        }

        internal void RunExamples(ExampleQuickContext exampleQuickContext)
        {
            // Example usage to get a DataTable using query only
            var queryOnly = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()};";

            var dataTable = RelmHelper.GetDataTable(exampleQuickContext, queryOnly, throwException: true);
            dataTable = exampleQuickContext.GetDataTable(queryOnly, throwException: true);

            // Example usage to get a DataTable using query and parameters
            var parametersQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                WHERE {RelmHelper.GetColumnName<ExampleModel>(x => x.ModelIndex)} = @some_value;";
            
            var exampleParameters = new Dictionary<string, object>
            {
                { "@some_value", 12345 }
            };

            dataTable = RelmHelper.GetDataTable(exampleQuickContext, parametersQuery, parameters: exampleParameters, throwException: true);
            dataTable = exampleQuickContext.GetDataTable(parametersQuery, parameters: exampleParameters, throwException: true);
        }
    }
}
