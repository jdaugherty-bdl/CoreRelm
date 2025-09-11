using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Expressions
{
    internal class VisitExpressions : ExpressionVisitor
    {
        internal override System.Linq.Expressions.Expression Visit(System.Linq.Expressions.Expression expression)
        {
            return base.Visit(expression);
        }
    }
}
