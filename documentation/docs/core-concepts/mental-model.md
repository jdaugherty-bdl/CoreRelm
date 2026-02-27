# CoreRelm Mental Model

This page explains the main runtime shape of CoreRelm and how its core pieces fit together.

CoreRelm is designed to make relational work more structured and readable without hiding the database behind a large abstraction layer. Your models, context, datasets, options, helpers, and migrations all work together, but the database is still visible in the way you use the library.

## The basic idea

At a high level, a typical CoreRelm workflow looks like this:

1. Define a model and apply mapping attributes
2. Define a context
3. Configure `RelmContextOptions`
4. Build or create a context
5. Work with models through data sets
6. Use explicit transactions when an operation spans multiple writes

That flow is simple on purpose. CoreRelm prefers explicitness over magic and favors a mental model that is easy to reason about.

## The main building blocks

Most of the library revolves around a few core pieces:

- `RelmContext`
- `RelmDataSet<T>`
- `RelmModel`
- `RelmContextOptions`
- `RelmContextOptionsBuilder`

### `RelmContext`

`RelmContext` is the main runtime entry point. It is where connection behavior, transaction behavior, dataset access, and lower-level execution helpers come together. If you think of CoreRelm as a system, the context is the center of it.

In practice, the context is where you:

- work with configured database access
- begin, commit, and roll back transactions
- expose your model-oriented datasets
- use lower-level helper operations when needed

### `RelmDataSet<T>`

A `RelmDataSet<T>` is the model-oriented access surface for a specific entity type. It is where many common operations are expressed, including querying, loading, creating, and referencing related data.

A data set is not just a collection. It is the place where CoreRelm brings together:

- model metadata
- query patterns
- load and save workflows
- relationship access patterns

### `RelmModel`

A `RelmModel` represents a mapped entity. Attributes applied to the model and its members describe how the entity relates to the database.

In CoreRelm, models are not just POCOs with no meaning. They carry mapping intent close to the code that uses them.

### `RelmContextOptions` and `RelmContextOptionsBuilder`

Context options define how a CoreRelm context behaves. The options builder is the primary way to configure connection details, auto-open behavior, dataset initialization, table verification, and related settings.

This is one of the key places where CoreRelm's design style shows up: configuration stays readable and close to usage.

## How these pieces fit together

A simple way to think about the system is:

- the **model** describes the mapped entity
- the **dataset** is the model-specific access surface
- the **context** holds the configured runtime environment
- the **options** control how that environment behaves

That gives you a layered workflow that still feels direct:

- define how the entity maps
- define where the entity is accessed
- configure how the runtime behaves
- perform database operations explicitly

## Metadata stays close to the model

One of CoreRelm's design preferences is that metadata should live near the model when practical. That makes it easier to understand how an entity maps to the database by reading the model directly, rather than chasing separate configuration systems.

This is a major reason the library uses attributes heavily.

## Explicit transaction boundaries

CoreRelm prefers explicit transaction handling. That preference is visible throughout the guide and the QuickStart examples. When an operation spans multiple writes, a clear begin/commit or begin/rollback pattern is usually the most readable approach.

That philosophy matters because it affects how the library should be read:

- CoreRelm is not trying to make database boundaries disappear
- CoreRelm is trying to make them more understandable

## Eager versus lazy context behavior

A useful high-level distinction in CoreRelm is the difference between an eager context and a quick context.

### Eager context

An eager context initializes datasets up front and verifies tables before later operations need them. This favors readiness and can reduce later metadata work.

### Lazy context

A lazy context disables automatic dataset initialization and automatic table verification. This reduces up-front work and allows metadata to load as needed.

This distinction is important because it changes the runtime feel of the library:

- eager favors readiness
- lazy favors lighter startup work
Neither is universally better. The right choice depends on the application shape.

## The role of the QuickStart project

The QuickStart project is documentation-first. Its job is to show what a call may look like, not to serve as a polished end-to-end sample application. That matters when reading examples across the docs: many examples are there to explain usage shape, not to be copied as-is into production code.

Use `examples/CoreRelm.QuickStart` as a usage map for how the API looks in practice.

## Where to go next

After this page, the best next steps are:

1. Review [Models and Attributes](models-and-attributes.md)
2. Review [Contexts and DataSets](contexts-and-datasets.md)
3. Review [Options and Configuration](options-and-configuration.md)

Together, those pages explain the main conceptual pieces that sit underneath the rest of the library.
