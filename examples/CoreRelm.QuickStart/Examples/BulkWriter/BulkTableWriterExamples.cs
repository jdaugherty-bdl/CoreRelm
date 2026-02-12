using CoreRelm.Interfaces;
using CoreRelm.Quickstart.Contexts;
using CoreRelm.Quickstart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Quickstart.Examples.BulkWriter
{
    internal class BulkTableWriterExamples
    {
        internal void RunExamples(ExampleContext exampleContext)
        {
            // Example usage to create a BulkTableWriter with all parameters
            var insertQuery = $@"INSERT INTO {RelmHelper.GetDalTable<ExampleModel>()} 
                ({RelmHelper.GetColumnName<ExampleModel>(x => x.GroupInternalId)}, 
                    {RelmHelper.GetColumnName<ExampleModel>(x => x.ModelName)}, 
                    {RelmHelper.GetColumnName<ExampleModel>(x => x.ModelIndex)}) 
                VALUES 
                (@value1, @value2, @value3);";

            var insertParameters = new Dictionary<string, object>
            {
                { "@value1", "abcd" },
                { "@value2", "efgh" },
                { "@value3", "ijkl" }
            };

            var bulkWriter = RelmHelper.GetBulkTableWriter<ExampleModel>(exampleContext, insertQuery: insertQuery, throwException: true, allowAutoIncrementColumns: false, allowPrimaryKeyColumns: false, allowUniqueColumns: false);
            bulkWriter = exampleContext.GetBulkTableWriter<ExampleModel>(insertQuery: insertQuery, throwException: true, allowAutoIncrementColumns: false, allowPrimaryKeyColumns: false, allowUniqueColumns: false);

            var rowsUpdated = bulkWriter.Write();
        }
    }
}
