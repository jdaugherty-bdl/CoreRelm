using CoreRelm.Interfaces;
using CoreRelm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.DataTransfer
{
    internal class RowIdentityHelper
    {
        internal static string GetLastInsertId(Enum ConfigConnectionString)
        {
            using (IRelmContext context = new RelmContext(ConfigConnectionString))
            {
                return GetLastInsertId(context);
            }
        }

        /// <summary>
        /// Use the MySql built in function to get the ID of the last row inserted.
        /// </summary>
        /// <param name="ConfigContext">The connection type to use when getting the last ID.</param>
        /// <returns>A string representation of the ID.</returns>
        internal static string GetLastInsertId(IRelmContext ConfigContext)
        {
            return RefinedResultsHelper.GetScalar<string>(ConfigContext, "SELECT LAST_INSERT_ID();");
        }

        internal static string GetIdFromInternalId(Enum ConfigConnectionString, string Table, string InternalId)
        {
            using (IRelmContext context = new RelmContext(ConfigConnectionString))
            {
                return GetIdFromInternalId(context, Table, InternalId);
            }
        }

        /// <summary>
        /// Converts an InternalId to an autonumbered row ID.
        /// </summary>
        /// <param name="ConfigContext">The connection type to use.</param>
        /// <param name="Table">Table name to use for the conversion.</param>
        /// <param name="InternalId">The GUID of the InternalId to convert.</param>
        /// <returns>ID of the row matching the InternalId.</returns>
        internal static string GetIdFromInternalId(IRelmContext ConfigContext, string Table, string InternalId)
        {
            return RefinedResultsHelper.GetScalar<string>(ConfigContext, $"SELECT ID FROM {Table} WHERE InternalId = @InternalId", new Dictionary<string, object> { { "@InternalId", InternalId } });
        }
    }
}
