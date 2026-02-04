using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Attributes
{
    /// <summary>
    /// Specifies that the decorated class or struct should have an index created on the specified properties when used
    /// with a Relm database.
    /// </summary>
    /// <remarks>Apply this attribute to a class or struct to indicate which properties should be indexed for
    /// improved query performance. Indexing can speed up lookups on the specified properties but may increase storage
    /// requirements and affect write performance. This attribute is intended for use with the Relm object
    /// database.</remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class RelmIndex : Attribute
    {
        /// <summary>
        /// Gets or sets the names of the properties that are indexed for search or filtering operations.
        /// </summary>
        public string[]? IndexedProperties { get; set; }

        /// <summary>
        /// Initializes a new instance of the RelmIndex class with the specified properties to be indexed.
        /// </summary>
        /// <param name="indexedProperties">An array of property names to be indexed. Must contain at least one element.</param>
        /// <exception cref="ArgumentException">Thrown if indexedProperties is null or empty.</exception>
        public RelmIndex(string[] indexedProperties)
        {
            if ((indexedProperties?.Length ?? 0) == 0)
                throw new ArgumentException("RelmIndex indexed properties must be provided.", nameof(indexedProperties));

            IndexedProperties = indexedProperties;
        }
    }
}
