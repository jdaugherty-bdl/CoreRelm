using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.Options;
using CoreRelm.RelmInternal.Helpers.Operations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Commands;
using static CoreRelm.RelmInternal.Helpers.Operations.ExpressionEvaluator;

namespace CoreRelm.Models
{
    /// <summary>
    /// Provides a default implementation of the <see cref="IRelmDataLoader{T}"/> interface for loading, querying, and
    /// writing data for a specified model type. This class is designed to work with models that implement the <see
    /// cref="IRelmModel"/> interface.
    /// </summary>
    /// <remarks>This class supports operations such as adding query expressions, retrieving data, and writing
    /// data to the database. It uses a combination of database column metadata and query expressions to dynamically
    /// build SQL queries.</remarks>
    /// <typeparam name="T">The type of the model to be loaded, queried, or written. The type must implement <see cref="IRelmModel"/> and
    /// have a parameterless constructor.</typeparam>
    public class RelmDefaultDataLoader<T> : IRelmDataLoader<T> where T : IRelmModel, new()
    {
        /// <summary>
        /// Gets or sets the collection of the most recently executed commands and their associated execution details.
        /// </summary>
        /// <remarks>This property provides a record of the last executed commands along with their
        /// execution details.  The dictionary keys represent the commands, and the associated lists contain the
        /// execution information  for each command. The property can be used to inspect or analyze the history of
        /// command executions.</remarks>
        public Dictionary<Command, List<IRelmExecutionCommand>> LastCommandsExecuted { get; set; }

        // this is marked as internal to facilitate unit testing only
        // get the table name from the DALTable attribute of T
        internal virtual string TableName => typeof(T).GetCustomAttribute<RelmTable>(false)?.TableName;

        private readonly RelmContextOptionsBuilder _contextOptionsBuilder;

        private string _fullPropertySelectList;
        private DatabaseColumnRegistry<T> _columnRegistry;

