using Moq;
using SimpleRelm.Interfaces;
using SimpleRelm.Models;
using SimpleRelm.RelmInternal.Helpers.DataTransfer;
using CoreRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.RelmInternal.Helpers.DataTransfer
{
    public class CustomDataLoader_Tester
    {
        private readonly ComplexTestContext context;

        public CustomDataLoader_Tester()
        {
            // dummy data
            var mockComplexTestModels = new List<ComplexTestModel>
            {
                new ComplexTestModel
                {
                    InternalId = "ID1",
                    TestFieldBoolean = null,
                    TestFieldBooleans = null,
                },
                new ComplexTestModel
                {
                    InternalId = "ID2",
                    TestFieldBoolean = null,
                    TestFieldBooleans = null,
                },
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
        public void FieldLoaderAttribute_DefaultRelmKey_UsedToResolveProperty_SingleReturn()
        {
            // Arrange & Act
            context.ComplexTestModels!.Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();
            var secondModel = context.ComplexTestModels.Skip(1).First();

            Assert.True(firstModel?.TestFieldBoolean);
            Assert.False(secondModel?.TestFieldBoolean);
        }

        [Fact]
        public void FieldLoaderAttribute_DefaultRelmKey_UsedToResolveProperty_ListReturn()
        {
            // Arrange & Act
            context.ComplexTestModels!.Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();
            var secondModel = context.ComplexTestModels.Skip(1).First();

            Assert.Equal(4, firstModel?.TestFieldBooleans?.Count);
            Assert.Equal(4, secondModel?.TestFieldBooleans?.Count);

            // true, false, true, false
            Assert.True(firstModel?.TestFieldBooleans?.FirstOrDefault());
            Assert.False(firstModel?.TestFieldBooleans?.Skip(1).FirstOrDefault());
            Assert.True(firstModel?.TestFieldBooleans?.Skip(2).FirstOrDefault());
            Assert.False(firstModel?.TestFieldBooleans?.Skip(3).FirstOrDefault());

            Assert.True(secondModel?.TestFieldBooleans?.FirstOrDefault());
            Assert.False(secondModel?.TestFieldBooleans?.Skip(1).FirstOrDefault());
            Assert.True(secondModel?.TestFieldBooleans?.Skip(2).FirstOrDefault());
            Assert.False(secondModel?.TestFieldBooleans?.Skip(3).FirstOrDefault());
        }

        [Fact]
        public void DataLoaderAttribute_IsSuccessful()
        {
            // Arrange
            context.DataLoaderTestModels!.Load();

            // Assert
            var firstModel = context.DataLoaderTestModels.First();
            var secondModel = context.DataLoaderTestModels.Skip(1).First();

            Assert.Equal("LOADER1", firstModel?.InternalId);
            Assert.Equal("LOADER2", secondModel?.InternalId);
        }
    }
}
