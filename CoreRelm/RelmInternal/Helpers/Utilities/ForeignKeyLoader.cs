using MySql.Data.MySqlClient;
using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.Options;
using CoreRelm.RelmInternal.Helpers.DataTransfer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Commands;

namespace CoreRelm.RelmInternal.Helpers.Utilities
{
    internal class ForeignKeyLoader<T> where T : IRelmModel, new()
    {
        private readonly ICollection<T> targetObjects;
        private readonly RelmContextOptionsBuilder contextOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ForeignKeyLoader{T}"/> class for the specified target object.
        /// </summary>
        /// <remarks>This constructor initializes the loader with a single target object and uses the
        /// context options from the provided <paramref name="relmContext"/>.</remarks>
        /// <param name="targetObject">The target object for which the foreign key data will be loaded.</param>
        /// <param name="relmContext">The context that provides configuration options for the operation. This parameter must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="targetObject"/> is <see langword="null"/> or if <paramref name="relmContext"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="targetObject"/> is <see langword="null"/>.</exception>
        public ForeignKeyLoader(T targetObject, IRelmContext relmContext) : this([targetObject], relmContext?.ContextOptions)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForeignKeyLoader{T}"/> class for loading foreign key
        /// relationships.
        /// </summary>
        /// <param name="targetObject">The target object whose foreign key relationships will be loaded.</param>
        /// <param name="contextOptions">The options used to configure the context for loading foreign key relationships.This parameter must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="targetObject"/> is <see langword="null"/> or if <paramref name="contextOptions"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="targetObject"/> is <see langword="null"/>.</exception>
        public ForeignKeyLoader(T targetObject, RelmContextOptionsBuilder contextOptions) : this([targetObject], contextOptions)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForeignKeyLoader{T}"/> class with the specified target objects
        /// and Realm context.
        /// </summary>
        /// <param name="targetObjects">The collection of target objects to be loaded. Cannot be null.</param>
        /// <param name="relmContext">The Realm context used to configure the loader. This parameter must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="targetObjects"/> is <see langword="null"/> or if <paramref name="relmContext"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="targetObjects"/> is empty or contains <see langword="null"/> values.</exception>
        public ForeignKeyLoader(ICollection<T> targetObjects, IRelmContext relmContext) : this(targetObjects, relmContext?.ContextOptions)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForeignKeyLoader{T}"/> class,  which is responsible for
        /// managing the loading of foreign key relationships  for a collection of target objects.
        /// </summary>
        /// <param name="targetObjects">The collection of target objects for which foreign key relationships will be loaded.  The collection must
        /// not be null, empty, or contain null values.</param>
        /// <param name="contextOptions">The options used to configure the context for loading foreign key relationships.  This parameter must not be
        /// null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="targetObjects"/> is <see langword="null"/> or if <paramref name="contextOptions"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="targetObjects"/> is empty or contains <see langword="null"/> values.</exception>
        public ForeignKeyLoader(ICollection<T> targetObjects, RelmContextOptionsBuilder contextOptions)
        {
            this.targetObjects = targetObjects ?? throw new ArgumentNullException(nameof(targetObjects));
            this.contextOptions = contextOptions ?? throw new ArgumentNullException(nameof(contextOptions));
        }

        /// <summary>
        /// Loads the collection of foreign key entities associated with the specified predicate.
        /// </summary>
        /// <typeparam name="R">The type of the foreign key entity, which must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="predicate">An expression that specifies the relationship between the current entity and the foreign key entity.</param>
        /// <returns>A collection of foreign key entities that match the specified predicate.</returns>
        internal ICollection<T> LoadForeignKey<R>(Expression<Func<T, R>> predicate) where R : IRelmModel, new()
        {
            return LoadForeignKey(predicate, default(IRelmDataLoader<RelmModel>), null);
        }

