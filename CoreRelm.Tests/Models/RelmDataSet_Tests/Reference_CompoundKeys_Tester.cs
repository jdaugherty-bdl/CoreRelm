using Moq;
using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.RelmInternal.Helpers.DataTransfer;
using CoreRelm.Tests.Interfaces;
using CoreRelm.Tests.TestModels;
using CoreRelm.Tests.TestModels.MultipleKeys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CoreRelm.Extensions;
using CoreRelm.Options;

namespace CoreRelm.Tests.Models.RelmDataSet_Tests
{
    [Collection("JsonConfiguration")]
    public class Reference_CompoundKeys_Tester : IClassFixture<JsonConfigurationFixture>
    {
        private readonly IConfiguration _configuration;
        private readonly MultipleKeysTestContext context;

        public Reference_CompoundKeys_Tester(JsonConfigurationFixture fixture) 
        {
            _configuration = fixture.Configuration;

            // dummy data
            var mockComplexTestModels = new List<MultipleKeysTestObject?>
            {
                new() { 
                    InternalId = "ID1", 
                    MultipleKeysReferenceObjectLocalKey1 = "LOCALKEY1",
                    MultipleKeysReferenceObjectLocalKey2 = "LOCALKEY2",
                    MultipleKeysReferenceObject_ForeignKeys = null,
                    MultipleKeysReferenceObject_ForeignKey_Item = null,
                    MultipleKeysReferenceObject_NavigationProperties = null,
                    MultipleKeysReferenceObject_NavigationProperty_Item = null,
                    MultipleKeysReferenceObject_PrincipalEntities = null,
                    MultipleKeysReferenceObject_PrincipalEntity_Item = null,
                },
                new() { 
                    InternalId = "ID2",
                    MultipleKeysReferenceObjectLocalKey1 = "LOCALKEY3",
                    MultipleKeysReferenceObjectLocalKey2 = "LOCALKEY4",
                    MultipleKeysReferenceObject_ForeignKeys = null,
                    MultipleKeysReferenceObject_ForeignKey_Item = null,
                    MultipleKeysReferenceObject_NavigationProperties = null,
                    MultipleKeysReferenceObject_NavigationProperty_Item = null,
                    MultipleKeysReferenceObject_PrincipalEntities = null,
                    MultipleKeysReferenceObject_PrincipalEntity_Item = null,
                },
            };

            new ServiceCollection().AddCoreRelm(_configuration);
            context = new RelmContextOptionsBuilder("name=SimpleRelmMySql")
                .SetAutoOpenConnection(false)
                .SetAutoInitializeDataSets(false)
                .SetAutoVerifyTables(false)
                .Build<MultipleKeysTestContext>()
                ?? throw new InvalidOperationException("Failed to build MultipleKeysTestContext");

            // create dummy data loaders for dummy data to be placed in both relevant data sets
            var modelDataLoader = new Mock<RelmDefaultDataLoader<MultipleKeysTestObject>>(context);

            // make sure GetLoadData() calls base so LastExecutedCommands (required for references) gets populated
            modelDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            modelDataLoader.Setup(x => x.GetLoadDataAsync(It.IsAny<CancellationToken>())).CallBase();
            modelDataLoader.Setup(x => x.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockComplexTestModels);
            
            context.GetDataSet<MultipleKeysTestObject>()?.SetDataLoader(modelDataLoader.Object);
        }

        private void SetupReferenceDataLoader(bool addSecondId)
        {
            var mockComplexReferenceObjects_ForeignKey = new List<MultipleKeysReferenceObject_ForeignKey?>
            {
                new() { ReferenceKey1 = "LOCALKEY1", ReferenceKey2 = "LOCALKEY2", MultipleKeysTestObject_Reference = null },
                new() { ReferenceKey1 = "LOCALKEY3", ReferenceKey2 = "LOCALKEY4", MultipleKeysTestObject_Reference = null },
            };

            if (addSecondId)
                mockComplexReferenceObjects_ForeignKey.Add(new MultipleKeysReferenceObject_ForeignKey { ReferenceKey1 = "LOCALKEY1", ReferenceKey2 = "LOCALKEY2", MultipleKeysTestObject_Reference = null });

            var referenceDataLoader = new Mock<RelmDefaultDataLoader<MultipleKeysReferenceObject_ForeignKey>>(context);
            referenceDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            referenceDataLoader.Setup(x => x.GetLoadDataAsync(It.IsAny<CancellationToken>())).CallBase();
            referenceDataLoader.Setup(x => x.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockComplexReferenceObjects_ForeignKey);

            context.GetDataSet<MultipleKeysReferenceObject_ForeignKey>()?.SetDataLoader(referenceDataLoader.Object);
        }

