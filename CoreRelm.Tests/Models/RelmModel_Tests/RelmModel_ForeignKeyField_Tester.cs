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

namespace CoreRelm.Tests.Models.RelmModel_Tests
{
    public class RelmModel_ForeignKeyField_Tester
    {
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

            var referenceDataLoader = new Mock<RelmDefaultDataLoader<ComplexReferenceObject>>();
            referenceDataLoader.Setup(x => x.TableName).Returns("nothing_table");
            referenceDataLoader.Setup(x => x.GetLoadData()).CallBase();
            referenceDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexReferenceObjects);

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

            var context = new ComplexTestContext(autoVerifyTables: false);
            context.ComplexReferenceObjects!.SetDataLoader(modelDataLoader.Object);

            // Act
            var exception = Record.Exception(() => complexTestModel.LoadForeignKeyField(context, x => x.ComplexReferenceObject));

            // Assert
            Assert.NotNull(exception?.InnerException?.InnerException); //?.InnerException);
            Assert.IsType<TargetInvocationException>(exception);
            //Assert.IsType<Exception>(exception.InnerException);
            Assert.IsType<MySqlException>(exception.InnerException);
            Assert.IsType<MySqlException>(exception.InnerException.InnerException);
            //Assert.IsType<MySqlException>(exception.InnerException.InnerException.InnerException);
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
            Assert.NotNull(complexTestModel.ComplexReferenceObjects);
            Assert.Equal(2, complexTestModel.ComplexReferenceObjects.Count);
            Assert.Equal(complexTestModel.InternalId, complexTestModel.ComplexReferenceObjects?.FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(complexTestModel.InternalId, complexTestModel.ComplexReferenceObjects?.Skip(1).FirstOrDefault()?.ComplexTestModelInternalId);
        }
    }
}
