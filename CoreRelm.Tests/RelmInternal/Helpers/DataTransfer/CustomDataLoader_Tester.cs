using Moq;
using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.RelmInternal.Helpers.DataTransfer;
using CoreRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CoreRelm.Extensions;
using CoreRelm.Options;

namespace CoreRelm.Tests.RelmInternal.Helpers.DataTransfer
{
    [Collection("JsonConfiguration")]
    public class CustomDataLoader_Tester : IClassFixture<JsonConfigurationFixture>
    {
        private readonly IConfiguration _configuration;
        private readonly ComplexTestContext context;

        public CustomDataLoader_Tester(JsonConfigurationFixture fixture)
        {
            _configuration = fixture.Configuration;

            // dummy data
            var mockComplexTestModels = new List<ComplexTestModel>
            {
                new() 
                {
                    InternalId = "ID1",
                    TestFieldBoolean = null,
                    TestFieldBooleans = null,
                },
                new() 
                {
                    InternalId = "ID2",
                    TestFieldBoolean = null,
                    TestFieldBooleans = null,
                },
            };

            new ServiceCollection().AddCoreRelm(_configuration);
            context = new RelmContextOptionsBuilder()
                .SetAutoVerifyTables(false)
                .Build<ComplexTestContext>()
                ?? throw new InvalidOperationException("Failed to build ComplexTestContext");

            // create dummy data loaders for dummy data to be placed in both relevant data sets
            var modelDataLoader = new Mock<RelmDefaultDataLoader<ComplexTestModel>>(context);

            // make sure GetLoadData() calls base so LastExecutedCommands (required for references) gets populated
            modelDataLoader.Setup(x => x.TableName).Returns("DUMMY NAME");
            modelDataLoader.Setup(x => x.GetLoadDataAsync(It.IsAny<CancellationToken>())).CallBase();
            modelDataLoader.Setup(x => x.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockComplexTestModels);

            context.GetDataSet<ComplexTestModel>()?.SetDataLoader(modelDataLoader.Object);
        }

        [Fact]
        public void FieldLoaderAttribute_DefaultRelmKey_UsedToResolveProperty_SingleReturn()
        {
            // Arrange & Act
            context.GetDataSet<ComplexTestModel>()?.Load();

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
            context.GetDataSet<ComplexTestModel>()?.Load();

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
            context.GetDataSet<ComplexTestModel>()?.Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();
            var secondModel = context.ComplexTestModels.Skip(1).First();

            Assert.Equal("ID1", firstModel?.InternalId);
            Assert.Equal("ID2", secondModel?.InternalId);
        }
    }
}
