using MoreLinq;
using Newtonsoft.Json;
using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
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
        private readonly ICollection<T> _items;
        private readonly IRelmContext _currentContext;
        private readonly IRelmQuickContext _currentQuickContext;

        internal ForeignObjectsLoader()
        {
        }

        internal ForeignObjectsLoader(ICollection<T> items, IRelmQuickContext relmContext)
        {
            _items = items;
            _currentQuickContext = relmContext;
        }

        internal ForeignObjectsLoader(ICollection<T> items, IRelmContext relmContext)
        {
            _items = items;
            _currentContext = relmContext;
        }

        internal LambdaExpression BuildLogicExpression(IRelmExecutionCommand member, ForeignKeyNavigationOptions navigationOptions)
        {
            var parameter = Expression.Parameter(navigationOptions.ReferenceType, "x");
            var funcType = typeof(Func<,>).MakeGenericType(navigationOptions.ReferenceType, typeof(bool));

            // create a Relm expression tree to execute on the where method of the target data set, handles compound keys
            BinaryExpression orExpression = null;
            foreach (var itemPrimaryKey in navigationOptions.ItemPrimaryKeys)
            {
                BinaryExpression andExpression = null;
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
                else
                    orExpression = Expression.OrElse(orExpression, andExpression);
            }

            // add any additional constraints
            foreach (var additionalCommand in member.GetAdditionalCommands())
            {
                var expression = additionalCommand.InitialExpression;

                if (expression is UnaryExpression unaryExpression)
                    expression = unaryExpression.Operand;

                orExpression = Expression.AndAlso(orExpression, expression);
            }

            var containsLambda = Expression.Lambda(funcType, orExpression, parameter)
                ?? throw new Exception("No contains lambda expression found.");

            return containsLambda;
        }

        internal IDictionary GetCollectionItems(LambdaExpression containsLambda, ForeignKeyNavigationOptions navigationOptions)
        {
            object dataSet = null;
            if (_currentContext == null)
            {
                // Instantiate a new DALContext of the same type as CurrentContext so we can load the data we need without modifying anything in our context
                var dataSetMethod = _currentQuickContext.GetType().GetMethod(nameof(_currentQuickContext.GetDataSetType), new[] { typeof(Type) })
                    ?? throw new InvalidOperationException("Method not found.");

                // Find the DALDataSet with the same generic type as referenceType and create a new one
                dataSet = dataSetMethod.Invoke(_currentQuickContext, new object[] { navigationOptions.ReferenceType }); //as IRelmDataSetBase
            }
            else
            {
                // Instantiate a new DALContext of the same type as CurrentContext so we can load the data we need without modifying anything in our context
                var dataSetMethod = _currentContext.GetType().GetMethod(nameof(_currentContext.GetDataSetType), new[] { typeof(Type) })
                    ?? throw new InvalidOperationException("Method not found.");

                // Find the DALDataSet with the same generic type as referenceType and create a new one
                dataSet = dataSetMethod.Invoke(_currentContext, new object[] { navigationOptions.ReferenceType }); //as IRelmDataSetBase
            }
                
            if (dataSet == null)
                throw new InvalidOperationException($"No RelmDataSet with generic type [{navigationOptions.ReferenceProperty.Type.Name}] found in context [{_currentContext.GetType().Name}].");

            var containsMethod = typeof(List<object>).GetMethod(nameof(List<object>.Contains));
            var whereMethod = dataSet
                .GetType()
                .GetMethods()
                .Where(m => m.Name == nameof(RelmDataSet<T>.Where))
                .First();

            var filteredDataSetContains = whereMethod.Invoke(dataSet, new object[] { containsLambda });
            var collectionItemsContains = dataSet.GetType()
                .GetMethods()
                .FirstOrDefault(x => x.Name == nameof(RelmDataSet<T>.Load) && x.GetParameters().Length == 1)
                .Invoke(filteredDataSetContains, new object[] { true });

            // use a foreach loop to convert collectionItemsContains to a dictionary where the key is the foreign key and the object is the item
            var collectionItems = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(object).MakeArrayType(), navigationOptions.ReferenceProperty.Type));

            foreach (var item in (IEnumerable)dataSet)
            {
                var targetObjectForeignKeyValues = navigationOptions.ForeignKeyProperties.Select(x => x.GetValue(item)).ToArray();

                if (collectionItems.Keys.Cast<object[]>().FirstOrDefault(x => x.Select((y, i) => ForeignKeyComparer.Compare(targetObjectForeignKeyValues[i], y)).All(y => y)) == null)
                {
                    collectionItems.Add(targetObjectForeignKeyValues, default);

                    if (navigationOptions.IsCollection)
                        collectionItems[targetObjectForeignKeyValues] = Activator.CreateInstance(typeof(List<>).MakeGenericType(navigationOptions.ReferenceType)); //.ReferenceProperty.Type));
                }
                else if (!navigationOptions.IsCollection)
                {
                    // if the collectionItems already contains the key and it's not a collection, throw an exception
                    throw new Exception($"Collection already contains an item with the same foreign key: collectionItems [{JsonConvert.SerializeObject(collectionItems)}], targetObjectForeignKeyValues: [{JsonConvert.SerializeObject(targetObjectForeignKeyValues)}]: nav options: [{JsonConvert.SerializeObject(navigationOptions)}].");
                }

                if (navigationOptions.IsCollection)
                    ((IList)collectionItems[collectionItems.Keys.Cast<object[]>().FirstOrDefault(x => x.Select((y, i) => ForeignKeyComparer.Compare(targetObjectForeignKeyValues[i], y)).All(y => y))]).Add(item);
                else
                    collectionItems[targetObjectForeignKeyValues] = item;
            }

            return collectionItems;
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
            if (_items == null)
                throw new InvalidOperationException("Items collection is null.");
            if (_currentContext == null && _currentQuickContext == null)
            {
                if (_currentContext == null)
                    throw new InvalidOperationException("Current context is null.");
                else
                    throw new InvalidOperationException("Current quick context is null.");
            }

            var navigationOptions = member.GetForeignKeyNavigationOptions(_items);

            _currentQuickContext?.GetDataSet(navigationOptions.ReferenceType);

            var containsLambda = BuildLogicExpression(member, navigationOptions);
            var collectionItems = GetCollectionItems(containsLambda, navigationOptions);

            // loop through each item in _items and add the related item to the collection
            foreach (var item in _items)
            {
                var foreignKeyValues = item.GetType().GetProperties().Where(x => navigationOptions.ReferenceKeys.Contains(x)).Select(x => x.GetValue(item)).ToArray();

                foreach (DictionaryEntry entry in collectionItems)
                {
                    // note: all keys should be in the same order as the foreign key values here
                    if (((object[])entry.Key).Select((x, i) => ForeignKeyComparer.Compare(foreignKeyValues[i], x)).All(x => x))
                    {
                        (navigationOptions.ReferenceProperty.Member as PropertyInfo).SetValue(item, entry.Value);

                        break;
                    }
                }
            }
        }
    }
}
