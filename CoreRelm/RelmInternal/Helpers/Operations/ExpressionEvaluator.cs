using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.RelmInternal.Helpers.Expressions;
using CoreRelm.RelmInternal.Helpers.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Commands;

namespace CoreRelm.RelmInternal.Helpers.Operations
{
    internal class ExpressionEvaluator<T> where T : IRelmModel, new()
    {
        private bool HasOrderBy = false;
        private bool HasGroupBy = false;

        private readonly Dictionary<string, string>? UnderscoreProperties;
        private readonly Dictionary<string, string> UsedTableAliases;
        private readonly string _tableName;

        internal ExpressionEvaluator(string? TableName = null, Dictionary<string, string>? UnderscoreProperties = null, Dictionary<string, string>? UsedTableAliases = null)
        {
            if (string.IsNullOrWhiteSpace(TableName))
                _tableName = typeof(T).GetCustomAttribute<RelmTable>(false)?.TableName ?? throw new ArgumentNullException(nameof(TableName));
            else
                _tableName = TableName;

            if ((UnderscoreProperties?.Count ?? 0) == 0)
                this.UnderscoreProperties = DataNamingHelper.GetUnderscoreProperties<T>(true, false).ToDictionary(x => x.Value.Item1, x => x.Key);
            else
                this.UnderscoreProperties = UnderscoreProperties;

            this.UsedTableAliases = UsedTableAliases ?? new Dictionary<string, string> { [_tableName] = "a" }; // reserve 'a' for the main table
        }

        private string? GetTableAlias(string? PropertyName)
        {
            if (string.IsNullOrWhiteSpace(PropertyName))
                return null;

            if (UsedTableAliases.TryGetValue(PropertyName, out string? value))
                return value;

            var aliasCount = UsedTableAliases.Count;
            var currentAlias = string.Concat(Enumerable.Repeat(((char)((aliasCount % 26) + 97)).ToString(), (int)(aliasCount / 26.0) + 1));

            if (string.IsNullOrWhiteSpace(UnderscoreProperties?.GetValueOrDefault(PropertyName)))
                throw new Exception($"No field named '{PropertyName}' with attribute [RelmColumn] found.");

            UsedTableAliases.Add(UnderscoreProperties[PropertyName], currentAlias);

            return string.Empty;
        }

        private string GenerateParameterName(string FieldName, Dictionary<string, object?> QueryParameters)
        {
            var duplicateCount = 0;
            var parameterName = $"@_{FieldName}_";

            while (QueryParameters.ContainsKey($"{parameterName}{++duplicateCount}_")) ;

            parameterName = $"{parameterName}{duplicateCount}_";

            if (QueryParameters.ContainsKey(parameterName))
                throw new AccessViolationException($"Key {parameterName} already exists.");

            return parameterName;
        }

        internal string EvaluateWhereNew(List<IRelmExecutionCommand?> executionCommands, Dictionary<string, object?> parameters)
        {
            var query = $" WHERE ( ";

            var expressionVisitor = new RelmExpressionVisitor<T>(_tableName, UnderscoreProperties, UsedTableAliases);

            // resolve query commands
            foreach (var command in executionCommands)
            {
                var commandResolution = expressionVisitor.Visit(command?.ExecutionExpression);

                query += $" ( {commandResolution?.Query} ) ";
            }

            query += " ) ";

            if (expressionVisitor?.QueryParameters == null)
                return query;

            // copy query parameters
            foreach (var key in expressionVisitor.QueryParameters.Keys.ToList())
            {
                if (expressionVisitor.QueryParameters[key] == null)
                    continue;

                if (!parameters.ContainsKey(key))
                    parameters.Add(key, expressionVisitor.QueryParameters[key]!);
                else
                    parameters[key] = expressionVisitor.QueryParameters[key]!;
            }

            return query;
        }

