using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.RelmInternal.Helpers.Operations;
using CoreRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Commands;

namespace CoreRelm.Tests.RelmInternal.Helpers.Operations.ExpressionEvaluatorTests
{
    public class OrderByExpressionTests
    {
        private readonly ExpressionEvaluator<ComplexTestModel> evaluator;
        private readonly Dictionary<string, object> queryParameters;
        private Expression<Func<ComplexTestModel, object>>? predicate;

        public OrderByExpressionTests()
        {
            var tableName = typeof(ComplexTestModel).GetCustomAttribute<RelmTable>(false)?.TableName ?? throw new ArgumentNullException();
            var underscoreProperties = DataNamingHelper.GetUnderscoreProperties<ComplexTestModel>(true, false).ToDictionary(x => x.Value.Item1, x => x.Key);

            evaluator = new ExpressionEvaluator<ComplexTestModel>(tableName, underscoreProperties, UsedTableAliases: new Dictionary<string, string> { [tableName] = "a" });

            queryParameters = new();
        }

        [Fact]
        public void TestOrderByQuery_Ascending()
        {
            // Arrange
            predicate = x => x.Id;

            // Act
            var result = evaluator.EvaluateOrderBy(new KeyValuePair<Command, List<IRelmExecutionCommand?>>(
                    Command.OrderBy, 
                    [new RelmExecutionCommand(Command.OrderBy, predicate.Body)])
                , false);

            // Assert
            Assert.Equal("  ORDER BY a.`Id`  ASC ", result);
        }

        [Fact]
        public void TestOrderByQuery_Descending()
        {
            // Arrange
            predicate = x => x.Id;

            // Act
            var result = evaluator.EvaluateOrderBy(new KeyValuePair<Command, List<IRelmExecutionCommand?>>(
                    Command.OrderBy, 
                    [new RelmExecutionCommand(Command.OrderBy, predicate.Body)])
                , true);

            // Assert
            Assert.Equal("  ORDER BY a.`Id`  DESC ", result);
        }

        [Fact]
        public void TestOrderByQuery_Ascending_PropertyArray_2Elements()
        {
            // Arrange
            predicate = x => new object?[] { x.Id, x.InternalId };

            // Act
            var result = evaluator.EvaluateOrderBy(new KeyValuePair<Command, List<IRelmExecutionCommand?>>(
                    Command.OrderBy, 
                    [new RelmExecutionCommand(Command.OrderBy, predicate.Body)])
                , false);

            // Assert
            Assert.Equal("   ORDER BY a.`Id`  ASC , a.`InternalId`  ASC ", result);
        }

        [Fact]
        public void TestOrderByQuery_Descending_PropertyArray_2Elements()
        {
            // Arrange
            predicate = x => new object?[] { x.Id, x.InternalId };

            // Act
            var result = evaluator.EvaluateOrderBy(new KeyValuePair<Command, List<IRelmExecutionCommand?>>(
                    Command.OrderBy, 
                    [new RelmExecutionCommand(Command.OrderBy, predicate.Body)])
                , true);

            // Assert
            Assert.Equal("   ORDER BY a.`Id`  DESC , a.`InternalId`  DESC ", result);
        }

        [Fact]
        public void TestOrderByQuery_Ascending_PropertyArray_3Elements()
        {
            // Arrange
            predicate = x => new object?[] { x.Id, x.InternalId, x.TestColumnInternalId };

            // Act
            var result = evaluator.EvaluateOrderBy(new KeyValuePair<Command, List<IRelmExecutionCommand?>>(
                    Command.OrderBy, 
                    [new RelmExecutionCommand(Command.OrderBy, predicate.Body)])
                , false);

            // Assert
            Assert.Equal("   ORDER BY a.`Id`  ASC , a.`InternalId`  ASC , a.`test_column_InternalId`  ASC ", result);
        }

        [Fact]
        public void TestOrderByQuery_Descending_PropertyArray_3Elements()
        {
            // Arrange
            predicate = x => new object?[] { x.Id, x.InternalId, x.TestColumnInternalId };

            // Act
            var result = evaluator.EvaluateOrderBy(new KeyValuePair<Command, List<IRelmExecutionCommand?>>(
                    Command.OrderBy, 
                    [new RelmExecutionCommand(Command.OrderBy, predicate.Body)])
                , true);

            // Assert
            Assert.Equal("   ORDER BY a.`Id`  DESC , a.`InternalId`  DESC , a.`test_column_InternalId`  DESC ", result);
        }

