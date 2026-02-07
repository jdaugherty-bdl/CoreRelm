using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.EventArguments
{
    /// <summary>
    /// Provides event data that includes additional object properties for data transfer operations.
    /// </summary>
    /// <remarks>Use this class to pass extra key-value pairs with event notifications when working with data
    /// transfer objects (DTOs). This enables event handlers to access supplementary information that may not be part of
    /// the standard event arguments.</remarks>
    public class DtoEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets a collection of additional properties associated with the object.
        /// </summary>
        /// <remarks>This dictionary allows for the storage of custom key-value pairs that are not
        /// explicitly defined by other properties. It can be used to extend the object with extra data as
        /// needed.</remarks>
        public Dictionary<string, object>? AdditionalObjectProperties { get; set; }
    }
}