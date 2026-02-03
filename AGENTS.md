# AGENTS.md

## TL;DR (most important)
- Primary code lives in `CoreRelm/`. Tests in `CoreRelm.Tests/`. Examples in `examples/CoreRelm.QuickStart/`.
- Always validate changes with:
  - `dotnet build`
  - `dotnet test`
- Do **not** hand-edit generated docs under `docs/api/`. Update code/XML docs and rebuild docs instead.
- After changes to `RelmHelper`, naming conversion, or schema introspection, run `dotnet test`.

## Project Overview
CoreRelm is a lightweight, attribute-based data access/ORM library for modern .NET.

Documentation: https://jdaugherty-bdl.github.io/CoreRelm/index.html

### Repository layout (key areas):
- `CoreRelm/` – Core library (primary target for changes)
- `CoreRelm.Tests/` – Unit tests
- `examples/CoreRelm.QuickStart/` – Runnable examples
- `docs/` – Generated docs and API reference (do not hand-edit files under `docs/api`)


## Environment

- **.NET SDK**: Core 8.0
- **Language**: C#
- **Database**: MySQL (via `MySql.Data.MySqlClient`)
- **IDE**: Visual Studio 2026


## Build, Test, Run

CLI:
- `dotnet restore`
- `dotnet build`
- `dotnet test`

DocFx:
- All commands should be run from the subdirectory `CoreRelm/docs/`.
- `docfx build` to build.

Visual Studio:
- Use __Rebuild Solution__ to compile.
- Use __Test Explorer__ and __Run All Tests__ to execute tests.
- To run examples, set `examples/CoreRelm.QuickStart` as startup and use __Start Debugging__.

NuGet:
- Manage packages via __Manage NuGet Packages__ on each project.
- Prefer `PackageReference` updates over adding custom restore logic.

**Correct:**
- Running tests with `dotnet test` after changes to `RelmHelper` or schema introspection.
**Incorrect:**
- Editing files under `docs/api` and expecting docs to update without rebuilding/generating.


## Configuration and DI

Connection strings are resolved by `DefaultRelmResolver_MySQL` through `Microsoft.Extensions.Configuration`. Add CoreRelm services and provide configuration:

### Typical wiring
```csharp
using CoreRelm.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCoreRelm(builder.Configuration); 
// Registers:
// - IRelmMetadataReader => RelmMetadataReader
// - IRelmSchemaIntrospector => MySqlSchemaIntrospector

var app = builder.Build();
app.Run();
```

### `appsettings.json` example:
```json
{
  "ConnectionStrings": {
    "ExampleContextDatabase": "Server=localhost;Database=example;User Id=root;Password=yourpassword;"
  }
}
```

`DefaultRelmResolver_MySQL` maps an enum/name to a connection string key and builds a `MySqlConnectionStringBuilder`.

**Correct:**
- Using `builder.Services.AddCoreRelm(builder.Configuration)` once at startup and letting `RelmHelper.UseConfiguration` wire configuration.
**Incorrect:**
- Manually new-ing `RelmMetadataReader` or `MySqlSchemaIntrospector` without registering them in the container, causing missing service resolutions.


## Models and Contexts

- Derive models from `RelmModel`.
- Use attributes:
  - `[RelmTable("table_name")]`
  - `[RelmColumn(...)]`
  - `[RelmForeignKey(ForeignKey: nameof(...), LocalKey: nameof(...))]`
- Expose datasets on a context:
  - `RelmContext` (eager metadata)
  - `RelmQuickContext` (lazy metadata)
- Prefer helper APIs from `RelmHelper` for column names, operations, and identity helpers.

**Correct:**
```csharp
using CoreRelm.Attributes;
using CoreRelm.Models;

[RelmTable("example_models")]
internal class ExampleModel : RelmModel
{
    [RelmColumn]
    public string ModelName { get; set; }

    [RelmColumn("group_internal_id")]
    public string GroupInternalId { get; set; }
}
```

**Incorrect:**
```csharp
// Missing RelmTable/RelmColumn; relies on magic strings elsewhere
internal class ExampleModel
{
    public string model_name;      // field instead of property; lacks attributes
    public string GroupInternalID; // wrong casing; incorrect column mapping
}
```

See `examples/CoreRelm.QuickStart` and `README.md` for end-to-end usage patterns.


## Naming and Database Conventions

