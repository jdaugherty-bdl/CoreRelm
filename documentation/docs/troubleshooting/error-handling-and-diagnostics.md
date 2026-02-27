# Error Handling and Diagnostics

This page explains how to think about error handling and diagnostics in CoreRelm.

CoreRelm is most effective when database behavior stays visible. That includes failure paths. A good error-handling strategy should make it easy to see what failed, where it failed, and what should happen next.

## Core error-handling mindset

A good default approach in CoreRelm is:

- make transaction boundaries explicit
- roll back multi-step operations when they fail
- keep error paths simple
- log useful application and database context
- avoid hiding failures behind overly clever abstractions

This fits the broader design preference of explicitness over magic.

## Error handling and transactions

The most important error-handling pattern in CoreRelm is usually the transaction boundary.

### Standard pattern

```csharp
using var context = new RelmContextOptionsBuilder("name=ExampleConnectionString")
    .Build<ExampleContext>();

context.BeginTransaction();

try
{
    // multi-step database work here

    context.CommitTransaction();
}
catch
{
    context.RollbackTransaction();
    throw;
}
```

This pattern is valuable because the success path and failure path are both easy to follow.

## Why rollback matters

Rollback matters when multiple related operations should succeed or fail together.

Without a clear rollback path, it becomes harder to answer:

- which operations were completed
- which operations failed
- whether the database is in a partial state
- what the recovery action should be

## Diagnostics and inspection

The broader CoreRelm docs direction already identifies diagnostics members such as:

- `HasError`
- `LastExecutionError`
- `LastExecutionException`

These kinds of diagnostics are useful because they let the developer inspect library- or database-related failure information without guessing blindly.

## A practical diagnostic workflow

A useful diagnostic workflow is:

1. identify whether the operation failed
2. inspect any CoreRelm-exposed diagnostic state
3. inspect the application exception path
4. confirm whether a rollback occurred or should occur
5. log enough information to reproduce or investigate later

## Migration-related diagnostics

Diagnostics matter for migrations too, not just runtime CRUD work.

Migration diagnostics should help answer questions such as:

- did generation fail?
- did validation detect drift?
- did application fail on a particular object or step?
- what environment was affected?

That is why troubleshooting and migrations should remain closely connected in the docs.

## Common failure categories

The most useful categories to cover over time are:

- configuration problems
- connection or provider issues
- dataset initialization confusion
- relationship or key mismatches
- migration validation or apply failures
- unexpected result-shape or loading behavior

## Design guidance

A good default approach is:

- make write boundaries explicit
- inspect diagnostics close to the failure
- do not swallow exceptions silently
- keep error handling boring, predictable, and readable

## Where to go next

After this page, the best next step is [FAQ](faq.md).

That page collects common problems and answers in one place.
