using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.RelmInternal.Helpers.Operations;
using CoreRelm.RelmInternal.Helpers.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace CoreRelm.Tests.RelmInternal.Helpers.Operations.ExpressionEvaluatorTests
{
    public class CopilotGeneratedTests
    {
        private class TestModel
        {
            [RelmColumn("id")]
            public int Id { get; set; }

            [RelmColumn("name")]
            public string Name { get; set; }

            [RelmColumn("status")]
            public TestEnum Status { get; set; }
        }

        private enum TestEnum
        {
            None = 0,
            Active = 1,
            Inactive = 2
        }

        private readonly ExpressionEvaluator evaluator;
        private readonly Dictionary<string, object> queryParameters;
        private readonly Dictionary<string, string> underscoreProperties;

        public CopilotGeneratedTests()
        {
            underscoreProperties = new Dictionary<string, string>
            {
                { nameof(TestModel.Id), "id" },
                { nameof(TestModel.Name), "name" },
                { nameof(TestModel.Status), "status" }
            };

            evaluator = new ExpressionEvaluator("TestTable", underscoreProperties);
            queryParameters = [];
        }

        [Fact]
        public void EvaluateWhere_SimpleEqualExpression_ReturnsCorrectSql()
        {
            // Arrange
            Expression<Func<TestModel, bool>> expression = x => x.Id == 5;
            var commandExpression = new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand?>>(
                ExpressionEvaluator.Command.Where,
                [new RelmExecutionCommand(ExpressionEvaluator.Command.Where, expression)]);

            // Act
            var sql = evaluator.EvaluateWhere(commandExpression, queryParameters);

            // Assert
            Assert.Equal(" WHERE ( a.`id` = @_Id_1_ )", sql);
            Assert.Single(queryParameters);
            Assert.Equal(5, queryParameters["@_Id_1_"]);
        }

        [Fact]
        public void EvaluateWhere_MultipleConditions_ReturnsCorrectSql()
        {
            // Arrange
            Expression<Func<TestModel, bool>> expression = x => x.Name == "Test" && x.Status == TestEnum.Active;
            var commandExpression = new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand?>>(
                ExpressionEvaluator.Command.Where,
                [new RelmExecutionCommand(ExpressionEvaluator.Command.Where, expression)]);

            // Act
            var sql = evaluator.EvaluateWhere(commandExpression, queryParameters);

            // Assert
            var expectedSql = " WHERE ( a.`name` = @_Name_1_ ) AND ( a.`status` = @_Status_1_ ))";
            Assert.Equal(expectedSql, sql);
            Assert.Equal(2, queryParameters.Count);
            Assert.Equal("Test", queryParameters["@_Name_1_"]);
            Assert.Equal(1, queryParameters["@_Status_1_"]);
        }

        [Fact]
        public void EvaluateWhere_WithMethodCall_ReturnsCorrectSql()
        {
            // Arrange
            Expression<Func<TestModel, bool>> expression = x => x.Name.Contains("Test");
            var commandExpression = new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand?>>(
                ExpressionEvaluator.Command.Where,
                [new RelmExecutionCommand(ExpressionEvaluator.Command.Where, expression)]);

            // Act
            var sql = evaluator.EvaluateWhere(commandExpression, queryParameters);

            // Assert
            var expectedSql = " WHERE ( a.`name` LIKE @_Name_1_ )";
            Assert.Equal(expectedSql, sql);
            Assert.Single(queryParameters);
            Assert.Equal("%Test%", queryParameters["@_Name_1_"]);
        }

        [Fact]
        public void EvaluateWhere_WithUnaryExpression_ReturnsCorrectSql()
        {
            // Arrange
            Expression<Func<TestModel, bool>> expression = x => !string.IsNullOrEmpty(x.Name);
            var commandExpression = new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand?>>(
                ExpressionEvaluator.Command.Where,
                [new RelmExecutionCommand(ExpressionEvaluator.Command.Where, expression)]);

            // Act
            var sql = evaluator.EvaluateWhere(commandExpression, queryParameters);

            // Assert
            var expectedSql = " WHERE ( a.`name` IS NOT NULL )";
            Assert.Equal(expectedSql, sql);
            Assert.Empty(queryParameters);
        }

        [Fact]
        public void EvaluateWhere_WithEnumComparison_ReturnsCorrectSql()
        {
            // Arrange
            Expression<Func<TestModel, bool>> expression = x => x.Status == TestEnum.Inactive;
            var commandExpression = new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand?>>(
                ExpressionEvaluator.Command.Where,
                [new RelmExecutionCommand(ExpressionEvaluator.Command.Where, expression)]);

            // Act
            var sql = evaluator.EvaluateWhere(commandExpression, queryParameters);

            // Assert
            var expectedSql = " WHERE ( a.`status` = @_Status_1_ )";
            Assert.Equal(expectedSql, sql);
            Assert.Single(queryParameters);
            Assert.Equal(2, queryParameters["@_Status_1_"]);
        }

        [Fact]
        public void EvaluateOrderBy_SimpleOrderBy_ReturnsCorrectSql()
        {
            // Arrange
            Expression<Func<TestModel, object>> expression = x => x.Name;
            var commandExpression = new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand?>>(
                ExpressionEvaluator.Command.OrderBy,
                [new RelmExecutionCommand(ExpressionEvaluator.Command.OrderBy, expression)]);

            // Act
            var sql = evaluator.EvaluateOrderBy(commandExpression, false);

            // Assert
            var expectedSql = " ORDER BY a.`name`  ASC ";
            Assert.Equal(expectedSql, sql);
        }

        [Fact]
        public void EvaluateGroupBy_SimpleGroupBy_ReturnsCorrectSql()
        {
            // Arrange
            Expression<Func<TestModel, object>> expression = x => x.Status;
            var commandExpression = new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand?>>(
                ExpressionEvaluator.Command.GroupBy,
                [new RelmExecutionCommand(ExpressionEvaluator.Command.GroupBy, expression)]);

            // Act
            var sql = evaluator.EvaluateGroupBy(commandExpression);

            // Assert
            var expectedSql = " GROUP BY a.`status` ";
            Assert.Equal(expectedSql, sql);
        }

        [Fact]
        public void EvaluateLimit_SimpleLimit_ReturnsCorrectSql()
        {
            // Arrange
            var constantExpression = Expression.Constant(10);
            var commandExpression = new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand?>>(
                ExpressionEvaluator.Command.Limit,
                [new RelmExecutionCommand(ExpressionEvaluator.Command.Limit, constantExpression)]);

            // Act
            var sql = evaluator.EvaluateLimit(commandExpression);

            // Assert
            Assert.Equal(" LIMIT 10 ", sql);
        }

        [Fact]
        public void EvaluateWhere_WithOrExpression_ReturnsCorrectSql()
        {
            // Arrange
            Expression<Func<TestModel, bool>> expression = x => x.Name == "Test" || x.Id > 100;
            var commandExpression = new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand?>>(
                ExpressionEvaluator.Command.Where,
                [new RelmExecutionCommand(ExpressionEvaluator.Command.Where, expression)]);

            // Act
            var sql = evaluator.EvaluateWhere(commandExpression, queryParameters);

            // Assert
            var expectedSql = " WHERE ( a.`name` = @_Name_1_ ) OR ( a.`id` > @_Id_1_ ))";
            Assert.Equal(expectedSql, sql);
            Assert.Equal(2, queryParameters.Count);
            Assert.Equal("Test", queryParameters["@_Name_1_"]);
            Assert.Equal(100, queryParameters["@_Id_1_"]);
        }

        [Fact]
        public void EvaluateWhere_WithNestedExpressions_ReturnsCorrectSql()
        {
            // Arrange
            Expression<Func<TestModel, bool>> expression = x => (x.Name == "Test" && x.Id > 100) || x.Status == TestEnum.Active;
            var commandExpression = new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand?>>(
                ExpressionEvaluator.Command.Where,
                [new RelmExecutionCommand(ExpressionEvaluator.Command.Where, expression)]);

            // Act
            var sql = evaluator.EvaluateWhere(commandExpression, queryParameters);

            // Assert
            var expectedSql = " WHERE ( ( a.`name` = @_Name_1_ ) AND ( a.`id` > @_Id_1_ ) ) OR ( a.`status` = @_Status_1_ ))";
            Assert.Equal(expectedSql, sql);
            Assert.Equal(3, queryParameters.Count);
            Assert.Equal("Test", queryParameters["@_Name_1_"]);
            Assert.Equal(100, queryParameters["@_Id_1_"]);
            Assert.Equal(1, queryParameters["@_Status_1_"]);
        }

        [Fact]
        public void EvaluateSet_SimpleSet_ReturnsCorrectSql()
        {
            // Arrange
            var memberInitExpression = Expression.MemberInit(
                Expression.New(typeof(TestModel)),
                Expression.Bind(typeof(TestModel).GetProperty(nameof(TestModel.Name)), Expression.Constant("UpdatedName")),
                Expression.Bind(typeof(TestModel).GetProperty(nameof(TestModel.Status)), Expression.Constant(TestEnum.Active))
            );
            var commandExpression = new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand?>>(
                ExpressionEvaluator.Command.Set,
                [new RelmExecutionCommand(ExpressionEvaluator.Command.Set, memberInitExpression)]);

            // Act
            var sql = evaluator.EvaluateSet(commandExpression, queryParameters);

            // Assert
            var expectedSql = " SET  a.`name` = @_Name_1_, a.`status` = @_Status_1_  ";
            Assert.Equal(expectedSql, sql);
            Assert.Equal(2, queryParameters.Count);
            Assert.Equal("UpdatedName", queryParameters["@_Name_1_"]);
            Assert.Equal(1, queryParameters["@_Status_1_"]);
        }

        [Fact]
        public void EvaluateDistinctBy_SimpleDistinctBy_ReturnsCorrectSql()
        {
            // Arrange
            Expression<Func<TestModel, object>> expression = x => x.Status;
            var commandExpression = new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand?>>(
                ExpressionEvaluator.Command.DistinctBy,
                [new RelmExecutionCommand(ExpressionEvaluator.Command.DistinctBy, expression)]);

            // Act
            var sql = evaluator.EvaluateDistinctBy(commandExpression);

            // Assert
            var expectedSql = " DISTINCT a.`status` ";
            Assert.Equal(expectedSql, sql);
        }

        [Fact]
        public void EvaluateWhere_WithContains_ReturnsCorrectSql()
        {
            // Arrange
            var values = new List<int> { 1, 2, 3 };
            Expression<Func<TestModel, bool>> expression = x => values.Contains(x.Id);
            var commandExpression = new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand?>>(
                ExpressionEvaluator.Command.Where,
                [new RelmExecutionCommand(ExpressionEvaluator.Command.Where, expression)]);

            // Act
            var sql = evaluator.EvaluateWhere(commandExpression, queryParameters);

            // Assert
            var expectedSql = " WHERE ( FIND_IN_SET(a.`id`, @_Id_1_) )";
            Assert.Equal(expectedSql, sql);
            Assert.Single(queryParameters);
            Assert.Equal("1,2,3", queryParameters["@_Id_1_"]);
        }
    }
}
