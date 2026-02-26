using MoreLinq;
using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Operations
{
    internal class DataNamingHelper
    {
        // The regular expression search and replace strings to turn "CapitalCase" property names into "underscore_case" column names
        //public static string UnderscoreSearchPattern => @"(?<!_|^|Internal)([A-Z])|(?<=[a-zA-Z])(?=\d)";
        public static string UnderscoreSearchPattern => @"(?<!_|^|Internal)([A-Z]|[0-9]+)";
        public static string UnderscoreReplacePattern => @"_$1";

        /// <summary>
        /// Takes in an object and gets the full info about its properties, including the underscore names.
        /// </summary>
        /// <param name="TargetObject">The object to pull the properties from.</param>
        /// <param name="GetOnlyDbResolvables">Indicate to get only properties marked with the DALResolvable attribute.</param>
        /// <param name="GetOnlyNonVirtualColumns">Indicate to get only non-virtual properties.</param>
        /// <returns>The full list of property info including underscore names.</returns>
        public static List<KeyValuePair<string, Tuple<string, PropertyInfo>>> GetUnderscoreProperties(object TargetObject, bool GetOnlyDbResolvables = true, bool GetOnlyNonVirtualColumns = true)
        {
            return GetUnderscoreProperties(TargetObject.GetType(), GetOnlyDbResolvables, GetOnlyNonVirtualColumns: GetOnlyNonVirtualColumns);
        }

        public static List<KeyValuePair<string, Tuple<string, PropertyInfo>>> GetUnderscoreProperties<T>(bool GetOnlyRelmColumns = true, bool GetOnlyNonVirtualColumns = true)
        {
            return GetUnderscoreProperties(typeof(T), GetOnlyRelmColumns: GetOnlyRelmColumns, GetOnlyNonVirtualColumns: GetOnlyNonVirtualColumns);
        }

        public static List<KeyValuePair<string, Tuple<string, PropertyInfo>>> GetUnderscoreProperties(Type TargetType, bool GetOnlyRelmColumns = true, bool GetOnlyNonVirtualColumns = true)
        {
            // get all properties marked with the DALResolvable attribute
            var convertableProperties = new List<PropertyInfo>();
            foreach (var x in TargetType.GetProperties())
            {
                var customAttributes = x.GetCustomAttributes(true);
                var addItem = !GetOnlyRelmColumns;
                if (!addItem)
                {
                    foreach (var customAttribute in customAttributes)
                    {
                        var attributeType = customAttribute.GetType();
                        if (attributeType == typeof(RelmColumn))
                        {
                            var relmColumn = (RelmColumn)customAttribute;
                            if (!GetOnlyNonVirtualColumns || (GetOnlyNonVirtualColumns && !relmColumn.Virtual))
                            {
                                addItem = true;
                                break;
                            }
                        }
                    }
                }

                if (addItem)
                    convertableProperties.Add(x);
            }

            // get the underscore names of all properties, add "_#" to the end of duplicate property names
            /*
            var underscoreNames = convertableProperties
                .Select(x => new Tuple<string?, PropertyInfo>(x.Name == "InternalId" 
                        ? x.Name 
                        : (string.IsNullOrWhiteSpace(x.GetCustomAttribute<RelmColumn>(true)?.ColumnName)
                            ? Regex.Replace(x.Name, UnderscoreSearchPattern, UnderscoreReplacePattern)
                            : x.GetCustomAttribute<RelmColumn>(true)?.ColumnName?.Trim())
                    , x))
                .OrderBy(x => x.Item1)
                .Segment((prev, next, index) => prev.Item1 != next.Item1)
                .SelectMany(x => x.Count() == 1
                    ? x
                    : x.Take(1)
                        .Concat(x
                            .Skip(1)
                            .Select((y, index) => new Tuple<string, PropertyInfo>($"{y.Item1}_{index}", y.Item2)))
                )
                .ToDictionary(x => x.Item1, x => new Tuple<string?, PropertyInfo>(x.Item2.Name, x.Item2))
                .ToList();
            */
            var columnNamedProperties = new List<Tuple<string, PropertyInfo>>();
            foreach (var x in convertableProperties)
            {
                if (x.Name.Equals("InternalId", StringComparison.OrdinalIgnoreCase))
                {
                    columnNamedProperties.Add(new Tuple<string, PropertyInfo>(x.Name, x));
                    continue;
                }

                var columnName = x.GetCustomAttribute<RelmColumn>(true)?.ColumnName?.Trim();
                if (string.IsNullOrWhiteSpace(columnName))
                    columnName = Regex.Replace(x.Name, UnderscoreSearchPattern, UnderscoreReplacePattern);

                columnNamedProperties.Add(new Tuple<string, PropertyInfo>(columnName, x));
            }

            var orderedColumnNamedProperties = columnNamedProperties
                .OrderBy(x => x.Item1)
                .ToList();

            var groupedColumnNamedProperties = orderedColumnNamedProperties
                .GroupBy(x => x.Item1)
                .ToDictionary(x => x.Key, x => x.ToList());

            var selectManyProperties = new Dictionary<string, Tuple<string, PropertyInfo>>();
            foreach (var kvp in groupedColumnNamedProperties)
            {
                var columnName = kvp.Key;
                var properties = kvp.Value;

                for (int i = 0; i < properties.Count; i++)
                {
                    var property = properties[i];
                    var itemKey = property.Item1;

                    if (i > 0)
                        itemKey = $"{property.Item1}_{i - 1}";

                    selectManyProperties[itemKey] = new Tuple<string, PropertyInfo>(property.Item2.Name, property.Item2);
                }
            }

            var underscoreNames = selectManyProperties.ToList();

            return underscoreNames;
        }
    }
}
