# QuickStart Examples Guide

This page explains how to use the QuickStart project effectively.

The QuickStart project is a documentation-first examples project. Its purpose is to show what usage calls may look like across the public CoreRelm feature surface.

## What the QuickStart project is for

Use `examples/CoreRelm.QuickStart` when you want to see:

- the general shape of an API call
- how a feature is typically expressed
- how related feature areas fit together
- where to look next in the guide or API reference

The QuickStart project is a usage map, not a polished end-to-end application.

## What the QuickStart project is not

The QuickStart project is not primarily intended to be:

- a production-ready sample app
- a guarantee that every example compiles and runs as-is
- the only source of conceptual documentation

That distinction is important, because it changes how the examples should be read.

## How to use QuickStart together with the guide

A good default approach is:

1. read the guide page for the concept
2. open the related QuickStart example file
3. inspect the API call shape
4. use the API reference for exact type/member details if needed

This three-layer workflow is often the fastest way to learn the library.

## Major example areas

The QuickStart inventory covers a broad set of feature areas, including:

- attributes
- connections
- contexts
- CRUD and data-access shapes
- data loaders
- DTOs
- foreign keys
- identity helpers
- bulk writing
- model reset and model property patterns

That breadth is one of the reasons the QuickStart project is worth documenting explicitly.

## Example interpretation guidance

When reading a QuickStart example, ask:

- what feature is this example trying to demonstrate?
- what call shape is it showing?
- is this a runtime example, a schema example, or both?
- should I go to the guide for explanation or the API docs for precision?

Those questions help keep the project useful instead of confusing.

## Suggested mapping approach

Over time, this page should grow into a feature-to-example map, such as:

- **Introduction / Getting Started**
  - Quick start patterns in `Program.cs`
- **Core Concepts**
  - context, options, and model examples
- **Working with Data**
  - CRUD, result shapes, relationships, DTOs, loaders, bulk writing
- **Advanced Topics**
  - migrations, helpers, integration, and advanced workflows

## Design guidance

A good default approach is:

- use QuickStart to understand API shape
- use the guide to understand concepts and recommendations
- use the API reference to confirm exact members and types

## Where to go next

After this page, the best next step is [Architecture Notes](architecture-notes.md).