        internal Tuple<string, string> EvaluateInsertInto(KeyValuePair<Command, List<IRelmExecutionCommand>> commandExpression, Dictionary<string, object> queryParameters)
        {
            var setLines = new List<string>();
            var usedColumns = new List<string>();

            var set = commandExpression.Value.FirstOrDefault();
            var currentAlias = GetTableAlias(((RelmTable?)set?.ExecutionExpression?.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

            if (string.IsNullOrWhiteSpace(currentAlias))
                throw new TypeAccessException($"Could not find 'RelmTable' custom attribute on type: [{set?.ExecutionExpression?.Type.FullName}]");

            var queryPrefix = " INSERT INTO ";
            queryPrefix += string.Join(",", setLines);
            queryPrefix += " ";

            var queryPostfix = " ON DUPLICATE KEY UPDATE ";
            queryPostfix += string.Join(",", usedColumns.Select(x => $"{x}=VALUES({x})"));
            queryPostfix += " ";

            return new Tuple<string, string>(queryPrefix, queryPostfix);
        }

        internal string EvaluateSet(KeyValuePair<Command, List<IRelmExecutionCommand?>> commandExpression, Dictionary<string, object?> queryParameters)
        {
            var setLines = new List<string>();
            var usedColumns = new List<string?>();

            var set = commandExpression.Value.FirstOrDefault();
            var currentAlias = GetTableAlias(((RelmTable?)set?.ExecutionExpression?.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

            if (string.IsNullOrWhiteSpace(currentAlias))
                throw new TypeAccessException($"Could not find 'RelmTable' custom attribute on type: [{set?.ExecutionExpression?.Type.FullName}]");

            if (set is MemberExpression memberAssignment)
            {
                var parameterName = GenerateParameterName(memberAssignment.Member.Name, queryParameters);
                var parameterValue = ExpressionUtilities.GetValue(memberAssignment.Expression);
                var columnName = UnderscoreProperties?[memberAssignment.Member.Name];

                var queryLine = " ";
                queryLine += currentAlias;
                queryLine += ".`";
                queryLine += columnName;
                queryLine += "` = ";
                queryLine += parameterName;
                queryLine += " ";

                setLines.Add(queryLine);

                queryParameters.Add(parameterName, parameterValue);
                usedColumns.Add(columnName);
            }
            else if (set?.ExecutionExpression is MemberInitExpression memberInit)
            {
                foreach (var binding in memberInit.Bindings)
                {
                    var parameterName = GenerateParameterName(binding.Member.Name, queryParameters);
                    var parameterValue = ExpressionUtilities.GetValue(((MemberAssignment)binding).Expression);
                    var columnName = UnderscoreProperties?[binding.Member.Name];

                    var queryLine = " ";
                    queryLine += currentAlias;
                    queryLine += ".`";
                    queryLine += columnName;
                    queryLine += "` = ";
                    queryLine += parameterName;
                    queryLine += " ";

                    setLines.Add(queryLine);

                    queryParameters.Add(parameterName, parameterValue);
                    usedColumns.Add(columnName);
                }
            }
            else
                throw new NotSupportedException();

            var findQuery = " SET ";
            findQuery += string.Join(",", setLines);
            findQuery += " ";

            return findQuery;
        }

        private string EvaluatePostProcessor(List<IRelmExecutionCommand?> commandExpressionValues, bool? isDescending = null)
        {
            var findQuery = " ";

            foreach (var commandExpression in commandExpressionValues)
            {
                if (commandExpression == null || commandExpression.ExecutionExpression == null)
                    continue;

                MemberExpression? methodOperand = default;
                if (commandExpression.ExecutionExpression is MemberExpression methodCall)
                    methodOperand = methodCall;
                else if (commandExpression.ExecutionExpression is UnaryExpression unaryExpression)
                    methodOperand = unaryExpression.Operand as MemberExpression;
                else if (commandExpression.ExecutionExpression is LambdaExpression lambdaExpression)
                    methodOperand = lambdaExpression.Body is UnaryExpression lambdaUnary && lambdaUnary.NodeType == ExpressionType.Convert
                        ? lambdaUnary.Operand as MemberExpression
                        : lambdaExpression.Body as MemberExpression;

                if (methodOperand == default)
                {
                    if (commandExpression.ExecutionExpression is NewArrayExpression arrayExpression)
                        findQuery += EvaluatePostProcessor([.. arrayExpression
                                .Expressions
                                .Select(x => new RelmExecutionCommand(commandExpression.ExecutionCommand, x))
                                .Cast<IRelmExecutionCommand?>()]
                            , isDescending);
                    else
                        throw new InvalidCastException();
                }
                else
                {
                    var currentAlias = GetTableAlias(((RelmTable?)methodOperand.Expression?.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

                    if (string.IsNullOrWhiteSpace(currentAlias))
                        throw new TypeAccessException($"Could not find 'RelmTable' custom attribute on type: [{methodOperand.Expression?.Type.FullName}]");

                    if (!(isDescending.HasValue ? HasOrderBy : HasGroupBy))
                    {
                        findQuery += $" ";
                        findQuery += isDescending.HasValue ? "ORDER" : "GROUP";
                        findQuery += $" BY ";

                        if (isDescending.HasValue)
                            HasOrderBy = true;
                        else
                            HasGroupBy = true;
                    }
                    else
                        findQuery += ", ";

                    findQuery += currentAlias;
                    findQuery += ".`";
                    findQuery += UnderscoreProperties?[methodOperand.Member.Name];
                    findQuery += "` ";

                    if (isDescending.HasValue)
                        findQuery += isDescending.Value ? " DESC " : " ASC ";
                }
            }

            return findQuery;

        }

        internal string EvaluateOrderBy(KeyValuePair<Command, List<IRelmExecutionCommand?>> CommandExpression, bool IsDescending)
        {
            return EvaluatePostProcessor(CommandExpression.Value, IsDescending);
        }

        internal string EvaluateGroupBy(KeyValuePair<Command, List<IRelmExecutionCommand?>> CommandExpression)
        {
            return EvaluatePostProcessor(CommandExpression.Value);
        }

        internal string EvaluateCount(KeyValuePair<Command, List<IRelmExecutionCommand?>> CommandExpression)
        {
            var findQuery = string.Empty;

            var countItems = new List<string>();
            foreach (var command in CommandExpression.Value)
            {
                if (command == null || command.ExecutionExpression == null)
                    continue;

                if (command.ExecutionExpression == null)
                    countItems.Add(" COUNT(*) AS `count_rows` ");
                else
                {
                    var methodOperands = new List<MemberExpression?>();

                    if (command.ExecutionExpression is MemberExpression methodCall)
                        methodOperands.Add(methodCall);
                    else if (command.ExecutionExpression is UnaryExpression unaryExpression)
                        methodOperands.Add(unaryExpression.Operand as MemberExpression);
                    else if (command.ExecutionExpression is NewExpression newExpression)
                        methodOperands = [.. newExpression.Arguments.Select(x => x as MemberExpression)];
                    else
                        throw new InvalidCastException();

                    foreach (var methodOperand in methodOperands)
                    {
                        if (methodOperand == null)
                            throw new InvalidCastException("Unsupported expression type for COUNT command. Only member access expressions are supported.");

                        var currentAlias = GetTableAlias(((RelmTable?)methodOperand.Expression?.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

                        if (string.IsNullOrWhiteSpace(currentAlias))
                            throw new TypeAccessException($"Could not find 'RelmTable' custom attribute on type: [{methodOperand.Expression?.Type.FullName}]");

                        var countExpression = " COUNT(";
                        countExpression += currentAlias;
                        countExpression += ".`";
                        countExpression += UnderscoreProperties?[methodOperand.Member.Name];
                        countExpression += "`) ";
                        countExpression += "AS `count_";
                        countExpression += UnderscoreProperties?[methodOperand.Member.Name];
                        countExpression += "` ";

                        countItems.Add(countExpression);
                    }
                }
            }

            findQuery += string.Join(",", countItems);

            return findQuery;
        }

        internal string EvaluateLimit(KeyValuePair<Command, List<IRelmExecutionCommand?>> CommandExpression)
        {
            return $" LIMIT {(CommandExpression.Value[0]?.ExecutionExpression as ConstantExpression)?.Value} ";
        }

        internal string EvaluateOffset(KeyValuePair<Command, List<IRelmExecutionCommand?>> CommandExpression)
        {
            return $" OFFSET {(CommandExpression.Value[0]?.ExecutionExpression as ConstantExpression)?.Value} ";
        }

        internal string EvaluateDistinctBy(KeyValuePair<Command, List<IRelmExecutionCommand?>> CommandExpression)
        {
            MemberExpression? methodOperand;
            if (CommandExpression.Value[0]?.ExecutionExpression is MemberExpression methodCall)
                methodOperand = methodCall;
            else if (CommandExpression.Value[0]?.ExecutionExpression is UnaryExpression unaryExpression)
                methodOperand = unaryExpression.Operand as MemberExpression;
            else if (CommandExpression.Value[0]?.ExecutionExpression is LambdaExpression lambdaExpression)
                methodOperand = lambdaExpression.Body is UnaryExpression lambdaUnary && lambdaUnary.NodeType == ExpressionType.Convert
                    ? lambdaUnary.Operand as MemberExpression
                    : lambdaExpression.Body as MemberExpression;
            else
                throw new InvalidCastException();

            if (methodOperand == null)
                throw new InvalidCastException("Unsupported expression type for DISTINCT BY command. Only member access expressions are supported.");

            var currentAlias = GetTableAlias(((RelmTable?)methodOperand.Expression?.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

            if (string.IsNullOrWhiteSpace(currentAlias))
                throw new TypeAccessException($"Could not find 'RelmTable' custom attribute on type: [{methodOperand.Expression?.Type.FullName}]");

            var findQuery = $" DISTINCT ";
            findQuery += currentAlias;
            findQuery += ".`";
            findQuery += UnderscoreProperties?[methodOperand.Member.Name];
            findQuery += "` ";

            return findQuery;
        }
    }
}
