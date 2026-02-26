using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

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
    public interface IRelmModel : IRelmModelClean
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
        string? InternalId { get; set; }

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
    }
}
