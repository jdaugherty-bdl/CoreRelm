# Models and Attributes

This page explains how CoreRelm models are structured and how attributes describe the relationship between your code and the database.

CoreRelm uses attribute-driven model metadata as one of its central design choices. The goal is to keep mapping information near the model so that relational intent remains easy to read.

## What a model is in CoreRelm

A `RelmModel` represents a mapped entity. Attributes applied to the model and its members describe how that entity relates to the database.

In practice, this means a model usually does two things at once:

- it represents domain or application data
- it carries mapping metadata that tells CoreRelm how to treat that data

## `RelmModel` versus `RelmModelClean`

CoreRelm supports two broad model starting points.

### `RelmModel`

Use `RelmModel` when you want the standard metadata fields included automatically.

Your updated Getting Started page calls out that `RelmModel` includes standard metadata columns such as:

- `Id`
- `InternalId`
- `CreateDate`
- `LastUpdated`

This is the most convenient starting point when those default fields match the shape you want.

### `RelmModelClean`

Use `RelmModelClean` when you do **not** want those standard fields included automatically and would rather define everything yourself.

This is useful when:

- you need tighter control over the entity shape
- you are mapping to tables that do not follow the default metadata pattern
- you want to opt into only the fields you explicitly define

## Common model-level attributes

CoreRelm uses attributes to describe mapping and schema intent. Common examples include:

- `RelmTable`
- `RelmColumn`
- `RelmKey`
- `RelmForeignKey`

The uploaded codebase inventory also shows a broader attribute surface in the API docs, which indicates that attributes are an important part of the public model.

## `RelmTable`

`RelmTable` declares the table that a model maps to.

### Example

```csharp
[RelmTable("example_models")]
public class ExampleModel : RelmModel
{
}
```

This is the top-level mapping anchor for the entity.

## `RelmColumn`

`RelmColumn` marks a property as a mapped column.

### Example

```csharp
[RelmColumn]
public string? Name { get; set; }
```

In your updated Getting Started page, `RelmColumn` is used to mark mapped fields such as `Name`, `Value`, and `GroupInternalId`.

At a conceptual level, `RelmColumn` answers the question:

> Which properties should CoreRelm treat as mapped relational values?

## `RelmKey`

`RelmKey` identifies key members. Depending on your model strategy, this may be part of the standard metadata you get from `RelmModel`, or it may be something you define more explicitly when working from a cleaner base model.

Key information matters because it affects:

- identity
- lookup behavior
- relationships
- migrations and schema workflows

## `RelmForeignKey`

`RelmForeignKey` describes relationship links between models.

Your updated Getting Started example uses `RelmForeignKey` in both directions:

- from `ExampleModel` to `ExampleGroup`
- from `ExampleGroup` to the collection of `ExampleModel` instances

### Example

```csharp
[RelmForeignKey(foreignKey: nameof(ExampleGroup.InternalId), localKey: nameof(GroupInternalId))]
public virtual ExampleGroup Group { get; set; }
```

This style keeps relationship intent readable directly on the model.

## Example model shape

The following example reflects the updated shape you provided:

```csharp
[RelmTable("example_models")]
public class ExampleModel : RelmModel
{
    [RelmColumn]
    public string? Name { get; set; }

    [RelmColumn]
    public int Value { get; set; }

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

This model shape demonstrates:
- table mapping
- column mapping
- use of the default metadata base model
- foreign key navigation intent

## Migratable versus non-migratable model shapes

One important point from your updated Getting Started page is that a model may be valid as a usage example but still not be fully migratable as-is.

In your example, the note is that the model is **not migratable as-is** because column types and lengths are not specified.

That is a very useful distinction for the docs:

- some examples are about runtime usage shape
- some examples are about complete schema intent
- those are not always the same thing

This should be called out clearly whenever model examples are introduced.

## Model design guidance

A good default approach is:

- start with `RelmModel` when the standard metadata fields fit
- use attributes to keep mapping intent close to the model
- introduce `RelmModelClean` when you need a more customized shape
- treat relationship properties as part of the model's relational story, not just convenience fields

## Where to go next

After this page, the best next steps are:

1. Review [Contexts and DataSets](contexts-and-datasets.md)
2. Review [Options and Configuration](options-and-configuration.md)

Those pages explain how models are used at runtime once they have been defined.
