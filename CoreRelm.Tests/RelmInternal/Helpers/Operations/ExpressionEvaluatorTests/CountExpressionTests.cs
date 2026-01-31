using Moq;
using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.RelmInternal.Helpers.DataTransfer;
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
    public class CountExpressionTests
    {
        private ComplexTestQuickContext context;
        private readonly ExpressionEvaluator<ComplexTestModel> evaluator;

        public CountExpressionTests()
        {
            // dummy data
            var mockComplexTestModels = new List<ComplexTestModel>
            {
                new ComplexTestModel { InternalId = "ID1" },
                new ComplexTestModel { InternalId = "ID2" },
            };

            context = new ComplexTestQuickContext("name=SimpleRelmMySql");

            // create dummy data loaders for dummy data to be placed in both relevant data sets
            var modelDataLoader = new Mock<RelmDefaultDataLoader<ComplexTestModel>>(); // { CallBase = true };

            // make sure GetLoadData() calls base so LastExecutedCommands (required for references) gets populated
            modelDataLoader.Setup(x => x.TableName).Returns("DUMMY NAME");
            modelDataLoader.Setup(x => x.GetLoadData()).CallBase();
            modelDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexTestModels);

            context.SetDataSet(new ComplexTestModel());
            context.ComplexTestModels!.SetDataLoader(modelDataLoader.Object);

            var tableName = typeof(ComplexTestModel).GetCustomAttribute<RelmTable>(false)?.TableName ?? throw new ArgumentNullException();
            var underscoreProperties = DataNamingHelper.GetUnderscoreProperties<ComplexTestModel>(true, false).ToDictionary(x => x.Value.Item1, x => x.Key);

            evaluator = new ExpressionEvaluator<ComplexTestModel>(tableName, underscoreProperties, UsedTableAliases: new Dictionary<string, string> { [tableName] = "a" });
        }

        [Fact]
        public void Get_Count_Should_Return_2()
        {
            // Arrange & Act
            var modelsCount = context.ComplexTestModels!.Load().Count();

            // Assert
            Assert.Equal(2, modelsCount);
        }

        [Fact]
        public void TestCountQuery_NoOperand()
        {
            // Act
            var result = evaluator.EvaluateCount(new KeyValuePair<Command, List<IRelmExecutionCommand>>(
                Command.Count,
                new List<IRelmExecutionCommand> { new RelmExecutionCommand(Command.Count, null) }));

            // Assert
            Assert.Equal(" COUNT(*) AS `count_rows` ", result);
        }

        [Fact]
        public void TestCountQuery_SingleOperand()
        {
            // Arrange
            Expression<Func<ComplexTestModel, object>>? predicate = x => x.Id;

            // Act
            var result = evaluator.EvaluateCount(new KeyValuePair<Command, List<IRelmExecutionCommand>>(
                Command.Count,
                new List<IRelmExecutionCommand> { new RelmExecutionCommand(Command.Count, predicate.Body) }));

            // Assert
            Assert.Equal(" COUNT(a.`Id`) AS `count_Id` ", result);
        }

        [Fact]
        public void TestCountQuery_MultipleOperands()
        {
            // Arrange
            Expression<Func<ComplexTestModel, object>>? predicate = x => new { x.Id, x.TestColumnInternalId };

            // Act
            var result = evaluator.EvaluateCount(new KeyValuePair<Command, List<IRelmExecutionCommand>>(
                Command.Count,
                new List<IRelmExecutionCommand> { new RelmExecutionCommand(Command.Count, predicate.Body) }));

            // Assert
            Assert.Equal(" COUNT(a.`Id`) AS `count_Id` , COUNT(a.`test_column_InternalId`) AS `count_test_column_InternalId` ", result);
        }
    }
}
