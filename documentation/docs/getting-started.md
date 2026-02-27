# Getting Started

This page is designed to help a new user understand the basic shape of CoreRelm as quickly as possible.

CoreRelm uses a documentation-first QuickStart project. The examples are intended to demonstrate what a call may look like. They are not required to compile or run as-is.

## Before you begin

CoreRelm currently supports:

- .NET 8+
- MySQL 8+ only

The QuickStart project and the broader codebase are currently organized around that provider model.

## The basic flow

A typical CoreRelm workflow looks like this:

1. Define a model and apply mapping attributes
2. Define a context
3. Build `RelmContextOptions`
4. Create a context
5. Read or write data through the context and its datasets
6. Use explicit transactions when an operation spans multiple writes

## Define a model

In order to work with CoreRelm, you need to define models that represent your database entities. Models are classes decorated with attributes that describe how they map to database tables and columns.

To automatically include the standard metadata columns, your model should inherit from `RelmModel`. This base class includes properties for `Id`, `InternalId`, `CreateDate`, and `LastUpdated`. If you want to define a model without those standard columns, you can inherit directly from `RelmModelClean` and apply the necessary attributes.

The following example shows a simple model with a few properties and a foreign key relationship. The attributes indicate how the model maps to the database, and the navigation properties allow for easy access to related data. This model is *not* migratable as-is because column types and lengths are not specified.

### Example model shape

```csharp
[RelmTable("example_models")]
public class ExampleModel : RelmModel
{
    [RelmColumn]
    public string? Name { get; set; }

    [RelmColumn]
    public int Value { get; set; }

    [RelmColumn]
    public string? GroupInternalId { get; set; }

    [RelmForeignKey(foreignKey: nameof(ExampleGroup.InternalId), localKey: nameof(GroupInternalId))]
    public virtual ExampleGroup Group { get; set; }
}

[RelmTable("example_groups")]
public class ExampleGroup : RelmModel
{
    [RelmColumn]
    public string? Name { get; set; }

    [RelmForeignKey(foreignKey: nameof(ExampleModel.GroupInternalId), localKey: nameof(InternalId))]
    public virtual ICollection<ExampleModel> Models { get; set; }
}
```

## Configure a context

CoreRelm uses `RelmContextOptionsBuilder` to configure runtime behavior.

The QuickStart project shows multiple options builder constructor styles, including:

- starting from defaults
- copying from an existing options object
- initializing from an enum-backed connection reference
- initializing from a connection
- initializing from a connection and transaction
- initializing from a named connection string
- initializing from a raw connection string
- initializing from explicit server, database, user, and password values
- initializing from explicit server, port, database, user, and password values

In practice, most applications should start with the simplest configuration that fits their use case and only override the settings they actually need.

### Example maximal options shape

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

CoreRelm also supports a more lazy-loading style by disabling automatic dataset initialization and automatic table verification.

```csharp
var quickOptions = new RelmContextOptionsBuilder("name=ExampleConnectionString")
    .SetAutoInitializeDataSets(false)
    .SetAutoVerifyTables(false)
    .BuildOptions();
```

The eager pattern favors readiness up front. The lazy pattern favors lower startup work and loads metadata as needed.

## Define a context

A context is the main runtime object used to work with CoreRelm.

The QuickStart project includes an `ExampleContext` that inherits from `RelmContext` and exposes model-oriented data access members.

### Example context shape

```csharp
public class ExampleContext(RelmContextOptions contextOptions) : RelmContext(contextOptions)
{
    public IRelmDataSet<ExampleModel>? ExampleModels { get; set; }
    public IRelmDataSet<ExampleGroup>? ExampleGroups { get; set; }
}
```

The exact datasets you expose will depend on your application. The important part is that the context becomes the central place where configuration, connection behavior, transactions, and model access come together.

## Initialize and use a context

The QuickStart shows both standard scoped usage and a more lazy-loading quick-context style.

### Example usage shape

```csharp
var relmContext = new RelmContextOptionsBuilder("name=ExampleConnectionString")
    .Build<ExampleContext>();
```

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

CoreRelm is most readable when transaction boundaries are explicit. If an operation spans multiple writes, prefer a clear begin/commit or begin/rollback pattern.

## Common feature areas shown in the QuickStart project

The QuickStart project is broad. It currently demonstrates the shape of calls for several public feature areas, including:

- context usage
- connection examples
- identity helpers
- data row, data table, data object, and data list access patterns
- database work helpers
- bulk table writing
- attributes
- foreign keys
- DTOs
- data loaders
- model reset behavior
- model property naming conventions

Treat the QuickStart project as a usage map. It is there to show how features look in code, even when the examples are intentionally lightweight.

## Quick Start in 60 Seconds

The following example is intentionally small and focuses on the shape of the workflow. 

It will create an `ExampleContext` context that:
- Automatically opens the connection named `ExampleConnectionString` in the `appSettings.json` file.
- Opens the database connection.
- Leaves the database transaction closed.
- Automatically (eager) initializes all data sets.
- Automatically (eager) verifies data sets against database table list.

### 1. Build options & create context (one step)

```csharp
using var context = new RelmContextOptionsBuilder("name=ExampleConnectionString")
    .Build<ExampleContext>();
```

### 2. Use the context

```csharp
var lastInsertId = context.GetLastInsertId();

var tableName = RelmHelper.GetDalTable<ExampleModel>();
var internalId = "some-guid-value";
var idFromInternalId = context.GetIdFromInternalId(tableName, internalId);

if (context.ExampleModels == null)
    context.GetDataSet<ExampleModel>();

var exampleActiveModels = context
    .ExampleModels
    .Where(model => model.Active == true && model.Id > 5)
    .OrderByDescending(model => model.CreateDate)
    .Load();
```

This is not meant to be a complete application. It is meant to show the basic shape of a configured context and a few common operations.

## Eager versus quick context behavior

The QuickStart distinguishes between two high-level approaches:

### Eager context

An eager context initializes datasets withno data retrieval and verifies tables up front.

This can make some later operations faster because metadata work has already been done.

### Quick context

A quick context disables automatic dataset initialization and automatic table verification.

This reduces up-front work and allows metadata to load as needed, but the first operation for a given path may do more work.

This distinction is useful when deciding how much startup work you want versus how much lazy runtime work you are comfortable with. For example, a web application may prefer a quick context to minimize startup time, while a background processing application may prefer an eager context to ensure readiness.

## Error handling guidance

A good default pattern is:

- start a transaction explicitly when needed
- commit only after all related operations succeed
- roll back on failure
- keep error paths simple and predictable

### Example

```csharp
using var relmContext = new RelmContextOptionsBuilder("name=ExampleConnectionString")
    .SetAutoOpenTransaction(true)
    .Build<ExampleContext>();

try
{
    // Run database work examples
    // Run bulk write examples

    // Transaction will auto-commit when the context is disposed if no errors occur
}
catch (Exception ex)
{
    relmContext.RollbackTransaction();
    Console.WriteLine($"An error occurred: {ex.Message}");
}
```

## Where to go next

After this page, the best next steps are:

1. Review the Core Concepts section to understand `RelmContext`, `RelmDataSet`, `RelmModel`, and attributes
2. Move into Working with Data for CRUD, querying, relationships, DTOs, loaders, and bulk operations
3. Review Transactions, Migrations, and Advanced Topics for reliability and schema workflows
4. Use the API reference when you need type and member details
