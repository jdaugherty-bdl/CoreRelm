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
            // Example usage to create an unscoped context
            var unscopedContext = new ExampleContext();
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

            // Example usage to create an unscoped context with auto-open transaction
            var unscopedContextAutoTransaction = new ExampleContext(autoOpenTransaction: true);
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

            // Example usage to create an unscoped quick context
            var unscopedQuickContext = new ExampleContext(autoInitializeDataSets: false, autoVerifyTables: false);
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

            // Example usage to create an unscoped quick context with auto-open transaction
            var unscopedQuickContextAutoTransaction = new ExampleContext(autoOpenTransaction: true, autoInitializeDataSets: false, autoVerifyTables: false);
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
