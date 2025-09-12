using SimpleRelm.Attributes;
using SimpleRelm.Interfaces;
using SimpleRelm.Models;
using SimpleRelm.RelmInternal.Helpers.Operations;
using CoreRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.RelmInternal.Helpers.Operations.ExpressionEvaluatorTests
{
    public class LimitExpression_Tester
    {
        private readonly ExpressionEvaluator evaluator;
        private readonly Dictionary<string, object> queryParameters;
        private Expression<Func<ComplexTestModel, object>>? predicate;

        public LimitExpression_Tester()
        {
            var tableName = typeof(ComplexTestModel).GetCustomAttribute<RelmTable>(false)?.TableName ?? throw new ArgumentNullException();
            var underscoreProperties = DataNamingHelper.GetUnderscoreProperties<ComplexTestModel>(true, false).ToDictionary(x => x.Value.Item1, x => x.Key);

            evaluator = new ExpressionEvaluator(tableName, underscoreProperties, UsedTableAliases: new Dictionary<string, string> { [tableName] = "a" });

            queryParameters = new();
        }

        [Fact]
        public void TestLimitQuery_SingleOperand()
        {
            // Arrange
            var limitCount = 1;

            // Act
            var result = evaluator.EvaluateLimit(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                ExpressionEvaluator.Command.GroupBy,
                new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Limit, Expression.Constant(limitCount, limitCount.GetType())) }));

            // Assert
            Assert.Equal(" LIMIT 1 ", result);
        }
    }
}