        private void SetupNavigationDataLoader(bool addSecondId)
        {
            var mockComplexReferenceObjects_Navigation = new List<MultipleKeysReferenceObject_NavigationProperty?>
            {
                new() { ReferenceKey1 = "LOCALKEY1", ReferenceKey2 = "LOCALKEY2", MultipleKeysTestObject_Reference = null },
                new() { ReferenceKey1 = "LOCALKEY3", ReferenceKey2 = "LOCALKEY4", MultipleKeysTestObject_Reference = null },
            };

            if (addSecondId)
                mockComplexReferenceObjects_Navigation.Add(new MultipleKeysReferenceObject_NavigationProperty { ReferenceKey1 = "LOCALKEY1", ReferenceKey2 = "LOCALKEY2", MultipleKeysTestObject_Reference = null });

            var navigationDataLoader = new Mock<RelmDefaultDataLoader<MultipleKeysReferenceObject_NavigationProperty>>(context);
            navigationDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            navigationDataLoader.Setup(x => x.GetLoadDataAsync(It.IsAny<CancellationToken>())).CallBase();
            navigationDataLoader.Setup(x => x.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockComplexReferenceObjects_Navigation);

            context.GetDataSet<MultipleKeysReferenceObject_NavigationProperty>()?.SetDataLoader(navigationDataLoader.Object);
        }

        private void SetupPrincipalDataLoader(bool addSecondId)
        {
            var mockComplexReferenceObjects_Principal = new List<MultipleKeysReferenceObject_PrincipalEntity?>
            {
                new() { ReferenceKey1 = "LOCALKEY1", ReferenceKey2 = "LOCALKEY2", MultipleKeysTestObject_Reference = null },
                new() { ReferenceKey1 = "LOCALKEY3", ReferenceKey2 = "LOCALKEY4", MultipleKeysTestObject_Reference = null },
            };

            if (addSecondId)
                mockComplexReferenceObjects_Principal.Add(new() { ReferenceKey1 = "LOCALKEY1", ReferenceKey2 = "LOCALKEY2", MultipleKeysTestObject_Reference = null });

            var principalDataLoader = new Mock<RelmDefaultDataLoader<MultipleKeysReferenceObject_PrincipalEntity>>(context);
            principalDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            principalDataLoader.Setup(x => x.GetLoadDataAsync(It.IsAny<CancellationToken>())).CallBase();
            principalDataLoader.Setup(x => x.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockComplexReferenceObjects_Principal);

            context.GetDataSet<MultipleKeysReferenceObject_PrincipalEntity>()?.SetDataLoader(principalDataLoader.Object);
        }

        [Fact]
        public void Reference_LoadsForeignKeyObjectsCorrectly()
        {
            // Arrange
            SetupReferenceDataLoader(true);

            // Act
            context.GetDataSet<MultipleKeysTestObject>()?.Reference(x => x.MultipleKeysReferenceObject_ForeignKeys).Load();

            // Assert
            var firstModel = context.MultipleKeysTestObjects?.First();
            var secondModel = context.MultipleKeysTestObjects?.Skip(1).First();

            Assert.NotNull(firstModel?.MultipleKeysReferenceObject_ForeignKeys);
            Assert.NotNull(secondModel?.MultipleKeysReferenceObject_ForeignKeys);

            Assert.True(firstModel.MultipleKeysReferenceObject_ForeignKeys.Count != 0);
            Assert.True(secondModel.MultipleKeysReferenceObject_ForeignKeys.Count != 0);

            Assert.Equal(2, firstModel.MultipleKeysReferenceObject_ForeignKeys.Count);
            Assert.Single(secondModel.MultipleKeysReferenceObject_ForeignKeys);

            Assert.Equal(firstModel.MultipleKeysReferenceObjectLocalKey1, firstModel.MultipleKeysReferenceObject_ForeignKeys?.FirstOrDefault()?.ReferenceKey1);
            Assert.Equal(firstModel.MultipleKeysReferenceObjectLocalKey2, firstModel.MultipleKeysReferenceObject_ForeignKeys?.FirstOrDefault()?.ReferenceKey2);

            Assert.Equal(firstModel.MultipleKeysReferenceObjectLocalKey1, firstModel.MultipleKeysReferenceObject_ForeignKeys?.Skip(1).FirstOrDefault()?.ReferenceKey1);
            Assert.Equal(firstModel.MultipleKeysReferenceObjectLocalKey2, firstModel.MultipleKeysReferenceObject_ForeignKeys?.Skip(1).FirstOrDefault()?.ReferenceKey2);

            Assert.Equal(secondModel.MultipleKeysReferenceObjectLocalKey1, secondModel.MultipleKeysReferenceObject_ForeignKeys?.FirstOrDefault()?.ReferenceKey1);
            Assert.Equal(secondModel.MultipleKeysReferenceObjectLocalKey2, secondModel.MultipleKeysReferenceObject_ForeignKeys?.FirstOrDefault()?.ReferenceKey2);
        }

