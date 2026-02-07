using CoreRelm.Models;
using CoreRelm.RelmInternal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Commands;

namespace CoreRelm.Interfaces
{
    /// <summary>
    /// Defines the contract for an execution command within the Relm framework, providing access to the initial command
    /// and expression, management of additional commands, and options for foreign key navigation.
    /// </summary>
    /// <remarks>Implementations of this interface allow for the composition and extension of execution
    /// commands, supporting scenarios where multiple related commands and expressions are processed together. The
    /// interface also facilitates navigation of foreign key relationships for collections of items, which can be useful
    /// in data access or object-relational mapping contexts.</remarks>
    public interface IRelmExecutionCommand
    {
        /// <summary>
        /// Gets the initial command that was associated with this execution command.
        /// </summary>
        Command ExecutionCommand { get; }

        /// <summary>
        /// Gets the initial expression associated with this execution command.
        /// </summary>
        Expression? ExecutionExpression { get; }

        /// <summary>
        /// Gets the number of child commands associated with the current context.
        /// </summary>
        int ChildCommandCount { get; }

        /// <summary>
        /// Adds an additional execution command to the current context using the specified command and associated
        /// expression.
        /// </summary>
        /// <param name="command">The command to be added to the execution context. Cannot be null.</param>
        /// <param name="expression">The expression associated with the command. Cannot be null.</param>
        /// <returns>A <see cref="RelmExecutionCommand"/> representing the newly added command and its associated expression.</returns>
        RelmExecutionCommand AddAdditionalCommand(Command command, Expression? expression);

        /// <summary>
        /// Retrieves a list of additional execution commands to be processed.
        /// </summary>
        /// <returns>A list of <see cref="RelmExecutionCommand"/> objects representing additional commands. The list may be empty
        /// if there are no additional commands to process.</returns>
        List<RelmExecutionCommand> GetAdditionalCommands();

        /// <summary>
        /// Retrieves navigation options for foreign key relationships based on the provided collection of items.
        /// </summary>
        /// <typeparam name="T">The type of elements contained in the collection for which to retrieve navigation options.</typeparam>
        /// <param name="_items">The collection of items used to determine the available foreign key navigation options. Cannot be null.</param>
        /// <returns>A <see cref="ForeignKeyNavigationOptions"/> instance representing the available navigation options for the
        /// specified items.</returns>
        ForeignKeyNavigationOptions GetForeignKeyNavigationOptions<T>(ICollection<T> _items);
    }
}
