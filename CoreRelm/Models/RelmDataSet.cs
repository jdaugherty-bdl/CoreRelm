using CoreRelm.Attributes;
using CoreRelm.Extensions;
using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.RelmInternal.Extensions;
using CoreRelm.RelmInternal.Helpers.DataTransfer;
using CoreRelm.RelmInternal.Helpers.Operations;
using CoreRelm.RelmInternal.Helpers.Utilities;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Commands;
using static CoreRelm.RelmInternal.Helpers.Operations.ExpressionEvaluator;

namespace CoreRelm.Models
{
    /// <summary>
    /// Represents a dataset that provides functionality for managing, querying, and persisting a collection of entities
    /// of type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>The <see cref="RelmDataSet{T}"/> class provides a flexible API for working with collections
    /// of entities, including support for filtering,  ordering, grouping, and managing relationships between entities.
    /// It integrates with a data loader to dynamically load and persist data  as needed. The dataset also supports
    /// deferred execution for query operations, ensuring efficient data handling.  This class is designed to work
    /// within the context of a relational data management system, leveraging the provided context and data loader  to
    /// manage the lifecycle and scope of the dataset.</remarks>
    /// <typeparam name="T">The type of the entities in the dataset. Must implement <see cref="IRelmModel"/> and have a parameterless
    /// constructor.</typeparam>
    public class RelmDataSet<T> : ICollection<T>, IRelmDataSet<T> where T : IRelmModel, new()
    {
        /// <summary>
        /// Gets or sets a value indicating whether the object has been modified.
        /// </summary>
        public bool Modified { get; set; }

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        public int Count => _items?.Count ?? 0;

        /// <summary>
        /// Gets a value indicating whether the collection is read-only.
        /// </summary>
        public bool IsReadOnly => _items?.IsReadOnly ?? true;

        private readonly IRelmContext _currentContext;
        private readonly IRelmQuickContext _currentQuickContext;
        private IRelmDataLoader<T> _dataLoader;
        private FieldLoaderRegistry<IRelmFieldLoader> _fieldDataLoaders;
        private FieldLoaderRegistry<IRelmQuickFieldLoader> _fieldDataLoadersQuick;

        private ICollection<T> _items;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelmDataSet{T}"/> class with the specified context and data
        /// loader.
        /// </summary>
        /// <param name="currentContext">The current context used to manage the lifecycle and scope of the dataset. Cannot be <see langword="null"/>.</param>
        /// <param name="dataLoader">The data loader responsible for loading and managing the data for the dataset. Cannot be <see
        /// langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="currentContext"/> is <see langword="null"/>.</exception>
        public RelmDataSet(IRelmContext currentContext, IRelmDataLoader<T> dataLoader)
        {
            _currentContext = currentContext ?? throw new ArgumentNullException(nameof(currentContext));

            SetupDataSet(dataLoader);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelmDataSet{T}"/> class with the specified context and data
        /// loader.
        /// </summary>
        /// <param name="currentContext">The current quick context used to manage the dataset's operational scope. Cannot be <see langword="null"/>.</param>
        /// <param name="dataLoader">The data loader responsible for loading and managing the dataset's data. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="currentContext"/> is <see langword="null"/>.</exception>
        public RelmDataSet(IRelmQuickContext currentContext, IRelmDataLoader<T> dataLoader)
        {
            _currentQuickContext = currentContext ?? throw new ArgumentNullException(nameof(currentContext));

            SetupDataSet(dataLoader);
        }

        private void SetupDataSet(IRelmDataLoader<T> dataLoader)
        { 
            _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));

            _fieldDataLoaders = new FieldLoaderRegistry<IRelmFieldLoader>();
            _fieldDataLoadersQuick = new FieldLoaderRegistry<IRelmQuickFieldLoader>();

            Modified = false;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <remarks>If the collection is not already loaded, it will attempt to load the items
        /// dynamically.  If no items are available, the enumerator will iterate over an empty collection.</remarks>
        /// <returns>An enumerator for the collection of items.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            // get cached items if not null, otherwise load new items list if not null, otherwise return empty collection
            return (_items ?? Load() ?? Enumerable.Empty<T>())?.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <remarks>This method is an explicit interface implementation for <see
        /// cref="IEnumerable.GetEnumerator"/>. It delegates to the strongly-typed <c>GetEnumerator</c> method of the
        /// collection.</remarks>
        /// <returns>An <see cref="IEnumerator"/> that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Registers a field loader for the specified field name.
        /// </summary>
        /// <remarks>This method ensures that the specified field exists on the model type <typeparamref
        /// name="T"/> before registering the loader.</remarks>
        /// <param name="fieldName">The name of the field on the model for which the loader is being registered.  The field must exist on the
        /// model type <typeparamref name="T"/>.</param>
        /// <param name="dataLoader">The <see cref="IRelmFieldLoader"/> instance responsible for loading data for the specified field.</param>
        /// <returns>The <see cref="IRelmFieldLoader"/> instance that was registered for the specified field.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="fieldName"/> does not correspond to a property on the model type <typeparamref
        /// name="T"/>.</exception>
        public IRelmFieldLoader SetFieldLoader(string fieldName, IRelmFieldLoader dataLoader)
        {
            if (!typeof(T).GetProperties().Any(x => x.Name == fieldName))
                throw new ArgumentException($"The field {fieldName} does not exist on the model {typeof(T).Name}");

            return _fieldDataLoaders.RegisterFieldLoader(fieldName, dataLoader);
        }