- Column naming follows underscore conversion with special handling for `InternalId`.
- See `RelmInternal/Extensions/UnderscoreNamesHelper.cs`:
  - Uppercase and digits become `_x` segments via regex.
  - `InternalId` casing is preserved.
- Prefer attribute-based explicit names when the default conversion isn’t desired.

**Correct:**
```csharp
// "ModelIndex" becomes "model_index" unless overridden
[RelmColumn]
public int ModelIndex { get; set; }

// "InternalId" remains "InternalId" and is not lower-cased
[RelmColumn(isNullable: false, unique: true)]
public string InternalId { get; set; }
```

**Incorrect:**
```csharp
// Expecting "InternalId" to convert to "internal_id" â€” CoreRelm preserves "InternalId"
[RelmColumn]
public string InternalId { get; set; }

// Acronyms: prefer explicit name when needed
[RelmColumn("api_version")]
public int APIVersion { get; set; }
```


## Code Conventions

C# style (align with existing code):
- Naming: PascalCase for types/methods/properties; camelCase for locals/parameters, _underscoreCamelCase for private properties and members.
- Use XML documentation comments for public classes, methods, and properties.
- Keep methods cohesive; avoid unnecessary abstractions.
- Avoid magic strings; prefer helpers (e.g., `RelmHelper.GetColumnName(x => x.SomeProperty)`) or configuration file setting
- When adding new helpers or interfaces, keep them small, explicit, and discoverable.

Correct:
```csharp
/// <summary>Gets the column name for the provided property.</summary>
public string GetModelNameColumn()
{
    var column = RelmHelper.GetColumnName<ExampleModel>(x => x.ModelName);
    return column;
}
```

Incorrect:
```csharp
// Magic string; breaks if renamed or overridden via attributes
public string GetModelNameColumn() => "model_name";
```


## Metadata, Introspection, and Migrations

- `IRelmMetadataReader` => `RelmMetadataReader`
- `IRelmSchemaIntrospector` => `MySqlSchemaIntrospector`
- Migration planner/SQL renderer interfaces exist; registration may be added when the feature stabilizes:
  - `IRelmMigrationPlanner`
  - `IRelmMigrationSqlRenderer`

If contributing to introspection or migrations:
- Keep MySQL-specific logic isolated.
- Add coverage for common edge cases (indexes, FKs, triggers).

Guidance:
- Keeping MySQL-specific logic isolated in `MySqlSchemaIntrospector` and related MySQL types.
- Adding tests that assert discovered columns/indexes/FKs match expected schema shapes.


## Testing

- Add/extend unit tests in `CoreRelm.Tests`.
- Favor focused tests around helpers, resolvers, loaders, and context behaviors.

Correct:
```csharp
[Fact]
public void GetColumnName_UsesAttributeOverride()
{
    var column = RelmHelper.GetColumnName<ExampleModel>(x => x.GroupInternalId);
    Assert.Equal("group_internal_id", column);
}
```

Incorrect:
```csharp
// Asserting hard-coded values without using helpers makes tests brittle
[Fact]
public void Column_Is_Correct()
{
    Assert.Equal("group_internal_id", "group_internal_id");
}
```


## Boundaries (Always / Ask first / Never)

### Always
- Keep solution building and tests passing (`dotnet build`, `dotnet test`)
- Update XML docs when public API changes
- Keep examples compiling and runnable

### Ask first (if running as an agent)
- Adding new dependencies
- Changing public APIs in a breaking way
- Editing CI/build/doc pipelines

### Never
- Hand-edit generated documentation under `docs/api/`
- Delete files via shell commands
- Use git commands (if you want read-only git, adjust this rule accordingly)

## PR checklist

- Builds cleanly: `dotnet build`
- Tests pass: `dotnet test`
- Added/updated tests where behavior changed
- Public APIs have XML docs
- No edits to `docs/api` generated files
- Changes respect existing naming/attribute conventions
- Consider DI wiring in `AddCoreRelm` if introducing new core services


## Quick Links

- README: `README.md`
- Quickstart: `examples/CoreRelm.QuickStart`
- Core helpers: `CoreRelm/RelmHelper.cs`
- Attributes: `CoreRelm/Attributes/*`
- DI extensions: `CoreRelm/Extensions/CoreRelmServiceCollectionExtensions.cs`
- MySQL resolver: `CoreRelm/RelmInternal/Resolvers/DefaultRelmResolver_MySQL.cs`
