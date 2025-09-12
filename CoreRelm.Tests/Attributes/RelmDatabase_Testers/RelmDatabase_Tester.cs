using CoreRelm.Attributes;
using CoreRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Attributes.RelmDatabase_Testers
{
    public class RelmDatabase_Tester
    {
        readonly RelmDatabase? relmDatabaseAttribute;

        public RelmDatabase_Tester()
        {
            var complexTestModel = new ComplexTestModel();
            var complexTestModelType = complexTestModel.GetType();

            relmDatabaseAttribute = complexTestModelType
                .GetCustomAttributes(typeof(RelmDatabase), true)
                .Cast<RelmDatabase>()
                .FirstOrDefault();
        }

        [Fact]
        public void RelmDatabase_ComplexTestModel_Has_Attribute_DatabaseName()
        {
            Assert.Equal("test_database", relmDatabaseAttribute?.DatabaseName);
        }
    }
}