        /// <summary>
        /// Registers a field loader for the specified field name.
        /// </summary>
        /// <remarks>This method ensures that the specified field exists on the model type <typeparamref
        /// name="T"/> before registering the loader.</remarks>
        /// <param name="fieldName">The name of the field on the model for which the loader is being registered.  The field must exist on the
        /// model type <typeparamref name="T"/>.</param>
        /// <param name="dataLoader">An instance of <see cref="IRelmQuickFieldLoader"/> that provides the logic for loading the field's data.</param>
        /// <returns>The registered <see cref="IRelmQuickFieldLoader"/> instance for the specified field.</returns>
        /// <exception cref="ArgumentException">Thrown if the specified <paramref name="fieldName"/> does not exist on the model type <typeparamref
        /// name="T"/>.</exception>
        public IRelmQuickFieldLoader SetFieldLoader(string fieldName, IRelmQuickFieldLoader dataLoader)
        {
            if (!typeof(T).GetProperties().Any(x => x.Name == fieldName))
                throw new ArgumentException($"The field {fieldName} does not exist on the model {typeof(T).Name}");

            return _fieldDataLoadersQuick.RegisterFieldLoader(fieldName, dataLoader);
        }

        /// <summary>
        /// Sets the data loader for the current instance.
        /// </summary>
        /// <param name="dataLoader">The data loader to be used. Cannot be <see langword="null"/>.</param>
        /// <returns>The data loader that was set.</returns>
        public IRelmDataLoader<T> SetDataLoader(IRelmDataLoader<T> dataLoader)
        {
            _dataLoader = dataLoader;

            return _dataLoader;
        }

        /// <summary>
        /// Retrieves the data loader instance associated with the current context.
        /// </summary>
        /// <returns>An instance of <see cref="IRelmDataLoader{T}"/> that can be used to load data of type <typeparamref
        /// name="T"/>.</returns>
        internal IRelmDataLoader<T> GetDataLoader()
        {
            return _dataLoader;
        }

        /// <summary>
        /// Filters the dataset based on the specified predicate.
        /// </summary>
        /// <remarks>This method allows you to apply a filtering condition to the dataset. The filtering
        /// is deferred, meaning the predicate is not evaluated  until the dataset is enumerated or further operations
        /// are performed.</remarks>
        /// <param name="predicate">An expression that defines the conditions of the filter. The predicate is a function that takes an element
        /// of type <typeparamref name="T"/>  and returns <see langword="true"/> if the element should be included in
        /// the result; otherwise, <see langword="false"/>.</param>
        /// <returns>A dataset containing only the elements that satisfy the specified predicate.</returns>
        public IRelmDataSet<T> Where(Expression<Func<T, bool>> predicate)
        {
            _dataLoader.AddExpression(Command.Where, predicate);

            return this;
        }

        /// <summary>
        /// Creates a reference to the specified property or field of the current entity.
        /// </summary>
        /// <remarks>This method allows you to define a reference to a property or field of the entity, 
        /// which can be used to include related data in subsequent operations.</remarks>
        /// <typeparam name="S">The type of the property or field being referenced.</typeparam>
        /// <param name="predicate">An expression that specifies the property or field to reference.</param>
        /// <returns>An <see cref="IRelmDataSet{T}"/> that includes the specified reference.</returns>
        public IRelmDataSet<T> Reference<S>(Expression<Func<T, S>> predicate)
        {
            return Reference(predicate, (ICollection<Expression<Func<S, object>>>)null);
        }

        /// <summary>
        /// Configures a reference navigation property for inclusion in the query,  allowing additional constraints to
        /// be applied to the related entities.
        /// </summary>
        /// <remarks>This method is typically used to include a collection navigation property in a query 
        /// and apply additional filtering or constraints to the related entities.</remarks>
        /// <typeparam name="S">The type of the related entities in the collection.</typeparam>
        /// <param name="predicate">An expression that specifies the navigation property to include.</param>
        /// <param name="additionalConstraints">An expression that defines additional constraints to apply to the related entities.</param>
        /// <returns>An <see cref="IRelmDataSet{T}"/> that includes the specified navigation property and applies the given
        /// constraints.</returns>
        public IRelmDataSet<T> Reference<S>(Expression<Func<T, ICollection<S>>> predicate, Expression<Func<S, object>> additionalConstraints)
        {
            return Reference(predicate, new Expression<Func<S, object>>[] { additionalConstraints });
        }

