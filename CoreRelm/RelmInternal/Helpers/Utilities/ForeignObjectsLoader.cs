using MoreLinq;
using Newtonsoft.Json;
using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.RelmInternal.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Utilities
{
    internal class ForeignObjectsLoader<T> where T : IRelmModel, new()
    {
        private readonly ICollection<T?>? _items;
        private readonly IRelmContext? _currentContext;

        internal ForeignObjectsLoader()
        {
        }

        internal ForeignObjectsLoader(ICollection<T?>? items, IRelmContext relmContext)
        {
            _items = items;
            _currentContext = relmContext;
        }

        internal LambdaExpression BuildLogicExpression(IRelmExecutionCommand member, ForeignKeyNavigationOptions navigationOptions)
        {
            ArgumentNullException.ThrowIfNull(navigationOptions);

            if (navigationOptions.ReferenceType == null)
                throw new ArgumentNullException(nameof(navigationOptions.ReferenceType), "Reference type is null.");

            if (navigationOptions.ItemPrimaryKeys == null || navigationOptions.ItemPrimaryKeys.Count == 0)
                throw new ArgumentException("Item primary keys are null or empty.", nameof(navigationOptions.ItemPrimaryKeys));

            if (navigationOptions.ForeignKeyProperties == null || navigationOptions.ForeignKeyProperties.Length == 0)
                throw new ArgumentException("Foreign key properties are null or empty.", nameof(navigationOptions.ForeignKeyProperties));

            var parameter = Expression.Parameter(navigationOptions.ReferenceType, "x");
            var funcType = typeof(Func<,>).MakeGenericType(navigationOptions.ReferenceType, typeof(bool));

            // create a Relm expression tree to execute on the where method of the target data set, handles compound keys
            BinaryExpression? orExpression = null;
            foreach (var itemPrimaryKey in navigationOptions.ItemPrimaryKeys)
            {
                BinaryExpression? andExpression = null;
                for (var i = 0; i < itemPrimaryKey.Count; i++)
                {
                    var memberExpression = Expression.Property(parameter, navigationOptions.ForeignKeyProperties[i].Name)
                        ?? throw new Exception("Property referenced by RelmForeignKey attribute could not be found.");

                    Expression constantExpression = Expression.Constant(itemPrimaryKey[i].Item2);

                    // check that types of constantExpression and memberExpression are compatible be placed in an Expression.Equal statement together
                    if (memberExpression.Type != constantExpression.Type)
                        constantExpression = Expression.Convert(constantExpression, memberExpression.Type);

                    var equalExpression = Expression.Equal(constantExpression, memberExpression);

                    if (andExpression == null)
                        andExpression = equalExpression;
                    else
                        andExpression = Expression.AndAlso(andExpression, equalExpression);
                }

                if (orExpression == null)
                    orExpression = andExpression;
                else if (andExpression != null)
                    orExpression = Expression.OrElse(orExpression, andExpression);
            }

            // add any additional constraints
            foreach (var additionalCommand in member.GetAdditionalCommands())
            {
                var expression = additionalCommand.ExecutionExpression;

                if (expression is UnaryExpression unaryExpression)
                    expression = unaryExpression.Operand;

                if (orExpression != null && expression != null)
                orExpression = Expression.AndAlso(orExpression, expression);
            }

            var containsLambda = Expression.Lambda(funcType, orExpression, parameter)
                ?? throw new Exception("No contains lambda expression found.");

            return containsLambda;
        }

        internal IDictionary GetCollectionItems(LambdaExpression containsLambda, ForeignKeyNavigationOptions navigationOptions)
        {
            return GetCollectionItemsAsync(containsLambda, navigationOptions)
                .GetAwaiter()
                .GetResult();
        }

        internal async Task<IDictionary> GetCollectionItemsAsync(LambdaExpression containsLambda, ForeignKeyNavigationOptions navigationOptions, CancellationToken cancellationToken = default)
        {
            // Instantiate a new DALContext of the same type as CurrentContext so we can load the data we need without modifying anything in our context
            var dataSetMethod = _currentContext?.GetType().GetMethod(nameof(_currentContext.GetDataSetType), [typeof(Type)])
                ?? throw new InvalidOperationException("Method not found.");

            // Find the DALDataSet with the same generic type as referenceType and create a new one
            var dataSet = dataSetMethod.Invoke(_currentContext, [navigationOptions.ReferenceType]) 
                ?? throw new InvalidOperationException($"No RelmDataSet with generic type [{navigationOptions.ReferenceProperty?.Type.Name}] found in context [{_currentContext.GetType().Name}]."); //as IRelmDataSetBase

            var containsMethod = typeof(List<object>).GetMethod(nameof(List<object>.Contains));
            var whereMethod = dataSet
                .GetType()
                .GetMethods()
                .Where(m => m.Name == nameof(RelmDataSet<T>.Where))
                .First();

            var filteredDataSetContains = whereMethod.Invoke(dataSet, [containsLambda]);
            var collectionItemsContainsTask = (Task<ICollection<T>?>?)dataSet.GetType()
                .GetMethods()
                .FirstOrDefault(x => x.Name == nameof(RelmDataSet<T>.LoadAsync) && x.GetParameters().Length == 2)
                ?.Invoke(filteredDataSetContains, [true, cancellationToken]);
            //var collectionItemsContains = await collectionItemsContainsTask;
            if (collectionItemsContainsTask != null)
                await collectionItemsContainsTask;

            // use a foreach loop to convert collectionItemsContains to a dictionary where the key is the foreign key and the object is the item
            var collectionItems = (IDictionary?)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(object).MakeArrayType(), navigationOptions.ReferenceProperty.Type));

            var concreteCollectionItems = (Dictionary<object[], object>?)collectionItems ?? [];
            foreach (var item in (IEnumerable)dataSet)
            {
                var targetObjectForeignKeyValues = navigationOptions.ForeignKeyProperties?.Select(x => x.GetValue(item)).ToArray() ?? [];

                if (concreteCollectionItems?.Keys.Cast<object[]>().FirstOrDefault(x => x.Select((y, i) => ForeignKeyComparer.Compare(targetObjectForeignKeyValues[i], y)).All(y => y)) == null)
                {
                    concreteCollectionItems!.Add(targetObjectForeignKeyValues, default);

                    if (navigationOptions.IsCollection)
                        concreteCollectionItems[targetObjectForeignKeyValues] = Activator.CreateInstance(typeof(List<>).MakeGenericType(navigationOptions.ReferenceType)); //.ReferenceProperty.Type));
                }
                else if (!navigationOptions.IsCollection)
                {
                    // if the collectionItems already contains the key and it's not a collection, throw an exception
                    throw new Exception($"Collection already contains an item with the same foreign key: concreteCollectionItems [{JsonConvert.SerializeObject(concreteCollectionItems)}], targetObjectForeignKeyValues: [{JsonConvert.SerializeObject(targetObjectForeignKeyValues)}]: nav options: [{JsonConvert.SerializeObject(navigationOptions)}].");
                }

                if (navigationOptions.IsCollection)
                    ((IList?)concreteCollectionItems[concreteCollectionItems.Keys.Cast<object[]>().FirstOrDefault(x => x.Select((y, i) => ForeignKeyComparer.Compare(targetObjectForeignKeyValues[i], y)).All(y => y))]).Add(item);
                else
                    concreteCollectionItems[targetObjectForeignKeyValues] = item;
            }

            return concreteCollectionItems;
        }

        /// <summary>
        /// Takes EF6-like foreign key attributes and loads the related objects into their respective data sets in the current context, with the
        /// difference that this function uses the explicitly declared [RelmKey] attribute. The foreign key may be 1) declared on the primary entity,
        /// indicating which property on the navigation entity is the foreign key, or 2) declared on the navigation entity, indicating which property
        /// is the foreign key, or 3) declared on the foreign key property itself, indicating which property is the navigation entity it is the primary
        /// key for. If no [RelmKey] is declared, will default to "InternalId".
        /// </summary>
        /// <param name="member">The property member to load references for.</param>
        /// <exception cref="InvalidOperationException">Thrown if there's an invalid operation.</exception>
        /// <exception cref="MemberAccessException">Thrown if there's an invalid member.</exception>
        /// <exception cref="Exception">Thrown if there's an exception.</exception>
        //internal void LoadForeignObjects(Expression member)
        internal void LoadForeignObjects(IRelmExecutionCommand member)
        {
            LoadForeignObjectsAsync(member)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Takes EF6-like foreign key attributes and loads the related objects into their respective data sets in the current context, with the
        /// difference that this function uses the explicitly declared [RelmKey] attribute. The foreign key may be 1) declared on the primary entity,
        /// indicating which property on the navigation entity is the foreign key, or 2) declared on the navigation entity, indicating which property
        /// is the foreign key, or 3) declared on the foreign key property itself, indicating which property is the navigation entity it is the primary
        /// key for. If no [RelmKey] is declared, will default to "InternalId".
        /// </summary>
        /// <param name="member">The property member to load references for.</param>
        /// <exception cref="InvalidOperationException">Thrown if there's an invalid operation.</exception>
        /// <exception cref="MemberAccessException">Thrown if there's an invalid member.</exception>
        /// <exception cref="Exception">Thrown if there's an exception.</exception>
        //internal void LoadForeignObjects(Expression member)
        internal async Task LoadForeignObjectsAsync(IRelmExecutionCommand member, CancellationToken cancellationToken = default)
        {
            if (_items == null)
                throw new InvalidOperationException("Items collection is null.");
            if (_currentContext == null)
                throw new InvalidOperationException("Current context is null.");

            var navigationOptions = member.GetForeignKeyNavigationOptions(_items);

            if (_currentContext != null && navigationOptions.ReferenceType != null)
                _currentContext.GetDataSet(navigationOptions.ReferenceType);

            var containsLambda = BuildLogicExpression(member, navigationOptions);
            var collectionItems = await GetCollectionItemsAsync(containsLambda, navigationOptions, cancellationToken);

            // loop through each item in _items and add the related item to the collection
            foreach (var item in _items)
            {
                var foreignKeyValues = item.GetType().GetProperties().Where(x => navigationOptions.ReferenceKeys?.Contains(x) ?? false).Select(x => x.GetValue(item)).ToArray();

                foreach (DictionaryEntry entry in collectionItems)
                {
                    // note: all keys should be in the same order as the foreign key values here
                    if (((object[])entry.Key).Select((x, i) => ForeignKeyComparer.Compare(foreignKeyValues[i], x)).All(x => x))
                    {
                        (navigationOptions.ReferenceProperty?.Member as PropertyInfo)?.SetValue(item, entry.Value);

                        break;
                    }
                }
            }
        }
    }
}
