using CoreRelm.Extensions;
using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.Tests.TestModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests
{
    [Collection("JsonConfiguration")]
    public class RelmHelper_Tester : IClassFixture<JsonConfigurationFixture>
    {
        private readonly IConfiguration _configuration;
        private List<ComplexTestModel>? mockComplexTestModels;

        public RelmHelper_Tester(JsonConfigurationFixture fixture)
        {
            _configuration = fixture.Configuration;

            SetupContext(true);
        }

        private List<ComplexTestModel> SetupContext(bool haveTwoRoots = true)
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
            ];

            if (haveTwoRoots)
                mockComplexTestModels.Add(new ComplexTestModel
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
                });

            return mockComplexTestModels;
        }

        private Mock<RelmDefaultDataLoader<ComplexReferenceObject>> SetupSingleReturnReferenceDataLoader(IRelmContext relmContext, bool addSecondId, bool haveTwoRoots)
        {
            var mockComplexReferenceObjects = new List<ComplexReferenceObject>
            {
                new ComplexReferenceObject { ComplexTestModelInternalId = "ID1", TestModel = null },
            };

            if (haveTwoRoots)
                mockComplexReferenceObjects.Add(new ComplexReferenceObject { ComplexTestModelInternalId = "ID2", TestModel = null });

            if (addSecondId)
                mockComplexReferenceObjects.Add(new ComplexReferenceObject { ComplexTestModelInternalId = "ID1", TestModel = null });

            var referenceDataLoader = new Mock<RelmDefaultDataLoader<ComplexReferenceObject>>(relmContext);
            referenceDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            referenceDataLoader.Setup(x => x.GetLoadDataAsync(It.IsAny<CancellationToken>())).CallBase();
            referenceDataLoader.Setup(x => x.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockComplexReferenceObjects);

            var services = new ServiceCollection();
            services.AddCoreRelm(_configuration);

            return referenceDataLoader;
        }

        private Mock<RelmDefaultDataLoader<ComplexReferenceObject>> SetupReferenceDataLoader(IRelmContext relmContext, bool addSecondId)
        {
            var mockComplexReferenceObjects = new List<ComplexReferenceObject>
            {
                new ComplexReferenceObject { ComplexTestModelInternalId = "ID1", TestModel = null },
                new ComplexReferenceObject { ComplexTestModelInternalId = "ID2", TestModel = null },
            };

            if (addSecondId)
                mockComplexReferenceObjects.Add(new ComplexReferenceObject { ComplexTestModelInternalId = "ID1", TestModel = null });

            var referenceDataLoader = new Mock<RelmDefaultDataLoader<ComplexReferenceObject>>(relmContext);
            referenceDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            referenceDataLoader.Setup(x => x.GetLoadDataAsync(It.IsAny<CancellationToken>())).CallBase();
            referenceDataLoader.Setup(x => x.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockComplexReferenceObjects);

            var services = new ServiceCollection();
            services.AddCoreRelm(_configuration);

            return referenceDataLoader;
        }

        private Mock<RelmDefaultDataLoader<ComplexReferenceObject_NavigationProperty>> SetupNavigationDataLoader(IRelmContext relmContext, bool addSecondId)
        {
            var mockComplexReferenceObjects_Navigation = new List<ComplexReferenceObject_NavigationProperty>
            {
                new ComplexReferenceObject_NavigationProperty { ComplexTestModelInternalId = "ID1", TestModel = null },
                new ComplexReferenceObject_NavigationProperty { ComplexTestModelInternalId = "ID2", TestModel = null },
            };

            if (addSecondId)
                mockComplexReferenceObjects_Navigation.Add(new ComplexReferenceObject_NavigationProperty { ComplexTestModelInternalId = "ID1", TestModel = null });

            var navigationDataLoader = new Mock<RelmDefaultDataLoader<ComplexReferenceObject_NavigationProperty>>(relmContext);
            navigationDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            navigationDataLoader.Setup(x => x.GetLoadDataAsync(It.IsAny<CancellationToken>())).CallBase();
            navigationDataLoader.Setup(x => x.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockComplexReferenceObjects_Navigation);

            var services = new ServiceCollection();
            services.AddCoreRelm(_configuration);

            return navigationDataLoader;
        }

        private Mock<RelmDefaultDataLoader<ComplexReferenceObject_PrincipalEntity>> SetupPrincipalDataLoader(IRelmContext relmContext, bool addSecondId)
        {
            var mockComplexReferenceObjects_Principal = new List<ComplexReferenceObject_PrincipalEntity>
            {
                new ComplexReferenceObject_PrincipalEntity { ComplexTestModelInternalId = "ID1", TestModel = null },
                new ComplexReferenceObject_PrincipalEntity { ComplexTestModelInternalId = "ID2", TestModel = null },
            };

            if (addSecondId)
                mockComplexReferenceObjects_Principal.Add(new ComplexReferenceObject_PrincipalEntity { ComplexTestModelInternalId = "ID1", TestModel = null });

            var principalDataLoader = new Mock<RelmDefaultDataLoader<ComplexReferenceObject_PrincipalEntity>>(relmContext);
            principalDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            principalDataLoader.Setup(x => x.GetLoadDataAsync(It.IsAny<CancellationToken>())).CallBase();
            principalDataLoader.Setup(x => x.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockComplexReferenceObjects_Principal);

            var services = new ServiceCollection();
            services.AddCoreRelm(_configuration);

            return principalDataLoader;
        }

        private Mock<RelmDefaultDataLoader<ComplexReferenceObject_PrincipalEntity>> SetupPrincipalDataLoaderLocalKey(IRelmContext relmContext, bool addSecondId)
        {
            var mockComplexReferenceObjects_Principal = new List<ComplexReferenceObject_PrincipalEntity>
            {
                new ComplexReferenceObject_PrincipalEntity { ComplexTestModelLocalKey = "LOCALKEY1", TestModel = null },
                new ComplexReferenceObject_PrincipalEntity { ComplexTestModelLocalKey = "LOCALKEY2", TestModel = null },
            };

            if (addSecondId)
                mockComplexReferenceObjects_Principal.Add(new ComplexReferenceObject_PrincipalEntity { ComplexTestModelLocalKey = "LOCALKEY1", TestModel = null });

            var principalDataLoader = new Mock<RelmDefaultDataLoader<ComplexReferenceObject_PrincipalEntity>>(relmContext);
            principalDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            principalDataLoader.Setup(x => x.GetLoadDataAsync(It.IsAny<CancellationToken>())).CallBase();
            principalDataLoader.Setup(x => x.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockComplexReferenceObjects_Principal);

            var services = new ServiceCollection();
            services.AddCoreRelm(_configuration);

            return principalDataLoader;
        }

        [Fact]
        public void Reference_LoadsForeignKeyObjectsCorrectly()
        {
            // Arrange
            new ServiceCollection().AddCoreRelm(_configuration);
            var context = new ComplexTestContext(autoInitializeDataSets: false, autoVerifyTables: false);
            var modelDataLoader = SetupReferenceDataLoader(context, true);

            // Act
            var loadedResults = RelmHelper.LoadForeignKeyField(context.ContextOptions, mockComplexTestModels, x => x.ComplexReferenceObjects, modelDataLoader.Object);

            // Assert
            modelDataLoader.Verify(r => r.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()));

            var firstSource = mockComplexTestModels!.First();
            var secondSource = mockComplexTestModels!.Skip(1).First();
            var firstResult = loadedResults?.First();
            var secondResult = loadedResults?.Skip(1).First();

            Assert.Equal(firstSource.InternalId, firstResult?.InternalId);
            Assert.Equal(secondSource.InternalId, secondResult?.InternalId);

            Assert.NotNull(firstSource.ComplexReferenceObjects);
            Assert.NotNull(secondSource.ComplexReferenceObjects);
            Assert.NotNull(firstResult?.ComplexReferenceObjects);
            Assert.NotNull(secondResult?.ComplexReferenceObjects);

            Assert.True(firstResult.ComplexReferenceObjects.Count != 0);
            Assert.True(secondResult.ComplexReferenceObjects.Count != 0);

            Assert.Equal(firstSource.ComplexReferenceObjects.Count, firstResult.ComplexReferenceObjects.Count);
            Assert.Equal(secondSource.ComplexReferenceObjects.Count, secondResult.ComplexReferenceObjects.Count);

            Assert.Equal(2, firstResult.ComplexReferenceObjects.Count);
            Assert.Single(secondResult.ComplexReferenceObjects);

            Assert.Equal(firstResult.InternalId, firstResult.ComplexReferenceObjects?.FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(firstResult.InternalId, firstResult.ComplexReferenceObjects?.Skip(1).FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(secondResult.InternalId, secondResult.ComplexReferenceObjects?.FirstOrDefault()?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsForeignKeyObjectCorrectly()
        {
            // Arrange
            new ServiceCollection().AddCoreRelm(_configuration);
            var context = new ComplexTestContext(autoInitializeDataSets: false, autoVerifyTables: false);
            var modelDataLoader = SetupReferenceDataLoader(context, false);

            // Act
            var loadedResults = RelmHelper.LoadForeignKeyField(context.ContextOptions, mockComplexTestModels, x => x.ComplexReferenceObject, modelDataLoader.Object);

            // Assert
            modelDataLoader.Verify(r => r.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()));

            Assert.Equal(2, loadedResults.Count);
            
            var firstSource = mockComplexTestModels!.First();
            var secondSource = mockComplexTestModels!.Skip(1).First();
            var firstResult = loadedResults.First();
            var secondResult = loadedResults?.Skip(1).First();

            Assert.Equal(firstSource.InternalId, firstResult?.InternalId);
            Assert.Equal(secondSource.InternalId, secondResult?.InternalId);

            Assert.NotNull(firstSource.ComplexReferenceObject);
            Assert.NotNull(secondSource.ComplexReferenceObject);
            Assert.NotNull(firstResult?.ComplexReferenceObject);
            Assert.NotNull(secondResult?.ComplexReferenceObject);

            Assert.Equal(firstResult.InternalId, firstResult?.ComplexReferenceObject?.ComplexTestModelInternalId);
            Assert.Equal(secondSource.InternalId, secondResult?.ComplexReferenceObject?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsForeignKeyObjectsCorrectly_SingleReturn()
        {
            // Arrange
            SetupContext(true);
            new ServiceCollection().AddCoreRelm(_configuration);
            var context = new ComplexTestContext(autoInitializeDataSets: false, autoVerifyTables: false);
            var modelDataLoader = SetupSingleReturnReferenceDataLoader(context, true, true);

            // Act
            var loadedResults = RelmHelper.LoadForeignKeyField(context.ContextOptions, mockComplexTestModels, x => x.ComplexReferenceObjects, modelDataLoader.Object);

            // Assert
            modelDataLoader.Verify(r => r.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()));

            var firstSource = mockComplexTestModels!.First();
            var secondSource = mockComplexTestModels!.Skip(1).First();
            var firstResult = loadedResults.First();
            var secondResult = loadedResults.Skip(1).First();

            Assert.NotNull(firstResult?.ComplexReferenceObjects);
            Assert.NotNull(secondResult?.ComplexReferenceObjects);

            Assert.True(firstResult.ComplexReferenceObjects.Count != 0);
            Assert.True(secondResult.ComplexReferenceObjects.Count != 0);

            Assert.Equal(2, firstResult.ComplexReferenceObjects.Count);
            Assert.Single(secondResult.ComplexReferenceObjects);

            Assert.Equal(firstResult.InternalId, firstResult.ComplexReferenceObjects?.FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(firstResult.InternalId, firstResult.ComplexReferenceObjects?.Skip(1).FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(secondResult.InternalId, secondResult.ComplexReferenceObjects?.FirstOrDefault()?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsForeignKeyObjectCorrectly_SingleReturn()
        {
            // Arrange
            SetupContext(false);
            new ServiceCollection().AddCoreRelm(_configuration);
            var context = new ComplexTestContext(autoInitializeDataSets: false, autoVerifyTables: false);
            var modelDataLoader = SetupSingleReturnReferenceDataLoader(context, false, false);

            // Act
            var loadedResults = RelmHelper.LoadForeignKeyField(context.ContextOptions, mockComplexTestModels, x => x.ComplexReferenceObject, modelDataLoader.Object);

            // Assert
            modelDataLoader.Verify(r => r.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()));

            var firstSource = mockComplexTestModels!.First();
            var firstModel = loadedResults.First();

            Assert.NotNull(firstModel.ComplexReferenceObject);
            Assert.Equal(firstModel.InternalId, firstModel?.ComplexReferenceObject?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsNavigationPropertyObjectsCorrectly()
        {
            // Arrange
            new ServiceCollection().AddCoreRelm(_configuration);
            var context = new ComplexTestContext(autoInitializeDataSets: false, autoVerifyTables: false);
            var modelDataLoader = SetupNavigationDataLoader(context, true);

            // Act
            var loadedResults = RelmHelper.LoadForeignKeyField(context.ContextOptions, mockComplexTestModels, x => x.ComplexReferenceObject_NavigationProperties, modelDataLoader.Object);

            // Assert
            modelDataLoader.Verify(r => r.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()));

            var firstSource = mockComplexTestModels!.First();
            var secondSource = mockComplexTestModels!.Skip(1).First();
            var firstModel = loadedResults.First();
            var secondModel = loadedResults.Skip(1).First();

            Assert.NotNull(firstModel?.ComplexReferenceObject_NavigationProperties);
            Assert.NotNull(secondModel?.ComplexReferenceObject_NavigationProperties);

            Assert.True(firstModel.ComplexReferenceObject_NavigationProperties.Count != 0);
            Assert.True(secondModel.ComplexReferenceObject_NavigationProperties.Count != 0);

            Assert.Equal(2, firstModel.ComplexReferenceObject_NavigationProperties.Count);
            Assert.Single(secondModel.ComplexReferenceObject_NavigationProperties);

            Assert.Equal(firstModel.InternalId, firstModel.ComplexReferenceObject_NavigationProperties?.FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(firstModel.InternalId, firstModel.ComplexReferenceObject_NavigationProperties?.Skip(1).FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(secondModel.InternalId, secondModel.ComplexReferenceObject_NavigationProperties?.FirstOrDefault()?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsNavigationPropertyObjectCorrectly()
        {
            // Arrange
            new ServiceCollection().AddCoreRelm(_configuration);
            var context = new ComplexTestContext(autoInitializeDataSets: false, autoVerifyTables: false);
            var modelDataLoader = SetupNavigationDataLoader(context, false);

            // Act
            var loadedResults = RelmHelper.LoadForeignKeyField(context.ContextOptions, mockComplexTestModels, x => x.ComplexReferenceObject_NavigationPropertyItem, modelDataLoader.Object);

            // Assert
            modelDataLoader.Verify(r => r.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()));

            var firstSource = mockComplexTestModels!.First();
            var secondSource = mockComplexTestModels!.Skip(1).First();
            var firstModel = loadedResults.First();

            Assert.NotNull(firstModel.ComplexReferenceObject_NavigationPropertyItem);
            Assert.Equal(firstModel.InternalId, firstModel?.ComplexReferenceObject_NavigationPropertyItem?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsPrincipalEntityObjectsCorrectly()
        {
            // Arrange
            new ServiceCollection().AddCoreRelm(_configuration);
            var context = new ComplexTestContext(autoInitializeDataSets: false, autoVerifyTables: false);
            var modelDataLoader = SetupPrincipalDataLoader(context, true);

            // Act
            var loadedResults = RelmHelper.LoadForeignKeyField(context.ContextOptions, mockComplexTestModels, x => x.ComplexReferenceObject_PrincipalEntities, modelDataLoader.Object);

            // Assert
            modelDataLoader.Verify(r => r.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()));

            var firstSource = mockComplexTestModels!.First();
            var secondSource = mockComplexTestModels!.Skip(1).First();
            var firstModel = loadedResults.First();
            var secondModel = loadedResults.Skip(1).First();

            Assert.NotNull(firstModel?.ComplexReferenceObject_PrincipalEntities);
            Assert.NotNull(secondModel?.ComplexReferenceObject_PrincipalEntities);

            Assert.True(firstModel.ComplexReferenceObject_PrincipalEntities.Count != 0);
            Assert.True(secondModel.ComplexReferenceObject_PrincipalEntities.Count != 0);

            Assert.Equal(2, firstModel.ComplexReferenceObject_PrincipalEntities.Count);
            Assert.Single(secondModel.ComplexReferenceObject_PrincipalEntities);

            Assert.Equal(firstModel.InternalId, firstModel.ComplexReferenceObject_PrincipalEntities?.FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(firstModel.InternalId, firstModel.ComplexReferenceObject_PrincipalEntities?.Skip(1).FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(secondModel.InternalId, secondModel.ComplexReferenceObject_PrincipalEntities?.FirstOrDefault()?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsPrincipalEntityObjectCorrectly()
        {
            // Arrange
            new ServiceCollection().AddCoreRelm(_configuration);
            var context = new ComplexTestContext(autoInitializeDataSets: false, autoVerifyTables: false);
            var modelDataLoader = SetupPrincipalDataLoader(context, false);

            // Act
            var loadedResults = RelmHelper.LoadForeignKeyField(context.ContextOptions, mockComplexTestModels, x => x.ComplexReferenceObject_PrincipalEntityItem, modelDataLoader.Object);

            // Assert
            modelDataLoader.Verify(r => r.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()));

            var firstSource = mockComplexTestModels!.First();
            var secondSource = mockComplexTestModels!.Skip(1).First();
            var firstModel = loadedResults.First();

            Assert.NotNull(firstModel.ComplexReferenceObject_PrincipalEntityItem);
            Assert.Equal(firstModel.InternalId, firstModel?.ComplexReferenceObject_PrincipalEntityItem?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadReferenceLocalKeyObjectsCorrectly()
        {
            // Arrange
            new ServiceCollection().AddCoreRelm(_configuration);
            var context = new ComplexTestContext(autoInitializeDataSets: false, autoVerifyTables: false);
            var modelDataLoader = SetupPrincipalDataLoaderLocalKey(context, true);

            // Act
            var loadedResults = RelmHelper.LoadForeignKeyField(context.ContextOptions, mockComplexTestModels, x => x.ComplexReferenceObject_PrincipalEntities_LocalKeys, modelDataLoader.Object);

            // Assert
            modelDataLoader.Verify(r => r.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()));

            var firstSource = mockComplexTestModels!.First();
            var secondSource = mockComplexTestModels!.Skip(1).First();
            var firstModel = loadedResults.First();
            var secondModel = loadedResults.Skip(1).First();

            Assert.NotNull(firstModel?.ComplexReferenceObject_PrincipalEntities_LocalKeys);
            Assert.NotNull(secondModel?.ComplexReferenceObject_PrincipalEntities_LocalKeys);

            Assert.True(firstModel.ComplexReferenceObject_PrincipalEntities_LocalKeys.Count != 0);
            Assert.True(secondModel.ComplexReferenceObject_PrincipalEntities_LocalKeys.Count != 0);

            Assert.Equal(2, firstModel.ComplexReferenceObject_PrincipalEntities_LocalKeys.Count);
            Assert.Single(secondModel.ComplexReferenceObject_PrincipalEntities_LocalKeys);

            Assert.Equal(firstModel.ComplexReferenceObjectLocalKey, firstModel.ComplexReferenceObject_PrincipalEntities_LocalKeys?.FirstOrDefault()?.ComplexTestModelLocalKey);
            Assert.Equal(firstModel.ComplexReferenceObjectLocalKey, firstModel.ComplexReferenceObject_PrincipalEntities_LocalKeys?.Skip(1).FirstOrDefault()?.ComplexTestModelLocalKey);
            Assert.Equal(secondModel.ComplexReferenceObjectLocalKey, secondModel.ComplexReferenceObject_PrincipalEntities_LocalKeys?.FirstOrDefault()?.ComplexTestModelLocalKey);
        }

        [Fact]
        public void Reference_LoadReferenceLocalKeyObjectCorrectly()
        {
            // Arrange
            new ServiceCollection().AddCoreRelm(_configuration);
            var context = new ComplexTestContext(autoInitializeDataSets: false, autoVerifyTables: false);
            var modelDataLoader = SetupPrincipalDataLoaderLocalKey(context, false);

            // Act
            var loadedResults = RelmHelper.LoadForeignKeyField(context.ContextOptions, mockComplexTestModels, x => x.ComplexReferenceObject_PrincipalEntity_LocalKey, modelDataLoader.Object);

            // Assert
            modelDataLoader.Verify(r => r.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()));

            var firstSource = mockComplexTestModels!.First();
            var secondSource = mockComplexTestModels!.Skip(1).First();
            var firstModel = loadedResults.First();

            Assert.NotNull(firstModel.ComplexReferenceObject_PrincipalEntity_LocalKey);
            Assert.Equal(firstModel.ComplexReferenceObjectLocalKey, firstModel?.ComplexReferenceObject_PrincipalEntity_LocalKey?.ComplexTestModelLocalKey);
        }

        [Fact]
        public void RelmHelper_LoadDataLoaderField_Single_Boolean()
        {
            // Arrange
            var complexTestModel = new ComplexTestModel();

            // Act
            RelmHelper.LoadDataLoaderField(new ComplexTestContext(autoVerifyTables: false), complexTestModel, x => x.TestFieldBoolean);

            // Assert
            Assert.NotNull(complexTestModel.TestFieldBoolean);
            Assert.True(complexTestModel.TestFieldBoolean);
        }

        [Fact]
        public void RelmHelper_LoadDataLoadersField_Multiple_Boolean()
        {
            // Arrange
            var complexTestModel = new ComplexTestModel();

            // Act
            RelmHelper.LoadDataLoaderField(new ComplexTestContext(autoVerifyTables: false), complexTestModel, x => x.TestFieldBooleans);

            // Assert
            Assert.Equal(4, complexTestModel?.TestFieldBooleans?.Count);

            // true, false, true, false
            Assert.True(complexTestModel?.TestFieldBooleans?.FirstOrDefault());
            Assert.False(complexTestModel?.TestFieldBooleans?.Skip(1).FirstOrDefault());
            Assert.True(complexTestModel?.TestFieldBooleans?.Skip(2).FirstOrDefault());
            Assert.False(complexTestModel?.TestFieldBooleans?.Skip(3).FirstOrDefault());
        }
    }
}
