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
        private bool HasWhere = false;
        private bool HasOrderBy = false;
        private bool HasGroupBy = false;

        private readonly Dictionary<string, string> UnderscoreProperties;
        private readonly Dictionary<string, string> UsedTableAliases;
        private readonly string _tableName;

        internal ExpressionEvaluator(string TableName = null, Dictionary<string, string> UnderscoreProperties = null, Dictionary<string, string> UsedTableAliases = null)
        {
            if (string.IsNullOrWhiteSpace(TableName))
                _tableName = typeof(T).GetCustomAttribute<RelmTable>(false)?.TableName ?? throw new ArgumentNullException();
            else
                _tableName = TableName;

            if ((UnderscoreProperties?.Count ?? 0) == 0)
                this.UnderscoreProperties = DataNamingHelper.GetUnderscoreProperties<T>(true, false).ToDictionary(x => x.Value.Item1, x => x.Key);
            else
                this.UnderscoreProperties = UnderscoreProperties;

            this.UsedTableAliases = UsedTableAliases ?? new Dictionary<string, string> { [_tableName] = "a" }; // reserve 'a' for the main table
        }

        private string GetTableAlias(string PropertyName)
        {
            if (string.IsNullOrWhiteSpace(PropertyName))
                return null;

            if (UsedTableAliases.ContainsKey(PropertyName))
                return UsedTableAliases[PropertyName];

            var aliasCount = UsedTableAliases.Count;
            var currentAlias = string.Concat(Enumerable.Repeat(((char)((aliasCount % 26) + 97)).ToString(), (int)(aliasCount / 26.0) + 1));

            UsedTableAliases.Add(UnderscoreProperties[PropertyName], currentAlias);

            return string.Empty;
        }

        private string GenerateParameterName(string FieldName, Dictionary<string, object> QueryParameters)
        {
            var duplicateCount = 0;
            var parameterName = $"@_{FieldName}_";

            while (QueryParameters.ContainsKey($"{parameterName}{++duplicateCount}_")) ;

            parameterName = $"{parameterName}{duplicateCount}_";

            if (QueryParameters.ContainsKey(parameterName))
                throw new AccessViolationException($"Key {parameterName} already exists.");

            return parameterName;
        }

        internal string EvaluateWhereNew(List<IRelmExecutionCommand> executionCommands, Dictionary<string, object> parameters, bool giveCommandPrefix = true, ExpressionType nodeType = ExpressionType.And)
        {
            var query = $" WHERE ( ";

            var expressionVisitor = new RelmExpressionVisitor<T>(_tableName, UnderscoreProperties, UsedTableAliases);

            // resolve query commands
            foreach (var command in executionCommands)
            {
                var commandResolution = expressionVisitor.Visit(command.ExecutionExpression);

                query += $" ( {commandResolution.Query} ) ";
            }

            query += " ) ";

            // copy query parameters
            foreach (var key in expressionVisitor.QueryParameters.Keys.ToList())
            {
                if (!parameters.ContainsKey(key))
                    parameters.Add(key, expressionVisitor.QueryParameters[key]);
                else
                    parameters[key] = expressionVisitor.QueryParameters[key];
            }

            return query;
        }

        internal string EvaluateWhere(KeyValuePair<Command, List<IRelmExecutionCommand>> CommandExpression, Dictionary<string, object> QueryParameters, bool GiveCommandPrefix = true, ExpressionType NodeType = ExpressionType.And)
        {
            var expression = new KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>>(
                CommandExpression.Key,
                CommandExpression.Value
                    .Select(x => new Tuple<Expression, ICollection<ParameterExpression>>(((LambdaExpression)x.InitialExpression).Body, ((LambdaExpression)x.InitialExpression).Parameters))
                    .ToList());

            return EvaluateWhereExpression(expression, QueryParameters, giveCommandPrefix: GiveCommandPrefix, nodeType: NodeType) + ")";
        }

        private object ResolveParameter(Expression resolvableExpression, Dictionary<string, object> queryParameters, string parameterName, bool asStringValue = false)
        {
            var parameterValue = ExpressionUtilities.GetValue(resolvableExpression);

            if (asStringValue)
                parameterValue = parameterValue.ToString();

            queryParameters.Add(parameterName, resolvableExpression.Type == typeof(bool)
                ? ((bool)parameterValue ? 1 : 0)
                : parameterValue);

            return parameterValue;
        }

        private Tuple<string, string, string> GetNamesAndAliases(MemberExpression memberExpression, Dictionary<string, object> queryParameters)
        {
            var fieldName = memberExpression.Member.Name;
            var parameterName = GenerateParameterName(memberExpression.Member.Name, queryParameters);

            var currentAlias = GetTableAlias(((RelmTable)memberExpression.Expression.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

            if (string.IsNullOrWhiteSpace(currentAlias))
                throw new TypeAccessException($"Could not find 'RelmTable' custom attribute on type: [{memberExpression.Expression.Type.FullName}]");

            return new Tuple<string, string, string>(fieldName, parameterName, currentAlias);
        }

        private string EvaluateWhereExpression(KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>> commandExpression, Dictionary<string, object> queryParameters, bool giveCommandPrefix = true, ExpressionType nodeType = ExpressionType.And)
        {
            var findQuery = string.Empty;

            foreach (var command in commandExpression.Value)
            {
                // used when calling recursive
                if (giveCommandPrefix)
                {
                    // TODO: fix bug where multiple OrElse statements produce incorrect parentheses when coupled with an And statement
                    findQuery += HasWhere
                        ? (nodeType == ExpressionType.Or || nodeType == ExpressionType.OrElse
                            ? " ) OR ( "
                            : " ) AND ( ")
                        : " WHERE (";
                
                    HasWhere = true;
                }

                if (command.Item1 is BinaryExpression binaryExpression)
                {
                    var fieldName = default(string);
                    var parameterName = default(string);
                    var currentAlias = default(string);
                    var parameterValue = default(object);
                    var enumType = default(Type);

                    // NOTE: the order of these if statements is VERY important, as the results of each are used in subsequent if statements

                    // get parameter names
                    if (binaryExpression.Left is MemberExpression memberExpressionLeft && memberExpressionLeft.NodeType == ExpressionType.MemberAccess && memberExpressionLeft.Expression.NodeType == ExpressionType.Parameter) // !(memberExpressionLeft.Expression.NodeType == ExpressionType.Constant || memberExpressionLeft.Expression.NodeType == ExpressionType.Call || memberExpressionLeft.Expression.NodeType == ExpressionType.MemberAccess))
                        (fieldName, parameterName, currentAlias) = GetNamesAndAliases(memberExpressionLeft, queryParameters);

                    if (binaryExpression.Right is MemberExpression memberExpressionRight && memberExpressionRight.NodeType == ExpressionType.MemberAccess && memberExpressionRight.Expression.NodeType == ExpressionType.Parameter) // !(memberExpressionRight.Expression.NodeType == ExpressionType.Constant || memberExpressionRight.Expression.NodeType == ExpressionType.Call || memberExpressionRight.Expression.NodeType == ExpressionType.MemberAccess))
                        (fieldName, parameterName, currentAlias) = GetNamesAndAliases(memberExpressionRight, queryParameters);

                    var leftBinaryQuery = string.Empty;
                    if (binaryExpression.Left is UnaryExpression unaryExpressionLeft)
                    {
                        //if (unaryExpressionLeft.Operand is MethodCallExpression)
                        if (unaryExpressionLeft.Operand.NodeType == ExpressionType.Call)
                            leftBinaryQuery += EvaluateWhereExpression(new KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>>(Command.Where, new List<Tuple<Expression, ICollection<ParameterExpression>>> { new Tuple<Expression, ICollection<ParameterExpression>>(unaryExpressionLeft.Operand, command.Item2) }), queryParameters, giveCommandPrefix: false, nodeType: unaryExpressionLeft.NodeType);
                        else if (unaryExpressionLeft.NodeType == ExpressionType.Convert && unaryExpressionLeft.Operand.Type.IsEnum && unaryExpressionLeft.Operand is MemberExpression memberExpression && memberExpression.Expression.NodeType == ExpressionType.Parameter)
                        {
                            (fieldName, parameterName, currentAlias) = GetNamesAndAliases(memberExpression, queryParameters);

                            enumType = memberExpression.Type;
                        }
                    }

                    var rightBinaryQuery = string.Empty;
                    if (binaryExpression.Right is UnaryExpression unaryExpressionRight)
                    {
                        //if (unaryExpressionRight.Operand is MethodCallExpression)
                        if (unaryExpressionRight.Operand.NodeType == ExpressionType.Call)
                            rightBinaryQuery += EvaluateWhereExpression(new KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>>(Command.Where, new List<Tuple<Expression, ICollection<ParameterExpression>>> { new Tuple<Expression, ICollection<ParameterExpression>>(unaryExpressionRight.Operand, command.Item2) }), queryParameters, nodeType: unaryExpressionRight.NodeType);
                        else if (unaryExpressionRight.NodeType == ExpressionType.Convert && unaryExpressionRight.Operand.Type.IsEnum && unaryExpressionRight.Operand is MemberExpression memberExpression && memberExpression.Expression.NodeType == ExpressionType.Parameter)
                        {
                            (fieldName, parameterName, currentAlias) = GetNamesAndAliases(memberExpression, queryParameters);

                            enumType = memberExpression.Type;
                        }
                    }

                    // get parameter values
                    if (binaryExpression.Left is MemberExpression memberExpressionLeft1 && memberExpressionLeft1.Expression.NodeType != ExpressionType.Parameter) // (memberExpressionLeft1.Expression.NodeType == ExpressionType.Constant || memberExpressionLeft1.Expression.NodeType == ExpressionType.Call))
                        parameterValue = ResolveParameter(memberExpressionLeft1, queryParameters, parameterName);

                    if (binaryExpression.Right is MemberExpression memberExpressionRight1 && memberExpressionRight1.Expression.NodeType != ExpressionType.Parameter) // (memberExpressionRight1.Expression.NodeType == ExpressionType.Constant || memberExpressionRight1.Expression.NodeType == ExpressionType.Call))
                        parameterValue = ResolveParameter(memberExpressionRight1, queryParameters, parameterName);

                    if (binaryExpression.Left is UnaryExpression unaryExpressionLeft1 && !(unaryExpressionLeft1.Operand is MethodCallExpression))
                    { 
                        if (unaryExpressionLeft1.NodeType == ExpressionType.Convert && unaryExpressionLeft1.Operand.Type.IsEnum && unaryExpressionLeft1.Operand is MemberExpression memberExpression)
                        {
                            if (memberExpression.Expression.NodeType != ExpressionType.Parameter)
                                parameterValue = ResolveParameter(memberExpression, queryParameters, parameterName, true); // convert all enum parameters to string representations
                        }
                        else
                            parameterValue = ResolveParameter(binaryExpression.Left, queryParameters, parameterName);
                    }

                    if (binaryExpression.Right is UnaryExpression unaryExpressionRight1 && !(unaryExpressionRight1.Operand is MethodCallExpression))
                    { 
                        if (unaryExpressionRight1.NodeType == ExpressionType.Convert && unaryExpressionRight1.Operand.Type.IsEnum && unaryExpressionRight1.Operand is MemberExpression memberExpression)
                        {
                            if (memberExpression.Expression.NodeType != ExpressionType.Parameter)
                                parameterValue = ResolveParameter(memberExpression, queryParameters, parameterName, true); // convert all enum parameters to string representations
                        }
                        else
                            parameterValue = ResolveParameter(binaryExpression.Right, queryParameters, parameterName);
                    }

                    // evaluate binary and method expressions recursively, otherwise get the parameter name and value
                    if (binaryExpression.Left is BinaryExpression subBinaryExpressionLeft)
                        leftBinaryQuery = EvaluateWhereExpression(new KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>>(Command.Where, new List<Tuple<Expression, ICollection<ParameterExpression>>> { new Tuple<Expression, ICollection<ParameterExpression>>(subBinaryExpressionLeft, command.Item2) }), queryParameters, giveCommandPrefix: false);
                    else if (binaryExpression.Left is MethodCallExpression methodCallExpressionLeft)
                    {
                        // if more than one argument, then recurse on the method call, if method call has object, resolve 
                        /*
                        if (!methodCallExpressionLeft.Arguments.Any(x => x is MemberExpression))
                            parameterValue = ResolveParameter(methodCallExpressionLeft, queryParameters, parameterName);
                        else
                            leftBinaryQuery = EvaluateWhereExpression(new KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>>(Command.Where, new List<Tuple<Expression, ICollection<ParameterExpression>>> { new Tuple<Expression, ICollection<ParameterExpression>>(methodCallExpressionLeft, command.Item2) }), queryParameters, giveCommandPrefix: false);
                        */
                        if (methodCallExpressionLeft.Arguments.Count > 1)
                            leftBinaryQuery = EvaluateWhereExpression(new KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>>(Command.Where, new List<Tuple<Expression, ICollection<ParameterExpression>>> { new Tuple<Expression, ICollection<ParameterExpression>>(methodCallExpressionLeft, command.Item2) }), queryParameters, giveCommandPrefix: false);
                        else
                            parameterValue = ResolveParameter(methodCallExpressionLeft, queryParameters, parameterName);
                    }

                    if (binaryExpression.Right is BinaryExpression subBinaryExpressionRight)
                        rightBinaryQuery = EvaluateWhereExpression(new KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>>(Command.Where, new List<Tuple<Expression, ICollection<ParameterExpression>>> { new Tuple<Expression, ICollection<ParameterExpression>>(subBinaryExpressionRight, command.Item2) }), queryParameters, nodeType: binaryExpression.NodeType);
                    else if (binaryExpression.Right is MethodCallExpression methodCallExpressionRight)
                    {
                        /*
                        if (!methodCallExpressionRight.Arguments.Any(x => x is MemberExpression))
                            parameterValue = ResolveParameter(methodCallExpressionRight, queryParameters, parameterName);
                        else
                            rightBinaryQuery = EvaluateWhereExpression(new KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>>(Command.Where, new List<Tuple<Expression, ICollection<ParameterExpression>>> { new Tuple<Expression, ICollection<ParameterExpression>>(methodCallExpressionRight, command.Item2) }), queryParameters, nodeType: binaryExpression.NodeType);
                        */
                        if (methodCallExpressionRight.Arguments.Count > 1)
                            rightBinaryQuery = EvaluateWhereExpression(new KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>>(Command.Where, new List<Tuple<Expression, ICollection<ParameterExpression>>> { new Tuple<Expression, ICollection<ParameterExpression>>(methodCallExpressionRight, command.Item2) }), queryParameters, nodeType: binaryExpression.NodeType);
                        else
                            parameterValue = ResolveParameter(methodCallExpressionRight, queryParameters, parameterName);
                    }

                    // resolve all other parameters not already resolved
                    if (binaryExpression.Left is NewExpression || binaryExpression.Left is ConstantExpression)
                    {
                        parameterValue = ResolveParameter(binaryExpression.Left, queryParameters, parameterName);

                        if (enumType != null)
                        {
                            // Convert parameterValue to the specified enum type
                            if (parameterValue is int intValue)
                                parameterValue = Enum.ToObject(enumType, intValue).ToString();

                            queryParameters[parameterName] = parameterValue;
                        }
                    }

                    if (binaryExpression.Right is NewExpression || binaryExpression.Right is ConstantExpression)
                    {
                        parameterValue = ResolveParameter(binaryExpression.Right, queryParameters, parameterName);

                        if (enumType != null)
                        {
                            // Convert parameterValue to the specified enum type
                            if (parameterValue is int intValue)
                                parameterValue = Enum.ToObject(enumType, intValue).ToString();

                            queryParameters[parameterName] = parameterValue;
                        }
                    }

                    // build the query
                    if (!string.IsNullOrWhiteSpace(leftBinaryQuery))
                        findQuery += leftBinaryQuery;
                    else
                    {
                        if (!UnderscoreProperties.ContainsKey(fieldName))
                            throw new Exception($"No field named '{fieldName}' with attribute [RelmColumn] found.");

                        findQuery += " ";
                        findQuery += currentAlias;
                        findQuery += ".`";
                        findQuery += UnderscoreProperties[fieldName];
                        findQuery += "` ";

                        switch (binaryExpression.NodeType)
                        {
                            case ExpressionType.Equal:
                                findQuery += "=";
                                break;
                            case ExpressionType.GreaterThan:
                                findQuery += ">";
                                break;
                            case ExpressionType.GreaterThanOrEqual:
                                findQuery += ">=";
                                break;
                            case ExpressionType.LessThan:
                                findQuery += "<";
                                break;
                            case ExpressionType.LessThanOrEqual:
                                findQuery += "<=";
                                break;
                            case ExpressionType.NotEqual:
                                findQuery += "<>";
                                break;
                        }
                    }

                    findQuery += " ";
                    findQuery += parameterName;
                    findQuery += " ";

                    if (!string.IsNullOrWhiteSpace(rightBinaryQuery))
                        findQuery += rightBinaryQuery;
                }
                else if (command.Item1 is MethodCallExpression methodCall)
                {
                    //if (methodCall.Arguments.Count > 1)
                    //{
                    //    EvaluateWhereExpression(new KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>>(Command.Where, new List<Tuple<Expression, ICollection<ParameterExpression>>> { new Tuple<Expression, ICollection<ParameterExpression>>(methodCall.Arguments[0], command.Item2) }), queryParameters, giveCommandPrefix: false);

                    //    var ddd = queryParameters["TESTEXPRESSION1"];
                    //    queryParameters.Remove("TESTEXPRESSION1");

                    //    command.Item2.Add(Expression.Parameter(ddd.GetType(), "y"));
                        
                    //    EvaluateWhereExpression(new KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>>(Command.Where, new List<Tuple<Expression, ICollection<ParameterExpression>>> { new Tuple<Expression, ICollection<ParameterExpression>>(methodCall.Arguments[1], command.Item2) }), queryParameters, giveCommandPrefix: false);
                    //}
                    //else
                    {
                        var referencedMember = ExpressionUtilities.GetReferencedMember(command);
                        var parameterName = referencedMember == null ? default : GenerateParameterName(referencedMember.Member.Name, queryParameters);

                        /*
                        var parameterValues1 = methodCall
                        //var parameterValues = methodCall
                            .Arguments
                            //.Select(x => x is MemberExpression ? default : ExpressionUtilities.GetValue(x))
                            .Select(x => x.NodeType == ExpressionType.Call ? ExpressionUtilities.GetValue(x) : default)
                            .ToList()
                            ;
                        */
                        var parameterValues = new List<object>();

                        foreach (var arg in methodCall.Arguments)
                        {
                            if (arg != referencedMember)
                            {
                                //if (arg is MemberExpression)
                                if (arg.NodeType == ExpressionType.Call || (arg is MemberExpression memberExpression && memberExpression.Expression.NodeType == ExpressionType.Constant))
                                {
                                    var vals = ExpressionUtilities.GetValue(arg);

                                    parameterValues.Add(vals is IEnumerable enumerable ? enumerable.Cast<object>() : vals);
                                }
                                else if (arg.NodeType == ExpressionType.Lambda)
                                {
                                    var paraValues = parameterValues;

                                    var ddd = ExpressionUtilities.ModifyLambdaExpression((LambdaExpression)arg, paraValues is IEnumerable enumerable ? enumerable.Cast<object>().ToList() : paraValues);

                                    // TODO: remove used parameter value from parameterValues
                                }
                            }
                        }

                        /*
                        var parameterValues = parameterValues1
                            .Where(x => x != null)
                            .Select(x => x is IEnumerable enumerable ? enumerable.Cast<object>().ToList() : x)
                            .ToList();

                        var parVal1 = ExpressionUtilities.GetReferencedValues(methodCall);
                        parameterValues = parVal1;
                        */

                        var parameterValue = default(object);
                        var currentAlias = default(string);

                        if (methodCall.Object != null)
                        {
                            if (methodCall.Object is MemberExpression expressedMember)
                            {
                                if (!UnderscoreProperties.ContainsKey(referencedMember.Member.Name))
                                    throw new Exception($"No field named '{referencedMember.Member.Name}' with attribute [RelmColumn] found.");

                                parameterValue = parameterValues.FirstOrDefault();

                                if (methodCall.Object.Type == typeof(string))
                                {
                                    if (methodCall.Method.Name == nameof(string.Contains))
                                        parameterValue = $"%{parameterValue}%";
                                    else if (methodCall.Method.Name == nameof(string.StartsWith))
                                        parameterValue = $"{parameterValue}%";
                                    else if (methodCall.Method.Name == nameof(string.EndsWith))
                                        parameterValue = $"%{parameterValue}";
                                    else
                                        throw new NotSupportedException();
                                }

                                currentAlias = GetTableAlias(((RelmTable)expressedMember.Expression.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

                                if (string.IsNullOrWhiteSpace(currentAlias))
                                    throw new TypeAccessException($"Could not find 'RelmTable' custom attribute on type: [{expressedMember.Expression.Type.FullName}]");

                                findQuery += " ";
                                findQuery += currentAlias;
                                findQuery += ".`";
                                findQuery += UnderscoreProperties[referencedMember.Member.Name];
                                findQuery += "` ";
                                findQuery += (parameterValue is string parVal && parVal.Contains('%')) ? "LIKE" : "=";
                                findQuery += " ";
                                findQuery += parameterName;
                                findQuery += " ";
                            }
                            else if (methodCall.Object is ConstantExpression || methodCall.Object is MethodCallExpression)
                            {
                                if (!UnderscoreProperties.ContainsKey(referencedMember.Member.Name))
                                    throw new Exception($"No field named '{referencedMember.Member.Name}' with attribute [RelmColumn] found.");

                                var constantValue = ExpressionUtilities.GetValue(methodCall.Object);

                                // if constant value is an enumerable, then string join all values and add single quotes around everything, otherwise just get the value with single quotes
                                if (constantValue is IEnumerable constantValues)
                                    parameterValue = string.Join(",", constantValues.Cast<object>());
                                else
                                    parameterValue = constantValue.ToString();

                                currentAlias = GetTableAlias(((RelmTable)referencedMember.Expression.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

                                if (string.IsNullOrWhiteSpace(currentAlias))
                                    throw new TypeAccessException($"Could not find 'RelmTable' custom attribute on type: [{referencedMember.Expression.Type.FullName}]");

                                findQuery += " FIND_IN_SET(";
                                findQuery += currentAlias;
                                findQuery += ".`";
                                findQuery += UnderscoreProperties[referencedMember.Member.Name];
                                findQuery += "`, ";
                                findQuery += parameterName;
                                findQuery += ") ";
                            }
                        }
                        else
                        {
                            if (methodCall.Method.Name == nameof(Enumerable.Contains) || methodCall.Method.Name == nameof(Enumerable.Any))
                            {
                                if (!UnderscoreProperties.ContainsKey(referencedMember.Member.Name))
                                    throw new Exception($"No field named '{referencedMember.Member.Name}' with attribute [RelmColumn] found.");

                                var parameterValueList = new List<object>();

                                foreach (var parameter in parameterValues)
                                {
                                    if (parameter is IEnumerable<object> parameterList)
                                        parameterValueList.AddRange(parameterList);
                                    else
                                        parameterValueList.Add(parameter);
                                }

                                parameterValue = string.Join(",", parameterValueList);

                                currentAlias = GetTableAlias(((RelmTable)referencedMember.Expression.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

                                if (string.IsNullOrWhiteSpace(currentAlias))
                                    throw new TypeAccessException($"Could not find 'RelmTable' custom attribute on type: [{referencedMember.Expression.Type.FullName}]");

                                findQuery += " FIND_IN_SET(";
                                findQuery += currentAlias;
                                findQuery += ".`";
                                findQuery += UnderscoreProperties[referencedMember.Member.Name];
                                findQuery += "`, ";
                                findQuery += parameterName;
                                findQuery += ") ";
                            }
                            else if (methodCall.Method.Name == nameof(string.IsNullOrEmpty) || methodCall.Method.Name == nameof(string.IsNullOrWhiteSpace))
                            {
                                if (!UnderscoreProperties.ContainsKey(referencedMember.Member.Name))
                                    throw new Exception($"No field named '{referencedMember.Member.Name}' with attribute [RelmColumn] found.");

                                currentAlias = GetTableAlias(((RelmTable)referencedMember.Expression.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

                                if (string.IsNullOrWhiteSpace(currentAlias))
                                    throw new TypeAccessException($"Could not find 'RelmTable' custom attribute on type: [{referencedMember.Expression.Type.FullName}]");

                                findQuery += " ";
                                findQuery += currentAlias;
                                findQuery += ".`";
                                findQuery += UnderscoreProperties[referencedMember.Member.Name];
                                findQuery += "` ";
                                findQuery += "IS";

                                if (nodeType == ExpressionType.Not)
                                    findQuery += " NOT";

                                findQuery += " NULL ";
                            }
                            else
                                throw new NotSupportedException($"Specified method is not supported: [{methodCall.Method.Name}]");
                        }

                        queryParameters.Add(parameterName, parameterValue);
                    }
                }
                else if (command.Item1 is UnaryExpression unaryExpression)
                {
                    findQuery += EvaluateWhereExpression(new KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>>(Command.Where, new List<Tuple<Expression, ICollection<ParameterExpression>>> { new Tuple<Expression, ICollection<ParameterExpression>>(unaryExpression.Operand, command.Item2) }), queryParameters, giveCommandPrefix: false, nodeType: unaryExpression.NodeType);
                }
                else if (command.Item1 is MemberExpression memberExpression1)
                {
                    var parameterValue = ResolveParameter(memberExpression1, queryParameters, "TESTEXPRESSION1");
                }
            }

            return findQuery;
        }

        internal Tuple<string, string> EvaluateInsertInto(KeyValuePair<Command, List<IRelmExecutionCommand>> commandExpression, Dictionary<string, object> queryParameters)
        {
            var setLines = new List<string>();
            var usedColumns = new List<string>();

            var set = commandExpression.Value.FirstOrDefault();
            var currentAlias = GetTableAlias(((RelmTable)set.InitialExpression.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

            if (string.IsNullOrWhiteSpace(currentAlias))
                throw new TypeAccessException($"Could not find 'RelmTable' custom attribute on type: [{set.InitialExpression.Type.FullName}]");

            var queryPrefix = " INSERT INTO ";
            queryPrefix += string.Join(",", setLines);
            queryPrefix += " ";

            var queryPostfix = " ON DUPLICATE KEY UPDATE ";
            queryPostfix += string.Join(",", usedColumns.Select(x => $"{x}=VALUES({x})"));
            queryPostfix += " ";

            return new Tuple<string, string>(queryPrefix, queryPostfix);
        }

        internal string EvaluateSet(KeyValuePair<Command, List<IRelmExecutionCommand>> commandExpression, Dictionary<string, object> queryParameters)
        {
            var setLines = new List<string>();
            var usedColumns = new List<string>();

            var set = commandExpression.Value.FirstOrDefault();
            var currentAlias = GetTableAlias(((RelmTable)set.InitialExpression.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

            if (string.IsNullOrWhiteSpace(currentAlias))
                throw new TypeAccessException($"Could not find 'RelmTable' custom attribute on type: [{set.InitialExpression.Type.FullName}]");

            if (set is MemberExpression memberAssignment)
            {
                var parameterName = GenerateParameterName(memberAssignment.Member.Name, queryParameters);
                var parameterValue = ExpressionUtilities.GetValue(memberAssignment.Expression);
                var columnName = UnderscoreProperties[memberAssignment.Member.Name];

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
            else if (set.InitialExpression is MemberInitExpression memberInit)
            {
                foreach (var binding in memberInit.Bindings)
                {
                    var parameterName = GenerateParameterName(binding.Member.Name, queryParameters);
                    var parameterValue = ExpressionUtilities.GetValue(((MemberAssignment)binding).Expression);
                    var columnName = UnderscoreProperties[binding.Member.Name];

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

        private string EvaluatePostProcessor(List<IRelmExecutionCommand> commandExpressionValues, bool? isDescending = null)
        {
            var findQuery = " ";

            foreach (var commandExpression in commandExpressionValues)
            {
                MemberExpression methodOperand = default;
                if (commandExpression.InitialExpression is MemberExpression methodCall)
                    methodOperand = methodCall;
                else if (commandExpression.InitialExpression is UnaryExpression unaryExpression)
                    methodOperand = unaryExpression.Operand as MemberExpression;
                else if (commandExpression.InitialExpression is LambdaExpression lambdaExpression)
                    methodOperand = lambdaExpression.Body is UnaryExpression lambdaUnary && lambdaUnary.NodeType == ExpressionType.Convert
                        ? lambdaUnary.Operand as MemberExpression
                        : lambdaExpression.Body as MemberExpression;

                if (methodOperand == default)
                {
                    if (commandExpression.InitialExpression is NewArrayExpression arrayExpression)
                        findQuery += EvaluatePostProcessor(arrayExpression
                                .Expressions
                                .Select(x => new RelmExecutionCommand(commandExpression.InitialCommand, x))
                                .Cast<IRelmExecutionCommand>()
                                .ToList()
                            , isDescending);
                    else
                        throw new InvalidCastException();
                }
                else
                {
                    var currentAlias = GetTableAlias(((RelmTable)methodOperand.Expression.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

                    if (string.IsNullOrWhiteSpace(currentAlias))
                        throw new TypeAccessException($"Could not find 'RelmTable' custom attribute on type: [{methodOperand.Expression.Type.FullName}]");

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
                    findQuery += UnderscoreProperties[methodOperand.Member.Name];
                    findQuery += "` ";

                    if (isDescending.HasValue)
                        findQuery += isDescending.Value ? " DESC " : " ASC ";
                }
            }

            return findQuery;

        }

        internal string EvaluateOrderBy(KeyValuePair<Command, List<IRelmExecutionCommand>> CommandExpression, bool IsDescending)
        {
            return EvaluatePostProcessor(CommandExpression.Value, IsDescending);
        }

        internal string EvaluateGroupBy(KeyValuePair<Command, List<IRelmExecutionCommand>> CommandExpression)
        {
            return EvaluatePostProcessor(CommandExpression.Value);
        }

        internal string EvaluateCount(KeyValuePair<Command, List<IRelmExecutionCommand>> CommandExpression)
        {
            var findQuery = string.Empty;

            var countItems = new List<string>();
            foreach (var command in CommandExpression.Value)
            {
                if (command.InitialExpression == null)
                    countItems.Add(" COUNT(*) AS `count_rows` ");
                else
                {
                    var methodOperands = new List<MemberExpression>();

                    if (command.InitialExpression is MemberExpression methodCall)
                        methodOperands.Add(methodCall);
                    else if (command.InitialExpression is UnaryExpression unaryExpression)
                        methodOperands.Add(unaryExpression.Operand as MemberExpression);
                    else if (command.InitialExpression is NewExpression newExpression)
                        methodOperands = newExpression.Arguments.Select(x => x as MemberExpression).ToList();
                    else
                        throw new InvalidCastException();

                    foreach (var methodOperand in methodOperands)
                    {
                        var currentAlias = GetTableAlias(((RelmTable)methodOperand.Expression.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

                        if (string.IsNullOrWhiteSpace(currentAlias))
                            throw new TypeAccessException($"Could not find 'RelmTable' custom attribute on type: [{methodOperand.Expression.Type.FullName}]");

                        var countExpression = " COUNT(";
                        countExpression += currentAlias;
                        countExpression += ".`";
                        countExpression += UnderscoreProperties[methodOperand.Member.Name];
                        countExpression += "`) ";
                        countExpression += "AS `count_";
                        countExpression += UnderscoreProperties[methodOperand.Member.Name];
                        countExpression += "` ";

                        countItems.Add(countExpression);
                    }
                }
            }

            findQuery += string.Join(",", countItems);

            return findQuery;
        }

        internal string EvaluateLimit(KeyValuePair<Command, List<IRelmExecutionCommand>> CommandExpression)
        {
            return $" LIMIT {(CommandExpression.Value[0].InitialExpression as ConstantExpression).Value} ";
        }

        internal string EvaluateOffset(KeyValuePair<Command, List<IRelmExecutionCommand>> CommandExpression)
        {
            return $" OFFSET {(CommandExpression.Value[0].InitialExpression as ConstantExpression).Value} ";
        }

        internal string EvaluateDistinctBy(KeyValuePair<Command, List<IRelmExecutionCommand>> CommandExpression)
        {
            MemberExpression methodOperand;
            if (CommandExpression.Value[0].InitialExpression is MemberExpression methodCall)
                methodOperand = methodCall;
            else if (CommandExpression.Value[0].InitialExpression is UnaryExpression unaryExpression)
                methodOperand = unaryExpression.Operand as MemberExpression;
            else if (CommandExpression.Value[0].InitialExpression is LambdaExpression lambdaExpression)
                methodOperand = lambdaExpression.Body is UnaryExpression lambdaUnary && lambdaUnary.NodeType == ExpressionType.Convert
                    ? lambdaUnary.Operand as MemberExpression
                    : lambdaExpression.Body as MemberExpression;
            else
                throw new InvalidCastException();

            var currentAlias = GetTableAlias(((RelmTable)methodOperand.Expression.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

            if (string.IsNullOrWhiteSpace(currentAlias))
                throw new TypeAccessException($"Could not find 'RelmTable' custom attribute on type: [{methodOperand.Expression.Type.FullName}]");

            var findQuery = $" DISTINCT ";
            findQuery += currentAlias;
            findQuery += ".`";
            findQuery += UnderscoreProperties[methodOperand.Member.Name];
            findQuery += "` ";

            return findQuery;
        }
    }
}
