using CoreRelm.Extensions;
using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.Models;
using CoreRelm.Quickstart.Contexts;
using CoreRelm.Quickstart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Quickstart.Examples.ForeignKeys
{
    internal class LoadForeignKeyExamples
    {
        internal void RunExamples(ExampleContext exampleContext)
        {
            // Example usage to load a foreign key of a single object
            var exampleQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                LIMIT 1;";

            var exampleModel = RelmHelper.GetDataObject<ExampleModel>(exampleContext, exampleQuery, throwException: true);

            exampleModel.LoadForeignKeyField(exampleContext, x => x.Group);
            exampleModel.LoadForeignKeyField(exampleContext, x => x.Group, x => x.Active == true);

            var validGroupNames = new[] { "Group A", "Group B", "Group C" };
            exampleModel.LoadForeignKeyField(exampleContext, x => x.Group, x => validGroupNames.Contains(x.GroupName));

            // Example usage to load a foreign key of a list of objects
            var exampleListQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                LIMIT 5;";

            var exampleModelList = RelmHelper.GetDataObjects<ExampleModel>(exampleContext, exampleListQuery, throwException: true)
                .ToList();

            exampleModelList.LoadForeignKeyField(exampleContext, x => x.Group);
            exampleModelList.LoadForeignKeyField(exampleContext, x => x.Group, x => x.Active == true);

            exampleModelList.LoadForeignKeyField(exampleContext, x => x.Group, x => validGroupNames.Contains(x.GroupName));
        }

        internal void RunExamples(ExampleQuickContext exampleQuickContext)
        {
            // Example usage to load a foreign key of a single object
            var exampleQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                LIMIT 1;";

            var exampleModel = RelmHelper.GetDataObject<ExampleModel>(exampleQuickContext, exampleQuery, throwException: true);

            exampleModel.LoadForeignKeyField(exampleQuickContext, x => x.Group);
            exampleModel.LoadForeignKeyField(exampleQuickContext, x => x.Group, x => x.Active == true);

            var validGroupNames = new[] { "Group A", "Group B", "Group C" };
            exampleModel.LoadForeignKeyField(exampleQuickContext, x => x.Group, x => validGroupNames.Contains(x.GroupName));

            // Example usage to load a foreign key of a list of objects
            var exampleListQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                LIMIT 5;";

            var exampleModelList = RelmHelper.GetDataObjects<ExampleModel>(exampleQuickContext, exampleListQuery, throwException: true)
                .ToList();

            exampleModelList.LoadForeignKeyField(exampleQuickContext, x => x.Group);
            exampleModelList.LoadForeignKeyField(exampleQuickContext, x => x.Group, x => x.Active == true);

            exampleModelList.LoadForeignKeyField(exampleQuickContext, x => x.Group, x => validGroupNames.Contains(x.GroupName));
        }
    }
}
