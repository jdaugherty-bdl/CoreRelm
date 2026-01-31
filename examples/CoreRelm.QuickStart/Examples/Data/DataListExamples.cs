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
    internal class DataListExamples
    {
        internal void RunExamples(ExampleContext exampleContext)
        {
            // Example usage to get a DataList using query only
            var queryOnly = $@"SELECT ID FROM {RelmHelper.GetDalTable<ExampleModel>()};";
            
            var dataList = RelmHelper.GetDataList<int>(exampleContext, queryOnly, throwException: true);
            dataList = exampleContext.GetDataList<int>(queryOnly, throwException: true);
            
            // Example usage to get a DataList using query and parameters
            var parametersQuery = $@"SELECT ID FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                WHERE {RelmHelper.GetColumnName<ExampleModel>(x => x.ModelIndex)} = @some_value;";
            
            var exampleParameters = new Dictionary<string, object>
            {
                { "@some_value", 12345 }
            };

            dataList = RelmHelper.GetDataList<int>(exampleContext, parametersQuery, parameters: exampleParameters, throwException: true);
            dataList = exampleContext.GetDataList<int>(parametersQuery, parameters: exampleParameters, throwException: true);
        }

        internal void RunExamples(ExampleQuickContext exampleQuickContext)
        {
            // Example usage to get a DataList using query only
            var queryOnly = $@"SELECT ID FROM {RelmHelper.GetDalTable<ExampleModel>()};";
            
            var dataList = RelmHelper.GetDataList<int>(exampleQuickContext, queryOnly, throwException: true);
            dataList = exampleQuickContext.GetDataList<int>(queryOnly, throwException: true);
            
            // Example usage to get a DataList using query and parameters
            var parametersQuery = $@"SELECT ID FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                WHERE {RelmHelper.GetColumnName<ExampleModel>(x => x.ModelIndex)} = @some_value;";
            
            var exampleParameters = new Dictionary<string, object>
            {
                { "@some_value", 12345 }
            };

            dataList = RelmHelper.GetDataList<int>(exampleQuickContext, parametersQuery, parameters: exampleParameters, throwException: true);
            dataList = exampleQuickContext.GetDataList<int>(parametersQuery, parameters: exampleParameters, throwException: true);
        }
    }
}
