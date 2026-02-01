using CoreRelm.RelmInternal.Helpers.CustomVisitors;
using CoreRelm.RelmInternal.Helpers.Expressions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Utilities
{
    internal static class ExpressionUtilities
    {
        public static object GetValue(Expression expression)
        {
            return getValue(expression, true);
            //return GetValue(expression, null, true);
        }

        public static object GetValueWithArguments(Expression expression, List<object> argumentValues)
        {
            //return GetValue(expression, argumentValues, true);
            return getValue(expression, true, argumentValues);
        }

        public static object GetValueWithoutCompiling(Expression expression)
        {
            return getValue(expression, false);
            //return GetValue(expression, null, false);
        }

        public static object GetValueUsingCompile(Expression expression, List<object> argumentValues)
        {
            LambdaExpression lambdaExpression;
            if (expression is LambdaExpression le)
            {
                lambdaExpression = le;
            }
            else if (expression is ParameterExpression pe)
            {
                // Wrap the parameter as a lambda using the same ParameterExpression instance
                lambdaExpression = Expression.Lambda(expression, pe);
            }
            else
            {
                // If the expression has parameters, extract them from the tree instead of recreating them
                var parameters = new ParameterExtractor().Extract(expression); // implement to walk the tree
                lambdaExpression = parameters.Any()
                    ? Expression.Lambda(expression, parameters)
                    : Expression.Lambda(expression);
            }

            var dele = lambdaExpression.Compile();
            if (argumentValues == null || argumentValues.Count == 0)
                return dele.DynamicInvoke();

            // Execute over jagged sources instead of collapsing to the first inner array
            if (TryExecuteOverJagged(lambdaExpression, argumentValues, dele, out var jaggedResult))
                return jaggedResult;

            var mappedArgs = MapArguments(lambdaExpression, argumentValues);
            return dele.DynamicInvoke(mappedArgs);
        }

        // helper to execute a single-parameter lambda over a jagged source
        private static bool TryExecuteOverJagged(LambdaExpression lambdaExpression, List<object> argumentValues, Delegate dele, out object result)
        {
            result = null;

            // We only handle single-parameter lambdas of array type: T[]
            if (lambdaExpression.Parameters.Count != 1)
                return false;

            var paramType = lambdaExpression.Parameters[0].Type;
            if (!paramType.IsArray)
                return false;

            // Detect jagged inputs: T[][] or IEnumerable<T[]>
            var jaggedArrayType = paramType.MakeArrayType();              // e.g. string[][]. Note: paramType is string[]
            var enumerableOfArraysType = typeof(IEnumerable<>).MakeGenericType(paramType);

            var source = argumentValues != null && argumentValues.Count > 0 ? argumentValues[0] : null;
            IEnumerable<object> innerArrays = null;

            if (source == null)
                return false;

            if (jaggedArrayType.IsInstanceOfType(source))
            {
                var array = (Array)source;
                innerArrays = array.Cast<object>();
            }
            else if (enumerableOfArraysType.IsInstanceOfType(source))
            {
                innerArrays = ((IEnumerable)source).Cast<object>();
            }
            else
            {
                return false;
            }

            // Invoke the delegate for each inner array
            var outputs = new List<object>();
            foreach (var inner in innerArrays)
            {
                outputs.Add(dele.DynamicInvoke(new[] { inner }));
            }

            result = outputs;
            return true;
        }

        /*
        public static object GetValueUsingCompile(Expression expression, List<object> argumentValues)
        {
            //return GetValue(expression, argumentValues, false, true);
            return getValue(expression, true, argumentValues);
        }
        */
        private static object[] MapArguments(LambdaExpression lambda, List<object> args)
        {
            var targetParams = lambda.Parameters;
            var result = new object[targetParams.Count];

            // If arg count matches, still coerce each arg; otherwise try to best-fit pull from the provided list.
            for (int i = 0; i < targetParams.Count; i++)
            {
                var pType = targetParams[i].Type;
                var source = i < args.Count ? args[i] : null;

                result[i] = CoerceArgument(source, pType, args);
            }

            return result;
        }

        private static object CoerceArgument(object source, Type targetType, List<object> pool)
        {
            if (source == null)
            {
                // Try to find a candidate from pool that matches targetType
                var candidate = pool.FirstOrDefault(o => IsAssignableTo(o, targetType));
                if (candidate != null) return candidate;
                return null;
            }

            // NEW: unwrap lists/collections by selecting a matching element
            if (source is IEnumerable enumerable && targetType != typeof(string))
            {
                foreach (var item in enumerable.Cast<object>())
                {
                    if (IsAssignableTo(item, targetType))
                        return item;
                    else if (item != null)
                    {
                        // Try coercion on each item
                        var coercedItem = CoerceArgument(item, targetType, pool);
                        if (IsAssignableTo(coercedItem, targetType))
                            return coercedItem;
                    }
                }
            }

            // Direct assignable
            if (IsAssignableTo(source, targetType))
                return source;

            // Unwrap Delegate: try invoke without parameters if return type matches
            if (source is Delegate del)
            {
                var method = del.Method;
                if (method.GetParameters().Length == 0 && IsAssignableTo(method.ReturnType, targetType))
                    return del.DynamicInvoke();
            }

            // If source is IEnumerable of elementType, and target is elementType[], take first or ToArray
            if (TryCoerceEnumerableToArray(source, targetType, out var arrayCoerced))
                return arrayCoerced;

            // If source is jagged array and target is single-dimensional array element, take first
            if (TryCoerceJaggedFirst(source, targetType, out var jaggedCoerced))
                return jaggedCoerced;

            // If target is string and source is not, use ToString
            if (targetType == typeof(string) && source != null)
                return source.ToString();

            // Last resort: try ChangeType for primitives
            try
            {
                if (source is IConvertible && typeof(IConvertible).IsAssignableFrom(targetType))
                    return Convert.ChangeType(source, targetType);
            }
            catch { /* ignore */ }

            // No safe coercion; return original to let DynamicInvoke surface a clear error
            return source;
        }

        private static bool IsAssignableTo(object value, Type targetType)
        {
            if (value == null) return !targetType.IsValueType || (Nullable.GetUnderlyingType(targetType) != null);
            return targetType.IsInstanceOfType(value);
        }

        private static bool TryCoerceEnumerableToArray(object source, Type targetType, out object result)
        {
            result = null;
            var elemType = targetType.IsArray ? targetType.GetElementType() : null;
            if (elemType == null) return false;

            // IEnumerable<elemType> -> elemType[] (use FirstOrDefault if the lambda expects elemType and we have IEnumerable<elemType>)
            var ienumOfElem = typeof(IEnumerable<>).MakeGenericType(elemType);
            if (ienumOfElem.IsInstanceOfType(source))
            {
                var enumerable = (IEnumerable)source;
                // If target is elemType[], return ToArray; else if target is elemType, return FirstOrDefault
                if (targetType.IsArray)
                {
                    var toArray = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))
                        .MakeGenericMethod(elemType);
                    result = toArray.Invoke(null, new object[] { source });
                    return true;
                }
            }

            return false;
        }

        private static bool TryCoerceJaggedFirst(object source, Type targetType, out object result)
        {
            result = null;
            if (!targetType.IsArray) return false;

            var elemType = targetType.GetElementType();

            // Handle string[][] -> string[] (take FirstOrDefault)
            //var jaggedType = elemType.MakeArrayType();
            var jaggedType = targetType.MakeArrayType();
            if (jaggedType.IsInstanceOfType(source))
            {
                var array = (Array)source;
                result = array.Length > 0 ? array.GetValue(0) : null;
                return true;
            }

            // Handle IEnumerable<string[]> -> string[] (take FirstOrDefault)
            var ienumOfElem = typeof(IEnumerable<>).MakeGenericType(elemType);
            if (ienumOfElem.IsInstanceOfType(source))
            {
                var firstOrDefault = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .First(m => m.Name == nameof(Enumerable.FirstOrDefault) && m.GetParameters().Length == 1)
                    .MakeGenericMethod(elemType);
                result = firstOrDefault.Invoke(null, new object[] { source });
                return true;
            }

            return false;
        }

        public static object GetValue(Expression expression, List<object> argumentValues, bool allowCompile, bool useDynamicInvoke = false)
        {
            // Implementation that uses argumentValues

            // The logic here depends on how you need to use argumentValues.
            // For instance, if exp is a lambda expression that needs these values,
            // you might modify the lambda to incorporate them.

            /*
            // Example:
            if (expression is LambdaExpression lambda)
            {
                // Modify the lambda expression or its invocation to use argumentValues
                // ...

                // Then compile and invoke the lambda
                var compiledLambda = lambda.Compile();
                // Assuming the lambda takes argumentValues as parameters
                return compiledLambda.DynamicInvoke(argumentValues.ToArray());
            }
            */

            // Handle other types of expressions as before
            // ...

            if (useDynamicInvoke)
            {
                //var currentExpression = (Expression.PropertyOrField(expression, ((ParameterExpression)expression).Name));
                var lambdaExpression = Expression.Lambda(expression);
                //var lambdaExpression = argumentValues == null ? Expression.Lambda(expression) : Expression.Lambda(expression, argumentValues.ToArray());
                var compiledDelegate = lambdaExpression.Compile();

                if (argumentValues != null)
                    return compiledDelegate.DynamicInvoke(argumentValues.ToArray());
                else
                    return compiledDelegate.DynamicInvoke();
            }
        
            return getValue(expression, allowCompile, argumentValues);
        }

        private static object getValue(Expression expression, bool allowCompile, List<object> argumentValues = null)
        {
            if (expression == null)
            {
                return null;
            }

            if (expression is ConstantExpression)
            {
                var constantExpression = (ConstantExpression)expression;
                return getValue(constantExpression);
            }

            if (expression is MemberExpression)
            {
                var memberExpression = (MemberExpression)expression;
                return getValue(memberExpression, allowCompile);
            }

            if (expression is MethodCallExpression)
            {
                var methodCallExpression = (MethodCallExpression)expression;
                return getValue(methodCallExpression, allowCompile, argumentValues);
            }

            if (allowCompile)
            {
                //return GetValueUsingCompile(expression);
                return GetValueUsingCompile(expression, argumentValues);
            }

            throw new Exception("Couldn't evaluate Expression without compiling: " + expression);
        }

        private static object getValue(ConstantExpression constantExpression)
        {
            return constantExpression.Value;
        }

        private static object getValue(MemberExpression memberExpression, bool allowCompile)
        {
            var value = getValue(memberExpression.Expression, allowCompile, null);

            var member = memberExpression.Member;
            if (member is FieldInfo)
            {
                var fieldInfo = (FieldInfo)member;
                return fieldInfo.GetValue(value);
            }

            if (member is PropertyInfo)
            {
                var propertyInfo = (PropertyInfo)member;

                try
                {
                    return propertyInfo.GetValue(value);
                }
                catch (TargetInvocationException e)
                {
                    throw e.InnerException;
                }
            }

            throw new Exception("Unknown member type: " + member.GetType());
        }

        private static object getValue(MethodCallExpression methodCallExpression, bool allowCompile, List<object> argumentValues)
        {
            var paras = getArray(methodCallExpression.Arguments, true, argumentValues);
            var obj = getValue(methodCallExpression.Object, allowCompile, argumentValues);

            try
            {
                return methodCallExpression.Method.Invoke(obj, paras);
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }

        private static object[] getArray(IEnumerable<Expression> expressions, bool allowCompile, List<object> argumentValues)
        {
            var list = new List<object>();
            foreach (var expression in expressions)
            {
                var value = getValue(expression, allowCompile, argumentValues);
                list.Add(value);
            }

            return list.ToArray();
        }

        public static MemberExpression GetReferencedMember(Tuple<Expression, ICollection<ParameterExpression>> command)
        {
            return GetReferencedMember((MethodCallExpression)command.Item1, command.Item2.FirstOrDefault());
        }

        public static MemberExpression GetReferencedMember(MethodCallExpression methodCall, ParameterExpression parameter)
        {
            var referencedMember = methodCall.Arguments.LastOrDefault(x => x is MemberExpression) as MemberExpression;

            if ((referencedMember?.Expression?.NodeType ?? ExpressionType.Not) != ExpressionType.Parameter)
            {
                foreach (var arg in methodCall.Arguments)
                {
                    if (arg is MemberExpression memberExpression)
                    {
                        if (memberExpression.Expression == parameter)
                        {
                            referencedMember = memberExpression;

                            break;
                        }

                        if (memberExpression.Expression is ConstantExpression constantExpression)
                        {
                            var ddd = GetValue(constantExpression);
                        }
                    }
                    else if (arg is MethodCallExpression methodCallExpression)
                    {
                        referencedMember = GetReferencedMember(methodCallExpression, parameter);

                        if (referencedMember != null)
                            break;
                    }
                    else if (arg is LambdaExpression lambdaExpression)
                    {
                        if (lambdaExpression.Body is MethodCallExpression method)
                        {
                            referencedMember = GetReferencedMember(method, parameter);

                            if (referencedMember != null)
                                break;
                        }
                        else if (lambdaExpression.Body is BinaryExpression binaryExpression)
                        {
                        }
                    }
                }
            }

            return referencedMember;
        }
        /*
        public static List<MemberExpression> GetReferencedMember(Tuple<Expression, ICollection<ParameterExpression>> command)
        {
            return GetReferencedMember((MethodCallExpression)command.Item1, command.Item2.FirstOrDefault());
        }

        public static List<MemberExpression> GetReferencedMember(MethodCallExpression methodCall, ParameterExpression parameter)
        {
            var referencedMembers = methodCall.Arguments.Where(x => x is MemberExpression).Cast<MemberExpression>().ToList();

            if (referencedMembers.All(x => (x?.Expression?.NodeType ?? ExpressionType.Not) == ExpressionType.Parameter))
                return referencedMembers;

            foreach (var arg in methodCall.Arguments)
            {
                if (arg is MemberExpression memberExpression)
                {
                    if (memberExpression.Expression == parameter)
                    {
                        referencedMembers.Add(memberExpression);

                        //break;
                    }
                    else if (memberExpression.Expression is ConstantExpression constantExpression)
                    {
                        var ddd = GetValue(constantExpression);
                    }
                }
                else if (arg is MethodCallExpression methodCallExpression)
                {
                    referencedMembers = GetReferencedMember(methodCallExpression, parameter);

                    //if (referencedMembers != null)
                        //break;
                }
                else if (arg is LambdaExpression lambdaExpression)
                {
                    if (lambdaExpression.Body is MethodCallExpression method)
                    {
                        referencedMembers = GetReferencedMember(method, parameter);

                        //if (referencedMembers != null)
                            //break;
                    }
                    else if (lambdaExpression.Body is BinaryExpression binaryExpression)
                    {
                    }
                }
            }

            return referencedMembers;
        }
        */

        public static List<object> GetReferencedValues(MethodCallExpression methodCall)
        {
            var argumentValues = new List<object>();

            foreach (var arg in methodCall.Arguments)
            {
                if (arg is LambdaExpression lambdaExpression && lambdaExpression.Body is MethodCallExpression method)
                {
                    // Modify the lambda expression to incorporate the values of evaluatedArgs as context
                    var modifiedLambdaExpression = ModifyLambdaExpression(lambdaExpression, argumentValues);

                    var lambdaExpressionValue = GetValue(modifiedLambdaExpression.Body);
                    //var lambdaExpressionValue = GetValue(modifiedLambdaExpression.Body, argumentValues);

                    argumentValues.Add(lambdaExpressionValue);
                }
                else
                {
                    var exp = ResolveMemberExpression(arg);
                    var val = GetValue(exp);
                    //var val = GetValue(exp, argumentValues); // Pass evaluatedArgs in case the value depends on them

                    argumentValues.Add(val);
                }
            }

            return argumentValues;
        }

        private static MemberExpression ResolveMemberExpression(Expression expression)
        {
            if (expression is MemberExpression memberExpression)
                return memberExpression;
            else if (expression is UnaryExpression unaryExpression)
                return (MemberExpression)unaryExpression.Operand;
            else
                throw new NotSupportedException();
        }

        public static LambdaExpression ModifyLambdaExpression(LambdaExpression lambdaExpression, List<object> evaluatedArgs)
        {
            // Check if the lambda expression needs to be modified based on evaluatedArgs
            // This could involve checking for specific patterns or types of references in the expression body
            // If no modifications are needed, return the original lambda expression
            if (!NeedsModification(lambdaExpression, evaluatedArgs))
            {
                return lambdaExpression;
            }

            // Create a new parameter list if necessary
            var newParameters = lambdaExpression.Parameters.ToList();
            // Potentially add new parameters based on evaluatedArgs

            // Modify the body of the lambda expression
            // This is the complex part and depends on how the lambda expression should use evaluatedArgs
            var newBody = ModifyExpressionBody(lambdaExpression.Body, evaluatedArgs, newParameters);

            // Create and return the new lambda expression
            return Expression.Lambda(newBody, newParameters);
        }

        private static Expression ModifyExpressionBody(Expression body, List<object> evaluatedArgs, List<ParameterExpression> newParameters)
        {
            // Implement logic to replace or supplement parts of the body with evaluatedArgs
            // This might involve visiting sub-expressions and replacing them

            // For example:
            var visitor = new CustomParameterVisitor(evaluatedArgs, newParameters);
            return visitor.Visit(body);
        }

        private static bool NeedsModification(LambdaExpression lambdaExpression, List<object> evaluatedArgs)
        {
            // Example condition: check if the lambda expression contains a method call
            // that requires a value from evaluatedArgs
            var methodCallVisitor = new CustomMethodCallVisitor();
            methodCallVisitor.Visit(lambdaExpression.Body);

            return methodCallVisitor.FoundRelevantMethodCall;
        }

        public static string GetSqlOperator(ExpressionType expressionType)
        {
            switch (expressionType)
            {
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.NotEqual:
                    return "<>";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.AndAlso:
                    return "AND";
                case ExpressionType.OrElse:
                    return "OR";
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    return "+";
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return "-";
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return "*";
                case ExpressionType.Divide:
                    return "/";
                case ExpressionType.Modulo:
                    return "%";
                default:
                    throw new NotSupportedException($"The expression type '{expressionType}' is not supported.");
            }
        }
    }
}
