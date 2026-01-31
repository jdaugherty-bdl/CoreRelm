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
    public class SetExpressionTests
    {
        private readonly ExpressionEvaluator<ComplexTestModel> evaluator;
        private readonly Dictionary<string, object> queryParameters;
        private Expression<Func<ComplexTestModel, ComplexTestModel>>? predicate;

        public SetExpressionTests()
        {
            var tableName = typeof(ComplexTestModel).GetCustomAttribute<RelmTable>(false)?.TableName ?? throw new ArgumentNullException();
            var underscoreProperties = DataNamingHelper.GetUnderscoreProperties<ComplexTestModel>(true, false).ToDictionary(x => x.Value.Item1, x => x.Key);

            evaluator = new ExpressionEvaluator<ComplexTestModel>(tableName, underscoreProperties, UsedTableAliases: new Dictionary<string, string> { [tableName] = "a" });

            queryParameters = new();
        }

        [Fact]
        public void TestExpressionEvaluatorSet_MultipleFields_ReturnsProperSql()
        {
            // Arrange
            predicate = x => new ComplexTestModel { TestColumnInternalId = "TEST_VALUE", TestColumnId = 1 };

            // Act
            var result = evaluator.EvaluateSet(new KeyValuePair<Command, List<IRelmExecutionCommand?>>(
                    Command.Set, 
                    [new RelmExecutionCommand(Command.Set, predicate.Body)])
                , queryParameters);

            // Assert
            Assert.Equal(" SET  a.`test_column_InternalId` = @_TestColumnInternalId_1_ , a.`test_column_id` = @_TestColumnId_1_  ", result);

            Assert.Equal("TEST_VALUE", queryParameters["@_TestColumnInternalId_1_"]);
            Assert.Equal(1, queryParameters["@_TestColumnId_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorSet_String_ReturnsProperSql()
        {
            // Arrange
            predicate = x => new ComplexTestModel { TestColumnInternalId = "TEST_VALUE" };

            // Act
            var result = evaluator.EvaluateSet(new KeyValuePair<Command, List<IRelmExecutionCommand?>>(
                    Command.Set, 
                    [new RelmExecutionCommand(Command.Set, predicate.Body)])
                , queryParameters);

            // Assert
            Assert.Equal(" SET  a.`test_column_InternalId` = @_TestColumnInternalId_1_  ", result);

            Assert.Equal("TEST_VALUE", queryParameters["@_TestColumnInternalId_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorSet_SetWithWhere_ReturnsProperSql()
        {
            // Arrange
            Expression<Func<ComplexTestModel, bool>>? wherePredicate = x => x.Active == false;
            predicate = x => new ComplexTestModel { TestColumnInternalId = "TEST_VALUE" };

            // Act
            var result = evaluator.EvaluateWhereNew([new RelmExecutionCommand(Command.Where, wherePredicate)], 
                queryParameters);

            result += evaluator.EvaluateSet(new KeyValuePair<Command, List<IRelmExecutionCommand?>>(
                    Command.Set,
                    [new RelmExecutionCommand(Command.Set, predicate.Body)])
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE (  ( a.`Active` = @_Active_1_ )  )  SET  a.`test_column_InternalId` = @_TestColumnInternalId_1_  ", result);

            Assert.IsType<int>(queryParameters["@_Active_1_"]);
            Assert.Equal(0, (int)queryParameters["@_Active_1_"]);
            Assert.Equal("TEST_VALUE", queryParameters["@_TestColumnInternalId_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorSet_Bool_ReturnsProperSql()
        {
            // Arrange
            predicate = x => new ComplexTestModel { Active = false };

            // Act
            var result = evaluator.EvaluateSet(new KeyValuePair<Command, List<IRelmExecutionCommand?>>(
                    Command.Set, 
                    [new RelmExecutionCommand(Command.Set, predicate.Body)])
                , queryParameters);

            // Assert
            Assert.Equal(" SET  a.`Active` = @_Active_1_  ", result);

            Assert.Equal(false, queryParameters["@_Active_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorSet_Nothing_ThrowsNotSupportedException()
        {
            // Arrange
            predicate = x => new ComplexTestModel();

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => evaluator.EvaluateSet(new KeyValuePair<Command, List<IRelmExecutionCommand?>>(
                    Command.Set, 
                    [new RelmExecutionCommand(Command.Set, predicate.Body)])
                , queryParameters));
        }
    }
}
