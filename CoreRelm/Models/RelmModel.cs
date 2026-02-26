
using CoreRelm.Attributes;
using CoreRelm.Extensions;
using CoreRelm.Interfaces;
using CoreRelm.Models.EventArguments;
using CoreRelm.Options;
using CoreRelm.RelmInternal.Extensions;
using CoreRelm.RelmInternal.Helpers.DataTransfer;
using CoreRelm.RelmInternal.Helpers.DataTransfer.Persistence;
using CoreRelm.RelmInternal.Helpers.EqualityComparers;
using CoreRelm.RelmInternal.Helpers.Utilities;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static CoreRelm.Enums.Indexes;
using static CoreRelm.Enums.SecurityEnums;
using static CoreRelm.Enums.StoredProcedures;
using static CoreRelm.Enums.Triggers;

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

    // Create function SQL (UUIDv4 using RANDOM_BYTES)
    // Uses RANDOM_BYTES for v4 randomness; returns CHAR(36) lower-case.
    // MySQL requires delimiters when creating routines.
    // new UUID function using MySQL 8's RANDOM_BYTES for better randomness and performance, with proper delimiters for routine creation
    [RelmFunction("uuid_v4",
        @"
        -- 16 cryptographically strong random bytes
        DECLARE b BINARY(16);
        SET b = RANDOM_BYTES(16);

        -- Set version to 4 (0100xxxx) in byte 7 (1-indexed)
        SET b = INSERT(b, 7, 1, CHAR((ASCII(SUBSTR(b, 7, 1)) & 15) | 64 USING latin1));

        -- Set variant to RFC 4122 (10xxxxxx) in byte 9
        SET b = INSERT(b, 9, 1, CHAR((ASCII(SUBSTR(b, 9, 1)) & 63) | 128 USING latin1));

        -- Format as 8-4-4-4-12
        RETURN LOWER(CONCAT(
            HEX(SUBSTR(b,  1, 4)), '-',
            HEX(SUBSTR(b,  5, 2)), '-',
            HEX(SUBSTR(b,  7, 2)), '-',
            HEX(SUBSTR(b,  9, 2)), '-',
            HEX(SUBSTR(b, 11, 6))
        ));",
        returnType: "CHAR",
        returnSize: 36,
        dataAccess: ProcedureDataAccess.NoSql,
        securityLevel: SqlSecurityLevel.Invoker,
        comment: "Generates a version 4 UUID as a string in the format 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx', where 'x' is any hexadecimal digit and 'y' is one of 8, 9, A, or B.",
        isDeterministic: false)]

    [RelmTrigger(TriggerTime.BEFORE, TriggerEvent.INSERT, @"
    BEGIN
        IF NEW.InternalId IS NULL OR NEW.InternalId = '' THEN
            SET NEW.InternalId = uuid_v4();
        END IF;
    END")]
    public class RelmModel : RelmModelClean, IRelmModel
    {
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
        [RelmIndex(indexType: IndexType.UNIQUE)]
        [RelmColumn(columnSize: 45, isNullable: false)]
        public string? InternalId { get; set; }

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
        /// Initializes a new instance of the <see cref="RelmModel"/> class.
        /// </summary>
        /// <remarks>This constructor initializes the core attributes of the model by invoking the <see
        /// cref="ResetCoreAttributes"/> method.</remarks>
        public RelmModel()
        {
            ResetCoreAttributes();
        }

        /// <summary>
        /// Initializes a new instance of the RelmModel class by copying the values from the specified source model.
        /// </summary>
        /// <remarks>This constructor copies the Id, Active, InternalId, CreateDate, and LastUpdated
        /// properties from the provided model. Use this constructor to create a new RelmModel instance that mirrors the
        /// state of an existing IRelmModel.</remarks>
        /// <param name="fromModel">The source model from which to copy property values. This parameter cannot be null.</param>
        public RelmModel(IRelmModel fromModel) : base(fromModel)
        {
            ArgumentNullException.ThrowIfNull(fromModel);

            Id = fromModel.Id;
            Active = fromModel.Active;
            InternalId = fromModel.InternalId;
            CreateDate = fromModel.CreateDate;
            LastUpdated = fromModel.LastUpdated;
        }

        /// <summary>
        /// Initializes a new instance of the RelmModel class using the specified data row and an optional alternate
        /// table name.
        /// </summary>
        /// <remarks>This constructor throws an ArgumentNullException if modelData is null, ensuring that
        /// valid data is provided for model initialization.</remarks>
        /// <param name="modelData">The DataRow containing the model data. This parameter cannot be null.</param>
        /// <param name="alternateTableName">An optional string representing an alternate table name. If provided, it will be used instead of the default
        /// table name.</param>
        public RelmModel(DataRow modelData, string? alternateTableName = null) : base(modelData, alternateTableName)
        {
            ArgumentNullException.ThrowIfNull(modelData);
        }

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
        /// Resets the model's attributes using the specified data row, optionally using an alternate table name.
        /// </summary>
        /// <remarks>This method resets core attributes before delegating to the base implementation. Use
        /// this method to update the model's state from a data source.</remarks>
        /// <param name="modelData">The data row containing the values to apply to the model. Cannot be null.</param>
        /// <param name="alternateTableName">An optional alternate table name to use when resetting the model. If null, the default table name is used.</param>
        /// <returns>An instance of <see cref="IRelmModel"/> representing the model after the reset operation.</returns>
        public override IRelmModel ResetWithData(DataRow modelData, string? alternateTableName = null)
        {
            ResetCoreAttributes();

            return base.ResetWithData(modelData, alternateTableName);
        }
    }
}
