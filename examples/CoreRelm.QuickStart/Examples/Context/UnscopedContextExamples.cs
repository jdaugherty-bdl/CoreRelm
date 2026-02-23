using CoreRelm.Options;
using CoreRelm.Quickstart.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Quickstart.Examples.Context
{
    internal class UnscopedContextExamples
    {
        internal void RunExamples()
        {
            // Example usage to create an unscoped context with auto-open connection
            var unscopedContext = new RelmContextOptionsBuilder().SetAutoOpenConnection(true).Build<ExampleContext>() ?? throw new InvalidOperationException("Failed to create ExampleContext");
            unscopedContext.BeginTransaction();

            try
            {
                // Perform database operations here using unscopedContext

                unscopedContext.CommitTransaction();
            }
            catch (Exception)
            {
                unscopedContext.RollbackTransaction();
                throw;
            }

            // Example usage to create an unscoped context with auto-open connection and transaction
            var unscopedContextAutoTransaction = new RelmContextOptionsBuilder().SetAutoOpenTransaction(true).Build<ExampleContext>() ?? throw new InvalidOperationException("Failed to create ExampleContext");
            try
            {
                // Perform database operations here using unscopedContextAutoTransaction
                
                unscopedContextAutoTransaction.CommitTransaction();
            }
            catch (Exception)
            {
                unscopedContextAutoTransaction.RollbackTransaction();
                throw;
            }

            // Example usage to create an unscoped quick context with auto-open connection
            var unscopedQuickContext = new RelmContextOptionsBuilder().SetAutoInitializeDataSets(false).SetAutoVerifyTables(false).Build<ExampleContext>() ?? throw new InvalidOperationException("Failed to create ExampleContext");
            unscopedQuickContext.BeginTransaction();
            
            try
            {
                // Perform database operations here using unscopedQuickContext
                
                unscopedQuickContext.CommitTransaction();
            }
            catch (Exception)
            {
                unscopedQuickContext.RollbackTransaction();
                throw;
            }

            // Example usage to create an unscoped quick context with auto-open connection and transaction
            var unscopedQuickContextAutoTransaction = new RelmContextOptionsBuilder().SetAutoOpenTransaction(true).SetAutoInitializeDataSets(false).SetAutoVerifyTables(false).Build<ExampleContext>() ?? throw new InvalidOperationException("Failed to create ExampleContext");
            try
            {
                // Perform database operations here using unscopedQuickContextAutoTransaction
        
                unscopedQuickContextAutoTransaction.CommitTransaction();
            }
            catch (Exception)
            {
                unscopedQuickContextAutoTransaction.RollbackTransaction();
                throw;
            }
        }
    }
}