        /// <summary>
        /// Creates a reference to a related dataset based on the specified predicate and additional constraints.
        /// </summary>
        /// <remarks>This method allows you to define a reference to a related dataset by specifying a
        /// predicate for the relationship  and additional constraints to further filter the related data. Use this
        /// method when you need to reference a related  dataset with specific filtering criteria.</remarks>
        /// <typeparam name="S">The type of the related entity being referenced.</typeparam>
        /// <param name="predicate">An expression that specifies the relationship between the current entity and the related entity.</param>
        /// <param name="additionalConstraints">An expression that defines additional constraints to apply to the related entity.</param>
        /// <returns>An <see cref="IRelmDataSet{T}"/> representing the related dataset that matches the specified predicate and
        /// constraints.</returns>
        public IRelmDataSet<T> Reference<S>(Expression<Func<T, S>> predicate, Expression<Func<S, object>> additionalConstraints)
        {
            return Reference(predicate, new Expression<Func<S, object>>[] { additionalConstraints });
        }

        /// <summary>
        /// Configures a reference navigation property for the current entity, allowing additional constraints to be
        /// applied.
        /// </summary>
        /// <remarks>This method is typically used to define relationships between entities and apply
        /// additional filtering or constraints on the related entities. The <paramref name="predicate"/> parameter
        /// identifies the navigation property, while the <paramref name="additionalConstraints"/> parameter allows
        /// specifying further conditions on the related entities.</remarks>
        /// <typeparam name="S">The type of the related entity in the collection.</typeparam>
        /// <param name="predicate">An expression that specifies the collection navigation property to configure.</param>
        /// <param name="additionalConstraints">A collection of expressions representing additional constraints to apply to the related entities.</param>
        /// <returns>An <see cref="IRelmDataSet{T}"/> representing the configured reference navigation property.</returns>
        public IRelmDataSet<T> Reference<S>(Expression<Func<T, ICollection<S>>> predicate, ICollection<Expression<Func<S, object>>> additionalConstraints)
        {
            return InternalReference(predicate, additionalConstraints);
        }

        /// <summary>
        /// Creates a reference to a related dataset based on the specified predicate and additional constraints.
        /// </summary>
        /// <typeparam name="S">The type of the related entity being referenced.</typeparam>
        /// <param name="predicate">An expression that specifies the relationship between the current entity and the related entity.</param>
        /// <param name="additionalConstraints">A collection of expressions that define additional constraints to apply to the related dataset.</param>
        /// <returns>An <see cref="IRelmDataSet{T}"/> representing the related dataset that matches the specified predicate and
        /// constraints.</returns>
        public IRelmDataSet<T> Reference<S>(Expression<Func<T, S>> predicate, ICollection<Expression<Func<S, object>>> additionalConstraints)
        {
            return InternalReference(predicate, additionalConstraints);
        }

        /// <summary>
        /// Creates a reference within the dataset based on the specified predicate and additional constraints.
        /// </summary>
        /// <typeparam name="S">The type of the entity used in the additional constraints.</typeparam>
        /// <param name="predicate">A lambda expression representing the condition to define the reference.</param>
        /// <param name="additionalConstraints">A collection of lambda expressions specifying additional constraints for the reference.  If null, no
        /// additional constraints are applied.</param>
        /// <returns>The current dataset instance with the reference applied.</returns>
        private IRelmDataSet<T> InternalReference<S>(LambdaExpression predicate, ICollection<Expression<Func<S, object>>> additionalConstraints)
        {
            var referenceExpression = _dataLoader.AddExpression(Command.Reference, predicate.Body);

            if (additionalConstraints != null)
                foreach (var additionalConstraint in additionalConstraints)
                    referenceExpression.AddAdditionalCommand(Command.Reference, additionalConstraint.Body);

            return this;
        }

        /// <summary>
        /// Finds and returns the first item with the specified identifier.
        /// </summary>
        /// <remarks>This method searches for an item based on its <c>Id</c> property. If multiple items 
        /// share the same identifier, only the first match is returned.</remarks>
        /// <param name="id">The unique identifier of the item to find.</param>
        /// <returns>The first item that matches the specified identifier, or <see langword="null"/>  if no such item is found.</returns>
        public T Find(int id)
        {
            return Where(x => x.Id == id).FirstOrDefault();
        }

