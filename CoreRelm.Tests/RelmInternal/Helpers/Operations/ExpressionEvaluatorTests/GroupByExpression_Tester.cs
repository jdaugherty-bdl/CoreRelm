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

namespace CoreRelm.Tests.RelmInternal.Helpers.Operations.ExpressionEvaluatorTests
{
    public class GroupByExpression_Tester
    {
        private readonly ExpressionEvaluator evaluator;
        private Expression<Func<ComplexTestModel, object?>>? predicate;

        public GroupByExpression_Tester()
        {
            var tableName = typeof(ComplexTestModel).GetCustomAttribute<RelmTable>(false)?.TableName ?? throw new ArgumentNullException();
            var underscoreProperties = DataNamingHelper.GetUnderscoreProperties<ComplexTestModel>(true, false).ToDictionary(x => x.Value.Item1, x => x.Key);

            evaluator = new ExpressionEvaluator(tableName, underscoreProperties, UsedTableAliases: new Dictionary<string, string> { [tableName] = "a" });
        }

        [Fact]
        public void TestGroupByQuery_SingleOperand()
        {
            // Arrange
            predicate = x => x.Id;

            // Act
            var result = evaluator.EvaluateGroupBy(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand?>>(
                ExpressionEvaluator.Command.GroupBy, 
                [new RelmExecutionCommand(ExpressionEvaluator.Command.GroupBy, predicate.Body)]));

            // Assert
            Assert.Equal("  GROUP BY a.`Id` ", result);
        }

        [Fact]
        public void TestGroupByQuery_PropertyArray_2Elements()
        {
            // Arrange
            predicate = x => new object?[] { x.Id, x.InternalId };

            // Act
            var result = evaluator.EvaluateGroupBy(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand?>>(
                ExpressionEvaluator.Command.GroupBy,
                [new RelmExecutionCommand(ExpressionEvaluator.Command.GroupBy, predicate.Body)]));

            // Assert
            Assert.Equal("   GROUP BY a.`Id` , a.`InternalId` ", result);
        }

        [Fact]
        public void TestGroupByQuery_PropertyArray_3Elements()
        {
            // Arrange
            predicate = x => new object?[] { x.Id, x.InternalId, x.TestColumnInternalId };

            // Act
            var result = evaluator.EvaluateGroupBy(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand?>>(
                ExpressionEvaluator.Command.GroupBy,
                [new RelmExecutionCommand(ExpressionEvaluator.Command.GroupBy, predicate.Body)]));

            // Assert
            Assert.Equal("   GROUP BY a.`Id` , a.`InternalId` , a.`test_column_InternalId` ", result);
        }

        [Fact]
        public void TestGroupByQuery_DoubleOperand()
        {
            // Arrange
            predicate = x => x.Id;
            Expression<Func<ComplexTestModel, object>>? predicate2 = x => x.InternalId;

            // Act
            var result = evaluator.EvaluateGroupBy(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand?>>(
                ExpressionEvaluator.Command.GroupBy,
                [ 
                    new RelmExecutionCommand(ExpressionEvaluator.Command.GroupBy, predicate.Body), 
                    new RelmExecutionCommand(ExpressionEvaluator.Command.GroupBy, predicate2.Body) 
                ]));

            // Assert
            Assert.Equal("  GROUP BY a.`Id` , a.`InternalId` ", result);
        }

        [Fact]
        public void TestGroupByQuery_TripleOperand()
        {
            // Arrange
            predicate = x => x.Id;
            Expression<Func<ComplexTestModel, object?>>? predicate2 = x => x.InternalId;
            Expression<Func<ComplexTestModel, object?>>? predicate3 = x => x.TestColumnInternalId;

            // Act
            var result = evaluator.EvaluateGroupBy(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand?>>(
                ExpressionEvaluator.Command.GroupBy,
                [
                    new RelmExecutionCommand(ExpressionEvaluator.Command.GroupBy, predicate.Body),
                    new RelmExecutionCommand(ExpressionEvaluator.Command.GroupBy, predicate2.Body),
                    new RelmExecutionCommand(ExpressionEvaluator.Command.GroupBy, predicate3.Body)
                ]));

            // Assert
            Assert.Equal("  GROUP BY a.`Id` , a.`InternalId` , a.`test_column_InternalId` ", result);
        }

        [Fact]
        public void TestGroupByQuery_TwoGroupBys_SingleOperand()
        {
            // Arrange
            predicate = x => x.Id;
            Expression<Func<ComplexTestModel, object>> predicate2 = x => x.InternalId;

            // Act
            var result = evaluator.EvaluateGroupBy(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand?>>(
                ExpressionEvaluator.Command.GroupBy,
                [new RelmExecutionCommand(ExpressionEvaluator.Command.GroupBy, predicate.Body)]));

            result += evaluator.EvaluateGroupBy(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand?>>(
                ExpressionEvaluator.Command.GroupBy,
                [new RelmExecutionCommand(ExpressionEvaluator.Command.GroupBy, predicate2.Body)]));

            // Assert
            Assert.Equal("  GROUP BY a.`Id`  , a.`InternalId` ", result);
        }
    }
}
