using CoreRelm.Models;
using CoreRelm.RelmInternal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Commands;
using static CoreRelm.RelmInternal.Helpers.Operations.ExpressionEvaluator;

namespace CoreRelm.Interfaces
{
    public interface IRelmExecutionCommand
    {
        Command InitialCommand { get; }
        Expression InitialExpression { get; }
        int AdditionalCommandCount { get; }

        RelmExecutionCommand AddAdditionalCommand(Command command, Expression expression);
        List<RelmExecutionCommand> GetAdditionalCommands();
        ForeignKeyNavigationOptions GetForeignKeyNavigationOptions<T>(ICollection<T> _items);
    }
}
