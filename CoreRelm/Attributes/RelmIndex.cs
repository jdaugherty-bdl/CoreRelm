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
    /// Specifies that the decorated class or struct should have an index created on the specified properties when used
    /// with a Relm database.
    /// </summary>
    /// <remarks>Apply this attribute to a class or struct to indicate which properties should be indexed for
    /// improved query performance. Indexing can speed up lookups on the specified properties but may increase storage
    /// requirements and affect write performance. This attribute is intended for use with the Relm object
    /// database.</remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property, AllowMultiple = true)]
    public sealed class RelmIndex<T> : RelmIndexBase
    {
        /// <summary>
        /// Initializes a new instance of the RelmIndex class with the specified index key and optional index
        /// configuration settings.
        /// </summary>
        /// <param name="indexKey">The key value that uniquely identifies the index. Cannot be null.</param>
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
        /// <exception cref="ArgumentException">Thrown if indexKey is null.</exception>
        public RelmIndex(
            T indexKey,
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
            null,
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
            if (indexKey == null)
                throw new ArgumentException("Index key cannot be null.", nameof(indexKey));

            IndexKeyHolder = indexKey;
        }
    }

    /// <summary>
    /// Non-keyed Relm index attribute for indexes that do not need to be paired with others.
    /// </summary>
    /// <remarks>Use this when no correlation key is required. All configuration options are identical
    /// to the generic version, except no index key is supplied.</remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class RelmIndex : RelmIndexBase
    {
        /// <summary>
        /// Initializes a new instance of the non-keyed RelmIndex with optional index configuration settings.
        /// </summary>
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
        public RelmIndex(
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
                null,
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
            // No pairing key; ensure holder is null for clarity.
            IndexKeyHolder = null;
        }
    }
}
