using CoreRelm.Extensions;
using CoreRelm.Quickstart.Contexts;
using CoreRelm.Quickstart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Quickstart.Examples.DataLoaders
{
    internal class LoadDataLoaderExamples
    {
        public void RunExamples(ExampleContext exampleContext)
        {
            // Example usage to load a DataLoader
            var model = exampleContext.FirstOrDefault<ExampleModel>(x => x.ModelIndex > 1000);

            var modifiedModel = model.LoadDataLoaderField(exampleContext, x => x.ModificationWithModification);
            modifiedModel = RelmHelper.LoadDataLoaderField(exampleContext, model, x => x.ModificationWithModification);

            var models = exampleContext.FirstOrDefault<ExampleModel>(x => x.ModelIndex > 1000);

            var modifiedModels = model.LoadDataLoaderField(exampleContext, x => x.ModificationWithModification);
            modifiedModels = RelmHelper.LoadDataLoaderField(exampleContext, model, x => x.ModificationWithModification);
        }
    }
}
