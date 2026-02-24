using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.RelmInternal.Models;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Extensions
{
    internal static class ForeignKeyExtensions
    {

        /// <summary>
        /// Retrieves navigation and foreign key mapping options for a collection of entities, enabling resolution of
        /// relationships between the provided items and their related entities.
        /// </summary>
        /// <remarks>This method analyzes the provided collection and its type metadata to determine the
        /// appropriate navigation and foreign key properties. It supports both principal and dependent entity
        /// configurations, and will throw exceptions if required attributes or keys are missing. The returned options
        /// can be used to facilitate relationship resolution in data access scenarios.</remarks>
        /// <typeparam name="T">The type of the entities in the collection for which foreign key navigation options are to be determined.</typeparam>
        /// <param name="_items">The collection of entities for which to resolve foreign key navigation options. Cannot be null.</param>
        /// <returns>A ForeignKeyNavigationOptions instance containing metadata about the navigation properties, foreign key
        /// properties, and primary key values for the specified collection.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the initial expression is not a lambda expression in the form of 'x => x.PropertyName'.</exception>
        /// <exception cref="Exception">Thrown if no primary keys or reference keys are found for the provided collection.</exception>
        /// <exception cref="MemberAccessException">Thrown if the foreign key referenced by the RelmForeignKey attribute cannot be found.</exception>
        public static ForeignKeyNavigationOptions GetForeignKeyNavigationOptions<T>(this IRelmExecutionCommand command, ICollection<T>? _items)
        {
            if (command.ExecutionExpression == null)
                throw new InvalidOperationException("Execution expression cannot be null.");

            var relmCommand = command as RelmExecutionCommand
                ?? throw new InvalidOperationException("Command must be of type RelmExecutionCommand.");

            var navigationOptions = new ForeignKeyNavigationOptions
            {
                ReferenceProperty = relmCommand.ExecutionExpression as MemberExpression
                    ?? throw new InvalidOperationException("Collection must be represented by a lambda expression in the form of 'x => x.PropertyName'.")
            };

            // if foreign key attribute on the current item's property, then we have principal resolution
            var principalReslolutionForeignKey = navigationOptions.ReferenceProperty.Member.GetCustomAttribute<RelmForeignKey>();

            // get all RelmKeys on the main object
            navigationOptions.ReferenceKeys = relmCommand.GetReferenceKeys<T>(principalReslolutionForeignKey?.LocalKeys);

            // go through all items in the current data set and collect all relmkey values
            navigationOptions.ItemPrimaryKeys = [.. _items
                ?.Select(x => x
                    ?.GetType()
                    .GetProperties()
                    .Intersect(navigationOptions.ReferenceKeys)
                    .Select(y => new Tuple<PropertyInfo, object?>(y, y.GetValue(x)))
                    .Where(y => y.Item2 != null)
                    .Cast<Tuple<PropertyInfo, object>>()
                    .ToList()
                    ?? [])
                ?? []];

            //if ((itemPrimaryKeys?.Count ?? 0) <= 0)
            if (navigationOptions.ItemPrimaryKeys == null)
                throw new Exception("No primary keys found.");

            var targetProperties = navigationOptions.ReferenceType?.GetProperties() ?? [];

            // make a list of all targetProperties that are of type T
            var targetPropertiesOfTypeT = targetProperties
                .Where(x => x.PropertyType == typeof(T) || x.PropertyType.GetGenericArguments().Contains(typeof(T)))
                .ToList();

            // dependent entity has foreign key attribute/navigation property instead of principal entity
            if (principalReslolutionForeignKey == null)
            {
                var defaultLocalKeys = targetProperties
                    .Where(x => x.GetCustomAttribute<RelmKey>() != null)
                    .Select(x => x.Name)
                    .ToArray();

                // get all properties on target that have a RelmForeignKey attribute and make dictionary with LocalKeys as keys
                var targetForeignKeyDecorators = targetProperties
                    .Where(x => x.GetCustomAttribute<RelmForeignKey>() != null)
                    .ToDictionary(x => x, x => x.GetCustomAttribute<RelmForeignKey>())
                    .Segment((prev, next, i) => !(prev.Value?.LocalKeys ?? defaultLocalKeys).All(x => (next.Value?.LocalKeys ?? defaultLocalKeys).Contains(x)))
                    .ToDictionary(x => x.FirstOrDefault().Value?.LocalKeys ?? defaultLocalKeys, x => x.ToDictionary(y => y.Key, y => y.Value?.ForeignKeys));

                // find any navigation properties that are the same type as this data set
                var navigationProps = targetPropertiesOfTypeT
                    .Where(x => targetForeignKeyDecorators.Any(y => y.Key.Contains(x.Name)))
                    .ToList();

                // TODO: allow multiple navigation properties on target class
                if (navigationProps.Count > 1)
                    throw new Exception("Multiple navigation properties found.");

                if (navigationProps.Count == 0)
                {
                    // we're using navigation properties
                    /*
                    navigationProps = targetPropertiesOfTypeT
                        .Where(x => targetForeignKeyDecorators.Any(y => y.Value.ContainsKey(x)))
                        .ToList();
                    */
                    navigationOptions.ForeignKeyProperties = targetForeignKeyDecorators
                        .Select(x => targetProperties.Where(y => x.Key.Contains(y.Name)).ToArray())
                        .FirstOrDefault();

                    navigationOptions.ReferenceKeys = relmCommand.GetReferenceKeys<T>(targetForeignKeyDecorators
                        .SelectMany(x => x.Value.Select(y => y.Value).ToArray())
                        .FirstOrDefault());

                    navigationOptions.ItemPrimaryKeys = [.. _items
                        ?.Select(x => x
                            ?.GetType()
                            .GetProperties()
                            .Intersect(navigationOptions.ReferenceKeys)
                            .Select(y => new Tuple<PropertyInfo, object?>(y, y.GetValue(x)))
                            .Where(y => y.Item2 != null)
                            .Cast<Tuple<PropertyInfo, object>>()
                            .ToList()
                            ?? [])
                        ?? []];
                }
                else
                {
                    // we're using foreign key properties
                    navigationOptions.ForeignKeyProperties = targetForeignKeyDecorators
                        .Select(x => x.Value.Keys.ToArray())
                        .FirstOrDefault();

                    navigationOptions.ReferenceKeys = relmCommand.GetReferenceKeys<T>([.. targetForeignKeyDecorators.SelectMany(x => x.Value.SelectMany(y => y.Value ?? []).ToArray())]);

                    navigationOptions.ItemPrimaryKeys = [.. _items
                        ?.Select(x => x
                            ?.GetType()
                            .GetProperties()
                            .Intersect(navigationOptions.ReferenceKeys)
                            .Select(y => new Tuple<PropertyInfo, object?>(y, y.GetValue(x)))
                            .Where(y => y.Item2 != null)
                            .Cast<Tuple<PropertyInfo, object>>()
                            .ToList()
                            ?? [])
                        ?? []];
                }

                //navigationOptions.NavigationProperty = navigationProps.FirstOrDefault();
            }
            else
            {
                // get the principal entity's foreign key property
                navigationOptions.ForeignKeyProperties = [.. targetProperties.Where(x => principalReslolutionForeignKey.ForeignKeys?.Contains(x.Name) ?? false)];
                //navigationOptions.NavigationProperty = targetPropertiesOfTypeT.FirstOrDefault(); //.Values.FirstOrDefault();
            }

            // check required variables have something in them
            if ((navigationOptions.ForeignKeyProperties?.Length ?? 0) <= 0)
                throw new MemberAccessException("Foreign key referenced by RelmForeignKey attribute could not be found.");
            /*
            if (navigationOptions.NavigationProperty == null)
                throw new MemberAccessException("Navigation property referenced by RelmForeignKey attribute could not be found.");
            */

            if ((navigationOptions.ItemPrimaryKeys?.Count ?? 0) <= 0)
                throw new Exception("No primary keys found.");

            if ((navigationOptions.ReferenceKeys?.Length ?? 0) <= 0)
                throw new Exception("No reference keys found.");

            return navigationOptions;
        }
    }
}
