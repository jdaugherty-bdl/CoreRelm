using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Utilities
{
    internal static class CoreUtilities
    {
        /// <summary>
        /// Gets an error message indicating that the table name could not be retrieved from the class.
        /// </summary>
        internal static string NoDalTableAttributeError => "Cannot get table name from class, try adding a 'RelmTable' attribute.";

        /// <summary>
        /// Gets an error message indicating that no column properties were found in the class.
        /// </summary>
        internal static string NoDalPropertyAttributeError => "Cannot find any column properties in class, try adding a 'RelmColumn' attribute.";

        /// <summary>
        /// Converts a scalar value to the specified type <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>This method supports conversion to common types such as <see cref="string"/>, <see
        /// cref="int"/>, <see cref="long"/>,  <see cref="decimal"/>, <see cref="float"/>, <see cref="bool"/>, <see
        /// cref="DateTime"/>, and enums.  For <see cref="bool"/>, the method interprets non-zero numeric or non-empty
        /// string values as <see langword="true"/>.</remarks>
        /// <typeparam name="T">The target type to which the scalar value should be converted.</typeparam>
        /// <param name="scalarValue">The scalar value to convert. Can be of any type, including <see langword="null"/> or <see cref="DBNull"/>.</param>
        /// <returns>The converted value of type <typeparamref name="T"/>. If <paramref name="scalarValue"/> is <see
        /// langword="null"/> or <see cref="DBNull"/>,  the default value of <typeparamref name="T"/> is returned. For
        /// nullable types, <see langword="null"/> is returned in such cases.</returns>
        internal static object ConvertScalar<T>(object scalarValue)
        {
            if (scalarValue == null || scalarValue is DBNull)
                return default(T);
            else if (typeof(T) == typeof(string))
                return scalarValue.ToString();
            else if (typeof(T) == typeof(int))
                return int.TryParse(scalarValue.ToString(), out int scalar) ? scalar : default;
            else if (typeof(T) == typeof(int?))
                return int.TryParse(scalarValue.ToString(), out int scalar) ? (int?)scalar : null;
            else if (typeof(T) == typeof(long))
                return long.TryParse(scalarValue.ToString(), out long scalar) ? scalar : default;
            else if (typeof(T) == typeof(decimal))
                return decimal.TryParse(scalarValue.ToString(), out decimal scalar) ? scalar : default;
            else if (typeof(T) == typeof(float))
                return float.TryParse(scalarValue.ToString(), out float scalar) ? scalar : default;
            else if (typeof(T) == typeof(bool) && new[] { typeof(string), typeof(int), typeof(long) }.Contains(scalarValue.GetType()))
                return scalarValue.ToString() != "0";
            else if (typeof(T) == typeof(DateTime))
                return DateTime.TryParse(scalarValue.ToString(), out DateTime scalar) ? scalar : default;
            else if (typeof(T).IsEnum)
                return (T)Enum.Parse(typeof(T), scalarValue.ToString(), true);
            else
                return (T)scalarValue;
        }

        /// <summary>
        /// Adds all parameters from the specified dictionary to the current <see cref="MySqlParameterCollection"/>.
        /// </summary>
        /// <remarks>This method converts each key-value pair in the dictionary into a <see
        /// cref="MySqlParameter"/>  and adds them to the <see cref="MySqlParameterCollection"/>. If the dictionary is
        /// empty or  <see langword="null"/>, the method performs no operation.</remarks>
        /// <param name="commandParameters">The <see cref="MySqlParameterCollection"/> to which the parameters will be added.</param>
        /// <param name="parameters">A dictionary containing parameter names and their corresponding values.  The keys represent the parameter
        /// names, and the values represent the parameter values.  If <paramref name="parameters"/> is <see
        /// langword="null"/>, no parameters are added.</param>
        internal static void AddAllParameters(this MySqlParameterCollection commandParameters, Dictionary<string, object> parameters)
        {
            commandParameters
                .AddRange(
                    parameters?
                        .Select(x => new MySqlParameter(x.Key, x.Value))
                        .ToArray()
                    ??
                    Enumerable
                        .Empty<MySqlParameter>()
                        .ToArray());
        }

        /// <summary>
        /// Creates a delegate that instantiates an object of type <typeparamref name="T"/> using a parameterless
        /// constructor.
        /// </summary>
        /// <remarks>This method searches for a parameterless constructor in the type <typeparamref
        /// name="T"/> and generates a compiled expression to create instances of the type. If no parameterless
        /// constructor is available, an exception is thrown.</remarks>
        /// <typeparam name="T">The type of object to create.</typeparam>
        /// <returns>A <see cref="Func{T}"/> delegate that, when invoked, creates a new instance of <typeparamref name="T"/>.</returns>
        internal static Func<T> CreateCreatorExpression<T>()
        {
            var constructor = GetConstructorsRecursively(typeof(T))
                .FirstOrDefault(x => x.GetParameters().Length == 0);

            var constExpression = Expression.Lambda<Func<T>>(Expression.New(constructor));
            return constExpression.Compile();
        }

        /// <summary>
        /// Creates a compiled lambda expression that constructs an instance of the specified type <typeparamref
        /// name="T"/>  using a constructor that accepts two parameters of types <typeparamref name="TArg1"/> and
        /// <typeparamref name="TArg2"/>.
        /// </summary>
        /// <remarks>This method uses expression trees to generate a compiled lambda expression, which is
        /// typically faster than  using <see cref="Activator.CreateInstance"/> for repeated object creation. Ensure
        /// that the type <typeparamref name="T"/>  has a public constructor that matches the specified parameter types;
        /// otherwise, an exception will be thrown at runtime.</remarks>
        /// <typeparam name="TArg1">The type of the first parameter required by the constructor.</typeparam>
        /// <typeparam name="TArg2">The type of the second parameter required by the constructor.</typeparam>
        /// <typeparam name="T">The type of the object to be created.</typeparam>
        /// <returns>A function delegate that takes two arguments of types <typeparamref name="TArg1"/> and <typeparamref
        /// name="TArg2"/>  and returns an instance of type <typeparamref name="T"/>.</returns>
        internal static Func<TArg1, TArg2, T> CreateCreatorExpression<TArg1, TArg2, T>()
        {
            var typeList = new Type[] { typeof(TArg1), typeof(TArg2) };

            // Lambda Expressions are much faster than Activator.CreateInstance when creating more than one object due to Expression caching

            // get object constructor
            var constructor = typeof(T).GetConstructor(typeList);

            // define individual parameters
            var parameterList = new ParameterExpression[]
            {
                Expression.Parameter(typeof(TArg1)),
                Expression.Parameter(typeof(TArg2))
            };

            // create the expression
            var creatorExpression = Expression.Lambda<Func<TArg1, TArg2, T>>(Expression.New(constructor, parameterList), parameterList);

            // compile the expression
            return creatorExpression.Compile();
        }

        /// <summary>
        /// Creates a compiled lambda expression that constructs an instance of the specified type <typeparamref
        /// name="T"/>  using a constructor that accepts four parameters of types <typeparamref name="TArg1"/>,
        /// <typeparamref name="TArg2"/>,  <typeparamref name="TArg3"/>, and <typeparamref name="TArg4"/>.
        /// </summary>
        /// <remarks>This method is optimized for scenarios where multiple instances of <typeparamref
        /// name="T"/> need to be created,  as the compiled lambda expression is significantly faster than using <see
        /// cref="Activator.CreateInstance"/>  for repeated object creation.</remarks>
        /// <typeparam name="TArg1">The type of the first parameter required by the constructor.</typeparam>
        /// <typeparam name="TArg2">The type of the second parameter required by the constructor.</typeparam>
        /// <typeparam name="TArg3">The type of the third parameter required by the constructor.</typeparam>
        /// <typeparam name="TArg4">The type of the fourth parameter required by the constructor.</typeparam>
        /// <typeparam name="T">The type of the object to be created.</typeparam>
        /// <returns>A compiled lambda expression that, when invoked, creates an instance of <typeparamref name="T"/>  using the
        /// specified constructor.</returns>
        internal static Func<TArg1, TArg2, TArg3, TArg4, T> CreateCreatorExpression<TArg1, TArg2, TArg3, TArg4, T>()
        {
            // Lambda Expressions are much faster than Activator.CreateInstance when creating more than one object due to Expression caching

            // get object constructor
            var constructor = typeof(T).GetConstructor(new Type[] { typeof(TArg1), typeof(TArg2), typeof(TArg3), typeof(TArg4) });

            // define individual parameters
            var parameterList = new ParameterExpression[]
            {
                Expression.Parameter(typeof(TArg1)),
                Expression.Parameter(typeof(TArg2)),
                Expression.Parameter(typeof(TArg3)),
                Expression.Parameter(typeof(TArg4))
            };

            // create the expression
            var creatorExpression = Expression.Lambda<Func<TArg1, TArg2, TArg3, TArg4, T>>(Expression.New(constructor, parameterList), parameterList);

            // compile the expression
            return creatorExpression.Compile();
        }

        /// <summary>
        /// Retrieves a list of all constructors defined in the specified type and its base types, recursively.
        /// </summary>
        /// <remarks>This method retrieves both public and non-public constructors of the specified type
        /// and its base types. The constructors are returned in the order they are discovered, starting with the
        /// specified type and moving up the inheritance hierarchy.</remarks>
        /// <param name="type">The <see cref="Type"/> for which to retrieve the constructors. If <paramref name="type"/> is <see
        /// langword="null"/>, an empty list is returned.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="ConstructorInfo"/> objects representing the constructors of the
        /// specified type and its base types. The list will be empty if the specified type is <see langword="null"/> or
        /// if no constructors are found.</returns>
        internal static List<ConstructorInfo> GetConstructorsRecursively(Type type)
        {
            List<ConstructorInfo> allConstructors = new List<ConstructorInfo>();

            if (type == null)
                return allConstructors;

            // Get the constructors of the current type and add them to the list
            ConstructorInfo[] constructors = type.GetConstructors();
            allConstructors.AddRange(constructors);

            // Recursively get the constructors of the base type and add them to the list
            List<ConstructorInfo> baseTypeConstructors = GetConstructorsRecursively(type.BaseType);
            allConstructors.AddRange(baseTypeConstructors);

            return allConstructors;
        }
    }
}
