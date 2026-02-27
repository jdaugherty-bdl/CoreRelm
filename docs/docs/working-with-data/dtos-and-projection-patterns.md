# DTOs and Projection Patterns

This page explains how DTO-style workflows fit into CoreRelm and when a DTO is a better fit than a mapped model.

CoreRelm supports model-oriented work, but not every query or response should become a full entity model. DTOs are useful when the shape you need is task-specific rather than entity-specific.

## Why DTOs exist

A mapped model is usually the right fit when:

- you are working with a real entity
- you want entity-oriented persistence
- you want the model to carry relational metadata

A DTO is often the better fit when:

- the shape is for transfer or presentation
- the result combines values for a specific task
- you want a lighter object than a full mapped entity
- persistence is not the main concern

## DTOs in the QuickStart surface

The QuickStart project includes dedicated DTO examples. That is a strong signal that DTO usage is a real public feature area rather than an afterthought.

That also fits the broader documentation philosophy: the library should support several useful data shapes instead of forcing everything into one pattern.

## DTOs versus models

A practical way to think about the difference is:

### Use a model when:

- the object represents a database entity
- the object needs relational metadata
- the object participates directly in model-oriented workflows

### Use a DTO when:

- the shape is tailored to a specific query or response
- the object is primarily for transfer or display
- you do not want to carry full entity semantics into the result

## Projection patterns

Projection is the broader idea of shaping results into the object you actually need.

A projection-oriented workflow is useful when:

- a full model would include unnecessary data
- the consuming code only needs a smaller shape
- the query result crosses multiple concepts but is not itself a true entity

## Example DTO shape

```csharp
public class ExampleSummaryDto
{
    public string? Name { get; set; }
    public string? GroupName { get; set; }
    public int Value { get; set; }
}
```

This kind of shape is useful for:

- summaries
- reporting
- API responses
- UI-facing data objects

## DTO workflow mindset

When you document or design DTO flows in CoreRelm, keep the following mental model:

- models are for mapped relational entities
- DTOs are for shaped results
- DTOs should make the result easier to consume
- DTOs should not be forced to act like entities if they are not entities

## DTOs and query result shapes

DTO usage connects naturally to the broader result-shape story in CoreRelm.

When a query does not need a full model result, a DTO or projected object may be a better choice than:

- loading a full entity
- manually unpacking row/table results later
- carrying unnecessary relational fields through the application

## Design guidance

A good default approach is:

- start with a model when the workflow is truly entity-based
- switch to a DTO when the result is clearly task-specific
- keep DTOs small and purposeful
- name DTOs by what they are for, not just by where they came from

## Where to go next

After this page, the best next steps are:

1. [Data Loaders and Field Loaders](data-loaders-and-field-loaders.md)
2. [Bulk Operations](bulk-operations.md)

Those pages explain additional ways CoreRelm supports specialized data-access workflows.
