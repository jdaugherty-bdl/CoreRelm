using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.RelmInternal.Helpers.Operations;
using CoreRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Commands;

namespace CoreRelm.Tests.RelmInternal.Helpers.Operations.ExpressionEvaluatorTests
{
    public class LimitExpression_Tester
    {
        private readonly ExpressionEvaluator<ComplexTestModel> evaluator;
        private readonly Dictionary<string, object> queryParameters;
        private Expression<Func<ComplexTestModel, object>>? predicate;

        public LimitExpression_Tester()
        {
            var tableName = typeof(ComplexTestModel).GetCustomAttribute<RelmTable>(false)?.TableName ?? throw new ArgumentNullException();
            var underscoreProperties = DataNamingHelper.GetUnderscoreProperties<ComplexTestModel>(true, false).ToDictionary(x => x.Value.Item1, x => x.Key);

            evaluator = new ExpressionEvaluator<ComplexTestModel>(tableName, underscoreProperties, UsedTableAliases: new Dictionary<string, string> { [tableName] = "a" });

            queryParameters = new();
        }

        [Fact]
        public void TestLimitQuery_SingleOperand()
        {
            // Arrange
            var limitCount = 1;

            // Act
            var result = evaluator.EvaluateLimit(new KeyValuePair<Command, List<IRelmExecutionCommand>>(
                Command.Limit,
                new List<IRelmExecutionCommand> { new RelmExecutionCommand(Command.Limit, Expression.Constant(limitCount, limitCount.GetType())) }));

            // Assert
            Assert.Equal(" LIMIT 1 ", result);
        }

        [Fact]
        public void TestOffsetQuery_SingleOperand()
        {
            // Arrange
            var offsetCount = 1;

            // Act
            var result = evaluator.EvaluateOffset(new KeyValuePair<Command, List<IRelmExecutionCommand>>(
                Command.Offset,
                new List<IRelmExecutionCommand> { new RelmExecutionCommand(Command.Offset, Expression.Constant(offsetCount, offsetCount.GetType())) }));

            // Assert
            Assert.Equal(" OFFSET 1 ", result);
        }
    }
}
