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
        public static string UnderscoreSearchPattern => @"(?<!_|^|Internal)([A-Z])";
        public static string UnderscoreReplacePattern => @"_$1";

        /// <summary>
        /// Takes in an object and gets the full info about its properties, including the underscore names.
        /// </summary>
        /// <param name="TargetObject">The object to pull the properties from.</param>
        /// <param name="GetOnlyDbResolvables">Indicate to get only properties marked with the DALResolvable attribute.</param>
        /// <returns>The full list of property info including underscore names.</returns>
        public static List<KeyValuePair<string, Tuple<string, PropertyInfo>>> GetUnderscoreProperties(object TargetObject, bool GetOnlyDbResolvables = true, bool GetOnlyNonVirtualColumns = true)
        {
            return GetUnderscoreProperties(TargetObject.GetType(), GetOnlyDbResolvables, GetOnlyNonVirtualColumns: GetOnlyNonVirtualColumns);
        }

        public static List<KeyValuePair<string, Tuple<string, PropertyInfo>>> GetUnderscoreProperties<T>(bool GetOnlyDbResolvables = true, bool GetOnlyNonVirtualColumns = true)
        {
            return GetUnderscoreProperties(typeof(T), GetOnlyDbResolvables: GetOnlyDbResolvables, GetOnlyNonVirtualColumns: GetOnlyNonVirtualColumns);
        }

        public static List<KeyValuePair<string, Tuple<string, PropertyInfo>>> GetUnderscoreProperties(Type TargetType, bool GetOnlyDbResolvables = true, bool GetOnlyNonVirtualColumns = true)
        {
            // get all properties marked with the DALResolvable attribute
            /*
            var convertableProperties = TargetType
                .GetProperties()
                .Where(x => !GetOnlyDbResolvables || x.GetCustomAttributes(true).Any(y => y.GetType() == typeof(RelmColumn) && (!GetOnlyNonVirtualColumns || (GetOnlyNonVirtualColumns && !((RelmColumn)y).Virtual))))
                .ToList();
            */
            var convertableProperties = new List<PropertyInfo>();
            foreach (var x in TargetType.GetProperties())
            {
                var customAttributes = x.GetCustomAttributes(true);
                var addItem = !GetOnlyDbResolvables;
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
            var underscoreNames = convertableProperties
                //.ToDictionary(x => x.Name.StartsWith("InternalId") ? x.Name : Regex.Replace(x.Name, UnderscoreSearchPattern, UnderscoreReplacePattern), x => new Tuple<string, PropertyInfo>(x.Name, x))
                .Select(x => new Tuple<string, PropertyInfo>(x.Name.StartsWith("InternalId") 
                        ? x.Name 
                        : (string.IsNullOrWhiteSpace(x.GetCustomAttribute<RelmColumn>(true).ColumnName)
                            ? Regex.Replace(x.Name, UnderscoreSearchPattern, UnderscoreReplacePattern)
                            : x.GetCustomAttribute<RelmColumn>(true).ColumnName.Trim())
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
                .ToDictionary(x => x.Item1, x => new Tuple<string, PropertyInfo>(x.Item2.Name, x.Item2))
                .ToList();

            return underscoreNames;
        }
    }
}
