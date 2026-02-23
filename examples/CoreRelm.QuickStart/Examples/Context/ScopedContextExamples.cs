using CoreRelm.Options;
using CoreRelm.Quickstart.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Quickstart.Examples.Context
{
    internal class ScopedContextExamples
    {
        internal void RunExamples()
        {
            // Example usage to create a scoped context with an automatically opened connection
            using (var scopedContext = new RelmContextOptionsBuilder().Build<ExampleContext>() ?? throw new InvalidOperationException("Failed to create ExampleContext"))
            {
                scopedContext.BeginTransaction();

                try
                {
                    // Perform database operations here using scopedContext, will automatically commit on dispose
                }
                catch (Exception)
                {
                    scopedContext.RollbackTransaction();
                    throw;
                }
            }

            // Example usage to create a scoped context with auto-open connection and transaction
            using (var scopedAutoOpenContext = new RelmContextOptionsBuilder().SetAutoOpenTransaction(true).Build<ExampleContext>() ?? throw new InvalidOperationException("Failed to create ExampleContext"))
            {
                try
                {
                    // Perform database operations here using scopedAutoOpenContext, will automatically commit on dispose
                }
                catch (Exception)
                {
                    scopedAutoOpenContext.RollbackTransaction();
                    throw;
                }
            }

            // Example usage to create a scoped quick context with automatically opened connection
            using (var scopedQuickContext = new RelmContextOptionsBuilder().SetAutoInitializeDataSets(false).SetAutoVerifyTables(false).Build<ExampleContext>() ?? throw new InvalidOperationException("Failed to create ExampleContext"))
            {
                scopedQuickContext.BeginTransaction();

                try
                {
                    // Perform database operations here using scopedQuickContext, will automatically commit on dispose
                }
                catch (Exception)
                {
                    scopedQuickContext.RollbackTransaction();
                    throw;
                }
            }

            // Example usage to create a scoped quick context with auto-open connection and transaction
            using (var scopedAutoOpenQuickContext = new RelmContextOptionsBuilder().SetAutoOpenTransaction(true).SetAutoInitializeDataSets(false).SetAutoVerifyTables(false).Build<ExampleContext>() ?? throw new InvalidOperationException("Failed to create ExampleContext"))
            {
                try
                {
                    // Perform database operations here using scopedAutoOpenQuickContext, will automatically commit on dispose
                }
                catch (Exception)
                {
                    scopedAutoOpenQuickContext.RollbackTransaction();
                    throw;
                }
            }
        }
    }
}
