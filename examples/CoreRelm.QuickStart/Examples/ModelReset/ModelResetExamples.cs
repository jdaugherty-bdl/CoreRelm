using CoreRelm.Extensions;
using CoreRelm.Interfaces;
using CoreRelm.Quickstart.Contexts;
using CoreRelm.Quickstart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Quickstart.Examples.ModelReset
{
    internal class ModelResetExamples
    {
        internal void RunExamples(ExampleContext exampleContext)
        {
            // Example usage to reset a model core attributes (essentially copies model data to a new model)
            var exampleQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                WHERE {RelmHelper.GetColumnName<ExampleModel>(x => x.ModelIndex)} = 1
                LIMIT 1;";

            var exampleModel = RelmHelper.GetDataObject<ExampleModel>(exampleContext, exampleQuery, throwException: true);

            exampleModel.ResetCoreAttributes();
            exampleModel.ResetCoreAttributes(nullInternalId: true);

            // Example usage to reset a model with data from a DataRow
            exampleQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                WHERE {RelmHelper.GetColumnName<ExampleModel>(x => x.ModelIndex)} = 2
                LIMIT 1;";

            var exampleModelRow = RelmHelper.GetDataRow(exampleContext, exampleQuery, throwException: true);
            
            exampleModel.ResetWithData(exampleModelRow);
        }
    }
}
