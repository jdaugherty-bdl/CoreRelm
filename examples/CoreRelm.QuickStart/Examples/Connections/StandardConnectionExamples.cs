using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Quickstart.Enums.ConnectionStrings;

namespace CoreRelm.Quickstart.Examples.Connections
{
    internal class StandardConnectionExamples
    {
        internal void RunExamples()
        {
            // Example usage to get a standard connection string without specifying a return type
            var connectionActionReturn = RelmHelper.StandardConnectionWrapper(ConnectionStringTypes.ExampleContextDatabase,
                (connection, transaction) =>
                {
                    // Use the connection and transaction as needed
                    // For example, you can execute a command here

                    return true;
                }, exceptionHandler: (exception, st) =>
                {
                    // Handle exceptions as needed
                    Console.WriteLine($"An error occurred: {exception.Message}");
                });

            // Example usage to get a standard connection string with specifying a return type
            var connectionActionReturnWithType = RelmHelper.StandardConnectionWrapper<int>(ConnectionStringTypes.ExampleContextDatabase,
                (connection, transaction) =>
                {
                    // Use the connection and transaction as needed
                    // For example, you can execute a command here

                    return 1;
                }, exceptionHandler: (exception, st) =>
                {
                    // Handle exceptions as needed
                    Console.WriteLine($"An error occurred: {exception.Message}");
                });
        }
    }
}
