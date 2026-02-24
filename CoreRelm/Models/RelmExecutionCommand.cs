using MoreLinq;
using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.RelmInternal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Commands;

namespace CoreRelm.Models
{
    /// <summary>
    /// Represents a command and its associated expression for execution within the Relm framework, supporting
    /// additional and child commands for complex execution scenarios.
    /// </summary>
    /// <remarks>A RelmExecutionCommand encapsulates a primary command and expression, and can aggregate
    /// additional related commands to support batch or hierarchical execution patterns. This class is typically used to
    /// construct and manage command trees or sequences for advanced data operations, such as those involving foreign
    /// key navigation or entity relationships. Thread safety is not guaranteed; if used concurrently, external
    /// synchronization is required.</remarks>
    public class RelmExecutionCommand : IRelmExecutionCommand
    {
        /// <summary>
        /// Gets the command that initiates execution for this operation.
        /// </summary>
        public Command ExecutionCommand { get; private set; }

        /// <summary>
        /// Gets the expression that represents the execution logic for this instance.
        /// </summary>
        public Expression? ExecutionExpression { get; private set; }

        /// <summary>
        /// Gets the number of child commands associated with this instance.
        /// </summary>
        public int ChildCommandCount => _childCommands?.Count ?? 0;

        private readonly List<RelmExecutionCommand> _childCommands = [];

        /// <summary>
        /// Initializes a new instance of the RelmExecutionCommand class.
        /// </summary>
        public RelmExecutionCommand()
        {
        }

        /// <summary>
        /// Initializes a new instance of the RelmExecutionCommand class with the specified command and expression.
        /// </summary>
        /// <param name="command">The command to be executed as part of the execution command. Cannot be null.</param>
        /// <param name="expression">The expression associated with the execution command. Cannot be null.</param>
        public RelmExecutionCommand(Command command, Expression? expression)
        {
            ExecutionCommand = command;
            ExecutionExpression = expression;
        }

        /// <summary>
        /// Adds an additional command and its associated expression to the current execution command sequence.
        /// </summary>
        /// <remarks>This method enables fluent chaining by returning the same instance after adding the
        /// command and expression.</remarks>
        /// <param name="command">The command to add to the execution sequence. Cannot be null.</param>
        /// <param name="expression">The expression associated with the command. Cannot be null.</param>
        /// <returns>The current <see cref="RelmExecutionCommand"/> instance with the additional command included.</returns>
        public RelmExecutionCommand AddAdditionalCommand(Command command, Expression? expression)
        {
            _childCommands.Add(new RelmExecutionCommand(command, expression));

            return this;
        }

        /// <summary>
        /// Adds an additional command to the execution pipeline using a strongly typed expression to specify the target
        /// property or member.
        /// </summary>
        /// <typeparam name="T">The type of the object that contains the member referenced by the expression.</typeparam>
        /// <param name="command">The command to add to the execution pipeline.</param>
        /// <param name="expression">An expression that identifies the property or member of type T to which the command applies.</param>
        /// <returns>The current <see cref="RelmExecutionCommand"/> instance, enabling method chaining.</returns>
        public RelmExecutionCommand AddAdditionalCommand<T>(Command command, Expression<Func<T, object>> expression)
        {
            AddAdditionalCommand(command, expression.Body);

            return this;
        }

        /// <summary>
        /// Gets the list of additional execution commands associated with this instance.
        /// </summary>
        /// <returns>A list of <see cref="RelmExecutionCommand"/> objects representing additional commands. The list may be empty
        /// if no additional commands are present.</returns>
        public List<RelmExecutionCommand> GetAdditionalCommands()
        {
            return _childCommands;
        }

        /// <summary>
        /// Searches through all properties in the current T type and identifies the property that is marked with the RelmKey attribute, overriding "InternalId" if necessary
        /// </summary>
        /// <param name="localKeyName"></param>
        /// <returns></returns>
        internal PropertyInfo? GetReferenceKeys<T>(string localKeyName)
        {
            return GetReferenceKeys<T>([localKeyName])?.FirstOrDefault();
        }

        internal PropertyInfo[] GetReferenceKeys<T>(string[]? localKeyNames)
        {
            var referenceKeys = typeof(T).GetProperties();

            if ((localKeyNames?.Length ?? 0) > 0)
                referenceKeys = [.. referenceKeys.Where(x => localKeyNames?.Contains(x.Name) ?? false)];
            else
                referenceKeys = [.. referenceKeys.Where(x => x.GetCustomAttribute<RelmKey>() != null)];

            return referenceKeys;
        }
    }
}
