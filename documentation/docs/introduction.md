# Introduction

CoreRelm is a lightweight, attribute-based ORM for C# that stays close to ADO.NET style development while reducing repetitive data access code.

It is designed for developers who want:

- Strong control over SQL and transactions
- Attribute-driven model mapping
- Predictable behavior without heavy abstraction
- A simple context and dataset workflow
- A MySQL-focused ORM for .NET 8+

CoreRelm favors explicitness over magic. The library gives you structure for models, datasets, options, helpers, and migrations, while still keeping the database visible in the way you work.

## What CoreRelm is trying to be

CoreRelm is intended to sit in the space between writing raw ADO.NET everywhere and adopting a heavier ORM abstraction.

At a high level, it provides:

- Attribute-based model metadata
- A `RelmContext`-centered workflow
- `RelmDataSet` access patterns for models
- Options-based context configuration
- Utility helpers for common relational tasks
- Migrations tooling and schema-oriented APIs
- Support for both eager and lazy context initialization patterns

The goal is not to hide the database. The goal is to make common relational work more structured, more readable, and more repeatable.

## Who CoreRelm is for

CoreRelm is a strong fit when you want:

- A practical ORM that still feels close to the database
- Clear transaction boundaries
- Attribute-driven mapping instead of large external configuration systems
- A model and dataset workflow that is simple to reason about
- A codebase where explicit behavior is preferred over implicit conventions

It may be especially appealing if you like the directness of lower-level database code but want more consistency and reuse around model definition, querying patterns, relationships, and schema workflows.

## Supported platform and provider model

CoreRelm currently targets:

- .NET 8+
- MySQL 8+ only

The current provider story is intentionally focused. The library and documentation should be read with MySQL as the active provider target.

## Core building blocks

Most of the library revolves around a few core ideas:

### `RelmContext`

`RelmContext` is the main runtime entry point. It represents a configured database context and is the place where connection behavior, transaction behavior, dataset access, and lower-level execution helpers come together.

### `RelmDataSet`

A `RelmDataSet` represents a model-oriented access surface for a particular entity type. This is where many common operations such as querying, loading, creating, and referencing related data are expressed.

### `RelmModel`

A `RelmModel` represents a mapped entity. Attributes applied to the model and its members describe how the model relates to the database.

### `RelmContextOptions` and `RelmContextOptionsBuilder`

Context options define how a CoreRelm context behaves. The options builder is the main way to configure connection details, auto-open behavior, initialization behavior, and related settings.

## Design style

CoreRelm follows a few broad design preferences:

- Explicit transaction handling is preferred
- Configuration should be readable and close to usage
- Metadata should live near the model when practical
- Examples should show the shape of the API clearly
- Runtime behavior should be understandable without guessing

That design style is visible in the QuickStart project as well. The examples there are documentation-style examples meant to show what a call may look like. They are not intended to serve as a polished end-to-end sample application.

## What you will find in this documentation

This guide is organized to help different kinds of readers move through the library effectively.

If you are new to CoreRelm:

1. Start with [Getting Started](getting-started.md)
2. Review the Core Concepts section
3. Move into the Working with Data section

If you already know what you need:

- Use the guide pages for narrative explanations and recommended patterns
- Use the API reference for type and member details
- Use `examples/CoreRelm.QuickStart` to see documentation-style usage examples

## CoreRelm versus EF and Dapper

CoreRelm is not trying to replace every ORM style.

Compared to Entity Framework, CoreRelm generally favors:

- More explicit behavior
- Less hidden runtime convention
- A simpler mental model for developers who want direct control
- A closer relationship to relational and connection-level details

Compared to Dapper, CoreRelm generally offers:

- More built-in structure around models and datasets
- Attribute-based mapping and metadata
- A more unified ORM-style surface for repeated patterns
- Schema- and migrations-related concepts in the same ecosystem

In practice, CoreRelm is best understood as a deliberate tradeoff: more structure than a very thin micro-ORM, but more explicitness than a heavier convention-driven ORM.

## Where to go next

The best next step is [Getting Started](getting-started.md).

That page will walk through:

- how CoreRelm is configured
- how contexts are initialized
- what a minimal usage flow looks like
- where the QuickStart project fits into the documentation
