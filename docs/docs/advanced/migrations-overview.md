# Migrations Overview

This page explains what migrations mean in CoreRelm and why migrations should be treated as a first-class part of the public feature set.

CoreRelm includes migrations-related APIs and tooling as part of its broader ORM story. Migrations are not just an implementation detail; they are part of how the library approaches schema evolution and relational consistency.

## Why migrations exist

A runtime ORM story is only part of the picture.

Applications also need a way to think about:

- schema changes over time
- desired model shape versus current database shape
- reviewable change history
- safe rollout of structural changes

CoreRelm's migrations tooling exists to support those needs.

## Migrations as part of the public surface

The current project materials make it clear that migrations are a major feature area.

That includes:

- public migrations-related documentation and examples
- migrations tests
- model set parsing and resolver behavior
- migration tooling and validation workflows
- schema-oriented helper and rendering layers

This is why migrations deserve their own section in the guide rather than just API reference pages.

## High-level migrations mental model

A useful way to think about migrations in CoreRelm is:

1. define or resolve the desired schema shape
2. compare that desired shape to the current database state
3. generate or plan the changes needed
4. review the resulting migration artifacts
5. apply the migration safely

That is the broad workflow the rest of this section builds on.

## Model sets and desired schema

One of the central ideas in the CoreRelm migrations surface is the model set.

A model set represents the schema-relevant view of the models that should define the desired database shape.

That matters because migrations need a stable source of truth for what the database is expected to look like.

## Migrations are more than script generation

A good migrations system is not only about producing SQL.

It is also about:

- naming and organizing changes
- validating assumptions
- detecting drift
- making rollout safer
- keeping schema history understandable over time

The CoreRelm migrations surface should be documented with that broader perspective in mind.

## Reviewability matters

CoreRelm's documentation should treat generated migrations as reviewable artifacts.

A good default stance is:

- prefer explicit, readable migration outputs
- review generated changes before applying them
- use tests and staging environments for confidence
- treat migration files and generated SQL as part of the deployment story

## Runtime examples versus migration-ready definitions

As elsewhere in the docs, it is important to distinguish between:

- examples that show runtime usage shape
- examples that fully define schema intent

Not every runtime model example is necessarily enough to produce a complete migration on its own.

## Design guidance

A good default approach is:

- treat migrations as deliberate, reviewable schema changes
- keep naming and organization consistent
- understand the difference between desired schema and current database state
- use validation and status checks before rollout where possible

## Where to go next

After this page, the best next steps are:

1. [Generating Migrations](generating-migrations.md)
2. [Applying Migrations](applying-migrations.md)
3. [Drift Detection and Validation](drift-detection-and-validation.md)
