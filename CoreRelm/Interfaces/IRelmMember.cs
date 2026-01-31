using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces
{
    /// <summary>
    /// Defines the contract for a member entity used in aparmenting within the Relm system, including identification, contact information,
    /// and value accessors.
    /// </summary>
    /// <remarks>Implementations of this interface represent individual members and provide methods to
    /// retrieve and assign values of arbitrary types associated with the member. The generic value accessors enable
    /// flexible storage and retrieval of member-specific data.</remarks>
    public interface IRelmMember
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        long Id { get; set; }

        /// <summary>
        /// Gets or sets the name associated with the user.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the email address associated with the user.
        /// </summary>
        string Email { get; set; }

        /// <summary>
        /// Gets or sets the login name associated with the user or account.
        /// </summary>
        string Login { get; set; }

        /// <summary>
        /// Retrieves the value of the specified type from the underlying source.
        /// </summary>
        /// <typeparam name="T">The type of the value to retrieve.</typeparam>
        /// <returns>The value of type <typeparamref name="T"/> obtained from the source.</returns>
        T GetValue<T>();

        /// <summary>
        /// Sets the value of the current object to the specified value.
        /// </summary>
        /// <typeparam name="T">The type of the value to set.</typeparam>
        /// <param name="value">The value to assign to the current object.</param>
        void SetValue<T>(T value);
    }
}
