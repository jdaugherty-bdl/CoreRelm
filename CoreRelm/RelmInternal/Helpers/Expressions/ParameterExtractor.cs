using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Expressions
{
    // Simple helper if you don't want a full extractor:
    internal sealed class ParameterExtractor : ExpressionVisitor
    {
        private readonly HashSet<ParameterExpression> _params = new HashSet<ParameterExpression>();

        public IReadOnlyList<ParameterExpression> Extract(Expression e)
        {
            Visit(e);
            return _params.ToList();
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            _params.Add(node);
            return base.VisitParameter(node);
        }
    }
}
