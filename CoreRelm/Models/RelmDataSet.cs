using CoreRelm.Attributes;
using CoreRelm.Extensions;
using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.RelmInternal.Helpers.DataTransfer;
using CoreRelm.RelmInternal.Helpers.Operations;
using CoreRelm.RelmInternal.Helpers.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.RelmInternal.Helpers.Operations.ExpressionEvaluator;

namespace CoreRelm.Models
{
    public class RelmDataSet<T> : ICollection<T>, IRelmDataSet<T> where T : IRelmModel, new()
    {
        public bool Modified { get; set; }

        public int Count => _items?.Count ?? 0;
        public bool IsReadOnly => _items?.IsReadOnly ?? true;

        private readonly IRelmContext _currentContext;
        private readonly IRelmQuickContext _currentQuickContext;
        private IRelmDataLoader<T> _dataLoader;
        private FieldLoaderRegistry<IRelmFieldLoader> _fieldDataLoaders;
        private FieldLoaderRegistry<IRelmQuickFieldLoader> _fieldDataLoadersQuick;

        private ICollection<T> _items;

        public RelmDataSet(IRelmQuickContext currentContext, IRelmDataLoader<T> dataLoader)
        {
            _currentQuickContext = currentContext ?? throw new ArgumentNullException(nameof(currentContext));

            SetupDataSet(dataLoader);
        }

        public RelmDataSet(IRelmContext currentContext, IRelmDataLoader<T> dataLoader)
        {
            _currentContext = currentContext ?? throw new ArgumentNullException(nameof(currentContext));

            SetupDataSet(dataLoader);
        }

        private void SetupDataSet(IRelmDataLoader<T> dataLoader)
        {
            _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));

            _fieldDataLoaders = new FieldLoaderRegistry<IRelmFieldLoader>();
            _fieldDataLoadersQuick = new FieldLoaderRegistry<IRelmQuickFieldLoader>();

            Modified = false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            // get cached items if not null, otherwise load new items list if not null, otherwise return empty collection
            return (_items ?? Load() ?? Enumerable.Empty<T>())?.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IRelmFieldLoader SetFieldLoader(string fieldName, IRelmFieldLoader dataLoader)
        {
            if (!typeof(T).GetProperties().Any(x => x.Name == fieldName))
                throw new ArgumentException($"The field {fieldName} does not exist on the model {typeof(T).Name}");

            return _fieldDataLoaders.RegisterFieldLoader(fieldName, dataLoader);
        }

        public IRelmQuickFieldLoader SetFieldLoader(string fieldName, IRelmQuickFieldLoader dataLoader)
        {
            if (!typeof(T).GetProperties().Any(x => x.Name == fieldName))
                throw new ArgumentException($"The field {fieldName} does not exist on the model {typeof(T).Name}");

            return _fieldDataLoadersQuick.RegisterFieldLoader(fieldName, dataLoader);
        }

        public IRelmDataLoader<T> SetDataLoader(IRelmDataLoader<T> dataLoader)
        {
            _dataLoader = dataLoader;

            return _dataLoader;
        }

        internal IRelmDataLoader<T> GetDataLoader()
        {
            return _dataLoader;
        }

        public IRelmDataSet<T> Where(Expression<Func<T, bool>> predicate)
        {
            _dataLoader.AddExpression(Command.Where, predicate);

            return this;
        }

        public IRelmDataSet<T> Reference<S>(Expression<Func<T, S>> predicate)
        {
            return Reference(predicate, (ICollection<Expression<Func<S, object>>>)null);
        }

        public IRelmDataSet<T> Reference<S>(Expression<Func<T, ICollection<S>>> predicate, Expression<Func<S, object>> additionalConstraints)
        {
            return Reference(predicate, new Expression<Func<S, object>>[] { additionalConstraints });
        }

        public IRelmDataSet<T> Reference<S>(Expression<Func<T, S>> predicate, Expression<Func<S, object>> additionalConstraints)
        {
            return Reference(predicate, new Expression<Func<S, object>>[] { additionalConstraints });
        }

        public IRelmDataSet<T> Reference<S>(Expression<Func<T, ICollection<S>>> predicate, ICollection<Expression<Func<S, object>>> additionalConstraints)
        {
            return InternalReference(predicate, additionalConstraints);
        }

        public IRelmDataSet<T> Reference<S>(Expression<Func<T, S>> predicate, ICollection<Expression<Func<S, object>>> additionalConstraints)
        {
            return InternalReference(predicate, additionalConstraints);
        }

        private IRelmDataSet<T> InternalReference<S>(LambdaExpression predicate, ICollection<Expression<Func<S, object>>> additionalConstraints)
        {
            var referenceExpression = _dataLoader.AddExpression(Command.Reference, predicate.Body);

            if (additionalConstraints != null)
                foreach (var additionalConstraint in additionalConstraints)
                    referenceExpression.AddAdditionalCommand(Command.Reference, additionalConstraint.Body);

            return this;
        }

