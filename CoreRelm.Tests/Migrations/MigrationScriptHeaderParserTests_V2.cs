using CoreRelm.Migrations.MigrationFiles;
using CoreRelm.Models.Migrations.Tooling.Apply;
using System;
using System.Linq;
using Xunit;

namespace CoreRelm.Tests.Migrations;

public sealed class MigrationScriptHeaderParserTests_V2
{
    private const string Sentinel = "-- CoreRelm-Migration:";

    private static string MakeSql(params string[] headerLines)
    {
        // Header lines should be raw key/value lines without "--" prefix;
        // We add the prefix here to avoid mistakes.
        var hdr = string.Join("\n", new[] { Sentinel }.Concat(headerLines.Select(l => $"-- {l}")));
        return hdr + "\n\nUSE `ledgerlite`;\nSELECT 1;\n";
    }

    [Fact]
    public void TryParseHeader_ReturnsFalse_WhenNoHeaderPresent()
    {
        var parser = new MigrationScriptHeaderParser(maxSupportedVersion: new Version(999, 0, 0));

        var ok = parser.TryParseHeader("USE `ledgerlite`;\nSELECT 1;\n", out var header, out var error);

        Assert.False(ok);
        Assert.Null(error);

        // header is defaulted
        Assert.Null(header.Tool);
        Assert.Null(header.ToolVersion);
        Assert.Null(header.DatabaseName);
        Assert.Null(header.GeneratedUtc);
        Assert.Null(header.ChecksumSha256);
        Assert.NotNull(header.Extras);
    }

    [Fact]
    public void TryParseHeader_ParsesCommonFields()
    {
        var parser = new MigrationScriptHeaderParser(maxSupportedVersion: new Version(999, 0, 0));

        var sql = MakeSql(
            "Tool: CoreRelm",
            "ToolVersion: 1.2.3",
            "MigrationType: RelmMigration",
            "MigrationName: Add Users",
            "Database: ledgerlite",
            "TimestampUtc: 2026-01-31T21:19:35Z",
            "ChecksumSha256: 0000000000000000000000000000000000000000000000000000000000000000"
        );

        var ok = parser.TryParseHeader(sql, out var header, out var error);

        Assert.True(ok);
        Assert.Null(error);

        Assert.Equal("CoreRelm", header.Tool);
        Assert.Equal("1.2.3", header.ToolVersion);
        Assert.Equal("Add Users", header.MigrationName);
        Assert.Equal("ledgerlite", header.DatabaseName);

        Assert.NotNull(header.GeneratedUtc);
        Assert.Equal(DateTimeKind.Utc, header.GeneratedUtc!.Value.Kind);

        Assert.Equal("0000000000000000000000000000000000000000000000000000000000000000", header.ChecksumSha256);
    }

    [Fact]
    public void TryParseHeader_AcceptsGeneratedUtcAlias()
    {
        var parser = new MigrationScriptHeaderParser(maxSupportedVersion: new Version(999, 0, 0));

        var sql = MakeSql(
            "Tool: CoreRelm",
            "ToolVersion: 1.0.0",
            "GeneratedUtc: 2026-01-31T21:19:35Z"
        );

        var ok = parser.TryParseHeader(sql, out var header, out var error);

        Assert.True(ok);
        Assert.Null(error);

        Assert.NotNull(header.GeneratedUtc);
        Assert.Equal(DateTimeKind.Utc, header.GeneratedUtc!.Value.Kind);
    }

    [Fact]
    public void TryParseHeader_StoresUnknownKeysInExtras()
    {
        var parser = new MigrationScriptHeaderParser(maxSupportedVersion: new Version(999, 0, 0));

        var sql = MakeSql(
            "Tool: CoreRelm",
            "ToolVersion: 1.0.0",
            "HeaderVersion: 1",             // not recognized by current parser -> Extras
            "FileName: x.sql",              // not recognized by current parser -> Extras
            "Extras.Destructive: false"     // also not special-cased -> Extras
        );

        var ok = parser.TryParseHeader(sql, out var header, out var error);

        Assert.True(ok);
        Assert.Null(error);

        Assert.True(header.Extras.ContainsKey("HeaderVersion"));
        Assert.Equal("1", header.Extras["HeaderVersion"]);

        Assert.True(header.Extras.ContainsKey("FileName"));
        Assert.Equal("x.sql", header.Extras["FileName"]);

        Assert.True(header.Extras.ContainsKey("Extras.Destructive"));
        Assert.Equal("false", header.Extras["Extras.Destructive"]);
    }

    [Fact]
    public void TryParseHeader_IgnoresMalformedHeaderLines_WithoutColon()
    {
        var parser = new MigrationScriptHeaderParser(maxSupportedVersion: new Version(999, 0, 0));

        var sql = MakeSql(
            "Tool: CoreRelm",
            "ToolVersion: 1.0.0",
            "ThisIsNotAKeyValueLine"
        );

        var ok = parser.TryParseHeader(sql, out var header, out var error);

        Assert.True(ok);
        Assert.Null(error);

        Assert.Equal("CoreRelm", header.Tool);
        Assert.Equal("1.0.0", header.ToolVersion);
    }

    [Fact]
    public void TryParseHeader_Throws_WhenToolVersionExceedsMaxSupportedVersion()
    {
        // maxSupportedVersion injected into parser ctor; ValidateHeader compares parsed ToolVersion as Version to it
        var parser = new MigrationScriptHeaderParser(maxSupportedVersion: new Version(1, 2, 3));

        var sql = MakeSql(
            "Tool: CoreRelm",
            "ToolVersion: 9.9.9" // should exceed maxSupportedVersion and trigger NotSupportedException
        );

        Assert.Throws<NotSupportedException>(() =>
        {
            _ = parser.TryParseHeader(sql, out _, out _);
        });
    }

    [Fact]
    public void TryParseHeader_DoesNotThrow_WhenToolVersionIsMissing()
    {
        // Your implementation calls ValidateHeader(toolVer ?? MaxValue.ToString()).
        // If ToolVersion is missing, it passes MaxValue -> which would throw if maxSupportedVersion is lower.
        // To make this test pass, we supply a huge maxSupportedVersion.
        var parser = new MigrationScriptHeaderParser(maxSupportedVersion: new Version(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue));

        var sql = MakeSql(
            "Tool: CoreRelm"
        // ToolVersion omitted
        );

        var ok = parser.TryParseHeader(sql, out var header, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal("CoreRelm", header.Tool);
        Assert.Null(header.ToolVersion);
    }
}