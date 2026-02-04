using MoreLinq;
using MySql.Data.MySqlClient;
using CoreRelm.Attributes;
using CoreRelm.Models;
using CoreRelm.RelmInternal.Extensions;
using CoreRelm.RelmInternal.Helpers.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Triggers;

namespace CoreRelm.RelmInternal.Models
{
    internal class WritableTableDefinition<T>
    {
        private string _databaseName;

        /// <summary>
        /// Gets the name of the database table associated with this instance.
        /// </summary>
        public string TableName { get; private set; }

        /// <summary>
        /// Gets the type of the table represented by this instance.
        /// </summary>
        public Type TableType { get; private set; }

        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        public string DatabaseName
        {
            get
            {
                return _databaseName;
            }
            set
            {
                _databaseName = value.MySqlObjectQuote();
            }
        }

        /// <summary>
        /// Gets or sets the collection of properties associated with the table.
        /// </summary>
        public IEnumerable<DALPropertyType_MySQL> TableProperties { get; set; }

        /// <summary>
        /// Gets or sets the collection of triggers associated with their respective trigger types.
        /// </summary>
        /// <remarks>Use this property to manage and access triggers based on their types. The dictionary
        /// allows for  efficient retrieval and modification of triggers. Ensure that the keys in the dictionary are
        /// unique  and correspond to valid <see cref="TriggerTypes"/> values.</remarks>
        public Dictionary<TriggerTypes, RelmTrigger<T>> Triggers { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WritableTableDefinition"/> class.
        /// </summary>
        /// <remarks>This constructor initializes the table definition for a writable table based on the
        /// specified generic type <c>T</c>. It validates that the type is decorated with the required <see
        /// cref="RelmTable"/> attribute and that it contains at least one property marked with the <see
        /// cref="RelmColumn"/> attribute. If these conditions are not met, an exception is thrown. Additionally, it
        /// prepares the table's properties for use in database operations by converting property names to their
        /// underscore equivalents and associating metadata such as column names and property types.</remarks>
        /// <exception cref="CustomAttributeFormatException">Thrown if the generic type <c>T</c> does not have the <see cref="RelmTable"/> attribute or if none of its
        /// properties are decorated with the <see cref="RelmColumn"/> attribute.</exception>
        internal WritableTableDefinition()
        {
            TableType = typeof(T);
            TableName = TableType
                .GetCustomAttribute<RelmTable>()?.TableName?.MySqlObjectQuote()
                ??
                throw new CustomAttributeFormatException(CoreUtilities.NoDalTableAttributeError);

            //TODO: check if table exists and either truncate or drop as necessary

            var resolvableProperties = TableType
                .GetProperties()
                .Where(x => x.GetCustomAttribute<RelmColumn>() != null);

            if (resolvableProperties.Count() == 0)
                throw new CustomAttributeFormatException(CoreUtilities.NoDalPropertyAttributeError);

            // get properties from object, convert to underscore names
            TableProperties = UnderscoreNamesHelper
                .ConvertPropertiesToUnderscoreNames(TableType, forceLowerCase: true, getOnlyRelmColumns: true)
                .Select(x => new DALPropertyType_MySQL(x.Value.Item2.PropertyType)
                {
                    ColumnName = x.Key.MySqlObjectQuote(),
                    PropertyName = x.Value.Item1,
                    PropertyTypeInformation = x.Value.Item2,
                    ResolvableSettings = x.Value.Item2.GetCustomAttribute<RelmColumn>()
                });
        }

        /// <summary>
        /// Clears all triggers by resetting the internal collection.
        /// </summary>
        /// <returns><see langword="true"/> to indicate that the operation was successful.</returns>
        public bool ClearAllTriggers()
        {
            Triggers = new Dictionary<TriggerTypes, RelmTrigger<T>>();

            return true;
        }

        /// <summary>
        /// Sets a trigger for the table definition with the specified type and body.
        /// </summary>
        /// <remarks>This method replaces any existing trigger of the same type with the specified trigger
        /// body. To append to an existing trigger body instead of replacing it, use an alternative method if
        /// available.</remarks>
        /// <param name="triggerType">The type of the trigger to set. This determines when the trigger will be executed.</param>
        /// <param name="triggerBody">The body of the trigger, typically containing the logic or SQL statement to execute.</param>
        /// <returns>A new <see cref="WritableTableDefinition{T}"/> instance with the specified trigger applied.</returns>
        public WritableTableDefinition<T> SetTrigger(TriggerTypes triggerType, string triggerBody)
        {
            return AppendTriggerData(triggerType, triggerBody, appendTriggerBody: false);
        }

        /// <summary>
        /// Appends a trigger to the table definition.
        /// </summary>
        /// <remarks>Use this method to add a trigger to the table definition. The trigger type specifies
        /// the event that activates the trigger,  such as an INSERT, UPDATE, or DELETE operation. The trigger body
        /// should contain valid SQL defining the trigger's behavior.</remarks>
        /// <param name="triggerType">The type of the trigger to append. This determines the event that will activate the trigger.</param>
        /// <param name="triggerBody">The SQL body of the trigger, defining the actions to perform when the trigger is activated.</param>
        /// <returns>A <see cref="WritableTableDefinition{T}"/> instance with the specified trigger appended.</returns>
        public WritableTableDefinition<T> AppendTrigger(TriggerTypes triggerType, string triggerBody)
        {
            return AppendTriggerData(triggerType, triggerBody, appendTriggerBody: true);
        }

        private WritableTableDefinition<T> AppendTriggerData(TriggerTypes triggerType, string triggerBody, bool appendTriggerBody = true)
        {
            var trigger = new RelmTrigger<T>(triggerType, triggerBody)
            {
                DatabaseName = this.DatabaseName
            };

            Triggers = Triggers ?? new Dictionary<TriggerTypes, RelmTrigger<T>>();

            if (Triggers.ContainsKey(triggerType))
            {
                if (appendTriggerBody)
                    trigger.TriggerBody = $"{Triggers[triggerType].TriggerBody};{Environment.NewLine}{trigger.TriggerBody}";

                Triggers[triggerType] = trigger;
            }
            else
                Triggers.Add(triggerType, trigger);

            return this;
        }

        /// <summary>
        /// Generates a string representation of the SQL statement required to create the table, including its columns,
        /// primary key, indexes, and triggers.
        /// </summary>
        /// <remarks>The generated SQL statement includes the table name, column definitions, primary key,
        /// and any indexes or triggers associated with the table. If the table or column definitions are invalid (e.g.,
        /// missing required sizes for certain column types), an exception may be thrown.</remarks>
        /// <returns>A string containing the SQL statement to create the table and its associated triggers.</returns>
        /// <exception cref="ArgumentException">Thrown if a column definition is invalid, such as when a column type requires a size but none is provided.</exception>
        public override string ToString()
        {
            var createTableStatement = new StringBuilder();
            createTableStatement.Append("CREATE TABLE ");

            if (!string.IsNullOrWhiteSpace(DatabaseName))
                createTableStatement.Append($"{DatabaseName}.");

            createTableStatement.Append(TableName);
            createTableStatement.Append(" (");

            // do columns
            createTableStatement.Append(string
                .Join(",", TableProperties
                    .Select(x => new StringBuilder()
                        .AppendLine()
                        .Append("\t")
                        .Append(x.ColumnName)
                        .Append(" ")
                        .Append(x.PropertyColumnType)
                        .Append(x.ResolvableSettings.ColumnSize != -1 || x.ResolvableSettings.CompoundColumnSize != null
                            ? $"({string.Join(",", x.ResolvableSettings.ColumnSize == -1 ? x.ResolvableSettings.CompoundColumnSize : new int[] { x.ResolvableSettings.ColumnSize })})"
                            : x.DefaultColumnSize != -1
                                ? $"({x.DefaultColumnSize})"
                                : x == MySqlDbType.VarChar
                                    ? throw new ArgumentException($"Cannot create table, error in DALResolvable attribute on [{TableType.Name}.{x.PropertyName}]: '{x.PropertyColumnType}' requires a column size.")
                                    : string.Empty)
                        .Append(" ")
                        .Append(x.ResolvableSettings.Unique ? "UNIQUE " : string.Empty)
                        .Append(!x.ResolvableSettings.IsNullable ? "NOT " : string.Empty)
                        .Append("NULL")
                        .Append(x.ResolvableSettings.Autonumber ? " AUTO_INCREMENT" : string.Empty)
                        .Append(!string.IsNullOrWhiteSpace(x.ResolvableSettings.DefaultValue) ? $" DEFAULT {x.ResolvableSettings.DefaultValue}" : string.Empty)
                        .ToString())));

            // do primary key
            var primaryKey = TableProperties.Where(x => x.ResolvableSettings?.PrimaryKey ?? false).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(primaryKey?.ColumnName))
            {
                createTableStatement.Append(",");
                createTableStatement.AppendLine();
                createTableStatement.Append($"\tPRIMARY KEY ({primaryKey.ColumnName}),");
            }

            // do indexes
            //var indexKeys = TableProperties.Where(x => !string.IsNullOrWhiteSpace(x.ResolvableSettings?.Index));
            var indexKeys = TableProperties.Where(x => x.ResolvableSettings?.Index ?? false);
            if (indexKeys.Count() > 0)
            {
                createTableStatement
                    .Append(string
                        .Join(",", indexKeys
                            //.Select(x => new Tuple<string, bool, string>(x.ResolvableSettings?.Index?.MySqlObjectQuote(), x.ResolvableSettings?.IndexDescending ?? false, x.ColumnName))
                            .Select(x => new Tuple<string, bool, string>($"ix_{DatabaseName}_{x.ColumnName}", x.ResolvableSettings?.IndexDescending ?? false, x.ColumnName))
                            .Segment((previous, next, index) =>
                            {
                                return previous.Item1 != next.Item1;
                            })
                            .Select(x => new StringBuilder()
                                .AppendLine()
                                .Append("\tINDEX ")
                                .Append(x.FirstOrDefault()?.Item1 ?? throw new NullReferenceException($"No index name found for: {x.FirstOrDefault()?.Item3}"))
                                .Append(" (")
                                .Append(string.Join(", ", x.Select(y => new StringBuilder()
                                    .Append(y.Item3)
                                    .Append(" ")
                                    .Append(y.Item2 ? "DESC" : "ASC")
                                    .ToString())))
                                .Append(")")
                                .ToString())));
            }

            createTableStatement.AppendLine(");");
            createTableStatement.AppendLine();

            // one last chance to set the database name
            Triggers.ForEach(x => x.Value.DatabaseName = DatabaseName);

            createTableStatement.Append(string.Join(Environment.NewLine, Triggers.Select(x => x.Value.ToString())));

            return createTableStatement.ToString();
        }
    }
}
