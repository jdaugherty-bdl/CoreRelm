# Bulk Operations

This page explains how bulk operations fit into CoreRelm and when they are a better fit than ordinary model-by-model write flows.

CoreRelm includes dedicated bulk table writing examples in the QuickStart project. That signals a deliberate distinction between standard entity-oriented persistence and higher-throughput write scenarios.

## Why bulk operations exist

A standard write flow is often the clearest option when:

- the number of records is small
- readability matters more than throughput
- entity-level logic is important

Bulk operations become useful when:

- many rows need to be written
- throughput matters
- the overhead of one-model-at-a-time persistence would be too high
- the workflow is more data-movement-oriented than entity-lifecycle-oriented

## Bulk table writing

The QuickStart inventory includes:

- `BulkTableWriterExamples`
- `WriteToDatabaseExamples`

That suggests the docs should treat bulk writing as its own feature area, not just a footnote under CRUD.

## Bulk operations versus standard writes

A practical way to think about the difference is:

### Standard writes

Use standard write flows when:

- the operation is entity-centric
- per-model logic matters
- clarity is more important than maximum throughput

### Bulk writes

Use bulk flows when:

- the operation is row-volume-centric
- the goal is efficient ingestion or movement
- you are working with larger batches

## Bulk operation tradeoffs

Bulk operations usually come with tradeoffs that should be called out clearly in the docs:

- they may be less model-centric
- they may bypass some of the feel of ordinary entity workflows
- they are often optimized for throughput rather than per-entity expressiveness

That does not make them worse. It just makes them better suited to a different kind of job.

## Bulk operations and transactions

Bulk workflows should still be documented with explicit reliability boundaries in mind.

A good default approach is:

- decide whether the whole batch should succeed or fail together
- use explicit transaction boundaries when appropriate
- keep rollback behavior understandable

## Design guidance

A good default approach is:

- start with standard write flows when the scale is modest
- move to bulk writing when volume makes it worth it
- document the operational goal clearly
- treat bulk workflows as performance-oriented tools, not as the default for every write

## Where to go next

After this page, the best next step is [Database Work Helpers](database-work-helpers.md).

That page covers the lower-level helper side of the data-access story.
