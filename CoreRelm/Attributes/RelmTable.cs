using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Attributes
{
    /// <summary>
    /// Specifies that a class or struct represents a table in a relational database and provides the name of the table.
    /// </summary>
    /// <remarks>This attribute is used to associate a class or struct with a specific table name in a
    /// relational database. The table name is specified via the <see cref="TableName"/> property.</remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class RelmTable : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the database table associated with this instance.
        /// </summary>
        public string TableName { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelmTable"/> class with the specified table name.
        /// </summary>
        /// <param name="tableName">The name of the table. This value cannot be null, empty, or consist only of white-space characters.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="tableName"/> is null, empty, or consists only of white-space characters.</exception>
        public RelmTable(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentNullException(nameof(tableName));

            TableName = tableName;
        }
    }
}
