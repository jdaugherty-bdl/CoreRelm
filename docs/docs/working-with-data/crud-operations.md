# CRUD Operations

This page explains the basic create, read, update, and delete workflow shape in CoreRelm.

CoreRelm is designed to make common relational work more structured and readable while still keeping the database visible in the way you work. In practice, that means CRUD operations are usually expressed through a configured context and its data sets.

## The basic CRUD flow

A typical CRUD workflow in CoreRelm looks like this:

1. Build or create a context
2. Access the relevant data set
3. Create, load, or query a model
4. Change the model or write a new one
5. Use explicit transactions when the operation spans multiple writes

This is intentionally direct. CoreRelm prefers explicitness over hidden behavior.

## Create

A common create flow starts from the context and a data set.

### Example shape

```csharp
var person = context.People.New();
person.FirstName = "Ada";
person.LastName = "Lovelace";
person.WriteToDatabase(context);
```

This pattern is useful because it keeps the operation readable:

- start from the configured runtime
- create a new model instance through the relevant data set
- populate values
- write the model using the context

Depending on the surrounding workflow, you may also use model-level save patterns in other examples. The important concept is that the create flow remains visible and easy to follow.

## Read

CoreRelm supports both direct lookup and broader query-style retrieval.

### Find a single model

```csharp
var found = context.People.Find(person.InternalId);
```

This is the narrow "get me this one model" shape.

### Query for multiple models

```csharp
var results = context.People
    .Where(x => x.LastName == "Lovelace")
    .OrderBy(x => x.FirstName)
    .Load();
```

This is the broader "filter, shape, and materialize" flow.

## Update

A common update shape is:

1. Load or find a model
2. Change one or more properties
3. Persist the updated model

### Example shape

```csharp
var found = context.People.Find(person.InternalId);

if (found != null)
{
    found.LastName = "Byron";
    found.WriteToDatabase(context);
}
```

In CoreRelm, the update step is usually easy to reason about because the model is explicit and the persistence step is visible.

## Delete

Delete examples should follow the same philosophy as the rest of the library:

- make the boundary clear
- make the target clear
- make the persistence step clear

### Example shape

```csharp
var found = context.People.Find(person.InternalId);

if (found != null)
{
    found.DeleteFromDatabase(context);
}
```

Even if the exact helper methods vary by feature area, the documentation should keep the mental model consistent: load the target, then perform an explicit removal step.

## Using data sets as the primary surface

For everyday CRUD work, the most common entry point is the model-specific data set exposed by the context.

That makes the code easier to navigate because the developer can usually answer:

- Which model am I working with?
- Which context owns it?
- Which data set is the access surface?

## Save and write patterns

Across the documentation and examples, you may see slightly different persistence verbs depending on the example focus:

- `WriteToDatabase(...)`
- model-level save patterns
- data set batch-add flows followed by a save step

Those are all part of the same broader idea: CoreRelm keeps persistence visible in code instead of hiding it behind large amounts of implicit state tracking.

## CRUD and transactions

For a single isolated write, a lightweight flow may be enough.

For multiple related writes, a better default pattern is:

```csharp
using var context = new RelmContextOptionsBuilder("name=ExampleConnectionString")
    .Build<ExampleContext>();

context.BeginTransaction();

try
{
    // create, update, or delete multiple related models here

    context.CommitTransaction();
}
catch
{
    context.RollbackTransaction();
    throw;
}
```

CoreRelm is most readable when transaction boundaries are explicit.

## Design guidance

A good default CRUD mindset in CoreRelm is:

- begin from the context
- use the relevant data set
- keep model state changes readable
- make persistence steps explicit
- introduce explicit transactions when operations become multi-step

## Where to go next

After this page, the best next steps are:

1. [Querying and Result Shapes](querying-and-result-shapes.md)
2. [Relationships and Foreign Keys](relationships-and-foreign-keys.md)

Those pages explain how CRUD grows into richer querying and relationship-oriented workflows.
