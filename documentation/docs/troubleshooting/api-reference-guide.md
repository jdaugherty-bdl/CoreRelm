# API Reference Guide

This page explains how to use the generated API reference effectively.

The API reference is valuable, but it works best when the reader knows what kind of question it is meant to answer.

## What the API reference is for

The generated API docs are best for questions such as:

- what type or member exists?
- what namespace is it in?
- what is the exact public surface?
- what related types exist nearby?
- what overloads or members should I inspect next?

The API reference is not a full replacement for narrative docs. It is the detailed reference layer.

## When to use the guide versus the API docs

A useful rule of thumb is:

- use the guide when you want explanation, workflow, and recommended usage
- use the API reference when you want exact type/member detail

Both are important, but they serve different purposes.

## Major API areas to know

A useful way to approach the API reference is by major feature area.

### Core runtime
Look here for types such as:

- `RelmContext`
- `RelmDataSet<T>`
- `RelmModel`
- `RelmModelClean`

### Options and configuration
Look here for types such as:

- `RelmContextOptions`
- `RelmContextOptionsBuilder`

### Helpers and extensions
Look here for utility surfaces such as:

- `RelmHelper`
- extension classes
- service-collection integration surfaces

### Schema and attributes
Look here for mapping and schema-related types such as:

- `RelmTable`
- `RelmColumn`
- `RelmKey`
- `RelmForeignKey`
- other schema-oriented attribute types

### Migrations
Look here for migration-related types such as:

- migration tooling
- model-set parsing and resolution
- planning and rendering types
- apply and validation result types

## How to read the API docs efficiently

A good default approach is:

1. start from the guide page for the concept
2. jump to the API docs for exact type/member details
3. move sideways to related types in the same namespace or feature area
4. return to the guide if you need workflow context

## Use the API docs to confirm, not to infer everything

The API docs are strongest when used to confirm details, such as:

- method names
- type relationships
- overloads
- namespaces
- publicly exposed feature areas

They are weaker as a standalone substitute for conceptual documentation.

## Design guidance

A good default approach is:

- start with the guide for concepts
- use the API docs for precision
- treat the API reference as a map of the public surface, not as the only learning path

## Where to go next

After this page, the best next step is [QuickStart Examples Guide](quickstart-examples-guide.md).

That page explains how to use the QuickStart project as a companion to both the guide and the API docs.
