using CoreRelm.Attributes;
using CoreRelm.Extensions;
using CoreRelm.Interfaces.Metadata;
using CoreRelm.Models;
using CoreRelm.RelmInternal.Helpers.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.Attributes.RelmUnique_Tests
{
    public class NonRelmMetadataUserModel
    {
    }

    [RelmUnique([nameof(MetadataUserModel.Name), nameof(MetadataUserModel.Email)])]
    [RelmDatabase("test_database")]
    [RelmTable("metadata_user_model")]
    public class MetadataUserModel : RelmModel
    {
        [RelmColumn(columnDbType: MySqlDbType.VarChar)]
        public string Name { get; set; } = "";

        [RelmColumn(columnDbType: MySqlDbType.VarChar)]
        public string Email { get; set; } = "";
    }

    public class RelmUnique_MetadataReader_Tests
    {
        private static IRelmMetadataReader CreateReader()
        {
            var services = new ServiceCollection();

            // minimal configuration for RelmHelper, adapt if your tests need specific values
            var config = new ConfigurationBuilder().AddInMemoryCollection([]).Build();

            services.AddCoreRelm(config);
            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IRelmMetadataReader>();
        }

        [Fact]
        public void MetadataReader_NonRelmMetadataUserModel_Throws()
        {
            var reader = CreateReader();
            Assert.Throws<ArgumentException>(() => reader.Describe(typeof(NonRelmMetadataUserModel))); // Adjust to actual API
        }

        [Fact]
        public void MetadataReader_Discovers_RelmUnique_Constraints()
        {
            var reader = CreateReader();
            var meta = reader.Describe(typeof(MetadataUserModel)); // Adjust to actual API

            // Replace with real assertions based on your metadata model:
            // e.g., meta.UniqueConstraints contains an entry with ["Name","Email"]
            Assert.NotNull(meta);
            var uniques = meta.Indexes.Where(x => x.IsUnique).ToList() ?? [];
            Assert.Contains(uniques, uc => uc.Columns.SequenceEqual(["Name", "Email"], StringComparer.OrdinalIgnoreCase));
        }
    }
}
