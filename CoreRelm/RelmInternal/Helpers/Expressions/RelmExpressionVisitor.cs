using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.RelmInternal.Helpers.Operations;
using CoreRelm.RelmInternal.Helpers.Utilities;
using CoreRelm.RelmInternal.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Expressions
{
    internal class RelmExpressionVisitor<T> : ExpressionVisitor where T : IRelmModel, new()
    {
        public Dictionary<string, object?>? QueryParameters { get; private set; }

        private readonly Dictionary<string, string>? _underscoreProperties;
        private readonly Dictionary<string, string> _usedTableAliases;
        private readonly Dictionary<Type, Dictionary<string, string>> _objectProperties;
        private readonly HashSet<ParameterExpression> _expressionParameters = [];

        internal RelmExpressionVisitor(string? TableName = null, Dictionary<string, string>? UnderscoreProperties = null, Dictionary<string, string>? UsedTableAliases = null)
        {
            var _tableName = TableName;
            if (string.IsNullOrWhiteSpace(_tableName))
                _tableName = typeof(T).GetCustomAttribute<RelmTable>(false)?.TableName ?? throw new ArgumentNullException(nameof(_tableName));

            _underscoreProperties = UnderscoreProperties ?? [];
            if ((_underscoreProperties?.Count ?? 0) == 0)
                _underscoreProperties = DataNamingHelper.GetUnderscoreProperties<T>(true, false).ToDictionary(x => x.Value.Item1, x => x.Key);

            _objectProperties = new Dictionary<Type, Dictionary<string, string>>
            {
                [typeof(T)] = _underscoreProperties ?? []
            };

            _usedTableAliases = UsedTableAliases ?? new Dictionary<string, string> { [_tableName] = "a" }; // reserve 'a' for the main table
        }

        internal ExpressionResolution? Visit(Expression? expression, ExpressionResolution? expressionResolution = null)
        {
            if (expression == null)
                return null;

            switch (expression.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                    return this.VisitUnary((UnaryExpression)expression, expressionResolution);
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.ArrayIndex:
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                case ExpressionType.ExclusiveOr:
                    return this.VisitBinary((BinaryExpression)expression, expressionResolution);
                /*
                case ExpressionType.TypeIs:
                    return this.VisitTypeIs((TypeBinaryExpression)expression);
                case ExpressionType.Conditional:
                    return this.VisitConditional((ConditionalExpression)expression);
                */
                case ExpressionType.Constant:
                    return this.VisitConstant((ConstantExpression)expression, expressionResolution);
                case ExpressionType.Parameter:
                    return this.VisitParameter((ParameterExpression)expression, expressionResolution);
                case ExpressionType.MemberAccess:
                    return this.VisitMemberAccess((MemberExpression)expression, expressionResolution);
                case ExpressionType.Call:
                    return this.VisitMethodCall((MethodCallExpression)expression, expressionResolution);
                case ExpressionType.Lambda:
                    return this.VisitLambda((LambdaExpression)expression);
                case ExpressionType.New:
                    return this.VisitNew((NewExpression)expression, expressionResolution);
                /*
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return this.VisitNewArray((NewArrayExpression)expression);
                case ExpressionType.Invoke:
                    return this.VisitInvocation((InvocationExpression)expression);
                case ExpressionType.MemberInit:
                    return this.VisitMemberInit((MemberInitExpression)expression);
                case ExpressionType.ListInit:
                    return this.VisitListInit((ListInitExpression)expression);
                */
                default:
                    throw new Exception($"Unhandled expression type: '{expression.NodeType}'");
            }
        }

        /*
        protected virtual MemberBinding VisitBinding(MemberBinding binding)
        {
            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:
                    return this.VisitMemberAssignment((MemberAssignment)binding);
                case MemberBindingType.MemberBinding:
                    return this.VisitMemberMemberBinding((MemberMemberBinding)binding);
                case MemberBindingType.ListBinding:
                    return this.VisitMemberListBinding((MemberListBinding)binding);
                default:
                    throw new Exception($"Unhandled binding type '{binding.BindingType}'");
            }
        }

        protected virtual ElementInit VisitElementInitializer(ElementInit initializer)
        {
            var arguments = this.VisitExpressionList(initializer.Arguments);

            if (arguments != initializer.Arguments)
                return Expression.ElementInit(initializer.AddMethod, arguments);

            return initializer;
        }
        */

        protected virtual ExpressionResolution? VisitUnary(UnaryExpression unary, ExpressionResolution? expressionResolution)
        {
            var operand = this.Visit(unary.Operand, expressionResolution);

            if (operand == null)
                return operand;

            if (unary.NodeType == ExpressionType.Not)
            {
                operand.Query = $"NOT ({operand.Query})";
            }

            return operand;
        }

        protected virtual ExpressionResolution? VisitBinary(BinaryExpression binary, ExpressionResolution? expressionResolution)
        {
            var left = this.Visit(binary.Left, expressionResolution);
            ExpressionResolution? right;
            if (binary.Right.NodeType == ExpressionType.ArrayIndex)
            {
                // Evaluate lambda body (e.g., y[0]) using previously collected argument values
                // The first collected argument is the sequence for Any/Contains; pass it to resolve indexers
                var list = ((List<object?>?)(expressionResolution?.ParameterValue ?? left?.ParameterValue)) ?? [];

                if (list.All(x => x is ExpressionResolution))
                    list = [.. list.Cast<ExpressionResolution?>().Select(x => x?.ParameterValue)];

                var resolved = ExpressionUtilities.GetValueWithArguments(binary.Right, list);
                list.Add(resolved);

                right = new ExpressionResolution
                {
                    ParameterValue = resolved
                };
            }
            else
            {
                right = this.Visit(binary.Right, binary.NodeType == ExpressionType.Equal ? left : expressionResolution ?? left);

                if (right != null && (binary.Left.NodeType == ExpressionType.Convert || binary.Left.NodeType == ExpressionType.ConvertChecked))
                {
                    var convertedLeft = (UnaryExpression)binary.Left;
                    if (convertedLeft.Operand.Type != right?.ParameterValue?.GetType())
                    {
                        var targetType = convertedLeft.Operand.Type;
                        if (targetType.IsEnum)
                        {
                            // Support int and string inputs as special cases
                            /*
                            if (right!.ParameterValue is int i)
                                right.ParameterValue = Enum.ToObject(targetType, i);
                            else if (right.ParameterValue is string s)
                                right.ParameterValue = Enum.Parse(targetType, s, ignoreCase: true);
                            else
                                right.ParameterValue = Enum.ToObject(targetType, Convert.ChangeType(right.ParameterValue, Enum.GetUnderlyingType(targetType)));
                            */
                            right!.ParameterValue = right.ParameterValue switch
                            {
                                int i => Enum.ToObject(targetType, i),
                                string s => Enum.Parse(targetType, s, ignoreCase: true),
                                _ => Enum.ToObject(targetType, Convert.ChangeType(right.ParameterValue, Enum.GetUnderlyingType(targetType)))
                            };
                        }
                        else
                        {
                            right!.ParameterValue = Convert.ChangeType(right.ParameterValue, targetType);
                        }
                    }
                }
            }
            //var conversion = this.Visit(binary.Conversion);

            var fieldName = left?.Query;
            var parameterName = left?.ParameterName;
            var parameterValue = right?.ParameterValue;
            var query = left?.Query;
            var tableAlias = left?.TableAlias;

            if (string.IsNullOrWhiteSpace(left?.TableAlias))
            {
                fieldName = right?.Query;
                parameterName = right?.ParameterName;
                parameterValue = left?.ParameterValue;
                query = right?.Query;
                tableAlias = right?.TableAlias;
            }

            QueryParameters ??= [];
            if (!string.IsNullOrWhiteSpace(parameterName) && !QueryParameters.ContainsKey(parameterName) && parameterValue != null)
                QueryParameters.Add(parameterName, parameterValue);

            var finalResolution = new ExpressionResolution
            {
                FieldName = fieldName,
                ParameterName = parameterName,
                ParameterValue = parameterValue,
                Query = $"{query} {ExpressionUtilities.GetSqlOperator(binary.NodeType)} {parameterName}",
                TableAlias = tableAlias,
                NodeType = binary.NodeType
            };

            switch (binary.NodeType)
            {
                case ExpressionType.AndAlso:
                case ExpressionType.And:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    finalResolution.ParameterValue = new[] { left, right };
                    finalResolution.Query = $"({left?.Query}) {ExpressionUtilities.GetSqlOperator(binary.NodeType)} ({right?.Query})";
                    break;
                case ExpressionType.Add:
                    finalResolution.ParameterValue = new[] { left, right };
                    finalResolution.Query = $"CONCAT_WS('', {left?.Query}, {right?.Query})";
                    break;
            }

            return finalResolution;
        }

        /*
        protected virtual ExpressionResolution VisitTypeIs(TypeBinaryExpression typeBinary)
        {
            var expr = this.Visit(typeBinary.Expression);

            if (expr != typeBinary.Expression)
                return Expression.TypeIs(expr, typeBinary.TypeOperand);

            return typeBinary;
        }
        */

        protected virtual ExpressionResolution VisitConstant(ConstantExpression constant, ExpressionResolution? expressionResolution)
        {
            var constantResolution = new ExpressionResolution
            {
                ParameterValue = ExpressionUtilities.GetValue(constant)
            };

            if (constantResolution.ParameterValue?.GetType() == typeof(bool))
            {
                constantResolution.Query = (bool)constantResolution.ParameterValue ? "1" : "0";
                constantResolution.ParameterValue = (bool)constantResolution.ParameterValue ? 1 : 0;
            }
            else if (constantResolution.ParameterValue is IEnumerable valueList)
                constantResolution.Query = string.Join(",", valueList.Cast<object>().Select(v => v.ToString()));
            else
                constantResolution.Query = constantResolution.ParameterValue?.ToString();

            return constantResolution;
        }

        /*
        protected virtual ExpressionResolution VisitConditional(ConditionalExpression conditional)
        {
            var test = this.Visit(conditional.Test);
            var ifTrue = this.Visit(conditional.IfTrue);
            var ifFalse = this.Visit(conditional.IfFalse);

            if (test != conditional.Test || ifTrue != conditional.IfTrue || ifFalse != conditional.IfFalse)
                return Expression.Condition(test, ifTrue, ifFalse);

            return conditional;
        }
        */

        protected virtual ExpressionResolution VisitParameter(ParameterExpression parameter, ExpressionResolution? expressionResolution)
        {
            _expressionParameters.Add(parameter);

            if (parameter.Type.GetInterface(nameof(IRelmModel)) == null)
            {
                var parameterValue = ExpressionUtilities.GetValueWithArguments(parameter, (List<object?>?)expressionResolution?.ParameterValue);

                return new ExpressionResolution
                {
                    ParameterValue = parameterValue
                };
            }

            var currentAlias = GetTableAlias(((RelmTable?)parameter.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

            if (string.IsNullOrWhiteSpace(currentAlias))
                throw new TypeAccessException($"Could not find 'RelmTable' custom attribute on type: [{parameter.Type.FullName}]");

            return new ExpressionResolution {
                TableAlias = currentAlias
            };
        }

        protected virtual ExpressionResolution? VisitMemberAccess(MemberExpression member, ExpressionResolution? expressionResolution)
        {
            if (member.Expression == null)
                return null;

            var resolution = this.Visit(member.Expression, expressionResolution);

            if (resolution == null)
                return resolution;

            if (member.Expression.NodeType == ExpressionType.Constant)
                resolution.ParameterValue = resolution.ParameterValue?.GetType().GetField(member.Member.Name)?.GetValue(resolution.ParameterValue);
            else
            {
                if (!_objectProperties.TryGetValue(member.Expression.Type, out Dictionary<string, string>? value))
                {
                    value = DataNamingHelper.GetUnderscoreProperties(member.Expression.Type, true, false).ToDictionary(x => x.Value.Item1, x => x.Key);
                    _objectProperties[member.Expression.Type] = value;
                }

                if (member.Expression.NodeType == ExpressionType.Call)
                {
                    var callValue = ExpressionUtilities.GetValueWithArguments(member, [resolution.ParameterValue]);
                    resolution.ParameterValue = callValue;
                }

                resolution.FieldName = member.Member.Name;
                resolution.ParameterName = GenerateParameterName(resolution);

                resolution.Query = $"{resolution.TableAlias}.`{value[resolution.FieldName]}`";
            }

            return resolution;
        }

        protected virtual ExpressionResolution VisitMethodCall(MethodCallExpression methodCall, ExpressionResolution? expressionResolution)
        {
            var obj = this.Visit(methodCall.Object, expressionResolution);

            // Prepare parameters: evaluate each argument, compiling lambdas if needed
            var evaluatedParams = VisitExpressionList(methodCall.Arguments, expressionResolution ?? obj);

            // Special-case Enumerable.Contains & Enumerable.Any -> SQL translation
            if (new[] { nameof(Enumerable.Contains), nameof(Enumerable.Any) }.Contains(methodCall.Method.Name))
            {
                if (methodCall.Method.DeclaringType == typeof(Enumerable)
                    || (methodCall.Object != null && typeof(IEnumerable).IsAssignableFrom(methodCall.Object.Type) && methodCall.Object.Type != typeof(string))
                    || ((methodCall.Method.DeclaringType?.IsGenericType ?? false) && (
                        methodCall.Method.DeclaringType.GetGenericTypeDefinition() == typeof(List<>) ||
                        methodCall.Method.DeclaringType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    )))
                {
                    ExpressionResolution? valueResolution = null;
                    List<string?> queries = [];
                    List<string?> paramNames = [];
                    List<IEnumerable<string?>?> sequenceStringLists = [];
                    if (methodCall.Method.Name == nameof(Enumerable.Any))
                    {
                        valueResolution = evaluatedParams.LastOrDefault(x => x is ExpressionResolution) as ExpressionResolution;

                        if (valueResolution?.ParameterValue is IEnumerable<ExpressionResolution> resolutionValue)
                        {
                            foreach (var res in resolutionValue)
                            {
                                sequenceStringLists.Add((res.ParameterValue as IEnumerable<object>)?.Cast<string>());
                                paramNames.Add(res.ParameterName);
                                queries.Add(res.FieldName);
                            }
                        }
                        else
                        {
                            sequenceStringLists.Add((valueResolution?.ParameterValue as IEnumerable<object>)?.Cast<string>());
                            paramNames.Add(valueResolution?.ParameterName);
                            queries.Add(valueResolution?.FieldName);
                        }
                    }
                    else
                    {
                        valueResolution = this.Visit(methodCall.Arguments.Last(), expressionResolution);

                        sequenceStringLists.Add(ToStringEnumerable(evaluatedParams?[0] is ExpressionResolution ? obj?.ParameterValue : evaluatedParams?[0]));
                        paramNames.Add(valueResolution?.ParameterName);
                        queries.Add(valueResolution?.Query);
                    }

                    QueryParameters ??= [];
                    var finalQuery = string.Empty;
                    for (var i = 0; i < sequenceStringLists.Count; i++)
                    {
                        var paramName = paramNames[i];
                        if (string.IsNullOrWhiteSpace(paramName))
                            continue;

                        var sequenceStrings = sequenceStringLists[i];
                        if (i > 0)
                            finalQuery += valueResolution?.NodeType == ExpressionType.AndAlso ? " AND " : " OR ";

                        QueryParameters[paramName] = string.Join(",", sequenceStrings ?? []);

                        finalQuery += $"FIND_IN_SET({queries[i]}, {paramNames[i]})";
                    }

                    return new ExpressionResolution
                    {
                        Query = finalQuery,
                        ParameterName = paramNames[0]
                    };
                }
                else if (methodCall.Object?.Type == typeof(string))
                {
                    ExpressionResolution? valueResolution = null;
                    List<string?> queries = [];
                    List<string?> paramNames = [];
                    List<string?> matchStrings = [];

                    valueResolution = this.Visit(methodCall.Arguments.Last(), expressionResolution);

                    matchStrings.Add(evaluatedParams[0] is ExpressionResolution ? obj?.ParameterValue?.ToString() : evaluatedParams[0]?.ToString());
                    paramNames.Add(obj?.ParameterName);
                    queries.Add(obj?.Query);

                    QueryParameters ??= [];
                    var finalQuery = string.Empty;
                    for (var i = 0; i < matchStrings.Count; i++)
                    {
                        var paramName = paramNames[i];
                        if (string.IsNullOrWhiteSpace(paramName))
                            continue;

                        var matchString = matchStrings[i];
                        if (i > 0)
                            finalQuery += valueResolution?.NodeType == ExpressionType.AndAlso ? " AND " : " OR ";

                        QueryParameters[paramName] = $"%{matchString}%";

                        finalQuery += $"{queries[i]} LIKE {paramNames[i]}";
                    }

                    return new ExpressionResolution
                    {
                        Query = finalQuery,
                        ParameterName = paramNames[0]
                    };
                }
            }

            if (methodCall.Method.Name == nameof(string.IsNullOrEmpty) || methodCall.Method.Name == nameof(string.IsNullOrWhiteSpace))
            {
                var valueResolution = this.Visit(methodCall.Arguments[0], expressionResolution);
                var paramName = valueResolution?.ParameterName;

                if (valueResolution?.ParameterValue != null && !string.IsNullOrWhiteSpace(paramName))
                {
                    QueryParameters ??= [];
                    QueryParameters[paramName] = valueResolution.ParameterValue;
                }
                
                return new ExpressionResolution
                {
                    Query = $"({valueResolution?.Query} IS NULL OR {valueResolution?.Query} = '')",
                    ParameterName = paramName
                };
            }

            // General invocation (avoid passing Expression[])
            var target = methodCall.Method.IsStatic ? null : obj?.ParameterValue;
            var result = methodCall.Method.Invoke(target, [.. evaluatedParams]);

            return new ExpressionResolution { ParameterValue = result, Query = obj?.Query, TableAlias = obj?.TableAlias };
        }

        private IEnumerable<string?> ToStringEnumerable(object? sequence)
        {
            if (sequence == null)
                return [];

            if (sequence is not IEnumerable enumerable)
                return [];

            var list = new List<string?>();
            foreach (var item in enumerable)
            {
                list.Add(item?.ToString());
            }
            return list;
        }

        protected virtual List<object?> VisitExpressionList(ReadOnlyCollection<Expression> original, ExpressionResolution? expressionResolution)
        {
            var list = new List<object?>();
            var originalCount = original.Count;

            for (var i = 0; i < originalCount; i++)
            {
                if (original[i] is LambdaExpression lambda)
                {
                    if (lambda.Body is BinaryExpression || lambda.Body is MethodCallExpression)
                    {
                        expressionResolution ??= new();
                        expressionResolution.ParameterValue = list;
                        var bodyResolution = this.Visit(lambda.Body, expressionResolution);
                        list.Add(bodyResolution);
                    }
                    else
                    {
                        list.Add(lambda.Compile());
                    }
                }
                else if ((original[i] is MemberExpression memberExpression) && memberExpression.Member.MemberType == MemberTypes.Property)
                {
                    var listItemResolution = this.Visit(original[i], expressionResolution);
                    list.Add(listItemResolution);
                }
                else
                {
                    //var listItemResolution = this.Visit(original[i], expressionResolution ?? new ExpressionResolution { ParameterValue = list.FirstOrDefault() });
                    var listItemResolution = this.Visit(original[i], expressionResolution);
                    list.Add(listItemResolution?.ParameterValue);
                    //list.Add(this.Visit(original[i], expressionResolution));
                }
            }

            return list;
        }

        /*
        protected virtual MemberAssignment VisitMemberAssignment(MemberAssignment memberAssignment)
                {
            var e = this.Visit(memberAssignment.Expression);

            if (e != memberAssignment.Expression)
                return Expression.Bind(memberAssignment.Member, e);

            return memberAssignment;
        }

        protected virtual MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding memberBinding)
        {
            var bindings = this.VisitBindingList(memberBinding.Bindings);

            if (bindings != memberBinding.Bindings)
                return Expression.MemberBind(memberBinding.Member, bindings);

            return memberBinding;
        }

        protected virtual MemberListBinding VisitMemberListBinding(MemberListBinding listBinding)
        {
            var initializers = this.VisitElementInitializerList(listBinding.Initializers);

            if (initializers != listBinding.Initializers)
                return Expression.ListBind(listBinding.Member, initializers);

            return listBinding;
        }

        protected virtual IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
        {
            List<MemberBinding> list = null;
            var originalCount = original.Count;

            for (var i = 0; i < originalCount; i++)
            {
                var currentBinding = this.VisitBinding(original[i]);

                if (list != null)
                    list.Add(currentBinding);
                else if (currentBinding != original[i])
                    list = new List<MemberBinding>(originalCount);

                for (var j = 0; j < i; j++)
                {
                    list.Add(original[j]);
                }

                list.Add(currentBinding);
            }

            if (list != null)
                return list;

            return original;
        }

        protected virtual IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
        {
            List<ElementInit> list = null;
            var originalCount = original.Count;

            for (var i = 0; i < originalCount; i++)
            {
                var init = this.VisitElementInitializer(original[i]);

                if (list != null)
                    list.Add(init);
                else if (init != original[i])
                    list = new List<ElementInit>(originalCount);

                for (var j = 0; j < i; j++)
                {
                    list.Add(original[j]);
                }

                list.Add(init);
            }

            if (list != null)
            return list;

            return original;
        }
        */

        protected new ExpressionResolution? VisitLambda(LambdaExpression lambda)
        {
            var resolution = this.Visit(lambda.Body);

            return resolution;
        }

        protected virtual ExpressionResolution VisitNew(NewExpression newExpression, ExpressionResolution? expressionResolution)
        {
            var newExpressionArguments = this.VisitExpressionList(newExpression.Arguments, expressionResolution);
            var newValue = newExpression.Constructor?.Invoke(newExpressionArguments.ToArray());

            var resolution = new ExpressionResolution
            {
                ParameterValue = QueryParameters[expressionResolution.ParameterName] = newValue
            };

            if (newExpression.Type == typeof(DateTime))
                resolution.Query = string.Join("-", newExpressionArguments);

            /*
            if (newExpressionArguments != newExpression.Arguments)
            {
                if (newExpression.Members != null)
                    return Expression.New(newExpression.Constructor, newExpressionArguments, newExpression.Members);
                else
                    return Expression.New(newExpression.Constructor, newExpressionArguments);
            }

            return newExpression;
            */
            return resolution;
        }
        /*

        protected virtual ExpressionResolution VisitMemberInit(MemberInitExpression memberInit)
        {
            var newExpression = this.VisitNew(memberInit.NewExpression);
            var bindings = this.VisitBindingList(memberInit.Bindings);

            if (newExpression != memberInit.NewExpression || bindings != memberInit.Bindings)
                return Expression.MemberInit(newExpression, bindings);

            return memberInit;
        }

        protected virtual ExpressionResolution VisitListInit(ListInitExpression listInit)
        {
            var newExpression = this.VisitNew(listInit.NewExpression);
            var initializers = this.VisitElementInitializerList(listInit.Initializers);

            if (newExpression != listInit.NewExpression || initializers != listInit.Initializers)
                return Expression.ListInit(newExpression, initializers);

            return listInit;
        }

        protected virtual ExpressionResolution VisitNewArray(NewArrayExpression newArray)
        {
            var expressions = this.VisitExpressionList(newArray.Expressions);

            if (expressions != newArray.Expressions)
            {
                if (newArray.NodeType == ExpressionType.NewArrayInit)
                    return Expression.NewArrayInit(newArray.Type.GetElementType(), expressions);
                else
                    return Expression.NewArrayBounds(newArray.Type.GetElementType(), expressions);
            }

            return newArray;
        }

        protected virtual ExpressionResolution VisitInvocation(InvocationExpression invocation)
        {
            var invocationArguments = this.VisitExpressionList(invocation.Arguments);
            var expression = this.Visit(invocation.Expression);

            if (invocationArguments != invocation.Arguments || expression != invocation.Expression)
                return Expression.Invoke(expression, invocationArguments);

            return invocation;
        }
        */

        private ExpressionResolution GetNamesAndAliases(MemberExpression memberExpression)
        {
            var expressionResolution = new ExpressionResolution
            {
                TableAlias = GetTableAlias(((RelmTable?)memberExpression.Expression?.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName),
                FieldName = memberExpression.Member.Name
            };

            expressionResolution.ParameterName = GenerateParameterName(expressionResolution);

            if (string.IsNullOrWhiteSpace(expressionResolution.TableAlias))
                throw new TypeAccessException($"Could not find 'RelmTable' custom attribute on type: [{memberExpression.Expression?.Type.FullName}]");

            //return new Tuple<string, string, string>(fieldName, parameterName, currentAlias);
            return expressionResolution;
        }

        private string GenerateParameterName(ExpressionResolution expressionResolution)
        {
            var duplicateCount = 0;
            var parameterName = $"@_{expressionResolution.FieldName}_";

            QueryParameters ??= [];
            while (QueryParameters.ContainsKey($"{parameterName}{++duplicateCount}_")) ;

            parameterName = $"{parameterName}{duplicateCount}_";

            if (QueryParameters.ContainsKey(parameterName))
                throw new AccessViolationException($"Key {parameterName} already exists.");

            return parameterName;
        }

        private string? GetTableAlias(string? PropertyName)
        {
            if (string.IsNullOrWhiteSpace(PropertyName))
                return null;

            if (_usedTableAliases.TryGetValue(PropertyName, out string? existingTableName))
                return existingTableName;

            var aliasCount = _usedTableAliases.Count;
            var currentAlias = string.Concat(Enumerable.Repeat(((char)((aliasCount % 26) + 97)).ToString(), (int)(aliasCount / 26.0) + 1));

            if (_underscoreProperties == null || !_underscoreProperties.TryGetValue(PropertyName, out string? newTableName))
                throw new KeyNotFoundException($"Could not find property '{PropertyName}' in any of the visited expression types. Ensure that the property exists and is properly decorated with 'RelmColumn' attribute if needed.");

            _usedTableAliases.Add(newTableName, currentAlias);

            return string.Empty;
        }

        private object? ResolveParameter(Expression resolvableExpression, string parameterName, bool asStringValue = false)
        {
            var parameterValue = ExpressionUtilities.GetValue(resolvableExpression);

            if (asStringValue)
                parameterValue = parameterValue.ToString();

            QueryParameters ??= [];
            if (!QueryParameters.ContainsKey(parameterName))
                QueryParameters.Add(parameterName, null);

            QueryParameters[parameterName] = resolvableExpression.Type == typeof(bool)
                ? (((bool?)parameterValue ?? false) ? 1 : 0)
                : parameterValue;

            return parameterValue;
        }
    }
}
