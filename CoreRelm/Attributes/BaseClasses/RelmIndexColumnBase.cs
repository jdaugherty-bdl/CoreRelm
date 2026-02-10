using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Attributes.BaseClasses
{
    /// <summary>
    /// Represents a key definition for an index column, including optional length, expression, and sort direction
    /// settings.
    /// </summary>
    /// <remarks>Use this class to specify index key details when defining indexes, including support for
    /// partial indexes or computed expressions. The combination of properties allows for flexible index definitions,
    /// such as descending order or partial column indexing.</remarks>
    /// <param name="columnName">The name of the column used as the index key. Cannot be null or empty.</param>
    /// <param name="length">The maximum length, in characters, to use for the index key. If null, the full column value is used.</param>
    /// <param name="expression">An optional expression used to define the index key. If specified, the expression is used instead of the column
    /// value.</param>
    /// <param name="isDescending">Indicates whether the index key is sorted in descending order. Set to <see langword="true"/> for descending;
    /// otherwise, <see langword="false"/>.</param>
    /// <param name="order">The zero-based position of the item within a collection or sequence.</param>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property, AllowMultiple = true)]
    public class RelmIndexColumnBase(
        string columnName, 
        int length = -1, 
        string expression = null, 
        bool isDescending = false, 
        int order = 0
        ) : Attribute
    {
        /// <summary>
        /// Gets or sets the key value used for indexing columns during migration.
        /// </summary>
        public object? IndexKeyHolder { get; set; }

        /// <summary>
        /// Gets the name of the column associated with this instance.
        /// </summary>
        public string ColumnName { get; } = columnName;

        /// <summary>
        /// Gets the length of the item, if specified.
        /// </summary>
        public int? Length { get; } = length;

        /// <summary>
        /// Gets the expression string associated with this instance.
        /// </summary>
        public string? Expression { get; } = expression;

        /// <summary>
        /// Gets a value indicating whether the sort order is descending.
        /// </summary>
        public bool IsDescending { get; } = isDescending;

        /// <summary>
        /// Gets the zero-based position of the item within a collection or sequence.
        /// </summary>
        public int Order { get; } = order;
    }
}
