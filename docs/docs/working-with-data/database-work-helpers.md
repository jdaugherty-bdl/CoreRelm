# Database Work Helpers

This page explains where lower-level database work helpers fit into CoreRelm.

CoreRelm is not trying to hide the database completely. One of its design goals is to give you structure while still keeping relational and connection-level details understandable. Database work helpers are part of that philosophy.

## Why database work helpers exist

A model- and dataset-centered workflow is often the best default.

But some situations call for a more direct shape, such as:

- administrative operations
- reporting-oriented access
- raw or specialized data retrieval
- tasks that do not naturally fit a full model workflow
- utility operations that are easier to express closer to the database

## Database work in the QuickStart surface

The QuickStart inventory includes a dedicated `DatabaseWorkExamples` file, along with separate examples for data rows, data tables, data objects, and data lists.

That suggests CoreRelm intentionally supports a layered data-access model:

- entity-oriented access when you want it
- lower-level access when you need it

## Lower-level helpers and identity utilities

The current docs and examples also show direct helper-style operations such as:

- `GetLastInsertId()`
- `GetIdFromInternalId(...)`
- `GetDataSet<T>()`
- `RelmHelper.GetDalTable<T>()`

These are useful because they support practical relational workflows without forcing the developer to abandon the broader CoreRelm model.

## Example helper shape

```csharp
var lastInsertId = context.GetLastInsertId();

var tableName = RelmHelper.GetDalTable<ExampleModel>();
var internalId = "some-guid-value";
var idFromInternalId = context.GetIdFromInternalId(tableName, internalId);
```

This is a good example of CoreRelm's design style:

- still structured
- still model-aware
- still explicit about the database

## When to go lower level

A good time to use lower-level database helpers is when:

- the job is not naturally entity-oriented
- you need a data shape other than full models
- the query is closer to raw relational logic
- the helper makes the intent clearer than forcing a model path would

## Lower-level does not mean abandoning structure

One of the strengths of CoreRelm is that even lower-level work can still remain part of a readable, structured system.

That means lower-level access should still aim to be:

- discoverable
- intentional
- well-bounded
- understandable in the surrounding context

## Design guidance

A good default approach is:

- start with models and datasets when the workflow is entity-based
- move to lower-level database helpers when the job clearly calls for it
- do not force everything into one abstraction level
- keep helper usage explicit and easy to trace

## Where to go next

After this page, the next best step is to move into the Transactions, Migrations, and Advanced Topics section.

That is where reliability, schema workflows, and more advanced runtime patterns come together.