        [Fact]
        public void Reference_LoadsForeignKeyObjectCorrectly()
        {
            // Arrange
            SetupReferenceDataLoader(false);

            // Act
            context.GetDataSet<MultipleKeysTestObject>()?.Reference(x => x.MultipleKeysReferenceObject_ForeignKey_Item).Load();

            // Assert
            var firstModel = context.MultipleKeysTestObjects?.First();

            Assert.NotNull(firstModel?.MultipleKeysReferenceObject_ForeignKey_Item);
            Assert.Equal(firstModel?.MultipleKeysReferenceObjectLocalKey1, firstModel?.MultipleKeysReferenceObject_ForeignKey_Item?.ReferenceKey1);
            Assert.Equal(firstModel?.MultipleKeysReferenceObjectLocalKey2, firstModel?.MultipleKeysReferenceObject_ForeignKey_Item?.ReferenceKey2);
        }

        [Fact]
        public void Reference_LoadsNavigationPropertyObjectsCorrectly()
        {
            // Arrange
            SetupNavigationDataLoader(true);

            // Act
            context.GetDataSet<MultipleKeysTestObject>()?.Reference(x => x.MultipleKeysReferenceObject_NavigationProperties).Load();

            // Assert
            var firstModel = context.MultipleKeysTestObjects?.First();
            var secondModel = context.MultipleKeysTestObjects?.Skip(1).First();

            Assert.NotNull(firstModel?.MultipleKeysReferenceObject_NavigationProperties);
            Assert.NotNull(secondModel?.MultipleKeysReferenceObject_NavigationProperties);

            Assert.True(firstModel.MultipleKeysReferenceObject_NavigationProperties.Count != 0);
            Assert.True(secondModel.MultipleKeysReferenceObject_NavigationProperties.Count != 0);

            Assert.Equal(2, firstModel.MultipleKeysReferenceObject_NavigationProperties.Count);
            Assert.Single(secondModel.MultipleKeysReferenceObject_NavigationProperties);

            Assert.Equal(firstModel.MultipleKeysReferenceObjectLocalKey1, firstModel.MultipleKeysReferenceObject_NavigationProperties?.FirstOrDefault()?.ReferenceKey1);
            Assert.Equal(firstModel.MultipleKeysReferenceObjectLocalKey2, firstModel.MultipleKeysReferenceObject_NavigationProperties?.FirstOrDefault()?.ReferenceKey2);

            Assert.Equal(firstModel.MultipleKeysReferenceObjectLocalKey1, firstModel.MultipleKeysReferenceObject_NavigationProperties?.Skip(1).FirstOrDefault()?.ReferenceKey1);
            Assert.Equal(firstModel.MultipleKeysReferenceObjectLocalKey2, firstModel.MultipleKeysReferenceObject_NavigationProperties?.Skip(1).FirstOrDefault()?.ReferenceKey2);

            Assert.Equal(secondModel.MultipleKeysReferenceObjectLocalKey1, secondModel.MultipleKeysReferenceObject_NavigationProperties?.FirstOrDefault()?.ReferenceKey1);
            Assert.Equal(secondModel.MultipleKeysReferenceObjectLocalKey2, secondModel.MultipleKeysReferenceObject_NavigationProperties?.FirstOrDefault()?.ReferenceKey2);
        }

