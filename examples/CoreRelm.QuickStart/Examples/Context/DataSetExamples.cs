using CoreRelm.Extensions;
using CoreRelm.Interfaces;
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
    internal class DataSetExamples
    {
        internal void RunExamples(ExampleContext exampleContext)
        {
            // Example usage to get a pre-initialized DataSet by type for a model
            var modelsDataSetType = exampleContext.GetDataSetType<ExampleModel>();

            // Example usage to create a new blank model with minimal identifiable information and persist it
            var blankModel = exampleContext
                .ExampleModels
                .New(Persist: true);

            blankModel.ModelName = "New Model";

            // Example usage to save changes to an existing model
            exampleContext
                .ExampleModels
                .Save(blankModel);

            // Example usage to create a new model with specified properties and persist it
            var newModel = exampleContext
                .ExampleModels
                .New(new ExampleModel
                {
                    ModelName = "New Model",
                }, Persist: true);

            // Example usage to find a model by ID
            var model = exampleContext
                .ExampleModels
                .Find(ItemId: 1);

            // Example usage to find a model by InternalId
            model = exampleContext
                .ExampleModels
                .Find(ItemInternalId: "some-guid-value");

            // Example usage to find a model by ModelIndex
            model = exampleContext
                .ExampleModels
                .FirstOrDefault(x => x.ModelIndex == 12345);

            // Example usage to load all models
            var models = exampleContext
                .ExampleModels
                .Load();

            // Example usage to load all models without loading data loaders
            models = exampleContext
                .ExampleModels
                .Load(loadDataLoaders: false);

            // Example usage to load all models with a model index greater than 1000
            models = exampleContext
                .ExampleModels
                .Where(x => x.ModelIndex > 1000)
                .Load();

            // Example usage to load all models with a model index greater than 1000, ordered by model index
            models = exampleContext
                .ExampleModels
                .Where(x => x.ModelIndex > 1000)
                .OrderBy(x => x.ModelIndex)
                .Load();

            // Example usage to load all models with a model index greater than 1000, ordered by model index descending
            models = exampleContext
                .ExampleModels
                .Where(x => x.ModelIndex > 1000)
                .OrderByDescending(x => x.ModelIndex)
                .Load();

            // Example usage to load all models with a model index greater than 1000, grouped by group internal ID and ordered by model index
            models = exampleContext
                .ExampleModels
                .Where(x => x.ModelIndex > 1000)
                .GroupBy(x => x.GroupInternalId)
                .OrderBy(x => x.ModelIndex)
                .Load();

            // Example usage to load 100 models with a model index greater than 1000, grouped by group internal ID and ordered by model index
            models = exampleContext
                .ExampleModels
                .Where(x => x.ModelIndex > 1000)
                .GroupBy(x => x.GroupInternalId)
                .OrderBy(x => x.ModelIndex)
                .Limit(100)
                .Load();

            // Example usage to load 100 distinct models by model name with a model index greater than 1000, grouped by group internal ID and ordered by model index
            models = exampleContext
                .ExampleModels
                .Where(x => x.ModelIndex > 1000)
                .DistinctBy(x => x.ModelName)
                .GroupBy(x => x.GroupInternalId)
                .OrderBy(x => x.ModelIndex)
                .Limit(100)
                .Load();

            // Example usage to load all models with their related groups
            models = exampleContext
                .ExampleModels
                .Reference(x => x.Group)
                .Load();

            // Example usage to load all models with their related active groups
            models = exampleContext
                .ExampleModels
                .Reference(x => x.Group, x => x.Active == true)
                .Load();

            newModel = new ExampleModel
            {
                GroupInternalId = "example-group-internal-id",
                ModelName = "New Model",
                ModelIndex = 9999,
                IsBoolColumn = true
            };

            // Example usage to add a single new model and persist it
            var itemsAdded = exampleContext
                .ExampleModels
                .Add(newModel, Persist: true);

            var newModels = new List<ExampleModel>
            {
                new ExampleModel
                {
                    GroupInternalId = "example-group-internal-id",
                    ModelName = "New Model 1",
                    ModelIndex = 10001,
                    IsBoolColumn = false
                },
                new ExampleModel
                {
                    GroupInternalId = "example-group-internal-id",
                    ModelName = "New Model 2",
                    ModelIndex = 10002,
                    IsBoolColumn = true
                }
            };

            // Example usage to add multiple new models and persist them
            itemsAdded = exampleContext
                .ExampleModels
                .Add(newModels, Persist: true);

            foreach (var updateModel in newModels)
            {
                updateModel.ModelIndex += 1;
            }

            // Example usage to save all models in the DataSet
            exampleContext
                .ExampleModels
                .Save();
        }

        internal void RunExamples(ExampleQuickContext exampleQuickContext)
        {
            // Example usage to get an uninitialized DataSet by type for a model
            var modelsDataSetType = exampleQuickContext.GetDataSetType<ExampleModel>();

            // Example usage to get a pre-initialized DataSet by type for a model
            var modelsDataSet = exampleQuickContext.GetDataSet<ExampleModel>();

            // Example usage to filter the DataSet for models with a model index greater than 1000
            var filteredModelsDataSet = exampleQuickContext.Where<ExampleModel>(x => x.ModelIndex > 1000);

            // Example usage to create a new blank model with minimal identifiable information and persist it
            var blankModel = exampleQuickContext
                .ExampleModels
                .New(Persist: true);

            blankModel.ModelName = "New Model";

            // Example usage to save changes to an existing model
            exampleQuickContext
                .ExampleModels
                .Save(blankModel);

            // Example usage to create a new model with specified properties and persist it
            var newModel = exampleQuickContext
                .ExampleModels
                .New(new ExampleModel
                {
                    ModelName = "New Model",
                }, Persist: true);

            // Example usage to find a model by ID
            var model = exampleQuickContext
                .ExampleModels
                .Find(ItemId: 1);

            // Example usage to find a model by InternalId
            model = exampleQuickContext
                .ExampleModels
                .Find(ItemInternalId: "some-guid-value");

            // Example usage to find a model by ModelIndex
            model = exampleQuickContext
                .ExampleModels
                .FirstOrDefault(x => x.ModelIndex == 12345);

            // Example usage to load all models
            var models = exampleQuickContext
                .ExampleModels
                .Load();

            // Example usage to load all models without loading data loaders
            models = exampleQuickContext
                .ExampleModels
                .Load(loadDataLoaders: false);

            // Example usage to load all models with a model index greater than 1000
            models = exampleQuickContext
                .ExampleModels
                .Where(x => x.ModelIndex > 1000)
                .Load();

            // Example usage to load all models with a model index greater than 1000, ordered by model index
            models = exampleQuickContext
                .ExampleModels
                .Where(x => x.ModelIndex > 1000)
                .OrderBy(x => x.ModelIndex)
                .Load();

            // Example usage to load all models with a model index greater than 1000, ordered by model index descending
            models = exampleQuickContext
                .ExampleModels
                .Where(x => x.ModelIndex > 1000)
                .OrderByDescending(x => x.ModelIndex)
                .Load();

            // Example usage to load all models with a model index greater than 1000, grouped by group internal ID and ordered by model index
            models = exampleQuickContext
                .ExampleModels
                .Where(x => x.ModelIndex > 1000)
                .GroupBy(x => x.GroupInternalId)
                .OrderBy(x => x.ModelIndex)
                .Load();

            // Example usage to load 100 models with a model index greater than 1000, grouped by group internal ID and ordered by model index
            models = exampleQuickContext
                .ExampleModels
                .Where(x => x.ModelIndex > 1000)
                .GroupBy(x => x.GroupInternalId)
                .OrderBy(x => x.ModelIndex)
                .Limit(100)
                .Load();

            // Example usage to load 100 distinct models by model name with a model index greater than 1000, grouped by group internal ID and ordered by model index
            models = exampleQuickContext
                .ExampleModels
                .Where(x => x.ModelIndex > 1000)
                .DistinctBy(x => x.ModelName)
                .GroupBy(x => x.GroupInternalId)
                .OrderBy(x => x.ModelIndex)
                .Limit(100)
                .Load();

            // Example usage to load all models with their related groups
            models = exampleQuickContext
                .ExampleModels
                .Reference(x => x.Group)
                .Load();

            // Example usage to load all models with their related active groups
            models = exampleQuickContext
                .ExampleModels
                .Reference(x => x.Group, x => x.Active == true)
                .Load();

            newModel = new ExampleModel
            {
                GroupInternalId = "example-group-internal-id",
                ModelName = "New Model",
                ModelIndex = 9999,
                IsBoolColumn = true
            };

            // Example usage to add a single new model and persist it
            var itemsAdded = exampleQuickContext
                .ExampleModels
                .Add(newModel, Persist: true);

            var newModels = new List<ExampleModel>
            {
                new ExampleModel
                {
                    GroupInternalId = "example-group-internal-id",
                    ModelName = "New Model 1",
                    ModelIndex = 10001,
                    IsBoolColumn = false
                },
                new ExampleModel
                {
                    GroupInternalId = "example-group-internal-id",
                    ModelName = "New Model 2",
                    ModelIndex = 10002,
                    IsBoolColumn = true
                }
            };

            // Example usage to add multiple new models and persist them
            itemsAdded = exampleQuickContext
                .ExampleModels
                .Add(newModels, Persist: true);

            foreach (var updateModel in newModels)
            {
                updateModel.ModelIndex += 1;
            }

            // Example usage to save all models in the DataSet
            exampleQuickContext
                .ExampleModels
                .Save();
        }
    }
}