        /// <summary>
        /// Finds and returns the first element in the collection that matches the specified internal identifier.
        /// </summary>
        /// <param name="InternalId">The internal identifier to search for. Cannot be null.</param>
        /// <returns>The first element that matches the specified internal identifier, or <see langword="null"/> if no match is
        /// found.</returns>
        public T Find(string InternalId)
        {
            return Where(x => x.InternalId == InternalId).FirstOrDefault();
        }

        /// <summary>
        /// Returns the first element of the sequence, or the default value for the type if the sequence is empty.
        /// </summary>
        /// <remarks>This method retrieves the first element of the sequence. If the sequence contains no
        /// elements,  it returns the default value for the type <typeparamref name="T"/> (e.g., <see langword="null"/>
        /// for reference types,  or the default zero value for value types).</remarks>
        /// <returns>The first element of the sequence, or the default value for the type <typeparamref name="T"/> if the
        /// sequence is empty.</returns>
        public T FirstOrDefault()
        {
            return FirstOrDefault(null, true);
        }

        /// <summary>
        /// Returns the first element of the collection, or the default value for the type if the collection is empty.
        /// </summary>
        /// <remarks>If <paramref name="loadItems"/> is <see langword="true"/>, the method ensures that
        /// items are loaded into the collection before attempting to retrieve the first element.</remarks>
        /// <param name="loadItems">A value indicating whether to load items into the collection before retrieving the first element.</param>
        /// <returns>The first element of the collection if it contains any elements; otherwise, the default value for the type
        /// <typeparamref name="T"/>.</returns>
        public T FirstOrDefault(bool loadItems)
        {
            return FirstOrDefault(null, loadItems);
        }

        /// <summary>
        /// Loads items from the database using the existing predicate conditions and then returns the first element of a sequence that satisfies 
        /// the specified condition,  or the default value for the type if no such element is found.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition. The function must not be <see langword="null"/>.</param>
        /// <returns>The first element in the sequence that satisfies the condition defined by <paramref name="predicate"/>,  or
        /// the default value of type <typeparamref name="T"/> if no such element is found.</returns>
        public T FirstOrDefault(Expression<Func<T, bool>> predicate)
        {
            return FirstOrDefault(predicate, true);
        }

        /// <summary>
        /// Retrieves the first element of a sequence that satisfies the specified condition,  or a default value if no
        /// such element is found.
        /// </summary>
        /// <remarks>If <paramref name="loadItems"/> is <see langword="true"/>, the method loads the items
        /// and applies the condition specified by <paramref name="predicate"/>. If <paramref name="loadItems"/>  is
        /// <see langword="false"/>, the method evaluates the condition on the existing items.</remarks>
        /// <param name="predicate">An expression that defines the condition to test each element for. If <see langword="null"/>,  no condition
        /// is applied.</param>
        /// <param name="loadItems">A value indicating whether to load the items from the database using the existing predicate conditions before 
        /// evaluating the condition.  If <see langword="true"/>,
        /// the items are loaded from the database; otherwise, the method operates on the existing items.</param>
        /// <returns>The first element that satisfies the condition defined by <paramref name="predicate"/>,  or the default
        /// value of type <typeparamref name="T"/> if no such element is found.</returns>
        public T FirstOrDefault(Expression<Func<T, bool>> predicate, bool loadItems)
        {
            if (loadItems)
            {
                Limit(1);

                if (predicate != null)
                    Where(predicate);

                _items = Load();
            }

            if (_items == null)
                return default;
            else
                return _items.FirstOrDefault();
        }

        /// <summary>
        /// Loads the current data and returns it as a dataset.
        /// </summary>
        /// <remarks>This method ensures that the data is loaded before returning the dataset.  It can be
        /// used to retrieve the data in its current state for further processing or querying.</remarks>
        /// <returns>An <see cref="IRelmDataSet{T}"/> representing the loaded dataset.</returns>
        public IRelmDataSet<T> LoadAsDataSet()
        {
            Load();

            return this;
        }

        /// <summary>
        /// Loads a collection of items of type <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>This method retrieves the items and returns them as a collection.  By default, it
        /// includes all items unless specified otherwise in an overload.</remarks>
        /// <returns>A collection of items of type <typeparamref name="T"/>. The collection may be empty if no items are
        /// available.</returns>
        public ICollection<T> Load()
        {
            return Load(true);
        }

