using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.Quickstart.Contexts;
using CoreRelm.Quickstart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Quickstart.Examples.Data
{
    internal class DatabaseWorkExamples
    {
        internal void RunExamples(ExampleContext exampleContext)
        {
            // Example usage to run DoDatabaseWork using query only with no return
            var queryOnly = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()};";

            RelmHelper.DoDatabaseWork(exampleContext, queryOnly, throwException: true);
            exampleContext.DoDatabaseWork(queryOnly, throwException: true);

            // Example usage to run DoDatabaseWork using query and parameters with no return
            var parametersQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                WHERE {RelmHelper.GetColumnName<ExampleModel>(x => x.InternalId)} = @guid_value;";
            
            var exampleParameters = new Dictionary<string, object>
            {
                { "@guid_value", "some-guid-value" }
            };
            
            RelmHelper.DoDatabaseWork(exampleContext, parametersQuery, exampleParameters, throwException: true);
            exampleContext.DoDatabaseWork(parametersQuery, exampleParameters, throwException: true);

            // Example usage to run DoDatabaseWork using query only with return of number of affected rows
            var affectedRowsQuery = $@"UPDATE {RelmHelper.GetDalTable<ExampleModel>()} 
                SET {RelmHelper.GetColumnName<ExampleModel>(x => x.Active)} = 0;";

            var affectedRows = RelmHelper.DoDatabaseWork<int>(exampleContext, affectedRowsQuery, throwException: true);
            affectedRows = exampleContext.DoDatabaseWork<int>(affectedRowsQuery, throwException: true);

            // Example usage to run DoDatabaseWork using query and parameters with return of number of affected rows
            var affectedRowsParametersQuery = $@"UPDATE {RelmHelper.GetDalTable<ExampleModel>()} 
                SET {RelmHelper.GetColumnName<ExampleModel>(x => x.InternalId)} = @new_value 
                WHERE {RelmHelper.GetColumnName<ExampleModel>(x => x.InternalId)} = @guid_value;";

            affectedRows = RelmHelper.DoDatabaseWork<int>(exampleContext, affectedRowsParametersQuery, exampleParameters, throwException: true);
            affectedRows = exampleContext.DoDatabaseWork<int>(affectedRowsParametersQuery, exampleParameters, throwException: true);
        }

        internal void RunExamples(ExampleQuickContext exampleQuickContext)
        {
            // Example usage to run DoDatabaseWork using query only with no return
            var queryOnly = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()};";

            RelmHelper.DoDatabaseWork(exampleQuickContext, queryOnly, throwException: true);
            exampleQuickContext.DoDatabaseWork(queryOnly, throwException: true);

            // Example usage to run DoDatabaseWork using query and parameters with no return
            var parametersQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                WHERE {RelmHelper.GetColumnName<ExampleModel>(x => x.InternalId)} = @guid_value;";
            
            var exampleParameters = new Dictionary<string, object>
            {
                { "@guid_value", "some-guid-value" }
            };
            
            RelmHelper.DoDatabaseWork(exampleQuickContext, parametersQuery, exampleParameters, throwException: true);
            exampleQuickContext.DoDatabaseWork(parametersQuery, exampleParameters, throwException: true);

            // Example usage to run DoDatabaseWork using query only with return of number of affected rows
            var affectedRowsQuery = $@"UPDATE {RelmHelper.GetDalTable<ExampleModel>()} 
                SET {RelmHelper.GetColumnName<ExampleModel>(x => x.Active)} = 0;";

            var affectedRows = RelmHelper.DoDatabaseWork<int>(exampleQuickContext, affectedRowsQuery, throwException: true);
            affectedRows = exampleQuickContext.DoDatabaseWork<int>(affectedRowsQuery, throwException: true);

            // Example usage to run DoDatabaseWork using query and parameters with return of number of affected rows
            var affectedRowsParametersQuery = $@"UPDATE {RelmHelper.GetDalTable<ExampleModel>()} 
                SET {RelmHelper.GetColumnName<ExampleModel>(x => x.InternalId)} = @new_value 
                WHERE {RelmHelper.GetColumnName<ExampleModel>(x => x.InternalId)} = @guid_value;";

            affectedRows = RelmHelper.DoDatabaseWork<int>(exampleQuickContext, affectedRowsParametersQuery, exampleParameters, throwException: true);
            affectedRows = exampleQuickContext.DoDatabaseWork<int>(affectedRowsParametersQuery, exampleParameters, throwException: true);
        }
    }
}
