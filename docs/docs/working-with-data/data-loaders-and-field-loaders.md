# Data Loaders and Field Loaders

This page explains how data loaders and field loaders fit into the CoreRelm model.

CoreRelm includes loader-oriented feature areas in the QuickStart and codebase. These patterns are useful when the default data-loading flow is not enough and you want a more specialized loading strategy.

## Why loaders exist

A standard dataset query is often enough for common entity work.

Loaders become useful when:

- a field needs custom load behavior
- related or derived values need special handling
- you want a reusable loading pattern
- the loading logic should be made explicit rather than hidden in scattered ad hoc code

## Loader-oriented feature areas

The QuickStart inventory includes:

- data loader examples
- field loader files
- dedicated loading examples tied to data access workflows

That indicates loaders are meant to be a supported concept, not a one-off internal trick.

## Data loaders

A data loader is best understood as a reusable strategy for loading or shaping data beyond the simplest default path.

Use a data loader when:

- the same loading behavior should be reused
- the logic is important enough to name and document
- you want a clearer boundary around data-shaping logic

## Field loaders

A field loader is a narrower version of the same idea. It focuses on a particular field or field-like behavior.

A field loader can be useful when:

- one field needs special lookup logic
- a field is derived from a more complex loading path
- a property should be filled by a clearly named reusable mechanism

## Loader mindset

The important design idea is this:

- keep common loading logic reusable
- keep special loading logic discoverable
- avoid burying repeated data-shaping behavior in many unrelated call sites

That makes the data-access layer easier to understand over time.

## Loaders versus DTOs versus relationships

These three ideas are related, but they solve different problems:

- **relationships** describe how entities connect
- **DTOs** shape results for a task
- **loaders** define reusable loading behavior when the default path is not enough

Keeping those responsibilities distinct will make the documentation clearer.

## Example loader-style mental model

A loader-oriented workflow often looks like this:

1. define the loader concept
2. attach or reference it where appropriate
3. use it as part of a query or load operation
4. keep the loading behavior reusable and understandable

## Design guidance

A good default approach is:

- do not reach for loaders first when a simple query is enough
- introduce loaders when the behavior is repeated or important
- keep loaders focused and clearly named
- document what the loader is responsible for and what it is not responsible for

## Where to go next

After this page, the best next steps are:

1. [Bulk Operations](bulk-operations.md)
2. [Database Work Helpers](database-work-helpers.md)

Those pages cover performance-oriented and lower-level data workflows.
