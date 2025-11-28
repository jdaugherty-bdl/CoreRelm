using MySql.Data.MySqlClient;
using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.Models;
using CoreRelm.Options;
using CoreRelm.RelmInternal.Helpers.DataTransfer;
using CoreRelm.RelmInternal.Helpers.DataTransfer.Persistence;
using CoreRelm.RelmInternal.Helpers.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Extensions
{
    /// <summary>
    /// Provides a set of extension methods for performing database operations,  loading related data, and manipulating
    /// collections in the context of  relational models.
    /// </summary>
    /// <remarks>The <see cref="ListExtensions"/> class includes methods for bulk writing  data to a database,
    /// loading foreign key fields, flattening hierarchical  data structures, generating DTOs, and retrieving dictionary
    /// entries.  These methods are designed to work with relational models that implement  the <see cref="IRelmModel"/>
    /// interface and are optimized for scenarios  involving database interactions and data transformation.  Many
    /// methods in this class support advanced customization through optional  parameters, such as specifying table
    /// names, batch sizes, and constraints  for foreign key loading. Additionally, the methods are designed to be  used
    /// with dependency injection and context-based database operations.</remarks>
    public static class ListExtensions
    {
        /****************************************** Write database data ******************************************/

        /// <summary>
        /// Writes a collection of model data to a database table in bulk.
        /// </summary>
        /// <remarks>This method performs a bulk write operation, which is optimized for high performance
        /// when inserting large amounts of data. Ensure that the database schema matches the structure of the model
        /// data to avoid runtime errors.</remarks>
        /// <typeparam name="T">The type of the model data, which must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="modelData">The collection of model data to be written to the database.</param>
        /// <param name="connectionName">The database connection identifier, represented as an enumeration value.</param>
        /// <param name="tableName">The name of the database table to write to. If <see langword="null"/>, the default table name for the model
        /// type <typeparamref name="T"/> is used.</param>
        /// <param name="forceType">An optional type to enforce during the write operation. If <see langword="null"/>, the type of <typeparamref
        /// name="T"/> is used.</param>
        /// <param name="allowUserVariables">A value indicating whether user-defined variables are allowed in the database operation. The default is <see
        /// langword="false"/>.</param>
        /// <param name="batchSize">The number of records to write in each batch. The default is 100.</param>
        /// <param name="databaseName">The name of the database to write to. If <see langword="null"/>, the default database for the connection is
        /// used.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns are allowed to be written. The default is <see
        /// langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are allowed to be written. The default is <see
        /// langword="false"/>.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns are allowed to be written. The default is <see langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether auto-generated date columns are allowed to be written. The default is <see
        /// langword="false"/>.</param>
        /// <returns>The number of records successfully written to the database.</returns>
        public static int WriteToDatabase<T>(this IEnumerable<T> modelData, Enum connectionName, string tableName = null, Type forceType = null, bool allowUserVariables = false, int batchSize = 100, string databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false) where T : IRelmModel
        	=> DataOutputOperations.BulkTableWrite<T>(connectionName, modelData, tableName, forceType, allowUserVariables: allowUserVariables, batchSize, databaseName, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
        
        /// <summary>
        /// Writes a collection of model data to a database table in bulk.
        /// </summary>
        /// <remarks>This method performs a bulk write operation, which is optimized for inserting large
        /// amounts of data efficiently. The caller is responsible for ensuring that the <paramref
        /// name="existingConnection"/> is open and valid before calling this method. If <paramref
        /// name="sqlTransaction"/> is provided, the operation will be part of the specified transaction.</remarks>
        /// <typeparam name="T">The type of the model data, which must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="modelData">The collection of model data to write to the database.</param>
        /// <param name="existingConnection">An open <see cref="MySqlConnection"/> to the database where the data will be written.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to use for the operation. If null, no transaction is used.</param>
        /// <param name="tableName">The name of the database table to write to. If null, the table name is inferred from the model type.</param>
        /// <param name="forceType">An optional <see cref="Type"/> to enforce a specific model type for the operation. If null, the type of
        /// <typeparamref name="T"/> is used.</param>
        /// <param name="batchSize">The number of rows to write in each batch. Defaults to 100.</param>
        /// <param name="databaseName">The name of the database to write to. If null, the default database for the connection is used.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns are allowed to be written. Defaults to <see langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns are allowed to be written. Defaults to <see langword="false"/>.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns are allowed to be written. Defaults to <see langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">Indicates whether auto-generated date columns are allowed to be written. Defaults to <see
        /// langword="false"/>.</param>
        /// <returns>The number of rows successfully written to the database.</returns>
        public static int WriteToDatabase<T>(this IEnumerable<T> modelData, MySqlConnection existingConnection, MySqlTransaction sqlTransaction = null, string tableName = null, Type forceType = null, int batchSize = 100, string databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false) where T : IRelmModel
        	=> DataOutputOperations.BulkTableWrite<T>(existingConnection, modelData, tableName, sqlTransaction: sqlTransaction, forceType, batchSize, databaseName, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
        
        /// <summary>
        /// Writes the specified collection of data to a database table in bulk.
        /// </summary>
        /// <remarks>This method performs a bulk write operation, which is optimized for performance when
        /// inserting large amounts of data. The table schema is inferred from the type <typeparamref name="T"/> unless
        /// a specific schema is enforced using the <paramref name="forceType"/> parameter.</remarks>
        /// <typeparam name="T">The type of the data objects to be written to the database.</typeparam>
        /// <param name="modelData">The collection of data objects to write to the database. Cannot be null or empty.</param>
        /// <param name="relmContext">The database context used to perform the write operation. Cannot be null.</param>
        /// <param name="tableName">The name of the database table to write to. If null, the table name is inferred from the type <typeparamref
        /// name="T"/>.</param>
        /// <param name="forceType">An optional type to enforce for the database table schema. If null, the schema is inferred from
        /// <typeparamref name="T"/>.</param>
        /// <param name="batchSize">The number of records to write in each batch. Defaults to 100. Must be greater than zero.</param>
        /// <param name="databaseName">The name of the database to write to. If null, the default database is used.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns are allowed in the table. Defaults to <see langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns are allowed in the table. Defaults to <see langword="false"/>.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns are allowed in the table. Defaults to <see langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">Indicates whether auto-generated date columns are allowed in the table. Defaults to <see langword="false"/>.</param>
        /// <returns>The number of records successfully written to the database.</returns>
        public static int WriteToDatabase<T>(this IEnumerable<T> modelData, IRelmContext relmContext, string tableName = null, Type forceType = null, int batchSize = 100, string databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        	=> DataOutputOperations.BulkTableWrite<T>(relmContext, modelData, tableName, forceType, batchSize, databaseName, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
        
        /// <summary>
        /// Writes the specified collection of data to a database table in bulk.
        /// </summary>
        /// <remarks>This method performs a bulk write operation, which is optimized for performance when
        /// inserting large amounts of data. The behavior of the write operation can be customized using the optional
        /// parameters.</remarks>
        /// <typeparam name="T">The type of the data objects to be written to the database.</typeparam>
        /// <param name="modelData">The collection of data objects to write to the database. Cannot be null or empty.</param>
        /// <param name="relmContext">The database context used to perform the write operation. Cannot be null.</param>
        /// <param name="tableName">The name of the database table to write to. If null, the table name is inferred from the type <typeparamref
        /// name="T"/>.</param>
        /// <param name="forceType">An optional type to enforce for the database table schema. If null, the schema is inferred from the type
        /// <typeparamref name="T"/>.</param>
        /// <param name="batchSize">The number of records to write in each batch. Defaults to 100. Must be greater than zero.</param>
        /// <param name="databaseName">The name of the database to write to. If null, the default database associated with <paramref
        /// name="relmContext"/> is used.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether columns with auto-increment constraints are allowed to be written. Defaults to
        /// <see langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are allowed to be written. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowUniqueColumns">A value indicating whether columns with unique constraints are allowed to be written. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether columns with automatic date constraints are allowed to be written. Defaults to
        /// <see langword="false"/>.</param>
        /// <returns>The total number of records successfully written to the database.</returns>
        public static int WriteToDatabase<T>(this IEnumerable<T> modelData, IRelmQuickContext relmContext, string tableName = null, Type forceType = null, int batchSize = 100, string databaseName = null, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        	=> DataOutputOperations.BulkTableWrite<T>(relmContext, modelData, tableName, forceType, batchSize, databaseName, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);

        /************************************************************************************************************************/
        /****************************************** RelmContext load singular property ******************************************/
        /************************************************************************************************************************/

        /// <summary>
        /// Loads and resolves a foreign key field for a collection of models.
        /// </summary>
        /// <remarks>This method uses the provided <paramref name="relmContext"/> to resolve the foreign
        /// key relationships for the specified field in the collection of models. The <paramref name="predicate"/>
        /// determines which foreign key field to load.</remarks>
        /// <typeparam name="T">The type of the model in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model to be loaded. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="modelData">The collection of models for which the foreign key field will be loaded.</param>
        /// <param name="relmContext">The context used to resolve the foreign key relationships.</param>
        /// <param name="predicate">An expression specifying the foreign key field to load.</param>
        /// <returns>A collection of models with the specified foreign key field resolved.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R>(this ICollection<T> modelData, IRelmContext relmContext, Expression<Func<T, R>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmContext).LoadForeignKey(predicate);
        
        /// <summary>
        /// Loads and resolves a foreign key field for a collection of models, applying the specified predicate and
        /// additional constraints.
        /// </summary>
        /// <remarks>This method is an extension method that simplifies the process of resolving foreign
        /// key relationships for a collection of models. It uses the provided <paramref name="relmContext"/> to perform
        /// the resolution and applies the specified <paramref name="predicate"/> to define the relationship. Additional
        /// constraints can be applied using the <paramref name="additionalConstraints"/> parameter.</remarks>
        /// <typeparam name="T">The type of the primary model in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model to be loaded. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="modelData">The collection of primary models for which the foreign key field will be loaded.</param>
        /// <param name="relmContext">The context used to resolve the foreign key relationships.</param>
        /// <param name="predicate">An expression specifying the foreign key relationship between the primary and related models.</param>
        /// <param name="additionalConstraints">An optional expression specifying additional constraints to apply when resolving the foreign key field.</param>
        /// <returns>A collection of the primary models with the foreign key field resolved based on the specified predicate and
        /// constraints.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R>(this ICollection<T> modelData, IRelmContext relmContext, Expression<Func<T, R>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmContext).LoadForeignKey(predicate, additionalConstraints);
        
        /// <summary>
        /// Loads and resolves a foreign key field for a collection of models, using the specified predicate and custom
        /// data loader.
        /// </summary>
        /// <remarks>This method is an extension method that facilitates the resolution of foreign key
        /// relationships for a collection of models. The <paramref name="customDataLoader"/> allows for custom logic to
        /// be applied when loading the related data.</remarks>
        /// <typeparam name="T">The type of the model in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model being loaded. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="modelData">The collection of models for which the foreign key field will be loaded.</param>
        /// <param name="relmContext">The context used to interact with the data source.</param>
        /// <param name="predicate">An expression specifying the foreign key field to load.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related data.</param>
        /// <returns>The original collection of models with the specified foreign key field resolved.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R, S>(this ICollection<T> modelData, IRelmContext relmContext, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmContext).LoadForeignKey(predicate, customDataLoader);
        
        /// <summary>
        /// Loads and resolves a foreign key field for a collection of models, applying optional constraints and using a
        /// custom data loader.
        /// </summary>
        /// <remarks>This method is an extension method designed to simplify the process of resolving
        /// foreign key relationships in a collection of models. It allows for the use of a custom data loader and
        /// additional constraints to tailor the loading process to specific requirements.</remarks>
        /// <typeparam name="T">The type of the primary model in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model representing the foreign key. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="modelData">The collection of primary models for which the foreign key field will be loaded.</param>
        /// <param name="relmContext">The context used to interact with the data source.</param>
        /// <param name="predicate">An expression specifying the foreign key field to load in the primary model.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related data for the foreign key field.</param>
        /// <param name="additionalConstraints">An optional expression specifying additional constraints to apply when resolving the foreign key field.</param>
        /// <returns>A collection of the primary models with the specified foreign key field resolved and populated.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R, S>(this ICollection<T> modelData, IRelmContext relmContext, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmContext).LoadForeignKey(predicate, customDataLoader, additionalConstraints);

        /************************************************************************************************************************/
        /****************************************** RelmQuickContext load singular property ******************************************/
        /************************************************************************************************************************/

        /// <summary>
        /// Loads and resolves a foreign key field for the specified collection of models.
        /// </summary>
        /// <remarks>This method uses the provided <paramref name="relmQuickContext"/> to resolve the
        /// foreign key relationships for the specified field in the collection of models. The foreign key field is
        /// loaded based on the <paramref name="predicate"/> expression.</remarks>
        /// <typeparam name="T">The type of the model in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model to be loaded. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="modelData">The collection of models for which the foreign key field will be loaded.</param>
        /// <param name="relmQuickContext">The database context used to resolve the foreign key relationships.</param>
        /// <param name="predicate">An expression specifying the foreign key field to load.</param>
        /// <returns>The original collection of models with the specified foreign key field resolved and loaded.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R>(this ICollection<T> modelData, IRelmQuickContext relmQuickContext, Expression<Func<T, R>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmQuickContext).LoadForeignKey(predicate);
        
        /// <summary>
        /// Loads and resolves a foreign key field for a collection of models, applying the specified constraints.
        /// </summary>
        /// <remarks>This method is an extension method designed to simplify the process of resolving
        /// foreign key relationships for a collection of models. It uses the provided database context and expressions
        /// to load the related data efficiently. The additional constraints can be used to filter or customize the
        /// resolution process.</remarks>
        /// <typeparam name="T">The type of the primary model in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model representing the foreign key. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="modelData">The collection of primary models for which the foreign key field will be loaded.</param>
        /// <param name="relmQuickContext">The database context used to resolve the foreign key relationships.</param>
        /// <param name="predicate">An expression specifying the foreign key relationship between the primary model and the related model.</param>
        /// <param name="additionalConstraints">An optional expression specifying additional constraints to apply when resolving the foreign key field.</param>
        /// <returns>A collection of the primary models with the foreign key field resolved and populated.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R>(this ICollection<T> modelData, IRelmQuickContext relmQuickContext, Expression<Func<T, R>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmQuickContext).LoadForeignKey(predicate, additionalConstraints);
        
        /// <summary>
        /// Loads and resolves a foreign key field for a collection of models, using the specified predicate and custom
        /// data loader.
        /// </summary>
        /// <remarks>This method is an extension method that simplifies the process of resolving foreign
        /// key relationships for a collection of models. The <paramref name="customDataLoader"/> allows for custom
        /// logic to be applied when retrieving the related data.</remarks>
        /// <typeparam name="T">The type of the primary model in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model being loaded. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="modelData">The collection of primary models for which the foreign key field will be loaded.</param>
        /// <param name="relmQuickContext">The context used to manage data loading operations.</param>
        /// <param name="predicate">An expression that specifies the foreign key field to be loaded.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related data.</param>
        /// <returns>A collection of the primary models with the specified foreign key field resolved.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R, S>(this ICollection<T> modelData, IRelmQuickContext relmQuickContext, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmQuickContext).LoadForeignKey(predicate, customDataLoader);
        
        /// <summary>
        /// Loads and resolves a foreign key field for a collection of models, applying additional constraints if
        /// specified.
        /// </summary>
        /// <typeparam name="T">The type of the primary model in the collection.</typeparam>
        /// <typeparam name="R">The type of the related model referenced by the foreign key.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader.</typeparam>
        /// <param name="modelData">The collection of primary models for which the foreign key field will be loaded.</param>
        /// <param name="relmQuickContext">The context used to manage data loading operations.</param>
        /// <param name="predicate">An expression specifying the foreign key relationship between the primary model and the related model.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related models.</param>
        /// <param name="additionalConstraints">An expression specifying additional constraints to apply when resolving the foreign key field.</param>
        /// <returns>A collection of the primary models with the foreign key field resolved and populated.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R, S>(this ICollection<T> modelData, IRelmQuickContext relmQuickContext, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmQuickContext).LoadForeignKey(predicate, customDataLoader, additionalConstraints);

        /************************************************************************************************************************/
        /****************************************** Context options load singular property ******************************************/
        /************************************************************************************************************************/

        /// <summary>
        /// Loads and resolves a foreign key field for the specified model data collection.
        /// </summary>
        /// <remarks>This method uses a <see cref="ForeignKeyLoader{T}"/> to load the foreign key field
        /// specified by the <paramref name="predicate"/>. The foreign key field is resolved based on the provided
        /// context options and the relationship defined in the model.</remarks>
        /// <typeparam name="T">The type of the model in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model to be loaded. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="modelData">The collection of models for which the foreign key field will be loaded.</param>
        /// <param name="relmContextOptionsBuilder">The context options builder used to configure the loading process.</param>
        /// <param name="predicate">An expression specifying the foreign key field to load.</param>
        /// <returns>A collection of models with the specified foreign key field resolved.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R>(this ICollection<T> modelData, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, R>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmContextOptionsBuilder).LoadForeignKey(predicate);
        
        /// <summary>
        /// Loads and populates a foreign key field for a collection of models based on the specified predicate and
        /// additional constraints.
        /// </summary>
        /// <remarks>This method uses the provided <paramref name="relmContextOptionsBuilder"/> to
        /// configure the database context and load the related data. The <paramref name="predicate"/> defines the
        /// relationship between the primary and related models, while the <paramref name="additionalConstraints"/> can
        /// be used to further filter the related data being loaded.</remarks>
        /// <typeparam name="T">The type of the primary model in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model representing the foreign key. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="modelData">The collection of primary models for which the foreign key field will be loaded.</param>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the database context for loading the foreign key data.</param>
        /// <param name="predicate">An expression specifying the foreign key relationship between the primary model and the related model.</param>
        /// <param name="additionalConstraints">An optional expression specifying additional constraints to apply when loading the foreign key data.</param>
        /// <returns>A collection of primary models with the specified foreign key field populated.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R>(this ICollection<T> modelData, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, R>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmContextOptionsBuilder).LoadForeignKey(predicate, additionalConstraints);
        
        /// <summary>
        /// Loads and resolves a foreign key field for a collection of models, using the specified predicate and custom
        /// data loader.
        /// </summary>
        /// <remarks>This method uses a foreign key loader to resolve the specified relationship for the
        /// provided collection of models. The <paramref name="customDataLoader"/> allows for custom logic to be applied
        /// when loading the related data.</remarks>
        /// <typeparam name="T">The type of the primary model in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model being resolved. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="modelData">The collection of primary models for which the foreign key field will be loaded.</param>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the data loading context.</param>
        /// <param name="predicate">An expression specifying the foreign key relationship to resolve.</param>
        /// <param name="customDataLoader">A custom data loader used to fetch the related data.</param>
        /// <returns>A collection of the primary models with the specified foreign key field resolved.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R, S>(this ICollection<T> modelData, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader);
        
        /// <summary>
        /// Loads and resolves a foreign key field for a collection of models, applying additional constraints if
        /// specified.
        /// </summary>
        /// <typeparam name="T">The type of the primary model in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model representing the foreign key. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="modelData">The collection of primary models for which the foreign key field will be loaded.</param>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the data loading context.</param>
        /// <param name="predicate">An expression specifying the foreign key relationship between the primary model and the related model.</param>
        /// <param name="customDataLoader">A custom data loader used to fetch the related data.</param>
        /// <param name="additionalConstraints">An optional expression specifying additional constraints to apply when resolving the foreign key field.</param>
        /// <returns>A collection of the primary models with the foreign key field resolved and loaded.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R, S>(this ICollection<T> modelData, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader, additionalConstraints);

        /************************************************************************************************************************/
        /****************************************** RelmContext load list property ******************************************/
        /************************************************************************************************************************/

        /// <summary>
        /// Loads and populates a foreign key collection field for a collection of models.
        /// </summary>
        /// <remarks>This method uses the provided <paramref name="relmContext"/> to resolve and populate
        /// the foreign key collection specified by the <paramref name="predicate"/> for each model in the <paramref
        /// name="modelData"/> collection.</remarks>
        /// <typeparam name="T">The type of the primary model in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model in the foreign key collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="modelData">The collection of primary models for which the foreign key field will be loaded.</param>
        /// <param name="relmContext">The context used to access the data source and resolve the foreign key relationships.</param>
        /// <param name="predicate">An expression specifying the foreign key collection property to load for each model in <paramref
        /// name="modelData"/>.</param>
        /// <returns>The original collection of primary models with the specified foreign key field populated for each model.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R>(this ICollection<T> modelData, IRelmContext relmContext, Expression<Func<T, ICollection<R>>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmContext).LoadForeignKey(predicate);

        /// <summary>
        /// Loads and populates a foreign key collection field for a collection of models.
        /// </summary>
        /// <remarks>This method is an extension method that simplifies the process of loading related
        /// data for a collection of models. It uses the provided <paramref name="relmContext"/> to resolve the foreign
        /// key relationships and applies any additional constraints specified by <paramref
        /// name="additionalConstraints"/>.</remarks>
        /// <typeparam name="T">The type of the primary model in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model representing the foreign key. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="modelData">The collection of primary models for which the foreign key field will be loaded.</param>
        /// <param name="relmContext">The context used to access the data source and resolve the foreign key relationships.</param>
        /// <param name="predicate">An expression specifying the foreign key field to populate in the primary model.</param>
        /// <param name="additionalConstraints">An optional expression specifying additional constraints to apply when loading the related models.</param>
        /// <returns>A collection of the primary models with the specified foreign key field populated.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R>(this ICollection<T> modelData, IRelmContext relmContext, Expression<Func<T, ICollection<R>>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmContext).LoadForeignKey(predicate, additionalConstraints);

        /// <summary>
        /// Loads and populates a foreign key collection field for a collection of models using the specified predicate and data
        /// loader.
        /// </summary>
        /// <remarks>This method uses the provided <paramref name="customDataLoader"/> to load the related
        /// data for the foreign key field specified by <paramref name="predicate"/>. The method is an extension method
        /// and operates on the collection of primary models (<paramref name="modelData"/>).</remarks>
        /// <typeparam name="T">The type of the primary model in the collection.</typeparam>
        /// <typeparam name="R">The type of the related model representing the foreign key field.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader.</typeparam>
        /// <param name="modelData">The collection of primary models for which the foreign key field will be loaded.</param>
        /// <param name="relmContext">The context used to interact with the data source.</param>
        /// <param name="predicate">An expression specifying the foreign key field to be loaded.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related data.</param>
        /// <returns>A collection of primary models with the specified foreign key field populated.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R, S>(this ICollection<T> modelData, IRelmContext relmContext, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmContext).LoadForeignKey(predicate, customDataLoader);

        /// <summary>
        /// Loads and populates a foreign key collection field for a collection of models using the specified data loader and
        /// constraints.
        /// </summary>
        /// <remarks>This method uses a custom data loader and optional constraints to populate a foreign
        /// key field in the primary models. It is designed to work with models that implement the <see
        /// cref="IRelmModel"/> interface.</remarks>
        /// <typeparam name="T">The type of the primary model in the collection.</typeparam>
        /// <typeparam name="R">The type of the related model representing the foreign key field.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader.</typeparam>
        /// <param name="modelData">The collection of primary models for which the foreign key field will be loaded.</param>
        /// <param name="relmContext">The context used to manage the data loading operation.</param>
        /// <param name="predicate">An expression specifying the foreign key field to populate in the primary model.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related data.</param>
        /// <param name="additionalConstraints">An expression specifying additional constraints to apply when loading the related data.</param>
        /// <returns>A collection of the primary models with the specified foreign key field populated.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R, S>(this ICollection<T> modelData, IRelmContext relmContext, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmContext).LoadForeignKey(predicate, customDataLoader, additionalConstraints);

        /************************************************************************************************************************/
        /****************************************** RelmQuickContext load list property ******************************************/
        /************************************************************************************************************************/

        /// <summary>
        /// Loads and populates a foreign key collection field for each entity in the specified model data
        /// collection.
        /// </summary>
        /// <remarks>This method uses the provided <paramref name="relmQuickContext"/> to query and load
        /// the related entities for the specified foreign key collection property. The foreign key field is populated
        /// for each entity in the <paramref name="modelData"/> collection based on the provided <paramref
        /// name="predicate"/>.</remarks>
        /// <typeparam name="T">The type of the primary model entities in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related entities in the foreign key collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="modelData">The collection of primary model entities for which the foreign key field will be loaded.</param>
        /// <param name="relmQuickContext">The database context used to query and load the related entities.</param>
        /// <param name="predicate">An expression specifying the foreign key collection property to populate for each entity in <paramref
        /// name="modelData"/>.</param>
        /// <returns>The original <paramref name="modelData"/> collection with the specified foreign key field populated for each
        /// entity.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R>(this ICollection<T> modelData, IRelmQuickContext relmQuickContext, Expression<Func<T, ICollection<R>>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmQuickContext).LoadForeignKey(predicate);

        /// <summary>
        /// Loads and populates a foreign key collection field for a collection of model entities.
        /// </summary>
        /// <remarks>This method is an extension method that facilitates the loading of related entities
        /// for a collection of primary model entities. It uses the specified database context and constraints to query
        /// and populate the foreign key field.</remarks>
        /// <typeparam name="T">The type of the primary model entities in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related entities referenced by the foreign key. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="modelData">The collection of primary model entities for which the foreign key field will be loaded.</param>
        /// <param name="relmQuickContext">The database context used to query and load the related entities.</param>
        /// <param name="predicate">An expression specifying the foreign key field to populate in the primary model entities.</param>
        /// <param name="additionalConstraints">An expression specifying additional constraints to apply when querying the related entities.</param>
        /// <returns>A collection of the primary model entities with the specified foreign key field populated.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R>(this ICollection<T> modelData, IRelmQuickContext relmQuickContext, Expression<Func<T, ICollection<R>>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmQuickContext).LoadForeignKey(predicate, additionalConstraints);        
        
        /// <summary>
        /// Loads and populates a foreign key collection field for a collection of models.
        /// </summary>
        /// <remarks>This method uses a <see cref="ForeignKeyLoader{T}"/> to load the foreign key
        /// collection field for each model in the provided collection. The <paramref name="customDataLoader"/> allows
        /// for custom logic to retrieve the related data, which is then used to populate the specified foreign key
        /// field.</remarks>
        /// <typeparam name="T">The type of the primary model in the collection.</typeparam>
        /// <typeparam name="R">The type of the related model in the foreign key collection.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader.</typeparam>
        /// <param name="modelData">The collection of primary models for which the foreign key field will be loaded.</param>
        /// <param name="relmQuickContext">The context used to manage data loading operations.</param>
        /// <param name="predicate">An expression specifying the foreign key collection property to populate.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related data.</param>
        /// <returns>The original collection of primary models with the specified foreign key field populated.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R, S>(this ICollection<T> modelData, IRelmQuickContext relmQuickContext, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmQuickContext).LoadForeignKey(predicate, customDataLoader);

        /// <summary>
        /// Loads and populates a foreign key collection field for a collection of models using the specified data loader and
        /// constraints.
        /// </summary>
        /// <remarks>This method uses a <see cref="ForeignKeyLoader{T}"/> to load the foreign key field
        /// for the provided collection of models. The <paramref name="customDataLoader"/> allows for custom logic to
        /// retrieve the related data, and the <paramref name="additionalConstraints"/> parameter can be used to further
        /// refine the data loading process.</remarks>
        /// <typeparam name="T">The type of the primary model in the collection.</typeparam>
        /// <typeparam name="R">The type of the related model representing the foreign key field.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader.</typeparam>
        /// <param name="modelData">The collection of primary models for which the foreign key field will be loaded.</param>
        /// <param name="relmQuickContext">The context used to manage data loading operations.</param>
        /// <param name="predicate">An expression specifying the foreign key field to be loaded for each primary model.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related data.</param>
        /// <param name="additionalConstraints">An expression specifying additional constraints to apply when loading the related data.</param>
        /// <returns>A collection of primary models with the specified foreign key field populated.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R, S>(this ICollection<T> modelData, IRelmQuickContext relmQuickContext, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmQuickContext).LoadForeignKey(predicate, customDataLoader, additionalConstraints);

        /************************************************************************************************************************/
        /****************************************** Context options load list property ******************************************/
        /************************************************************************************************************************/

        /// <summary>
        /// Loads and populates a foreign key collection field for the specified collection of model data.
        /// </summary>
        /// <remarks>This method uses the provided <paramref name="relmContextOptionsBuilder"/> to
        /// configure the loading process and populates the foreign key field specified by the <paramref
        /// name="predicate"/> for each item in the <paramref name="modelData"/> collection.</remarks>
        /// <typeparam name="T">The type of the primary model in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model representing the foreign key. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="modelData">The collection of primary model data for which the foreign key field will be loaded.</param>
        /// <param name="relmContextOptionsBuilder">The context options builder used to configure the data loading process.</param>
        /// <param name="predicate">An expression specifying the foreign key field to load, represented as a navigation property.</param>
        /// <returns>A collection of the primary model data with the specified foreign key field populated.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R>(this ICollection<T> modelData, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, ICollection<R>>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(modelData, relmContextOptionsBuilder).LoadForeignKey(predicate);
        
        /// <summary>
        /// Loads and populates a foreign key collection field for the specified model data.
        /// </summary>
        /// <remarks>This method uses the provided <paramref name="relmContextOptionsBuilder"/> to
        /// configure the database context and loads the related data for the specified foreign key field. The <paramref
        /// name="additionalConstraints"/> parameter can be used to filter the related data further, if
        /// needed.</remarks>
        /// <typeparam name="T">The type of the primary model, which must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the related model, which must implement <see cref="IRelmModel"/>.</typeparam>
        /// <param name="modelData">The collection of primary model instances for which the foreign key field will be loaded.</param>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the database context for the operation.</param>
        /// <param name="predicate">An expression specifying the foreign key collection property to populate.</param>
        /// <param name="additionalConstraints">An optional expression defining additional constraints to apply when loading the related data.</param>
        /// <returns>A collection of the primary model instances with the specified foreign key field populated.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R>(this ICollection<T> modelData, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, ICollection<R>>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmContextOptionsBuilder).LoadForeignKey(predicate, additionalConstraints);        
        
        /// <summary>
        /// Loads and populates a foreign key collection field for the specified model data.
        /// </summary>
        /// <remarks>This method uses a <see cref="ForeignKeyLoader{T}"/> to load the foreign key
        /// collection field for each model instance in the provided <paramref name="modelData"/> collection. The
        /// <paramref name="customDataLoader"/> is used to retrieve the related data, and the <paramref
        /// name="predicate"/> specifies the foreign key collection property to populate.</remarks>
        /// <typeparam name="T">The type of the primary model in the collection.</typeparam>
        /// <typeparam name="R">The type of the related model in the foreign key collection.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader.</typeparam>
        /// <param name="modelData">The collection of primary model instances for which the foreign key field will be loaded.</param>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the data loading context.</param>
        /// <param name="predicate">An expression specifying the foreign key collection property to populate.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related data.</param>
        /// <returns>The original collection of primary model instances with the specified foreign key field populated.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R, S>(this ICollection<T> modelData, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader);

        /// <summary>
        /// Loads and populates a foreign key collection field for a collection of model data.
        /// </summary>
        /// <remarks>This method uses a foreign key loader to populate the specified foreign key field for
        /// the provided collection of model data. The <paramref name="customDataLoader"/> allows for custom logic to
        /// retrieve the related data, and the <paramref name="additionalConstraints"/> parameter can be used to apply
        /// additional filtering or constraints.</remarks>
        /// <typeparam name="T">The type of the primary model in the collection.</typeparam>
        /// <typeparam name="R">The type of the related model representing the foreign key field.</typeparam>
        /// <typeparam name="S">The type of the model used by the custom data loader.</typeparam>
        /// <param name="modelData">The collection of primary model data for which the foreign key field will be loaded.</param>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the data loading context.</param>
        /// <param name="predicate">An expression specifying the foreign key field to be loaded.</param>
        /// <param name="customDataLoader">A custom data loader used to retrieve the related data.</param>
        /// <param name="additionalConstraints">An expression specifying additional constraints to apply when loading the foreign key field.</param>
        /// <returns>A collection of the primary model data with the specified foreign key field populated.</returns>
        public static ICollection<T> LoadForeignKeyField<T, R, S>(this ICollection<T> modelData, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
        	=> new ForeignKeyLoader<T>(modelData, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader, additionalConstraints);

        /************************************************************************************************************************/
        /****************************************** Data loader fields ******************************************/
        /************************************************************************************************************************/

        /// <summary>
        /// Loads and initializes a specific field for a collection of models using a data loader.
        /// </summary>
        /// <remarks>This method uses a data loader to populate the specified field for each model in the
        /// collection. The field to be loaded is determined by the provided <paramref name="predicate"/>
        /// expression.</remarks>
        /// <typeparam name="T">The type of the model in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the field to be loaded.</typeparam>
        /// <param name="modelData">The collection of models for which the field will be loaded. Cannot be <see langword="null"/>.</param>
        /// <param name="relmContext">The context used to access the data loader. Cannot be <see langword="null"/>.</param>
        /// <param name="predicate">An expression specifying the field to be loaded. Cannot be <see langword="null"/>.</param>
        /// <returns>The original collection of models with the specified field loaded.</returns>
        public static ICollection<T> LoadDataLoaderField<T, R>(this ICollection<T> modelData, IRelmContext relmContext, Expression<Func<T, R>> predicate) where T : IRelmModel, new()
            => new DataLoaderHelper<T>(relmContext, modelData).LoadField(predicate);
        
        /// <summary>
        /// Loads and populates a specified field for a collection of models using a data loader.
        /// </summary>
        /// <remarks>This method uses a data loader to populate the specified field for each model in the
        /// collection. The <paramref name="predicate"/> parameter determines which field is loaded.</remarks>
        /// <typeparam name="T">The type of the model in the collection. Must implement <see cref="IRelmModel"/>.</typeparam>
        /// <typeparam name="R">The type of the field to be loaded.</typeparam>
        /// <param name="modelData">The collection of models for which the field will be loaded.</param>
        /// <param name="relmQuickContext">The context used to facilitate data loading operations.</param>
        /// <param name="predicate">An expression specifying the field to be loaded for each model in the collection.</param>
        /// <returns>The original collection of models with the specified field loaded.</returns>
        public static ICollection<T> LoadDataLoaderField<T, R>(this ICollection<T> modelData, IRelmQuickContext relmQuickContext, Expression<Func<T, R>> predicate) where T : IRelmModel, new()
            => new DataLoaderHelper<T>(relmQuickContext, modelData).LoadField(predicate);

        /// <summary>
        /// Loads and populates a specified field for a collection of models using a data loader.
        /// </summary>
        /// <remarks>This method uses a data loader to populate the specified field in the provided
        /// collection of models. Ensure that the <paramref name="relmContextOptionsBuilder"/> is properly configured to
        /// access the required data source.</remarks>
        /// <typeparam name="T">The type of the model in the collection. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <typeparam name="R">The type of the field to be loaded, as specified by the predicate.</typeparam>
        /// <param name="modelData">The collection of models to be updated with the loaded field data.</param>
        /// <param name="relmContextOptionsBuilder">The options builder used to configure the database context for loading the field.</param>
        /// <param name="predicate">An expression specifying the field to be loaded for each model in the collection.</param>
        /// <returns>The updated collection of models with the specified field populated.</returns>
        public static ICollection<T> LoadDataLoaderField<T, R>(this ICollection<T> modelData, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, R>> predicate) where T : IRelmModel, new()
            => new DataLoaderHelper<T>(relmContextOptionsBuilder, modelData).LoadField(predicate);

        /************************************************************************************************************************/
        /****************************************** Other functions ******************************************/
        /************************************************************************************************************************/

        /// <summary>
        /// Flattens a hierarchical structure of objects into a single collection.
        /// </summary>
        /// <remarks>This method recursively traverses the hierarchy defined by the <paramref
        /// name="getChildrenFunction"/>  and combines all objects into a single collection. If an object has no
        /// children, it is included as-is.</remarks>
        /// <typeparam name="T">The type of the objects in the hierarchy.</typeparam>
        /// <param name="enumerableList">The top-level collection of objects to flatten.</param>
        /// <param name="getChildrenFunction">A function that retrieves the child objects of a given object.  The function should return an <see
        /// cref="ICollection{T}"/> of child objects, or <see langword="null"/> if there are no children.</param>
        /// <returns>A flattened <see cref="ICollection{T}"/> containing all objects in the hierarchy, including the top-level
        /// objects and their descendants.</returns>
        public static ICollection<T> FlattenTreeObject<T>(this IEnumerable<T> enumerableList, Func<T, ICollection<T>> getChildrenFunction)
        	=> enumerableList
                .SelectMany(enumerableItem =>
                    Enumerable
                    .Repeat(enumerableItem, 1)
                    .Concat(getChildrenFunction(enumerableItem)
                        ?.FlattenTreeObject(getChildrenFunction)
                        ??
                        Enumerable.Empty<T>()))
                .ToList();
        
        /// <summary>
        /// Generates a collection of dynamic objects (DTOs) from the specified collection of base objects,  including
        /// or excluding specific properties as needed.
        /// </summary>
        /// <remarks>This method is an extension method for collections of objects implementing the <see
        /// cref="IRelmModel"/>  interface. It allows for flexible generation of DTOs by specifying properties to
        /// include or exclude,  and by adding custom properties through the <paramref
        /// name="getAdditionalObjectProperties"/> function.</remarks>
        /// <typeparam name="T">The type of the base objects, which must implement the <see cref="IRelmModel"/> interface.</typeparam>
        /// <param name="baseObjects">The collection of base objects to generate DTOs from. This parameter cannot be <see langword="null"/>.</param>
        /// <param name="includeProperties">An optional collection of property names to include in the generated DTOs. If <see langword="null"/>,  all
        /// properties are included by default unless explicitly excluded.</param>
        /// <param name="excludeProperties">An optional collection of property names to exclude from the generated DTOs. If <see langword="null"/>,  no
        /// properties are excluded unless explicitly specified in <paramref name="includeProperties"/>.</param>
        /// <param name="sourceObjectName">An optional name of the source object to include in the DTOs for identification purposes. If  <see
        /// langword="null"/>, the source object name is not included.</param>
        /// <param name="getAdditionalObjectProperties">An optional function that takes an <see cref="IRelmModel"/> object and returns a dictionary of  additional
        /// properties to include in the generated DTOs. If <see langword="null"/>, no additional  properties are added.</param>
        /// <returns>A collection of dynamic objects (DTOs) generated from the specified base objects. Each DTO includes  the
        /// properties specified in <paramref name="includeProperties"/> and excludes those in  <paramref
        /// name="excludeProperties"/>, along with any additional properties provided by  <paramref
        /// name="getAdditionalObjectProperties"/>.</returns>
        public static ICollection<dynamic> GenerateDTO<T>(this IEnumerable<T> baseObjects, ICollection<string> includeProperties = null, ICollection<string> excludeProperties = null, string sourceObjectName = null, Func<IRelmModel, Dictionary<string, object>> getAdditionalObjectProperties = null) where T : IRelmModel
        	=> baseObjects.Select(x => x.GenerateDTO(includeProperties: includeProperties, excludeProperties: excludeProperties, sourceObjectName: sourceObjectName, getAdditionalObjectProperties: getAdditionalObjectProperties)).ToList();
        
        /// <summary>
        /// Retrieves the key-value pair associated with the specified key in the dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary from which to retrieve the key-value pair. Cannot be <see langword="null"/>.</param>
        /// <param name="key">The key whose associated key-value pair is to be retrieved. Must exist in the dictionary.</param>
        /// <returns>A <see cref="KeyValuePair{TKey, TValue}"/> containing the specified key and its associated value.</returns>
        public static KeyValuePair<TKey, TValue> GetEntry<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        	=> new KeyValuePair<TKey, TValue>(key, dictionary[key]);    }
}
