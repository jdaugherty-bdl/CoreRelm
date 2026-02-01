using CoreRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Models.RelmModel_Tests
{
    public class RelmModel_DataLoader_Tester
    {
        public void RelmModel_LoadDataLoaderField_ComplexObject()
        {
            // Arrange
            var complexTestModel = new ComplexTestModel
            {
                InternalId = "ID1"
            };

            // Act
            //complexTestModel.LoadDataLoaderField(x => x.TestFieldBoolean);

            // Assert
            Assert.NotNull(complexTestModel.TestFieldBoolean);
            Assert.True(complexTestModel.TestFieldBoolean);
        }
    }
}
