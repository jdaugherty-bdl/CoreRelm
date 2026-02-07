using CoreRelm.Attributes;
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

namespace CoreRelm.Models
{
    /// <summary>
    /// Represents a database trigger associated with a specific table and operation type.
    /// </summary>
    /// <remarks>The <see cref="RelmTrigger{T}"/> class is used to define and manage database triggers for a
    /// table represented by the type <typeparamref name="T"/>. It provides properties to configure the trigger's
    /// behavior, such as the trigger type, body, and associated database details. The table name is derived from the
    /// <see cref="RelmTable"/> attribute applied to the type.</remarks>
    /// <typeparam name="T">The type representing the database table. The type must have a <see cref="RelmTable"/> attribute to define the
    /// table name.</typeparam>
    public class RelmTrigger<T>
    {
        /// <summary>
        /// Gets the name of the database table associated with this instance.
        /// </summary>
        public string TableName { get; private set; }

        /// <summary>
        /// Gets the type of trigger associated with the current operation.
        /// </summary>
        public TriggerTypes TriggerType { get; private set; }

        /// <summary>
        /// Gets or sets the body of the trigger event.
        /// </summary>
        public string? TriggerBody { get; set; }

        /// <summary>
        /// Gets or sets the name of the user who defined the current entity or operation.
        /// </summary>
        public string DefinerUser { get; set; }

        /// <summary>
        /// Gets or sets the delimiter used to separate individual statements in a batch.
        /// </summary>
        /// <remarks>This property is typically used in scenarios where multiple statements are processed
        /// together, such as in database scripts or command batches. Ensure the delimiter matches the expected syntax
        /// of the target system.</remarks>
        public string StatementDelimiter { get; set; }

        private string? _databaseName;

        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        public string? DatabaseName
        {
            get { return _databaseName; }
            set { _databaseName = value?.MySqlObjectQuote(); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelmTrigger{T}"/> class with the specified trigger type and
        /// optional trigger body.
        /// </summary>
        /// <remarks>The <see cref="RelmTrigger{T}"/> class is used to define database triggers for a
        /// table represented by the type <typeparamref name="T"/>. The table name is derived from the <see
        /// cref="RelmTable"/> attribute applied to the type.</remarks>
        /// <param name="triggerType">The type of the trigger, specifying the database event that activates the trigger.</param>
        /// <param name="triggerBody">The SQL body of the trigger, defining the actions to be performed when the trigger is activated. This
        /// parameter is optional and can be <see langword="null"/>.</param>
        /// <exception cref="CustomAttributeFormatException">Thrown if the type <typeparamref name="T"/> does not have a <see cref="RelmTable"/> attribute or if the
        /// attribute is improperly formatted.</exception>
        internal RelmTrigger(TriggerTypes triggerType, string? triggerBody = null)
        {
            TableName = typeof(T)
                .GetCustomAttribute<RelmTable>()?.TableName?.MySqlObjectQuote()
                ??
                throw new CustomAttributeFormatException(CoreUtilities.NoDalTableAttributeError);

            this.TriggerType = triggerType;
            this.TriggerBody = triggerBody;
            this.DefinerUser = "CURRENT_USER";
            this.StatementDelimiter = "$$";
        }

        private string? TriggerTypeToIdentifier(bool isCommand)
        {
            return TriggerType switch
            {
                TriggerTypes.BeforeInsert => "BEFORE_INSERT".Replace("_", isCommand ? " " : "_"),
                TriggerTypes.AfterInsert => "AFTER_INSERT".Replace("_", isCommand ? " " : "_"),
                TriggerTypes.BeforeUpdate => "BEFORE_UPDATE".Replace("_", isCommand ? " " : "_"),
                TriggerTypes.AfterUpdate => "AFTER_UPDATE".Replace("_", isCommand ? " " : "_"),
                TriggerTypes.BeforeDelete => "BEFORE_DELETE".Replace("_", isCommand ? " " : "_"),
                TriggerTypes.AfterDelete => "AFTER_DELETE".Replace("_", isCommand ? " " : "_"),
                _ => null,
            };
        }

        /// <summary>
        /// Generates a string representation of the SQL statement required to create a database trigger.
        /// </summary>
        /// <remarks>The generated SQL includes the necessary statements to drop an existing trigger, set
        /// the delimiter,  specify the definer, and create the trigger with the provided body and configuration. The
        /// output  string is formatted to include the database name (if specified), the trigger name, and the trigger 
        /// body. This method is useful for dynamically generating SQL scripts for database triggers.</remarks>
        /// <returns>A string containing the complete SQL script to create the database trigger.</returns>
        public override string ToString()
        {
            var triggerName = $"{TableName[..^1]}_{TriggerTypeToIdentifier(false)}`";

            var createTriggerStatement = new StringBuilder();

            createTriggerStatement.Append("DROP TRIGGER IF EXISTS ");

            if (!string.IsNullOrWhiteSpace(DatabaseName))
                createTriggerStatement.Append($"{DatabaseName}.");

            createTriggerStatement.Append(triggerName);
            createTriggerStatement.AppendLine(";");

            createTriggerStatement.AppendLine();

            createTriggerStatement.Append("DELIMITER ");
            createTriggerStatement.AppendLine(StatementDelimiter);

            if (!string.IsNullOrWhiteSpace(DatabaseName))
            {
                createTriggerStatement.Append("USE ");
                createTriggerStatement.Append(DatabaseName);
                createTriggerStatement.AppendLine(StatementDelimiter);
            }

            createTriggerStatement.Append("CREATE DEFINER = ");
            createTriggerStatement.Append(DefinerUser);
            createTriggerStatement.Append(" TRIGGER ");

            if (!string.IsNullOrWhiteSpace(DatabaseName))
                createTriggerStatement.Append($"{DatabaseName}.");

            createTriggerStatement.Append(triggerName);
            createTriggerStatement.Append(' ');
            createTriggerStatement.Append(TriggerTypeToIdentifier(true));
            createTriggerStatement.Append(" ON ");
            createTriggerStatement.Append(TableName);
            createTriggerStatement.AppendLine(" FOR EACH ROW");

            createTriggerStatement.AppendLine("BEGIN");

            createTriggerStatement.AppendLine(TriggerBody);

            createTriggerStatement.Append("END");
            createTriggerStatement.AppendLine(StatementDelimiter);

            createTriggerStatement.AppendLine("DELIMITER ;");

            return createTriggerStatement.ToString();
        }
    }
}
