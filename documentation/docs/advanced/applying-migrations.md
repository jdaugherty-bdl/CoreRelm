# Applying Migrations

This page explains how to think about applying migrations in CoreRelm and why application should be treated as a deliberate deployment step.

Applying a migration is where schema intent becomes an actual database change. Because of that, apply workflows should be explicit, reviewable, and operationally careful.

## The goal of application

The purpose of applying a migration is to move the database from its current state toward the desired state in a controlled way.

That means application is not just a technical step. It is also an operational step.

## Example usage shape

```csharp
var migrationTool = new RelmMigrationTooling();
var applyResult = migrationTool.ApplyMigrations(/* connection / options */);
```

As with the rest of the QuickStart-style documentation, this example is intended to show the call shape rather than a full runnable workflow.

## Why apply should be separated from generate

Generation and application should usually be documented separately because they answer different questions:

- generation asks what change artifact should exist
- application asks when and how the change should be executed

Keeping those responsibilities separate improves clarity and reduces accidental rollout risk.

## Safe rollout mindset

A good default apply mindset is:

- review the migration before applying it
- understand which environment you are targeting
- prefer staged rollout where appropriate
- verify success after application
- have a rollback or recovery plan where possible

## Apply results and diagnostics

Application workflows should be documented with diagnostics in mind.

Developers will want to know:

- did the migration apply successfully?
- what failed if it did not?
- what object or step caused the failure?
- what state is the database in afterward?

That is one reason apply-result types and related diagnostics deserve documentation attention.

## Applying in different environments

Apply guidance should distinguish between environments such as:

- local development
- integration or staging
- production

A behavior that is fine during local iteration may not be appropriate for production rollout.

## Operational guidance

Good operational guidance for applying migrations includes:

- avoid applying unreviewed changes directly to production
- use staging verification when possible
- keep backups and recovery strategy in mind
- log migration execution details
- verify post-apply state

## Design guidance

A good default approach is:

- generate first
- review second
- apply deliberately
- verify afterward
- keep the migration history understandable

## Where to go next

After this page, the next best step is [Drift Detection and Validation](drift-detection-and-validation.md).

That page covers how CoreRelm helps compare intended schema state with actual database state.
