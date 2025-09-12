using Moq;
using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.RelmInternal.Helpers.DataTransfer;
using CoreRelm.Tests.Interfaces;
using CoreRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Models.RelmDataSet_Tests
{
    public class Reference__BasicDefault_Tester
    {
        private ComplexTestContext context;

        public Reference__BasicDefault_Tester()
        {
            context = SetupContext(true);
        }

        private ComplexTestContext SetupContext(bool haveTwoRoots = true)
        { 
            // dummy data
            var mockComplexTestModels = new List<ComplexTestModel>
            {
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
            };

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

            context = new ComplexTestContext("name=SimpleRelmMySql");

            // create dummy data loaders for dummy data to be placed in both relevant data sets
            var modelDataLoader = new Mock<RelmDefaultDataLoader<ComplexTestModel>>(); // { CallBase = true };

            // make sure GetLoadData() calls base so LastExecutedCommands (required for references) gets populated
            modelDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            modelDataLoader.Setup(x => x.GetLoadData()).CallBase();
            modelDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexTestModels);
            
            context.ComplexTestModels!.SetDataLoader(modelDataLoader.Object);

            return context;
        }

        private void SetupSingleReturnReferenceDataLoader(bool addSecondId, bool haveTwoRoots)
        {
            var mockComplexReferenceObjects = new List<ComplexReferenceObject>
            {
                new() { ComplexTestModelInternalId = "ID1", TestModel = null },
            };

            if (haveTwoRoots)
                mockComplexReferenceObjects.Add(new ComplexReferenceObject { ComplexTestModelInternalId = "ID2", TestModel = null });

            if (addSecondId)
                mockComplexReferenceObjects.Add(new ComplexReferenceObject { ComplexTestModelInternalId = "ID1", TestModel = null });

            var referenceDataLoader = new Mock<RelmDefaultDataLoader<ComplexReferenceObject>>();
            referenceDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            referenceDataLoader.Setup(x => x.GetLoadData()).CallBase();
            referenceDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexReferenceObjects);

            context.ComplexReferenceObjects!.SetDataLoader(referenceDataLoader.Object);
        }

        private void SetupReferenceDataLoader(bool addSecondId)
        {
            var mockComplexReferenceObjects = new List<ComplexReferenceObject>
            {
                new() { ComplexTestModelInternalId = "ID1", TestModel = null },
                new() { ComplexTestModelInternalId = "ID2", TestModel = null },
            };

            if (addSecondId)
                mockComplexReferenceObjects.Add(new ComplexReferenceObject { ComplexTestModelInternalId = "ID1", TestModel = null });

            var referenceDataLoader = new Mock<RelmDefaultDataLoader<ComplexReferenceObject>>();
            referenceDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            referenceDataLoader.Setup(x => x.GetLoadData()).CallBase();
            referenceDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexReferenceObjects);

            context.ComplexReferenceObjects!.SetDataLoader(referenceDataLoader.Object);
        }

        private void SetupNavigationDataLoader(bool addSecondId)
        {
            var mockComplexReferenceObjects_Navigation = new List<ComplexReferenceObject_NavigationProperty>
            {
                new() { ComplexTestModelInternalId = "ID1", TestModel = null },
                new() { ComplexTestModelInternalId = "ID2", TestModel = null },
            };

            if (addSecondId)
                mockComplexReferenceObjects_Navigation.Add(new ComplexReferenceObject_NavigationProperty { ComplexTestModelInternalId = "ID1", TestModel = null });

            var navigationDataLoader = new Mock<RelmDefaultDataLoader<ComplexReferenceObject_NavigationProperty>>();
            navigationDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            navigationDataLoader.Setup(x => x.GetLoadData()).CallBase();
            navigationDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexReferenceObjects_Navigation);

            context.ComplexReferenceObject_NavigationProperties!.SetDataLoader(navigationDataLoader.Object);
        }

        private void SetupPrincipalDataLoader(bool addSecondId)
        {
            var mockComplexReferenceObjects_Principal = new List<ComplexReferenceObject_PrincipalEntity>
            {
                new() { ComplexTestModelInternalId = "ID1", TestModel = null },
                new() { ComplexTestModelInternalId = "ID2", TestModel = null },
            };

            if (addSecondId)
                mockComplexReferenceObjects_Principal.Add(new ComplexReferenceObject_PrincipalEntity { ComplexTestModelInternalId = "ID1", TestModel = null });

            var principalDataLoader = new Mock<RelmDefaultDataLoader<ComplexReferenceObject_PrincipalEntity>>();
            principalDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            principalDataLoader.Setup(x => x.GetLoadData()).CallBase();
            principalDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexReferenceObjects_Principal);

            context.ComplexReferenceObject_PrincipalEntities!.SetDataLoader(principalDataLoader.Object);
        }

        private void SetupPrincipalDataLoaderLocalKey(bool addSecondId)
        {
            var mockComplexReferenceObjects_Principal = new List<ComplexReferenceObject_PrincipalEntity>
            {
                new() { ComplexTestModelLocalKey = "LOCALKEY1", TestModel = null },
                new() { ComplexTestModelLocalKey = "LOCALKEY2", TestModel = null },
            };

            if (addSecondId)
                mockComplexReferenceObjects_Principal.Add(new ComplexReferenceObject_PrincipalEntity { ComplexTestModelLocalKey = "LOCALKEY1", TestModel = null });

            var principalDataLoader = new Mock<RelmDefaultDataLoader<ComplexReferenceObject_PrincipalEntity>>();
            principalDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            principalDataLoader.Setup(x => x.GetLoadData()).CallBase();
            principalDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexReferenceObjects_Principal);

            context.ComplexReferenceObject_PrincipalEntities!.SetDataLoader(principalDataLoader.Object);
        }

        [Fact]
        public void Reference_LoadsReferenceLocalKeyObjectWithConstraints_LoadsModelCorrectly()
        {
            // Arrange
            SetupPrincipalDataLoaderLocalKey(false);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject_PrincipalEntity_LocalKey, x => x.Active == true).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();

            Assert.NotNull(firstModel.ComplexReferenceObject_PrincipalEntity_LocalKey);
            Assert.Equal(firstModel.ComplexReferenceObjectLocalKey, firstModel?.ComplexReferenceObject_PrincipalEntity_LocalKey?.ComplexTestModelLocalKey);
        }

        [Fact]
        public void Reference_LoadsPrincipalEntityLocalKeyObject_LoadsModelCorrectly()
        {
            // Arrange
            SetupPrincipalDataLoaderLocalKey(false);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject_PrincipalEntity_LocalKey).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();

            Assert.NotNull(firstModel.ComplexReferenceObject_PrincipalEntity_LocalKey);
            Assert.Equal(firstModel.ComplexReferenceObjectLocalKey, firstModel?.ComplexReferenceObject_PrincipalEntity_LocalKey?.ComplexTestModelLocalKey);
        }

        [Fact]
        public void Reference_LoadsPrincipalEntityLocalKeyObjectsWithConstraints_LoadsModelsCorrectly()
        {
            // Arrange
            SetupPrincipalDataLoaderLocalKey(true);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject_PrincipalEntities_LocalKeys, x => x.Active == true).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();
            var secondModel = context.ComplexTestModels.Skip(1).First();

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
        public void Reference_LoadsReferenceLocalKeyObjects_LoadsModelsCorrectly()
        {
            // Arrange
            SetupPrincipalDataLoaderLocalKey(true);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject_PrincipalEntities_LocalKeys).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();
            var secondModel = context.ComplexTestModels.Skip(1).First();

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
        public void Reference_LoadsReferenceObjects_ThrowsException()
        {
            // Arrange
            SetupReferenceDataLoader(false);

            // Act & Assert
            var exception = Assert.Throws<MemberAccessException>(() => context.ComplexTestModels!.Reference(x => x.SimpleReferenceObjects).Load());
            Assert.Equal("Foreign key referenced by RelmForeignKey attribute could not be found.", exception.Message);
        }

        [Fact]
        public void Reference_LoadsForeignKeyObjectsWithConstraints_LoadsModelsCorrectly()
        {
            // Arrange
            SetupReferenceDataLoader(true);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObjects, x => x.Active == true).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();
            var secondModel = context.ComplexTestModels.Skip(1).First();

            Assert.NotNull(firstModel?.ComplexReferenceObjects);
            Assert.NotNull(secondModel?.ComplexReferenceObjects);

            Assert.True(firstModel.ComplexReferenceObjects.Count != 0);
            Assert.True(secondModel.ComplexReferenceObjects.Count != 0);

            Assert.Equal(2, firstModel.ComplexReferenceObjects.Count);
            Assert.Single(secondModel.ComplexReferenceObjects);

            Assert.Equal(firstModel.InternalId, firstModel.ComplexReferenceObjects?.FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(firstModel.InternalId, firstModel.ComplexReferenceObjects?.Skip(1).FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(secondModel.InternalId, secondModel.ComplexReferenceObjects?.FirstOrDefault()?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsForeignKeyObjects_LoadsModelsCorrectly()
        {
            // Arrange
            SetupReferenceDataLoader(true);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObjects).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();
            var secondModel = context.ComplexTestModels.Skip(1).First();

            Assert.NotNull(firstModel?.ComplexReferenceObjects);
            Assert.NotNull(secondModel?.ComplexReferenceObjects);

            Assert.True(firstModel.ComplexReferenceObjects.Count != 0);
            Assert.True(secondModel.ComplexReferenceObjects.Count != 0);

            Assert.Equal(2, firstModel.ComplexReferenceObjects.Count);
            Assert.Single(secondModel.ComplexReferenceObjects);

            Assert.Equal(firstModel.InternalId, firstModel.ComplexReferenceObjects?.FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(firstModel.InternalId, firstModel.ComplexReferenceObjects?.Skip(1).FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(secondModel.InternalId, secondModel.ComplexReferenceObjects?.FirstOrDefault()?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsForeignKeyObjectWithConstraints_LoadsModelCorrectly()
        {
            // Arrange
            SetupReferenceDataLoader(false);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject, x => x.Active == true).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();

            Assert.NotNull(firstModel.ComplexReferenceObject);
            Assert.Equal(firstModel.InternalId, firstModel?.ComplexReferenceObject?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsForeignKeyObject_LoadsModelCorrectly()
        {
            // Arrange
            SetupReferenceDataLoader(false);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();

            Assert.NotNull(firstModel.ComplexReferenceObject);
            Assert.Equal(firstModel.InternalId, firstModel?.ComplexReferenceObject?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsForeignKeyObjectsWithConstraints_LoadsModelsCorrectly_SingleReturn()
        {
            // Arrange
            SetupContext(false);
            SetupSingleReturnReferenceDataLoader(true, false);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObjects, x => x.Active == true).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();

            Assert.NotNull(firstModel?.ComplexReferenceObjects);

            Assert.True(firstModel.ComplexReferenceObjects.Count != 0);

            Assert.Equal(2, firstModel.ComplexReferenceObjects.Count);

            Assert.Equal(firstModel.InternalId, firstModel.ComplexReferenceObjects?.FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(firstModel.InternalId, firstModel.ComplexReferenceObjects?.Skip(1).FirstOrDefault()?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsForeignKeyObjects_LoadsModelsCorrectly_SingleReturn()
        {
            // Arrange
            SetupContext(false);
            SetupSingleReturnReferenceDataLoader(true, false);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObjects).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();

            Assert.NotNull(firstModel?.ComplexReferenceObjects);

            Assert.True(firstModel.ComplexReferenceObjects.Count != 0);

            Assert.Equal(2, firstModel.ComplexReferenceObjects.Count);

            Assert.Equal(firstModel.InternalId, firstModel.ComplexReferenceObjects?.FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(firstModel.InternalId, firstModel.ComplexReferenceObjects?.Skip(1).FirstOrDefault()?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsForeignKeyObjectWithConstraints_LoadsModelCorrectly_SingleReturn()
        {
            // Arrange
            SetupContext(false);
            SetupSingleReturnReferenceDataLoader(false, false);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject, x => x.Active == true).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();

            Assert.NotNull(firstModel.ComplexReferenceObject);
            Assert.Equal(firstModel.InternalId, firstModel?.ComplexReferenceObject?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsForeignKeyObject_LoadsModelCorrectly_SingleReturn()
        {
            // Arrange
            SetupContext(false);
            SetupSingleReturnReferenceDataLoader(false, false);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();

            Assert.NotNull(firstModel.ComplexReferenceObject);
            Assert.Equal(firstModel.InternalId, firstModel?.ComplexReferenceObject?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsNavigationPropertyObjectsWithConstraints_LoadsModelsCorrectly()
        {
            // Arrange
            SetupNavigationDataLoader(true);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject_NavigationProperties, x => x.Active == true).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();
            var secondModel = context.ComplexTestModels.Skip(1).First();

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
        public void Reference_LoadsNavigationPropertyObjects_LoadsModelsCorrectly()
        {
            // Arrange
            SetupNavigationDataLoader(true);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject_NavigationProperties).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();
            var secondModel = context.ComplexTestModels.Skip(1).First();

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
        public void Reference_LoadsNavigationPropertyObjectWithConstraints_LoadsModelCorrectly()
        {
            // Arrange
            SetupNavigationDataLoader(false);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject_NavigationPropertyItem, x => x.Active == true).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();

            Assert.NotNull(firstModel.ComplexReferenceObject_NavigationPropertyItem);
            Assert.Equal(firstModel.InternalId, firstModel?.ComplexReferenceObject_NavigationPropertyItem?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsNavigationPropertyObject_LoadsModelCorrectly()
        {
            // Arrange
            SetupNavigationDataLoader(false);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject_NavigationPropertyItem).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();

            Assert.NotNull(firstModel.ComplexReferenceObject_NavigationPropertyItem);
            Assert.Equal(firstModel.InternalId, firstModel?.ComplexReferenceObject_NavigationPropertyItem?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsPrincipalEntityObjectsWithConstraints_LoadsModelsCorrectly()
        {
            // Arrange
            SetupPrincipalDataLoader(true);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject_PrincipalEntities, x => x.Active == true).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();
            var secondModel = context.ComplexTestModels.Skip(1).First();

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
        public void Reference_LoadsPrincipalEntityObjects_LoadsModelsCorrectly()
        {
            // Arrange
            SetupPrincipalDataLoader(true);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject_PrincipalEntities).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();
            var secondModel = context.ComplexTestModels.Skip(1).First();

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
        public void Reference_LoadsPrincipalEntityObjectWithConstraints_LoadsModelCorrectly()
        {
            // Arrange
            SetupPrincipalDataLoader(false);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject_PrincipalEntityItem, x => x.Active == true).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();

            Assert.NotNull(firstModel.ComplexReferenceObject_PrincipalEntityItem);
            Assert.Equal(firstModel.InternalId, firstModel?.ComplexReferenceObject_PrincipalEntityItem?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsPrincipalEntityObject_LoadsModelCorrectly()
        {
            // Arrange
            SetupPrincipalDataLoader(false);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject_PrincipalEntityItem).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();

            Assert.NotNull(firstModel.ComplexReferenceObject_PrincipalEntityItem);
            Assert.Equal(firstModel.InternalId, firstModel?.ComplexReferenceObject_PrincipalEntityItem?.ComplexTestModelInternalId);
        }
    }
}
