using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.Quickstart.Contexts;
using CoreRelm.Quickstart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Quickstart.Examples.Context
{
    internal class ContextDataSetExamples
    {
        internal void RunExamples(ExampleContext exampleContext)
        {
            // Example usage to check if a DataSet exists
            var hasModelsDataSet = exampleContext.HasDataSet<ExampleModel>();

            // Example usage to get a DataSet type
            var modelsDataSetType = exampleContext.GetDataSet<ExampleModel>();

            // Example usage to get a DataSet
            var modelsDataSet = exampleContext.GetDataSet<ExampleModel>();
        }

        internal void RunExamples(ExampleQuickContext exampleQuickContext)
        {
            // Example usage to check if a DataSet exists
            var hasModelsDataSet = exampleQuickContext.HasDataSet<ExampleModel>();

            // Example usage to get a DataSet
            var modelsDataSetType = exampleQuickContext.GetDataSet<ExampleModel>();

            // Example usage to get a DataSet
            var modelsDataSet = exampleQuickContext.GetDataSet<ExampleModel>();
        }
    }
}
