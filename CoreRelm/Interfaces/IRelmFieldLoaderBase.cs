using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces
{
    /// <summary>
    /// Defines the contract for loading field data based on key values in a data source.
    /// </summary>
    /// <remarks>Implementations of this interface provide mechanisms to retrieve field values using one or
    /// more key fields. This interface is typically used in scenarios where fields are dynamically loaded or mapped
    /// from external data sources based on composite keys.</remarks>
    public interface IRelmFieldLoaderBase
    {
        /// <summary>
        /// Gets the name of the field represented by this member.
        /// </summary>
        string FieldName { get; }

        /// <summary>
        /// Gets the names of the fields that uniquely identify an entity within the data source.
        /// </summary>
        string[] KeyFields { get; }

        /// <summary>
        /// Retrieves field data for the specified collection of key arrays.
        /// </summary>
        /// <typeparam name="S">The type of the elements in each key array.</typeparam>
        /// <param name="keyData">A collection of key arrays for which to retrieve associated field data. Cannot be null.</param>
        /// <returns>A dictionary mapping each input key array to its corresponding field data. If a key array has no associated
        /// data, it may be omitted from the dictionary.</returns>
        Dictionary<S[], object>? GetFieldData<S>(ICollection<S[]>? keyData) where S : notnull;

        /*************************************************************************************************
         *                                         ASYNC METHODS                                         *
         *************************************************************************************************/

        /// <summary>
        /// Retrieves field data for the specified collection of key arrays.
        /// </summary>
        /// <typeparam name="S">The type of the elements in each key array.</typeparam>
        /// <param name="keyData">A collection of key arrays for which to retrieve associated field data. Cannot be null.</param>
        /// <returns>A dictionary mapping each input key array to its corresponding field data. If a key array has no associated
        /// data, it may be omitted from the dictionary.</returns>
        Task<Dictionary<S[], object>?> GetFieldDataAsync<S>(ICollection<S[]>? keyData, CancellationToken cancellationToken = default) where S : notnull;
    }
}
