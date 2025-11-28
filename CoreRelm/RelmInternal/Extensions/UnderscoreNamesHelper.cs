using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Extensions
{
    internal static class UnderscoreNamesHelper
    {
        //TODO: update ReplacePattern to include option for converting to lowercase everything EXCEPT "InternalId"

        /// <summary>
        /// Gets the regular expression pattern used to identify uppercase letters  that are not preceded by an
        /// underscore, the start of a string, or the text "Internal".
        /// </summary>
        public static string UppercaseSearchPattern => @"(?<!_|^|Internal)([A-Z])";

        /// <summary>
        /// Gets the default replacement pattern used in string replacement operations.
        /// </summary>
        /// <remarks>This pattern is commonly used in regular expression replacements to prepend an
        /// underscore to the first captured group.</remarks>
        public static string ReplacePattern => @"_$1";

        /// <summary>
        /// Converts the properties of the specified type to a list of key-value pairs, where the keys are  property
        /// names formatted as underscore-separated strings, and the values contain the original  property name and its
        /// metadata.
        /// </summary>
        /// <remarks>The method processes the properties of the specified type, applying the following
        /// transformations: - If the property name starts with "InternalId", it is used as-is. - If the property has a
        /// <see cref="RelmColumn"/> attribute, the column name specified in the attribute    is used as the key. -
        /// Otherwise, the property name is converted to an underscore-separated format.  This method is useful for
        /// scenarios where property names need to be mapped to a specific naming convention  (e.g., database column
        /// names) while retaining metadata about the original properties.</remarks>
        /// <param name="dataType">The type whose properties will be converted.</param>
        /// <param name="forceLowerCase">A value indicating whether the resulting keys should be converted to lowercase.  If <see langword="true"/>,
        /// all keys will be lowercase except for "InternalId", which retains its casing.</param>
        /// <param name="getOnlyRelmColumns">A value indicating whether to include only properties marked with the <see cref="RelmColumn"/> attribute. 
        /// If <see langword="true"/>, only such properties are included; otherwise, all properties are considered.</param>
        /// <returns>A list of key-value pairs, where each key is the underscore-separated name of a property, and each value  is
        /// a tuple containing the original property name and its <see cref="PropertyInfo"/> metadata.</returns>
        public static List<KeyValuePair<string, Tuple<string, PropertyInfo>>> ConvertPropertiesToUnderscoreNames(Type dataType, bool forceLowerCase = false, bool getOnlyRelmColumns = true)
        {
            // get all potential properties that can be converted to data
            var convertableProperties = dataType
                .GetProperties()
                .Where(x => !getOnlyRelmColumns || x.GetCustomAttribute<RelmColumn>() != null)
                .ToList();

            // get the underscore names and type info of the properties
            var convertableList = convertableProperties
                .ToDictionary(x => x.Name.StartsWith("InternalId")
                        ? x.Name
                        : (x.GetCustomAttribute<RelmColumn>().ColumnName
                            ?? Regex.Replace(x.Name, UppercaseSearchPattern, ReplacePattern)),
                    x => new Tuple<string, PropertyInfo>(x.Name, x))
                .Select(x => new KeyValuePair<string, Tuple<string, PropertyInfo>>(forceLowerCase
                        ? x.Key.ToLower().Replace("internalid", "InternalId")
                        : x.Key,
                    x.Value))
                .ToList();

            return convertableList;
        }

        /// <summary>
        /// Converts the properties of the specified object to a list of key-value pairs,  where the keys are property
        /// names formatted with underscores and the values  contain additional metadata about the properties.
        /// </summary>
        /// <typeparam name="T">The type of the object whose properties are being converted.</typeparam>
        /// <param name="rowData">The object whose properties will be converted.</param>
        /// <param name="forceLowerCase">A value indicating whether the property names should be converted to lowercase.  If <see langword="true"/>,
        /// all property names will be in lowercase; otherwise,  the original casing is preserved.</param>
        /// <returns>A list of key-value pairs, where each key is the underscore-formatted name of a property,  and each value is
        /// a tuple containing additional metadata about the property.</returns>
        public static List<KeyValuePair<string, Tuple<string, PropertyInfo>>> ConvertPropertiesToUnderscoreNames<T>(this T rowData, bool forceLowerCase = false)
        {
            return ConvertPropertiesToUnderscoreNames(rowData.GetType(), forceLowerCase: forceLowerCase, getOnlyRelmColumns: true);
        }
    }
}
