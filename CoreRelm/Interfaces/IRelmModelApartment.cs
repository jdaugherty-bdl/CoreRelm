using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces
{
    /// <summary>
    /// Represents an apartment entity within the Relm model, including associated user and membership information.
    /// </summary>
    public interface IRelmModelApartment
    {
        /// <summary>
        /// Gets or sets the unique identifier for the apartment.
        /// </summary>
        string ApartmentId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        int UserId { get; set; }

        /// <summary>
        /// Gets or sets the email address associated with the user.
        /// </summary>
        string UserEmail { get; set; }

        /// <summary>
        /// Gets or sets the user name associated with the current context.
        /// </summary>
        string UserName { get; set; }

        /// <summary>
        /// Gets or sets the associated member for this instance.
        /// </summary>
        IRelmMember Member { get; set; }
    }
}