        /// <summary>
        /// Loads the data for the current context and optionally initializes and executes field loaders.
        /// </summary>
        /// <remarks>If <paramref name="loadDataLoaders"/> is <see langword="true"/>, the method
        /// identifies properties in the type <typeparamref name="T"/> that are marked with the <see
        /// cref="RelmDataLoader"/> attribute and initializes field loaders for them. These field loaders are then
        /// executed to load additional data related to the fields.  If the data loader has executed commands related to
        /// references, the method also loads all references associated with the data.</remarks>
        /// <param name="loadDataLoaders">A value indicating whether to initialize and execute field loaders for properties marked with the <see
        /// cref="RelmDataLoader"/> attribute. If <see langword="true"/>, field loaders are registered and executed;
        /// otherwise, only the data is loaded.</param>
        /// <returns>A collection of items of type <typeparamref name="T"/> representing the loaded data. The collection will be
        /// empty if no data is available.</returns>
        public ICollection<T> Load(bool loadDataLoaders)
        {
            _items = _dataLoader.GetLoadData();

            if (_items?.Any() ?? false)
            {
                if (loadDataLoaders)
                {
                    // find all fields marked with a RelmFieldLoader attribute that have a type derived from IRelmFieldLoader<> and add them to the list of field loaders as long as they are not already there
                    var relevantContextName = _currentContext == null ? nameof(IRelmQuickFieldLoader) : nameof(IRelmFieldLoader);
                    var fieldLoaders = new List<PropertyInfo>();
                    var loaderProperties = typeof(T).GetProperties();
                    foreach (var property in loaderProperties)
                    {
                        var dataLoaderAttributes = property.GetCustomAttributes<RelmDataLoader>();
                        if (dataLoaderAttributes?.Any() ?? false)
                        {
                            foreach (var dataLoaderAttribute in dataLoaderAttributes)
                            {
                                if (dataLoaderAttribute.LoaderType.GetInterface(relevantContextName) != null)
                                {
                                    fieldLoaders.Add(property);
                                    break;
                                }
                            }
                        }
                    }

                    foreach (var field in fieldLoaders)
                    {
                        if (_fieldDataLoaders.HasFieldLoader(field.Name) || _fieldDataLoadersQuick.HasFieldLoader(field.Name))
                            continue;

                        var dataLoaderAttribute = field.GetCustomAttributes<RelmDataLoader>().FirstOrDefault(x => x.LoaderType.GetInterfaces().FirstOrDefault(y => y.Name == relevantContextName) != null);
                        if (_currentQuickContext != null)
                            _fieldDataLoadersQuick.RegisterFieldLoader(field.Name, (IRelmQuickFieldLoader)Activator.CreateInstance(dataLoaderAttribute.LoaderType, new object[] { _currentQuickContext, field.Name, dataLoaderAttribute.KeyFields }));
                        if (_currentContext != null)
                            _fieldDataLoaders.RegisterFieldLoader(field.Name, (IRelmFieldLoader)Activator.CreateInstance(dataLoaderAttribute.LoaderType, new object[] { _currentContext, field.Name, dataLoaderAttribute.KeyFields }));
                    }

                    // execute all field loaders
                    var fieldHelper = new FieldLoaderHelper<T>(_items);
                    foreach (var fieldLoader in _fieldDataLoaders)
                    {
                        fieldHelper.LoadData(fieldLoader);
                    }
                    foreach (var fieldLoader in _fieldDataLoadersQuick)
                    {
                        fieldHelper.LoadData(fieldLoader);
                    }
                }

                // load all references
                if (_dataLoader.LastCommandsExecuted?.ContainsKey(Command.Reference) ?? false)
                    LoadReference();
            }

            return _items;
        }

        /// <summary>
        /// Writes data using the underlying data loader.
        /// </summary>
        /// <returns>The number of records successfully written.</returns>
        public int Write()
        {
            return _dataLoader.WriteData();
        }

        /// <summary>
        /// Loads related single objects (references) into the current data set based on foreign key attributes.
        /// </summary>
        /// <remarks>
        /// Uses reflection to dynamically generate the queries and collect the data for these references.
        /// This method assumes that each "reference" refers to a property that is a single object (e.g., not a collection).
        /// 
        /// The process involves the following steps:
        /// - Validate that the property representing the reference conforms to expected types.
        /// - Locate a property in the related type that is marked with the DALForeignKey attribute, which indicates a foreign key relationship.
        /// - Generate a WHERE clause based on the foreign key relationship to identify the specific object.
        /// - Execute the query and fill the property in the current data set with the loaded object.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if any of the validations or assumptions fail.</exception>
        /// <exception cref="Exception">General exception for unexpected issues, such as a failure to find attributes or properties.</exception>
        private void LoadReference()
        {
            var objectsLoader = _currentContext == null
                ? new ForeignObjectsLoader<T>(_items, _currentQuickContext)
                : new ForeignObjectsLoader<T>(_items, _currentContext);

            foreach (var reference in _dataLoader.LastCommandsExecuted[Command.Reference])
            {
                objectsLoader.LoadForeignObjects(reference);
            }
        }

