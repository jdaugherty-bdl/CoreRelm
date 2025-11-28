using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Attributes
{
    /// <summary>
    /// Specifies that a class or struct represents a database entity and associates it with a database name.
    /// </summary>
    /// <remarks>This attribute is used to annotate classes or structs that correspond to database entities. 
    /// The <see cref="DatabaseName"/> property specifies the name of the database associated with the entity.</remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class RelmDatabase : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        public string DatabaseName { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelmDatabase"/> class with the specified database name.
        /// </summary>
        /// <param name="databaseName">The name of the database. This value cannot be null, empty, or consist only of white-space characters.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="databaseName"/> is null, empty, or consists only of white-space characters.</exception>
        public RelmDatabase(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentNullException(nameof(databaseName));

            DatabaseName = databaseName;
        }
    }
}
