using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Triggers;

namespace CoreRelm.Attributes
{
    /// <summary>
    /// Specifies a trigger configuration for a property or struct, defining when and how a trigger should be executed
    /// within the Relm framework.
    /// </summary>
    /// <remarks>Apply this attribute to properties or structs to define custom trigger behavior in Relm-based
    /// applications. Multiple triggers can be configured by using this attribute with different parameters. The
    /// attribute does not enforce trigger execution; it serves as metadata for frameworks or tools that interpret
    /// trigger definitions.</remarks>
    /// <param name="triggerTime">The time at which the trigger is executed. Determines whether the trigger occurs before or after a specified
    /// event.</param>
    /// <param name="triggerEvent">The event that activates the trigger. Indicates the action or operation that causes the trigger to run.</param>
    /// <param name="triggerBody">The body or logic of the trigger. Represents the code or expression to be executed when the trigger is
    /// activated. Can be null if no body is specified.</param>
    /// <param name="triggerName">The name of the trigger associated with this instance.</param>
    /// <param name="triggerOrder">The order in which the trigger is executed relative to other triggers. Optional; can be null if no specific
    /// order is required.</param>
    /// <param name="otherTriggerName">The name of another trigger to reference or relate to. Optional; can be null if not applicable.</param>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class RelmTrigger(TriggerTime triggerTime, TriggerEvent triggerEvent, string triggerBody, string triggerName = null, TriggerOrder triggerOrder = TriggerOrder.FOLLOWS, string? otherTriggerName = null) : Attribute
    {
        /// <summary>
        /// Gets or set the trigger action time relative to the database operation (e.g., before or after an insert, update, or delete).
        /// </summary>
        public TriggerTime TriggerTime { get; set; } = triggerTime;
        
        /// <summary>
        /// Gets or sets the event that triggers the associated action.
        /// </summary>
        public TriggerEvent TriggerEvent { get; set; } = triggerEvent;
        
        /// <summary>
        /// Gets or sets the body content associated with the trigger.
        /// </summary>
        public string TriggerBody { get; set; } = triggerBody.Trim();
        
        /// <summary>
        /// Gets or sets the name of the trigger associated with this instance.
        /// </summary>
        public string? TriggerName { get; set; } = triggerName;

        /// <summary>
        /// Gets or sets the trigger order relative to the other trigger specified in <see cref="OtherTriggerName"/>.
        /// </summary>
        public TriggerOrder? TriggerOrder { get; set; } = triggerOrder;

        /// <summary>
        /// Gets or sets the name of the other trigger that this trigger is related to. Set the order with <see cref="TriggerOrder"/>.
        /// </summary>
        public string? OtherTriggerName { get; set; } = otherTriggerName;
    }
}