        [Fact]
        public void Reference_LoadsNavigationPropertyObjectCorrectly()
        {
            // Arrange
            SetupNavigationDataLoader(false);

            // Act
            context.GetDataSet<MultipleKeysTestObject>()?.Reference(x => x.MultipleKeysReferenceObject_NavigationProperty_Item).Load();

            // Assert
            var firstModel = context.MultipleKeysTestObjects?.First();

            Assert.NotNull(firstModel?.MultipleKeysReferenceObject_NavigationProperty_Item);
            Assert.Equal(firstModel?.MultipleKeysReferenceObjectLocalKey1, firstModel?.MultipleKeysReferenceObject_NavigationProperty_Item?.ReferenceKey1);
            Assert.Equal(firstModel?.MultipleKeysReferenceObjectLocalKey2, firstModel?.MultipleKeysReferenceObject_NavigationProperty_Item?.ReferenceKey2);
        }

        [Fact]
        public void Reference_LoadsPrincipalEntityObjectsCorrectly()
        {
            // Arrange
            SetupPrincipalDataLoader(true);

            // Act
            context.GetDataSet<MultipleKeysTestObject>()?.Reference(x => x.MultipleKeysReferenceObject_PrincipalEntities).Load();

            // Assert
            var firstModel = context.MultipleKeysTestObjects?.First();
            var secondModel = context.MultipleKeysTestObjects?.Skip(1).First();

            Assert.NotNull(firstModel?.MultipleKeysReferenceObject_PrincipalEntities);
            Assert.NotNull(secondModel?.MultipleKeysReferenceObject_PrincipalEntities);

            Assert.True(firstModel.MultipleKeysReferenceObject_PrincipalEntities.Count != 0);
            Assert.True(secondModel.MultipleKeysReferenceObject_PrincipalEntities.Count != 0);

            Assert.Equal(2, firstModel.MultipleKeysReferenceObject_PrincipalEntities.Count);
            Assert.Single(secondModel.MultipleKeysReferenceObject_PrincipalEntities);

            Assert.Equal(firstModel.MultipleKeysReferenceObjectLocalKey1, firstModel.MultipleKeysReferenceObject_PrincipalEntities?.FirstOrDefault()?.ReferenceKey1);
            Assert.Equal(firstModel.MultipleKeysReferenceObjectLocalKey2, firstModel.MultipleKeysReferenceObject_PrincipalEntities?.FirstOrDefault()?.ReferenceKey2);

            Assert.Equal(firstModel.MultipleKeysReferenceObjectLocalKey1, firstModel.MultipleKeysReferenceObject_PrincipalEntities?.Skip(1).FirstOrDefault()?.ReferenceKey1);
            Assert.Equal(firstModel.MultipleKeysReferenceObjectLocalKey2, firstModel.MultipleKeysReferenceObject_PrincipalEntities?.Skip(1).FirstOrDefault()?.ReferenceKey2);

            Assert.Equal(secondModel.MultipleKeysReferenceObjectLocalKey1, secondModel.MultipleKeysReferenceObject_PrincipalEntities?.FirstOrDefault()?.ReferenceKey1);
            Assert.Equal(secondModel.MultipleKeysReferenceObjectLocalKey2, secondModel.MultipleKeysReferenceObject_PrincipalEntities?.FirstOrDefault()?.ReferenceKey2);
        }

        [Fact]
        public void Reference_LoadsPrincipalEntityObjectCorrectly()
        {
            // Arrange
            SetupPrincipalDataLoader(false);

            // Act
            context.MultipleKeysTestObjects!.Reference(x => x.MultipleKeysReferenceObject_PrincipalEntity_Item).Load();

            // Assert
            var firstModel = context.MultipleKeysTestObjects.First();

            Assert.NotNull(firstModel.MultipleKeysReferenceObject_PrincipalEntity_Item);
            Assert.Equal(firstModel.MultipleKeysReferenceObjectLocalKey1, firstModel.MultipleKeysReferenceObject_PrincipalEntity_Item?.ReferenceKey1);
            Assert.Equal(firstModel.MultipleKeysReferenceObjectLocalKey2, firstModel.MultipleKeysReferenceObject_PrincipalEntity_Item?.ReferenceKey2);
        }
    }
}