        /// <summary>
        /// Clears the current dataset, then adds the specified item to the dataset and marks the dataset as modified.
        /// </summary>
        /// <remarks>The item is added to the cleared dataset without persisting the change. The dataset's state
        /// is marked as modified.</remarks>
        /// <param name="Item">The item to be added to the dataset. Cannot be null.</param>
        /// <returns>The current dataset instance, allowing for method chaining.</returns>
        public IRelmDataSet<T> Entry(T Item)
        {
            _items = new List<T>();
                
            Add(Item, Persist: false);

            Modified = true;

            return this;
        }

        /// <summary>
        /// Clears the current dataset, then adds the specified item to the dataset and optionally persists it.
        /// </summary>
        /// <remarks>This method modifies the dataset by clearing then adding the specified item and marks the dataset
        /// as modified.  If <paramref name="Persist"/> is <see langword="true"/>, the item will be persisted.</remarks>
        /// <param name="Item">The item to add to the dataset. Cannot be <see langword="null"/>.</param>
        /// <param name="Persist">A value indicating whether the item should be persisted.  The default value is <see langword="true"/>.</param>
        /// <returns>The current dataset instance with the added item.</returns>
        public IRelmDataSet<T> Entry(T Item, bool Persist = true)
        {
            _items = new List<T>();

            Add(Item, Persist: Persist);

            Modified = true;

            return this;
        }

        /// <summary>
        /// Orders the elements in the dataset based on the specified key selector.
        /// </summary>
        /// <remarks>This method modifies the query to include an "ORDER BY" clause based on the provided
        /// key selector. The ordering is applied in ascending order by default.</remarks>
        /// <param name="predicate">An expression that specifies the key to order the elements by. The key is derived from the dataset elements.</param>
        /// <returns>A dataset with the elements ordered according to the specified key.</returns>
        public IRelmDataSet<T> OrderBy(Expression<Func<T, object>> predicate)
        {
            // add an "order by" command to the SQL query
            _dataLoader.AddSingleExpression(Command.OrderBy, predicate.Body);

            return this;
        }

        /// <summary>
        /// Orders the elements of the dataset in descending order based on the specified key.
        /// </summary>
        /// <remarks>This method modifies the query by appending an "ORDER BY DESC" clause using the
        /// specified key. The ordering is applied to the dataset when the query is executed.</remarks>
        /// <param name="predicate">An expression that specifies the key to use for ordering the elements.  The key is derived from the property
        /// or computed value defined in the expression.</param>
        /// <returns>A dataset with the elements ordered in descending order based on the specified key.</returns>
        public IRelmDataSet<T> OrderByDescending(Expression<Func<T, object>> predicate)
        {
            // add an "order by descending" command to the SQL query
            _dataLoader.AddSingleExpression(Command.OrderByDescending, predicate.Body);

            return this;
        }

        /// <summary>
        /// Updates a single column's value in the dataset based on the specified expression.
        /// </summary>
        /// <remarks>This method modifies the dataset by applying the specified update expression.  The
        /// changes are not persisted until the dataset is committed.</remarks>
        /// <param name="predicate">An expression that specifies the column to update and the value to assign.  The expression must be in the
        /// form of a lambda, such as <c>x => x.Property = value</c>.</param>
        /// <returns>The current dataset instance, allowing for method chaining.</returns>
        public IRelmDataSet<T> Set(Expression<Func<T, T>> predicate)
        {
            // set a single column's value based on the expression
            _dataLoader.AddExpression(Command.Set, predicate.Body);

            return this;
        }

        /// <summary>
        /// Groups the elements of the dataset based on the specified key selector.
        /// </summary>
        /// <remarks>The grouping is applied to the underlying query, modifying the dataset to reflect the
        /// grouping operation. This method does not execute the query; it only adds the grouping condition to the query
        /// definition.</remarks>
        /// <param name="predicate">An expression that specifies the key to group by. The key is derived from the elements of the dataset.</param>
        /// <returns>A dataset grouped by the specified key.</returns>
        public IRelmDataSet<T> GroupBy(Expression<Func<T, object>> predicate)
        {
            // add a "group by" command to the SQL query
            _dataLoader.AddSingleExpression(Command.GroupBy, predicate.Body);

            return this;
        }

        /// <summary>
        /// Limits the number of results returned by the dataset to the specified count.
        /// </summary>
        /// <param name="limitCount">The maximum number of results to include in the dataset. Must be a non-negative integer.</param>
        /// <returns>The current dataset instance with the applied limit, allowing for further query chaining.</returns>
        public IRelmDataSet<T> Limit(int limitCount)
        {
            _dataLoader.AddSingleExpression(Command.Limit, Expression.Constant(limitCount, limitCount.GetType()));

            return this;
        }

