using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.RelmInternal.Helpers.Operations.ExpressionEvaluator;

namespace CoreRelm.Interfaces
{
    public interface IRelmDataLoader<T>
    {
        Dictionary<Command, List<IRelmExecutionCommand>> LastCommandsExecuted { get; set; }

        ICollection<T> GetLoadData();
        int WriteData();
        IRelmExecutionCommand AddExpression(Command command, Expression expression);
        IRelmExecutionCommand AddSingleExpression(Command command, Expression expression);
        bool HasUnderscoreProperty(string PropertyKey);
    }
}
