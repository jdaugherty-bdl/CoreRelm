using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.RelmInternal.Helpers.Operations.ExpressionEvaluator;

namespace CoreRelm.Models
{
    public class RelmExecutionCommand : IRelmExecutionCommand
    {
        public Command InitialCommand { get; private set; }
        public Expression InitialExpression { get; private set; }
        public int AdditionalCommandCount => _additionalCommands?.Count ?? 0;

        private readonly List<RelmExecutionCommand> _additionalCommands = new List<RelmExecutionCommand>();

        public RelmExecutionCommand()
        {

        }

        public RelmExecutionCommand(Command command, Expression expression)
        {
            InitialCommand = command;
            InitialExpression = expression;
        }

        public RelmExecutionCommand AddAdditionalCommand(Command command, Expression expression)
        {
            _additionalCommands.Add(new RelmExecutionCommand(command, expression));

            return this;
        }

        public RelmExecutionCommand AddAdditionalCommand<T>(Command command, Expression<Func<T, object>> expression)
        {
            AddAdditionalCommand(command, expression.Body);

            return this;
        }

        public List<RelmExecutionCommand> GetAdditionalCommands()
        {
            return _additionalCommands;
        }

        public ForeignKeyNavigationOptions GetForeignKeyNavigationOptions<T>(ICollection<T> _items)
        {
            var navigationOptions = new ForeignKeyNavigationOptions();

            navigationOptions.ReferenceProperty = this.InitialExpression as MemberExpression
                ?? throw new InvalidOperationException("Collection must be represented by a lambda expression in the form of 'x => x.PropertyName'.");

            // if foreign key attribute on the current item's property, then we have principal resolution
            var principalReslolutionForeignKey = navigationOptions.ReferenceProperty.Member.GetCustomAttribute<RelmForeignKey>();

            // get all RelmKeys on the main object
            navigationOptions.ReferenceKeys = GetReferenceKeys<T>(principalReslolutionForeignKey?.LocalKeys);

            // go through all items in the current data set and collect all relmkey values
            navigationOptions.ItemPrimaryKeys = _items
                .Select(x => x
                    .GetType()
                    .GetProperties()
                    .Intersect(navigationOptions.ReferenceKeys)
                    .Select(y => new Tuple<PropertyInfo, object>(y, y.GetValue(x)))
                    .ToList())
                .ToList();

            //if ((itemPrimaryKeys?.Count ?? 0) <= 0)
            if (navigationOptions.ItemPrimaryKeys == null)
                throw new Exception("No primary keys found.");

            var targetProperties = navigationOptions.ReferenceType.GetProperties();

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
                    .Segment((prev, next, i) => !(prev.Value.LocalKeys ?? defaultLocalKeys).All(x => (next.Value.LocalKeys ?? defaultLocalKeys).Contains(x)))
                    .ToDictionary(x => x.FirstOrDefault().Value.LocalKeys ?? defaultLocalKeys, x => x.ToDictionary(y => y.Key, y => y.Value.ForeignKeys));

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

                    navigationOptions.ItemPrimaryKeys = _items
                        .Select(x => x
                            .GetType()
                            .GetProperties()
                            .Intersect(navigationOptions.ReferenceKeys)
                            .Select(y => new Tuple<PropertyInfo, object>(y, y.GetValue(x)))
                            .ToList())
                        .ToList();
                }
                else
                {
                    // we're using foreign key properties
                    navigationOptions.ForeignKeyProperties = targetForeignKeyDecorators
                        .Select(x => x.Value.Keys.ToArray())
                        .FirstOrDefault();

                    navigationOptions.ReferenceKeys = GetReferenceKeys<T>(targetForeignKeyDecorators
                        .SelectMany(x => x.Value.SelectMany(y => y.Value ?? new string[] { }).ToArray())
                        .ToArray());

                    navigationOptions.ItemPrimaryKeys = _items
                        .Select(x => x
                            .GetType()
                            .GetProperties()
                            .Intersect(navigationOptions.ReferenceKeys)
                            .Select(y => new Tuple<PropertyInfo, object>(y, y.GetValue(x)))
                            .ToList())
                        .ToList();
                }

                //navigationOptions.NavigationProperty = navigationProps.FirstOrDefault();
            }
            else
            {
                // get the principal entity's foreign key property
                navigationOptions.ForeignKeyProperties = targetProperties.Where(x => principalReslolutionForeignKey.ForeignKeys.Contains(x.Name)).ToArray();
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
        internal PropertyInfo GetReferenceKeys<T>(string localKeyName)
        {
            return GetReferenceKeys<T>(new string[] { localKeyName })?.FirstOrDefault();
        }

        internal PropertyInfo[] GetReferenceKeys<T>(string[] localKeyNames)
        {
            var referenceKeys = typeof(T).GetProperties();

            if ((localKeyNames?.Length ?? 0) > 0)
                referenceKeys = referenceKeys.Where(x => localKeyNames.Contains(x.Name)).ToArray();
            else
                referenceKeys = referenceKeys.Where(x => x.GetCustomAttribute<RelmKey>() != null).ToArray();

            return referenceKeys;
        }
    }
}
