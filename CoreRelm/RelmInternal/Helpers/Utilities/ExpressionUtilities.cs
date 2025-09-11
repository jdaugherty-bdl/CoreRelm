using CoreRelm.RelmInternal.Helpers.CustomVisitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Utilities
{
    public static class ExpressionUtilities
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

        public static object GetValueUsingCompile(Expression expression)
        {
            var lambdaExpression = Expression.Lambda(expression);
            var dele = lambdaExpression.Compile();
            return dele.DynamicInvoke();
            /*
            return GetValue(expression, null, false, true);
            */
        }

        /*
        public static object GetValueUsingCompile(Expression expression, List<object> argumentValues)
        {
            //return GetValue(expression, argumentValues, false, true);
            return getValue(expression, true, argumentValues);
        }
        */

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
                return GetValueUsingCompile(expression);
                //return GetValueUsingCompile(expression, argumentValues);
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
            //if (!(command.Item1 is MethodCallExpression methodCall))
            //    return null;

            //return GetReferencedMember(methodCall, command.Item2.FirstOrDefault());
            return GetReferencedMember((MethodCallExpression)command.Item1, command.Item2.FirstOrDefault());
        }

        public static MemberExpression GetReferencedMember(MethodCallExpression methodCall, ParameterExpression parameter)
        {
            var referencedMember = methodCall.Arguments.LastOrDefault(x => x is MemberExpression) as MemberExpression;

            if ((referencedMember?.Expression?.NodeType ?? ExpressionType.Not) != ExpressionType.Parameter)
            {
                foreach (var arg in methodCall.Arguments)
                {
                    if (arg is MemberExpression memberExpression && memberExpression.Expression == parameter)
                    {
                        referencedMember = memberExpression;

                        break;
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
                    }
                }
            }

            return referencedMember;
        }

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
    }
}
