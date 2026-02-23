using MySql.Data.MySqlClient;
using CoreRelm.Options;
using CoreRelm.Quickstart.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Quickstart.Enums.ConnectionStrings;

namespace CoreRelm.Quickstart
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var exampleConnection = new MySqlConnection();

            /*
             * Eager load contexts with settings shown as examples. In reality, you would likely want to leave these 
             * settings to their default values and only change the ones that are relevant to your use case, but they 
             * are all shown here as examples of what can be set in the options builder. You can also mix and match 
             * settings from different examples as needed since they are all just setting properties on the same 
             * options builder.
            */
            // Example: Your context constructor handles connection strings/names and calling base()
            var autoSelectInitializedContext = new RelmContextOptionsBuilder()
                .SetAutoOpenConnection(true)
                .SetAutoOpenTransaction(false)
                .SetAllowUserVariables(false)
                .SetConvertZeroDateTime(false)
                .SetLockWaitTimeoutSeconds(0)
                .SetAutoInitializeDataSets(true)
                .SetAutoVerifyTables(true)
                .Build<ExampleContext>() 
                ?? throw new InvalidOperationException("Failed to create ExampleContext");

            // Example: Auto select context options from a previous context
            var relmContextInitializedContext = new RelmContextOptionsBuilder(autoSelectInitializedContext)
                .SetAutoOpenConnection(true)
                .SetAllowUserVariables(false)
                .SetConvertZeroDateTime(false)
                .SetLockWaitTimeoutSeconds(0)
                .SetAutoInitializeDataSets(true)
                .SetAutoVerifyTables(true)
                .Build<ExampleContext>()
                ?? throw new InvalidOperationException("Failed to create ExampleContext");

            // Example: Initialize context options from an enum value
            var enumInitializedContext = new RelmContextOptionsBuilder(ConnectionStringTypes.ExampleContextDatabase)
                .SetAutoOpenConnection(true)
                .SetAutoOpenTransaction(false)
                .SetAllowUserVariables(false)
                .SetConvertZeroDateTime(false)
                .SetLockWaitTimeoutSeconds(0)
                .SetAutoInitializeDataSets(true)
                .SetAutoVerifyTables(true)
                .Build<ExampleContext>()
                ?? throw new InvalidOperationException("Failed to create ExampleContext");

            // Example: Initialize context options from a connection
            var connectionInitializedContext = new RelmContextOptionsBuilder(exampleConnection)
                .SetAutoOpenTransaction(false)
                .SetAllowUserVariables(false)
                .SetConvertZeroDateTime(false)
                .SetLockWaitTimeoutSeconds(0)
                .SetAutoInitializeDataSets(true)
                .SetAutoVerifyTables(true)
                .Build<ExampleContext>()
                ?? throw new InvalidOperationException("Failed to create ExampleContext");

            // Example: Initialize context options from a connection and transaction
            var connectionTransactionInitializedContext = new RelmContextOptionsBuilder(exampleConnection, exampleConnection.BeginTransaction())
                .SetAllowUserVariables(false)
                .SetConvertZeroDateTime(false)
                .SetLockWaitTimeoutSeconds(0)
                .SetAutoInitializeDataSets(true)
                .SetAutoVerifyTables(true)
                .Build<ExampleContext>()
                ?? throw new InvalidOperationException("Failed to create ExampleContext");

            // Example: Initialize context options from an named connection string in configuration
            var optionsBuilderInitializedContext = new RelmContextOptionsBuilder("name=ExampleContextDatabase")
                .SetAutoOpenConnection(true)
                .SetAutoOpenTransaction(false)
                .SetAllowUserVariables(false)
                .SetConvertZeroDateTime(false)
                .SetLockWaitTimeoutSeconds(0)
                .SetAutoInitializeDataSets(true)
                .SetAutoVerifyTables(true)
                .Build<ExampleContext>()
                ?? throw new InvalidOperationException("Failed to create ExampleContext");

            // Example: Initialize context options from a connection string
            var connectionStringInitializedContext = new RelmContextOptionsBuilder("Server=example_server;Port=3307;Database=example_database;Uid=example_user;Pwd=example_password;")
                .SetAutoOpenConnection(true)
                .SetAutoOpenTransaction(false)
                .SetAllowUserVariables(false)
                .SetConvertZeroDateTime(false)
                .SetLockWaitTimeoutSeconds(0)
                .SetAutoInitializeDataSets(true)
                .SetAutoVerifyTables(true)
                .Build<ExampleContext>()
                ?? throw new InvalidOperationException("Failed to create ExampleContext");

            // Example: Initialize context options from connection details
            var connectionDetailsInitializedContext = new RelmContextOptionsBuilder("example_server", "example_database", "example_user", "example_password")
                .SetAutoOpenConnection(true)
                .SetAutoOpenTransaction(false)
                .SetAllowUserVariables(false)
                .SetConvertZeroDateTime(false)
                .SetLockWaitTimeoutSeconds(0)
                .SetAutoInitializeDataSets(true)
                .SetAutoVerifyTables(true)
                .Build<ExampleContext>()
                ?? throw new InvalidOperationException("Failed to create ExampleContext");

            // Example: Initialize context options from connection details with port specified
            var connectionDetailsWithPortInitializedContext = new RelmContextOptionsBuilder("example_server", "3307", "example_database", "example_user", "example_password")
                .SetAutoOpenConnection(true)
                .SetAutoOpenTransaction(false)
                .SetAllowUserVariables(false)
                .SetConvertZeroDateTime(false)
                .SetLockWaitTimeoutSeconds(0)
                .SetAutoInitializeDataSets(true)
                .SetAutoVerifyTables(true)
                .Build<ExampleContext>()
                ?? throw new InvalidOperationException("Failed to create ExampleContext");

            /*
             * Lazy load contexts with settings shown as examples. In reality, you would likely want to leave these 
             * settings to their default values and only change the ones that are relevant to your use case, but they 
             * are all shown here as examples of what can be set in the options builder. You can also mix and match 
             * settings from different examples as needed since they are all just setting properties on the same 
             * options builder.
            */
            // Example: Your context constructor handles connection strings/names and calling base()
            var autoSelectInitializedQuickContext = new RelmContextOptionsBuilder()
                .SetAutoOpenConnection(true)
                .SetAutoOpenTransaction(false)
                .SetAllowUserVariables(false)
                .SetConvertZeroDateTime(false)
                .SetLockWaitTimeoutSeconds(0)
                .SetAutoInitializeDataSets(false)
                .SetAutoVerifyTables(false)
                .Build<ExampleContext>()
                ?? throw new InvalidOperationException("Failed to create ExampleContext");

            // Example: Auto select context options from a previous context
            var relmContextInitializedQuickContext = new RelmContextOptionsBuilder(autoSelectInitializedQuickContext)
                .SetAutoOpenConnection(true)
                .SetAllowUserVariables(false)
                .SetConvertZeroDateTime(false)
                .SetLockWaitTimeoutSeconds(0)
                .SetAutoInitializeDataSets(false)
                .SetAutoVerifyTables(false)
                .Build<ExampleContext>()
                ?? throw new InvalidOperationException("Failed to create ExampleContext");

            // Example: Initialize context options from an enum value
            var enumInitializedQuickContext = new RelmContextOptionsBuilder(ConnectionStringTypes.ExampleContextDatabase)
                .SetAutoOpenConnection(true)
                .SetAutoOpenTransaction(false)
                .SetAllowUserVariables(false)
                .SetConvertZeroDateTime(false)
                .SetLockWaitTimeoutSeconds(0)
                .SetAutoInitializeDataSets(false)
                .SetAutoVerifyTables(false)
                .Build<ExampleContext>()
                ?? throw new InvalidOperationException("Failed to create ExampleContext");

            // Example: Initialize context options from a connection
            var connectionInitializedQuickContext = new RelmContextOptionsBuilder(exampleConnection)
                .SetAutoOpenConnection(true)
                .SetAutoOpenTransaction(false)
                .SetAllowUserVariables(false)
                .SetConvertZeroDateTime(false)
                .SetLockWaitTimeoutSeconds(0)
                .SetAutoInitializeDataSets(false)
                .SetAutoVerifyTables(false)
                .Build<ExampleContext>()
                ?? throw new InvalidOperationException("Failed to create ExampleContext");

            // Example: Initialize context options from a connection and transaction
            var connectionTransactionInitializedQuickContext = new RelmContextOptionsBuilder(exampleConnection, exampleConnection.BeginTransaction())
                .SetAutoOpenConnection(true)
                .SetAutoOpenTransaction(false)
                .SetAllowUserVariables(false)
                .SetConvertZeroDateTime(false)
                .SetLockWaitTimeoutSeconds(0)
                .SetAutoInitializeDataSets(false)
                .SetAutoVerifyTables(false)
                .Build<ExampleContext>()
                ?? throw new InvalidOperationException("Failed to create ExampleContext");

            // Example: Initialize context options from an named connection string in configuration
            var optionsBuilderInitializedQuickContext = new RelmContextOptionsBuilder("name=PortalCertDatabase")
                .SetAutoOpenConnection(true)
                .SetAutoOpenTransaction(false)
                .SetAllowUserVariables(false)
                .SetConvertZeroDateTime(false)
                .SetLockWaitTimeoutSeconds(0)
                .SetAutoInitializeDataSets(false)
                .SetAutoVerifyTables(false)
                .Build<ExampleContext>()
                ?? throw new InvalidOperationException("Failed to create ExampleContext");

            // Example: Initialize context options from a connection string
            var connectionStringInitializedQuickContext = new RelmContextOptionsBuilder("Server=example_server;Port=3307;Database=example_database;Uid=example_user;Pwd=example_password;")
                .SetAutoOpenConnection(true)
                .SetAutoOpenTransaction(false)
                .SetAllowUserVariables(false)
                .SetConvertZeroDateTime(false)
                .SetLockWaitTimeoutSeconds(0)
                .SetAutoInitializeDataSets(false)
                .SetAutoVerifyTables(false)
                .Build<ExampleContext>()
                ?? throw new InvalidOperationException("Failed to create ExampleContext");

            // Example: Initialize context options from connection details
            var connectionDetailsInitializedQuickContext = new RelmContextOptionsBuilder("example_server", "example_database", "example_user", "example_password")
                .SetAutoOpenConnection(true)
                .SetAutoOpenTransaction(false)
                .SetAllowUserVariables(false)
                .SetConvertZeroDateTime(false)
                .SetLockWaitTimeoutSeconds(0)
                .SetAutoInitializeDataSets(false)
                .SetAutoVerifyTables(false)
                .Build<ExampleContext>()
                ?? throw new InvalidOperationException("Failed to create ExampleContext");

            // Example: Initialize context options from connection details with port specified
            var connectionDetailsWithPortInitializedQuickContext = new RelmContextOptionsBuilder("example_server", "3307", "example_database", "example_user", "example_password")
                .SetAutoOpenConnection(true)
                .SetAutoOpenTransaction(false)
                .SetAllowUserVariables(false)
                .SetConvertZeroDateTime(false)
                .SetLockWaitTimeoutSeconds(0)
                .SetAutoInitializeDataSets(false)
                .SetAutoVerifyTables(false)
                .Build<ExampleContext>()
                ?? throw new InvalidOperationException("Failed to create ExampleContext");

            var scopedContextExamples = new Examples.Context.ScopedContextExamples();
            scopedContextExamples.RunExamples();

            var unscopedContextExamples = new Examples.Context.UnscopedContextExamples();
            unscopedContextExamples.RunExamples();

            // Run attributes examples
            var attributesExamples = new Examples.Attributes.AttributesExamples();
            attributesExamples.RunExamples();

            // Run standard connection examples
            var standardConnectionExamples = new Examples.Connections.StandardConnectionExamples();
            standardConnectionExamples.RunExamples();

            // Relm Context initializes all datasets and reads the database to preload metadata, making some subsequent operations faster

            // Initialize a scoped Relm context
            using (var relmContext = new RelmContextOptionsBuilder().Build<ExampleContext>() ?? throw new InvalidOperationException("Failed to create ExampleContext"))
            {
                // Run identity examples
                var identityExamples = new Examples.Identity.IdentityExamples();
                identityExamples.RunExamples(relmContext);

                // Run data row examples
                var dataRowExamples = new Examples.Data.DataRowExamples();
                dataRowExamples.RunExamples(relmContext);

                // Run data table examples
                var dataTableExamples = new Examples.Data.DataTableExamples();
                dataTableExamples.RunExamples(relmContext);

                // Run data object examples
                var dataObjectExamples = new Examples.Data.DataObjectExamples();
                dataObjectExamples.RunExamples(relmContext);

                // Run data list examples
                var dataListExamples = new Examples.Data.DataListExamples();
                dataListExamples.RunExamples(relmContext);
            }

            // Relm Quick Context lazy loads metadata as needed with the first operation, so some operations may be slower the first time they are run

            // Initialize a scoped Relm Quick context
            using (var relmQuickContext = new RelmContextOptionsBuilder().SetAutoInitializeDataSets(false).SetAutoVerifyTables(false).Build<ExampleContext>() ?? throw new InvalidOperationException("Failed to create ExampleContext"))
            {
                // Run identity examples
                var identityExamples = new Examples.Identity.IdentityExamples();
                identityExamples.RunExamples(relmQuickContext);

                // Run data row examples
                var dataRowExamples = new Examples.Data.DataRowExamples();
                dataRowExamples.RunExamples(relmQuickContext);

                // Run data table examples
                var dataTableExamples = new Examples.Data.DataTableExamples();
                dataTableExamples.RunExamples(relmQuickContext);

                // Run data object examples
                var dataObjectExamples = new Examples.Data.DataObjectExamples();
                dataObjectExamples.RunExamples(relmQuickContext);

                // Run data list examples
                var dataListExamples = new Examples.Data.DataListExamples();
                dataListExamples.RunExamples(relmQuickContext);
            }

            // Initialize the Relm context
            using (var relmContext = new RelmContextOptionsBuilder().SetAutoOpenTransaction(true).Build<ExampleContext>() ?? throw new InvalidOperationException("Failed to create ExampleContext"))
            {
                try
                {
                    // Run database work examples
                    var databaseWorkExamples = new Examples.Data.DatabaseWorkExamples();
                    databaseWorkExamples.RunExamples(relmContext);

                    // Run bulk table write examples
                    var bulkTableWriteExamples = new Examples.BulkWriter.BulkTableWriterExamples();
                    bulkTableWriteExamples.RunExamples(relmContext);
                }
                catch (Exception ex)
                {
                    relmContext.RollbackTransaction();

                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }

            // Initialize the Relm Quick context
            using (var relmQuickContext = new RelmContextOptionsBuilder().SetAutoOpenTransaction(true).SetAutoInitializeDataSets(false).SetAutoVerifyTables(false).Build<ExampleContext>() ?? throw new InvalidOperationException("Failed to create ExampleContext"))
            {
                try
                {
                    // Run database work examples
                    var databaseWorkExamples = new Examples.Data.DatabaseWorkExamples();
                    databaseWorkExamples.RunExamples(relmQuickContext);

                    // Run bulk table write examples
                    var bulkTableWriteExamples = new Examples.BulkWriter.BulkTableWriterExamples();
                    bulkTableWriteExamples.RunExamples(relmQuickContext);
                }
                catch (Exception ex)
                {
                    relmQuickContext.RollbackTransaction();

                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
