# Contexts and DataSets

This page explains how CoreRelm organizes runtime access through contexts and data sets.

If models describe what your entities look like, contexts and datasets describe how you work with them at runtime.

## The role of `RelmContext`

`RelmContext` is the main runtime object used to work with CoreRelm. It is where configuration, connection behavior, transaction behavior, model access, and lower-level execution helpers come together.

In a typical application, the context becomes the main entry point for:

- opening and using a configured runtime
- accessing model-oriented datasets
- beginning and controlling transactions
- coordinating work across multiple entities

## The role of `RelmDataSet<T>`

A `RelmDataSet<T>` is the model-oriented access surface for a specific entity type. It is where common operations such as querying, creating, loading, and referencing related data are expressed.

From a design perspective:

- the model describes the entity
- the dataset is the access surface for that entity
- the context is where datasets and runtime behavior come together

## Defining a context

Your updated Getting Started page describes the context as the main runtime object and shows an `ExampleContext` that exposes model-oriented data access members.

### Example context shape

```csharp
public class ExampleContext(RelmContextOptions contextOptions) : RelmContext(contextOptions)
{
    public IRelmDataSet<ExampleModel>? ExampleModels { get; set; }
    public IRelmDataSet<ExampleGroup>? ExampleGroups { get; set; }
}
```

This is a good mental model for how a context is typically defined:

- inherit from `RelmContext`
- accept `RelmContextOptions`
- expose one or more datasets for your mapped entities

## What a context is responsible for

A CoreRelm context usually carries responsibility for four things:

### 1. Configuration

The context is created from `RelmContextOptions`, which determine how it behaves at runtime. That includes connection and initialization behavior.

### 2. Dataset access

The context is the home of datasets such as `ExampleModels` and `ExampleGroups`. This is how model-specific data access becomes discoverable and organized.

### 3. Transactions

The context is where explicit transaction control usually lives. Your Getting Started example shows a clear `BeginTransaction` / `CommitTransaction` / `RollbackTransaction` pattern.

### 4. Lower-level helpers

The context also provides access to lower-level operations such as identity helpers, dataset retrieval, and other direct runtime behaviors. Your Quick Start in 60 Seconds example uses helpers like `GetLastInsertId`, `GetIdFromInternalId`, and `GetDataSet<T>()`.
## Context creation patterns

The updated Getting Started page shows a simple build pattern:

```csharp
var relmContext = new RelmContextOptionsBuilder("name=ExampleConnectionString")
    .Build<ExampleContext>();
```

This is a very useful pattern because it keeps configuration and context creation readable and close together.

## Datasets as the working surface

Once the context exists, datasets become one of the main working surfaces.

For example:

```csharp
var exampleActiveModels = context
    .ExampleModels
    .Where(model => model.Active == true && model.Id > 5)
    .OrderByDescending(model => model.CreateDate)
    .Load();
```

This demonstrates the general idea:

- start from a context
- work through the relevant dataset
- express a query
- materialize the result shape you need

## Lazy dataset access

Your updated Getting Started page also shows a manual dataset-access pattern:

```csharp
if (context.ExampleModels == null)
    context.GetDataSet<ExampleModel>();
```

This is important because it connects directly to the eager versus quick context distinction. In a quick context, datasets may not be initialized up front, so retrieving them manually becomes part of the usage pattern.

## Eager versus quick context behavior

Your updated Getting Started page elevates this distinction clearly, and it belongs in the core conceptual docs.

### Eager context

An eager context initializes datasets up front and verifies tables before later operations need them. This favors readiness and can reduce later metadata work.

### Quick context

A quick context disables automatic dataset initialization and automatic table verification. This reduces up-front work and allows metadata to load as needed.
This choice is not just a minor configuration detail. It changes how the context behaves at runtime and how datasets are accessed in practice.

## Scoped usage and transactions

Contexts are also the place where transaction boundaries become visible.

### Example scoped transaction pattern

```csharp
using var scopedContext = new RelmContextOptionsBuilder("name=ExampleConnectionString")
    .Build<ExampleContext>();

scopedContext.BeginTransaction();

try
{
    // Perform database operations here

    scopedContext.CommitTransaction();
}
catch
{
    scopedContext.RollbackTransaction();
    throw;
}
```

This pattern is worth emphasizing because it reflects one of CoreRelm's central design preferences: transaction handling should be explicit and readable.

## Design guidance

A good default way to think about contexts and datasets is:

- the context is the configured runtime shell
- datasets are the model-specific entry points
- transactions belong where the operation boundary is clear
- eager versus quick behavior should be a deliberate choice

## Where to go next

After this page, the best next step is [Options and Configuration](options-and-configuration.md).

That page explains how context behavior is configured and how the builder patterns shown here fit together.