        /// <summary>
        /// Filters the dataset to include only distinct elements based on the specified key selector.
        /// </summary>
        /// <remarks>This method modifies the current dataset by adding a distinct operation to the query.
        /// The distinctness is determined by the value of the key selected by the <paramref name="predicate"/>
        /// expression.</remarks>
        /// <param name="predicate">An expression that specifies the key used to determine distinct elements. The key is derived from the
        /// properties of the dataset elements.</param>
        /// <returns>A dataset containing only distinct elements based on the specified key.</returns>
        public IRelmDataSet<T> DistinctBy(Expression<Func<T, object>> predicate)
        {
            // adds a "distinct" command to the SQL query
            _dataLoader.AddSingleExpression(Command.DistinctBy, predicate.Body);

            return this;
        }

        /// <summary>
        /// Saves the specified item to the collection. If an item with the same internal identifier  already exists, it
        /// is replaced; otherwise, the item is added to the collection.
        /// </summary>
        /// <remarks>This method ensures that the collection does not contain duplicate items based on
        /// their  internal identifiers. If an item with the same identifier already exists, it is replaced  with the
        /// new item. Otherwise, the item is added to the collection, and the operation is  persisted.</remarks>
        /// <param name="Item">The item to save. The item must have a valid internal identifier.</param>
        /// <returns>An integer representing the result of the save operation. The exact meaning of the return  value depends on
        /// the underlying implementation of the save or add operation.</returns>
        public int Save(T Item)
        {
            // check if the item is already in the list, and if so, replace it, otherwise, add it
            if (_items?.Any(x => x.InternalId == Item.InternalId) ?? false)
            {
                _items = _items.Select(x => x.InternalId == Item.InternalId ? Item : x).ToList();

                return Save();
            }
            else
                return Add(Item, Persist: true);
        }

        /// <summary>
        /// Saves the current items to the database and returns the number of rows updated.
        /// </summary>
        /// <remarks>The method determines the database connection settings based on the current context
        /// options. If the context options specify an open connection, the method uses the provided database connection
        /// and transaction. Otherwise, it uses the connection string type to establish the connection. After the save
        /// operation, the <see cref="Modified"/> property is set to <see langword="false"/>.</remarks>
        /// <returns>The number of rows updated in the database.</returns>
        public int Save()
        {
            int rowsUpdated;
            var contextOptions = _currentContext?.ContextOptions ?? _currentQuickContext?.ContextOptions;
            if (contextOptions.OptionsBuilderType == Options.RelmContextOptionsBuilder.OptionsBuilderTypes.OpenConnection)
                rowsUpdated = _items.WriteToDatabase(contextOptions.DatabaseConnection, sqlTransaction: contextOptions.DatabaseTransaction);
            else
                rowsUpdated = _items.WriteToDatabase(contextOptions.ConnectionStringType);

            Modified = false;

            return rowsUpdated;
        }

        /// <summary>
        /// Creates a new blank instance of the object, with an option to persist it.
        /// </summary>
        /// <param name="Persist">A value indicating whether the new instance should be persisted.  <see langword="true"/> to persist the
        /// instance; otherwise, <see langword="false"/>.</param>
        /// <returns>A new instance of the object.</returns>
        public T New(bool Persist = true)
        {
            return New(null, Persist: Persist);
        }

        /// <summary>
        /// Creates a new instance of the specified type <typeparamref name="T"/> and initializes its properties based
        /// on the provided dynamic parameters.
        /// </summary>
        /// <remarks>This method uses reflection to set the properties of the new instance. Ensure that
        /// the property names in <paramref name="NewObjectParameters"/> match the expected property names of
        /// <typeparamref name="T"/>.</remarks>
        /// <param name="NewObjectParameters">A dynamic object containing property names and values to initialize the new instance.  Only properties
        /// matching the keys in the underscore properties of <typeparamref name="T"/> will be set.</param>
        /// <param name="Persist">A boolean value indicating whether the newly created object should be persisted.  The default value is <see
        /// langword="true"/>.</param>
        /// <returns>A new instance of <typeparamref name="T"/> with its properties initialized based on the provided parameters.</returns>
        public T New(dynamic NewObjectParameters, bool Persist = true)
        {
            // create a new instance of T
            var newObject = new T();

            // run through each property in the dynamic object, and if the name matches one of the keys in Underscore properties, use reflection to set the value of the new object
            if (NewObjectParameters != null)
                foreach (var property in new RouteValueDictionary(NewObjectParameters))
                    if (_dataLoader.HasUnderscoreProperty(property.Key))
                        typeof(T).GetProperty(property.Key).SetValue(newObject, property.Value);

            Add(newObject, Persist: Persist);

            return newObject;
        }

