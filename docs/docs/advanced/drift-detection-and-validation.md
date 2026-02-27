# Drift Detection and Validation

This page explains how validation and drift detection fit into the CoreRelm migrations story.

Schema work is not only about creating migrations. It is also about making sure the database and the intended model remain aligned over time.

## What drift means

Schema drift is the gap between:

- the desired schema state
- the actual database state

Drift can happen for many reasons, including:

- manual database changes
- partially applied changes
- schema differences between environments
- model evolution without matching migration discipline

## Why validation matters

Validation helps answer questions such as:

- is the schema still aligned with the intended model?
- are required migration steps missing?
- does the current state match what the tooling expects?
- is it safe to proceed with the next schema step?

That makes validation one of the most practical safety features in a migrations system.

## Drift detection as an ongoing workflow

Drift detection should not be treated as a one-time step.

It is useful:

- before generating migrations
- before applying migrations
- after applying migrations
- when comparing environments
- when investigating unexpected schema behavior

## Diagnostic mindset

When documenting validation and drift detection, the guide should help readers answer:

- what is out of sync?
- how severe is the mismatch?
- is it a naming issue, type issue, key issue, or constraint issue?
- what should be done next?

That will make this section more useful than a simple feature checklist.

## Validation inputs

The broader CoreRelm migrations materials suggest validation depends on several components working together, including:

- model-set parsing or resolution
- desired schema construction
- current-schema inspection
- migration planning and comparison logic

That is why validation deserves its own explanation instead of being hidden inside other migration pages.

## Common validation situations

The most useful cases to document are:

- expected clean alignment
- missing migration or unapplied changes
- manual database drift
- object mismatch such as columns, keys, or constraints
- environment mismatch between staging and production

## Design guidance

A good default approach is:

- validate before applying important changes
- treat drift as a real signal, not as a nuisance
- investigate the source of mismatch instead of papering over it
- document the next action clearly whenever validation fails

## Where to go next

After this page, the best next steps are:

1. [Schema Metadata Features](schema-metadata-features.md)
2. [Dependency Injection and Integration](dependency-injection-and-integration.md)
