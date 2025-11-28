using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.Quickstart.Contexts;
using CoreRelm.Quickstart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Quickstart.Examples.ModelProperties
{
    internal class UnderscorePropertiesExamples
    {
        internal void RunExamples(ExampleContext exampleContext)
        {
            // Example usage to get all model properties' names converted to underscore format
            var exampleQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                LIMIT 1;";

            var exampleModel = RelmHelper.GetDataObject<ExampleModel>(exampleContext, exampleQuery, throwException: true);

            var modelUnderscorePropertyNames = exampleModel.GetUnderscoreProperties();
            modelUnderscorePropertyNames = exampleModel.GetUnderscoreProperties(getOnlyRelmColumns: false); // includes all properties, not just those mapped to database columns
        }

        internal void RunExamples(ExampleQuickContext exampleQuickContext)
        {
            // Example usage to get all model properties' names converted to underscore format
            var exampleQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                LIMIT 1;";

            var exampleModel = RelmHelper.GetDataObject<ExampleModel>(exampleQuickContext, exampleQuery, throwException: true);

            var modelUnderscorePropertyNames = exampleModel.GetUnderscoreProperties();
            modelUnderscorePropertyNames = exampleModel.GetUnderscoreProperties(getOnlyRelmColumns: false); // includes all properties, not just those mapped to database columns
        }
    }
}
