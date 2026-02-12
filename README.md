# CoreRelm

**CoreRelm** is a lightweight, attribute-based ORM / data access layer for C# and .NET Framework developers who want a small, predictable alternative to heavyweight ORMs.

It sits close to ADO.NET, but gives you:

- Strongly-typed table/column access via attributes and expressions
- A simple context model (`RelmContext`)
- Helper methods for the common database shapes you actually use
- A clean, explicit pattern for transactions and error handling

It’s especially aimed at **.NET Core** apps and services that need something quick and focused, but it also works from modern .NET projects.

> ⚠️ **Status:** CoreRelm is under active development. APIs and examples may evolve as features are added and real-world scenarios are covered. Feedback, issues, and PRs are welcome.

> ❗ **.NET Core 9+ ONLY** ❗ For the .NET Framework 4.8 version of this library, please go to https://github.com/jdaugherty-bdl/SimpleRelm 

Documentation: https://jdaugherty-bdl.github.io/CoreRelm/index.html

---

## Features

- **Lightweight ORM**  
  Thin abstraction on top of ADO.NET to keep things transparent and predictable.

- **POCO-friendly mapping**  
  Map query results directly into your own C# classes without invasive attributes or base classes.

- **Explicit transactions**  
  You stay in control of when transactions begin, commit, and roll back — no hidden magic.

- **`using`-friendly API**  
  Designed to be used in a `using` block so connections/transactions are cleaned up correctly.

- **Framework & Core friendly**  
  Built with traditional .NET Framework apps in mind, but usable from modern .NET (Core) projects as well.

- **Actively evolving**  
  New features and refinements are being added as the library is used in real projects.

---

## Key concepts

CoreRelm revolves around a few core pieces:

- **Models**  
  Inherit from `RelmModel` and decorate with attributes such as:
  - `[RelmTable("example_models")]`
  - `[RelmColumn]` / `[RelmColumn("actual_column_name")]`
  - `[RelmForeignKey(...)]`

  ```csharp
  using CoreRelm.Attributes;
  using CoreRelm.Models;

  [RelmTable("example_models")]
  internal class ExampleModel : RelmModel
  {
      [RelmColumn]
      public string GroupInternalId { get; set; }   // group_InternalId

      [RelmColumn]
      public string ModelName { get; set; }         // model_name

      [RelmColumn]
      public int ModelIndex { get; set; }           // model_index

      [RelmColumn("bool_column")]
      public bool IsBoolColumn { get; set; }        // bool_column

      [RelmForeignKey(
          ForeignKey: nameof(ExampleGroup.InternalId),
          LocalKey: nameof(GroupInternalId))]
      public virtual ExampleGroup Group { get; set; }
  }
  ```

- **Contexts**  
  You create your own context classes that inherit from:

  - `RelmContext` – **eager**: preloads table metadata up-front by turning on auto-initialize data sets and auto-verify tables
  - `RelmContext` – **lazy**: loads metadata on first use by turning off auto-initialize data sets and auto-verify tables

  and expose your datasets as `IRelmDataSet` properties:

  ```csharp
  internal class ExampleContext : RelmContext
  {
      public ExampleContext(
          bool autoOpenConnection = true,
          bool autoOpenTransaction = false,
          bool allowUserVariables = false,
          bool convertZeroDateTime = false,
          int  lockWaitTimeoutSeconds = 0)
          : base("name=ExampleContextDatabase",
                 autoOpenConnection:  autoOpenConnection,
                 autoOpenTransaction: autoOpenTransaction,
                 allowUserVariables:  allowUserVariables,
                 convertZeroDateTime: convertZeroDateTime,
                 lockWaitTimeoutSeconds: lockWaitTimeoutSeconds)
      {
      }

      public IRelmDataSet ExampleModels { get; set; }
      public IRelmDataSet ExampleGroups { get; set; }
  }
  ```

- **Helpers & interfaces**  
  The `RelmHelper` static class and `IRelmContext` / `IRelmQuickContext`
  interfaces provide the main API surface:

  - `RelmHelper.GetDalTable()`
  - `RelmHelper.GetColumnName(x => x.SomeProperty)`
  - `RelmHelper.StandardConnectionWrapper(...)`
  - `RelmHelper.DoDatabaseWork(...)`
  - `RelmHelper.GetDataObject(...) / GetDataObjects(...)`
  - `RelmHelper.GetLastInsertId(...)`
  - `RelmHelper.GetIdFromInternalId(...)`

  Most of these also exist as instance methods on `IRelmContext` (`relmContext.DoDatabaseWork(...)`, etc.).

---

## Features

From the Quickstart examples, CoreRelm currently provides:

- **Attribute-based mapping**
  - `[RelmTable]`, `[RelmColumn]`, `[RelmForeignKey]` give you strongly-typed access to table and column names without scattering strings everywhere.
  - `RelmHelper.GetDalTable()` and `RelmHelper.GetColumnName(...)` use those attributes.

