using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.Models;
using CoreRelm.Options;
using CoreRelm.RelmInternal.Helpers.DataTransfer;
using CoreRelm.RelmInternal.Helpers.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Extensions
{
    public static class ModelExtensions
    {

        /************************************************************************************************************************/
        /****************************************** RelmContext load singular property ******************************************/
        /************************************************************************************************************************/

        /// <summary>
        /// Loads a foreign key field for the specified target model using the provided context and predicate.
        /// </summary>
        /// <remarks>This method is an extension method that simplifies loading a foreign key field for a
        /// model by leveraging the provided context and predicate. The method ensures that the specified foreign key
        /// field is populated based on the context's configuration.</remarks>
        /// <typeparam name="T">The type of the target model. Must implement <see cref="IRelmModel"/> and have a parameterless constructor.</typeparam>
        /// <typeparam name="R">The type of the related model. Must implement <see cref="IRelmModel"/> and have a parameterless constructor.</typeparam>
        /// <param name="target">The target model instance for which the foreign key field will be loaded.</param>
        /// <param name="relmContext">The context used to load the foreign key field.</param>
        /// <param name="predicate">An expression specifying the foreign key field to load.</param>
        /// <returns>The target model instance with the specified foreign key field loaded.</returns>
        public static T LoadForeignKeyField<T, R>(this T target, IRelmContext relmContext, Expression<Func<T, R>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate);
        
        /// <summary>
        /// Loads and resolves a foreign key field for the specified target model, applying the given predicate and
        /// additional constraints.
        /// </summary>
        /// <remarks>This method is an extension method that simplifies the process of resolving foreign
        /// key fields in models. It uses the provided context and constraints to load the related model data.</remarks>
        /// <typeparam name="T">The type of the target model. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="target">The target model instance for which the foreign key field is being loaded.</param>
        /// <param name="relmContext">The context used to resolve the foreign key field, providing configuration and database access.</param>
        /// <param name="predicate">An expression specifying the foreign key relationship between the target model and the related model.</param>
        /// <param name="additionalConstraints">An expression specifying additional constraints to apply when resolving the foreign key field.</param>
        /// <returns>The target model instance with the foreign key field resolved.</returns>
        public static T LoadForeignKeyField<T, R>(this T target, IRelmContext relmContext, Expression<Func<T, R>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate, additionalConstraints);
        
        /// <summary>
        /// Loads and assigns a foreign key field for the specified target model using the provided data loader.
        /// </summary>
        /// <typeparam name="T">The type of the target model. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the foreign key model. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="target">The target model instance for which the foreign key field will be loaded.</param>
        /// <param name="relmContext">The context used to configure the data loading operation.</param>
        /// <param name="predicate">An expression identifying the foreign key field to be loaded.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the foreign key model.</param>
        /// <returns>The target model instance with the foreign key field loaded and assigned.</returns>
        public static T LoadForeignKeyField<T, R, S>(this T target, IRelmContext relmContext, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate, customDataLoader);
        
        /// <summary>
        /// Loads and initializes a foreign key field for the specified model, using the provided data loader and
        /// constraints.
        /// </summary>
        /// <remarks>This method provides a way to load and initialize a foreign key field for a model,
        /// allowing for custom data loading and additional constraints. It is particularly useful in scenarios where
        /// the default data loading behavior needs to be customized or extended.</remarks>
        /// <typeparam name="T">The type of the model containing the foreign key field. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model referenced by the foreign key. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="target">The instance of the model for which the foreign key field is being loaded.</param>
        /// <param name="relmContext">The context used to manage the data loading operation.</param>
        /// <param name="predicate">An expression identifying the foreign key field to be loaded.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related data.</param>
        /// <param name="additionalConstraints">An expression specifying additional constraints to apply when loading the related data.</param>
        /// <returns>The updated instance of the model with the foreign key field loaded.</returns>
        public static T LoadForeignKeyField<T, R, S>(this T target, IRelmContext relmContext, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate, customDataLoader, additionalConstraints);

        /************************************************************************************************************************/
        /****************************************** RelmQuickContext load singular property ******************************************/
        /************************************************************************************************************************/

        /// <summary>
        /// Loads the foreign key field specified by the given predicate for the target model.
        /// </summary>
        /// <remarks>This method is an extension method that simplifies loading a specific foreign key
        /// field for a model. The <paramref name="predicate"/> parameter should specify the property representing the
        /// foreign key relationship.</remarks>
        /// <typeparam name="T">The type of the target model, which must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model, which must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="target">The target model instance for which the foreign key field will be loaded.</param>
        /// <param name="relmContext">The context used to access the database or data source.</param>
        /// <param name="predicate">An expression specifying the foreign key field to load.</param>
        /// <returns>The target model instance with the specified foreign key field loaded.</returns>
        public static T LoadForeignKeyField<T, R>(this T target, IRelmQuickContext relmContext, Expression<Func<T, R>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate);
        
        /// <summary>
        /// Loads a foreign key field for the specified target model, applying the given predicate and additional
        /// constraints.
        /// </summary>
        /// <remarks>This method is an extension method that simplifies loading related data for a model
        /// by resolving foreign key relationships. Ensure that the <paramref name="relmContext"/> is properly
        /// configured to access the underlying data source.</remarks>
        /// <typeparam name="T">The type of the target model. Must implement <see cref="IRelmModel"/> and have a parameterless constructor.</typeparam>
        /// <typeparam name="R">The type of the related model. Must implement <see cref="IRelmModel"/> and have a parameterless constructor.</typeparam>
        /// <param name="target">The target model instance for which the foreign key field is to be loaded.</param>
        /// <param name="relmContext">The context used to access the database or data source.</param>
        /// <param name="predicate">An expression specifying the foreign key relationship between the target model and the related model.</param>
        /// <param name="additionalConstraints">An expression specifying additional constraints to apply when loading the related model.</param>
        /// <returns>The target model instance with the foreign key field loaded based on the specified predicate and
        /// constraints.</returns>
        public static T LoadForeignKeyField<T, R>(this T target, IRelmQuickContext relmContext, Expression<Func<T, R>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate, additionalConstraints);
        
        /// <summary>
        /// Loads and initializes a foreign key field for the specified model using the provided data loader and
        /// context.
        /// </summary>
        /// <remarks>This method is an extension method that simplifies the process of loading foreign key
        /// fields for models in the Relm framework. It delegates the operation to an overload that uses the context
        /// options from the provided <paramref name="relmContext"/>.</remarks>
        /// <typeparam name="T">The type of the target model. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the foreign key field to be loaded. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="target">The target model instance for which the foreign key field will be loaded.</param>
        /// <param name="relmContext">The context providing configuration and options for the operation.</param>
        /// <param name="predicate">An expression identifying the foreign key field to be loaded.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the foreign key field data.</param>
        /// <returns>The target model instance with the specified foreign key field loaded.</returns>
        public static T LoadForeignKeyField<T, R, S>(this T target, IRelmQuickContext relmContext, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate, customDataLoader);
        
        /// <summary>
        /// Loads and initializes a foreign key field for the specified target model using the provided context,
        /// predicate, and data loader.
        /// </summary>
        /// <remarks>This method is an extension method that simplifies the process of loading foreign key
        /// fields in models. It delegates the operation to an overload that uses the context options from the provided
        /// <paramref name="relmContext"/>.</remarks>
        /// <typeparam name="T">The type of the target model. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the foreign key model. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="target">The target model instance for which the foreign key field will be loaded.</param>
        /// <param name="relmContext">The context used to configure and manage the data loading operation.</param>
        /// <param name="predicate">An expression identifying the foreign key field to be loaded on the target model.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the foreign key data.</param>
        /// <param name="additionalConstraints">An optional expression specifying additional constraints to apply when loading the foreign key data.</param>
        /// <returns>The target model instance with the specified foreign key field loaded and initialized.</returns>
        public static T LoadForeignKeyField<T, R, S>(this T target, IRelmQuickContext relmContext, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate, customDataLoader, additionalConstraints);

        /************************************************************************************************************************/
        /****************************************** Context options load singular property ******************************************/
        /************************************************************************************************************************/

        /// <summary>
        /// Loads a foreign key field for the specified target model using the provided context options and predicate.
        /// </summary>
        /// <remarks>This method uses a <see cref="ForeignKeyLoader{T}"/> to load the foreign key field
        /// based on the provided predicate.</remarks>
        /// <typeparam name="T">The type of the target model. Must implement <see cref="IRelmModel"/> and have a parameterless constructor.</typeparam>
        /// <typeparam name="R">The type of the related model. Must implement <see cref="IRelmModel"/> and have a parameterless constructor.</typeparam>
        /// <param name="target">The target model instance for which the foreign key field is to be loaded.</param>
        /// <param name="relmContextOptionsBuilder">The context options builder used to configure the database context for the operation.</param>
        /// <param name="predicate">An expression specifying the foreign key field to load.</param>
        /// <returns>The first related model instance matching the foreign key field, or <see langword="null"/> if no match is
        /// found.</returns>
        public static T LoadForeignKeyField<T, R>(this T target, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, R>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate).FirstOrDefault();
        
        /// <summary>
        /// Loads a foreign key field for the specified target model, applying the given predicate and additional
        /// constraints.
        /// </summary>
        /// <remarks>This method is typically used to load a related entity for a given model instance
        /// based on a foreign key relationship. Ensure that the provided expressions are valid and compatible with the
        /// underlying data context.</remarks>
        /// <typeparam name="T">The type of the target model. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="target">The target model instance for which the foreign key field is being loaded.</param>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the context for the operation.</param>
        /// <param name="predicate">An expression specifying the foreign key relationship to load.</param>
        /// <param name="additionalConstraints">An expression specifying additional constraints to apply when loading the foreign key field.</param>
        /// <returns>The first related model that matches the specified predicate and constraints, or <see langword="null"/> if
        /// no match is found.</returns>
        public static T LoadForeignKeyField<T, R>(this T target, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, R>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, additionalConstraints).FirstOrDefault();
        
        /// <summary>
        /// Loads a foreign key field for the specified target model using the provided predicate and data loader.
        /// </summary>
        /// <remarks>This method uses a <see cref="ForeignKeyLoader{T}"/> to load the foreign key field
        /// based on the provided predicate and custom data loader. The method returns the first related model found, or
        /// <see langword="null"/> if no data matches.</remarks>
        /// <typeparam name="T">The type of the target model. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model represented by the foreign key. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="target">The target model instance for which the foreign key field is being loaded.</param>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the context for data loading.</param>
        /// <param name="predicate">An expression that specifies the foreign key field to load.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related data.</param>
        /// <returns>The first related model of type <typeparamref name="R"/> loaded for the specified foreign key field, or <see
        /// langword="null"/> if no related data is found.</returns>
        public static T LoadForeignKeyField<T, R, S>(this T target, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader).FirstOrDefault();
        
        /// <summary>
        /// Loads a foreign key field for the specified target model using the provided predicate, data loader, and
        /// additional constraints.
        /// </summary>
        /// <typeparam name="T">The type of the target model. Must implement <see cref="IRelmModel"/> and have a parameterless constructor.</typeparam>
        /// <typeparam name="R">The type of the related model representing the foreign key. Must implement <see cref="IRelmModel"/> and have
        /// a parameterless constructor.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader. Must implement <see cref="IRelmModel"/> and have a
        /// parameterless constructor.</typeparam>
        /// <param name="target">The target model instance for which the foreign key field is being loaded.</param>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the context for the operation.</param>
        /// <param name="predicate">An expression specifying the foreign key property on the target model.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related data.</param>
        /// <param name="additionalConstraints">An expression specifying additional constraints to apply when loading the foreign key field.</param>
        /// <returns>The first related model that matches the specified predicate and constraints, or <see langword="null"/> if
        /// no match is found.</returns>
        public static T LoadForeignKeyField<T, R, S>(this T target, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader, additionalConstraints).FirstOrDefault();

        /************************************************************************************************************************/
        /****************************************** RelmContext load collection property ******************************************/
        /************************************************************************************************************************/

        /// <summary>
        /// Loads a foreign key collection property for the specified target model using the provided context and
        /// predicate.
        /// </summary>
        /// <remarks>This method is an extension method that simplifies the process of loading a foreign
        /// key collection property for a model. It uses the provided <paramref name="relmContext"/> and <paramref
        /// name="predicate"/> to determine which collection property to load.</remarks>
        /// <typeparam name="T">The type of the target model, which must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model in the collection, which must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="target">The target model instance for which the foreign key collection property will be loaded.</param>
        /// <param name="relmContext">The context used to load the foreign key collection property.</param>
        /// <param name="predicate">An expression specifying the foreign key collection property to load.</param>
        /// <returns>The target model instance with the specified foreign key collection property loaded.</returns>
        public static T LoadForeignKeyField<T, R>(this T target, IRelmContext relmContext, Expression<Func<T, ICollection<R>>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate);

        /// <summary>
        /// Loads and initializes a foreign key collection property for the specified target model, applying the given constraints.
        /// </summary>
        /// <typeparam name="T">The type of the target model that contains the foreign key field. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model referenced by the foreign key field. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="target">The target model instance for which the foreign key field will be loaded.</param>
        /// <param name="relmContext">The context used to manage the relationship and database operations.</param>
        /// <param name="predicate">An expression specifying the foreign key field to be loaded as a collection of related models.</param>
        /// <param name="additionalConstraints">An expression defining additional constraints to filter the related models.</param>
        /// <returns>The target model instance with the foreign key field loaded and initialized.</returns>
        public static T LoadForeignKeyField<T, R>(this T target, IRelmContext relmContext, Expression<Func<T, ICollection<R>>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate, additionalConstraints);

        /// <summary>
        /// Loads and initializes a foreign key collection property for the specified target model using the provided data loader.
        /// </summary>
        /// <typeparam name="T">The type of the target model that contains the foreign key field.</typeparam>
        /// <typeparam name="R">The type of the related model in the foreign key collection.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader.</typeparam>
        /// <param name="target">The target model instance for which the foreign key field will be loaded.</param>
        /// <param name="relmContext">The context used to manage the data loading operation.</param>
        /// <param name="predicate">An expression specifying the foreign key collection property on the target model.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related data.</param>
        /// <returns>The target model instance with the foreign key field loaded.</returns>
        public static T LoadForeignKeyField<T, R, S>(this T target, IRelmContext relmContext, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate, customDataLoader);

        /// <summary>
        /// Loads a foreign key collection property for the specified target model, resolving related entities based on the provided
        /// predicate and constraints.
        /// </summary>
        /// <remarks>This method facilitates the loading of related entities for a foreign key collection
        /// property on the target model. The <paramref name="customDataLoader"/> allows for custom logic to be applied
        /// during the data loading process, and the <paramref name="additionalConstraints"/> can be used to further
        /// filter the related entities.</remarks>
        /// <typeparam name="T">The type of the target model. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model in the foreign key collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="target">The target model instance for which the foreign key field will be loaded.</param>
        /// <param name="relmContext">The context used to manage the data loading operation.</param>
        /// <param name="predicate">An expression specifying the foreign key collection property on the target model.</param>
        /// <param name="customDataLoader">A custom data loader used to resolve the related entities.</param>
        /// <param name="additionalConstraints">An expression specifying additional constraints to apply when resolving the related entities.</param>
        /// <returns>The target model instance with the foreign key field loaded.</returns>
        public static T LoadForeignKeyField<T, R, S>(this T target, IRelmContext relmContext, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate, customDataLoader, additionalConstraints);

        /************************************************************************************************************************/
        /****************************************** RelmQuickContext load collection property ******************************************/
        /************************************************************************************************************************/

        /// <summary>
        /// Loads a foreign key collection property for the specified target model.
        /// </summary>
        /// <remarks>This method is an extension method that simplifies loading a foreign key collection
        /// property for a model. It uses the provided context and predicate to retrieve and populate the related
        /// data.</remarks>
        /// <typeparam name="T">The type of the target model, which must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model in the collection, which must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="target">The target model instance for which the foreign key collection property will be loaded.</param>
        /// <param name="relmContext">The context providing access to the database and configuration options.</param>
        /// <param name="predicate">An expression identifying the foreign key collection property to load.</param>
        /// <returns>The target model instance with the specified foreign key collection property loaded.</returns>
        public static T LoadForeignKeyField<T, R>(this T target, IRelmQuickContext relmContext, Expression<Func<T, ICollection<R>>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate);

        /// <summary>
        /// Loads a foreign key collection property for the specified target model, resolving the related collection based on the
        /// provided predicate and additional constraints.
        /// </summary>
        /// <typeparam name="T">The type of the target model. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model in the foreign key relationship. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="target">The target model instance for which the foreign key field is being loaded.</param>
        /// <param name="relmContext">The context providing access to the database and configuration options.</param>
        /// <param name="predicate">An expression specifying the foreign key collection property on the target model.</param>
        /// <param name="additionalConstraints">An expression defining additional constraints to filter the related models.</param>
        /// <returns>The target model instance with the foreign key field loaded.</returns>
        public static T LoadForeignKeyField<T, R>(this T target, IRelmQuickContext relmContext, Expression<Func<T, ICollection<R>>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate, additionalConstraints);

        /// <summary>
        /// Loads and initializes a foreign key collection property for the specified target model, using the provided data loader and
        /// predicate.
        /// </summary>
        /// <remarks>This method is an extension method that simplifies the process of loading and
        /// initializing foreign key fields for models in a relational context. The <paramref name="customDataLoader"/>
        /// is used to retrieve the related data, and the <paramref name="predicate"/> specifies the foreign key
        /// collection to be populated.</remarks>
        /// <typeparam name="T">The type of the target model. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model in the foreign key collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="target">The target model instance for which the foreign key field will be loaded.</param>
        /// <param name="relmContext">The context providing configuration and options for the operation.</param>
        /// <param name="predicate">An expression specifying the foreign key collection property on the target model.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related data.</param>
        /// <returns>The target model instance with the foreign key field loaded.</returns>
        public static T LoadForeignKeyField<T, R, S>(this T target, IRelmQuickContext relmContext, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate, customDataLoader);

        /// <summary>
        /// Loads and populates a foreign key collection property for the specified target model using the provided data loader and
        /// constraints.
        /// </summary>
        /// <remarks>This method is an extension method that simplifies the process of loading related
        /// data for a foreign key field. It uses the provided <paramref name="customDataLoader"/> and <paramref
        /// name="additionalConstraints"/> to retrieve and populate the related data.</remarks>
        /// <typeparam name="T">The type of the target model that contains the foreign key field. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model in the foreign key collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="target">The target model instance for which the foreign key field will be loaded.</param>
        /// <param name="relmContext">The context providing configuration and services for data loading.</param>
        /// <param name="predicate">An expression identifying the foreign key collection property on the target model.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related data.</param>
        /// <param name="additionalConstraints">An expression specifying additional constraints to apply when loading the related data.</param>
        /// <returns>The target model instance with the foreign key field populated.</returns>
        public static T LoadForeignKeyField<T, R, S>(this T target, IRelmQuickContext relmContext, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate, customDataLoader, additionalConstraints);

        /************************************************************************************************************************/
        /****************************************** Context options load collection property ******************************************/
        /************************************************************************************************************************/

        /// <summary>
        /// Loads a foreign key collection property for the specified target model and returns the first related entity.
        /// </summary>
        /// <remarks>This method uses a <see cref="ForeignKeyLoader{T}"/> to load the foreign key field
        /// based on the specified predicate.</remarks>
        /// <typeparam name="T">The type of the target model, which must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model, which must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="target">The target model instance for which the foreign key field is being loaded.</param>
        /// <param name="relmContextOptionsBuilder">The context options builder used to configure the data context.</param>
        /// <param name="predicate">An expression specifying the collection property on the target model that represents the foreign key
        /// relationship.</param>
        /// <returns>The first related entity of type <typeparamref name="R"/> from the foreign key field, or <see
        /// langword="null"/> if no related entities are found.</returns>
        public static T LoadForeignKeyField<T, R>(this T target, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, ICollection<R>>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate).FirstOrDefault();

        /// <summary>
        /// Loads a foreign key collection property for the specified target model, applying the given predicate and additional
        /// constraints.
        /// </summary>
        /// <typeparam name="T">The type of the target model. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="target">The target model instance for which the foreign key field is being loaded.</param>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the database context.</param>
        /// <param name="predicate">An expression specifying the collection navigation property on the target model.</param>
        /// <param name="additionalConstraints">An expression specifying additional constraints to apply when loading the related models.</param>
        /// <returns>The first related model that matches the specified predicate and constraints, or <see langword="null"/> if
        /// no match is found.</returns>
        public static T LoadForeignKeyField<T, R>(this T target, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, ICollection<R>>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, additionalConstraints).FirstOrDefault();

        /// <summary>
        /// Loads a foreign key collection property for the specified target model using the provided predicate and custom data
        /// loader.
        /// </summary>
        /// <remarks>This method uses a <see cref="ForeignKeyLoader{T}"/> to load the foreign key field
        /// based on the specified predicate and custom data loader. The method is designed to return only the first
        /// related model from the collection.</remarks>
        /// <typeparam name="T">The type of the target model. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model in the foreign key collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="target">The target model instance for which the foreign key field is being loaded.</param>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the data loading context.</param>
        /// <param name="predicate">An expression specifying the foreign key collection property on the target model.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related data.</param>
        /// <returns>The first related model from the foreign key collection, or <see langword="null"/> if no related models are
        /// found.</returns>
        public static T LoadForeignKeyField<T, R, S>(this T target, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader).FirstOrDefault();

        /// <summary>
        /// Loads a foreign key collection property for the specified target model, applying the given constraints and using a custom
        /// data loader.
        /// </summary>
        /// <typeparam name="T">The type of the target model that implements <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model that implements <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader that implements <see cref="IRelmModel"/>.</typeparam>
        /// <param name="target">The target model instance for which the foreign key field is being loaded.</param>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the context for data loading.</param>
        /// <param name="predicate">An expression specifying the navigation property on the target model that represents the foreign key
        /// relationship.</param>
        /// <param name="customDataLoader">The custom data loader used to retrieve the related data.</param>
        /// <param name="additionalConstraints">An expression specifying additional constraints to apply when loading the related data.</param>
        /// <returns>The first related entity that matches the specified constraints, or <see langword="null"/> if no matching
        /// entity is found.</returns>
        public static T LoadForeignKeyField<T, R, S>(this T target, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader, additionalConstraints).FirstOrDefault();

        /************************************************************************************************************************/
        /****************************************** Data loader fields ******************************************/
        /************************************************************************************************************************/

        /// <summary>
        /// Loads a related data field for the specified model using the provided predicate.
        /// </summary>
        /// <remarks>This method is an extension method that facilitates loading a specific related data
        /// field for a model. The <paramref name="predicate"/> parameter is used to identify the field to be
        /// loaded.</remarks>
        /// <typeparam name="T">The type of the input model, which must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the related data field, which must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="inputModel">The model instance for which the related data field is to be loaded.</param>
        /// <param name="relmContext">The context used to access the data source.</param>
        /// <param name="predicate">An expression specifying the related data field to load.</param>
        /// <returns>The input model with the specified related data field loaded.</returns>
        public static T LoadDataLoaderField<T, S>(this T inputModel, IRelmContext relmContext, Expression<Func<T, S>> predicate) where T : IRelmModel, new() where S : IRelmModel, new()
            => new DataLoaderHelper<T>(relmContext, inputModel).LoadField(predicate).FirstOrDefault();

        /// <summary>
        /// Loads a related data loader field for the specified model using the provided context and predicate.
        /// </summary>
        /// <remarks>This method is an extension method that facilitates the loading of related data
        /// fields in a model. It uses the provided predicate to identify the field to be loaded and the context to
        /// resolve the data.</remarks>
        /// <typeparam name="T">The type of the input model, which must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <typeparam name="S">The type of the related model, which must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="inputModel">The model instance for which the data loader field is to be loaded.</param>
        /// <param name="relmQuickContext">The context used to resolve the data loader field.</param>
        /// <param name="predicate">An expression specifying the property of the input model that represents the related data loader field.</param>
        /// <returns>The input model with the specified data loader field loaded.</returns>
        public static T LoadDataLoaderField<T, S>(this T inputModel, IRelmQuickContext relmQuickContext, Expression<Func<T, S>> predicate) where T : IRelmModel, new() where S : IRelmModel, new()
            => new DataLoaderHelper<T>(relmQuickContext, inputModel).LoadField(predicate).FirstOrDefault();

        /// <summary>
        /// Loads a related data loader field for the specified input model using the provided context options and
        /// predicate.
        /// </summary>
        /// <remarks>This method is an extension method that simplifies the process of loading a related
        /// data loader field for a given model. It creates a new <see cref="RelmContext"/> using the provided options
        /// builder and applies the specified predicate to determine the field to load.</remarks>
        /// <typeparam name="T">The type of the input model, which must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <typeparam name="S">The type of the related model to load, which must implement <see cref="IRelmModel"/> and have a
        /// parameterless constructor.</typeparam>
        /// <param name="inputModel">The input model instance for which the related data loader field is to be loaded.</param>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the <see cref="RelmContext"/> for the operation.</param>
        /// <param name="predicate">An expression specifying the related data loader field to load.</param>
        /// <returns>The input model with the specified related data loader field loaded.</returns>
        public static T LoadDataLoaderField<T, S>(this T inputModel, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, S>> predicate) where T : IRelmModel, new() where S : IRelmModel, new()
            => new DataLoaderHelper<T>(relmContextOptionsBuilder, inputModel).LoadField(predicate).FirstOrDefault();
    }
}
