# Contributing

This page explains how to contribute to CoreRelm.

As the project grows, it is helpful to make contribution expectations clear for code, tests, docs, examples, and broader project maintenance.

## General contribution mindset

A good contribution should aim to improve one or more of the following:

- clarity
- correctness
- maintainability
- documentation quality
- test coverage
- user experience for library consumers

## Core expectations

A good default expectation is:

- keep changes focused
- prefer clear and explicit code
- add or update tests when behavior changes
- update documentation when public behavior changes
- keep examples aligned with the public surface

## Documentation contributions

If a public feature changes, documentation should usually be updated in more than one place.

That may include:

- the guide pages
- the QuickStart examples
- the README
- generated API docs or API-doc inputs
- tests that serve as behavioral evidence

This matters because different documentation layers serve different purposes.

## Example contributions

QuickStart examples should stay aligned with the real public surface.

Because the QuickStart project is documentation-first, example contributions should prioritize:

- clear usage shape
- focused feature demonstration
- consistency with the guide
- consistency with actual public types and members

## Code contributions

Code contributions should keep CoreRelm's design style in mind:

- explicitness over magic
- readable relational behavior
- visible transaction and configuration boundaries
- practical, understandable APIs

## Test contributions

Tests are especially important for:

- migrations behavior
- parsing and planning logic
- relationship behavior
- configuration and options behavior
- public feature regressions

A good contribution often includes the tests needed to make the change trustworthy.

## Documentation-first improvements

Not every valuable contribution needs to be a code change.

Useful contributions can also include:

- clarifying docs wording
- mapping examples to guide pages
- improving troubleshooting coverage
- refining architecture notes
- improving contribution guidance itself

## Design guidance

A good default approach is:

- make the smallest clear change that solves the problem
- support public changes with tests and docs
- keep the project easier to understand after the change than before it

## Where to go next

After this page, the next best step is [Roadmap](roadmap.md).
