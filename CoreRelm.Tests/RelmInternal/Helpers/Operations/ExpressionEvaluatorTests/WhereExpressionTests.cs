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
using static CoreRelm.Tests.TestModels.ComplexTestModel;

namespace CoreRelm.Tests.RelmInternal.Helpers.Operations.ExpressionEvaluatorTests
{
    public class WhereExpressionTests
    {
        private readonly ExpressionEvaluator evaluator;
        private readonly Dictionary<string, object> queryParameters;
        private Expression<Func<ComplexTestModel, bool>>? predicate;

        public WhereExpressionTests()
        {
            var tableName = typeof(ComplexTestModel).GetCustomAttribute<RelmTable>(false)?.TableName ?? throw new ArgumentNullException();
            var underscoreProperties = DataNamingHelper.GetUnderscoreProperties<ComplexTestModel>(true).ToDictionary(x => x.Value.Item1, x => x.Key);

            evaluator = new ExpressionEvaluator(tableName, underscoreProperties, UsedTableAliases: new Dictionary<string, string> { [tableName] = "a" });

            queryParameters = new();
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_Equalities_Equals_Int_Constant()
        {
            // Arrange
            predicate = x => x.Id == 3L;

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where, 
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( a.`Id` = @_Id_1_ )", result);
            Assert.Equal(3L, queryParameters["@_Id_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_Equalities_Equals_Int_Variable()
        {
            // Arrange
            var id = 3L;
            predicate = x => x.Id == id;

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where,
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( a.`Id` = @_Id_1_ )", result);
            Assert.Equal(id, queryParameters["@_Id_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_Equalities_Equals_String_Constant()
        {
            // Arrange
            predicate = x => x.InternalId == "00000000-0000-0000-0000-000000000000";

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where,
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( a.`InternalId` = @_InternalId_1_ )", result);
            Assert.Equal("00000000-0000-0000-0000-000000000000", queryParameters["@_InternalId_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_Equalities_Equals_String_Variable()
        {
            // Arrange
            var internalId = "00000000-0000-0000-0000-000000000000";
            predicate = x => x.InternalId == internalId;

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where,
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( a.`InternalId` = @_InternalId_1_ )", result);
            Assert.Equal(internalId, queryParameters["@_InternalId_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_Equalities_Equals_DateTime()
        {
            // Arrange
            predicate = x => x.CreateDate == new DateTime(2021, 1, 1);

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where,
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( a.`Create_Date` = @_CreateDate_1_ )", result);
            Assert.Equal(new DateTime(2021, 1, 1), queryParameters["@_CreateDate_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_Equalities_Equals_Boolean()
        {
            // Arrange
            predicate = x => x.Active == true;

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where,
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( a.`Active` = @_Active_1_ )", result);
            Assert.Equal(1, queryParameters["@_Active_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_ListContainsField()
        {
            var objectList = new List<ComplexTestModel>
            {
                new ComplexTestModel { TestColumnInternalId = "00000000-0000-0000-0000-000000000000" },
                new ComplexTestModel { TestColumnInternalId = "00000000-0000-0000-0000-000000000001" }
            };

            // Arrange
            predicate = x => objectList.Select(y => y.TestColumnInternalId).Contains(x.InternalId);

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where,
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( FIND_IN_SET(a.`InternalId`, @_InternalId_1_) )", result);
            Assert.Equal("00000000-0000-0000-0000-000000000000,00000000-0000-0000-0000-000000000001", queryParameters["@_InternalId_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_Equalities_Equals_4Types()
        {
            // Arrange
            predicate = x => x.Id == 3L && x.InternalId == "00000000-0000-0000-0000-000000000000" && x.CreateDate == new DateTime(2021, 1, 1) && x.Active == true;

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where,
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( a.`Id` = @_Id_1_    AND  a.`InternalId` = @_InternalId_1_    AND  a.`Create_Date` = @_CreateDate_1_    AND  a.`Active` = @_Active_1_ )", result);

            Assert.Equal(3L, queryParameters["@_Id_1_"]);
            Assert.Equal("00000000-0000-0000-0000-000000000000", queryParameters["@_InternalId_1_"]);
            Assert.Equal(new DateTime(2021, 1, 1), queryParameters["@_CreateDate_1_"]);
            Assert.Equal(1, queryParameters["@_Active_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_Equalities_Equals_2Types_Or_2Groups()
        {
            // Arrange
            predicate = x => (x.Id == 3L && x.InternalId == "00000000-0000-0000-0000-000000000000") || (x.CreateDate == new DateTime(2021, 1, 1) && x.Active == true);

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where,
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( a.`Id` = @_Id_1_    AND  a.`InternalId` = @_InternalId_1_    ) OR (  a.`Create_Date` = @_CreateDate_1_    AND  a.`Active` = @_Active_1_ )", result);

            Assert.Equal(3L, queryParameters["@_Id_1_"]);
            Assert.Equal("00000000-0000-0000-0000-000000000000", queryParameters["@_InternalId_1_"]);
            Assert.Equal(new DateTime(2021, 1, 1), queryParameters["@_CreateDate_1_"]);
            Assert.Equal(1, queryParameters["@_Active_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_Equalities_GreaterThanOrEqual_DateTime()
        {
            // Arrange
            predicate = x => x.CreateDate >= new DateTime(2021, 1, 1);

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where,
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( a.`Create_Date` >= @_CreateDate_1_ )", result);
            Assert.Equal(new DateTime(2021, 1, 1), queryParameters["@_CreateDate_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_MethodCallExpression_LeftRight()
        {
            // Arrange
            var originalDate = new DateTime(2021, 1, 1);
            var expectedDate = originalDate.AddMinutes(-15);

            predicate = x => x.CreateDate >= originalDate.AddMinutes(-15); // make originalDate.AddMinutes instead of expectedDate so we get a MethodCallExpression

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where,
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( a.`Create_Date` >= @_CreateDate_1_ )", result);
            Assert.Equal(expectedDate, queryParameters["@_CreateDate_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_MethodCallExpression_RightLeft()
        {
            // Arrange
            var originalDate = new DateTime(2021, 1, 1);
            var expectedDate = originalDate.AddMinutes(-15);

            predicate = x => originalDate.AddMinutes(-15) < x.CreateDate; // make originalDate.AddMinutes instead of expectedDate so we get a MethodCallExpression

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where,
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( a.`Create_Date` < @_CreateDate_1_ )", result);
            Assert.Equal(expectedDate, queryParameters["@_CreateDate_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_String_FirstOrDefault()
        {
            // Arrange
            var submissionIds = new List<string> { "ID1" };
            predicate = x => x.InternalId == submissionIds.FirstOrDefault();

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where,
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( a.`InternalId` = @_InternalId_1_ )", result);
            Assert.Equal(submissionIds.FirstOrDefault(), queryParameters["@_InternalId_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_String_IsNotNull()
        {
            // Arrange
            predicate = x => !string.IsNullOrWhiteSpace(x.TestColumnNoAttributeArguments);

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where,
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( a.`Test_Column_No_Attribute_Arguments` IS NOT NULL )", result);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_String_IsNull()
        {
            // Arrange
            predicate = x => string.IsNullOrWhiteSpace(x.TestColumnNoAttributeArguments);

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where,
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( a.`Test_Column_No_Attribute_Arguments` IS NULL )", result);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_CompoundBuild_NotEquals()
        {
            // Arrange
            var expectedIds = new List<ComplexTestModel>
            { 
                new ComplexTestModel { Id = 1 },
                new ComplexTestModel { Id = 2 },
                new ComplexTestModel { Id = 3 },
                new ComplexTestModel { Id = 4 },
            };
            var expectedInternalId = "00000000-0000-0000-0000-000000000000";

            predicate = x => expectedIds.Select(y => y.Id).Contains(x.Id) && x.TestColumnNoAttributeArguments != expectedInternalId;

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where,
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( FIND_IN_SET(a.`Id`, @_Id_1_)    AND  a.`Test_Column_No_Attribute_Arguments` <> @_TestColumnNoAttributeArguments_1_ )", result);
            Assert.Equal(string.Join(",", expectedIds.Select(x => x.Id)), string.Join(",", queryParameters["@_Id_1_"]));
            Assert.Equal(expectedInternalId, queryParameters["@_TestColumnNoAttributeArguments_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_CompoundBuildWithMultipleLayers_NotEquals()
        {
            // Arrange
            var keyData = new List<long[]>
            {
                new long[] { 2 },
            };
            var expectedIds = new List<ComplexTestModel>
            { 
                new ComplexTestModel { InternalId = "1" },
                new ComplexTestModel { InternalId = "2" },
                new ComplexTestModel { InternalId = "3" },
                new ComplexTestModel { InternalId = "4" },
            };
            var expectedInternalId = "00000000-0000-0000-0000-000000000000";

            predicate = x => keyData.All(y => y.Select(z => z.ToString()).ToList().Contains(x.InternalId));

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where,
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( FIND_IN_SET(a.`Id`, @_Id_1_)    AND  a.`Test_Column_No_Attribute_Arguments` <> @_TestColumnNoAttributeArguments_1_ )", result);
            Assert.Equal(string.Join(",", expectedIds.Select(x => x.Id)), string.Join(",", queryParameters["@_Id_1_"]));
            Assert.Equal(expectedInternalId, queryParameters["@_TestColumnNoAttributeArguments_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_CompoundBuild_Select_NotNull()
        {
            // Arrange
            var expectedIds = new List<ComplexTestModel>
            { 
                new ComplexTestModel { Id = 1 },
                new ComplexTestModel { Id = 2 },
                new ComplexTestModel { Id = 3 },
                new ComplexTestModel { Id = 4 },
            };

            predicate = x => expectedIds.Select(y => y.Id).Contains(x.Id) && !string.IsNullOrWhiteSpace(x.TestColumnNoAttributeArguments);

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where,
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( FIND_IN_SET(a.`Id`, @_Id_1_)    AND  a.`Test_Column_No_Attribute_Arguments` IS NOT NULL )", result);
            Assert.Equal(string.Join(",", expectedIds.Select(x => x.Id)), string.Join(",", queryParameters["@_Id_1_"]));
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_CompoundBuild_NotNull_Select()
        {
            // Arrange
            var expectedIds = new List<ComplexTestModel>
            { 
                new ComplexTestModel { Id = 1 },
                new ComplexTestModel { Id = 2 },
                new ComplexTestModel { Id = 3 },
                new ComplexTestModel { Id = 4 },
            };

            predicate = x => !string.IsNullOrWhiteSpace(x.TestColumnNoAttributeArguments) && expectedIds.Select(y => y.Id).Contains(x.Id);

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where,
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( a.`Test_Column_No_Attribute_Arguments` IS NOT NULL    AND  FIND_IN_SET(a.`Id`, @_Id_1_) )", result);
            Assert.Equal(string.Join(",", expectedIds.Select(x => x.Id)), string.Join(",", queryParameters["@_Id_1_"]));
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_CompareEnumWithVariable_Select()
        {
            // Arrange
            var whereType = WhereTypes.WhereType1;

            predicate = x => x.WhereTypeProperty == whereType;

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(ExpressionEvaluator.Command.Where, new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) }), queryParameters);

            // Assert
            Assert.Equal(" WHERE ( a.`Where_Type_Property` = @_WhereTypeProperty_1_ )", result);
            Assert.Equal(whereType.ToString(), queryParameters["@_WhereTypeProperty_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_CompareEnumWithConstant_Select()
        {
            // Arrange
            predicate = x => x.WhereTypeProperty == WhereTypes.WhereType1;

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(ExpressionEvaluator.Command.Where, new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) }), queryParameters);

            // Assert
            Assert.Equal(" WHERE ( a.`Where_Type_Property` = @_WhereTypeProperty_1_ )", result);
            Assert.Equal(WhereTypes.WhereType1.ToString(), queryParameters["@_WhereTypeProperty_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_CallExpressionRight_Select()
        {
            // Arrange
            var compareModels = new ComplexTestModel[]
            {
                new ComplexTestModel { Id = 0, InternalId = "TEST1" },
                new ComplexTestModel { Id = 1, InternalId = "TEST2" }
            }
            .ToDictionary(x => x.Id ?? 0, x => x);
            var modelCount = 0;

            predicate = x => x.InternalId == compareModels[modelCount].InternalId;

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(ExpressionEvaluator.Command.Where, new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) }), queryParameters);

            // Assert
            Assert.Equal(" WHERE ( a.`InternalId` = @_InternalId_1_ )", result);
            Assert.Equal(compareModels[modelCount].InternalId, queryParameters["@_InternalId_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_CallExpressionLeft_Select()
        {
            // Arrange
            var compareModels = new ComplexTestModel[]
            {
                new ComplexTestModel { Id = 0, InternalId = "TEST1" },
                new ComplexTestModel { Id = 1, InternalId = "TEST2" }
            }
            .ToDictionary(x => x.Id ?? 0, x => x);
            var modelCount = 0L;

            predicate = x => compareModels[modelCount].InternalId == x.InternalId;

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(ExpressionEvaluator.Command.Where, new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) }), queryParameters);

            // Assert
            Assert.Equal(" WHERE ( a.`InternalId` = @_InternalId_1_ )", result);
            Assert.Equal(compareModels[modelCount].InternalId, queryParameters["@_InternalId_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_AnyTwoComparisons()
        {
            var objectList = new List<ComplexTestModel>
            {
                new ComplexTestModel { TestColumnInternalId = "00000000-0000-0000-0000-000000000000", TestColumnNoAttributeArguments = "0" },
                new ComplexTestModel { TestColumnInternalId = "00000000-0000-0000-0000-000000000001", TestColumnNoAttributeArguments = "1" }
            };

            var objectCompareList = new string[][]
            {
                new string[] { "00000000-0000-0000-0000-000000000001", "1" }
            };

            // Arrange
            predicate = x => objectCompareList.Any(y => x.TestColumnInternalId == y[0] && x.TestColumnNoAttributeArguments == y[1]);

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where,
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( FIND_IN_SET(a.`InternalId`, @_InternalId_1_) )", result);
            Assert.Equal("00000000-0000-0000-0000-000000000000,00000000-0000-0000-0000-000000000001", queryParameters["@_InternalId_1_"]);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_AnyTwoComparisons_Hack()
        {
            var objectList = new List<ComplexTestModel>
            {
                new ComplexTestModel { TestColumnInternalId = "00000000-0000-0000-0000-000000000000", TestColumnNoAttributeArguments = "0" },
                new ComplexTestModel { TestColumnInternalId = "00000000-0000-0000-0000-000000000001", TestColumnNoAttributeArguments = "1" }
            };

            var objectCompareList = new string[][]
            {
                new string[] { "00000000-0000-0000-0000-000000000001", "1" }
            };

            var objectCompareListKeys = objectCompareList.Select(x => string.Join(string.Empty, x)).ToArray();

            // Arrange
            predicate = x => objectCompareListKeys.Contains(x.TestColumnInternalId + x.TestColumnNoAttributeArguments);

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Where,
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Where, predicate) })
                , queryParameters);

            // Assert
            Assert.Equal(" WHERE ( FIND_IN_SET(a.`InternalId`, @_InternalId_1_) )", result);
            Assert.Equal("00000000-0000-0000-0000-000000000000,00000000-0000-0000-0000-000000000001", queryParameters["@_InternalId_1_"]);
        }
    }
}
