using CoreRelm.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces
{
    /// <summary>
    /// Represents a strongly-typed, queryable, and updatable collection of data model entities, providing advanced data
    /// loading, filtering, and manipulation capabilities.
    /// </summary>
    /// <remarks>The <see cref="IRelmDataSet{T}"/> interface extends standard collection functionality with methods for
    /// querying, loading, and persisting data entities. It supports fluent query composition, including filtering,
    /// ordering, grouping, and referencing related entities. Implementations may provide deferred execution and
    /// optimized data access patterns. This interface is typically used in data access layers to manage and interact
    /// with sets of domain models.</remarks>
    /// <typeparam name="T">The type of data model entities contained in the data set. Must implement IRelmModel and have a parameterless
    /// constructor.</typeparam>
    public interface IRelmDataSet<T> : ICollection<T>, IRelmDataSetBase where T : IRelmModel, new()
    {
        /// <summary>
        /// Associates a custom field loader with the specified field name.
        /// </summary>
        /// <param name="fieldName">The name of the field for which to set the data loader. Cannot be null or empty.</param>
        /// <param name="dataLoader">The field loader to associate with the specified field. Cannot be null.</param>
        /// <returns>The updated field loader associated with the specified field name.</returns>
        IRelmFieldLoader? SetFieldLoader(string fieldName, IRelmFieldLoader dataLoader);
        
        /// <summary>
        /// Sets the data loader to be used for retrieving data in this dataset.
        /// </summary>
        /// <param name="dataLoader">The data loader instance that will be used to load data. Cannot be null.</param>
        /// <returns>The data loader instance that was set.</returns>
        IRelmDataLoader<T> SetDataLoader(IRelmDataLoader<T> dataLoader);

        /// <summary>
        /// Filters the data set based on a specified predicate expression.
        /// </summary>
        /// <remarks>The returned data set is not materialized until it is enumerated. Multiple calls to
        /// this method can be chained to apply additional filters.</remarks>
        /// <param name="predicate">An expression that defines the conditions each element must satisfy to be included in the returned data set.
        /// Cannot be null.</param>
        /// <returns>A new data set containing elements that satisfy the specified predicate.</returns>
        IRelmDataSet<T> Where(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Creates a reference to a related data set based on the specified property expression.
        /// </summary>
        /// <remarks>Use this method to navigate relationships between data sets, such as foreign key
        /// associations. The returned data set allows further querying or operations on the related entities.</remarks>
        /// <typeparam name="S">The type of the property referenced by the expression.</typeparam>
        /// <param name="predicate">An expression that specifies the property of type T to use as the reference. Typically a lambda expression
        /// selecting a navigation property.</param>
        /// <returns>An <see cref="IRelmDataSet{T}"/> representing the related data set identified by the specified property expression.</returns>
        IRelmDataSet<T> Reference<S>(Expression<Func<T, S>> predicate);
        
        /// <summary>
        /// Creates a reference to a related data set based on the specified navigation property and applies additional
        /// constraints to the referenced set.
        /// </summary>
        /// <remarks>Use this method to navigate relationships between entities and further filter the
        /// referenced data set. This is useful for querying related data with custom constraints in a type-safe
        /// manner.</remarks>
        /// <typeparam name="S">The type of the related entity to reference.</typeparam>
        /// <param name="predicate">An expression that specifies the navigation property used to reference the related entity.</param>
        /// <param name="additionalConstraints">An expression that defines additional constraints to apply to the referenced data set.</param>
        /// <returns>An <see cref="IRelmDataSet{S}"/> representing the related data set with the specified constraints applied.</returns>
        IRelmDataSet<T> Reference<S>(Expression<Func<T, S>> predicate, Expression<Func<S, object>>? additionalConstraints);

        /// <summary>
        /// Creates a reference to a related data set based on the specified navigation property and applies additional
        /// constraints to the referenced entities.
        /// </summary>
        /// <remarks>Use this method to navigate relationships between entities and to further filter the
        /// referenced data set using additional property constraints. This is useful for scenarios where related data
        /// must be accessed with specific filtering criteria.</remarks>
        /// <typeparam name="S">The type of the related entity to reference.</typeparam>
        /// <param name="predicate">An expression that specifies the navigation property from the current entity to the related entity.</param>
        /// <param name="additionalConstraints">A collection of expressions that define additional constraints to apply to the referenced entities. Each
        /// expression specifies a property of the related entity and a constraint to filter the results.</param>
        /// <returns>An <see cref="IRelmDataSet{T}"/> representing the referenced data set with the applied constraints.</returns>
        IRelmDataSet<T> Reference<S>(Expression<Func<T, S>> predicate, ICollection<Expression<Func<S, object>>?>? additionalConstraints);

        /// <summary>
        /// Creates a reference to a related collection of entities, applying additional constraints to the referenced
        /// items.
        /// </summary>
        /// <remarks>Use this method to navigate and filter related collections in a type-safe manner. The
        /// returned data set reflects the specified navigation and constraints, enabling further query
        /// composition.</remarks>
        /// <typeparam name="S">The type of the entities in the referenced collection.</typeparam>
        /// <param name="predicate">An expression that specifies the collection navigation property on the source entity to reference.</param>
        /// <param name="additionalConstraints">An expression that defines additional constraints or filters to apply to the referenced entities.</param>
        /// <returns>An <see cref="IRelmDataSet{S}"/> representing the referenced collection with the specified constraints
        /// applied.</returns>
        IRelmDataSet<T> Reference<S>(Expression<Func<T, ICollection<S>>> predicate, Expression<Func<S, object>>? additionalConstraints);

        /// <summary>
        /// Creates a reference to a related collection of entities based on the specified navigation property and
        /// applies additional constraints to the referenced entities.
        /// </summary>
        /// <remarks>Use this method to include and constrain related collections when querying or
        /// manipulating the data set. This is useful for eager loading or applying filters to related entities in
        /// complex queries.</remarks>
        /// <typeparam name="S">The type of the related entities in the referenced collection.</typeparam>
        /// <param name="predicate">An expression that specifies the navigation property representing the collection of related entities to
        /// reference.</param>
        /// <param name="additionalConstraints">A collection of expressions that define additional constraints to apply to the referenced entities. Each
        /// expression specifies a property or condition to filter or shape the referenced collection.</param>
        /// <returns>An <see cref="IRelmDataSet{T}"/> that represents the current data set with the specified reference and constraints
        /// applied.</returns>
        IRelmDataSet<T> Reference<S>(Expression<Func<T, ICollection<S>>> predicate, ICollection<Expression<Func<S, object>>?>? additionalConstraints);
        
        /// <summary>
        /// Retrieves an item of type T with the specified identifier.
        /// </summary>
        /// <param name="itemId">The unique identifier of the item to locate.</param>
        /// <returns>The item of type T that matches the specified identifier, or the default value for type T if no matching
        /// item is found.</returns>
        T? Find(int itemId);

        /// <summary>
        /// Retrieves an item of type T that matches the specified internal identifier.
        /// </summary>
        /// <param name="itemInternalId">The unique internal identifier of the item to locate. Cannot be null or empty.</param>
        /// <returns>The item of type T that matches the specified internal identifier, or null if no matching item is found.</returns>
        T? Find(string itemInternalId);

        /// <summary>
        /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
        /// </summary>
        /// <returns>The first element in the sequence, or the default value for type <typeparamref name="T"/> if the sequence is
        /// empty.</returns>
        T? FirstOrDefault();

        /// <summary>
        /// Returns the first element of the sequence, or the default value for type T if the sequence contains no
        /// elements.
        /// </summary>
        /// <param name="loadItems">true to load items before retrieving the first element; otherwise, false to use the current state of the
        /// sequence.</param>
        /// <returns>The first element of the sequence if it exists; otherwise, the default value for type T.</returns>
        T? FirstOrDefault(bool loadItems);

        /// <summary>
        /// Returns the first element that satisfies the specified condition, or the default value for the type if no
        /// such element is found.
        /// </summary>
        /// <param name="predicate">An expression that defines the condition to test each element for. Cannot be null.</param>
        /// <returns>The first element that matches the specified predicate; or the default value for type T if no such element
        /// is found.</returns>
        T? FirstOrDefault(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Returns the first element that matches the specified predicate, or the default value for type T if no such
        /// element is found.
        /// </summary>
        /// <param name="predicate">An expression that defines the condition to test each element for. Cannot be null.</param>
        /// <param name="loadItems">true to load items before retrieving the first element; otherwise, false to use the current state of the
        /// sequence.</param>
        /// <returns>The first element that matches the predicate, or the default value for type T if no matching element is
        /// found.</returns>
        T? FirstOrDefault(Expression<Func<T, bool>> predicate, bool loadItems);

        /// <summary>
        /// Loads the items in the data set.
        /// </summary>
        /// <returns>A collection of all items in the data set.</returns>
        ICollection<T?>? Load();

        /// <summary>
        /// Loads a collection of items of type T, optionally including data loaders based on the specified flag.
        /// </summary>
        /// <param name="loadDataLoaders">true to include data loaders in the returned collection; otherwise, false to exclude them.</param>
        /// <returns>A collection of items of type T. The collection may include data loaders if loadDataLoaders is set to true.</returns>
        ICollection<T?>? Load(bool loadDataLoaders);

        /// <summary>
        /// Loads the current data as an <see cref="IRelmDataSet{T}"/> instance for querying and manipulation.
        /// </summary>
        /// <returns>An <see cref="IRelmDataSet{T}"/> containing the loaded data. The returned data set reflects the current
        /// state of the underlying source at the time of the call.</returns>
        IRelmDataSet<T> LoadAsDataSet();

        /// <summary>
        /// Writes data to the underlying destination.
        /// </summary>
        /// <returns>The number of rows written to the destination.</returns>
        int Write();

        /// <summary>
        /// Creates an entry in the data set for the specified item.
        /// </summary>
        /// <param name="item">The item to create an entry for.</param>
        /// <returns>An <see cref="IRelmDataSet{T}"/> holding the created entry.</returns>
        IRelmDataSet<T> Entry(T item);

        /// <summary>
        /// Creates an entry in the data set for the specified item.
        /// </summary>
        /// <param name="item">The item to create an entry for.</param>
        /// <param name="persist">A value indicating whether the entry should be persisted. If <see langword="true"/>, the entry is saved to
        /// the underlying data store; otherwise, it is not persisted.</param>
        /// <returns>An <see cref="IRelmDataSet{T}"/> holding the created entry.</returns>
        IRelmDataSet<T> Entry(T item, bool persist = true);

        /// <summary>
        /// Sorts the elements of the data set in ascending order according to a specified key.
        /// </summary>
        /// <remarks>The ordering is stable; elements with equal keys maintain their original relative
        /// order. To perform a descending sort, use the corresponding OrderByDescending method.</remarks>
        /// <param name="predicate">An expression that specifies the key by which to order the elements. The expression should select a property
        /// or value from each element to use as the sort key.</param>
        /// <returns>A new data set with the elements ordered in ascending order according to the specified key.</returns>
        IRelmDataSet<T> OrderBy(Expression<Func<T, object>> predicate);

        /// <summary>
        /// Sorts the elements of the data set in descending order according to a specified key.
        /// </summary>
        /// <param name="predicate">An expression that selects the key to order the elements by. The key is extracted from each element in the
        /// data set.</param>
        /// <returns>A new data set with the elements sorted in descending order based on the specified key.</returns>
        IRelmDataSet<T> OrderByDescending(Expression<Func<T, object>> predicate);

        /// <summary>
        /// Sets the value of the specified property for all elements in the data set.
        /// </summary>
        /// <param name="predicate">An expression that specifies the property to set and the value to assign.</param>
        /// <returns>The current data set with the updated elements.</returns>
        IRelmDataSet<T> Set(Expression<Func<T, T>> predicate);

        /// <summary>
        /// Sets the "group by" clause for the data set based on the specified key selector.
        /// </summary>
        /// <param name="predicate">An expression that defines the key by which to group the elements.</param>
        /// <returns>The current data set with the "group by" clause added.</returns>
        IRelmDataSet<T> GroupBy(Expression<Func<T, object>> predicate);

        /// <summary>
        /// Sets the "limit" clause for the data set, restricting the number of items returned.
        /// </summary>
        /// <param name="limitCount">The maximum number of items to include in the result set. Must be greater than zero.</param>
        /// <returns>The current data set with the "limit" clause added.</returns>
        IRelmDataSet<T> Limit(int limitCount);

        /// <summary>
        /// Sets the "offset" clause for the data set, specifying the number of items to skip before starting to
        /// return items.
        /// </summary>
        /// <param name="offsetCount">The number of items to skip. Must be greater than or equal to zero.</param>
        /// <returns>The current data set with the "offset" clause added.</returns>
        IRelmDataSet<T> Offset(int offsetCount);

        /// <summary>
        /// Sets a "COUNT(*)" return column on the data set.
        /// </summary>
        /// <returns>The current data set with the "count" operation added.</returns>
        new IRelmDataSet<T> Count();

        /// <summary>
        /// Sets a "COUNT" return column on the data set with a specified predicate.
        /// </summary>
        /// <param name="predicate">An expression that defines the criteria for counting items.</param>
        /// <returns>The current data set with the "count" operation added.</returns>
        new IRelmDataSet<T> Count(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Sets a "DISTINCT" return column on the data set with a specified key selector.
        /// </summary>
        /// <param name="predicate">An expression that defines the key by which to filter distinct items.</param>
        /// <returns>The current data set with the "distinct" operation added.</returns>
        IRelmDataSet<T> DistinctBy(Expression<Func<T, object>> predicate);

        /// <summary>
        /// Saves the specified item to the underlying data store.
        /// </summary>
        /// <param name="item">The item to be saved. Cannot be null.</param>
        /// <returns>The number of records affected by the save operation. Typically returns 1 if the item was saved
        /// successfully; otherwise, returns 0.</returns>
        int Save(T item);

        /// <summary>
        /// Saves the current changes to the underlying data store.
        /// </summary>
        /// <returns>The number of objects that were saved to the data store. Returns 0 if no changes were detected.</returns>
        int Save();

        /// <summary>
        /// Creates a new instance of type T, optionally persisting it.
        /// </summary>
        /// <param name="persist">true to persist the new instance; otherwise, false. The default is true.</param>
        /// <returns>A new instance of type T. The instance may be persisted depending on the value of the persist parameter.</returns>
        T New(bool persist = true);

        /// <summary>
        /// Creates a new instance of type T using the specified parameters.
        /// </summary>
        /// <remarks>If persist is set to false, the new instance will not be saved or committed to any
        /// underlying data store. The caller is responsible for ensuring that newObjectParameters contains all
        /// necessary information for constructing a valid instance of T.</remarks>
        /// <param name="newObjectParameters">An object containing the parameters required to initialize the new instance. The structure and required
        /// properties depend on the type T.</param>
        /// <param name="persist">true to persist the new instance immediately; otherwise, false. The default is true.</param>
        /// <returns>A new instance of type T initialized with the provided parameters.</returns>
        T New(dynamic newObjectParameters, bool persist = true);

        /// <summary>
        /// Adds the specified item to the data set and returns its identifier.
        /// </summary>
        /// <param name="item">The item to add to the data set.</param>
        /// <returns>The identifier of the added item.</returns>
        new int Add(T item);

        /// <summary>
        /// Adds the specified item to the collection and optionally persists the change.
        /// </summary>
        /// <param name="item">The item to add to the collection. Cannot be null.</param>
        /// <param name="persist">A value indicating whether the addition should be persisted. If <see langword="true"/>, the change is saved;
        /// otherwise, it is not.</param>
        /// <returns>The zero-based index at which the item was added.</returns>
        int Add(T item, bool persist);

        /// <summary>
        /// Adds the elements of the specified collection to the current collection.
        /// </summary>
        /// <param name="items">The collection of items to add. Cannot be null. All elements in the collection will be added in enumeration
        /// order.</param>
        /// <returns>The number of items successfully added to the collection.</returns>
        int Add(ICollection<T> items);

        /// <summary>
        /// Adds the specified collection of items to the underlying store, optionally persisting the changes.
        /// </summary>
        /// <param name="items">The collection of items to add. Cannot be null or contain null elements.</param>
        /// <param name="persist">A value indicating whether the changes should be persisted immediately. If <see langword="true"/>, the items
        /// are saved to the store; otherwise, the changes remain in memory until persisted.</param>
        /// <returns>The number of items successfully added to the store.</returns>
        int Add(ICollection<T> items, bool persist);

        /*************************************************************************************************
         *                                         ASYNC METHODS                                         *
         *************************************************************************************************/

        /// <summary>
        /// Retrieves an item of type T with the specified identifier.
        /// </summary>
        /// <param name="itemId">The unique identifier of the item to locate.</param>
        /// <returns>The item of type T that matches the specified identifier, or the default value for type T if no matching
        /// item is found.</returns>
        Task<T?> FindAsync(int itemId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves an item of type T that matches the specified internal identifier.
        /// </summary>
        /// <param name="itemInternalId">The unique internal identifier of the item to locate. Cannot be null or empty.</param>
        /// <returns>The item of type T that matches the specified internal identifier, or null if no matching item is found.</returns>
        Task<T?> FindAsync(string itemInternalId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
        /// </summary>
        /// <returns>The first element in the sequence, or the default value for type <typeparamref name="T"/> if the sequence is
        /// empty.</returns>
        Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the first element of the sequence, or the default value for type T if the sequence contains no
        /// elements.
        /// </summary>
        /// <param name="loadItems">true to load items before retrieving the first element; otherwise, false to use the current state of the
        /// sequence.</param>
        /// <returns>The first element of the sequence if it exists; otherwise, the default value for type T.</returns>
        Task<T?> FirstOrDefaultAsync(bool loadItems, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the first element that satisfies the specified condition, or the default value for the type if no
        /// such element is found.
        /// </summary>
        /// <param name="predicate">An expression that defines the condition to test each element for. Cannot be null.</param>
        /// <returns>The first element that matches the specified predicate; or the default value for type T if no such element
        /// is found.</returns>
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the first element that matches the specified predicate, or the default value for type T if no such
        /// element is found.
        /// </summary>
        /// <param name="predicate">An expression that defines the condition to test each element for. Cannot be null.</param>
        /// <param name="loadItems">true to load items before retrieving the first element; otherwise, false to use the current state of the
        /// sequence.</param>
        /// <returns>The first element that matches the predicate, or the default value for type T if no matching element is
        /// found.</returns>
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, bool loadItems, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads the items in the data set.
        /// </summary>
        /// <returns>A collection of all items in the data set.</returns>
        Task<ICollection<T?>?> LoadAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads a collection of items of type T, optionally including data loaders based on the specified flag.
        /// </summary>
        /// <param name="loadDataLoaders">true to include data loaders in the returned collection; otherwise, false to exclude them.</param>
        /// <returns>A collection of items of type T. The collection may include data loaders if loadDataLoaders is set to true.</returns>
        Task<ICollection<T?>?> LoadAsync(bool loadDataLoaders, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads the current data as an <see cref="IRelmDataSet{T}"/> instance for querying and manipulation.
        /// </summary>
        /// <returns>An <see cref="IRelmDataSet{T}"/> containing the loaded data. The returned data set reflects the current
        /// state of the underlying source at the time of the call.</returns>
        Task<IRelmDataSet<T>> LoadAsDataSetAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes data to the underlying destination.
        /// </summary>
        /// <returns>The number of rows written to the destination.</returns>
        Task<int> WriteAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates an entry in the data set for the specified item.
        /// </summary>
        /// <param name="item">The item to create an entry for.</param>
        /// <returns>An <see cref="IRelmDataSet{T}"/> holding the created entry.</returns>
        Task<IRelmDataSet<T>> EntryAsync(T item, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates an entry in the data set for the specified item.
        /// </summary>
        /// <param name="item">The item to create an entry for.</param>
        /// <param name="persist">A value indicating whether the entry should be persisted. If <see langword="true"/>, the entry is saved to
        /// the underlying data store; otherwise, it is not persisted.</param>
        /// <returns>An <see cref="IRelmDataSet{T}"/> holding the created entry.</returns>
        Task<IRelmDataSet<T>> EntryAsync(T item, bool persist = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves the specified item to the underlying data store.
        /// </summary>
        /// <param name="item">The item to be saved. Cannot be null.</param>
        /// <returns>The number of records affected by the save operation. Typically returns 1 if the item was saved
        /// successfully; otherwise, returns 0.</returns>
        Task<int> SaveAsync(T item, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves the current changes to the underlying data store.
        /// </summary>
        /// <returns>The number of objects that were saved to the data store. Returns 0 if no changes were detected.</returns>
        Task<int> SaveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new instance of type T, optionally persisting it.
        /// </summary>
        /// <param name="persist">true to persist the new instance; otherwise, false. The default is true.</param>
        /// <returns>A new instance of type T. The instance may be persisted depending on the value of the persist parameter.</returns>
        Task<T> NewAsync(bool persist = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new instance of type T using the specified parameters.
        /// </summary>
        /// <remarks>If persist is set to false, the new instance will not be saved or committed to any
        /// underlying data store. The caller is responsible for ensuring that newObjectParameters contains all
        /// necessary information for constructing a valid instance of T.</remarks>
        /// <param name="newObjectParameters">An object containing the parameters required to initialize the new instance. The structure and required
        /// properties depend on the type T.</param>
        /// <param name="persist">true to persist the new instance immediately; otherwise, false. The default is true.</param>
        /// <returns>A new instance of type T initialized with the provided parameters.</returns>
        Task<T> NewAsync(dynamic newObjectParameters, bool persist = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds the specified item to the data set and returns its identifier.
        /// </summary>
        /// <param name="item">The item to add to the data set.</param>
        /// <returns>The identifier of the added item.</returns>
        Task<int> AddAsync(T item, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds the specified item to the collection and optionally persists the change.
        /// </summary>
        /// <param name="item">The item to add to the collection. Cannot be null.</param>
        /// <param name="persist">A value indicating whether the addition should be persisted. If <see langword="true"/>, the change is saved;
        /// otherwise, it is not.</param>
        /// <returns>The zero-based index at which the item was added.</returns>
        Task<int> AddAsync(T item, bool persist, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds the elements of the specified collection to the current collection.
        /// </summary>
        /// <param name="items">The collection of items to add. Cannot be null. All elements in the collection will be added in enumeration
        /// order.</param>
        /// <returns>The number of items successfully added to the collection.</returns>
        Task<int> AddAsync(ICollection<T> items, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds the specified collection of items to the underlying store, optionally persisting the changes.
        /// </summary>
        /// <param name="items">The collection of items to add. Cannot be null or contain null elements.</param>
        /// <param name="persist">A value indicating whether the changes should be persisted immediately. If <see langword="true"/>, the items
        /// are saved to the store; otherwise, the changes remain in memory until persisted.</param>
        /// <returns>The number of items successfully added to the store.</returns>
        Task<int> AddAsync(ICollection<T> items, bool persist, CancellationToken cancellationToken = default);
    }
}
