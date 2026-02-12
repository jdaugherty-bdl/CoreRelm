using CoreRelm.Extensions;
using CoreRelm.Interfaces;
using CoreRelm.Quickstart.Contexts;
using CoreRelm.Quickstart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Quickstart.Examples.DataTransferObjects
{
    internal class DTOExamples
    {
        internal void RunExamples(ExampleContext exampleContext)
        {
            // Example usage to create a DTO from a single object
            var exampleQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                LIMIT 1;";

            var exampleModel = RelmHelper.GetDataObject<ExampleModel>(exampleContext, exampleQuery, throwException: true);

            var modelDTO = exampleModel.GenerateDTO();
            modelDTO = exampleModel.GenerateDTO(includeProperties: new[] { "Group" });
            modelDTO = exampleModel.GenerateDTO(includeProperties: new[] { "Group.ExampleModels" }); // be careful with circular references

            // Example usage to create a DTO from a list of objects
            var exampleListQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                LIMIT 5;";

            var exampleModelList = RelmHelper.GetDataObjects<ExampleModel>(exampleContext, exampleListQuery, throwException: true)
                .ToList();

            modelDTO = exampleModelList.GenerateDTO();
            modelDTO = exampleModelList.GenerateDTO(includeProperties: new[] { "Group" });
            modelDTO = exampleModelList.GenerateDTO(includeProperties: new[] { "Group.ExampleModels" }); // be careful with circular references
        }
    }
}
