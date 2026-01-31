using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.Models;
using CoreRelm.Options;
using CoreRelm.RelmInternal.Helpers.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.DataTransfer
{
    internal class DataLoaderHelper<T> where T : IRelmModel, new()
    {
        private readonly ICollection<T> targetObjects;
        private readonly IRelmQuickContext relmQuickContext;
        private readonly IRelmContext relmContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataLoaderHelper{T}"/> class with the specified context and
        /// target object.
        /// </summary>
        /// <param name="relmContext">The context used to interact with the Realm database. This parameter cannot be <see langword="null"/>.</param>
        /// <param name="targetObject">The target object to be managed by the helper. This parameter cannot be <see langword="null"/>.</param>
        public DataLoaderHelper(IRelmQuickContext relmContext, T targetObject)
        {
            this.targetObjects = new[] { targetObject };
            this.relmQuickContext = relmContext;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataLoaderHelper{T}"/> class with the specified context and
        /// target objects.
        /// </summary>
        /// <param name="relmContext">The context used to interact with the Realm database. This parameter cannot be <see langword="null"/>.</param>
        /// <param name="targetObjects">A collection of target objects to be processed. This parameter cannot be <see langword="null"/>.</param>
        public DataLoaderHelper(IRelmQuickContext relmContext, ICollection<T> targetObjects)
        {
            this.targetObjects = targetObjects;
            this.relmQuickContext = relmContext;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataLoaderHelper{T}"/> class with the specified context and
        /// target object.
        /// </summary>
        /// <param name="relmContext">The context used to interact with the data source. This parameter cannot be <see langword="null"/>.</param>
        /// <param name="targetObject">The target object to be processed by the data loader. This parameter cannot be <see langword="null"/>.</param>
        public DataLoaderHelper(IRelmContext relmContext, T targetObject)
        {
            this.targetObjects = new[] { targetObject };
            this.relmContext = relmContext;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataLoaderHelper{T}"/> class with the specified context and
        /// target objects.
        /// </summary>
        /// <param name="relmContext">The context used to interact with the data source. This parameter cannot be <see langword="null"/>.</param>
        /// <param name="targetObjects">A collection of target objects to be processed. This parameter cannot be <see langword="null"/>.</param>
        public DataLoaderHelper(IRelmContext relmContext, ICollection<T> targetObjects)
        {
            this.targetObjects = targetObjects;
            this.relmContext = relmContext;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataLoaderHelper{T}"/> class with the specified context options
        /// and target object.
        /// </summary>
        /// <remarks>This constructor sets up the <see cref="RelmQuickContext"/> using the provided
        /// options builder and initializes the helper with a single target object.</remarks>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the <see cref="RelmQuickContext"/> instance.</param>
        /// <param name="targetObject">The target object of type <typeparamref name="T"/> to be associated with this helper.</param>
        public DataLoaderHelper(RelmContextOptionsBuilder relmContextOptionsBuilder, T targetObject)
        {
            this.targetObjects = new[] { targetObject };
            this.relmQuickContext = new RelmQuickContext(relmContextOptionsBuilder);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataLoaderHelper{T}"/> class, which facilitates loading data
        /// into the specified target collection using the provided context options.
        /// </summary>
        /// <remarks>This class is designed to streamline data loading processes by leveraging the
        /// provided <see cref="RelmQuickContext"/> configured with the specified options. Ensure that the <paramref
        /// name="targetObjects"/> collection is properly initialized before using this helper.</remarks>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the <see cref="RelmQuickContext"/> for data operations.</param>
        /// <param name="targetObjects">The collection of objects of type <typeparamref name="T"/> that will be the target of data loading
        /// operations.</param>
        public DataLoaderHelper(RelmContextOptionsBuilder relmContextOptionsBuilder, ICollection<T> targetObjects)
        {
            this.targetObjects = targetObjects;
            this.relmQuickContext = new RelmQuickContext(relmContextOptionsBuilder);
        }

        /// <summary>
        /// Loads and populates a collection or property for the specified field based on the provided lambda
        /// expression.
        /// </summary>
        /// <remarks>This method uses the <see cref="RelmDataLoader"/> attribute on the specified field to
        /// determine the appropriate loader type. The loader type must implement either <see
        /// cref="IRelmQuickFieldLoader"/> or <see cref="IRelmFieldLoader"/>, depending on the context.</remarks>
        /// <typeparam name="R">The type of the field being loaded.</typeparam>
        /// <param name="predicate">A lambda expression representing the field to load, in the form of <c>x => x.PropertyName</c>. The field
        /// must be decorated with a <see cref="RelmDataLoader"/> attribute.</param>
        /// <returns>A collection of objects of type <typeparamref name="T"/> with the specified field populated.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the provided lambda expression does not represent a valid member expression.</exception>
        /// <exception cref="MemberAccessException">Thrown if the specified field does not have a <see cref="RelmDataLoader"/> attribute or if the attribute is
        /// not configured correctly.</exception>
        internal ICollection<T> LoadField<R>(Expression<Func<T, R>> predicate)
        {
            var referenceProperty = predicate.Body as MemberExpression
                ?? throw new InvalidOperationException("Collection or property must be represented by a lambda expression in the form of 'x => x.PropertyName'.");

            var dataLoaderAttribute = referenceProperty.Member.GetCustomAttributes<RelmDataLoader>()
                ?.FirstOrDefault(y => y.LoaderType?.GetInterface(relmContext == null ? nameof(IRelmQuickFieldLoader) : nameof(IRelmFieldLoader)) != null)
                ?? throw new MemberAccessException($"The property or collection [{referenceProperty.Member.Name}] on type [{referenceProperty.Expression.Type.Name}] does not have a RelmDataLoader attribute.");

            var fieldLoader = relmContext == null
                ? Activator.CreateInstance(dataLoaderAttribute.LoaderType, new object[] { relmQuickContext, referenceProperty.Member.Name, dataLoaderAttribute.KeyFields })
                : Activator.CreateInstance(dataLoaderAttribute.LoaderType, new object[] { relmContext, referenceProperty.Member.Name, dataLoaderAttribute.KeyFields });

            new FieldLoaderHelper<T>(targetObjects).LoadData((IRelmFieldLoaderBase)fieldLoader);

            return targetObjects;
        }
    }
}
