using CoreRelm.Interfaces;
using CoreRelm.Quickstart.Contexts;
using CoreRelm.Quickstart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Quickstart.Examples.Identity
{
    internal class IdentityExamples
    {
        internal void RunExamples(ExampleContext exampleContext)
        {
            // Example usage to get the last inserted ID
            var lastInsertId = RelmHelper.GetLastInsertId(exampleContext);
            lastInsertId = exampleContext.GetLastInsertId();

            // Example usage to get ID from InternalId
            var tableName = RelmHelper.GetDalTable<ExampleModel>();
            var internalId = "some-guid-value";

            var idFromInternalId = RelmHelper.GetIdFromInternalId(exampleContext, tableName, internalId);
            idFromInternalId = exampleContext.GetIdFromInternalId(tableName, internalId);
        }
    }
}
