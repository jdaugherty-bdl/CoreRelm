using CoreRelm.Interfaces;
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
    /// Defines methods and properties for loading, writing, and managing data, as well as executing commands with
    /// associated expressions.
    /// </summary>
    /// <typeparam name="T">The type of data managed by the loader.</typeparam>
    public interface IRelmDataLoader<T>
    {
        /// <summary>
        /// Gets or sets the collection of the most recently executed commands and their associated execution details.
        /// </summary>
        /// <remarks>This property provides a record of the last executed commands along with their
        /// execution details.  It can be used to analyze or inspect the history of command executions.</remarks>
        Dictionary<Command, List<IRelmExecutionCommand?>>? LastCommandsExecuted { get; set; }

        /// <summary>
        /// Retrieves a collection of data items of type <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>The returned collection contains the data items that are to be loaded.  The
        /// collection may be empty if no data is available.</remarks>
        /// <returns>A collection of data items of type <typeparamref name="T"/>. The collection will be empty if no data is
        /// available.</returns>
        ICollection<T?>? GetLoadData();

        /// <summary>
        /// Writes data to the underlying storage and returns the number of rows written.
        /// </summary>
        /// <returns>The number of rows successfully written to the storage.</returns>
        int WriteData();

        /// <summary>
        /// Adds an expression to the specified command and returns the resulting execution command.
        /// </summary>
        /// <param name="command">The command to which the expression will be added. Cannot be null.</param>
        /// <param name="expression">The expression to add to the command. Cannot be null.</param>
        /// <returns>An <see cref="IRelmExecutionCommand"/> representing the updated execution command with the added expression.</returns>
        IRelmExecutionCommand AddExpression(Command command, Expression expression);

        /// <summary>
        /// Adds a single expression to the specified command and returns the resulting execution command.
        /// </summary>
        /// <param name="command">The command to which the expression will be added. Cannot be <see langword="null"/>.</param>
        /// <param name="expression">The expression to add to the command. Cannot be <see langword="null"/>.</param>
        /// <returns>An <see cref="IRelmExecutionCommand"/> representing the updated command with the added expression.</returns>
        IRelmExecutionCommand? AddSingleExpression(Command command, Expression? expression);

        /// <summary>
        /// Determines whether the specified property key contains an underscore.
        /// </summary>
        /// <param name="propertyKey">The property key to check. Cannot be null or empty.</param>
        /// <returns><see langword="true"/> if the property key contains at least one underscore; otherwise, <see
        /// langword="false"/>.</returns>
        bool HasUnderscoreProperty(string propertyKey);

        /*************************************************************************************************
         *                                         ASYNC METHODS                                         *
         *************************************************************************************************/

        /// <summary>
        /// Retrieves a collection of data items of type <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>The returned collection contains the data items that are to be loaded.  The
        /// collection may be empty if no data is available.</remarks>
        /// <returns>A collection of data items of type <typeparamref name="T"/>. The collection will be empty if no data is
        /// available.</returns>
        Task<ICollection<T?>?> GetLoadDataAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Writes data to the underlying storage and returns the number of rows written.
        /// </summary>
        /// <returns>The number of rows successfully written to the storage.</returns>
        Task<int> WriteDataAsync(CancellationToken cancellationToken);
    }
}
