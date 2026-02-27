# Querying and Result Shapes

This page explains how CoreRelm expresses query workflows and how different result shapes fit into the overall data-access model.

CoreRelm uses a model- and dataset-centered query style. The goal is to keep queries readable, explicit, and close to the model being worked with.

## Querying starts from a data set

A common query flow begins with the context and then moves into the model-specific data set.

### Example shape

```csharp
var exampleActiveModels = context
    .ExampleModels
    .Where(model => model.Active == true && model.Id > 5)
    .OrderByDescending(model => model.CreateDate)
    .Load();
```

This illustrates the general pattern:

- start from the configured context
- use the appropriate data set
- express the query conditions
- materialize the result

## Common query steps

The QuickStart and broader documentation surface point to several common query-building ideas:

- filtering
- ordering
- grouping
- limiting
- offsets
- distinct selection patterns

### Filtering

Filtering is usually expressed through `Where(...)`.

```csharp
var filtered = context.ExampleModels
    .Where(model => model.Active == true && model.Id > 5)
    .Load();
```

### Ordering

Ordering is usually expressed through `OrderBy(...)` or `OrderByDescending(...)`.

```csharp
var ordered = context.ExampleModels
    .OrderByDescending(model => model.CreateDate)
    .Load();
```

### Grouping, limits, and offsets

The QuickStart feature inventory indicates support for grouping, limits, and offsets in the broader data set workflow.

The purpose of these operations is to keep query intent close to the data-access call chain rather than pushing every query shape into a completely separate layer.

## Result shapes

One of the important themes in the QuickStart project is that CoreRelm demonstrates multiple data result shapes, not only model-list loading.

The current docs and example inventory call out support for:

- data row access patterns
- data table access patterns
- data object access patterns
- data list access patterns

That is useful because not every query needs to materialize full model instances.

## Model-oriented result shape

The most familiar result shape is loading one or more mapped models.

```csharp
var models = context.ExampleModels
    .Where(model => model.Active == true)
    .Load();
```

This is usually the right choice when:

- you want mapped entity instances
- you want to keep working in a model-oriented workflow
- you want to continue into relationships or model-based logic

## Object-oriented result shape

Sometimes you want a lightweight object-oriented result that is not centered on a mapped model instance.

Use this shape when:

- you need a projected or non-model object
- you want a lighter result
- the query does not naturally map to a full entity workflow

## List-oriented result shape

A list result shape is useful when the main goal is to gather repeated values or repeated projected items.

This can be useful for:

- simple reporting flows
- quick data access helpers
- workflows where full model mapping would be unnecessary

## Row-oriented result shape

A row-oriented result shape is useful when you want direct access to a single raw row structure without wrapping the result in a mapped model.

This is often a practical choice when:

- the result is ad hoc
- you need a database-shaped response
- you are closer to low-level database work

## Table-oriented result shape

A table-oriented result shape is useful for broader result sets where a tabular structure is more natural than a mapped-model workflow.

This can be a good fit for:

- reporting
- export-like workflows
- internal admin or diagnostics scenarios

## Querying philosophy

The main design idea is not that every workflow must become a model list.

Instead, CoreRelm tries to support several levels of access:

- model-oriented queries through data sets
- object/list/row/table-oriented result shapes when a different shape is a better fit
- lower-level database work helpers when you need more direct control

## Choosing a result shape

A good default decision process is:

- use mapped models when you are working with real entities
- use projected or lightweight shapes when the result is more task-specific
- use row/table-oriented access when the query is closer to raw relational work
- keep the result shape aligned to the job, not to habit

## Design guidance

CoreRelm is easiest to reason about when the query shape matches the usage shape:

- entity work should look like entity work
- reporting work should look like reporting work
- low-level data work should stay visibly low-level

That consistency helps keep the documentation and the code aligned.

## Where to go next

After this page, the best next steps are:

1. [Relationships and Foreign Keys](relationships-and-foreign-keys.md)
2. [DTOs and Projection Patterns](dtos-and-projection-patterns.md)

Those pages explain how query results connect to related data and alternative shapes.
