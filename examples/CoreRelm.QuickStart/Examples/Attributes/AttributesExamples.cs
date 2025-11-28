using CoreRelm.Quickstart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Quickstart.Examples.Attributes
{
    internal class AttributesExamples
    {
        internal void RunExamples()
        {
            // Example usage to get table name from attribute
            var tableName = RelmHelper.GetDalTable<ExampleModel>();

            // Example usage to get column name from attribute
            var columnName = RelmHelper.GetColumnName<ExampleModel>(x => x.ModelName);
        }
    }
}
