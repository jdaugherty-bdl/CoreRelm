using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Attributes
{
    /// <summary>
    /// Specifies metadata for a database column, including its name, size, constraints, and other properties.
    /// </summary>
    /// <remarks>This attribute is used to annotate properties or structs that represent database columns. It
    /// provides detailed configuration options such as column name, size, nullability, uniqueness, default values, and
    /// indexing. The attribute can be applied to define primary keys, auto-incrementing columns, and virtual columns,
    /// among other features.  When applied, the annotated property or struct is treated as a database column with the
    /// specified characteristics. This attribute is commonly used in object-relational mapping (ORM) scenarios to
    /// define the schema of a database table.</remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="RelmColumn"/> class, representing a column definition with
    /// various configuration options for database schema design.
    /// </remarks>
    /// <param name="columnType">The type of the column. If <see langword="null"/> or empty, the column will use the property's type.</param>
    /// <param name="columnName">The name of the column. If <see langword="null"/> or empty, the column will use the property's underscore name.</param>
    /// <param name="columnSize">The size of the column, typically used for fixed-length data types (e.g., strings). A value of -1 indicates
    /// that the size is unspecified.</param>
    /// <param name="compoundColumnSize">An array specifying the sizes of compound columns, if applicable. Can be <see langword="null"/> if the
    /// column is not part of a compound structure.</param>
    /// <param name="isNullable">A value indicating whether the column allows <see langword="null"/> values. The default is <see
    /// langword="true"/>.</param>
    /// <param name="primaryKey">A value indicating whether the column is part of the primary key. The default is <see langword="false"/>.</param>
    /// <param name="autonumber">A value indicating whether the column is an auto-incrementing identity column. The default is <see
    /// langword="false"/>.</param>
    /// <param name="unique">A value indicating whether the column enforces uniqueness. The default is <see langword="false"/>.</param>
    /// <param name="defaultValue">The default value for the column, represented as a string. Can be <see langword="null"/> if no default value
    /// is specified.</param>
    /// <param name="index">A value indicating whether the column is indexed. The default is <see langword="false"/>.</param>
    /// <param name="indexDescending">A value indicating whether the index on the column is sorted in descending order. The default is <see
    /// langword="false"/>.</param>
    /// <param name="allowDataTruncation">A value indicating whether data truncation is allowed for this column. The default is <see
    /// langword="false"/>.</param>
    /// <param name="isVirtual">A value indicating whether the column is a virtual column (i.e., its value is computed rather than stored).
    /// The default is <see langword="false"/>.</param>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Struct)]
    public sealed class RelmColumn(string? columnType = null, string? columnName = null, int columnSize = -1, int[]? compoundColumnSize = null, bool isNullable = true, bool primaryKey = false, bool autonumber = false, bool unique = false, string? defaultValue = null, bool index = false, bool indexDescending = false, bool allowDataTruncation = false, bool isVirtual = false) : Attribute
    {
        /// <summary>
        /// Gets or sets the database column type to use for this property. Will override default type mapping.
        /// </summary>
        public string? ColumnType { get; set; } = columnType;

        /// <summary>
        /// Gets the name of the database column associated with this instance. If not specified, the underscore version 
        /// of the property or struct name is used as the column name.
        /// </summary>
        public string? ColumnName { get; private set; } = columnName;

        /// <summary>
        /// Gets the size of the column, typically representing the maximum number of characters or bytes that the
        /// column can hold.
        /// </summary>
        public int ColumnSize { get; private set; } = columnSize;

        /// <summary>
        /// Gets the sizes of the compound columns, such as decimal.
        /// </summary>
        public int[]? CompoundColumnSize { get; private set; } = compoundColumnSize;

        /// <summary>
        /// Gets a value indicating whether data truncation is allowed during processing.
        /// </summary>
        public bool AllowDataTruncation { get; private set; } = allowDataTruncation;

        /// <summary>
        /// Gets a value indicating whether the associated entity or value can be null.
        /// </summary>
        public bool IsNullable { get; private set; } = isNullable;

        /// <summary>
        /// Gets a value indicating whether this property is the primary key of the entity.
        /// </summary>
        public bool PrimaryKey { get; private set; } = primaryKey;

        /// <summary>
        /// Gets a value indicating whether the entity is configured to automatically generate unique identifiers.
        /// </summary>
        public bool Autonumber { get; private set; } = autonumber;

        /// <summary>
        /// Gets a value indicating whether the column can only hold unique values.
        /// </summary>
        public bool Unique { get; private set; } = unique;

        /// <summary>
        /// Gets the default value associated with this column.
        /// </summary>
        public string? DefaultValue { get; private set; } = defaultValue;

        /// <summary>
        /// Gets a value indicating whether indexing is enabled for this column.
        /// </summary>
        public bool Index { get; private set; } = index;

        /// <summary>
        /// Gets a value indicating whether the index is sorted in descending order.
        /// </summary>
        public bool IndexDescending { get; private set; } = indexDescending;

        /// <summary>
        /// Gets a value indicating whether the object is virtual.
        /// </summary>
        public bool Virtual { get; private set; } = isVirtual;
    }
}
