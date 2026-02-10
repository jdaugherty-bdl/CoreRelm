using CoreRelm.Attributes.BaseClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Indexes;

namespace CoreRelm.Attributes
{
    /// <summary>
    /// Non-keyed simple Relm index attribute for indexes only name columns and index properties, but 
    /// don't need any per-column settings.
    /// </summary>
    /// <remarks>Use this when no correlation key is required. All configuration options are identical
    /// to the generic version, except no index key is supplied.</remarks>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true)]
    public class RelmIndexNamed : RelmIndexBase
    {
        /// <summary>
        /// Initializes a new instance of the non-keyed RelmIndex with required indexed property names and optional index configuration settings.
        /// </summary>
        /// <param name="indexedPropertyNames">Required array of property names to be included in the index. Cannot be null or empty.</param>
        /// <param name="indexName">The name of the index. If null, a default name may be used.</param>
        /// <param name="uniquenessType">Specifies whether the index enforces uniqueness constraints.</param>
        /// <param name="indexType">The type of index to create, such as primary or secondary.</param>
        /// <param name="keyBlockSize">The size, in bytes, of the key block for the index. Specify -1 to use the default size.</param>
        /// <param name="parserName">The name of the parser to use for the index, or null to use the default parser.</param>
        /// <param name="comment">An optional comment describing the index.</param>
        /// <param name="visibility">Specifies the visibility of the index, such as public or private.</param>
        /// <param name="engineAttribute">An optional engine-specific attribute for the index.</param>
        /// <param name="secondaryEngineAttribute">An optional secondary engine-specific attribute for the index.</param>
        /// <param name="algorithmType">The algorithm to use for the index, or Algorithm.None to use the default.</param>
        /// <param name="lockOption">The locking option to use for the index, or LockOption.None for the default behavior.</param>
        public RelmIndexNamed(
            string[] indexedPropertyNames,
            string indexName = null,
            IndexType indexType = IndexType.None,
            int keyBlockSize = -1,
            string parserName = null,
            string comment = null,
            Visibility visibility = Visibility.None,
            string engineAttribute = null,
            string secondaryEngineAttribute = null,
            Algorithm algorithmType = Algorithm.None,
            LockOption lockOption = LockOption.None)
            : base(
                indexedPropertyNames,
                indexName,
                indexType,
                keyBlockSize,
                parserName,
                comment,
                visibility,
                engineAttribute,
                secondaryEngineAttribute,
                algorithmType,
                lockOption)
        {
            if (indexedPropertyNames is null || indexedPropertyNames.Length == 0)
                throw new ArgumentException("You must supply at least one indexed property name.", nameof(indexedPropertyNames));

            IndexKeyHolder = null; // explicitly non-keyed
        }
    }
}
