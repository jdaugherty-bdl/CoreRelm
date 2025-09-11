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
        // message for the "no DALTable attribute" exception
        internal static string NoDalTableAttributeError => "Cannot get table name from class, try adding a 'DALTable' attribute.";
        // message for the "no DALResolvable attributes" exception
        internal static string NoDalPropertyAttributeError => "Cannot find any table properties in class, try adding a 'DALResolvable' attribute.";

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

        internal static void AddAllParameters(this MySqlParameterCollection CommandParameters, Dictionary<string, object> Parameters)
        {
            CommandParameters
                .AddRange(
                    Parameters?
                        .Select(x => new MySqlParameter(x.Key, x.Value))
                        .ToArray()
                    ??
                    Enumerable
                        .Empty<MySqlParameter>()
                        .ToArray());
        }

        internal static Func<T> CreateCreatorExpression<T>()
        {
            var constructor = GetConstructorsRecursively(typeof(T))
                .FirstOrDefault(x => x.GetParameters().Length == 0);

            var constExpression = Expression.Lambda<Func<T>>(Expression.New(constructor));
            return constExpression.Compile();
        }

        /// <summary>
        /// Creates a labmda expression to instantiate objects of type T which take two constructor parameters.
        /// </summary>
        /// <typeparam name="TArg1">First parameter type.</typeparam>
        /// <typeparam name="TArg2">Second parameter type.</typeparam>
        /// <typeparam name="T">Return type.</typeparam>
        /// <returns>An instantiation function that will create a new concrete object of type T.</returns>
        internal static Func<TArg1, TArg2, T> CreateCreatorExpression<TArg1, TArg2, T>()
        {
            //TODO: make this allow a variable number of TArgs
            var typeList = new Type[] { typeof(TArg1), typeof(TArg2) };

            // Lambda Expressions are much faster than Activator.CreateInstance when creating more than one object due to Expression caching

            // get object constructor
            //var constructor = typeof(T).GetConstructor(new Type[] { typeof(TArg1), typeof(TArg2) });
            var constructor = typeof(T).GetConstructor(typeList);
            /*
            var constructor = GetConstructorsRecursively(typeof(T))
                .Where(x => x
                    .GetParameters()
                    .Select(y => y.ParameterType)
                    .Intersect(typeList)
                    .Any())
                .FirstOrDefault();
            */

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
        /// Creates a labmda expression to instantiate objects of type T which take four constructor parameters.
        /// </summary>
        /// <typeparam name="TArg1">First parameter type.</typeparam>
        /// <typeparam name="TArg2">Second parameter type.</typeparam>
        /// <typeparam name="TArg3">Third parameter type.</typeparam>
        /// <typeparam name="TArg4">Fourth parameter type</typeparam>
        /// <typeparam name="T">Return type.</typeparam>
        /// <returns>An instantiation function that will create a new concrete object of type T.</returns>
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
