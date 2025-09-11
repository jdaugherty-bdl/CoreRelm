using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.CustomVisitors
{
    internal class CustomMethodCallVisitor : ExpressionVisitor
    {
        public bool FoundRelevantMethodCall { get; private set; } = false;

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // Check if the method call is of a type that requires modification.
            // For example, check the method name, parameters, etc.
            //if (/* condition to identify relevant method calls */)
            {
                FoundRelevantMethodCall = true;
            }

            return base.VisitMethodCall(node);
        }
    }
}