        /// <summary>
        /// Loads a collection of related entities based on the specified foreign key relationship and additional
        /// constraints.
        /// </summary>
        /// <typeparam name="R">The type of the related entity, which must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="predicate">An expression that specifies the foreign key relationship between the current entity and the related entity.</param>
        /// <param name="additionalConstraints">An expression that defines additional constraints to apply when loading the related entities.</param>
        /// <returns>A collection of related entities of type <typeparamref name="R"/> that satisfy the specified relationship
        /// and constraints.</returns>
        internal ICollection<T> LoadForeignKey<R>(Expression<Func<T, R>> predicate, Expression<Func<R, object>> additionalConstraints) where R : IRelmModel, new()
        {
            return LoadForeignKey(predicate, default(IRelmDataLoader<RelmModel>), additionalConstraints);
        }

        /// <summary>
        /// Loads a collection of related entities based on the specified foreign key relationship.
        /// </summary>
        /// <typeparam name="R">The type of the related entity, which must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <typeparam name="S">The type of the data loader used to load the related entities, which must implement <see cref="IRelmModel"/>
        /// and have a parameterless constructor.</typeparam>
        /// <param name="predicate">An expression that specifies the foreign key relationship to load.</param>
        /// <param name="customDataLoader">An instance of a custom data loader used to retrieve the related entities.</param>
        /// <returns>A collection of related entities of type <typeparamref name="R"/>.</returns>
        internal ICollection<T> LoadForeignKey<R, S>(Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader) where R : IRelmModel, new() where S : IRelmModel, new()
        {
            return LoadForeignKey(predicate, customDataLoader, null);
        }

        /// <summary>
        /// Loads a collection of related entities based on the specified foreign key relationship.
        /// </summary>
        /// <typeparam name="R">The type of the related entities to load. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader. Must implement <see cref="IRelmModel"/> and have a
        /// parameterless constructor.</typeparam>
        /// <param name="predicate">An expression that specifies the foreign key relationship to load.</param>
        /// <param name="customDataLoader">An instance of a custom data loader used to retrieve the related entities.</param>
        /// <returns>A collection of related entities of type <typeparamref name="R"/>.</returns>
        internal ICollection<T> LoadForeignKey<R, S>(Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader) where R : IRelmModel, new() where S : IRelmModel, new()
        {
            return LoadForeignKey(predicate, customDataLoader, null);
        }

        /// <summary>
        /// Loads the collection of related entities for the specified foreign key relationship.
        /// </summary>
        /// <remarks>This method resolves the foreign key relationship defined by the provided expression
        /// and returns the associated entities.</remarks>
        /// <typeparam name="R">The type of the related entity, which must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="predicate">An expression specifying the foreign key relationship to load.</param>
        /// <returns>A collection of related entities of type <typeparamref name="R"/>. The collection may be empty if no related
        /// entities are found.</returns>
        internal ICollection<T> LoadForeignKey<R>(Expression<Func<T, ICollection<R>>> predicate) where R : IRelmModel, new()
        {
            return LoadForeignKey(predicate, default(IRelmDataLoader<RelmModel>), null);
        }

        /// <summary>
        /// Loads a collection of related entities based on the specified foreign key relationship.
        /// </summary>
        /// <typeparam name="R">The type of the related entity, which must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="predicate">An expression specifying the foreign key relationship to load.</param>
        /// <param name="additionalConstraints">An expression defining additional constraints to apply when loading the related entities.</param>
        /// <returns>A collection of related entities of type <typeparamref name="R"/> that satisfy the specified relationship
        /// and constraints.</returns>
        internal ICollection<T> LoadForeignKey<R>(Expression<Func<T, ICollection<R>>> predicate, Expression<Func<R, object>> additionalConstraints) where R : IRelmModel, new()
        {
            return LoadForeignKey(predicate, default(IRelmDataLoader<RelmModel>), additionalConstraints);
        }

