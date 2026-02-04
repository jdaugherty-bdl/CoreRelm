using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Attributes
{
    /// <summary>
    /// Specifies that the decorated class or struct must have compound unique values for the specified properties.
    /// </summary>
    /// <remarks>Apply this attribute to a class or struct to indicate that a combination of the specified
    /// properties should be unique across all instances. This is typically used to enforce uniqueness constraints in
    /// data models or persistence layers.</remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class RelmUnique : Attribute
    {
        /// <summary>
        /// Gets or sets the names of the properties that are subject to constraints.
        /// </summary>
        public string[]? ConstraintProperties { get; set; }

        /// <summary>
        /// Initializes a new instance of the RelmUnique class with the specified constraint properties.
        /// </summary>
        /// <param name="constraintProperties">An array of property names that define the uniqueness constraint. Cannot be null or empty.</param>
        /// <exception cref="ArgumentException">Thrown if constraintProperties is null or contains no elements.</exception>
        public RelmUnique(string[] constraintProperties)
        {
            if ((constraintProperties?.Length ?? 0) == 0)
                throw new ArgumentException("RelmUnique constraint properties must be provided.", nameof(constraintProperties));

            ConstraintProperties = constraintProperties;
        }
    }
}
