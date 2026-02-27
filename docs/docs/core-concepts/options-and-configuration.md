# Options and Configuration

This page explains how CoreRelm contexts are configured and how `RelmContextOptions` and `RelmContextOptionsBuilder` shape runtime behavior.

CoreRelm treats configuration as part of the readable runtime story. Rather than hiding important behavior behind distant configuration systems, the library makes options visible near the point of use.

## Why options matter

Context options control how a CoreRelm context behaves. That includes:

- how the context connects
- whether the connection opens automatically
- whether a transaction opens automatically
- whether data sets initialize eagerly
- whether tables are verified automatically
- related provider/runtime settings

This means configuration is not just setup boilerplate. It directly affects the runtime shape of the library.

## `RelmContextOptions`

`RelmContextOptions` is the object that represents runtime configuration for a context.

In practical terms, it is the packaged result of your configuration choices. Once built, it is used to construct the context that your application will work through.

## `RelmContextOptionsBuilder`

`RelmContextOptionsBuilder` is the main entry point for creating those options. Your updated Getting Started page and README revisions both treat it as the standard configuration path.

## Constructor styles shown in the QuickStart

Your updated Getting Started page calls out several builder constructor styles shown in the QuickStart project:

- starting from defaults
- copying from an existing options object
- initializing from an enum-backed connection reference
- initializing from a connection
- initializing from a connection and transaction
- initializing from a named connection string
- initializing from a raw connection string
- initializing from explicit server, database, user, and password values
- initializing from explicit server, port, database, user, and password values

This is a useful part of the API design because it allows applications to start with the most natural entry point for their environment.

## Simple build pattern

A compact configuration pattern looks like this:

```csharp
using var context = new RelmContextOptionsBuilder("name=ExampleConnectionString")
    .Build<ExampleContext>();
```

This is often the clearest starting point for a new application because it keeps the configuration shape minimal while still using the standard builder pathway.

## Example maximal options shape

Your updated Getting Started page also includes a fuller options example:

```csharp
var options = new RelmContextOptionsBuilder("name=ExampleConnectionString")
    .SetAutoOpenConnection(true)
    .SetAutoOpenTransaction(false)
    .SetAllowUserVariables(false)
    .SetConvertZeroDateTime(false)
    .SetLockWaitTimeoutSeconds(0)
    .SetAutoInitializeDataSets(true)
    .SetAutoVerifyTables(true)
    .BuildOptions();
```

This is helpful because it shows that options are not only about the connection string. They also control runtime behavior such as connection opening, transaction behavior, and dataset/table initialization.

## Eager versus quick configuration

One of the most important runtime choices is whether you want eager or quick behavior.

### Eager configuration

An eager context initializes datasets up front and verifies tables up front.

```csharp
var options = new RelmContextOptionsBuilder("name=ExampleConnectionString")
    .SetAutoInitializeDataSets(true)
    .SetAutoVerifyTables(true)
    .BuildOptions();
```

This favors readiness and can reduce later metadata work.

### Quick configuration

A quick context disables automatic dataset initialization and automatic table verification.

```csharp
var quickOptions = new RelmContextOptionsBuilder("name=ExampleConnectionString")
    .SetAutoInitializeDataSets(false)
    .SetAutoVerifyTables(false)
    .BuildOptions();
```

This reduces up-front work and allows metadata to load as needed.

This distinction should be treated as a first-class configuration choice because it affects both startup behavior and later runtime work.

## Build patterns

Your docs now show two practical build styles.

### Build options first, then create the context

```csharp
var options = new RelmContextOptionsBuilder("name=ExampleConnectionString")
    .SetAutoInitializeDataSets(true)
    .SetAutoVerifyTables(true)
    .BuildOptions();

using var context = new ExampleContext(options);
```

This can be useful when you want to separate configuration from context construction.

### Build the context directly from the builder

```csharp
using var context = new RelmContextOptionsBuilder("name=ExampleConnectionString")
    .Build<ExampleContext>();
```

This keeps common usage short and readable.

## How to choose a configuration style

A good default strategy is:

- start with the simplest builder constructor that matches your environment
- override only the options you actually need
- make eager versus quick behavior a deliberate choice
- keep configuration readable and near the usage that depends on it

Your updated Getting Started page states this directly, and it is good advice for the broader docs as well.

## Configuration and transaction behavior

Options can also influence transaction behavior. For example, your updated Getting Started page includes an example that enables auto-open transaction behavior:

```csharp
using var relmContext = new RelmContextOptionsBuilder("name=ExampleConnectionString")
    .SetAutoOpenTransaction(true)
    .Build<ExampleContext>();
```

That can be useful in some flows, but the broader documentation still emphasizes explicit transaction boundaries as the most readable default.

## Configuration philosophy

The design preference behind the options system is straightforward:

- configuration should be understandable
- important runtime behavior should be visible
- developers should not have to guess how the context will behave

That fits the larger CoreRelm style of explicitness over magic.

## Where to go next

After this page, the next best step is to move into the Working with Data section.

That is where the configured context, data sets, and models start to come together in CRUD, querying, relationships, DTOs, loaders, and bulk operations.
