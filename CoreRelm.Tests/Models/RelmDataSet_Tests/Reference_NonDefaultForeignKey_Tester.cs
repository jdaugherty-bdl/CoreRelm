using Moq;
using CoreRelm.Models;
using CoreRelm.Tests.TestModels.MultipleKeys;
using CoreRelm.Tests.TestModels.NonDefaultForeignKeys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Models.RelmDataSet_Tests
{
    public class Reference_NonDefaultForeignKey_Tester
    {
        private NonDefaultForeignKeysTestContext context;

        public Reference_NonDefaultForeignKey_Tester()
        {
            // dummy data
            var mockComplexTestModels = new List<NonDefaultForeignKeysTestObject>
            {
                new() 
                {
                    InternalId = "ID1",
                    NonDefaultForeignKeysReferenceObjectLocalKey = "LOCALKEY1",
                    NonDefaultForeignKeysReferenceObject_ForeignKeys = null,
                    NonDefaultForeignKeysReferenceObject_ForeignKey_Item = null,
                    NonDefaultForeignKeysReferenceObject_NavigationProperties = null,
                    NonDefaultForeignKeysReferenceObject_NavigationProperty_Item = null,
                    NonDefaultForeignKeysReferenceObject_PrincipalEntities = null,
                    NonDefaultForeignKeysReferenceObject_PrincipalEntity_Item = null,
                },
                new() 
                {
                    InternalId = "ID1",
                    NonDefaultForeignKeysReferenceObjectLocalKey = "LOCALKEY2",
                    NonDefaultForeignKeysReferenceObject_ForeignKeys = null,
                    NonDefaultForeignKeysReferenceObject_ForeignKey_Item = null,
                    NonDefaultForeignKeysReferenceObject_NavigationProperties = null,
                    NonDefaultForeignKeysReferenceObject_NavigationProperty_Item = null,
                    NonDefaultForeignKeysReferenceObject_PrincipalEntities = null,
                    NonDefaultForeignKeysReferenceObject_PrincipalEntity_Item = null,
                },
            };

            context = new NonDefaultForeignKeysTestContext("name=SimpleRelmMySql");

            // create dummy data loaders for dummy data to be placed in both relevant data sets
            var modelDataLoader = new Mock<RelmDefaultDataLoader<NonDefaultForeignKeysTestObject>>(); // { CallBase = true };

            // make sure GetLoadData() calls base so LastExecutedCommands (required for references) gets populated
            modelDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            modelDataLoader.Setup(x => x.GetLoadData()).CallBase();
            modelDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexTestModels);

            context.NonDefaultForeignKeysTestObjects!.SetDataLoader(modelDataLoader.Object);
        }

        private void SetupReferenceDataLoader(bool addSecondId)
        {
            var mockComplexReferenceObjects_ForeignKey = new List<NonDefaultForeignKeysReferenceObject_ForeignKey>
            {
                new() { ReferenceKey = "LOCALKEY1", NonDefaultForeignKeysTestObject_Reference= null },
                new() { ReferenceKey = "LOCALKEY2", NonDefaultForeignKeysTestObject_Reference = null },
            };

            if (addSecondId)
                mockComplexReferenceObjects_ForeignKey.Add(new NonDefaultForeignKeysReferenceObject_ForeignKey { ReferenceKey = "LOCALKEY1", NonDefaultForeignKeysTestObject_Reference = null });

            var referenceDataLoader = new Mock<RelmDefaultDataLoader<NonDefaultForeignKeysReferenceObject_ForeignKey>>();
            referenceDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            referenceDataLoader.Setup(x => x.GetLoadData()).CallBase();
            referenceDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexReferenceObjects_ForeignKey);

            context.NonDefaultForeignKeysReferenceObject_ForeignKeys!.SetDataLoader(referenceDataLoader.Object);
        }

        private void SetupNavigationDataLoader(bool addSecondId)
        {
            var mockComplexReferenceObjects_Navigation = new List<NonDefaultForeignKeysReferenceObject_NavigationProperty>
            {
                new() { ReferenceKey = "LOCALKEY1", NonDefaultForeignKeysTestObject_Reference = null },
                new() { ReferenceKey = "LOCALKEY2", NonDefaultForeignKeysTestObject_Reference = null },
            };

            if (addSecondId)
                mockComplexReferenceObjects_Navigation.Add(new NonDefaultForeignKeysReferenceObject_NavigationProperty { ReferenceKey = "LOCALKEY1", NonDefaultForeignKeysTestObject_Reference = null });

            var navigationDataLoader = new Mock<RelmDefaultDataLoader<NonDefaultForeignKeysReferenceObject_NavigationProperty>>();
            navigationDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            navigationDataLoader.Setup(x => x.GetLoadData()).CallBase();
            navigationDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexReferenceObjects_Navigation);

            context.NonDefaultForeignKeysReferenceObject_NavigationProperties!.SetDataLoader(navigationDataLoader.Object);
        }

        private void SetupPrincipalDataLoader(bool addSecondId)
        {
            var mockComplexReferenceObjects_Principal = new List<NonDefaultForeignKeysReferenceObject_PrincipalEntity>
            {
                new() { ReferenceKey = "LOCALKEY1", NonDefaultForeignKeysTestObject_Reference = null },
                new() { ReferenceKey = "LOCALKEY2", NonDefaultForeignKeysTestObject_Reference = null },
            };

            if (addSecondId)
                mockComplexReferenceObjects_Principal.Add(new NonDefaultForeignKeysReferenceObject_PrincipalEntity { ReferenceKey = "LOCALKEY1", NonDefaultForeignKeysTestObject_Reference = null });

            var principalDataLoader = new Mock<RelmDefaultDataLoader<NonDefaultForeignKeysReferenceObject_PrincipalEntity>>();
            principalDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            principalDataLoader.Setup(x => x.GetLoadData()).CallBase();
            principalDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexReferenceObjects_Principal);

            context.NonDefaultForeignKeysReferenceObject_PrincipalEntities!.SetDataLoader(principalDataLoader.Object);
        }

        [Fact]
        public void Reference_LoadsForeignKeyObjectsCorrectly()
        {
            // Arrange
            SetupReferenceDataLoader(true);

            // Act
            context.NonDefaultForeignKeysTestObjects!.Reference(x => x.NonDefaultForeignKeysReferenceObject_ForeignKeys).Load();

            // Assert
            var firstModel = context.NonDefaultForeignKeysTestObjects.First();
            var secondModel = context.NonDefaultForeignKeysTestObjects.Skip(1).First();

            Assert.NotNull(firstModel?.NonDefaultForeignKeysReferenceObject_ForeignKeys);
            Assert.NotNull(secondModel?.NonDefaultForeignKeysReferenceObject_ForeignKeys);

            Assert.True(firstModel.NonDefaultForeignKeysReferenceObject_ForeignKeys.Count != 0);
            Assert.True(secondModel.NonDefaultForeignKeysReferenceObject_ForeignKeys.Count != 0);

            Assert.Equal(2, firstModel.NonDefaultForeignKeysReferenceObject_ForeignKeys.Count);
            Assert.Single(secondModel.NonDefaultForeignKeysReferenceObject_ForeignKeys);

            Assert.Equal(firstModel.NonDefaultForeignKeysReferenceObjectLocalKey, firstModel.NonDefaultForeignKeysReferenceObject_ForeignKeys?.FirstOrDefault()?.ReferenceKey);
            Assert.Equal(firstModel.NonDefaultForeignKeysReferenceObjectLocalKey, firstModel.NonDefaultForeignKeysReferenceObject_ForeignKeys?.Skip(1).FirstOrDefault()?.ReferenceKey);
            Assert.Equal(secondModel.NonDefaultForeignKeysReferenceObjectLocalKey, secondModel.NonDefaultForeignKeysReferenceObject_ForeignKeys?.FirstOrDefault()?.ReferenceKey);
        }

        [Fact]
        public void Reference_LoadsForeignKeyObjectCorrectly()
        {
            // Arrange
            SetupReferenceDataLoader(false);

            // Act
            context.NonDefaultForeignKeysTestObjects!.Reference(x => x.NonDefaultForeignKeysReferenceObject_ForeignKey_Item).Load();

            // Assert
            var firstModel = context.NonDefaultForeignKeysTestObjects.First();

            Assert.NotNull(firstModel.NonDefaultForeignKeysReferenceObject_ForeignKey_Item);
            Assert.Equal(firstModel.NonDefaultForeignKeysReferenceObjectLocalKey, firstModel.NonDefaultForeignKeysReferenceObject_ForeignKey_Item?.ReferenceKey);
        }

        [Fact]
        public void Reference_LoadsNavigationPropertyObjectsCorrectly()
        {
            // Arrange
            SetupNavigationDataLoader(true);

            // Act
            context.NonDefaultForeignKeysTestObjects!.Reference(x => x.NonDefaultForeignKeysReferenceObject_NavigationProperties).Load();

            // Assert
            var firstModel = context.NonDefaultForeignKeysTestObjects.First();
            var secondModel = context.NonDefaultForeignKeysTestObjects.Skip(1).First();

            Assert.NotNull(firstModel?.NonDefaultForeignKeysReferenceObject_NavigationProperties);
            Assert.NotNull(secondModel?.NonDefaultForeignKeysReferenceObject_NavigationProperties);

            Assert.True(firstModel.NonDefaultForeignKeysReferenceObject_NavigationProperties.Count != 0);
            Assert.True(secondModel.NonDefaultForeignKeysReferenceObject_NavigationProperties.Count != 0);

            Assert.Equal(2, firstModel.NonDefaultForeignKeysReferenceObject_NavigationProperties.Count);
            Assert.Single(secondModel.NonDefaultForeignKeysReferenceObject_NavigationProperties);

            Assert.Equal(firstModel.NonDefaultForeignKeysReferenceObjectLocalKey, firstModel.NonDefaultForeignKeysReferenceObject_NavigationProperties?.FirstOrDefault()?.ReferenceKey);
            Assert.Equal(firstModel.NonDefaultForeignKeysReferenceObjectLocalKey, firstModel.NonDefaultForeignKeysReferenceObject_NavigationProperties?.Skip(1).FirstOrDefault()?.ReferenceKey);
            Assert.Equal(secondModel.NonDefaultForeignKeysReferenceObjectLocalKey, secondModel.NonDefaultForeignKeysReferenceObject_NavigationProperties?.FirstOrDefault()?.ReferenceKey);
        }

        [Fact]
        public void Reference_LoadsNavigationPropertyObjectCorrectly()
        {
            // Arrange
            SetupNavigationDataLoader(false);

            // Act
            context.NonDefaultForeignKeysTestObjects!.Reference(x => x.NonDefaultForeignKeysReferenceObject_NavigationProperty_Item).Load();

            // Assert
            var firstModel = context.NonDefaultForeignKeysTestObjects.First();

            Assert.NotNull(firstModel.NonDefaultForeignKeysReferenceObject_NavigationProperty_Item);
            Assert.Equal(firstModel.NonDefaultForeignKeysReferenceObjectLocalKey, firstModel.NonDefaultForeignKeysReferenceObject_NavigationProperty_Item?.ReferenceKey);
        }

        [Fact]
        public void Reference_LoadsPrincipalEntityObjectsCorrectly()
        {
            // Arrange
            SetupPrincipalDataLoader(true);

            // Act
            context.NonDefaultForeignKeysTestObjects!.Reference(x => x.NonDefaultForeignKeysReferenceObject_PrincipalEntities).Load();

            // Assert
            var firstModel = context.NonDefaultForeignKeysTestObjects.First();
            var secondModel = context.NonDefaultForeignKeysTestObjects.Skip(1).First();

            Assert.NotNull(firstModel?.NonDefaultForeignKeysReferenceObject_PrincipalEntities);
            Assert.NotNull(secondModel?.NonDefaultForeignKeysReferenceObject_PrincipalEntities);

            Assert.True(firstModel.NonDefaultForeignKeysReferenceObject_PrincipalEntities.Count != 0);
            Assert.True(secondModel.NonDefaultForeignKeysReferenceObject_PrincipalEntities.Count != 0);

            Assert.Equal(2, firstModel.NonDefaultForeignKeysReferenceObject_PrincipalEntities.Count);
            Assert.Single(secondModel.NonDefaultForeignKeysReferenceObject_PrincipalEntities);

            Assert.Equal(firstModel.NonDefaultForeignKeysReferenceObjectLocalKey, firstModel.NonDefaultForeignKeysReferenceObject_PrincipalEntities?.FirstOrDefault()?.ReferenceKey);
            Assert.Equal(firstModel.NonDefaultForeignKeysReferenceObjectLocalKey, firstModel.NonDefaultForeignKeysReferenceObject_PrincipalEntities?.Skip(1).FirstOrDefault()?.ReferenceKey);
            Assert.Equal(secondModel.NonDefaultForeignKeysReferenceObjectLocalKey, secondModel.NonDefaultForeignKeysReferenceObject_PrincipalEntities?.FirstOrDefault()?.ReferenceKey);
        }

        [Fact]
        public void Reference_LoadsPrincipalEntityObjectCorrectly()
        {
            // Arrange
            SetupPrincipalDataLoader(false);

            // Act
            context.NonDefaultForeignKeysTestObjects!.Reference(x => x.NonDefaultForeignKeysReferenceObject_PrincipalEntity_Item).Load();

            // Assert
            var firstModel = context.NonDefaultForeignKeysTestObjects.First();

            Assert.NotNull(firstModel.NonDefaultForeignKeysReferenceObject_PrincipalEntity_Item);
            Assert.Equal(firstModel.NonDefaultForeignKeysReferenceObjectLocalKey, firstModel.NonDefaultForeignKeysReferenceObject_PrincipalEntity_Item?.ReferenceKey);
        }
    }
}
