using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using CoreRelm.Interfaces.RelmQuick;

namespace CoreRelm.Interfaces
{
    /// <summary>
    /// Defines the contract for a Relm-compatible model, providing properties and methods for managing entity state,
    /// database interactions, and data transfer object (DTO) generation.
    /// </summary>
    /// <remarks>This interface is designed to represent a database entity with common attributes such as
    /// identifiers, timestamps, and active state. It also includes methods for resetting attributes, writing data to
    /// the database, and generating dynamic DTOs. Implementations of this interface are expected to handle
    /// database-specific operations and provide fine-grained control over column constraints during write
    /// operations.</remarks>
    public interface IRelmModel
    {
        /// <summary>
        /// Gets or sets the database row identifier for the entity.
        /// </summary>
        /// <remarks>This ID corresponds to the primary key in the database table.</remarks>
        long? Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is active.
        /// </summary>
        bool Active { get; set; }

        /// <summary>
        /// Gets or sets the internal identifier for the entity.
        /// </summary>
        /// <remarks>This GUID identifier is globally unique and is intended to uniquely identify this entity across
        /// all platforms.</remarks>
        string InternalId { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the entity was created.
        /// </summary>
        DateTime CreateDate { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the object was last updated.
        /// </summary>
        DateTime LastUpdated { get; set; }

        /// <summary>
        /// Resets the core attributes of the model to their default values.
        /// </summary>
        /// <remarks>Resets fields <see cref="Id"/>, <see cref="Active"/>, <see cref="InternalId"/>, and <see cref="CreateDate"/>.</remarks>
        /// <param name="nullInternalId">A value indicating whether the internal ID should be set to <see langword="null"/>.  Pass <see
        /// langword="true"/> to nullify the internal ID; otherwise, <see langword="false"/>.</param>
        /// <param name="resetCreateDate">A value indicating whether the creation date should be reset to the current date and time.  Pass <see
        /// langword="true"/> to reset the creation date; otherwise, <see langword="false"/>.</param>
        /// <returns>The updated model instance with the core attributes reset.</returns>
        IRelmModel ResetCoreAttributes(bool nullInternalId = false, bool resetCreateDate = true);

        /// <summary>
        /// Resets the current model's state using the specified data and optionally sets an alternate table name.
        /// </summary>
        /// <param name="modelData">The <see cref="DataRow"/> containing the data to reset the model with. Cannot be <see langword="null"/>.</param>
        /// <param name="alternateTableName">An optional alternate table name to associate with the model. If <see langword="null"/>, the default table
        /// name is used.</param>
        /// <returns>An updated instance of the model implementing <see cref="IRelmModel"/> with the new data applied.</returns>
        IRelmModel ResetWithData(DataRow modelData, string alternateTableName = null);

        /// <summary>
        /// Retrieves a list of properties with each name converted to underscore case.
        /// </summary>
        /// <param name="getOnlyRelmColumns">A boolean value indicating whether to include only properties marked as Relm columns.  If <see
        /// langword="true"/>, only Realm column properties are included; otherwise, all underscore-prefixed properties
        /// are included.</param>
        /// <returns>A list of key-value pairs where the key is the property name, and the value is a tuple containing the
        /// property's metadata string and its <see cref="PropertyInfo"/>.</returns>
        List<KeyValuePair<string, Tuple<string, PropertyInfo>>> GetUnderscoreProperties(bool getOnlyRelmColumns = true);

        /// <summary>
        /// Writes data to the specified database connection in batches.
        /// </summary>
        /// <remarks>This method writes data to the database in batches to optimize performance.  Ensure
        /// that the specified connection is valid and that the data conforms to the constraints  specified by the
        /// parameter flags. If any of the flags are set to <see langword="true"/>,  the corresponding column types will
        /// be allowed in the data being written.</remarks>
        /// <param name="connectionName">The name of the database connection to use. Must be a valid connection identifier.</param>
        /// <param name="batchSize">The number of records to write in each batch. Defaults to 10. Must be greater than 0.</param>
        /// <param name="allowAutoIncrementColumns">Indicates whether auto-increment columns are allowed in the data being written. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowPrimaryKeyColumns">Indicates whether primary key columns are allowed in the data being written. Defaults to <see
        /// langword="false"/>.</param>
        /// <param name="allowUniqueColumns">Indicates whether unique columns are allowed in the data being written. Defaults to <see langword="false"/>.</param>
        /// <param name="allowAutoDateColumns">Indicates whether auto-generated date columns are allowed in the data being written. Defaults to <see
        /// langword="false"/>.</param>
        /// <returns>The number of records successfully written to the database.</returns>
        int WriteToDatabase(Enum connectionName, int batchSize = 10, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false);

        /// <summary>
        /// Writes data to the database using the specified connection and optional transaction.
        /// </summary>
        /// <param name="existingConnection">An open <see cref="MySqlConnection"/> to the database where the data will be written. The connection must
        /// remain open for the duration of the operation.</param>
        /// <param name="sqlTransaction">An optional <see cref="MySqlTransaction"/> to use for the operation. If null, the operation will not be part
        /// of a transaction.</param>
        /// <param name="BatchSize">The number of rows to write in each batch. Must be greater than zero. Defaults to 10.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether columns with auto-increment constraints are allowed to be written. If <see
        /// langword="true"/>, auto-increment columns will be included; otherwise, they will be excluded.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are allowed to be written. If <see langword="true"/>, primary
        /// key columns will be included; otherwise, they will be excluded.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns are allowed to be written. If <see langword="true"/>, unique
        /// columns will be included; otherwise, they will be excluded.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether columns with automatic date generation constraints are allowed to be written. If
        /// <see langword="true"/>, such columns will be included; otherwise, they will be excluded.</param>
        /// <returns>The number of rows successfully written to the database.</returns>
        int WriteToDatabase(MySqlConnection existingConnection, MySqlTransaction sqlTransaction = null, int BatchSize = 10, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false);

        /// <summary>
        /// Writes data to the database using the specified context and batch size, with options to control column
        /// behavior.
        /// </summary>
        /// <remarks>This method provides fine-grained control over how data is written to the database by
        /// allowing or disallowing writes to specific types of columns. Use the optional parameters to customize the
        /// behavior based on the constraints of your database schema.</remarks>
        /// <param name="relmContext">The database context used to perform the write operation. This cannot be <see langword="null"/>.</param>
        /// <param name="batchSize">The number of records to write in each batch. Must be greater than 0. The default value is 10.</param>
        /// <param name="allowAutoIncrementColumns">If <see langword="true"/>, allows writing to columns with auto-increment constraints; otherwise, these
        /// columns are ignored.</param>
        /// <param name="allowPrimaryKeyColumns">If <see langword="true"/>, allows writing to primary key columns; otherwise, these columns are ignored.</param>
        /// <param name="allowUniqueColumns">If <see langword="true"/>, allows writing to columns with unique constraints; otherwise, these columns are
        /// ignored.</param>
        /// <param name="allowAutoDateColumns">If <see langword="true"/>, allows writing to columns with automatic date generation constraints; otherwise,
        /// these columns are ignored.</param>
        /// <returns>The number of records successfully written to the database.</returns>
        int WriteToDatabase(IRelmContext relmContext, int batchSize = 10, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false);

        /// <summary>
        /// Writes data to the database using the specified context and batch size, with options to control column
        /// constraints.
        /// </summary>
        /// <remarks>This method provides fine-grained control over which types of columns are included
        /// during the write operation. Use the boolean parameters to enable or disable specific column constraints as
        /// needed.</remarks>
        /// <param name="relmQuickContext">The database context used to perform the write operation. This context must be properly initialized before
        /// calling this method.</param>
        /// <param name="batchSize">The number of records to write in each batch. Must be greater than zero. The default value is 10.</param>
        /// <param name="allowAutoIncrementColumns">A value indicating whether auto-increment columns are allowed during the write operation. If <see
        /// langword="true"/>, auto-increment columns will be included; otherwise, they will be excluded.</param>
        /// <param name="allowPrimaryKeyColumns">A value indicating whether primary key columns are allowed during the write operation. If <see
        /// langword="true"/>, primary key columns will be included; otherwise, they will be excluded.</param>
        /// <param name="allowUniqueColumns">A value indicating whether unique columns are allowed during the write operation. If <see langword="true"/>,
        /// unique columns will be included; otherwise, they will be excluded.</param>
        /// <param name="allowAutoDateColumns">A value indicating whether auto-date columns are allowed during the write operation. If <see
        /// langword="true"/>, auto-date columns will be included; otherwise, they will be excluded.</param>
        /// <returns>The total number of records successfully written to the database.</returns>
        int WriteToDatabase(IRelmQuickContext relmQuickContext, int batchSize = 10, bool allowAutoIncrementColumns = false, bool allowPrimaryKeyColumns = false, bool allowUniqueColumns = false, bool allowAutoDateColumns = false);

        /// <summary>
        /// Generates a dynamic Data Transfer Object (DTO) based on the specified properties and additional
        /// configuration.
        /// </summary>
        /// <remarks>The method allows fine-grained control over the properties included in the DTO by
        /// specifying inclusion and exclusion lists. If both <paramref name="includeProperties"/> and <paramref
        /// name="excludeProperties"/> are provided, the exclusion list takes precedence.</remarks>
        /// <param name="includeProperties">A collection of property names to include in the generated DTO. If <see langword="null"/>, all properties
        /// are included by default.</param>
        /// <param name="excludeProperties">A collection of property names to exclude from the generated DTO. If <see langword="null"/>, no properties
        /// are excluded.</param>
        /// <param name="sourceObjectName">The name of the source object from which the DTO is generated. Can be <see langword="null"/> if the source
        /// object name is not required.</param>
        /// <param name="getAdditionalObjectProperties">A function that takes an <see cref="IRelmModel"/> instance and returns a dictionary of additional properties
        /// to include in the DTO. If <see langword="null"/>, no additional properties are added.</param>
        /// <param name="iteration">The current iteration count, used to track recursive or iterative calls. Defaults to 0.</param>
        /// <returns>A dynamic object representing the generated DTO, including the specified properties and any additional
        /// properties provided.</returns>
        dynamic GenerateDTO(IEnumerable<string> includeProperties = null, IEnumerable<string> excludeProperties = null, string sourceObjectName = null, Func<IRelmModel, Dictionary<string, object>> getAdditionalObjectProperties = null, int iteration = 0);
    }
}
