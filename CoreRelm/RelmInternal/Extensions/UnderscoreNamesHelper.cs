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
        public static string UppercaseSearchPattern => @"(?<!_|^|Internal)([A-Z])";
        public static string ReplacePattern => @"_$1";

        public static List<KeyValuePair<string, Tuple<string, PropertyInfo>>> ConvertPropertiesToUnderscoreNames(Type DataType, bool ForceLowerCase = false, bool GetOnlyDalResolvables = true)
        {
            // get all potential properties that can be converted to data
            var convertableProperties = DataType
                .GetProperties()
                .Where(x => !GetOnlyDalResolvables || x.GetCustomAttribute<RelmColumn>() != null)
                .ToList();

            // get the underscore names and type info of the properties
            var convertableList = convertableProperties
                .ToDictionary(x => x.Name.StartsWith("InternalId")
                        ? x.Name
                        : Regex.Replace(x.Name, UppercaseSearchPattern, ReplacePattern),
                    x => new Tuple<string, PropertyInfo>(x.Name, x))
                .Select(x => new KeyValuePair<string, Tuple<string, PropertyInfo>>(ForceLowerCase
                        ? x.Key.ToLower().Replace("internalid", "InternalId")
                        : x.Key,
                    x.Value))
                .ToList();

            return convertableList;
        }

        public static List<KeyValuePair<string, Tuple<string, PropertyInfo>>> ConvertPropertiesToUnderscoreNames<T>(this T RowData, bool ForceLowerCase = false)
        {
            if (RowData == null)
                throw new ArgumentNullException(nameof(RowData), "RowData cannot be null when converting properties to underscore names.");

            return ConvertPropertiesToUnderscoreNames(RowData.GetType(), ForceLowerCase: ForceLowerCase, GetOnlyDalResolvables: true);
        }
    }
}
