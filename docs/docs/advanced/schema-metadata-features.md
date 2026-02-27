# Schema Metadata Features

This page explains the broader schema-oriented metadata features exposed by CoreRelm beyond basic table and column mapping.

CoreRelm's public surface includes schema-related concepts that go beyond simple entity mapping. These features are important both for runtime understanding and for migrations/schema workflows.

## Why schema metadata matters

Schema metadata is where relational intent becomes more precise.

It helps describe things such as:

- identity and keys
- indexes
- uniqueness constraints
- foreign keys
- triggers
- functions
- procedures
- database-level features

This is one of the reasons CoreRelm can support both runtime ORM workflows and schema-oriented workflows in the same ecosystem.

## More than tables and columns

A new reader may first encounter CoreRelm through `RelmTable` and `RelmColumn`, but the schema story is broader than that.

The broader metadata surface helps the library express:

- how an entity is identified
- how it relates to other entities
- how constraints should behave
- what additional database objects participate in the schema

## Keys and identity

Keys and identity metadata are central because they affect:

- entity lookup
- update and delete targeting
- relationship behavior
- schema generation and migration planning

## Indexes and uniqueness

Indexes and uniqueness-related metadata matter for:

- performance
- data integrity
- query behavior
- schema planning

These should be documented not only as attributes or types, but also in terms of why a developer would choose them.

## Foreign keys

Foreign-key metadata connects this page back to the relationships section.

The schema perspective adds another important layer:

- the relationship is not only a navigation concern
- it is also a database constraint and schema concern

## Triggers, functions, and procedures

The broader project inventory shows dedicated attribute and API surface coverage for triggers, functions, procedures, and related concepts.

That means the documentation should treat them as real schema features, not as obscure edge cases.

These features are useful when:

- database behavior needs to include server-side logic
- the schema story includes more than plain tables
- migrations and schema tooling must understand more than simple entity mapping

## Database-level metadata

The broader schema story can also include database-level attributes or provider-specific schema features.

Because CoreRelm is currently focused on MySQL 8+, the docs should explain these features with that provider context in mind.

## Design guidance

A good default approach is:

- document schema metadata as part of relational intent
- explain both runtime impact and schema impact
- keep examples clear about whether they are usage examples or full schema examples
- connect schema metadata docs to the migrations section whenever relevant

## Where to go next

After this page, the next best step is [Dependency Injection and Integration](dependency-injection-and-integration.md).