        /// <summary>
        /// Loads a collection of related entities based on the specified foreign key relationship.
        /// </summary>
        /// <typeparam name="R">The type of the related entity, which must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <typeparam name="S">The type of the data loader entity, which must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="predicate">An expression that specifies the foreign key relationship between the current entity and the related entity.</param>
        /// <param name="customDataLoader">An instance of a custom data loader used to retrieve the related entities.</param>
        /// <param name="additionalConstraints">An expression that defines additional constraints to apply when loading the related entities.</param>
        /// <returns>A collection of related entities of type <typeparamref name="T"/> that satisfy the specified foreign key
        /// relationship and constraints.</returns>
        internal ICollection<T> LoadForeignKey<R, S>(Expression<Func<T, R>> predicate, IRelmDataLoader<S>? customDataLoader, Expression<Func<R, object>>? additionalConstraints) where R : IRelmModel, new() where S : IRelmModel, new()
        {
            return LoadForeignKeyInternal(predicate, customDataLoader, additionalConstraints);
        }

        /// <summary>
        /// Loads a collection of related entities based on the specified foreign key relationship.
        /// </summary>
        /// <typeparam name="R">The type of the related entities to load. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader. Must implement <see cref="IRelmModel"/> and have a
        /// parameterless constructor.</typeparam>
        /// <param name="predicate">An expression specifying the foreign key relationship to load.</param>
        /// <param name="customDataLoader">An optional custom data loader to use for retrieving the related entities.</param>
        /// <param name="additionalConstraints">An expression specifying additional constraints to apply when loading the related entities.</param>
        /// <returns>A collection of related entities that match the specified foreign key relationship and constraints.</returns>
        internal ICollection<T> LoadForeignKey<R, S>(Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S>? customDataLoader, Expression<Func<R, object>>? additionalConstraints) where R : IRelmModel, new() where S : IRelmModel, new()
        {
            return LoadForeignKeyInternal(predicate, customDataLoader, additionalConstraints);
        }

