using CoreRelm.Attributes.BaseClasses;
using CoreRelm.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Attributes
{
    /// <summary>
    /// Represents a key definition for an index column, including optional length, expression, and sort direction
    /// settings.
    /// </summary>
    /// <remarks>Use this class to specify index key details when defining indexes, including support for
    /// partial indexes or computed expressions. The combination of properties allows for flexible index definitions,
    /// such as descending order or partial column indexing.</remarks>
    /// <param name="indexKey">The key associated with this index column.</param>
    /// <param name="columnName">The name of the column used as the index key. Cannot be null or empty.</param>
    /// <param name="length">The maximum length, in characters, to use for the index key. If null, the full column value is used.</param>
    /// <param name="expression">An optional expression used to define the index key. If specified, the expression is used instead of the column
    /// value.</param>
    /// <param name="isDescending">Indicates whether the index key is sorted in descending order. Set to <see langword="true"/> for descending;
    /// otherwise, <see langword="false"/>.</param>
    /// <param name="order">The zero-based position of the item within a collection or sequence.</param>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property, AllowMultiple = true)]
    public sealed class RelmIndexColumn<T> : RelmIndexColumnBase
    {
        public RelmIndexColumn(
            T indexKey, 
            string columnName, 
            int length = -1, 
            string expression = null, 
            bool isDescending = false, 
            int order = 0)
        : base(
              columnName, 
              length, 
              expression, 
              isDescending, 
              order)
        {
            if (indexKey == null)
                throw new ArgumentException("Index key cannot be null.", nameof(indexKey));

            IndexKey = indexKey;
        }

        /// <summary>
        /// Gets or sets the key used to identify the index entry.
        /// </summary>
        public T IndexKey { get; set; }
    }
}