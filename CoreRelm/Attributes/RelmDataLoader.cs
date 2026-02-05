using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Attributes
{
    /// <summary>
    /// Specifies metadata for associating a data loader with a property, struct, or class.
    /// </summary>
    /// <remarks>This attribute is used to define the type of a data loader and the key fields that the loader
    /// uses to identify or retrieve data. It can be applied to properties, structs, or classes, and supports multiple
    /// usages on the same target.</remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true)]
    public class RelmDataLoader : Attribute
    {
        /// <summary>
        /// Gets or sets the type of the loader used to process data or resources.
        /// </summary>
        /// <remarks>The specified type is expected to define the behavior for loading data or resources.
        /// Ensure  that the type is compatible with the intended usage context.</remarks>
        public Type? LoaderType { get; set; } = default;

        /// <summary>
        /// Gets or sets the collection of key field names used to uniquely identify an entity.
        /// </summary>
        /// <remarks>Key fields are typically used to identify unique records in a dataset or entity.
        /// Ensure that the field names provided correspond to valid and unique identifiers within the context of the
        /// entity.</remarks>
        public string[]? KeyFields { get; set; } = default;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelmDataLoader"/> class with the specified loader type.
        /// </summary>
        /// <param name="loaderType">The type of the loader to be used. This parameter cannot be <see langword="null"/>.</param>
        public RelmDataLoader(Type loaderType)
        {
            this.LoaderType = loaderType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelmDataLoader"/> class with the specified loader type and key
        /// field.
        /// </summary>
        /// <remarks>If <paramref name="keyField"/> is <see langword="null"/>, the <see cref="KeyFields"/>
        /// property will remain uninitialized.</remarks>
        /// <param name="loaderType">The type of the loader used to process data. This parameter cannot be <see langword="null"/>.</param>
        /// <param name="keyField">The name of the key field used to identify data. If not <see langword="null"/>, it will be added as the sole
        /// key field.</param>
        public RelmDataLoader(Type loaderType, string keyField)
        {
            this.LoaderType = loaderType;

            if (keyField != null)
                this.KeyFields = [keyField];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelmDataLoader"/> class with the specified loader type and key
        /// fields.
        /// </summary>
        /// <param name="loaderType">The type of the loader used to process the data. This must be a valid <see cref="Type"/> representing the
        /// loader implementation.</param>
        /// <param name="keyFields">An array of strings representing the key fields used to identify or process the data. This array cannot be
        /// null and must contain at least one element.</param>
        public RelmDataLoader(Type loaderType, string[] keyFields)
        {
            this.LoaderType = loaderType;
            this.KeyFields = keyFields;
        }
    }
}
