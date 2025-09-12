using Moq;
using CoreRelm.Models;
using CoreRelm.RelmInternal.Helpers.Operations;
using CoreRelm.RelmInternal.Helpers.Utilities;
using CoreRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.RelmInternal.Helpers.Utilities.ForeignObjectsLoader_Tests
{
    public class ForeignObjectsLoader_Tester
    {
        private readonly ComplexTestContext context;
        private readonly List<ComplexTestModel> mockComplexTestModels;
        private LambdaExpression? containsLambda;

        public ForeignObjectsLoader_Tester()
        {
            // dummy data
            mockComplexTestModels =
            [
                new() 
                {
                    InternalId = "ID1",
                    ComplexReferenceObjectLocalKey = "LOCALKEY1",
                    ComplexReferenceObjects = null,
                    ComplexReferenceObject = null,
                    ComplexReferenceObject_NavigationProperties = null,
                    ComplexReferenceObject_NavigationPropertyItem = null,
                    ComplexReferenceObject_PrincipalEntities = null,
                    ComplexReferenceObject_PrincipalEntity_LocalKey = null,
                    ComplexReferenceObject_PrincipalEntities_LocalKeys = null,
                    ComplexReferenceObject_PrincipalEntityItem = null,
                    ComplexTestModels = null,
                    SimpleReferenceObjects = null,
                    TestColumnId = default,
                    TestColumnInternalId = null,
                    TestColumnNoAttributeArguments = null,
                    TestFieldBoolean = null,
                },
                new()
                {
                    InternalId = "ID2",
                    ComplexReferenceObjectLocalKey = "LOCALKEY2",
                    ComplexReferenceObjects = null,
                    ComplexReferenceObject = null,
                    ComplexReferenceObject_NavigationProperties = null,
                    ComplexReferenceObject_NavigationPropertyItem = null,
                    ComplexReferenceObject_PrincipalEntities = null,
                    ComplexReferenceObject_PrincipalEntity_LocalKey = null,
                    ComplexReferenceObject_PrincipalEntities_LocalKeys = null,
                    ComplexReferenceObject_PrincipalEntityItem = null,
                    ComplexTestModels = null,
                    SimpleReferenceObjects = null,
                    TestColumnId = default,
                    TestColumnInternalId = null,
                    TestColumnNoAttributeArguments = null,
                    TestFieldBoolean = null,
                }
            ];

            context = new ComplexTestContext("name=SimpleRelmMySql");

            // create dummy data loaders for dummy data to be placed in both relevant data sets
            var modelDataLoader = new Mock<RelmDefaultDataLoader<ComplexTestModel>>(); // { CallBase = true };

            // make sure GetLoadData() calls base so LastExecutedCommands (required for references) gets populated
            modelDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            modelDataLoader.Setup(x => x.GetLoadData()).CallBase();
            modelDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexTestModels);

            context.ComplexTestModels!.SetDataLoader(modelDataLoader.Object);
        }

        private Expression<Func<ComplexReferenceObject, object>> SetupReferenceDataLoader(Expression referencePredicate, bool useVariable)
        {
            var objectsLoader = new ForeignObjectsLoader<ComplexTestModel>(mockComplexTestModels, context);
            var executionCommand = new RelmExecutionCommand(ExpressionEvaluator.Command.Reference, referencePredicate);

            Expression<Func<ComplexReferenceObject, object>> predicate2 = x => x.Active == true;
            if (useVariable)
                executionCommand.AddAdditionalCommand(ExpressionEvaluator.Command.Reference, predicate2.Body);
            else
                executionCommand.AddAdditionalCommand<ComplexReferenceObject>(ExpressionEvaluator.Command.Reference, x => x.Active == false);

            var navigationOptions = executionCommand.GetForeignKeyNavigationOptions(mockComplexTestModels);
            containsLambda = objectsLoader.BuildLogicExpression(executionCommand, navigationOptions);

            if (useVariable)
                return predicate2;
            else
                return x => x.Active == true;
        }

        private Expression<Func<ComplexReferenceObject_NavigationProperty, object>> SetupNavigationDataLoader(Expression referencePredicate, bool useVariable)
        {
            var objectsLoader = new ForeignObjectsLoader<ComplexTestModel>(mockComplexTestModels, context);
            var executionCommand = new RelmExecutionCommand(ExpressionEvaluator.Command.Reference, referencePredicate);

            Expression<Func<ComplexReferenceObject_NavigationProperty, object>> predicate2 = x => x.Active == true;
            if (useVariable)
                executionCommand.AddAdditionalCommand(ExpressionEvaluator.Command.Reference, predicate2.Body);
            else
                executionCommand.AddAdditionalCommand<ComplexReferenceObject>(ExpressionEvaluator.Command.Reference, x => x.Active == false);

            var navigationOptions = executionCommand.GetForeignKeyNavigationOptions(mockComplexTestModels);
            containsLambda = objectsLoader.BuildLogicExpression(executionCommand, navigationOptions);

            if (useVariable)
                return predicate2;
            else
                return x => x.Active == true;
        }

        private Expression<Func<ComplexReferenceObject_PrincipalEntity, object>> SetupPrincipalDataLoader(Expression referencePredicate, bool useVariable)
        {
            var objectsLoader = new ForeignObjectsLoader<ComplexTestModel>(mockComplexTestModels, context);
            var executionCommand = new RelmExecutionCommand(ExpressionEvaluator.Command.Reference, referencePredicate);

            Expression<Func<ComplexReferenceObject_PrincipalEntity, object>> predicate2 = x => x.Active == true;
            if (useVariable)
                executionCommand.AddAdditionalCommand(ExpressionEvaluator.Command.Reference, predicate2.Body);
            else
                executionCommand.AddAdditionalCommand<ComplexReferenceObject>(ExpressionEvaluator.Command.Reference, x => x.Active == false);

            var navigationOptions = executionCommand.GetForeignKeyNavigationOptions(mockComplexTestModels);
            containsLambda = objectsLoader.BuildLogicExpression(executionCommand, navigationOptions);

            if (useVariable)
                return predicate2;
            else
                return x => x.Active == true;
        }

        [Fact]
        public void AddAdditionalCommands_LoadsForeignObjectsWithVariablePredicate_ReturnsModifiedLambda()
        {
            // Arrange
            Expression<Func<ComplexTestModel, object?>> predicate = x => x.ComplexReferenceObjects;

            // Act
            var predicate2 = SetupReferenceDataLoader(predicate.Body, true);

            // Assert
            Assert.NotNull(containsLambda);
            Assert.True(containsLambda.Body is BinaryExpression);
            Assert.Equal("((\"ID1\" == x.ComplexTestModelInternalId) OrElse (\"ID2\" == x.ComplexTestModelInternalId))", ((BinaryExpression)containsLambda.Body).Left.ToString());
            Assert.Equal(((UnaryExpression)predicate2.Body).Operand, ((BinaryExpression)containsLambda.Body).Right);
        }

        [Fact]
        public void AddAdditionalCommands_LoadsForeignObjectsWithExpressionPredicate_ReturnsModifiedLambda()
        {
            // Arrange
            Expression<Func<ComplexTestModel, object?>> predicate = x => x.ComplexReferenceObjects;

            // Act
            SetupReferenceDataLoader(predicate.Body, false);

            // Assert
            Assert.NotNull(containsLambda);
            Assert.True(containsLambda.Body is BinaryExpression);
            Assert.Equal("((\"ID1\" == x.ComplexTestModelInternalId) OrElse (\"ID2\" == x.ComplexTestModelInternalId))", ((BinaryExpression)containsLambda.Body).Left.ToString());

            // cannot equate a lambda operand with an expression directly so we get the debug view of the expressions and compare those
            Assert.Equal(((UnaryExpression)((Expression<Func<ComplexTestModel, object>>)(x => x.Active == false)).Body).Operand.ToString(), ((BinaryExpression)containsLambda.Body).Right.ToString());
        }

        [Fact]
        public void AddAdditionalCommands_LoadsForeignObjectWithVariablePredicate_ReturnsModifiedLambda()
        {
            // Arrange
            Expression<Func<ComplexTestModel, object?>> predicate = x => x.ComplexReferenceObject;
            
            // Act
            var predicate2 = SetupReferenceDataLoader(predicate.Body, true);

            // Assert
            Assert.NotNull(containsLambda);
            Assert.True(containsLambda.Body is BinaryExpression);
            Assert.Equal("((\"ID1\" == x.ComplexTestModelInternalId) OrElse (\"ID2\" == x.ComplexTestModelInternalId))", ((BinaryExpression)containsLambda.Body).Left.ToString());
            Assert.Equal(((UnaryExpression)predicate2.Body).Operand, ((BinaryExpression)containsLambda.Body).Right);
        }

        [Fact]
        public void AddAdditionalCommands_LoadsForeignObjectWithExpressionPredicate_ReturnsModifiedLambda()
        {
            // Arrange
            Expression<Func<ComplexTestModel, object?>> predicate = x => x.ComplexReferenceObject;
            
            // Act
            SetupReferenceDataLoader(predicate.Body, false);

            // Assert
            Assert.NotNull(containsLambda);
            Assert.True(containsLambda.Body is BinaryExpression);
            Assert.Equal("((\"ID1\" == x.ComplexTestModelInternalId) OrElse (\"ID2\" == x.ComplexTestModelInternalId))", ((BinaryExpression)containsLambda.Body).Left.ToString());

            // cannot equate a lambda operand with an expression directly so we get the debug view of the expressions and compare those
            Assert.Equal(((UnaryExpression)((Expression<Func<ComplexTestModel, object>>)(x => x.Active == false)).Body).Operand.ToString(), ((BinaryExpression)containsLambda.Body).Right.ToString());
        }

        [Fact]
        public void AddAdditionalCommands_LoadsNavigationPropertyObjectsWithVariablePredicate_ReturnsModifiedLambda()
        {
            // Arrange
            Expression<Func<ComplexTestModel, object?>> predicate = x => x.ComplexReferenceObject_NavigationProperties;

            // Act
            var predicate2 = SetupNavigationDataLoader(predicate.Body, true);

            // Assert
            Assert.NotNull(containsLambda);
            Assert.True(containsLambda.Body is BinaryExpression);
            Assert.Equal("((\"ID1\" == x.ComplexTestModelInternalId) OrElse (\"ID2\" == x.ComplexTestModelInternalId))", ((BinaryExpression)containsLambda.Body).Left.ToString());
            Assert.Equal(((UnaryExpression)predicate2.Body).Operand, ((BinaryExpression)containsLambda.Body).Right);
        }

        [Fact]
        public void AddAdditionalCommands_LoadsNavigationPropertyObjectsWithExpressionPredicate_ReturnsModifiedLambda()
        {
            // Arrange
            Expression<Func<ComplexTestModel, object?>> predicate = x => x.ComplexReferenceObject_NavigationProperties;

            // Act
            SetupNavigationDataLoader(predicate.Body, false);

            // Assert
            Assert.NotNull(containsLambda);
            Assert.True(containsLambda.Body is BinaryExpression);
            Assert.Equal("((\"ID1\" == x.ComplexTestModelInternalId) OrElse (\"ID2\" == x.ComplexTestModelInternalId))", ((BinaryExpression)containsLambda.Body).Left.ToString());

            // cannot equate a lambda operand with an expression directly so we get the debug view of the expressions and compare those
            Assert.Equal(((UnaryExpression)((Expression<Func<ComplexTestModel, object>>)(x => x.Active == false)).Body).Operand.ToString(), ((BinaryExpression)containsLambda.Body).Right.ToString());
        }

        [Fact]
        public void AddAdditionalCommands_LoadsNavigationPropertyObjectWithVariablePredicate_ReturnsModifiedLambda()
        {
            // Arrange
            Expression<Func<ComplexTestModel, object?>> predicate = x => x.ComplexReferenceObject_NavigationPropertyItem;

            // Act
            var predicate2 = SetupNavigationDataLoader(predicate.Body, true);

            // Assert
            Assert.NotNull(containsLambda);
            Assert.True(containsLambda.Body is BinaryExpression);
            Assert.Equal("((\"ID1\" == x.ComplexTestModelInternalId) OrElse (\"ID2\" == x.ComplexTestModelInternalId))", ((BinaryExpression)containsLambda.Body).Left.ToString());
            Assert.Equal(((UnaryExpression)predicate2.Body).Operand, ((BinaryExpression)containsLambda.Body).Right);
        }

        [Fact]
        public void AddAdditionalCommands_LoadsNavigationPropertyObjectWithExpressionPredicate_ReturnsModifiedLambda()
        {
            // Arrange
            Expression<Func<ComplexTestModel, object?>> predicate = x => x.ComplexReferenceObject_NavigationPropertyItem;

            // Act
            SetupNavigationDataLoader(predicate.Body, false);

            // Assert
            Assert.NotNull(containsLambda);
            Assert.True(containsLambda.Body is BinaryExpression);
            Assert.Equal("((\"ID1\" == x.ComplexTestModelInternalId) OrElse (\"ID2\" == x.ComplexTestModelInternalId))", ((BinaryExpression)containsLambda.Body).Left.ToString());

            // cannot equate a lambda operand with an expression directly so we get the debug view of the expressions and compare those
            Assert.Equal(((UnaryExpression)((Expression<Func<ComplexTestModel, object>>)(x => x.Active == false)).Body).Operand.ToString(), ((BinaryExpression)containsLambda.Body).Right.ToString());
        }

        [Fact]
        public void AddAdditionalCommands_LoadsPrincipalEntityObjectsWithVariablePredicate_ReturnsModifiedLambda()
        {
            // Arrange
            Expression<Func<ComplexTestModel, object?>> predicate = x => x.ComplexReferenceObject_PrincipalEntities;

            // Act
            var predicate2 = SetupPrincipalDataLoader(predicate.Body, true);

            // Assert
            Assert.NotNull(containsLambda);
            Assert.True(containsLambda.Body is BinaryExpression);
            Assert.Equal("((\"ID1\" == x.ComplexTestModelInternalId) OrElse (\"ID2\" == x.ComplexTestModelInternalId))", ((BinaryExpression)containsLambda.Body).Left.ToString());
            Assert.Equal(((UnaryExpression)predicate2.Body).Operand, ((BinaryExpression)containsLambda.Body).Right);
        }

        [Fact]
        public void AddAdditionalCommands_LoadsPrincipalEntityObjectsWithExpressionPredicate_ReturnsModifiedLambda()
        {
            // Arrange
            Expression<Func<ComplexTestModel, object?>> predicate = x => x.ComplexReferenceObject_PrincipalEntities;

            // Act
            SetupPrincipalDataLoader(predicate.Body, false);

            // Assert
            Assert.NotNull(containsLambda);
            Assert.True(containsLambda.Body is BinaryExpression);
            Assert.Equal("((\"ID1\" == x.ComplexTestModelInternalId) OrElse (\"ID2\" == x.ComplexTestModelInternalId))", ((BinaryExpression)containsLambda.Body).Left.ToString());

            // cannot equate a lambda operand with an expression directly so we get the debug view of the expressions and compare those
            Assert.Equal(((UnaryExpression)((Expression<Func<ComplexTestModel, object>>)(x => x.Active == false)).Body).Operand.ToString(), ((BinaryExpression)containsLambda.Body).Right.ToString());
        }

        [Fact]
        public void AddAdditionalCommands_LoadsPrincipalEntityObjectWithVariablePredicate_ReturnsModifiedLambda()
        {
            // Arrange
            Expression<Func<ComplexTestModel, object?>> predicate = x => x.ComplexReferenceObject_PrincipalEntityItem;

            // Act
            var predicate2 = SetupPrincipalDataLoader(predicate.Body, true);

            // Assert
            Assert.NotNull(containsLambda);
            Assert.True(containsLambda.Body is BinaryExpression);
            Assert.Equal("((\"ID1\" == x.ComplexTestModelInternalId) OrElse (\"ID2\" == x.ComplexTestModelInternalId))", ((BinaryExpression)containsLambda.Body).Left.ToString());
            Assert.Equal(((UnaryExpression)predicate2.Body).Operand, ((BinaryExpression)containsLambda.Body).Right);
        }

        [Fact]
        public void AddAdditionalCommands_LoadsPrincipalEntityObjectWithExpressionPredicate_ReturnsModifiedLambda()
        {
            // Arrange
            Expression<Func<ComplexTestModel, object?>> predicate = x => x.ComplexReferenceObject_PrincipalEntityItem;

            // Act
            SetupPrincipalDataLoader(predicate.Body, false);

            // Assert
            Assert.NotNull(containsLambda);
            Assert.True(containsLambda.Body is BinaryExpression);
            Assert.Equal("((\"ID1\" == x.ComplexTestModelInternalId) OrElse (\"ID2\" == x.ComplexTestModelInternalId))", ((BinaryExpression)containsLambda.Body).Left.ToString());

            // cannot equate a lambda operand with an expression directly so we get the debug view of the expressions and compare those
            Assert.Equal(((UnaryExpression)((Expression<Func<ComplexTestModel, object>>)(x => x.Active == false)).Body).Operand.ToString(), ((BinaryExpression)containsLambda.Body).Right.ToString());
        }

        [Fact]
        public void AddAdditionalCommands_LoadsPrincipalEntityLocalKeyObjectsWithVariablePredicate_ReturnsModifiedLambda()
        {
            // Arrange
            Expression<Func<ComplexTestModel, object?>> predicate = x => x.ComplexReferenceObject_PrincipalEntities_LocalKeys;

            // Act
            var predicate2 = SetupPrincipalDataLoader(predicate.Body, true);

            // Assert
            Assert.NotNull(containsLambda);
            Assert.True(containsLambda.Body is BinaryExpression);
            Assert.Equal("((\"LOCALKEY1\" == x.ComplexTestModelLocalKey) OrElse (\"LOCALKEY2\" == x.ComplexTestModelLocalKey))", ((BinaryExpression)containsLambda.Body).Left.ToString());
            Assert.Equal(((UnaryExpression)predicate2.Body).Operand, ((BinaryExpression)containsLambda.Body).Right);
        }

        [Fact]
        public void AddAdditionalCommands_LoadsPrincipalEntityLocalKeyObjectsWithExpressionPredicate_ReturnsModifiedLambda()
        {
            // Arrange
            Expression<Func<ComplexTestModel, object?>> predicate = x => x.ComplexReferenceObject_PrincipalEntities_LocalKeys;

            // Act
            SetupPrincipalDataLoader(predicate.Body, false);

            // Assert
            Assert.NotNull(containsLambda);
            Assert.True(containsLambda.Body is BinaryExpression);
            Assert.Equal("((\"LOCALKEY1\" == x.ComplexTestModelLocalKey) OrElse (\"LOCALKEY2\" == x.ComplexTestModelLocalKey))", ((BinaryExpression)containsLambda.Body).Left.ToString());

            // cannot equate a lambda operand with an expression directly so we get the debug view of the expressions and compare those
            Assert.Equal(((UnaryExpression)((Expression<Func<ComplexTestModel, object>>)(x => x.Active == false)).Body).Operand.ToString(), ((BinaryExpression)containsLambda.Body).Right.ToString());
        }

        [Fact]
        public void AddAdditionalCommands_LoadsPrincipalEntityLocalKeyObjectWithVariablePredicate_ReturnsModifiedLambda()
        {
            // Arrange
            Expression<Func<ComplexTestModel, object?>> predicate = x => x.ComplexReferenceObject_PrincipalEntity_LocalKey;

            // Act
            var predicate2 = SetupPrincipalDataLoader(predicate.Body, true);

            // Assert
            Assert.NotNull(containsLambda);
            Assert.True(containsLambda.Body is BinaryExpression);
            Assert.Equal("((\"LOCALKEY1\" == x.ComplexTestModelLocalKey) OrElse (\"LOCALKEY2\" == x.ComplexTestModelLocalKey))", ((BinaryExpression)containsLambda.Body).Left.ToString());
            Assert.Equal(((UnaryExpression)predicate2.Body).Operand, ((BinaryExpression)containsLambda.Body).Right);
        }

        [Fact]
        public void AddAdditionalCommands_LoadsPrincipalEntityLocalKeyObjectWithExpressionPredicate_ReturnsModifiedLambda()
        {
            // Arrange
            Expression<Func<ComplexTestModel, object?>> predicate = x => x.ComplexReferenceObject_PrincipalEntity_LocalKey;

            // Act
            SetupPrincipalDataLoader(predicate.Body, false);

            // Assert
            Assert.NotNull(containsLambda);
            Assert.True(containsLambda.Body is BinaryExpression);
            Assert.Equal("((\"LOCALKEY1\" == x.ComplexTestModelLocalKey) OrElse (\"LOCALKEY2\" == x.ComplexTestModelLocalKey))", ((BinaryExpression)containsLambda.Body).Left.ToString());

            // cannot equate a lambda operand with an expression directly so we get the debug view of the expressions and compare those
            Assert.Equal(((UnaryExpression)((Expression<Func<ComplexTestModel, object>>)(x => x.Active == false)).Body).Operand.ToString(), ((BinaryExpression)containsLambda.Body).Right.ToString());
        }
    }
}