        /// <summary>
        /// Loads and resolves foreign key relationships for the specified target objects based on the provided
        /// predicate.
        /// </summary>
        /// <remarks>This method resolves foreign key relationships for the specified target objects by
        /// identifying the relevant context and dataset. If a custom data loader is provided, it is applied to the
        /// dataset before loading the foreign key relationships.  The <paramref name="predicate"/> parameter must
        /// represent a property or collection in the form of a lambda expression.  If the expression is invalid, an
        /// <see cref="InvalidOperationException"/> is thrown.  If the foreign key property is marked with the <see
        /// cref="RelmDataLoader"/> attribute, a <see cref="CustomAttributeFormatException"/> is thrown.</remarks>
        /// <typeparam name="R">The type of the related model that the foreign key points to. Must implement <see cref="IRelmModel"/> and
        /// have a parameterless constructor.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader, if provided. Must implement <see cref="IRelmModel"/>
        /// and have a parameterless constructor.</typeparam>
        /// <param name="predicate">An expression that specifies the foreign key property or collection to load.  Must be in the form of a
        /// lambda expression, such as <c>x => x.PropertyName</c>.</param>
        /// <param name="customDataLoader">An optional custom data loader to use for resolving the foreign key relationships.  If provided, it will be
        /// applied to the relevant dataset.</param>
        /// <param name="additionalConstraints">An optional expression specifying additional constraints to apply when resolving the foreign key
        /// relationships.</param>
        /// <returns>A collection of the target objects with their foreign key relationships resolved, or <see langword="null"/>
        /// if the target objects collection is empty.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the <paramref name="predicate"/> does not represent a valid property or collection in the form of
        /// a lambda expression.</exception>
        /// <exception cref="CustomAttributeFormatException">Thrown if the foreign key property specified by <paramref name="predicate"/> is marked with the <see
        /// cref="RelmDataLoader"/> attribute.</exception>
        private ICollection<T>? LoadForeignKeyInternal<R, S>(Expression predicate, IRelmDataLoader<S>? customDataLoader, Expression<Func<R, object>>? additionalConstraints) where R : IRelmModel, new() where S : IRelmModel, new()
        {
            if ((targetObjects?.Count ?? 0) == 0)
                return null;

            // get all types in the context assembly and look for one that inherits from RelmContext
            var member = (predicate as Expression<Func<T, R>>)?.Body ?? (predicate as Expression<Func<T, ICollection<R>>>)?.Body;
            var referenceProperty = member as MemberExpression
                ?? throw new InvalidOperationException("Collection or property must be represented by a lambda expression in the form of 'x => x.PropertyName'.");

            var referenceType = referenceProperty.Type;
            
            if (referenceProperty.Member.GetCustomAttribute<RelmDataLoader>() != null)
                throw new CustomAttributeFormatException($"Field [{referenceType.Name}] is a Data Loader field, not a Foreign Key field");

            // find the context that contains a dataset for T
            var relevantContext = (Assembly
                .GetAssembly(typeof(T))
                ?.GetTypes()
                .Where(x => x.BaseType == typeof(RelmContext))
                .FirstOrDefault(x => x
                    .GetProperties()
                    .Where(y => y.PropertyType == typeof(IRelmDataSet<T>))
                    .Any())) 
                ?? throw new InvalidOperationException($"No valid context found for type [{typeof(T).Name}].");

            var relevantDataSet = relevantContext.GetProperties().FirstOrDefault(x => x.PropertyType == typeof(IRelmDataSet<T>));

            var relevantProperty = relevantDataSet.PropertyType.GetGenericArguments().FirstOrDefault().GetProperties().FirstOrDefault(x => x.PropertyType == typeof(T))
                ?? relevantDataSet.PropertyType.GetGenericArguments().FirstOrDefault().GetProperties().FirstOrDefault(x => x.PropertyType.GenericTypeArguments.Any(y => y == typeof(T)));

            var contextArgs = new List<object> { contextOptions };

            // if we can't find a constructor with the builder by itself, look for one that has it plus other parameters
            var contextConstructor = relevantContext.GetConstructor([typeof(RelmContextOptionsBuilder)]);
            if (contextConstructor == null)
            {
                var allConstructors = relevantContext.GetConstructors();
                contextConstructor = allConstructors.FirstOrDefault(x => x.GetParameters().Select(y => y.ParameterType).Contains(typeof(RelmContextOptionsBuilder)));

                if (contextConstructor == null)
                    throw new InvalidOperationException($"No valid constructor found for context type [{relevantContext.Name}] that accepts a RelmContextOptionsBuilder parameter.");
                else
                {
                    foreach (var parameter in contextConstructor.GetParameters())
                    {
                        if (parameter.ParameterType != typeof(RelmContextOptionsBuilder))
                        {
                            contextArgs.Add(parameter.DefaultValue);
                        }
                    }
                }
            }

            var contextActivator = FastActivatorHelper.GetActivator<IRelmContext>(contextConstructor);
            var currentContext = contextActivator(contextArgs.ToArray());

            if (customDataLoader != null)
            {
                var returnType = (predicate as Expression<Func<T, R>>)?.ReturnType ?? (predicate as Expression<Func<T, ICollection<R>>>)?.ReturnType;
                if ((returnType?.IsGenericType ?? false) && returnType.GetGenericTypeDefinition() == typeof(ICollection<>))
                    returnType = returnType.GetGenericArguments()[0];

                var foreignDataSet = relevantContext.GetProperties().FirstOrDefault(x => x.PropertyType == typeof(IRelmDataSet<>).MakeGenericType(returnType));

                foreignDataSet
                    ?.PropertyType
                    .GetMethod(nameof(IRelmDataSet<T>.SetDataLoader))
                    ?.Invoke(foreignDataSet.GetValue(currentContext), new object[] { customDataLoader });
            }

            var executionCommand = new RelmExecutionCommand(Command.Reference, member);

            if (additionalConstraints != null)
                executionCommand.AddAdditionalCommand(Command.Reference, additionalConstraints.Body);
            
            var objectsLoader = new ForeignObjectsLoader<T>(targetObjects!, currentContext);
            objectsLoader.LoadForeignObjects(executionCommand);

            return targetObjects;
        }
    }
}
