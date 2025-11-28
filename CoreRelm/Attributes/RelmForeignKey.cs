using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Attributes
{
    /// <summary>
    /// Specifies a foreign key relationship for a property or struct in a data model.
    /// </summary>
    /// <remarks>This attribute is used to define the mapping between local keys and foreign keys in a
    /// relational context. It can also specify an optional ordering for the related data. The attribute can be applied
    /// to properties or structs, and supports both single and multiple key mappings.</remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Struct)]
    public sealed class RelmForeignKey : Attribute
    {
        /// <summary>
        /// Gets the collection of foreign key names associated with the entity.
        /// </summary>
        public string[] ForeignKeys { get; private set; } = default;

        /// <summary>
        /// Gets the collection of local keys associated with the entity.
        /// </summary>
        public string[] LocalKeys { get; private set; } = default;

        /// <summary>
        /// Gets the list of fields used to determine the order of items.
        /// </summary>
        public string[] OrderBy { get; private set; } = default;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelmForeignKey"/> class with optional foreign key, local key,
        /// and order-by values.
        /// </summary>
        /// <param name="foreignKey">An optional foreign key used to establish the relationship. If provided, it will be stored as a
        /// single-element array in the <see cref="ForeignKeys"/> property.</param>
        /// <param name="localKey">An optional local key used to establish the relationship. If provided, it will be stored as a single-element
        /// array in the <see cref="LocalKeys"/> property.</param>
        /// <param name="orderBy">An optional order-by clause used to define the sorting of the relationship. If provided, it will be stored
        /// as a single-element array in the <see cref="OrderBy"/> property.</param>
        public RelmForeignKey(string foreignKey = null, string localKey = null, string orderBy = null)
        {
            if (foreignKey != null)
                this.ForeignKeys = new string[] { foreignKey };

            if (localKey != null)
                this.LocalKeys = new string[] { localKey };

            if (orderBy != null) 
                this.OrderBy = new string[] { orderBy };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelmForeignKey"/> class, specifying the foreign keys, local
        /// keys, and optional ordering criteria.
        /// </summary>
        /// <param name="foreignKeys">An array of strings representing the foreign key columns in the related table. Can be <see langword="null"/>
        /// if no foreign keys are specified.</param>
        /// <param name="localKeys">An array of strings representing the local key columns in the current table that map to the foreign keys.
        /// Can be <see langword="null"/> if no local keys are specified.</param>
        /// <param name="orderBy">An array of strings specifying the ordering criteria for the related data. Each string represents a column
        /// name, optionally followed by a direction (e.g., "ColumnName ASC" or "ColumnName DESC"). Can be <see
        /// langword="null"/> if no ordering is required.</param>
        public RelmForeignKey(string[] foreignKeys = null, string[] localKeys = null, string[] orderBy = null)
        {
            this.ForeignKeys = foreignKeys;
            this.LocalKeys = localKeys;
            this.OrderBy = orderBy;
        }
    }
}