        //private Dictionary<Command, List<Expression>> _commands;
        private Dictionary<Command, List<IRelmExecutionCommand>> _commands;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelmDefaultDataLoader"/> class.
        /// </summary>
        /// <remarks>This constructor performs the initial setup required for the data loader by invoking
        /// the <see cref="InitialSetup"/> method.</remarks>
        public RelmDefaultDataLoader()
        {
            InitialSetup();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelmDefaultDataLoader"/> class with the specified context
        /// options builder.
        /// </summary>
        /// <param name="contextOptionsBuilder">The builder used to configure options for the <see cref="RelmContext"/>.</param>
        public RelmDefaultDataLoader(RelmContextOptionsBuilder contextOptionsBuilder)
        {
            this._contextOptionsBuilder = contextOptionsBuilder;
         
            InitialSetup();
        }

        /// <summary>
        /// Performs the initial setup for the database column registry and prepares the property select list.
        /// </summary>
        /// <remarks>This method initializes the database column registry based on the provided context
        /// options.  If a database connection or connection string type is specified, the registry is configured
        /// accordingly.  If the context options allow opening a connection and a table name is provided, the method
        /// reads database  column descriptions for the specified table. Finally, it generates a comma-separated list of
        /// property names  for use in database queries.</remarks>
        private void InitialSetup()
        {
            if (_contextOptionsBuilder == null)
            {
                _columnRegistry = new DatabaseColumnRegistry<T>();
            }
            else
            {
                if (_contextOptionsBuilder.OptionsBuilderType == RelmContextOptionsBuilder.OptionsBuilderTypes.OpenConnection)
                    _columnRegistry = new DatabaseColumnRegistry<T>(_contextOptionsBuilder.DatabaseConnection);
                else
                    _columnRegistry = new DatabaseColumnRegistry<T>(_contextOptionsBuilder.ConnectionStringType);
            }

            if ((_contextOptionsBuilder?.CanOpenConnection ?? false) && !string.IsNullOrWhiteSpace(TableName))
                _columnRegistry.ReadDatabaseDescriptions(TableName);

            // get a list of all class property names surrounded by ` quotes separated by commas
            _fullPropertySelectList = string.Join(", ", (_columnRegistry.HasDatabaseColumns
                ? _columnRegistry.DatabaseColumns
                : _columnRegistry.PropertyColumns)
                    .Select(p => $"a.`{p.Value.Item1}`"));
        }

        /// <summary>
        /// Determines whether the specified property key exists in the column registry.
        /// </summary>
        /// <remarks>This method checks the presence of a property key in the internal column registry. 
        /// If the column registry is null, the method returns <see langword="false"/>.</remarks>
        /// <param name="PropertyKey">The key of the property to check for existence.</param>
        /// <returns><see langword="true"/> if the column registry contains the specified property key;  otherwise, <see
        /// langword="false"/>.</returns>
        public bool HasUnderscoreProperty(string PropertyKey) => _columnRegistry.PropertyColumns?.ContainsKey(PropertyKey) ?? false;

        /// <summary>
        /// Adds an expression to the specified command and returns a new execution command.
        /// </summary>
        /// <remarks>This method creates a new execution command by associating the provided expression
        /// with the specified command. The new execution command is added to the prewarmed query for the given
        /// command.</remarks>
        /// <param name="command">The command to which the expression will be added. Must not be <see langword="null"/>.</param>
        /// <param name="expression">The expression to add to the command. Must not be <see langword="null"/>.</param>
        /// <returns>A new <see cref="IRelmExecutionCommand"/> instance representing the command with the added expression.</returns>
        public IRelmExecutionCommand AddExpression(Command command, Expression expression)
        {
            var newExecution = new RelmExecutionCommand(command, expression);

            PrewarmQuery(command).Add(newExecution);

            return newExecution;
        }

        /// <summary>
        /// Adds a single expression to the specified command and returns the resulting execution command.
        /// </summary>
        /// <remarks>If the command does not already have any associated expressions, a new execution
        /// command is created.</remarks>
        /// <param name="command">The command to which the expression will be added.</param>
        /// <param name="expression">The expression to associate with the command.</param>
        /// <returns>An <see cref="IRelmExecutionCommand"/> representing the updated execution command with the specified
        /// expression.</returns>
        public IRelmExecutionCommand AddSingleExpression(Command command, Expression expression)
        {
            var expressions = PrewarmQuery(command);

            if (expressions.Count == 0)
                expressions.Add(null);

            expressions[0] = new RelmExecutionCommand(command, expression);

            return expressions[0];
        }

        /// <summary>
        /// Retrieves or initializes a list of execution commands associated with the specified predicate command.
        /// </summary>
        /// <remarks>This method ensures that a list of execution commands exists for the given predicate
        /// command. If the predicate command is not already present in the internal dictionary,  a new entry is created
        /// with an empty list of execution commands.</remarks>
        /// <param name="PredicateCommand">The predicate command used as the key to retrieve or initialize the associated execution commands.</param>
        /// <returns>A list of execution commands associated with the specified predicate command. If no commands are associated,
        /// an empty list is initialized and returned.</returns>
        private List<IRelmExecutionCommand> PrewarmQuery(Command PredicateCommand)
        {
            if (_commands == null)
                _commands = new Dictionary<Command, List<IRelmExecutionCommand>>();

            if (!_commands.ContainsKey(PredicateCommand))
                _commands.Add(PredicateCommand, new List<IRelmExecutionCommand>());

            return _commands[PredicateCommand];
        }

        /// <summary>
        /// Retrieves a collection of data entities based on the current query configuration.
        /// </summary>
        /// <remarks>This method constructs a query using the current configuration and retrieves the
        /// corresponding data entities. The returned collection may be empty if no matching data is found.</remarks>
        /// <returns>A collection of data entities of type <typeparamref name="T"/>. The collection will be empty if no data
        /// matches the query.</returns>
        public virtual ICollection<T> GetLoadData()
        {
            var findOptions = new Dictionary<string, object>();
            var selectQuery = GetSelectQuery(findOptions);

            return PullData(selectQuery, findOptions);
        }

        /// <summary>
        /// Executes the specified SQL query and retrieves a collection of data objects of type <typeparamref
        /// name="T"/>.
        /// </summary>
        /// <remarks>This method supports two modes of database connection: - If the context is configured
        /// with an open database connection, the query is executed using the provided connection and transaction. -
        /// Otherwise, the query is executed using the configured connection string type.</remarks>
        /// <param name="selectQuery">The SQL query to execute. This query should be a valid SELECT statement that matches the structure of the
        /// data objects being retrieved.</param>
        /// <param name="findOptions">A dictionary of parameters to be used in the query. The keys represent parameter names, and the values
        /// represent their corresponding values.</param>
        /// <returns>A collection of data objects of type <typeparamref name="T"/> that match the results of the query.  Returns
        /// an empty collection if no data is found.</returns>
        public virtual ICollection<T> PullData(string selectQuery, Dictionary<string, object> findOptions)
        {
            if (_contextOptionsBuilder.OptionsBuilderType == RelmContextOptionsBuilder.OptionsBuilderTypes.OpenConnection)
                return RelmHelper.GetDataObjects<T>(_contextOptionsBuilder.DatabaseConnection, selectQuery, findOptions, sqlTransaction: _contextOptionsBuilder.DatabaseTransaction).ToList();
            else
                return RelmHelper.GetDataObjects<T>(_contextOptionsBuilder.ConnectionStringType, selectQuery, findOptions).ToList();
        }

        /// <summary>
        /// Executes a database operation to write data and returns the result of the operation.
        /// </summary>
        /// <remarks>This method constructs an update query and performs the database operation based on
        /// the  configuration of the context options. It supports both open database connections and  connection
        /// strings, depending on the specified options.</remarks>
        /// <returns>The result of the database operation as an integer. The value typically represents the number of rows
        /// affected by the operation.</returns>
        public int WriteData()
        {
            var findOptions = new Dictionary<string, object>();

            var selectQuery = GetUpdateQuery(findOptions);

            if (_contextOptionsBuilder.OptionsBuilderType == RelmContextOptionsBuilder.OptionsBuilderTypes.OpenConnection)
                return RelmHelper.DoDatabaseWork<int>(_contextOptionsBuilder.DatabaseConnection, selectQuery, findOptions, sqlTransaction: _contextOptionsBuilder.DatabaseTransaction);
            else
                return RelmHelper.DoDatabaseWork<int>(_contextOptionsBuilder.ConnectionStringType, selectQuery, findOptions);
        }

        /// <summary>
        /// Constructs a SQL SELECT query based on the specified search options.
        /// </summary>
        /// <remarks>The generated query includes the full property select list and applies the specified
        /// search criteria. Ensure that the keys in <paramref name="FindOptions"/> match the column names in the
        /// database schema.</remarks>
        /// <param name="FindOptions">A dictionary containing key-value pairs that represent the search criteria.  Keys are column names, and
        /// values are the corresponding filter values.</param>
        /// <returns>A string representing the constructed SQL SELECT query.</returns>
        internal string GetSelectQuery(Dictionary<string, object> FindOptions)
        {
            return BuildQuery($"SELECT {_fullPropertySelectList} ", FindOptions, true);
        }

        /// <summary>
        /// Generates an SQL UPDATE query based on the specified criteria.
        /// </summary>
        /// <remarks>The generated query is constructed using the provided criteria in <paramref
        /// name="FindOptions" />. Ensure that the dictionary contains valid column names and values to avoid SQL syntax
        /// errors.</remarks>
        /// <param name="FindOptions">A dictionary containing column-value pairs that define the criteria for the update operation. The keys
        /// represent column names, and the values represent the corresponding values to match.</param>
        /// <returns>A string representing the constructed SQL UPDATE query.</returns>
        internal string GetUpdateQuery(Dictionary<string, object> FindOptions)
        {
            return BuildQuery($"UPDATE ", FindOptions, false);
        }

        /// <summary>
        /// Constructs a SQL query string based on the specified predicate, options, and query type.
        /// </summary>
        /// <remarks>This method dynamically evaluates and assembles various components of a SQL query,
        /// such as WHERE conditions,  ORDER BY clauses, and LIMIT constraints, based on the commands and options
        /// provided.  The table name is aliased as "a" by default, and the method ensures that all expressions are
        /// evaluated  in the context of the specified table and column mappings.</remarks>
        /// <param name="QueryPredicate">A string representing the base structure of the query, such as the operation to perform (e.g., "SELECT",
        /// "UPDATE").</param>
        /// <param name="FindOptions">A dictionary containing key-value pairs that provide additional options or parameters for the query.</param>
        /// <param name="isSelect">A boolean value indicating whether the query is a SELECT statement.  If <see langword="true"/>, the query
        /// will include a "FROM" clause.</param>
        /// <returns>A string representing the fully constructed SQL query, including clauses such as WHERE, ORDER BY, GROUP BY,
        /// and others,  based on the provided predicate and options.</returns>
        /// <exception cref="Exception">Thrown if the table name is not specified or the required table metadata is missing.</exception>
        private string BuildQuery(string QueryPredicate, Dictionary<string, object> FindOptions, bool isSelect)
        {
            if (string.IsNullOrWhiteSpace(TableName))
                throw new Exception($"RelmTable attribute not found on type {typeof(T).Name}");

            // hardcode first table alias to 'a', and inject that into the expression evaluator
            var expressionEvaluator = new ExpressionEvaluator(TableName, _columnRegistry.PropertyColumns.ToDictionary(x => x.Key, x => x.Value.Item1), UsedTableAliases: new Dictionary<string, string> { [TableName] = "a" });

            // evaluate all the pieces of the query
            var queryPieces = new Dictionary<Command, List<string>>();
            if (_commands != null)
            {
                foreach (var command in _commands)
                {
                    if (!queryPieces.ContainsKey(command.Key))
                        queryPieces.Add(command.Key, new List<string>());

                    // evaluate all expressions, except references and collections as those are evaluated after selection
                    switch (command.Key)
                    {
                        case Command.Where:
                            queryPieces[command.Key].Add(expressionEvaluator.EvaluateWhere(command, FindOptions));
                            break;
                        case Command.OrderBy:
                        case Command.OrderByDescending:
                            queryPieces[command.Key].Add(expressionEvaluator.EvaluateOrderBy(command, command.Key == Command.OrderByDescending));
                            break;
                        case Command.Set:
                            queryPieces[command.Key].Add(expressionEvaluator.EvaluateSet(command, FindOptions));
                            break;
                        case Command.Limit:
                            queryPieces[command.Key].Add(expressionEvaluator.EvaluateLimit(command));
                            break;
                        case Command.GroupBy:
                            queryPieces[command.Key].Add(expressionEvaluator.EvaluateGroupBy(command));
                            break;
                        case Command.DistinctBy:
                            queryPieces[command.Key].Add(expressionEvaluator.EvaluateDistinctBy(command));
                            break;
                        case Command.Count:
                            queryPieces[command.Key].Add(expressionEvaluator.EvaluateCount(command));
                            break;
                    }
                }
            }

            // build the query
            var predicatePieces = QueryPredicate.Split(' ');
            var findQuery = predicatePieces[0];

            findQuery += " ";

            if (queryPieces.ContainsKey(Command.Count))
            {
                findQuery += queryPieces[Command.Count];
            }
            else
            {
                if (queryPieces.ContainsKey(Command.DistinctBy))
                {
                    findQuery += string.Join("\n", queryPieces[Command.DistinctBy]);
                    findQuery += ", ";
                }

                if (predicatePieces.Length > 1)
                    findQuery += string.Join(" ", predicatePieces.Skip(1));
            }

            if (isSelect)
                findQuery += " FROM ";
            findQuery += $" `{TableName}` a "; // hardcode first table alias to 'a'

            if (queryPieces.ContainsKey(Command.Reference))
                findQuery += string.Join("\n", queryPieces[Command.Reference]);

            if (queryPieces.ContainsKey(Command.Set))
                findQuery += string.Join("\n", queryPieces[Command.Set]);

            if (queryPieces.ContainsKey(Command.Where))
                findQuery += string.Join("\n", queryPieces[Command.Where]);

            if (queryPieces.ContainsKey(Command.OrderBy))
                findQuery += string.Join("\n", queryPieces[Command.OrderBy]);
            if (queryPieces.ContainsKey(Command.OrderByDescending))
                findQuery += string.Join("\n", queryPieces[Command.OrderByDescending]);
            if (queryPieces.ContainsKey(Command.GroupBy))
                findQuery += string.Join("\n", queryPieces[Command.GroupBy]);

            if (queryPieces.ContainsKey(Command.Limit))
                findQuery += string.Join("\n", queryPieces[Command.Limit]);

            LastCommandsExecuted = _commands;
            _commands = null;

            findQuery += ";";

            return findQuery;
        }
    }
}
