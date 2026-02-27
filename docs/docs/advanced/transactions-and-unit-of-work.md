# Transactions and Unit of Work

This page explains how transaction handling fits into CoreRelm and why explicit transaction boundaries are one of the library's core design preferences.

CoreRelm favors making database boundaries visible. When an operation spans multiple related writes, the transaction should usually be easy to see in the code.

## Transaction philosophy

CoreRelm is most readable when transaction handling is explicit.

A good default pattern is:

- begin a transaction when an operation spans multiple writes
- commit only after all related operations succeed
- roll back on failure
- keep error paths simple and predictable

This matches the broader design style of the library: explicitness over hidden behavior.

## Standard explicit transaction pattern

A common transaction workflow looks like this:

```csharp
using var context = new RelmContextOptionsBuilder("name=ExampleConnectionString")
    .Build<ExampleContext>();

context.BeginTransaction();

try
{
    // Perform multiple related operations here

    context.CommitTransaction();
}
catch
{
    context.RollbackTransaction();
    throw;
}
```

This is often the clearest and safest default for multi-step write work.

## Why explicit transactions are recommended

Explicit transactions make several things easier to reason about:

- where a unit of work starts
- where a unit of work ends
- what should succeed together
- what should roll back together
- where error handling belongs

That is especially useful in an ORM that is intentionally trying to stay close to relational reality.

## Auto-open transaction behavior

CoreRelm also supports configuration that can automatically open a transaction.

### Example shape

```csharp
using var context = new RelmContextOptionsBuilder("name=ExampleConnectionString")
    .SetAutoOpenTransaction(true)
    .Build<ExampleContext>();

try
{
    // database work here
}
catch
{
    context.RollbackTransaction();
    throw;
}
```

This can be useful in some flows, but the broader documentation still treats explicit transaction boundaries as the most readable default.

## Unit of work mindset

A useful way to think about transaction handling in CoreRelm is in terms of a unit of work.

A unit of work is the set of related changes that should succeed or fail together.

That means transaction design should be driven by business and data boundaries, not just by convenience.

## Contexts and transactions

The context is where transaction behavior usually lives.

That matters because the context is already the place where:

- configuration comes together
- dataset access is exposed
- connection behavior is managed
- lower-level helpers are available

Keeping transactions there makes the workflow easier to follow.

## Error handling and rollback

A good default rollback mindset is:

- roll back as soon as the multi-step operation is known to have failed
- keep the rollback path simple
- log application and database diagnostics where useful
- rethrow or wrap exceptions at the appropriate boundary

## Design guidance

A good default approach is:

- use explicit transactions for multi-step writes
- keep the transaction boundary close to the operations it protects
- prefer clarity over cleverness
- treat auto-open transaction behavior as an optional tool, not as the universal default

## Where to go next

After this page, the best next steps are:

1. [Migrations Overview](migrations-overview.md)
2. [Generating Migrations](generating-migrations.md)

Those pages explain how CoreRelm handles schema-oriented workflows in addition to runtime operations.
