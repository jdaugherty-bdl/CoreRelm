using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Expressions
{
    // from: https://learn.microsoft.com/en-us/previous-versions/visualstudio/visual-studio-2008/bb882521(v=vs.90)?redirectedfrom=MSDN
    internal abstract class ExpressionVisitor
    {
        protected ExpressionVisitor() { }

        internal virtual Expression Visit(Expression expression)
        {
            if (expression == null)
                return expression;

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
                    return this.VisitUnary((UnaryExpression)expression);
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
                    return this.VisitBinary((BinaryExpression)expression);
                case ExpressionType.TypeIs:
                    return this.VisitTypeIs((TypeBinaryExpression)expression);
                case ExpressionType.Conditional:
                    return this.VisitConditional((ConditionalExpression)expression);
                case ExpressionType.Constant:
                    return this.VisitConstant((ConstantExpression)expression);
                case ExpressionType.Parameter:
                    return this.VisitParameter((ParameterExpression)expression);
                case ExpressionType.MemberAccess:
                    return this.VisitMemberAccess((MemberExpression)expression);
                case ExpressionType.Call:
                    return this.VisitMethodCall((MethodCallExpression)expression);
                case ExpressionType.Lambda:
                    return this.VisitLambda((LambdaExpression)expression);
                case ExpressionType.New:
                    return this.VisitNew((NewExpression)expression);
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return this.VisitNewArray((NewArrayExpression)expression);
                case ExpressionType.Invoke:
                    return this.VisitInvocation((InvocationExpression)expression);
                case ExpressionType.MemberInit:
                    return this.VisitMemberInit((MemberInitExpression)expression);
                case ExpressionType.ListInit:
                    return this.VisitListInit((ListInitExpression)expression);
                default:
                    throw new Exception($"Unhandled expression type: '{expression.NodeType}'");
            }
        }

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

        protected virtual Expression VisitUnary(UnaryExpression unary)
        {
            var operand = this.Visit(unary.Operand);

            if (operand != unary.Operand)
                return Expression.MakeUnary(unary.NodeType, operand, unary.Type, unary.Method);

            return unary;
        }

        protected virtual Expression VisitBinary(BinaryExpression binary)
        {
            var left = this.Visit(binary.Left);
            var right = this.Visit(binary.Right);
            var conversion = this.Visit(binary.Conversion);

            if (left != binary.Left || right != binary.Right || conversion != binary.Conversion)
            {
                if (binary.NodeType == ExpressionType.Coalesce && binary.Conversion != null)
                    return Expression.Coalesce(left, right, (LambdaExpression)conversion);
                else
                    return Expression.MakeBinary(binary.NodeType, left, right, binary.IsLiftedToNull, binary.Method);
            }

            return binary;
        }

        protected virtual Expression VisitTypeIs(TypeBinaryExpression typeBinary)
        {
            var expr = this.Visit(typeBinary.Expression);

            if (expr != typeBinary.Expression)
                return Expression.TypeIs(expr, typeBinary.TypeOperand);

            return typeBinary;
        }

        protected virtual Expression VisitConstant(ConstantExpression constant)
        {
            return constant;
        }

        protected virtual Expression VisitConditional(ConditionalExpression conditional)
        {
            var test = this.Visit(conditional.Test);
            var ifTrue = this.Visit(conditional.IfTrue);
            var ifFalse = this.Visit(conditional.IfFalse);

            if (test != conditional.Test || ifTrue != conditional.IfTrue || ifFalse != conditional.IfFalse)
                return Expression.Condition(test, ifTrue, ifFalse);

            return conditional;
        }

        protected virtual Expression VisitParameter(ParameterExpression parameter)
        {
            return parameter;
        }

        protected virtual Expression VisitMemberAccess(MemberExpression member)
        {
            var exp = this.Visit(member.Expression);

            if (exp != member.Expression)
                return Expression.MakeMemberAccess(exp, member.Member);

            return member;
        }

        protected virtual Expression VisitMethodCall(MethodCallExpression methodCall)
        {
            var obj = this.Visit(methodCall.Object);
            var args = this.VisitExpressionList(methodCall.Arguments);

            if (obj != methodCall.Object || args != methodCall.Arguments)
                return Expression.Call(obj, methodCall.Method, args);

            return methodCall;
        }

        protected virtual ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
        {
            List<Expression> list = null;
            var originalCount = original.Count;

            for (var i = 0; i < originalCount; i++)
            {
                var currentExpression = this.Visit(original[i]);

                if (list != null)
                    list.Add(currentExpression);
                else if (currentExpression != original[i])
                    list = new List<Expression>(originalCount);

                for (var j = 0; j < i; j++)
                {
                    list.Add(original[j]);
                }

                list.Add(currentExpression);
            }

            if (list != null)
                return list.AsReadOnly();

            return original;
        }

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

        protected virtual Expression VisitLambda(LambdaExpression lambda)
        {
            var body = this.Visit(lambda.Body);

            if (body != lambda.Body)
                return Expression.Lambda(lambda.Type, body, lambda.Parameters);

            return lambda;
        }

        protected virtual NewExpression VisitNew(NewExpression newExpression)
        {
            var newExpressionArguments = this.VisitExpressionList(newExpression.Arguments);

            if (newExpressionArguments != newExpression.Arguments)
            {
                if (newExpression.Members != null)
                    return Expression.New(newExpression.Constructor, newExpressionArguments, newExpression.Members);
                else
                    return Expression.New(newExpression.Constructor, newExpressionArguments);
            }

            return newExpression;
        }

        protected virtual Expression VisitMemberInit(MemberInitExpression memberInit)
        {
            var newExpression = this.VisitNew(memberInit.NewExpression);
            var bindings = this.VisitBindingList(memberInit.Bindings);

            if (newExpression != memberInit.NewExpression || bindings != memberInit.Bindings)
                return Expression.MemberInit(newExpression, bindings);

            return memberInit;
        }

        protected virtual Expression VisitListInit(ListInitExpression listInit)
        {
            var newExpression = this.VisitNew(listInit.NewExpression);
            var initializers = this.VisitElementInitializerList(listInit.Initializers);

            if (newExpression != listInit.NewExpression || initializers != listInit.Initializers)
                return Expression.ListInit(newExpression, initializers);

            return listInit;
        }

        protected virtual Expression VisitNewArray(NewArrayExpression newArray)
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

        protected virtual Expression VisitInvocation(InvocationExpression invocation)
        {
            var invocationArguments = this.VisitExpressionList(invocation.Arguments);
            var expression = this.Visit(invocation.Expression);

            if (invocationArguments != invocation.Arguments || expression != invocation.Expression)
                return Expression.Invoke(expression, invocationArguments);

            return invocation;
        }
    }
}
