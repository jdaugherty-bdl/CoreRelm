using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Enums
{
    /// <summary>
    /// Represents a collection of database trigger types that define actions to be executed  before or after specific
    /// data modification operations.
    /// </summary>
    /// <remarks>The <see cref="TriggerTypes"/> enumeration provides values for common database triggers, 
    /// including those that occur before or after insert, update, or delete operations.  These trigger types can be
    /// used to specify the timing and context of database actions.</remarks>
    public class Triggers
    {
        /// <summary>
        /// Specifies the types of triggers that can be executed in response to database operations.
        /// </summary>
        /// <remarks>This enumeration defines trigger types that correspond to specific stages of database
        /// operations,  such as inserts, updates, and deletes. Triggers can be executed either before or after the
        /// operation.</remarks>
        public enum TriggerTypes
        {
            /// <summary>
            /// Occurs before an item is inserted into the collection.
            /// </summary>
            /// <remarks>This event allows subscribers to perform custom logic or validation before an
            /// item is added to the collection. If the operation should be canceled, the event handler can throw an
            /// exception or modify the state as needed.</remarks>
            BeforeInsert,
            /// <summary>
            /// Occurs after an item has been inserted into the collection or data store.
            /// </summary>
            /// <remarks>This event allows subscribers to perform additional actions or processing
            /// after an item has been successfully inserted.</remarks>
            AfterInsert,
            /// <summary>
            /// Occurs before an update operation is performed.
            /// </summary>
            /// <remarks>This event allows subscribers to execute custom logic or validate data  prior
            /// to the update operation. If the operation should be canceled,  subscribers can throw an exception or
            /// modify the state as needed.</remarks>
            BeforeUpdate,
            /// <summary>
            /// Occurs after an update operation has been completed.
            /// </summary>
            /// <remarks>This event is triggered once the update process finishes successfully. 
            /// Subscribers can use this event to perform any post-update actions, such as refreshing data or
            /// logging.</remarks>
            AfterUpdate,
            /// <summary>
            /// Occurs before an item is deleted.
            /// </summary>
            /// <remarks>This event allows subscribers to perform any necessary actions or validations
            /// prior to the deletion of the item. If the operation should be canceled,  subscribers can throw an
            /// exception or use a specific cancellation mechanism  provided by the implementation.</remarks>
            BeforeDelete,
            /// <summary>
            /// Occurs after an item has been deleted.
            /// </summary>
            /// <remarks>This event is triggered once the deletion process is completed successfully. 
            /// Subscribers can use this event to perform any post-deletion operations, such as cleanup or
            /// logging.</remarks>
            AfterDelete
        }

        /// <summary>
        /// Specifies when an action or event is triggered relative to a particular operation.
        /// </summary>
        /// <remarks>Use this enumeration to indicate whether a trigger should occur before or after the
        /// associated operation. This is commonly used in event-driven systems or database triggers to define the
        /// timing of custom logic execution.</remarks>
        public enum TriggerTime
        {
            /// <summary>
            /// Represents the state before a specified operation or event.
            /// </summary>
            BEFORE,
            /// <summary>
            /// Represents the state after a specified operation or event.
            /// </summary>
            AFTER
        }

        /// <summary>
        /// Specifies the type of database operation that can trigger an event.
        /// </summary>
        /// <remarks>Use this enumeration to indicate whether an event is triggered by an insert, update,
        /// or delete operation. This is commonly used in database change tracking, auditing, or notification
        /// scenarios.</remarks>
        public enum TriggerEvent
        {
            /// <summary>
            /// Represents the specified operation or event 'INSERT'.
            /// </summary>
            INSERT,
            /// <summary>
            /// Represents the specified operation or event 'UPDATE'.
            /// </summary>
            UPDATE,
            /// <summary>
            /// Represents the specified operation or event 'DELETE'.
            /// </summary>
            DELETE
        }

        /// <summary>
        /// Specifies the relative ordering of triggers in a sequence.
        /// </summary>
        /// <remarks>Use this enumeration to indicate whether a trigger follows or precedes another
        /// trigger in an ordered collection. This is commonly used in scenarios where the execution or evaluation order
        /// of triggers affects application behavior.</remarks>
        public enum TriggerOrdering
        {
            /// <summary>
            /// Represents the order following the specified trigger or event.
            /// </summary>
            FOLLOWS,
            /// <summary>
            /// Represents the order preceding the specified trigger or event.
            /// </summary>
            PRECEDES
        }
    }
}
