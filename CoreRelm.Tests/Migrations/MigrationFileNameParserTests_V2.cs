using CoreRelm.Interfaces.Migrations.MigrationFiles;
using CoreRelm.Migrations.MigrationFiles;
using CoreRelm.Models.Migrations.Tooling.Apply;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CoreRelm.Tests.Migrations;

public sealed class MigrationFileNameParserTests_V2
{
    private readonly IMigrationFileNameParser _parser = new MigrationFileNameParser();

    // ----------------------------
    // Valid cases
    // ----------------------------

    [Theory]
    [InlineData("RelmMigration_20260131_235959_init__db-ledgerlite.sql", "ledgerlite", "init", false)]
    [InlineData("SYSTEM_MIGRATION_20260131_235959_bootstrap__db-ledgerlite.sql", "ledgerlite", "bootstrap", true)]
    [InlineData("RelmMigration_20260131_000000_add_users_table__db-ledgerlite.sql", "ledgerlite", "add_users_table", false)]
    [InlineData("RelmMigration_20260131_235959_name_with__underscores__db-ledgerlite.sql", "ledgerlite", "name_with__underscores", false)]
    [InlineData("RelmMigration_20260131_235959_x__db-ledger_lite.sql", "ledger_lite", "x", false)]
    public void TryParse_ValidFileNames_ParseExpectedParts(
        string fileName,
        string expectedDb,
        string expectedSlug,
        bool expectSystem
    )
    {
        var ok = _parser.TryParse(fileName, out var parsed, out var error);

        Assert.True(ok);
        Assert.Null(error);

        Assert.Equal(expectedDb, parsed.DatabaseName);
        Assert.Equal(fileName, parsed.FileName);
        Assert.Equal(expectedSlug, parsed.MigrationSlug);

        Assert.NotNull(parsed.TimestampUtc);
        Assert.Equal(DateTimeKind.Utc, parsed.TimestampUtc!.Value.Kind);

        // SortKey should begin with the timestamp portion
        Assert.StartsWith("20260131_", parsed.SortKey);

        // System migrations should sort before regular migrations when timestamps collide
        // (based on typeRank in SortKey: system => 0, regular => 1)
        if (expectSystem)
            Assert.Contains("_0_", parsed.SortKey);
        else
            Assert.Contains("_1_", parsed.SortKey);
    }