        public T Find(int ItemId)
        {
            return Where(x => x.Id == ItemId).FirstOrDefault();
        }

        public T Find(string ItemInternalId)
        {
            return Where(x => x.InternalId == ItemInternalId).FirstOrDefault();
        }

        public T FirstOrDefault()
        {
            return FirstOrDefault(null, true);
        }

        public T FirstOrDefault(bool loadItems)
        {
            return FirstOrDefault(null, loadItems);
        }

        public T FirstOrDefault(Expression<Func<T, bool>> predicate)
        {
            return FirstOrDefault(predicate, true);
        }

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

        public IRelmDataSet<T> LoadAsDataSet()
        {
            Load();

            return this;
        }

        public ICollection<T> Load()
        {
            return Load(true);
        }

        public ICollection<T> Load(bool loadDataLoaders)
        {
            _items = _dataLoader.GetLoadData();

            if (_items?.Any() ?? false)
            {
                if (loadDataLoaders)
                {
                    // find all fields marked with a RelmFieldLoader attribute that have a type derived from IRelmFieldLoader<> and add them to the list of field loaders as long as they are not already there
                    /*
                    var fieldLoaders = typeof(T)
                        .GetProperties()
                        .Where(x => x.GetCustomAttribute<RelmDataLoader>()?.LoaderType?.GetInterfaces()?.Any(y => y == (_currentContext == null ? typeof(IRelmQuickFieldLoader) : typeof(IRelmFieldLoader))) ?? false)
                        .ToList();
                    */
                    /*
                    var fieldLoaders = typeof(T)
                        .GetProperties()
                        .Where(x => x.GetCustomAttributes<RelmDataLoader>()?.Any(y => y.LoaderType?.GetInterface(_currentContext == null ? nameof(IRelmQuickFieldLoader) : nameof(IRelmFieldLoader)) != null) ?? false)
                        .ToList();
                    */
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

        public IRelmDataSet<T> Entry(T Item)
        {
            if (_items == null)
                Add(Item);
            else
                _items = new List<T> { Item };

            Modified = true;

            return this;
        }

        public IRelmDataSet<T> Entry(T Item, bool Persist = true)
        {
            if (_items == null)
                Add(Item, Persist);
            else
                _items = new List<T> { Item };

            Modified = true;

            return this;
        }


        public IRelmDataSet<T> OrderBy(Expression<Func<T, object>> predicate)
        {
            _dataLoader.AddSingleExpression(Command.OrderBy, predicate.Body);

            return this;
        }

        public IRelmDataSet<T> OrderByDescending(Expression<Func<T, object>> predicate)
        {
            _dataLoader.AddSingleExpression(Command.OrderByDescending, predicate.Body);

            return this;
        }

        public IRelmDataSet<T> Set(Expression<Func<T, T>> predicate)
        {
            _dataLoader.AddExpression(Command.Set, predicate.Body);

            return this;
        }

        public IRelmDataSet<T> GroupBy(Expression<Func<T, object>> predicate)
        {
            _dataLoader.AddSingleExpression(Command.GroupBy, predicate.Body);

            return this;
        }

        public IRelmDataSet<T> Limit(int LimitCount)
        {
            _dataLoader.AddSingleExpression(Command.Limit, Expression.Constant(LimitCount, LimitCount.GetType()));

            return this;
        }

        public IRelmDataSet<T> DistinctBy(Expression<Func<T, object>> predicate)
        {
            _dataLoader.AddSingleExpression(Command.DistinctBy, predicate.Body);

            return this;
        }

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

        public int Save()
        {
            int rowsUpdated;
            var contextOptions = _currentContext?.ContextOptions ?? _currentQuickContext?.ContextOptions;
            if (contextOptions.OptionsBuilderType == Options.RelmContextOptionsBuilder.OptionsBuilderTypes.OpenConnection)
                rowsUpdated = _items.WriteToDatabase(contextOptions.DatabaseConnection, SqlTransaction: contextOptions.DatabaseTransaction);
            else
                rowsUpdated = _items.WriteToDatabase(contextOptions.ConnectionStringType);

            Modified = false;

            return rowsUpdated;
        }

        public T New()
        {
            return New(null);
        }

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

        void ICollection<T>.Add(T item)
        {
            Add(item, true);
        }

        public int Add(T item)
        {
            return Add(item, true);
        }

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
                    return _items.WriteToDatabase(contextOptions.DatabaseConnection, SqlTransaction: contextOptions.DatabaseTransaction);
                else
                    return item.WriteToDatabase(contextOptions.ConnectionStringType);
            }
            else
                Modified = true;

            return 1;
        }

        public int Add(ICollection<T> items)
        {
            return Add(items, true);
        }

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

        public void Clear()
        {
            _items?.Clear();
        }

        public bool Contains(T item)
        {
            return _items?.Contains(item) ?? false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            // Copy items
            _items?.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return _items?.Remove(item) ?? false;
        }
    }
}
