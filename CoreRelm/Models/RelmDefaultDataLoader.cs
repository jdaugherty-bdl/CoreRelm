using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.Options;
using CoreRelm.RelmInternal.Helpers.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.RelmInternal.Helpers.Operations.ExpressionEvaluator;

namespace CoreRelm.Models
{
    public class RelmDefaultDataLoader<T> : IRelmDataLoader<T> where T : IRelmModel, new()
    {
        public Dictionary<Command, List<IRelmExecutionCommand?>>? LastCommandsExecuted { get; set; }

        // this is marked as internal to facilitate unit testing only
        // get the table name from the DALTable attribute of T
        internal virtual string? TableName => typeof(T).GetCustomAttribute<RelmTable>(false)?.TableName;

        private readonly RelmContextOptionsBuilder? _contextOptionsBuilder;

        private string? _fullPropertySelectList;
        private DatabaseColumnRegistry<T>? _columnRegistry = null;

        private Dictionary<Command, List<IRelmExecutionCommand?>>? _commands;

        public RelmDefaultDataLoader()
        {
            InitialSetup();
        }

        public RelmDefaultDataLoader(RelmContextOptionsBuilder contextOptionsBuilder)
        {
            _contextOptionsBuilder = contextOptionsBuilder;

            InitialSetup();
        }

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

        public bool HasUnderscoreProperty(string PropertyKey) => _columnRegistry?.PropertyColumns?.ContainsKey(PropertyKey) ?? false;

        public IRelmExecutionCommand AddExpression(Command command, Expression expression)
        {
            var newExecution = new RelmExecutionCommand(command, expression);

            PrewarmQuery(command).Add(newExecution);

            return newExecution;
        }

        public IRelmExecutionCommand? AddSingleExpression(Command command, Expression expression)
        {
            var expressions = PrewarmQuery(command);

            if (expressions.Count == 0)
                expressions.Add(null);

            expressions[0] = new RelmExecutionCommand(command, expression);

            return expressions[0];
        }

        private List<IRelmExecutionCommand?> PrewarmQuery(Command PredicateCommand)
        {
            _commands ??= [];

            if (!_commands.ContainsKey(PredicateCommand))
                _commands.Add(PredicateCommand, []);

            return _commands[PredicateCommand];
        }

        public virtual ICollection<T> GetLoadData()
        {
            var findOptions = new Dictionary<string, object>();
            var selectQuery = GetSelectQuery(findOptions);

            return PullData(selectQuery, findOptions);
        }

        public virtual ICollection<T> PullData(string selectQuery, Dictionary<string, object> findOptions)
        {
            if (_contextOptionsBuilder == null)
                throw new Exception("Context options builder not set on data loader.");

            if (_contextOptionsBuilder.OptionsBuilderType == RelmContextOptionsBuilder.OptionsBuilderTypes.OpenConnection)
                return [.. RelmHelper.GetDataObjects<T>(_contextOptionsBuilder.DatabaseConnection, selectQuery, findOptions, SqlTransaction: _contextOptionsBuilder.DatabaseTransaction)];
            else
                return [.. RelmHelper.GetDataObjects<T>(_contextOptionsBuilder.ConnectionStringType, selectQuery, findOptions)];
        }

        public int WriteData()
        {
            if (_contextOptionsBuilder == null)
                throw new Exception("Context options builder not set on data loader.");

            var findOptions = new Dictionary<string, object>();

            var selectQuery = GetUpdateQuery(findOptions);

            if (_contextOptionsBuilder.OptionsBuilderType == RelmContextOptionsBuilder.OptionsBuilderTypes.OpenConnection)
                return RelmHelper.DoDatabaseWork<int>(_contextOptionsBuilder.DatabaseConnection, selectQuery, findOptions, SqlTransaction: _contextOptionsBuilder.DatabaseTransaction);
            else
                return RelmHelper.DoDatabaseWork<int>(_contextOptionsBuilder.ConnectionStringType, selectQuery, findOptions);
        }

        internal string GetSelectQuery(Dictionary<string, object> FindOptions)
        {
            if (string.IsNullOrWhiteSpace(_fullPropertySelectList))
                throw new Exception("No properties found to select.");

            return BuildQuery($"SELECT {_fullPropertySelectList} ", FindOptions, true);
        }

        internal string GetUpdateQuery(Dictionary<string, object> FindOptions)
        {
            return BuildQuery($"UPDATE ", FindOptions, false);
        }

        private string BuildQuery(string QueryPredicate, Dictionary<string, object> FindOptions, bool isSelect)
        {
            if (string.IsNullOrWhiteSpace(TableName))
                throw new Exception($"RelmTable attribute not found on type {typeof(T).Name}");

            if (_columnRegistry == null)
                throw new Exception("Column registry not initialized.");

            // hardcode first table alias to 'a', and inject that into the expression evaluator
            var expressionEvaluator = new ExpressionEvaluator(TableName, _columnRegistry.PropertyColumns.ToDictionary(x => x.Key, x => x.Value.Item1), UsedTableAliases: new Dictionary<string, string> { [TableName] = "a" });

            // evaluate all the pieces of the query
            var queryPieces = new Dictionary<Command, List<string>>();
            if (_commands != null)
            {
                foreach (var command in _commands)
                {
                    if (!queryPieces.ContainsKey(command.Key))
                        queryPieces.Add(command.Key, []);

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

            if (queryPieces.TryGetValue(Command.Count, out List<string>? countValue))
            {
                findQuery += countValue;
            }
            else
            {
                if (queryPieces.TryGetValue(Command.DistinctBy, out List<string>? distinctByValue))
                {
                    findQuery += string.Join("\n", distinctByValue);
                    findQuery += ", ";
                }

                if (predicatePieces.Length > 1)
                    findQuery += string.Join(" ", predicatePieces.Skip(1));
            }

            if (isSelect)
                findQuery += " FROM ";
            findQuery += $" `{TableName}` a "; // hardcode first table alias to 'a'

            if (queryPieces.TryGetValue(Command.Reference, out List<string>? referenceValue))
                findQuery += string.Join("\n", referenceValue);

            if (queryPieces.TryGetValue(Command.Set, out List<string>? setValue))
                findQuery += string.Join("\n", setValue);

            if (queryPieces.TryGetValue(Command.Where, out List<string>? whereValue))
                findQuery += string.Join("\n", whereValue);

            if (queryPieces.TryGetValue(Command.OrderBy, out List<string>? orderByValue))
                findQuery += string.Join("\n", orderByValue);
            if (queryPieces.TryGetValue(Command.OrderByDescending, out List<string>? orderByDescendingValue))
                findQuery += string.Join("\n", orderByDescendingValue);
            if (queryPieces.TryGetValue(Command.GroupBy, out List<string>? groupByValue))
                findQuery += string.Join("\n", groupByValue);

            if (queryPieces.TryGetValue(Command.Limit, out List<string>? limitValue))
                findQuery += string.Join("\n", limitValue);

            LastCommandsExecuted = _commands;
            _commands = null;

            findQuery += ";";

            return findQuery;
        }
    }
}
