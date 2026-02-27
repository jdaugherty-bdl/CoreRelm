# Dependency Injection and Integration

This page explains where dependency injection and broader application integration fit into CoreRelm.

CoreRelm is designed around explicit context construction, but that does not mean it cannot fit into larger application architectures. Integration guidance helps teams use CoreRelm cleanly inside services, web applications, background processors, and other hosted environments.

## Why integration guidance matters

Even a clear library becomes harder to use if the surrounding application shape is not well understood.

Integration documentation helps answer:

- where should contexts be created?
- how should configuration be supplied?
- where should transaction boundaries live?
- how should CoreRelm fit into a service-based application?

## Explicitness still matters

A useful principle for integration is:

> dependency injection should help manage structure, not hide important database behavior

That means DI should support CoreRelm's design style rather than pushing it toward invisible behavior.

## Service boundaries and context usage

A good default approach is to keep context usage close to the application service or operation that owns the unit of work.

That usually means:

- create or resolve the context at a clear application boundary
- keep transaction handling near the business operation it protects
- avoid spreading one implicit unit of work across too many unrelated layers

## Configuration and environment wiring

Integration also includes deciding how runtime configuration enters the system.

That may include:

- named connection strings
- environment-specific configuration
- startup wiring
- app-level service registration

Those choices should preserve readability and make the runtime behavior easier, not harder, to understand.

## Dependency injection support

The broader CoreRelm public surface includes service-collection integration support, which suggests DI is a supported advanced topic rather than only a user invention.

That means this page should eventually document:

- the available DI extension surface
- recommended registration patterns
- where to resolve contexts
- how to avoid lifetime confusion

## Integration patterns by app type

Over time, this page should distinguish between common application shapes such as:

- console or utility applications
- web applications
- background processing services
- integration or migration runners

Each of those may prefer slightly different context and transaction patterns.

## Design guidance

A good default approach is:

- let DI manage construction and configuration when it helps
- keep database behavior explicit
- keep unit-of-work boundaries easy to see
- avoid hiding CoreRelm behind layers that make troubleshooting harder

## Where to go next

After this page, the next best step is to move into the Troubleshooting, Reference, and Project Guidance section.

That is where error handling, API reference guidance, QuickStart mapping, architecture notes, and contribution guidance come together.