        [Fact]
        public void TestOrderByQuery_Ascending_DoubleOperand()
        {
            // Arrange
            predicate = x => x.Id;
            Expression<Func<ComplexTestModel, object>>? predicate2 = x => x.InternalId;

            // Act
            var result = evaluator.EvaluateOrderBy(new KeyValuePair<Command, List<IRelmExecutionCommand?>>(
                    Command.OrderBy, 
                    [ 
                        new RelmExecutionCommand(Command.Set, predicate.Body), 
                        new RelmExecutionCommand(Command.Set, predicate2.Body) 
                    ])
                , false);

            // Assert
            Assert.Equal("  ORDER BY a.`Id`  ASC , a.`InternalId`  ASC ", result);
        }

        [Fact]
        public void TestOrderByQuery_Descending_DoubleOperand()
        {
            // Arrange
            predicate = x => x.Id;
            Expression<Func<ComplexTestModel, object>>? predicate2 = x => x.InternalId;

            // Act
            var result = evaluator.EvaluateOrderBy(new KeyValuePair<Command, List<IRelmExecutionCommand?>>(
                    Command.OrderBy,
                    [
                        new RelmExecutionCommand(Command.Set, predicate.Body),
                        new RelmExecutionCommand(Command.Set, predicate2.Body)
                    ])
                , true);

            // Assert
            Assert.Equal("  ORDER BY a.`Id`  DESC , a.`InternalId`  DESC ", result);
        }

        [Fact]
        public void TestOrderByQuery_Ascending_TripleOperand()
        {
            // Arrange
            predicate = x => x.Id;
            Expression<Func<ComplexTestModel, object?>>? predicate2 = x => x.InternalId;
            Expression<Func<ComplexTestModel, object?>>? predicate3 = x => x.TestColumnInternalId;

            // Act
            var result = evaluator.EvaluateOrderBy(new KeyValuePair<Command, List<IRelmExecutionCommand?>>(
                    Command.OrderBy, 
                    [
                        new RelmExecutionCommand(Command.Set, predicate.Body),
                        new RelmExecutionCommand(Command.Set, predicate2.Body),
                        new RelmExecutionCommand(Command.Set, predicate3.Body)
                    ])
                , false);

            // Assert
            Assert.Equal("  ORDER BY a.`Id`  ASC , a.`InternalId`  ASC , a.`test_column_InternalId`  ASC ", result);
        }

        [Fact]
        public void TestOrderByQuery_Descending_TripleOperand()
        {
            // Arrange
            predicate = x => x.Id;
            Expression<Func<ComplexTestModel, object?>>? predicate2 = x => x.InternalId;
            Expression<Func<ComplexTestModel, object?>>? predicate3 = x => x.TestColumnInternalId;

            // Act
            var result = evaluator.EvaluateOrderBy(new KeyValuePair<Command, List<IRelmExecutionCommand?>>(
                    Command.OrderBy,
                    [
                        new RelmExecutionCommand(Command.Set, predicate.Body),
                        new RelmExecutionCommand(Command.Set, predicate2.Body),
                        new RelmExecutionCommand(Command.Set, predicate3.Body)
                    ])
                , true);

            // Assert
            Assert.Equal("  ORDER BY a.`Id`  DESC , a.`InternalId`  DESC , a.`test_column_InternalId`  DESC ", result);
        }

        [Fact]
        public void TestOrderByQuery_TwoOrderBys_BothAscending()
        {
            // Arrange
            predicate = x => x.Id;
            Expression<Func<ComplexTestModel, object>>? predicate2 = x => x.InternalId;

            // Act
            var result = evaluator.EvaluateOrderBy(new KeyValuePair<Command, List<IRelmExecutionCommand?>>(
                    Command.OrderBy, 
                    [new RelmExecutionCommand(Command.OrderBy, predicate.Body)])
                , false);

            result += evaluator.EvaluateOrderBy(new KeyValuePair<Command, List<IRelmExecutionCommand?>>(
                    Command.OrderBy, 
                    [new RelmExecutionCommand(Command.OrderBy, predicate2.Body)])
                , false);

            // Assert
            Assert.Equal("  ORDER BY a.`Id`  ASC  , a.`InternalId`  ASC ", result);
        }

        [Fact]
        public void TestOrderByQuery_TwoOrderBys_OneAscending_OneDescending()
        {
            // Arrange
            predicate = x => x.Id;
            Expression<Func<ComplexTestModel, object>>? predicate2 = x => x.InternalId;

            // Act
            var result = evaluator.EvaluateOrderBy(new KeyValuePair<Command, List<IRelmExecutionCommand?>>(
                    Command.OrderBy,
                    [new RelmExecutionCommand(Command.OrderBy, predicate.Body)])
                , false);

            result += evaluator.EvaluateOrderBy(new KeyValuePair<Command, List<IRelmExecutionCommand?>>(
                    Command.OrderBy,
                    [new RelmExecutionCommand(Command.OrderBy, predicate2.Body)])
                , true);

            // Assert
            Assert.Equal("  ORDER BY a.`Id`  ASC  , a.`InternalId`  DESC ", result);
        }
    }
}