        /// <summary>
        /// Adds the specified item to the collection.
        /// </summary>
        /// <remarks>This method is an explicit implementation of the <see cref="ICollection{T}.Add"/>
        /// method.</remarks>
        /// <param name="item">The item to add to the collection.</param>
        void ICollection<T>.Add(T item)
        {
            Add(item, true);
        }

        /// <summary>
        /// Adds the specified item to the collection and persists the change.
        /// </summary>
        /// <param name="item">The item to add to the collection. Cannot be <see langword="null"/>.</param>
        /// <returns>The index at which the item was added.</returns>
        public int Add(T item)
        {
            return Add(item, Persist: true);
        }

        /// <summary>
        /// Adds the specified item to the collection and optionally persists it to the database.
        /// </summary>
        /// <remarks>If <paramref name="Persist"/> is <see langword="true"/>, the method writes the item
        /// to the database  using the current database context. The database connection and transaction details are
        /// determined  by the context options. If <paramref name="Persist"/> is <see langword="false"/>, the
        /// collection's  state is marked as modified.</remarks>
        /// <param name="item">The item to add to the collection.</param>
        /// <param name="Persist">A value indicating whether the item should be persisted to the database.  If <see langword="true"/>, the
        /// item is written to the database; otherwise, it is only added to the collection.</param>
        /// <returns>An integer indicating the result of the operation. Returns 1 if the item is added to the collection  without
        /// persistence, or the result of the database write operation if <paramref name="Persist"/> is <see
        /// langword="true"/>.</returns>
        public int Add(T item, bool Persist)
        {
            // Instantiate _items if it has not been initialized
            _items = _items ?? new List<T>();

            // Add the item to the internal collection
            _items.Add(item);

            // If persisting is necessary, write to database
            if (Persist)
            {
                var contextOptions = _currentContext?.ContextOptions ?? _currentQuickContext?.ContextOptions;
                if (contextOptions.OptionsBuilderType == Options.RelmContextOptionsBuilder.OptionsBuilderTypes.OpenConnection)
                    return _items.WriteToDatabase(contextOptions.DatabaseConnection, sqlTransaction: contextOptions.DatabaseTransaction);
                else
                    return item.WriteToDatabase(contextOptions.ConnectionStringType);
            }
            else
                Modified = true;

            return 1;
        }

        /// <summary>
        /// Adds the specified collection of items to the current instance and persists the change.
        /// </summary>
        /// <param name="items">The collection of items to add. Cannot be null.</param>
        /// <returns>The number of items successfully added.</returns>
        public int Add(ICollection<T> items)
        {
            return Add(items, Persist: true);
        }

        /// <summary>
        /// Adds a collection of items to the current instance and optionally persists the changes.
        /// </summary>
        /// <param name="items">The collection of items to add. Cannot be null.</param>
        /// <param name="Persist">A value indicating whether the changes should be persisted after adding the items. If <see
        /// langword="true"/>, the method persists the changes and returns the result of the save operation. If <see
        /// langword="false"/>, the method only adds the items and returns the count of items added.</param>
        /// <returns>The number of items added if <paramref name="Persist"/> is <see langword="false"/>,  or the result of the
        /// save operation if <paramref name="Persist"/> is <see langword="true"/>.</returns>
        public int Add(ICollection<T> items, bool Persist)
        {
            var itemCounter = 0;
            foreach (T item in items)
            {
                Add(item, false);
                itemCounter++;
            }

            if (Persist)
                return Save();

            return itemCounter;
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        /// <remarks>After calling this method, the collection will be empty. If the collection is already
        /// empty, this method has no effect.</remarks>
        public void Clear()
        {
            _items?.Clear();
        }

        /// <summary>
        /// Determines whether the collection contains the specified item.
        /// </summary>
        /// <remarks>If the collection is null, this method returns <see langword="false"/>.</remarks>
        /// <param name="item">The item to locate in the collection.</param>
        /// <returns><see langword="true"/> if the specified item is found in the collection; otherwise, <see langword="false"/>.</returns>
        public bool Contains(T item)
        {
            return _items?.Contains(item) ?? false;
        }

        /// <summary>
        /// Copies the elements of the collection to the specified array, starting at the specified index.
        /// </summary>
        /// <remarks>This method copies the elements in the same order they are enumerated by the
        /// collection.</remarks>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection. The array must
        /// have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in the <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            // Copy items
            _items?.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes the specified item from the collection.
        /// </summary>
        /// <param name="item">The item to remove from the collection.</param>
        /// <returns><see langword="true"/> if the item was successfully removed; otherwise, <see langword="false"/>. Returns
        /// <see langword="false"/> if the item was not found in the collection or if the collection is null.</returns>
        public bool Remove(T item)
        {
            return _items?.Remove(item) ?? false;
        }
    }
}
