# CoreRelm

[![.NET](https://img.shields.io/badge/.NET-Core%208%2B-blue)](#support)
[![Database](https://img.shields.io/badge/MySQL-8%2B-orange)](#support)
[![Status](https://img.shields.io/badge/status-active%20development-yellow)](#support)

CoreRelm is a lightweight, attribute-based ORM for C# with a database-first, explicit style that stays close to ADO.NET while reducing repetitive data access code.

CoreRelm is a good fit when you want:

- Attribute-driven model mapping
- Explicit transaction control
- Predictable behavior and readable calls
- A simple context + dataset workflow
- MySQL 8+ focused usage

## Table of Contents

- [Support](#support)
- [Quick Start in 60 Seconds](#quick-start-in-60-seconds)
- [Features](#features)
- [Quickstart examples](#quickstart-examples)
- [Migrations](#migrations)
- [Error handling](#error-handling)
- [Documentation](#documentation)
- [CoreRelm vs EF and Dapper](#corerelm-vs-ef-and-dapper)
- [Contributing](#contributing)

## Support

- CoreRelm supports **Core 8+**
- Database provider: **MySQL 8+ only**
- Status: **Active development**

> Quickstart examples are documentation-style examples that demonstrate what a call may look like. They are not required to compile or run.

## Quick Start in 60 Seconds

### 1) Define a model

```csharp
using CoreRelm.Attributes;

[RelmTable("People")]
public class PersonModel : RelmModel
{
    [RelmColumn]
    public string FirstName { get; set; } = string.Empty; // column name: "first_name"

    [RelmColumn]
    public string LastName { get; set; } = string.Empty; // column name: "last_name"
}
```

### 2) Define a context

```csharp
using CoreRelm;
using CoreRelm.Models;

public class ExampleContext(RelmContextOptions options) : RelmContext(options)
{
    public RelmDataSet<PersonModel>? People { get; set; }
}
```

### 3) Build options and use the context

```csharp
using var context = new RelmContextOptionsBuilder("localhost", "example_database", "root", "password")
    .Build<ExampleContext>();

var person = context.People.New();
person.FirstName = "Ada";
person.LastName = "Lovelace";
person.WriteToDatabase(context);

var results = context.People
    .Where(x => x.LastName == "Lovelace")
    .OrderBy(x => x.FirstName)
    .Load();
```

## Features

CoreRelm includes support for:

- Attribute-based model mapping
- Context and dataset patterns
- Identity and key handling
- Foreign key loading patterns
- DTO and data loader patterns
- Bulk write helpers
- Options builder configuration
- Helper APIs for metadata/mapping
- Migrations tooling and schema workflows

## Quickstart examples

The `examples/CoreRelm.Quickstart` project is documentation-first and demonstrates usage shapes for public features, including:

- Context usage and connection patterns
- Dataset and data retrieval call shapes
- DTOs, data loaders, and foreign keys
- Identity, bulk writer, model reset, and attributes
- Migrations examples (documentation-style)

## Migrations

CoreRelm includes migrations-related APIs and tooling as part of the public surface.

Use the README for overview guidance, Quickstart for usage-shape examples, and `docs/api` for full type/member reference.

```csharp
// Example only: demonstrate migrations tooling call shape.

var migrationTool = new RelmMigrationTooling();
var result = migrationTool.GenerateMigration(/* model set / options */);

if (!result.Success)
{
    // inspect errors
}

var applyResult = migrationTool.ApplyMigrations(/* connection / options */);
```

## Error handling

CoreRelm works best with explicit transactions and simple error-handling paths.

```csharp
using var context = new ExampleContext(options);
context.BeginTransaction();

try
{
    // write operations
    context.CommitTransaction();
}
catch
{
    context.RollbackTransaction();

    // Optional diagnostics (where applicable):
    // context.HasError
    // context.LastExecutionError
    // context.LastExecutionException

    throw;
}
```

## Documentation

Use each source for a different depth of detail:

- `README.md` for overview and common patterns
- `examples/CoreRelm.Quickstart` for documentation-style usage examples
- `docs/api` for full API reference
- `CoreRelm.Tests` for behavioral examples and edge cases

## CoreRelm vs EF and Dapper

CoreRelm is a different tradeoff, not a replacement for every ORM style.

- Compared to **EF**: CoreRelm favors more explicit behavior and a simpler mental model.
- Compared to **Dapper**: CoreRelm adds more built-in structure (models, datasets, attributes) while preserving directness.

CoreRelm is a strong fit when you want ORM helpers without giving up explicit control.

## Contributing

Contributions are welcome.

If a public feature changes, please update:

- `README.md` (high-level guidance)
- `examples/CoreRelm.Quickstart` (usage-shape examples)
- `docs/api` (reference docs, as applicable)
- Tests (behavior coverage)
