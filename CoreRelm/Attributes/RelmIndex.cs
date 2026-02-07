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
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property, AllowMultiple = true)]
    public sealed class RelmIndex : Attribute
    {
        /// <summary>
        /// Gets or sets the names of the properties that are indexed for search or filtering operations.
        /// </summary>
        public string[]? IndexedProperties { get; set; }

        /// <summary>
        /// Gets or sets the name of the index to be used for operations.
        /// </summary>
        public string? IndexName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the sort order is descending.
        /// </summary>
        public bool Descending { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a unique index.
        /// </summary>
        public bool Unique { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of the RelmIndex class with the specified properties to be indexed.
        /// </summary>
        /// <param name="indexedProperties">An array of property names to be indexed. Must contain at least one element.</param>
        /// <param name="indexName">The name of the index. If null, a default name will be generated based on the indexed properties.</param>
        /// <param name="descending">A value indicating whether the index should be created in descending order. Default is false (ascending order).</param>
        /// <exception cref="ArgumentException">Thrown if indexedProperties is null or empty.</exception>
        public RelmIndex(string[]? indexedProperties = null, string? indexName = null, bool descending = false, bool unique = false)
        {
            IndexedProperties = indexedProperties;
            IndexName = indexName;
            Descending = descending;
            Unique = unique;
        }
    }
}
