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
            // Example usage to create a scoped context
            using (var scopedContext = new ExampleContext())
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

            // Example usage to create a scoped context with auto-open transaction
            using (var scopedAutoOpenContext = new ExampleContext(autoOpenTransaction: true))
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

            // Example usage to create a scoped quick context
            using (var scopedQuickContext = new ExampleContext(autoInitializeDataSets: false, autoVerifyTables: false))
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

            // Example usage to create a scoped quick context with auto-open transaction
            using (var scopedAutoOpenQuickContext = new ExampleContext(autoOpenTransaction: true, autoInitializeDataSets: false, autoVerifyTables: false))
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
