using CoreRelm.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.DataTransfer
{
    internal class RowIdentityHelper
    {
        /// <summary>
        /// Retrieves the identifier of the last inserted row in the database for the specified connection.
        /// </summary>
        /// <remarks>This method relies on the database's LAST_INSERT_ID() function, which typically
        /// returns the most recent auto-increment value generated during the current session.  Ensure that the database
        /// supports this function and that the session context is consistent with the operation that generated the
        /// ID.</remarks>
        /// <param name="connectionName">An enumeration value representing the configuration connection string to use for the database query.</param>
        /// <returns>A string containing the identifier of the last inserted row. The value is determined by the database's
        /// LAST_INSERT_ID() function.</returns>
        internal static string? GetLastInsertId(Enum connectionName)
        {
            return RefinedResultsHelper.GetScalar<string>(connectionName, "SELECT LAST_INSERT_ID();");
        }

        /// <summary>
        /// Retrieves the identifier of the last inserted row in the current database session.
        /// </summary>
        /// <remarks>This method relies on the database's LAST_INSERT_ID() function, which typically
        /// returns the most recent auto-increment value generated during the current session.  Ensure that the database
        /// supports this function and that the session context is consistent with the operation that generated the
        /// ID.</remarks>
        /// <param name="relmContext">The database context used to execute the query. This context must be properly initialized and connected to
        /// the database.</param>
        /// <returns>A string containing the identifier of the last inserted row. The value is determined by the database's
        /// LAST_INSERT_ID() function.</returns>
        internal static string? GetLastInsertId(IRelmContext relmContext)
        {
            return RefinedResultsHelper.GetScalar<string>(relmContext, "SELECT LAST_INSERT_ID();");
        }

        /// <summary>
        /// Retrieves the ID associated with the specified internal ID from the given table.
        /// </summary>
        /// <remarks>This method executes a SQL query to retrieve the ID corresponding to the
        /// provided internal ID. Ensure that the <paramref name="connectionName"/> corresponds to a valid database
        /// connection and that the <paramref name="tableName"/> exists in the database schema.</remarks>
        /// <param name="connectionName">The database connection identifier, represented as an enumeration value.</param>
        /// <param name="tableName">The name of the database table to query. Must not be null or empty.</param>
        /// <param name="InternalId">The internal ID to search for. Must not be null or empty.</param>
        /// <returns>The ID as a string if a matching record is found; otherwise, <see langword="null"/>.</returns>
        internal static string? GetIdFromInternalId(Enum connectionName, string tableName, string InternalId)
        {
            return RefinedResultsHelper.GetScalar<string>(connectionName, $"SELECT ID FROM {tableName} WHERE InternalId = @InternalId", new Dictionary<string, object> { { "@InternalId", InternalId } });
        }

        /// <summary>
        /// Retrieves the ID associated with the specified internal ID from the given table.
        /// </summary>
        /// <remarks>This method executes a SQL query to retrieve the ID corresponding to the
        /// provided internal ID. Ensure that the table specified by <paramref name="tableName"/> contains columns named
        /// "ID" and "InternalId".</remarks>
        /// <param name="relmContext">The database context used to execute the query.</param>
        /// <param name="tableName">The name of the table to query. Must not be null or empty.</param>
        /// <param name="InternalId">The internal ID to search for. Must not be null or empty.</param>
        /// <returns>The ID as a string if a matching record is found; otherwise, <see langword="null"/>.</returns>
        internal static string? GetIdFromInternalId(IRelmContext relmContext, string tableName, string InternalId)
        {
            return RefinedResultsHelper.GetScalar<string>(relmContext, $"SELECT ID FROM {tableName} WHERE InternalId = @InternalId", new Dictionary<string, object> { { "@InternalId", InternalId } });
        }
    }
}