- **Two context modes**
  - `RelmContext` – **eager** reads the database and preloads datasets/metadata when created (slightly heavier startup, faster subsequent operations).
  - `RelmContext` – **lazy** lazy-loads metadata as needed (faster startup, first operations may be slower).

- **Standard connection wrapper**
  - `RelmHelper.StandardConnectionWrapper(...)` lets you run a lambda with a raw `connection` and `transaction` without manually wiring up boilerplate.

- **Data access helpers**
  - `DoDatabaseWork` for command/query execution (with or without parameters, with or without return values).
  - `GetDataObject` / `GetDataObjects` for object-shaped results.
  - Additional examples show DataRow/DataTable/DataList usage.

- **Identity helpers**
  - `GetLastInsertId` to retrieve the last auto-increment ID.
  - `GetIdFromInternalId` to resolve IDs from internal GUIDs.

- **Bulk operations**
  - `BulkTableWriterExamples` demonstrates writing multiple rows efficiently via the bulk writer helpers.

- **Explicit transaction handling**
  - `autoOpenTransaction: true` plus a `try/catch` pattern where you explicitly call `RollbackTransactions()` on failure.

Concrete usage for all of these lives under:

```text
examples/CoreRelm.Quickstart
```

---

## Getting started

### 1. Add CoreRelm to your solution

Right now the library is consumed as a project reference:

1. Clone this repository.
2. Add the `CoreRelm` project to your solution.
3. Add a reference from your application to the `CoreRelm` project.

(When/if a NuGet package is published, this can become a simple `dotnet add package CoreRelm` step.)

### 2. Define a model and context

Use attributes on your models and expose `IRelmDataSet` properties on a context that inherits from `RelmContext`.

The Quickstart project includes:

- `Models/ExampleModel.cs`
- `Models/ExampleGroup.cs`
- `Contexts/ExampleContext.cs`

These show the expected pattern end-to-end.

---

## Usage patterns

### Using RelmContext vs RelmQuickContext

The Quickstart `Program.cs` demonstrates both:

```csharp
// Relm Context: preloads datasets and metadata
using (var relmContext = new ExampleContext())
{
    var identityExamples   = new Examples.Identity.IdentityExamples();
    var dataRowExamples    = new Examples.Data.DataRowExamples();
    var dataTableExamples  = new Examples.Data.DataTableExamples();
    var dataObjectExamples = new Examples.Data.DataObjectExamples();
    var dataListExamples   = new Examples.Data.DataListExamples();

    identityExamples.RunExamples(relmContext);
    dataRowExamples.RunExamples(relmContext);
    dataTableExamples.RunExamples(relmContext);
    dataObjectExamples.RunExamples(relmContext);
    dataListExamples.RunExamples(relmContext);
}

// Relm Quick Context: lazy-loads metadata on first use
using (var relmQuickContext = new ExampleContext(autoInitializeDataSets: false, autoVerifyTables: false))
{
    var identityExamples   = new Examples.Identity.IdentityExamples();
    var dataRowExamples    = new Examples.Data.DataRowExamples();
    var dataTableExamples  = new Examples.Data.DataTableExamples();
    var dataObjectExamples = new Examples.Data.DataObjectExamples();
    var dataListExamples   = new Examples.Data.DataListExamples();

    identityExamples.RunExamples(relmQuickContext);
    dataRowExamples.RunExamples(relmQuickContext);
    dataTableExamples.RunExamples(relmQuickContext);
    dataObjectExamples.RunExamples(relmQuickContext);
    dataListExamples.RunExamples(relmQuickContext);
}
```

### Recommended transaction pattern

When you want CoreRelm to manage a transaction for you, pass `autoOpenTransaction: true` and explicitly roll back on error:

```csharp
using (var relmContext = new ExampleContext(autoOpenTransaction: true))
{
    try
    {
        var databaseWorkExamples    = new Examples.Data.DatabaseWorkExamples();
        var bulkTableWriteExamples  = new Examples.BulkWriter.BulkTableWriterExamples();

        databaseWorkExamples.RunExamples(relmContext);
        bulkTableWriteExamples.RunExamples(relmContext);
        // On success, the transaction is allowed to complete normally.
    }
    catch (Exception ex)
    {
        // On failure, explicitly roll back any open transactions.
        relmContext.RollbackTransactions();
        Console.WriteLine($"An error occurred: {ex.Message}");
        throw;
    }
}
```

If you just need quick, one-off access to a connection/transaction without a full context, you can use:

```csharp
using static CoreRelm.Quickstart.Enums.ConnectionStrings;

var result = RelmHelper.StandardConnectionWrapper(
    ConnectionStringTypes.ExampleContextDatabase,
    (connection, transaction) =>
    {
        // Use the connection and transaction as needed
        return true;
    },
    ExceptionHandler: (exception, st) =>
    {
        Console.WriteLine($"An error occurred: {exception.Message}");
    });
```

---

For more detailed examples (DataRow/DataTable/DataObject/DataList, identity helpers, bulk writer, etc.), see the files under:

```text
examples/CoreRelm.Quickstart/Examples
```