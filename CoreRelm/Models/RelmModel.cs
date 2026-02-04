
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CoreRelm.Attributes;
using CoreRelm.Extensions;
using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.Models.EventArguments;
using CoreRelm.Options;
using CoreRelm.RelmInternal.Extensions;
using CoreRelm.RelmInternal.Helpers.DataTransfer;
using CoreRelm.RelmInternal.Helpers.DataTransfer.Persistence;
using CoreRelm.RelmInternal.Helpers.EqualityComparers;
using CoreRelm.RelmInternal.Helpers.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CoreRelm.Models
{
    /// <summary>
    /// Represents a base model for entities in the Relm framework, providing core attributes and functionality for data
    /// manipulation, database operations, and DTO generation.
    /// </summary>
    /// <remarks>The <see cref="RelmModel"/> class serves as a foundational class for entities, offering
    /// features such as: <list type="bullet"> <item>Core attributes like <see cref="Id"/>, <see cref="Active"/>, <see
    /// cref="InternalId"/>, <see cref="CreateDate"/>, and <see cref="LastUpdated"/>.</item> <item>Support for resetting
    /// attributes to default values using <see cref="ResetCoreAttributes"/>.</item> <item>Data population from database
    /// rows via <see cref="ResetWithData"/>.</item> <item>Bulk database write operations through various
    /// <c>WriteToDatabase</c> overloads.</item> <item>Dynamic Data Transfer Object (DTO) generation with <see
    /// cref="GenerateDTO"/>.</item> </list> This class is designed to be extended by specific entity types and provides
    /// a flexible foundation for working with relational data in the Relm framework.</remarks>
    public class RelmModel : IRelmModel
    {
        /// <summary>
        /// Occurs when a DTO (Data Transfer Object) is processed.
        /// </summary>
        /// <remarks>This event is triggered after a DTO has been processed, providing the processed DTO
        /// as part of the event data. Subscribers can use this event to perform additional actions or handle the
        /// processed DTO as needed.</remarks>
        public event EventHandler<DtoEventArgs> DtoTypeProcessor;

        /// <summary>
        /// Gets or sets the unique auto-number row identifier for the entity.
        /// </summary>
        [RelmColumn(primaryKey: true, autonumber: true, isNullable: false)]
        public long? Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is active.
        /// </summary>
        [RelmColumn(columnSize: 1, isNullable: false, defaultValue: "1")]
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets the unique internal identifier for the entity.
        /// </summary>
        [RelmKey]
        [RelmDto]
        [RelmColumn(columnSize: 45, isNullable: false, unique: true)]
        public string InternalId { get; set; }

        /// <summary>
        /// Gets or sets the creation date and time of the entity.
        /// </summary>
        [RelmColumn(isNullable: false, defaultValue: "CURRENT_TIMESTAMP")]
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last update.
        /// </summary>
        [RelmColumn(isNullable: false, defaultValue: "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")]
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Resets the core attributes of the model to their default values.
        /// </summary>
        /// <param name="nullInternalId">If <see langword="true"/>, sets the internal ID to <see langword="null"/>; otherwise, generates a new unique
        /// identifier.</param>
        /// <param name="resetCreateDate">If <see langword="true"/>, resets the creation date to the current date and time.</param>
        /// <returns>The current instance of the model with updated core attributes.</returns>
        public IRelmModel ResetCoreAttributes(bool nullInternalId = false, bool resetCreateDate = true)
        {
            Active = true;

            if (nullInternalId)
                InternalId = null;
            else
                InternalId = Guid.NewGuid().ToString();

            if (resetCreateDate)
                CreateDate = DateTime.Now;
            LastUpdated = CreateDate;

            return this;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelmModel"/> class.
        /// </summary>
        /// <remarks>This constructor initializes the core attributes of the model by invoking the <see
        /// cref="ResetCoreAttributes"/> method.</remarks>
        public RelmModel()
        {
            ResetCoreAttributes();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelmModel"/> class by copying all writable properties  from the
        /// specified source model.
        /// </summary>
        /// <remarks>This constructor performs a shallow copy of all writable properties from the
        /// specified source model  to the new instance. Only properties with matching names and compatible types
        /// between the source  and target models are copied. Properties that are read-only or do not exist in the
        /// target model  are ignored.</remarks>
        /// <param name="fromModel">The source model from which to copy property values. Must not be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="fromModel"/> is <see langword="null"/>.</exception>
        public RelmModel(IRelmModel fromModel)
        {
            // surface copy all properties from the FromModel to this one
            if (fromModel == null)
                throw new ArgumentNullException(nameof(fromModel));

            var sourceType = fromModel.GetType();
            var targetType = this.GetType();

            var sourceProperties = sourceType.GetProperties();
            var targetProperties = targetType.GetProperties();

            var targetList = targetProperties
                .ToDictionary(x => x, x => sourceProperties.FirstOrDefault(y => y.Name == x.Name && x.CanWrite))
                .Where(x => x.Value != null)
                .ToList();

            foreach (var target in targetList)
            {
                target.Key.SetValue(this, target.Value.GetValue(fromModel));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelmModel"/> class and populates its properties  using data
        /// from the specified database row.
        /// </summary>
        /// <remarks>This constructor automatically maps the data from the provided <paramref
        /// name="modelData"/> to the  properties of the model based on naming conventions. Ensure that the column names
        /// in the  <paramref name="modelData"/> match the property names of the model for accurate mapping.</remarks>
        /// <param name="modelData">A <see cref="DataRow"/> containing the data to populate the model. The column names in the  <paramref
        /// name="modelData"/> must match the property names of the model for successful mapping.</param>
        /// <param name="alternateTableName">An optional alternate table name to use when searching for data in the <paramref name="modelData"/>.  If not
        /// specified, the default table name is used.</param>
        public RelmModel(DataRow modelData, string alternateTableName = null)
        {
            ResetWithData(modelData, alternateTableName: alternateTableName);
        }

        /// <summary>
        /// Resets the current model instance with data from the specified <see cref="DataRow"/>.
        /// </summary>
        /// <remarks>This method maps the columns in the provided <see cref="DataRow"/> to the model's
        /// properties based on underscore naming conventions. If a property has a <see cref="JsonConverterAttribute"/>,
        /// the data is deserialized using the specified JSON converter. The method supports both default column names
        /// and column names suffixed with the table name.</remarks>
        /// <param name="modelData">The <see cref="DataRow"/> containing the data to populate the model. The column names in the <see
        /// cref="DataRow"/> should match the model's property names, using underscore naming conventions.</param>
        /// <param name="alternateTableName">An optional alternate table name used to match column names in the <see cref="DataRow"/>. If not provided,
        /// the model's type name is used as the table name.</param>
        /// <returns>The current model instance, updated with the data from the specified <see cref="DataRow"/>.</returns>
        public IRelmModel ResetWithData(DataRow modelData, string alternateTableName = null)
        {
            var tableName = alternateTableName ?? GetType().Name;

            ResetCoreAttributes();

            var underscoreProperties = GetUnderscoreProperties();
            var jsonConverters = underscoreProperties
                .Select(x => x.Value.Item2.GetCustomAttribute<JsonConverterAttribute>())
                .Where(x => x != null)
                .Distinct(new JsonConverterAttributeEqualityComparer())
                .Select(x => (JsonConverter)Activator.CreateInstance(x.ConverterType, x.ConverterParameters))
                .ToArray();

            // match up all properties to columns using underscore names and populate matches with data from the row
            foreach (var underscoreName in underscoreProperties)
            {
                // first do the default column names
                if (modelData.Table.Columns.IndexOf(underscoreName.Key) >= 0 && !(modelData[underscoreName.Key] is DBNull) && underscoreName.Value.Item2.SetMethod != null)
                {
                    var jsonConverter = underscoreName.Value.Item2.GetCustomAttribute<JsonConverterAttribute>();
                    if (jsonConverter == null)
                        underscoreName.Value.Item2.SetValue(this, GetValueData(underscoreName.Key, underscoreName.Value.Item2.PropertyType, modelData));
                    else
                        underscoreName.Value.Item2.SetValue(this, JsonConvert.DeserializeObject($"'{modelData[underscoreName.Key]}'", underscoreName.Value.Item2.PropertyType, new JsonSerializerSettings { Converters = jsonConverters }));
                }

                // then do the alternate table names
                if (modelData.Table.Columns.IndexOf($"{underscoreName.Key}_{tableName}") >= 0 && !(modelData[$"{underscoreName.Key}_{tableName}"] is DBNull) && underscoreName.Value.Item2.SetMethod != null)
                {
                    var jsonConverter = underscoreName.Value.Item2.GetCustomAttribute<JsonConverterAttribute>();
                    if (jsonConverter == null)
                        underscoreName.Value.Item2.SetValue(this, GetValueData($"{underscoreName.Key}_{tableName}", underscoreName.Value.Item2.PropertyType, modelData));
                    else
                        underscoreName.Value.Item2.SetValue(this, JsonConvert.DeserializeObject($"'{modelData[$"{underscoreName.Key}_{tableName}"]}'", underscoreName.Value.Item2.PropertyType, new JsonSerializerSettings { Converters = jsonConverters }));
                }
            }

            return this;
        }

        /// <summary>
        /// Converts and retrieves the value associated with the specified key from the provided <see cref="DataRow"/>,
        /// transforming it into the specified target type.
        /// </summary>
        /// <remarks>This method supports direct passthrough for primitive types and <see
        /// cref="DateTime"/> values, as well as conversion for enums, <see cref="DateTime"/> strings, <see
        /// cref="TimeSpan"/> strings, and single-character strings. For other types, the method attempts to deserialize
        /// the value from JSON.</remarks>
        /// <param name="underscoreKey">The key used to retrieve the value from the <paramref name="modelData"/> row.</param>
        /// <param name="propertyValueType">The target <see cref="Type"/> to which the retrieved value should be converted.</param>
        /// <param name="modelData">The <see cref="DataRow"/> containing the data to retrieve and convert.</param>
        /// <returns>An object representing the value associated with <paramref name="underscoreKey"/>, converted to the
        /// specified <paramref name="propertyValueType"/>. Returns <see langword="null"/> if the value is <see
        /// cref="DBNull"/>.</returns>
        private object GetValueData(string underscoreKey, Type propertyValueType, DataRow modelData)
        {
            object valueData;

            // most primitive types are just 1:1 passthrough and don't require post-processing
            if (propertyValueType == modelData[underscoreKey].GetType() || modelData[underscoreKey].GetType() == typeof(DateTime))
                valueData = modelData[underscoreKey];

            // if it's an Enum, do a parse
            else if (propertyValueType.BaseType == typeof(Enum))
                valueData = Enum.Parse(propertyValueType, modelData[underscoreKey].ToString());

            // if we're putting it in a DateTime, but we have a string, parse it
            else if (propertyValueType == typeof(DateTime) && modelData[underscoreKey].GetType() == typeof(string))
                valueData = DateTime.TryParse(modelData[underscoreKey].ToString(), out DateTime _dateData) ? _dateData : default;

            // if we're putting it in a TimeSpan, but we have a string, parse it
            else if (propertyValueType == typeof(TimeSpan) && modelData[underscoreKey].GetType() == typeof(string))
                valueData = TimeSpan.TryParse(modelData[underscoreKey].ToString(), out TimeSpan _timeSpanData) ? _timeSpanData : default;

            else if (propertyValueType == typeof(char) && modelData[underscoreKey].GetType() == typeof(string))
                valueData = modelData[underscoreKey].ToString()?[0];

            else if (propertyValueType == typeof(string) && modelData[underscoreKey].GetType() == typeof(byte[]))
                valueData = System.Text.Encoding.UTF8.GetString((byte[])modelData[underscoreKey]);

            // if none of those are true, then we have some serialized JSON data, so deserialize it
            else
            {
                if (modelData[underscoreKey] is DBNull)
                    valueData = null;
                else
                    valueData = JsonConvert.DeserializeObject(modelData[underscoreKey].ToString(), propertyValueType);
            }

            return valueData;
        }

        /// <summary>
        /// Retrieves a list of properties for the current object, with their names converted to underscore-separated
        /// format.
        /// </summary>
        /// <param name="getOnlyRelmColumns">A value indicating whether to include only properties marked with the <c>RelmColumn</c> attribute.  If
        /// <see langword="true"/>, only such properties are included; otherwise, all properties are included.</param>
        /// <returns>A list of key-value pairs where the key is the underscore-separated name of the property,  and the value is
        /// a tuple containing the original property name and its <see cref="PropertyInfo"/> metadata.</returns>
        public List<KeyValuePair<string, Tuple<string, PropertyInfo>>> GetUnderscoreProperties(bool getOnlyRelmColumns = true)
        {
            return UnderscoreNamesHelper.ConvertPropertiesToUnderscoreNames(this.GetType(), getOnlyRelmColumns: getOnlyRelmColumns);
        }

        /// <summary>
        /// Writes the current object to the database using the table specified in the <c>RelmTable</c> attribute.
        /// </summary>
        /// <remarks>This method performs a bulk write operation to the database. The table to which the
        /// data is written  is determined by the <c>RelmTable</c> attribute applied to the current object's type. 
        /// Ensure that the database schema matches the structure of the object being written.</remarks>
        /// <param name="connectionStringType">The type of connection to use for the database operation.</param>
        /// <param name="batchSize">The number of items to write to the database per batch. Must be a positive integer. Defaults to 100.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether columns with auto-increment constraints are allowed to be written.  Set to <see
        /// langword="true"/> to allow writing to such columns; otherwise, <see langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are allowed to be written.  Set to <see langword="true"/> to
        /// allow writing to primary key columns; otherwise, <see langword="false"/>.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique constraint columns are allowed to be written.  Set to <see
        /// langword="true"/> to allow writing to such columns; otherwise, <see langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether columns with automatic date generation (e.g., timestamps) are allowed to be
        /// written.  Set to <see langword="true"/> to allow writing to such columns; otherwise, <see
        /// langword="false"/>.</param>
        /// <returns>The number of rows successfully written to the database.</returns>
        public int WriteToDatabase(Enum connectionStringType, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        {
            return DataOutputOperations.BulkTableWrite(connectionStringType, this, forceType: this.GetType(), batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
        }

        /// <summary>
        /// Writes the current object to the database using the table specified in the <c>RelmTable</c> attribute.
        /// </summary>
        /// <remarks>This method performs a bulk write operation to the database. The table to which the
        /// data is written is determined by the <c>RelmTable</c> attribute applied to the object's type. Ensure that the
        /// database schema matches the structure of the object being written.</remarks>
        /// <param name="existingConnection">An existing and open <see cref="MySqlConnection"/> to use for writing the data. The connection must remain
        /// open for the duration of the operation.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> under which the data will be written. If not provided, the
        /// operation will execute outside of a transaction.</param>
        /// <param name="batchSize">The number of rows to write to the database in each batch. Must be greater than zero. Defaults to 100.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns should be included in the write operation. If <see
        /// langword="true"/>, auto-increment columns will be written; otherwise, they will be excluded.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns should be included in the write operation. If <see
        /// langword="true"/>, primary key columns will be written; otherwise, they will be excluded.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns should be included in the write operation. If <see
        /// langword="true"/>, unique columns will be written; otherwise, they will be excluded.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether auto-generated date columns (e.g., timestamps) should be included in the write
        /// operation. If <see langword="true"/>, auto-date columns will be written; otherwise, they will be excluded.</param>
        /// <returns>The number of rows successfully written to the database.</returns>
        public int WriteToDatabase(MySqlConnection existingConnection, MySqlTransaction sqlTransaction = null, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        {
            return DataOutputOperations.BulkTableWrite(existingConnection, this, sqlTransaction: sqlTransaction, forceType: this.GetType(), batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
        }

        /// <summary>
        /// Writes the current object to the database using the table specified in the <c>RelmTable</c> attribute.
        /// </summary>
        /// <remarks>This method performs a bulk write operation to the database. The behavior of the
        /// write operation can be customized using the optional parameters to control whether specific types of columns
        /// (e.g., auto-increment, primary key, unique, or auto-date columns) are included in the operation. The table
        /// to which the object is written is determined by the <c>RelmTable</c> attribute applied to the object's
        /// type.</remarks>
        /// <param name="relmContext">An <see cref="IRelmContext"/> instance that provides an open connection and transaction for database
        /// operations.</param>
        /// <param name="batchSize">The number of items to write to the database in each batch. Must be greater than zero. The default value is
        /// 100.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether columns marked as auto-increment are allowed to be written. If <see
        /// langword="true"/>, auto-increment columns will be included in the write operation; otherwise, they will be
        /// excluded.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are allowed to be written. If <see langword="true"/>, primary
        /// key columns will be included in the write operation; otherwise, they will be excluded.</param>
        /// <param name="allowUniqueColumns">A value indicating whether columns with unique constraints are allowed to be written. If <see
        /// langword="true"/>, unique columns will be included in the write operation; otherwise, they will be excluded.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether columns with automatic date generation (e.g., timestamps) are allowed to be
        /// written. If <see langword="true"/>, such columns will be included in the write operation; otherwise, they
        /// will be excluded.</param>
        /// <returns>The number of rows successfully written to the database.</returns>
        public int WriteToDatabase(IRelmContext relmContext, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        {
            return DataOutputOperations.BulkTableWrite(relmContext, this, forceType: this.GetType(), batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
        }

        /// <summary>
        /// Writes the current object to the database using the table specified in the <c>RelmTable</c> attribute.
        /// </summary>
        /// <remarks>This method performs a bulk write operation, which can improve performance when
        /// writing large amounts of data. Ensure that the provided <paramref name="relmContext"/> is properly
        /// configured and that the database schema matches the structure of the data being written. The behavior of the
        /// write operation can be customized using the provided options.</remarks>
        /// <param name="relmContext">The database context used to perform the write operation. This context must be properly configured        
        /// to connect to the target database.</param>
        /// <param name="batchSize">The number of records to include in each batch during the bulk write operation. The default value is 100.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether columns with auto-increment constraints are allowed to be written. If <see
        /// langword="true"/>,         auto-increment columns will be included in the write operation; otherwise, they
        /// will be excluded. The default value is <see langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are allowed to be written. If <see langword="true"/>,        
        /// primary key columns will be included in the write operation; otherwise, they will be excluded. The default
        /// value is <see langword="false"/>.</param>
        /// <param name="allowUniqueColumns">A value indicating whether columns with unique constraints are allowed to be written. If <see
        /// langword="true"/>,         unique columns will be included in the write operation; otherwise, they will be
        /// excluded. The default value is <see langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether columns with automatic date constraints (e.g., timestamps) are allowed to be
        /// written.         If <see langword="true"/>, such columns will be included in the write operation; otherwise,
        /// they will be excluded.         The default value is <see langword="false"/>.</param>
        /// <returns>The number of records successfully written to the database.</returns>
        public int WriteToDatabase(IRelmQuickContext relmContext, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        {
            return DataOutputOperations.BulkTableWrite(relmContext, this, forceType: this.GetType(), batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
        }

        /// <summary>
        /// Writes the current object to the database using the table specified in the <c>RelmTable</c> attribute.
        /// </summary>
        /// <remarks>This method performs a bulk write operation, which can improve performance when
        /// writing large datasets. Ensure that the specified options and constraints align with the database schema to
        /// avoid runtime errors.</remarks>
        /// <param name="relmContextOptions">The options used to configure the database context.</param>
        /// <param name="batchSize">The number of records to process in each batch. Defaults to 100.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether columns with auto-increment constraints are allowed to be written. Defaults to
        /// <see langword="false" />.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are allowed to be written. Defaults to <see langword="false"
        /// />.</param>
        /// <param name="allowUniqueColumns">A value indicating whether columns with unique constraints are allowed to be written. Defaults to <see
        /// langword="false" />.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether columns with automatic date generation (e.g., timestamps) are allowed to be
        /// written. Defaults to <see langword="false" />.</param>
        /// <returns>The number of records successfully written to the database.</returns>
        public int WriteToDatabase(RelmContextOptionsBuilder relmContextOptions, int batchSize = 100, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false)
        {
            return DataOutputOperations.BulkTableWrite(new RelmContext(relmContextOptions), this, forceType: this.GetType(), batchSize: batchSize, allowAutoIncrementColumns: allowAutoIncrementColumns, allowPrimaryKeyColumns: allowPrimaryKeyColumns, allowUniqueColumns: allowUniqueColumns, allowAutoDateColumns: allowAutoDateColumns);
        }

        /// <summary>
        /// Creates a new instance of the specified type <typeparamref name="T"/> and copies all public properties and
        /// fields  from the specified <paramref name="source"/> object to the new instance.
        /// </summary>
        /// <remarks>This method performs a shallow copy of all public properties and fields. Only
        /// properties that are writable  and not indexed are copied. Fields are copied regardless of their
        /// accessibility.</remarks>
        /// <typeparam name="T">The type of the object to create and copy values to. Must be a class that derives from <see
        /// cref="RelmModel"/>  and has a parameterless constructor.</typeparam>
        /// <param name="source">The source object from which to copy property and field values. Cannot be <see langword="null"/>.</param>
        /// <returns>A new instance of type <typeparamref name="T"/> with all public properties and fields copied from the
        /// <paramref name="source"/> object.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
        public T CopyFromSource<T>(T source) where T : RelmModel, new()
        {
            // create a new object of type T, then run through all the properties and members available on source and copy the value of each property and member that exists on the new object
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            T target = new T();

            foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // Check if the property can be written to and is not an index property
                if (property.CanWrite && property.GetIndexParameters().Length == 0)
                    property.SetValue(target, property.GetValue(source));
            }

            foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                field.SetValue(target, field.GetValue(source));
            }

            return target;
        }

        /// <summary>
        /// Generates a dynamic Data Transfer Object (DTO) containing properties from the current object based on
        /// specific inclusion and exclusion criteria.
        /// </summary>
        /// <remarks>This method supports recursive generation of DTOs for nested objects. If a property
        /// is a collection of objects, each item in the collection is processed recursively. Properties not marked with
        /// the <see cref="RelmDto"/> attribute are included only if explicitly specified in <paramref
        /// name="includeProperties"/>.</remarks>
        /// <param name="includeProperties">A collection of property names to explicitly include in the DTO, even if they are not marked with the <see
        /// cref="RelmDto"/> attribute.</param>
        /// <param name="excludeProperties">A collection of property names to explicitly exclude from the DTO, even if they are marked with the <see
        /// cref="RelmDto"/> attribute.</param>
        /// <param name="sourceObjectName">An optional string representing the source object name, used to resolve nested property paths.</param>
        /// <param name="getAdditionalObjectProperties">A function that provides additional properties to include in the DTO. The function takes the current object
        /// as input and returns a dictionary of property names and values.</param>
        /// <param name="iteration">The current recursion depth, used internally to track nested object processing. Defaults to 0.</param>
        /// <returns>A dynamic object containing the selected properties from the current object. The resulting object includes
        /// properties marked with the <see cref="RelmDto"/> attribute,  explicitly included properties, and additional
        /// properties provided by the <paramref name="getAdditionalObjectProperties"/> function, while excluding
        /// explicitly excluded properties.</returns>
        public dynamic GenerateDTO(IEnumerable<string> includeProperties = null, IEnumerable<string> excludeProperties = null, string sourceObjectName = null, Func<IRelmModel, Dictionary<string, object>> getAdditionalObjectProperties = null, int iteration = 0)
        {
            var baseRef = this;
            var baseRefType = this.GetType();

            var namespaceIterations = baseRef
                .GetType()
                .FullName
                .Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            var sourceObjectIterations = sourceObjectName
                ?.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                ??
                Enumerable.Empty<string>();

            var invocationTypeList = DtoTypeProcessor
                ?.GetInvocationList()
                .ToDictionary(x => x.GetMethodInfo().GetGenericArguments().FirstOrDefault(), x => x);

            // get object properties, if any are DALBaseModels marked with RelmDto then GenerateDTO() on those recursively, otherwise just return the value. if there are any IEnumerables, DTO each item in the enumerable.
            return (ExpandoObject)baseRefType
                .GetRuntimeProperties()
                .Select(x => new KeyValuePair<PropertyInfo, IEnumerable<string>>(x, namespaceIterations
                    .Select((y, index) => string.Join(".", namespaceIterations.Skip(index).Append(x.Name)))
                    .Append(x.Name)
                    .Concat(sourceObjectIterations
                        .Select((y, index) => string.Join(".", sourceObjectIterations.Skip(index).Append(x.Name)))
                        .Append(x.Name))
                    .Where(y => !string.IsNullOrWhiteSpace(y))))
                .Where(x => (x.Key.GetCustomAttribute<RelmDto>() != null
                        || ((includeProperties?.Intersect(x.Value, StringComparer.InvariantCultureIgnoreCase)?.Count() ?? 0) > 0))
                    && !((excludeProperties?.Intersect(x.Value, StringComparer.InvariantCultureIgnoreCase).Count() ?? 0) > 0))
                .Select(x => x.Key)
                .Aggregate(new ExpandoObject() as IDictionary<string, object>,
                    (seed, property) =>
                    {
                        // look for enumerables, DTO each item within
                        if (property
                                .PropertyType
                                .GetInterfaces()
                                .Intersect(typeof(IEnumerable<>)
                                    .GetInterfaces())
                                .Count()
                                > 0
                            &&
                            property
                                .PropertyType
                                .GenericTypeArguments
                                .FlattenTreeObject(x => string.IsNullOrWhiteSpace(x?.BaseType?.Name)
                                    ? null
                                    : new Type[] { x.BaseType })
                                .Contains(typeof(RelmModel)))
                        /*
                        var isEnumerable = property.PropertyType.GetInterfaces().Any(y => y.IsGenericType && y.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                        var isRelmModel = property
                            .PropertyType
                            .GenericTypeArguments
                            .FlattenTreeObject(x => string.IsNullOrWhiteSpace(x?.BaseType?.Name) ? null : new Type[] { x.BaseType })
                            .Contains(typeof(RelmModel));
                        if (isEnumerable && isRelmModel)
                        */
                        {
                            seed.Add(property.Name,
                                ((IEnumerable<RelmModel>)property
                                    .GetValue(baseRef))
                                    ?.Select(x => x.GenerateDTO(includeProperties: includeProperties, excludeProperties: excludeProperties, sourceObjectName: string.Join(".", new List<string> { sourceObjectName, property.Name }.Where(y => !string.IsNullOrWhiteSpace(y))), getAdditionalObjectProperties: getAdditionalObjectProperties, iteration: iteration + 1)));
                        }
                        else
                        {
                            /* FOR DEBUGGING
                            var hasTransfer = property
                                    .PropertyType
                                    ?.GetRuntimeProperties()
                                    ?.Any(x => x.GetCustomAttribute<RelmDto>() != null);

                            var isBaseModel = new Type[] { property.PropertyType }
                                    .FlattenTreeObject(x => string.IsNullOrWhiteSpace(x?.BaseType?.Name) ? null : new Type[] { x.BaseType })
                                    .Contains(typeof(DALBaseModel));

                            var fieldValue = property.GetValue(baseRef);
                            if ((hasTransfer ?? false) && isBaseModel)
                                fieldValue = ((DALBaseModel)property.GetValue(baseRef))?.GenerateDTO(BaseRef: baseRef, includeProperties: includeProperties, excludeProperties: excludeProperties, SourceObjectName: string.Join(".", new List<string> { SourceObjectName, property.Name }.Where(y => !string.IsNullOrWhiteSpace(y))));

                            seed.Add(property.Name, fieldValue);
                            */
                            // convert a property
                            seed.Add(property.Name,
                                (property
                                    .PropertyType
                                    ?.GetRuntimeProperties()
                                    ?.Any(x => x.GetCustomAttribute<RelmDto>() != null)
                                    ??
                                    false)
                                &&
                                new Type[] { property.PropertyType }
                                    .FlattenTreeObject(x => string.IsNullOrWhiteSpace(x?.BaseType?.Name) ? null : new Type[] { x.BaseType })
                                    .Contains(typeof(RelmModel)
                                )
                                ? ((RelmModel)property.GetValue(baseRef))?.GenerateDTO(includeProperties: includeProperties, excludeProperties: excludeProperties, sourceObjectName: string.Join(".", new List<string> { sourceObjectName, property.Name }.Where(y => !string.IsNullOrWhiteSpace(y))), getAdditionalObjectProperties: getAdditionalObjectProperties, iteration: iteration + 1)
                                : (property.PropertyType.BaseType == typeof(Enum)
                                    ? property.GetValue(baseRef).ToString()
                                    : property.GetValue(baseRef)));
                            /* FOR DEBUGGING
                            var isRelmDto = property.PropertyType?.GetRuntimeProperties()?.Any(x => x.GetCustomAttribute<RelmDto>() != null) ?? false;
                            var isBaseModel = new Type[] { property.PropertyType }
                                .FlattenTreeObject(x => string.IsNullOrWhiteSpace(x?.BaseType?.Name) ? null : new Type[] { x.BaseType })
                                .Contains(typeof(RelmModel));
                            if (isRelmDto && isBaseModel)
                            {
                                // if the property is a RelmDto and a RelmModel, then call GenerateDTO on it recursively
                                seed.Add(property.Name, ((RelmModel)property.GetValue(baseRef))
                                    .GenerateDTO(includeProperties: includeProperties, excludeProperties: excludeProperties, SourceObjectName: string.Join(".", new List<string> { SourceObjectName, property.Name }.Where(y => !string.IsNullOrWhiteSpace(y))), getAdditionalObjectProperties: getAdditionalObjectProperties, Iteration: Iteration + 1));
                            }
                            else
                            {
                                // if the property is a RelmDto, but not a RelmModel, then just return the value as is
                                seed.Add(property.Name, property.GetValue(baseRef));
                            }
                            */
                        }

                        if (invocationTypeList?.ContainsKey(baseRefType) ?? false)
                        {
                            var additionalProperties = new DtoEventArgs();

                            invocationTypeList[baseRefType].DynamicInvoke(this, additionalProperties);

                            var filteredProperties = additionalProperties
                                .AdditionalObjectProperties
                                ?.Where(x => !seed.ContainsKey(x.Key) && !(excludeProperties?.Contains(x.Key) ?? false))
                                .ToList();

                            if (filteredProperties != null)
                            {
                                foreach (var filteredProperty in filteredProperties)
                                {
                                    seed.Add(filteredProperty);
                                }
                            }
                        }

                        if (iteration == 0 && getAdditionalObjectProperties != null)
                        {
                            var additionalProperties = getAdditionalObjectProperties(this)
                                ?.Where(x => !seed.ContainsKey(x.Key) && !(excludeProperties?.Contains(x.Key) ?? false))
                                .ToList();

                            if (additionalProperties != null)
                            {
                                foreach (var additionalProperty in additionalProperties)
                                {
                                    seed.Add(additionalProperty);
                                }
                            }
                        }

                        return seed;
                    });
        }

        /// <summary>
        /// Generates the Cartesian product of a sequence of sequences.
        /// </summary>
        /// <remarks>The Cartesian product is the set of all possible combinations where one element is
        /// taken from each  sequence in <paramref name="sequences"/>. For example, given two sequences {1, 2} and {A,
        /// B}, the  Cartesian product is {{1, A}, {1, B}, {2, A}, {2, B}}.</remarks>
        /// <typeparam name="T">The type of elements in the sequences.</typeparam>
        /// <param name="sequences">A collection of sequences for which the Cartesian product is to be computed.  Each sequence represents a
        /// dimension in the product.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of sequences, where each sequence represents one combination  in the
        /// Cartesian product. If <paramref name="sequences"/> is empty, the result will contain a single  empty
        /// sequence.</returns>
        private static IEnumerable<IEnumerable<T>> CartesianProduct<T>(IEnumerable<IEnumerable<T>> sequences)
        {
            // base case:
            IEnumerable<IEnumerable<T>> result = new[] { Enumerable.Empty<T>() };
            foreach (var sequence in sequences)
            {
                var s = sequence; // don't close over the loop variable
                                  // recursive case: use SelectMany to build the new product out of the old one
                result =
                    from seq in result
                    from item in s
                    select seq.Concat(new[] { item });
            }
            return result;
        }

        /// <summary>
        /// Creates a shallow copy of the current <see cref="RelmModel"/> instance.
        /// </summary>
        /// <remarks>The returned object is a shallow copy, meaning that only the top-level fields of the
        /// object are duplicated.  References to other objects within the instance are not deeply copied.</remarks>
        /// <returns>A new <see cref="RelmModel"/> instance that is a shallow copy of the current instance.</returns>
        public RelmModel Duplicate()
        {
            return (RelmModel)this.MemberwiseClone();
        }
    }
}
