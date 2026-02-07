using MoreLinq;
using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.RelmInternal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Commands;

namespace CoreRelm.Models
{
    /// <summary>
    /// Represents a command and its associated expression for execution within the Relm framework, supporting
    /// additional and child commands for complex execution scenarios.
    /// </summary>
    /// <remarks>A RelmExecutionCommand encapsulates a primary command and expression, and can aggregate
    /// additional related commands to support batch or hierarchical execution patterns. This class is typically used to
    /// construct and manage command trees or sequences for advanced data operations, such as those involving foreign
    /// key navigation or entity relationships. Thread safety is not guaranteed; if used concurrently, external
    /// synchronization is required.</remarks>
    public class RelmExecutionCommand : IRelmExecutionCommand
    {
        /// <summary>
        /// Gets the command that initiates execution for this operation.
        /// </summary>
        public Command ExecutionCommand { get; private set; }

        /// <summary>
        /// Gets the expression that represents the execution logic for this instance.
        /// </summary>
        public Expression? ExecutionExpression { get; private set; }

        /// <summary>
        /// Gets the number of child commands associated with this instance.
        /// </summary>
        public int ChildCommandCount => _childCommands?.Count ?? 0;

        private readonly List<RelmExecutionCommand> _childCommands = [];

        /// <summary>
        /// Initializes a new instance of the RelmExecutionCommand class.
        /// </summary>
        public RelmExecutionCommand()
        {
        }

        /// <summary>
        /// Initializes a new instance of the RelmExecutionCommand class with the specified command and expression.
        /// </summary>
        /// <param name="command">The command to be executed as part of the execution command. Cannot be null.</param>
        /// <param name="expression">The expression associated with the execution command. Cannot be null.</param>
        public RelmExecutionCommand(Command command, Expression? expression)
        {
            ExecutionCommand = command;
            ExecutionExpression = expression;
        }

        /// <summary>
        /// Adds an additional command and its associated expression to the current execution command sequence.
        /// </summary>
        /// <remarks>This method enables fluent chaining by returning the same instance after adding the
        /// command and expression.</remarks>
        /// <param name="command">The command to add to the execution sequence. Cannot be null.</param>
        /// <param name="expression">The expression associated with the command. Cannot be null.</param>
        /// <returns>The current <see cref="RelmExecutionCommand"/> instance with the additional command included.</returns>
        public RelmExecutionCommand AddAdditionalCommand(Command command, Expression? expression)
        {
            _childCommands.Add(new RelmExecutionCommand(command, expression));

            return this;
        }

        /// <summary>
        /// Adds an additional command to the execution pipeline using a strongly typed expression to specify the target
        /// property or member.
        /// </summary>
        /// <typeparam name="T">The type of the object that contains the member referenced by the expression.</typeparam>
        /// <param name="command">The command to add to the execution pipeline.</param>
        /// <param name="expression">An expression that identifies the property or member of type T to which the command applies.</param>
        /// <returns>The current <see cref="RelmExecutionCommand"/> instance, enabling method chaining.</returns>
        public RelmExecutionCommand AddAdditionalCommand<T>(Command command, Expression<Func<T, object>> expression)
        {
            AddAdditionalCommand(command, expression.Body);

            return this;
        }

        /// <summary>
        /// Gets the list of additional execution commands associated with this instance.
        /// </summary>
        /// <returns>A list of <see cref="RelmExecutionCommand"/> objects representing additional commands. The list may be empty
        /// if no additional commands are present.</returns>
        public List<RelmExecutionCommand> GetAdditionalCommands()
        {
            return _childCommands;
        }

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
        public ForeignKeyNavigationOptions GetForeignKeyNavigationOptions<T>(ICollection<T> _items)
        {
            var navigationOptions = new ForeignKeyNavigationOptions
            {
                ReferenceProperty = this.ExecutionExpression as MemberExpression
                    ?? throw new InvalidOperationException("Collection must be represented by a lambda expression in the form of 'x => x.PropertyName'.")
            };

            // if foreign key attribute on the current item's property, then we have principal resolution
            var principalReslolutionForeignKey = navigationOptions.ReferenceProperty.Member.GetCustomAttribute<RelmForeignKey>();

            // get all RelmKeys on the main object
            navigationOptions.ReferenceKeys = GetReferenceKeys<T>(principalReslolutionForeignKey?.LocalKeys);

            // go through all items in the current data set and collect all relmkey values
            navigationOptions.ItemPrimaryKeys = [.. _items
                .Select(x => x
                    ?.GetType()
                    .GetProperties()
                    .Intersect(navigationOptions.ReferenceKeys)
                    .Select(y => new Tuple<PropertyInfo, object?>(y, y.GetValue(x)))
                    .Where(y => y.Item2 != null)
                    .Cast<Tuple<PropertyInfo, object>>()
                    .ToList()
                    ?? [])];

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

                    navigationOptions.ReferenceKeys = GetReferenceKeys<T>(targetForeignKeyDecorators
                        .SelectMany(x => x.Value.Select(y => y.Value).ToArray())
                        .FirstOrDefault());

                    navigationOptions.ItemPrimaryKeys = [.. _items
                        .Select(x => x
                            ?.GetType()
                            .GetProperties()
                            .Intersect(navigationOptions.ReferenceKeys)
                            .Select(y => new Tuple<PropertyInfo, object?>(y, y.GetValue(x)))
                            .Where(y => y.Item2 != null)
                            .Cast<Tuple<PropertyInfo, object>>()
                            .ToList()
                            ?? [])];
                }
                else
                {
                    // we're using foreign key properties
                    navigationOptions.ForeignKeyProperties = targetForeignKeyDecorators
                        .Select(x => x.Value.Keys.ToArray())
                        .FirstOrDefault();

                    navigationOptions.ReferenceKeys = GetReferenceKeys<T>([.. targetForeignKeyDecorators.SelectMany(x => x.Value.SelectMany(y => y.Value ?? []).ToArray())]);

                    navigationOptions.ItemPrimaryKeys = [.. _items
                        .Select(x => x
                            ?.GetType()
                            .GetProperties()
                            .Intersect(navigationOptions.ReferenceKeys)
                            .Select(y => new Tuple<PropertyInfo, object?>(y, y.GetValue(x)))
                            .Where(y => y.Item2 != null)
                            .Cast<Tuple<PropertyInfo, object>>()
                            .ToList()
                            ?? [])];
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

        /// <summary>
        /// Searches through all properties in the current T type and identifies the property that is marked with the RelmKey attribute, overriding "InternalId" if necessary
        /// </summary>
        /// <param name="localKeyName"></param>
        /// <returns></returns>
        internal PropertyInfo? GetReferenceKeys<T>(string localKeyName)
        {
            return GetReferenceKeys<T>([localKeyName])?.FirstOrDefault();
        }

        internal PropertyInfo[] GetReferenceKeys<T>(string[]? localKeyNames)
        {
            var referenceKeys = typeof(T).GetProperties();

            if ((localKeyNames?.Length ?? 0) > 0)
                referenceKeys = [.. referenceKeys.Where(x => localKeyNames?.Contains(x.Name) ?? false)];
            else
                referenceKeys = [.. referenceKeys.Where(x => x.GetCustomAttribute<RelmKey>() != null)];

            return referenceKeys;
        }
    }
}
