# Architecture Notes

This page explains the high-level architectural shape of CoreRelm.

It is not meant to be a deep internal design specification. Instead, it should help readers understand the main layers of the project and how the public pieces fit together.

## High-level architecture

A useful way to think about CoreRelm is as several connected layers:

- model and attribute metadata
- runtime context and dataset workflows
- options and configuration
- helper and extension surfaces
- schema and migrations tooling
- examples, tests, and generated API docs

Each layer supports a different kind of developer task.

## Public surface versus internal machinery

One of the useful distinctions in any library is the difference between:

- what users are expected to work with directly
- what exists mainly to support those public features behind the scenes

The CoreRelm guide should generally focus on the public mental model first, then expose deeper internals only when they help users reason about advanced behavior.

## Metadata-centered design

A major architectural theme in CoreRelm is that metadata stays close to the model through attributes.

That helps keep relational intent readable and discoverable without forcing everything into a separate configuration system.

## Runtime-centered design

Another important theme is that runtime behavior gathers around the context.

This makes the context the central place where:

- configuration is applied
- data sets are accessed
- transactions are controlled
- helper operations are performed

## Schema and migrations as a real subsystem

Migrations are not just an optional sidecar feature. They represent a real subsystem in the project architecture.

That matters because it means the project has two major stories:

- runtime ORM usage
- schema evolution and migration workflows

## Docs, examples, tests, and API reference as architecture support

The project also has a broader support architecture for learning and maintenance:

- guide pages for explanation
- QuickStart examples for usage shape
- tests for behavior verification
- generated API docs for precision

That is worth documenting because it tells contributors where each kind of documentation responsibility belongs.

## Extension points and boundaries

Over time, this page should become more specific about:

- intended extension points
- provider-specific assumptions
- what is considered stable public behavior
- what is more internal or subject to change

## Design guidance

A good default approach is:

- explain architecture in terms of responsibilities, not only file layout
- keep the public mental model front and center
- introduce deeper implementation detail only when it improves understanding

## Where to go next

After this page, the best next steps are:

1. [Contributing](contributing.md)
2. [Roadmap](roadmap.md)
