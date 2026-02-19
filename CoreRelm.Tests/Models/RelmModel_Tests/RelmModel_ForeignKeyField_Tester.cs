using Moq;
using MySql.Data.MySqlClient;
using CoreRelm.Extensions;
using CoreRelm.Models;
using CoreRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreRelm.Tests.Models.RelmModel_Tests
{
    [Collection("JsonConfiguration")]
    public class RelmModel_ForeignKeyField_Tester : IClassFixture<JsonConfigurationFixture>
    {
        private readonly IConfiguration _configuration;
        private ComplexTestContext context;

        public RelmModel_ForeignKeyField_Tester(JsonConfigurationFixture fixture)
        {
            _configuration = fixture.Configuration;
        }

        private Mock<RelmDefaultDataLoader<ComplexReferenceObject>> SetupSingleReturnReferenceDataLoader(bool addSecondId, bool haveTwoRoots)
        {
            var mockComplexReferenceObjects = new List<ComplexReferenceObject>
            {
                new ComplexReferenceObject { ComplexTestModelInternalId = "ID1", TestModel = null },
            };

            if (haveTwoRoots)
                mockComplexReferenceObjects.Add(new ComplexReferenceObject { ComplexTestModelInternalId = "ID2", TestModel = null });

            if (addSecondId)
                mockComplexReferenceObjects.Add(new ComplexReferenceObject { ComplexTestModelInternalId = "ID1", TestModel = null });

            new ServiceCollection().AddCoreRelm(_configuration);
            context = new ComplexTestContext(autoInitializeDataSets: false, autoVerifyTables: false);

            var referenceDataLoader = new Mock<RelmDefaultDataLoader<ComplexReferenceObject>>(context);
            referenceDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            referenceDataLoader.Setup(x => x.GetLoadDataAsync(It.IsAny<CancellationToken>())).CallBase();
            referenceDataLoader.Setup(x => x.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockComplexReferenceObjects);

            return referenceDataLoader;
        }

        [Fact]
        public void RelmModel_LoadForeignKeyField_ComplexObject_ThrowsException()
        {
            // Arrange
            var complexTestModel = new ComplexTestModel
            {
                InternalId = "ID1"
            };

            var modelDataLoader = SetupSingleReturnReferenceDataLoader(true, false);

            context.GetDataSet<ComplexReferenceObject>()?.SetDataLoader(modelDataLoader.Object);
            //context.ComplexReferenceObjects!.SetDataLoader(modelDataLoader.Object);

            // Act
            var exception = Record.Exception(() => complexTestModel.LoadForeignKeyField(context, x => x.ComplexReferenceObject));

            // Assert
            /*
            Assert.NotNull(exception?.InnerException);
            Assert.IsType<MySqlException>(exception);
            Assert.IsType<MySqlException>(exception.InnerException);
            */
            Assert.NotNull(exception);
            Assert.IsType<InvalidOperationException>(exception);
        }

        [Fact]
        public void RelmModel_LoadForeignKeyField_ComplexObject()
        {
            // Arrange
            var complexTestModel = new ComplexTestModel
            {
                InternalId = "ID1"
            };

            var dataLoader = SetupSingleReturnReferenceDataLoader(false, false);

            // Act
            complexTestModel.LoadForeignKeyField(new ComplexTestContext(autoVerifyTables: false), x => x.ComplexReferenceObject, dataLoader.Object);

            // Assert
            dataLoader.Verify(r => r.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()));

            Assert.NotNull(complexTestModel.ComplexReferenceObject);
            Assert.Equal(complexTestModel.InternalId, complexTestModel.ComplexReferenceObject.ComplexTestModelInternalId);
        }

        [Fact]
        public void RelmModel_LoadForeignKeyField_ComplexObjects()
        {
            // Arrange
            var complexTestModel = new ComplexTestModel
            {
                InternalId = "ID1"
            };

            var dataLoader = SetupSingleReturnReferenceDataLoader(true, false);

            // Act
            complexTestModel.LoadForeignKeyField(new ComplexTestContext(autoVerifyTables: false), x => x.ComplexReferenceObjects, dataLoader.Object);

            // Assert
            dataLoader.Verify(r => r.PullDataAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()));

            Assert.NotNull(complexTestModel.ComplexReferenceObjects);
            Assert.Equal(2, complexTestModel.ComplexReferenceObjects.Count);
            Assert.Equal(complexTestModel.InternalId, complexTestModel.ComplexReferenceObjects?.FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(complexTestModel.InternalId, complexTestModel.ComplexReferenceObjects?.Skip(1).FirstOrDefault()?.ComplexTestModelInternalId);
        }
    }
}
