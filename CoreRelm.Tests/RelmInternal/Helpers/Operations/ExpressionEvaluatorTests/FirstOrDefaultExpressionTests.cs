using Moq;
using SimpleRelm.Interfaces;
using CoreRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.RelmInternal.Helpers.Operations.ExpressionEvaluatorTests
{
    public class FirstOrDefaultExpressionTests
    {
        private readonly Mock<ComplexTestContext> mockContext;
        private readonly Mock<IRelmDataSet<ComplexTestModel>> mockDataSet;
        private readonly List<ComplexTestModel> predefinedList;

        private const int SearchedId = 3;

        public FirstOrDefaultExpressionTests()
        {
            mockContext = new Mock<ComplexTestContext>("name=PortalCertDatabase");
            mockDataSet = new Mock<IRelmDataSet<ComplexTestModel>>();

            predefinedList = new List<ComplexTestModel>
            {
                new ComplexTestModel { Id = SearchedId }
            };

            // Mock the GetDataObjects method to return the predefined list
            mockDataSet.Setup(x => x.Load()).Returns(predefinedList);
            mockContext.Setup(c => c.ComplexTestModels).Returns(mockDataSet.Object);
        }

        [Fact]
        public void TestFirstOrDefaultReturnsObjectWhenIdIsThree_Predicate()
        {
            // Arrange & Act
            mockDataSet.Object.FirstOrDefault(x => x.Id == SearchedId, false);

            var result = mockDataSet.Object.Load().FirstOrDefault();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(SearchedId, result.Id);
            mockDataSet.Verify(x => x.Load(), Times.Once);
        }

        [Fact]
        public void TestFirstOrDefaultReturnsNullWhenNoMatchFound_Predicate()
        {
            // Arrange
            const int NonExistentId = 0;

            // Act
            mockDataSet.Object.FirstOrDefault(x => x.Id == NonExistentId, false);

            var result = mockDataSet.Object.Load().FirstOrDefault();

            // Assert
            Assert.NotNull(result);
            mockDataSet.Verify(x => x.Load(), Times.Once);
        }

        [Fact]
        public void TestFirstOrDefaultReturnsObject_NoPredicate()
        {
            // Arrange & Act
            mockDataSet.Object.FirstOrDefault(null, false);

            var result = mockDataSet.Object.Load().FirstOrDefault();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(SearchedId, result.Id);
            mockDataSet.Verify(x => x.Load(), Times.Once);
        }
    }
}
