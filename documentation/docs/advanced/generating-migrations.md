# Generating Migrations

This page explains the role of migration generation in CoreRelm and how to think about it safely.

Generating a migration is the point where the desired schema model becomes a concrete change plan or artifact that can be reviewed and eventually applied.

## The goal of generation

Migration generation should answer a simple question:

> Given the desired schema and the current database state, what change artifact should be produced?

That artifact should be understandable enough to review and reliable enough to use as part of a controlled schema workflow.

## High-level generation flow

A good mental model for generation is:

1. resolve the desired schema input
2. compare it to the current state
3. build a migration plan
4. render the resulting artifact
5. review the output before applying it

This is one reason the documentation should separate generation from application. They are related, but they are not the same responsibility.

## Example usage shape

```csharp
var migrationTool = new RelmMigrationTooling();
var result = migrationTool.GenerateMigration(/* model set / options */);

if (!result.Success)
{
    // inspect errors
}
```

This is intentionally documentation-style and focuses on call shape rather than complete runnable setup.

## Naming and organization

Generated migrations are easier to maintain over time when naming is consistent.

Good documentation guidance should include:

- use clear, descriptive names
- keep ordering understandable
- avoid ambiguous migration purpose
- organize files in a way that makes history readable

## Review before apply

Generation should usually be followed by review.

That review may include:

- reading the generated SQL or migration script
- checking intended object changes
- confirming table, key, and constraint behavior
- verifying that the migration matches the desired change, not just that it exists

## Generation inputs

The broader CoreRelm migrations surface suggests that generation depends on more than one thing, including:

- model sets
- current schema state
- migration planning logic
- rendering logic

That is why generation should be documented as a workflow step, not just as a single method call.

## Generation and tests

Migration generation benefits from tests because schema workflows are easy to break in subtle ways.

The repository materials indicate test coverage around migrations planning and parsing, which is a strong signal that generation should be treated carefully.

## Design guidance

A good default approach is:

- generate with clear intent
- review the artifact before applying it
- keep naming consistent
- treat migration outputs as part of the versioned schema story

## Where to go next

After this page, the best next steps are:

1. [Applying Migrations](applying-migrations.md)
2. [Drift Detection and Validation](drift-detection-and-validation.md)
