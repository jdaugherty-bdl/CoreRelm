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

namespace CoreRelm.Quickstart.Examples.BulkWriter
{
    internal class WriteToDatabaseExamples
    {
        internal void RunExamples(ExampleContext exampleContext)
        {
            // Example usage to write a single object to the database
            var exampleQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                LIMIT 1;";

            var exampleModel = RelmHelper.GetDataObject<ExampleModel>(exampleContext, exampleQuery, throwException: true);

            exampleModel.ModelIndex += 1000; // Modify the object to see a change in the database

            RelmHelper.WriteToDatabase(exampleContext, exampleModel);
            exampleModel.WriteToDatabase(exampleContext);
            exampleContext.WriteToDatabase(exampleModel);

            // Example usage to write a list of objects to the database
            var exampleListQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                LIMIT 5;";
            
            var exampleModelList = RelmHelper.GetDataObjects<ExampleModel>(exampleContext, exampleListQuery, throwException: true);

            foreach (var example in exampleModelList)
            {
                example.ModelIndex += 1000; // Modify the objects to see a change in the database
            }

            RelmHelper.WriteToDatabase(exampleContext, exampleModelList);
            exampleModelList.WriteToDatabase(exampleContext);
            exampleContext.WriteToDatabase(exampleModelList);
        }

        internal void RunExamples(ExampleQuickContext exampleQuickContext)
        {
            // Example usage to write a single object to the database
            var exampleQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                LIMIT 1;";

            var exampleModel = RelmHelper.GetDataObject<ExampleModel>(exampleQuickContext, exampleQuery, throwException: true);

            exampleModel.ModelIndex += 1000; // Modify the object to see a change in the database

            RelmHelper.WriteToDatabase(exampleQuickContext, exampleModel);
            exampleModel.WriteToDatabase(exampleQuickContext);
            exampleQuickContext.WriteToDatabase(exampleModel);
            
            // Example usage to write a list of objects to the database
            var exampleListQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                LIMIT 5;";
            
            var exampleModelList = RelmHelper.GetDataObjects<ExampleModel>(exampleQuickContext, exampleListQuery, throwException: true);

            foreach (var example in exampleModelList)
            {
                example.ModelIndex += 1000; // Modify the objects to see a change in the database
            }

            RelmHelper.WriteToDatabase(exampleQuickContext, exampleModelList);
            exampleModelList.WriteToDatabase(exampleQuickContext);
            exampleQuickContext.WriteToDatabase(exampleModelList);
        }
    }
}