    [Fact]
    public void TryParse_AllowsCaseInsensitiveSqlExtension()
    {
        var fileName = "RelmMigration_20260131_235959_init__db-ledgerlite.SQL";

        var ok = _parser.TryParse(fileName, out var parsed, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal("ledgerlite", parsed.DatabaseName);
        Assert.Equal("init", parsed.MigrationSlug);
    }

    [Fact]
    public void TryParse_AllowsPaths_ByTakingFileNameOnly()
    {
        var path = @"C:\migrations\RelmMigration_20260131_235959_init__db-ledgerlite.sql";

        var ok = _parser.TryParse(path, out var parsed, out var error);

        Assert.True(ok);
        Assert.Null(error);

        Assert.Equal("RelmMigration_20260131_235959_init__db-ledgerlite.sql", parsed.FileName);
        Assert.Equal("ledgerlite", parsed.DatabaseName);
        Assert.Equal("init", parsed.MigrationSlug);
    }

    // ----------------------------
    // Invalid cases: prefix, marker, extension
    // ----------------------------

    [Theory]
    [InlineData("Migration_20260131_235959_init__db-ledgerlite.sql")]
    [InlineData("RELMMIGRATION_20260131_235959_init__db-ledgerlite.sql")]
    [InlineData("RelmMigration-20260131_235959_init__db-ledgerlite.sql")]
    public void TryParse_InvalidPrefix_Fails(string fileName)
    {
        var ok = _parser.TryParse(fileName, out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("must start", error!, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("RelmMigration_20260131_235959_init.sql")]
    [InlineData("SYSTEM_MIGRATION_20260131_235959_init.sql")]
    public void TryParse_MissingDbMarker_Fails(string fileName)
    {
        var ok = _parser.TryParse(fileName, out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("__db-", error!, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("RelmMigration_20260131_235959_init__db-ledgerlite.txt")]
    [InlineData("RelmMigration_20260131_235959_init__db-ledgerlite")]
    public void TryParse_WrongExtension_Fails(string fileName)
    {
        var ok = _parser.TryParse(fileName, out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains(".sql", error!, StringComparison.OrdinalIgnoreCase);
    }

    // ----------------------------
    // Invalid cases: timestamp / slug
    // ----------------------------

    [Theory]
    [InlineData("RelmMigration_20260131_246060_init__db-ledgerlite.sql")] // invalid time
    [InlineData("RelmMigration_2026013_235959_init__db-ledgerlite.sql")]  // wrong length
    [InlineData("RelmMigration_20260131_23595_init__db-ledgerlite.sql")]  // wrong length
    [InlineData("RelmMigration_20260131-235959_init__db-ledgerlite.sql")] // wrong separator
    public void TryParse_InvalidTimestamp_Fails(string fileName)
    {
        var ok = _parser.TryParse(fileName, out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("timestamp", error!, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("RelmMigration_20260131_235959___db-ledgerlite.sql")] // empty slug
    [InlineData("RelmMigration_20260131_235959_ __db-ledgerlite.sql")] // same, explicit
    public void TryParse_EmptySlug_Fails(string fileName)
    {
        var ok = _parser.TryParse(fileName, out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("slug", error!, StringComparison.OrdinalIgnoreCase);
    }

    // ----------------------------
    // Invalid cases: database name rules
    // ----------------------------

    [Theory]
    [InlineData("RelmMigration_20260131_235959_init__db-ledger-lite.sql")] // dash not allowed
    [InlineData("RelmMigration_20260131_235959_init__db-ledger.lite.sql")] // dot not allowed
    [InlineData("RelmMigration_20260131_235959_init__db-ledger lite.sql")] // space not allowed
    [InlineData("RelmMigration_20260131_235959_init__db-.sql")]            // empty db
    public void TryParse_InvalidDatabaseName_Fails(string fileName)
    {
        var ok = _parser.TryParse(fileName, out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("Database name", error!, StringComparison.OrdinalIgnoreCase);
    }

    // ----------------------------
    // SortKey behavior
    // ----------------------------

    [Fact]
    public void SortKey_SortsByTimestamp_ThenSystemBeforeRegular_ThenSlug()
    {
        var files = new[]
        {
            "RelmMigration_20260131_120000_b__db-ledgerlite.sql",
            "SYSTEM_MIGRATION_20260131_120000_a__db-ledgerlite.sql",
            "RelmMigration_20260131_115959_z__db-ledgerlite.sql",
            "SYSTEM_MIGRATION_20260131_120000_c__db-ledgerlite.sql",
        };

        var parsed = new List<ParsedMigrationFileName>();
        foreach (var f in files)
        {
            var ok = _parser.TryParse(f, out var p, out var err);
            Assert.True(ok);
            Assert.Null(err);
            parsed.Add(p);
        }

        var ordered = parsed.OrderBy(p => p.SortKey, StringComparer.Ordinal).ToList();

        // Expected order:
        // 1) 11:59:59 (older) regular z
        // 2) 12:00:00 system a
        // 3) 12:00:00 system c
        // 4) 12:00:00 regular b
        Assert.Equal("RelmMigration_20260131_115959_z__db-ledgerlite.sql", ordered[0].FileName);
        Assert.Equal("SYSTEM_MIGRATION_20260131_120000_a__db-ledgerlite.sql", ordered[1].FileName);
        Assert.Equal("SYSTEM_MIGRATION_20260131_120000_c__db-ledgerlite.sql", ordered[2].FileName);
        Assert.Equal("RelmMigration_20260131_120000_b__db-ledgerlite.sql", ordered[3].FileName);
    }
}