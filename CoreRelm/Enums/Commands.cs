using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Enums
{
    /// <summary>
    /// Represents a collection of commands that can be used to define and manipulate query operations.
    /// </summary>
    /// <remarks>The <see cref="Commands.Command"/> enumeration provides a set of predefined commands commonly
    /// used  in query construction, such as filtering, ordering, grouping, and limiting results. These commands  can be
    /// used to specify the behavior of a query in a structured and consistent manner.</remarks>
    public class Commands
    {
        /// <summary>
        /// Represents the set of commands that can be used to define and manipulate query operations.
        /// </summary>
        /// <remarks>This enumeration provides a collection of commands commonly used in query-building
        /// scenarios,  such as filtering, sorting, grouping, and limiting results. Each command corresponds to a
        /// specific  operation that can be applied to a query.</remarks>
        public enum Command
        {
            /// <summary>
            /// Filters a sequence of values based on a predicate.
            /// </summary>
            /// <remarks>This method uses deferred execution. The query represented by this method is
            /// not executed until the resulting sequence is enumerated.</remarks>
            Where,
            /// <summary>
            /// Represents a reference to an object or entity.
            /// </summary>
            /// <remarks>This class is typically used to store and manage references to other objects
            /// or entities. It may include additional metadata or functionality depending on the specific
            /// implementation.</remarks>
            Reference,
            /// <summary>
            /// Sorts the elements of a sequence in ascending order according to a specified key.
            /// </summary>
            /// <remarks>This method performs a stable sort, meaning that if two elements have the
            /// same key, their original order in the sequence is preserved. To perform a descending sort, use the <see
            /// cref="Enumerable.OrderByDescending{TSource, TKey}(IEnumerable{TSource}, Func{TSource, TKey})"/>
            /// method.</remarks>
            OrderBy,
            /// <summary>
            /// Sorts the elements of a sequence in descending order according to a specified key.
            /// </summary>
            /// <remarks>This method performs a stable sort; that is, if two elements have the same
            /// key, their original order is preserved in the returned sequence.</remarks>
            OrderByDescending,
            /// <summary>
            /// Sets the value of the specified property or field.
            /// </summary>
            /// <remarks>This method allows dynamic assignment of values to properties or fields by
            /// name. Ensure that the property or field exists and is accessible in the current context.</remarks>
            Set,
            /// <summary>
            /// Sets the postfix string that will be appended to the output of the operation.
            /// </summary>
            SetPostfix,
            /// <summary>
            /// Groups the elements of a sequence according to a specified key selector function.
            /// </summary>
            /// <remarks>This method uses deferred execution. The query is not executed until the
            /// resulting collection is enumerated.</remarks>
            GroupBy,
            /// <summary>
            /// Gets or sets the maximum allowable limit result for the operation.
            /// </summary>
            Limit,
            /// <summary>
            /// Gets or sets the offset value used for positioning or alignment.
            /// </summary>
            Offset,
            /// <summary>
            /// Returns a collection of distinct elements from the input sequence based on a specified key selector.
            /// </summary>
            /// <remarks>This method uses deferred execution. The distinctness of elements is
            /// determined by comparing the keys returned by <paramref name="keySelector"/>. The order of the elements
            /// in the returned collection is preserved based on their first occurrence in the source
            /// sequence.</remarks>
            DistinctBy,
            /// <summary>
            /// Gets the number of elements contained in the collection.
            /// </summary>
            Count
        }
    }
}
