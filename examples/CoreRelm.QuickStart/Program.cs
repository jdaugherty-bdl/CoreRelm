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

            var autoSelectInitializedContext = new ExampleContext(autoOpenConnection: true, autoOpenTransaction: false, allowUserVariables: false, convertZeroDateTime: false, lockWaitTimeoutSeconds: 0);
            var relmContextInitializedContext = new ExampleContext(autoSelectInitializedContext, autoOpenConnection: true, allowUserVariables: false, convertZeroDateTime: false, lockWaitTimeoutSeconds: 0);
            var enumInitializedContext = new ExampleContext(ConnectionStringTypes.ExampleContextDatabase, autoOpenConnection: true, autoOpenTransaction: false, allowUserVariables: false, convertZeroDateTime: false, lockWaitTimeoutSeconds: 0);
            var connectionInitializedContext = new ExampleContext(exampleConnection, autoOpenConnection: true, autoOpenTransaction: false, allowUserVariables: false, convertZeroDateTime: false, lockWaitTimeoutSeconds: 0);
            var connectionTransactionInitializedContext = new ExampleContext(exampleConnection, exampleConnection.BeginTransaction(), autoOpenConnection: true, allowUserVariables: false, convertZeroDateTime: false, lockWaitTimeoutSeconds: 0);
            var optionsBuilderInitializedContext = new ExampleContext(new RelmContextOptionsBuilder("name=ExampleContextDatabase"));
            var connectionStringInitializedContext = new ExampleContext(new RelmContextOptionsBuilder("example_server", "example_database", "example_user", "example_password"));

            var autoSelectInitializedQuickContext = new ExampleContext(autoOpenConnection: true, autoOpenTransaction: false, allowUserVariables: false, convertZeroDateTime: false, lockWaitTimeoutSeconds: 0, autoInitializeDataSets: false, autoVerifyTables: false);
            var enumInitializedQuickContext = new ExampleContext(ConnectionStringTypes.ExampleContextDatabase, autoOpenConnection: true, autoOpenTransaction: false, allowUserVariables: false, convertZeroDateTime: false, lockWaitTimeoutSeconds: 0, autoInitializeDataSets: false, autoVerifyTables: false);
            var connectionInitializedQuickContext = new ExampleContext(exampleConnection, autoOpenConnection: true, autoOpenTransaction: false, allowUserVariables: false, convertZeroDateTime: false, lockWaitTimeoutSeconds: 0, autoInitializeDataSets: false, autoVerifyTables: false);
            var connectionTransactionInitializedQuickContext = new ExampleContext(exampleConnection, exampleConnection.BeginTransaction(), autoOpenConnection: true, allowUserVariables: false, convertZeroDateTime: false, lockWaitTimeoutSeconds: 0, autoInitializeDataSets: false, autoVerifyTables: false);
            var optionsBuilderInitializedQuickContext = new ExampleContext(new RelmContextOptionsBuilder("name=PortalCertDatabase")
                .SetAutoOpenConnection(true)
                .SetAutoOpenTransaction(false)
                .SetAllowUserVariables(false)
                .SetConvertZeroDateTime(false)
                .SetLockWaitTimeoutSeconds(0)
                .SetAutoInitializeDataSets(false)
                .SetAutoVerifyTables(false));
            var connectionStringInitializedQuickContext = new ExampleContext(new RelmContextOptionsBuilder("example_server", "example_database", "example_user", "example_password")
                .SetAutoOpenConnection(true)
                .SetAutoOpenTransaction(false)
                .SetAllowUserVariables(false)
                .SetConvertZeroDateTime(false)
                .SetLockWaitTimeoutSeconds(0)
                .SetAutoInitializeDataSets(false)
                .SetAutoVerifyTables(false));

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
            using (var relmContext = new ExampleContext())
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
            using (var relmQuickContext = new ExampleContext(autoInitializeDataSets: false, autoVerifyTables: false))
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
            using (var relmContext = new ExampleContext(autoOpenTransaction: true))
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
            using (var relmQuickContext = new ExampleContext(autoOpenTransaction: true, autoInitializeDataSets: false, autoVerifyTables: false))
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
