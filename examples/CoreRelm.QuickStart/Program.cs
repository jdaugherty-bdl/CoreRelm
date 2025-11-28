using MySql.Data.MySqlClient;
using CoreRelm.Quickstart.Contexts;
using static CoreRelm.Quickstart.Enums.ConnectionStrings;
using CoreRelm.Options;

namespace CoreRelm.QuickStart
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
            var optionsBuilderInitializedContext = new ExampleContext(new RelmContextOptionsBuilder("name=ExampleContextDatabase"), autoOpenConnection: true, autoOpenTransaction: false, allowUserVariables: false, convertZeroDateTime: false, lockWaitTimeoutSeconds: 0);
            var connectionStringInitializedContext = new ExampleContext(new RelmContextOptionsBuilder("example_server", "example_database", "example_user", "example_password"), autoOpenConnection: true, autoOpenTransaction: false, allowUserVariables: false, convertZeroDateTime: false, lockWaitTimeoutSeconds: 0);

            var autoSelectInitializedQuickContext = new ExampleQuickContext(autoOpenConnection: true, autoOpenTransaction: false, allowUserVariables: false, convertZeroDateTime: false, lockWaitTimeoutSeconds: 0);
            var enumInitializedQuickContext = new ExampleQuickContext(ConnectionStringTypes.ExampleContextDatabase, autoOpenConnection: true, autoOpenTransaction: false, allowUserVariables: false, convertZeroDateTime: false, lockWaitTimeoutSeconds: 0);
            var connectionInitializedQuickContext = new ExampleQuickContext(exampleConnection, autoOpenConnection: true, autoOpenTransaction: false, allowUserVariables: false, convertZeroDateTime: false, lockWaitTimeoutSeconds: 0);
            var connectionTransactionInitializedQuickContext = new ExampleContext(exampleConnection, exampleConnection.BeginTransaction(), autoOpenConnection: true, allowUserVariables: false, convertZeroDateTime: false, lockWaitTimeoutSeconds: 0);
            var optionsBuilderInitializedQuickContext = new ExampleQuickContext(new RelmContextOptionsBuilder("name=PortalCertDatabase"), autoOpenConnection: true, autoOpenTransaction: false, allowUserVariables: false, convertZeroDateTime: false, lockWaitTimeoutSeconds: 0);
            var connectionStringInitializedQuickContext = new ExampleQuickContext(new RelmContextOptionsBuilder("example_server", "example_database", "example_user", "example_password"), autoOpenConnection: true, autoOpenTransaction: false, allowUserVariables: false, convertZeroDateTime: false, lockWaitTimeoutSeconds: 0);

            var scopedContextExamples = new Quickstart.Examples.Context.ScopedContextExamples();
            scopedContextExamples.RunExamples();

            var unscopedContextExamples = new Quickstart.Examples.Context.UnscopedContextExamples();
            unscopedContextExamples.RunExamples();

            // Run attributes examples
            var attributesExamples = new Quickstart.Examples.Attributes.AttributesExamples();
            attributesExamples.RunExamples();

            // Run standard connection examples
            var standardConnectionExamples = new Quickstart.Examples.Connections.StandardConnectionExamples();
            standardConnectionExamples.RunExamples();

            // Relm Context initializes all datasets and reads the database to preload metadata, making some subsequent operations faster

            // Initialize a scoped Relm context
            using (var relmContext = new ExampleContext())
            {
                // Run identity examples
                var identityExamples = new Quickstart.Examples.Identity.IdentityExamples();
                identityExamples.RunExamples(relmContext);

                // Run data row examples
                var dataRowExamples = new Quickstart.Examples.Data.DataRowExamples();
                dataRowExamples.RunExamples(relmContext);

                // Run data table examples
                var dataTableExamples = new Quickstart.Examples.Data.DataTableExamples();
                dataTableExamples.RunExamples(relmContext);

                // Run data object examples
                var dataObjectExamples = new Quickstart.Examples.Data.DataObjectExamples();
                dataObjectExamples.RunExamples(relmContext);

                // Run data list examples
                var dataListExamples = new Quickstart.Examples.Data.DataListExamples();
                dataListExamples.RunExamples(relmContext);
            }

            // Relm Quick Context lazy loads metadata as needed with the first operation, so some operations may be slower the first time they are run

            // Initialize a scoped Relm Quick context
            using (var relmQuickContext = new ExampleQuickContext())
            {
                // Run identity examples
                var identityExamples = new Quickstart.Examples.Identity.IdentityExamples();
                identityExamples.RunExamples(relmQuickContext);

                // Run data row examples
                var dataRowExamples = new Quickstart.Examples.Data.DataRowExamples();
                dataRowExamples.RunExamples(relmQuickContext);

                // Run data table examples
                var dataTableExamples = new Quickstart.Examples.Data.DataTableExamples();
                dataTableExamples.RunExamples(relmQuickContext);

                // Run data object examples
                var dataObjectExamples = new Quickstart.Examples.Data.DataObjectExamples();
                dataObjectExamples.RunExamples(relmQuickContext);

                // Run data list examples
                var dataListExamples = new Quickstart.Examples.Data.DataListExamples();
                dataListExamples.RunExamples(relmQuickContext);
            }

            // Initialize the Relm context
            using (var relmContext = new ExampleContext(autoOpenTransaction: true))
            {
                try
                {
                    // Run database work examples
                    var databaseWorkExamples = new Quickstart.Examples.Data.DatabaseWorkExamples();
                    databaseWorkExamples.RunExamples(relmContext);

                    // Run bulk table write examples
                    var bulkTableWriteExamples = new Quickstart.Examples.BulkWriter.BulkTableWriterExamples();
                    bulkTableWriteExamples.RunExamples(relmContext);
                }
                catch (Exception ex)
                {
                    relmContext.RollbackTransactions();

                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }

            // Initialize the Relm Quick context
            using (var relmQuickContext = new ExampleQuickContext(autoOpenTransaction: true))
            {
                try
                {
                    // Run database work examples
                    var databaseWorkExamples = new Quickstart.Examples.Data.DatabaseWorkExamples();
                    databaseWorkExamples.RunExamples(relmQuickContext);

                    // Run bulk table write examples
                    var bulkTableWriteExamples = new Quickstart.Examples.BulkWriter.BulkTableWriterExamples();
                    bulkTableWriteExamples.RunExamples(relmQuickContext);
                }
                catch (Exception ex)
                {
                    relmQuickContext.RollbackTransactions();

                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
