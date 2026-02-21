using CoreRelm.Attributes;
using CoreRelm.RelmInternal.Helpers.Migrations.MigrationPlans;
using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CoreRelm.Tests.Migrations;

[Collection("JsonConfiguration")]
public sealed class DesiredSchemaBuilderTests_V2 : IClassFixture<JsonConfigurationFixture>
{
    private readonly IConfiguration _configuration;

    public DesiredSchemaBuilderTests_V2(JsonConfigurationFixture fixture)
    {
        _configuration = fixture.Configuration;
        RelmHelper.UseConfiguration(_configuration);
    }

    [Fact]
    public void Build_CreatesTablesDeterministically_ByTableName()
    {
        // Arrange
        var builder = new DesiredSchemaBuilder(log: null);

        var models = new List<ValidatedModelType>
        {
            new(typeof(B_Table_Model), "db1", "b_table"),
            new(typeof(A_Table_Model), "db1", "a_table"),
        };

        // Act
        var snapshot = builder.Build("db1", models);

        // Assert: order should be deterministic because builder iterates models ordered by TableName
        var keys = snapshot.Tables.Keys.ToList();
        Assert.Equal(new[] { "a_table", "b_table" }, keys);
    }

    [Fact]
    public void Build_AssignsBaseColumnsFirst_WithOrdinalChain()
    {
        // Arrange
        var builder = new DesiredSchemaBuilder(log: null);

        var models = new List<ValidatedModelType>
        {
            new(typeof(A_Table_Model), "db1", "a_table")
        };

        // Act
        var snapshot = builder.Build("db1", models);
        var table = snapshot.Tables["a_table"];

        // Assert: base columns exist
        Assert.True(table.Columns.ContainsKey("id"));
        Assert.True(table.Columns.ContainsKey("active"));
        Assert.True(table.Columns.ContainsKey("InternalId"));
        Assert.True(table.Columns.ContainsKey("create_date"));
        Assert.True(table.Columns.ContainsKey("last_updated"));

        // Assert: base columns appear in the intended order via OrdinalPosition + AfterColumnName chain
        var id = table.Columns["id"];
        var active = table.Columns["active"];
        var internalId = table.Columns["InternalId"];
        var createDate = table.Columns["create_date"];
        var lastUpdated = table.Columns["last_updated"];

        Assert.Equal(1, id.OrdinalPosition);
        Assert.Null(id.AfterColumnName);

        Assert.Equal(4, active.OrdinalPosition);
        Assert.Equal("group_InternalId", active.AfterColumnName);

        Assert.Equal(5, internalId.OrdinalPosition);
        Assert.Equal("active", internalId.AfterColumnName);

        Assert.Equal(6, createDate.OrdinalPosition);
        Assert.Equal("InternalId", createDate.AfterColumnName);

        Assert.Equal(7, lastUpdated.OrdinalPosition);
        Assert.Equal("create_date", lastUpdated.AfterColumnName);
    }

    [Fact]
    public void Build_ConvertsColumnNames_WithInternalIdSpecialCase()
    {
        // Arrange
        var builder = new DesiredSchemaBuilder(log: null);

        var models = new List<ValidatedModelType>
        {
            new(typeof(A_Table_Model), "db1", "a_table")
        };

        // Act
        var snapshot = builder.Build("db1", models);
        var table = snapshot.Tables["a_table"];

        // Assert:
        // - CompanyName -> company_name (underscore + lower)
        // - GroupInternalId -> group_InternalId (InternalId casing preserved as per helper)
        Assert.True(table.Columns.ContainsKey("company_name"));
        Assert.True(table.Columns.ContainsKey("group_InternalId"));
    }

    [Fact]
    public void Build_CreatesForeignKeySchema_FromRelmForeignKeyAttribute()
    {
        // Arrange
        var builder = new DesiredSchemaBuilder(log: null);

        var models = new List<ValidatedModelType>
        {
            new(typeof(PrincipalGroup), "db1", "groups"),
            new(typeof(DependentModel), "db1", "items")
        };

        // Act
        var snapshot = builder.Build("db1", models);
        var dependent = snapshot.Tables["items"];

        // Assert: FK exists; name is FK_{table}_{navPropUnderscored}
        // nav prop = Group => "group"
        var expectedFkName = "FK_items_group";
        Assert.True(dependent.ForeignKeys.ContainsKey(expectedFkName));

        var fk = dependent.ForeignKeys[expectedFkName];
        Assert.Equal("items", fk.TableName);
        Assert.Equal("groups", fk.ReferencedTableName);

        // Local column should be "group_InternalId"
        Assert.NotNull(fk.ColumnNames);
        Assert.Contains("group_InternalId", fk.ColumnNames!);

        // Referenced column should be "InternalId"
        Assert.NotNull(fk.ReferencedColumnNames);
        Assert.Contains("InternalId", fk.ReferencedColumnNames!);

        // Rules are fixed by builder currently
        Assert.Equal("RESTRICT", fk.UpdateRule);
        Assert.Equal("CASCADE", fk.DeleteRule);
    }

    [Fact]
    public void Build_ThrowsOnCrossDatabaseForeignKey()
    {
        // Arrange
        var builder = new DesiredSchemaBuilder(log: null);

        var modelsDb1 = new List<ValidatedModelType>
        {
            // dependent in db1
            new(typeof(CrossDbDependent), "db1", "items")
        };

        // Act + Assert: builder rejects cross-db FK
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build("db1", modelsDb1));
        Assert.Contains("Cross-database FK not supported", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // --------------------------
    // Test models
    // --------------------------

    [RelmDatabase("db1")]
    [RelmTable("a_table")]
    private sealed class A_Table_Model : CoreRelm.Models.RelmModel
    {
        [RelmColumn]
        public string CompanyName { get; set; } = string.Empty;

        [RelmColumn]
        public string GroupInternalId { get; set; } = string.Empty;
    }

    [RelmDatabase("db1")]
    [RelmTable("b_table")]
    private sealed class B_Table_Model : CoreRelm.Models.RelmModel
    {
        [RelmColumn]
        public string SomeValue { get; set; } = string.Empty;
    }

    [RelmDatabase("db1")]
    [RelmTable("groups")]
    private sealed class PrincipalGroup : CoreRelm.Models.RelmModel
    {
        // RelmModel base is expected to provide InternalId etc.
        [RelmColumn]
        public string GroupName { get; set; } = string.Empty;
    }

    [RelmDatabase("db1")]
    [RelmTable("items")]
    private sealed class DependentModel : CoreRelm.Models.RelmModel
    {
        [RelmColumn]
        public string GroupInternalId { get; set; } = string.Empty;

        [RelmForeignKey(foreignKey: nameof(PrincipalGroup.InternalId), localKey: nameof(GroupInternalId))]
        public PrincipalGroup? Group { get; set; }
    }

    [RelmDatabase("db2")]
    [RelmTable("groups")]
    private sealed class PrincipalGroup_Db2 : CoreRelm.Models.RelmModel
    {
        [RelmColumn]
        public string GroupName { get; set; } = string.Empty;
    }

    [RelmDatabase("db1")]
    [RelmTable("items")]
    private sealed class CrossDbDependent : CoreRelm.Models.RelmModel
    {
        [RelmColumn]
        public string GroupInternalId { get; set; } = string.Empty;

        // Cross-db FK: db1 -> db2
        [RelmForeignKey(foreignKey: nameof(PrincipalGroup_Db2.InternalId), localKey: nameof(GroupInternalId))]
        public PrincipalGroup_Db2? Group { get; set; }
    }
}