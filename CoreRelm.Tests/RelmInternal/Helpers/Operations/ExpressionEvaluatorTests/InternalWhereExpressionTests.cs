using SimpleRelm.Attributes;
using SimpleRelm.Interfaces;
using SimpleRelm.Models;
using SimpleRelm.RelmInternal.Helpers.Operations;
using CoreRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.RelmInternal.Helpers.Operations.ExpressionEvaluatorTests
{
    public class InternalWhereExpressionTests
    {
        private readonly ExpressionEvaluator evaluator;
        private readonly Dictionary<string, object> queryParameters;
        private Expression<Func<ComplexTestModel, bool>>? predicate;

        public InternalWhereExpressionTests()
        {
            var tableName = typeof(ComplexTestModel).GetCustomAttribute<RelmTable>(false)?.TableName ?? throw new ArgumentNullException();
            var underscoreProperties = DataNamingHelper.GetUnderscoreProperties<ComplexTestModel>(true, false).ToDictionary(x => x.Value.Item1, x => x.Key);

            evaluator = new ExpressionEvaluator(tableName, underscoreProperties, UsedTableAliases: new Dictionary<string, string> { [tableName] = "a" });

            queryParameters = new();
        }

        [Fact]
        public void TestWhereQueryWithComplexConditions_BuildExpressionTree()
        {
            // Arrange
            var objectList = new List<string>
            {
                "00000000-0000-0000-0000-000000000000",
                "00000000-0000-0000-0000-000000000001"
            };

            var parameter = Expression.Parameter(typeof(ComplexTestModel), "x");

            var memberExpression = Expression.Property(parameter, nameof(ComplexTestModel.TestColumnInternalId))
                ?? throw new Exception("Property referenced by TestColumnInternalId could not be found.");

            var containsMethod = objectList.GetType().GetMethod(nameof(List<object>.Contains))
                ?? throw new Exception("Object list does not have property of type 'Contains'");

            var containsExpression = Expression.Call(Expression.Constant(objectList), containsMethod, memberExpression);
            var funcType = typeof(Func<,>).MakeGenericType(typeof(ComplexTestModel), typeof(bool));
            var containsCall = Expression.Lambda(funcType, containsExpression, parameter);

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where, 
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, containsCall) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( FIND_IN_SET(a.`test_column_InternalId`, @_TestColumnInternalId_1_) )", result);
        }

        [Fact]
        public void TestWhereParametersWithComplexConditions_BuildExpressionTree()
        {
            // Arrange
            var objectList = new List<string>
            {
                "00000000-0000-0000-0000-000000000000",
                "00000000-0000-0000-0000-000000000001"
            };

            var parameter = Expression.Parameter(typeof(ComplexTestModel), "x");

            var memberExpression = Expression.Property(parameter, nameof(ComplexTestModel.TestColumnInternalId))
                ?? throw new Exception("Property referenced by TestColumnInternalId could not be found.");

            var containsMethod = objectList.GetType().GetMethod(nameof(List<object>.Contains))
                ?? throw new Exception("Object list does not have property of type 'Contains'");

            var containsExpression = Expression.Call(Expression.Constant(objectList), containsMethod, memberExpression);
            var funcType = typeof(Func<,>).MakeGenericType(typeof(ComplexTestModel), typeof(bool));
            var containsCall = Expression.Lambda(funcType, containsExpression, parameter);

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where,
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, containsCall) })
                , queryParameters);

            // Assert
            Assert.Equal(queryParameters["@_TestColumnInternalId_1_"], "00000000-0000-0000-0000-000000000000,00000000-0000-0000-0000-000000000001");
        }
    }
}
