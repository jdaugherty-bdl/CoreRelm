using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Extensions
{
    public static class EnumExtensions
    {
        public static string ToDescriptionString(this Enum value)
        {
            FieldInfo? field = value.GetType().GetField(value.ToString());

            if (field != null)
            {
                var attribute = field.GetCustomAttribute<DescriptionAttribute>();
                if (attribute != null)
                {
                    return attribute.Description;
                }
            }

            return value.ToString(); // Fallback to default name if no attribute exists
        }

        public static T? ParseEnumerationDescription<T>(this string description, bool ignoreCase = false) where T : Enum
        {
            if (string.IsNullOrWhiteSpace(description))
                return default;

            foreach (var field in typeof(T).GetFields())
            {
                if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                {
                    if (string.Equals(attribute.Description, description, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                    {
                        return (T)field.GetValue(null)!;
                    }
                }
                else if (string.Equals(field.Name, description, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    // Fallback: Check the actual member name if no attribute exists
                    return (T)field.GetValue(null)!;
                }
            }

            throw new ArgumentException($"No enum member with description '{description}' found in {typeof(T).Name}.");
        }
    }
}
