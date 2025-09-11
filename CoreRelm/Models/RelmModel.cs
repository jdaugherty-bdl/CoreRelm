using CoreRelm.Attributes;
using CoreRelm.Extensions;
using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.Models.EventArguments;
using CoreRelm.Options;
using CoreRelm.RelmInternal.Extensions;
using CoreRelm.RelmInternal.Helpers.DataTransfer.Persistence;
using CoreRelm.RelmInternal.Helpers.EqualityComparers;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models
{
    public class RelmModel : IRelmModel
    {
        public event EventHandler<DtoEventArgs> DtoTypeProcessor;

        [RelmColumn(PrimaryKey: true, Autonumber: true, IsNullable: false)]
        public long? Id { get; set; }

        [RelmColumn(IsNullable: false, DefaultValue: "1")]
        public bool Active { get; set; }

        [RelmKey]
        [RelmDto]
        [RelmColumn(IsNullable: false, Unique: true)]
        public string InternalId { get; set; }

        [RelmColumn(IsNullable: false, DefaultValue: "CURRENT_TIMESTAMP")]
        public DateTime CreateDate { get; set; }

        [RelmColumn(IsNullable: false, DefaultValue: "CURRENT_TIMESTAMP")]
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Resets every property to its default value
        /// </summary>
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

        public RelmModel()
        {
            ResetCoreAttributes();
        }

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
        /// Creates an object and automatcially places data from a database row into it based on naming conventions.
        /// </summary>
        /// <param name="ModelData">Row of data from the database.</param>
        /// <param name="AlternateTableName">The alternate table name to search for in data results.</param>
        public RelmModel(DataRow ModelData, string AlternateTableName = null)
        {
            ResetWithData(ModelData, AlternateTableName: AlternateTableName);
        }

        public IRelmModel ResetWithData(DataRow ModelData, string AlternateTableName = null)
        {
            var alternateTableName = AlternateTableName ?? GetType().Name;

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
                if (ModelData.Table.Columns.IndexOf(underscoreName.Key) >= 0 && !(ModelData[underscoreName.Key] is DBNull) && underscoreName.Value.Item2.SetMethod != null)
                {
                    var jsonConverter = underscoreName.Value.Item2.GetCustomAttribute<JsonConverterAttribute>();
                    if (jsonConverter == null)
                        underscoreName.Value.Item2.SetValue(this, GetValueData(underscoreName.Key, underscoreName.Value.Item2.PropertyType, ModelData));
                    else
                        underscoreName.Value.Item2.SetValue(this, JsonConvert.DeserializeObject($"'{ModelData[underscoreName.Key]}'", underscoreName.Value.Item2.PropertyType, new JsonSerializerSettings { Converters = jsonConverters }));
                }

                // then do the alternate table names
                if (ModelData.Table.Columns.IndexOf($"{underscoreName.Key}_{alternateTableName}") >= 0 && !(ModelData[$"{underscoreName.Key}_{alternateTableName}"] is DBNull) && underscoreName.Value.Item2.SetMethod != null)
                {
                    var jsonConverter = underscoreName.Value.Item2.GetCustomAttribute<JsonConverterAttribute>();
                    if (jsonConverter == null)
                        underscoreName.Value.Item2.SetValue(this, GetValueData($"{underscoreName.Key}_{alternateTableName}", underscoreName.Value.Item2.PropertyType, ModelData));
                    else
                        underscoreName.Value.Item2.SetValue(this, JsonConvert.DeserializeObject($"'{ModelData[$"{underscoreName.Key}_{alternateTableName}"]}'", underscoreName.Value.Item2.PropertyType, new JsonSerializerSettings { Converters = jsonConverters }));
                }
            }

            return this;
        }

        /// <summary>
        /// Gets the data from the named column in the DataRow and properly parses/converts it based on Type factors.
        /// </summary>
        /// <param name="UnderscoreKey">Underscore name of the column.</param>
        /// <param name="PropertyValueType">Type of the property, used for parsing/conversion.</param>
        /// <param name="ModelData">Raw database data row.</param>
        /// <returns>The processed data.</returns>
        private object GetValueData(string UnderscoreKey, Type PropertyValueType, DataRow ModelData)
        {
            object valueData;

            // most primitive types are just 1:1 passthrough and don't require post-processing
            if (PropertyValueType == ModelData[UnderscoreKey].GetType() || ModelData[UnderscoreKey].GetType() == typeof(DateTime))
                valueData = ModelData[UnderscoreKey];

            // if it's an Enum, do a parse
            else if (PropertyValueType.BaseType == typeof(Enum))
                valueData = Enum.Parse(PropertyValueType, ModelData[UnderscoreKey].ToString());

            // if we're putting it in a DateTime, but we have a string, parse it
            else if (PropertyValueType == typeof(DateTime) && ModelData[UnderscoreKey].GetType() == typeof(string))
                valueData = DateTime.TryParse(ModelData[UnderscoreKey].ToString(), out DateTime _dateData) ? _dateData : default;

            // if we're putting it in a TimeSpan, but we have a string, parse it
            else if (PropertyValueType == typeof(TimeSpan) && ModelData[UnderscoreKey].GetType() == typeof(string))
                valueData = TimeSpan.TryParse(ModelData[UnderscoreKey].ToString(), out TimeSpan _timeSpanData) ? _timeSpanData : default;

            else if (PropertyValueType == typeof(char) && ModelData[UnderscoreKey].GetType() == typeof(string))
                valueData = ModelData[UnderscoreKey].ToString()?[0];

            // if none of those are true, then we have some serialized JSON data, so deserialize it
            else
            {
                if (ModelData[UnderscoreKey] is DBNull)
                    valueData = null;
                else
                    valueData = JsonConvert.DeserializeObject(ModelData[UnderscoreKey].ToString(), PropertyValueType);
            }

            return valueData;
        }

        /// <summary>
        /// Gets the full info about the current object's properties, including the underscore names.
        /// </summary>
        /// <param name="GetOnlyDbResolvables">Indicate to get only properties marked with the DALResolvable attribute.</param>
        /// <returns>The full list of property info including underscore names.</returns>
        public List<KeyValuePair<string, Tuple<string, PropertyInfo>>> GetUnderscoreProperties(bool GetOnlyDbResolvables = true)
        {
            return UnderscoreNamesHelper.ConvertPropertiesToUnderscoreNames(this.GetType(), GetOnlyDalResolvables: GetOnlyDbResolvables);
        }

        /// <summary>
        /// Writes the current object to the database using the table named in the DALTable attribute.
        /// </summary>
        /// <param name="connectionStringType">Type of connection to use.</param>
        /// <param name="batchSize">The number of items to write out to the database per batch.</param>
        /// <returns>The number of rows written to the database.</returns>
        public int WriteToDatabase(Enum connectionStringType, int batchSize = 100, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false)
        {
            return DataOutputOperations.BulkTableWrite(connectionStringType, this, ForceType: this.GetType(), BatchSize: batchSize, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns, AllowAutoDateColumns: AllowAutoDateColumns);
        }

        /// <summary>
        /// Writes the current object to the database using the table named in the DALTable attribute.
        /// </summary>
        /// <param name="relmContext">An IRelmContext object with open connection and transaction.</param>
        /// <param name="batchSize">The number of items to write out to the database per batch.</param>
        /// <returns>The number of rows written to the database.</returns>
        public int WriteToDatabase(IRelmContext relmContext, int batchSize = 100, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false)
        {
            return DataOutputOperations.BulkTableWrite(relmContext, this, ForceType: this.GetType(), BatchSize: batchSize, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns, AllowAutoDateColumns: AllowAutoDateColumns);
        }

        public int WriteToDatabase(IRelmQuickContext relmContext, int batchSize = 100, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false)
        {
            return DataOutputOperations.BulkTableWrite(relmContext, this, ForceType: this.GetType(), BatchSize: batchSize, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns, AllowAutoDateColumns: AllowAutoDateColumns);
        }

        public int WriteToDatabase(RelmContextOptionsBuilder relmContextOptions, int batchSize = 100, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false)
        {
            return DataOutputOperations.BulkTableWrite(new RelmContext(relmContextOptions), this, ForceType: this.GetType(), BatchSize: batchSize, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns, AllowAutoDateColumns: AllowAutoDateColumns);
        }

        /// <summary>
        /// Writes the current object to the database using the table named in the DALTable attribute.
        /// </summary>
        /// <param name="ExistingConnection">An existing and open connection to use when writing this data.</param>
        /// <param name="SqlTransaction">An optional transaction to write to the database under.</param>
        /// <param name="batchSize">The number of items to write out to the database per batch.</param>
        /// <returns>The number of rows written to the database.</returns>
        public int WriteToDatabase(MySqlConnection ExistingConnection, MySqlTransaction SqlTransaction = null, int BatchSize = 100, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false)
        {
            return DataOutputOperations.BulkTableWrite(ExistingConnection, this, SqlTransaction: SqlTransaction, ForceType: this.GetType(), BatchSize: BatchSize, AllowAutoIncrementColumns: AllowAutoIncrementColumns, AllowPrimaryKeyColumns: AllowPrimaryKeyColumns, AllowUniqueColumns: AllowUniqueColumns, AllowAutoDateColumns: AllowAutoDateColumns);
        }

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
        /// Generate a DTO POCO object based on properties marked with a DALTransferProperty attribute, plus any requested included properties, minus any requested excluded properties.
        /// If no DALTransferProperty attributes are found on a child object, this function will just include all properties from the child object.
        /// </summary>
        /// <param name="IncludeProperties">A list of properties to include in the DTO, even if they aren't marked with DALTransferProperty.</param>
        /// <param name="ExcludeProperties">A list of properties to exclude from the DTO, even if they are marked with DALTransferProperty.</param>
        /// <returns>A serializable object with only the requested properties included.</returns>
        public dynamic GenerateDTO(IEnumerable<string> IncludeProperties = null, IEnumerable<string> ExcludeProperties = null, string SourceObjectName = null, Func<IRelmModel, Dictionary<string, object>> GetAdditionalObjectProperties = null, int Iteration = 0)
        {
            var baseRef = this;
            var baseRefType = this.GetType();

            var namespaceIterations = baseRef
                .GetType()
                .FullName
                .Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            var sourceObjectIterations = SourceObjectName
                ?.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                ??
                Enumerable.Empty<string>();

            var invocationTypeList = DtoTypeProcessor
                ?.GetInvocationList()
                .ToDictionary(x => x.GetMethodInfo().GetGenericArguments().FirstOrDefault(), x => x);

            // get object properties, if any are DALBaseModels marked with DALTransferProperty then GenerateDTO() on those recursively, otherwise just return the value. if there are any IEnumerables, DTO each item in the enumerable.
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
                        || ((IncludeProperties?.Intersect(x.Value, StringComparer.InvariantCultureIgnoreCase)?.Count() ?? 0) > 0))
                    && !((ExcludeProperties?.Intersect(x.Value, StringComparer.InvariantCultureIgnoreCase).Count() ?? 0) > 0))
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
                                    ?.Select(x => x.GenerateDTO(IncludeProperties: IncludeProperties, ExcludeProperties: ExcludeProperties, SourceObjectName: string.Join(".", new List<string> { SourceObjectName, property.Name }.Where(y => !string.IsNullOrWhiteSpace(y))), GetAdditionalObjectProperties: GetAdditionalObjectProperties, Iteration: Iteration + 1)));
                        }
                        else
                        {
                            /* FOR DEBUGGING
                            var hasTransfer = property
                                    .PropertyType
                                    ?.GetRuntimeProperties()
                                    ?.Any(x => x.GetCustomAttribute<DALTransferProperty>() != null);

                            var isBaseModel = new Type[] { property.PropertyType }
                                    .FlattenTreeObject(x => string.IsNullOrWhiteSpace(x?.BaseType?.Name) ? null : new Type[] { x.BaseType })
                                    .Contains(typeof(DALBaseModel));

                            var fieldValue = property.GetValue(baseRef);
                            if ((hasTransfer ?? false) && isBaseModel)
                                fieldValue = ((DALBaseModel)property.GetValue(baseRef))?.GenerateDTO(BaseRef: baseRef, IncludeProperties: IncludeProperties, ExcludeProperties: ExcludeProperties, SourceObjectName: string.Join(".", new List<string> { SourceObjectName, property.Name }.Where(y => !string.IsNullOrWhiteSpace(y))));

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
                                ? ((RelmModel)property.GetValue(baseRef))?.GenerateDTO(IncludeProperties: IncludeProperties, ExcludeProperties: ExcludeProperties, SourceObjectName: string.Join(".", new List<string> { SourceObjectName, property.Name }.Where(y => !string.IsNullOrWhiteSpace(y))), GetAdditionalObjectProperties: GetAdditionalObjectProperties, Iteration: Iteration + 1)
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
                                    .GenerateDTO(IncludeProperties: IncludeProperties, ExcludeProperties: ExcludeProperties, SourceObjectName: string.Join(".", new List<string> { SourceObjectName, property.Name }.Where(y => !string.IsNullOrWhiteSpace(y))), GetAdditionalObjectProperties: GetAdditionalObjectProperties, Iteration: Iteration + 1));
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
                                ?.Where(x => !seed.ContainsKey(x.Key) && !(ExcludeProperties?.Contains(x.Key) ?? false))
                                .ToList();

                            if (filteredProperties != null)
                            {
                                foreach (var filteredProperty in filteredProperties)
                                {
                                    seed.Add(filteredProperty);
                                }
                            }
                        }

                        if (Iteration == 0 && GetAdditionalObjectProperties != null)
                        {
                            var additionalProperties = GetAdditionalObjectProperties(this)
                                ?.Where(x => !seed.ContainsKey(x.Key) && !(ExcludeProperties?.Contains(x.Key) ?? false))
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

        public RelmModel Duplicate()
        {
            return (RelmModel)this.MemberwiseClone();
        }
    }
}
