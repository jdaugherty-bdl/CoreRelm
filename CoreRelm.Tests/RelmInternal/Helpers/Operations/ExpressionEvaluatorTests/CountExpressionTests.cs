using Moq;
using SimpleRelm.Attributes;
using SimpleRelm.Models;
using SimpleRelm.RelmInternal.Helpers.DataTransfer;
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
    public class CountExpressionTests
    {
        private ComplexTestContext context;

        public CountExpressionTests()
        {
            // dummy data
            var mockComplexTestModels = new List<ComplexTestModel>
            {
                new ComplexTestModel { InternalId = "ID1" },
                new ComplexTestModel { InternalId = "ID2" },
            };

            context = new ComplexTestContext("name=SimpleRelmMySql");

            // create dummy data loaders for dummy data to be placed in both relevant data sets
            var modelDataLoader = new Mock<RelmDefaultDataLoader<ComplexTestModel>>(); // { CallBase = true };

            // make sure GetLoadData() calls base so LastExecutedCommands (required for references) gets populated
            modelDataLoader.Setup(x => x.TableName).Returns("DUMMY NAME");
            modelDataLoader.Setup(x => x.GetLoadData()).CallBase();
            modelDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexTestModels);

            context.ComplexTestModels!.SetDataLoader(modelDataLoader.Object);
        }

        [Fact]
        public void Get_Count_Should_Return_2()
        {
            // Arrange & Act
            var modelsCount = context.ComplexTestModels!.Load().Count();

            // Assert
            Assert.Equal(2, modelsCount);
        }
    }
}
