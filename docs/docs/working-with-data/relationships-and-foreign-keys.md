# Relationships and Foreign Keys

This page explains how CoreRelm models describe relationships and how related data is loaded through foreign-key workflows.

CoreRelm keeps relationship intent close to the model by using attributes and readable navigation properties.

## Relationship mapping at the model level

A common relationship shape in CoreRelm starts with a model property and a `RelmForeignKey` attribute.

### Example shape

```csharp
[RelmTable("example_models")]
public class ExampleModel : RelmModel
{
    [RelmColumn]
    public string? GroupInternalId { get; set; }

    [RelmForeignKey(foreignKey: nameof(ExampleGroup.InternalId), localKey: nameof(GroupInternalId))]
    public virtual ExampleGroup Group { get; set; }
}

[RelmTable("example_groups")]
public class ExampleGroup : RelmModel
{
    [RelmColumn]
    public string? Name { get; set; }

    [RelmForeignKey(foreignKey: nameof(ExampleModel.GroupInternalId), localKey: nameof(InternalId))]
    public virtual ICollection<ExampleModel> Models { get; set; }
}
```

This demonstrates a few important ideas:

- the local key is visible on the model
- the foreign key target is visible on the model
- relationship direction is easy to read
- navigation intent stays close to the entity definition

## Why foreign-key metadata matters

Foreign-key metadata helps CoreRelm understand how one model relates to another. That matters for:

- readable navigation properties
- relationship loading
- model-oriented queries
- schema and migrations workflows

## Single-reference navigation

A single-reference navigation points from one entity to a related parent or peer.

### Example

```csharp
[RelmForeignKey(foreignKey: nameof(ExampleGroup.InternalId), localKey: nameof(GroupInternalId))]
public virtual ExampleGroup Group { get; set; }
```

This is a good fit when one model belongs to or references another model.

## Collection navigation

A collection navigation points from one entity to a related set of other entities.

### Example

```csharp
[RelmForeignKey(foreignKey: nameof(ExampleModel.GroupInternalId), localKey: nameof(InternalId))]
public virtual ICollection<ExampleModel> Models { get; set; }
```

This is a good fit when one model owns or groups many related models.

## Loading related data

The QuickStart inventory shows dedicated foreign-key loading examples and data set reference behavior. That indicates CoreRelm expects related-data loading to be a first-class part of the workflow.

At a high level, there are two important concepts here:

- mapping the relationship clearly
- loading or referencing the related data when needed

## Relationship loading patterns

A relationship workflow usually looks like one of these:

### Load the main model, then load references

```csharp
var models = context.ExampleModels
    .Where(model => model.Active == true)
    .Load();

// relationship loading or reference-loading step here
```

### Query through a data set and use reference-oriented helpers

CoreRelm also supports relationship-oriented access patterns through data sets and reference-style workflows.

This keeps relationship logic attached to the model and dataset surfaces rather than forcing every relationship query into raw SQL.

## Non-default and advanced foreign-key shapes

The broader codebase inventory indicates foreign-key navigation and compound-key testing in the repository.

That suggests the relationship system is meant to support more than only the simplest one-column default case.

This is worth documenting clearly over time:

- default key relationships
- non-default local and foreign keys
- compound-key scenarios
- bidirectional relationship navigation patterns

## Runtime versus schema examples

As with other CoreRelm docs pages, it is important to distinguish between:

- examples that show usage shape
- examples that fully define migratable schema intent

A relationship example may be excellent for teaching runtime navigation while still not being a full migrations-ready schema definition.

## Design guidance

A good default approach to relationships in CoreRelm is:

- keep key fields visible on the model
- use `RelmForeignKey` to make the relationship readable
- treat navigation properties as part of the relational story
- keep relationship loading explicit enough that readers can follow it

## Where to go next

After this page, the best next steps are:

1. [DTOs and Projection Patterns](dtos-and-projection-patterns.md)
2. [Data Loaders and Field Loaders](data-loaders-and-field-loaders.md)

Those pages explain alternative ways to shape and load related data.
