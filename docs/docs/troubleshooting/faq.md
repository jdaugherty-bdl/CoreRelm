# FAQ

This page collects common questions and recurring points of confusion about CoreRelm.

It should grow over time as real usage reveals which topics users most often stumble on.

## General questions

### Is CoreRelm trying to hide the database?

No. CoreRelm is designed to provide structure and readability while still keeping relational behavior understandable and visible.

### Are the QuickStart examples supposed to compile and run as-is?

Not necessarily. The QuickStart project is documentation-first. Its purpose is to show what usage calls may look like and to demonstrate feature shape.

### What database provider does CoreRelm currently target?

CoreRelm is currently focused on MySQL 8+.

## Models and mapping

### What is the difference between `RelmModel` and `RelmModelClean`?

`RelmModel` includes standard metadata fields by default, while `RelmModelClean` gives you a cleaner base when you want tighter control over the model shape.

### Why does my model example work conceptually but not seem migration-ready?

Because a runtime usage example and a full schema-definition example are not always the same thing. Some model examples are intended to teach call shape and mapping intent, not to be a complete migrations-ready schema.

## Contexts and datasets

### What is the difference between an eager context and a quick context?

An eager context initializes datasets and verifies tables up front. A quick context disables that automatic work and loads metadata as needed.

### Why do I sometimes need to retrieve a data set manually?

In quick-context-style workflows, datasets may not be initialized up front, so manual retrieval becomes part of the runtime pattern.

## Transactions

### Should I use explicit transactions?

Yes, especially for multi-step write workflows. Explicit transaction boundaries are one of the clearest and safest defaults in CoreRelm.

### When would I use auto-open transaction behavior?

When it fits the application flow, but explicit transactions are usually easier to read and troubleshoot.

## Migrations

### Are migrations a first-class feature in CoreRelm?

Yes. Migrations are part of the public story and deserve both narrative docs and API reference coverage.

### Why separate generating from applying migrations?

Because they answer different questions. Generating creates the artifact; applying executes it against a database.

### What is schema drift?

Schema drift is the mismatch between the intended schema state and the actual database state.

## Troubleshooting questions to add over time

This page should eventually collect real-world answers for issues such as:

- connection string problems
- foreign-key confusion
- migrations validation failures
- unexpected loader behavior
- result-shape misunderstandings
- DI lifetime or integration confusion
