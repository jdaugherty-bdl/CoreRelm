---
_layout: landing
---
# CoreRelm

**CoreRelm** is a lightweight, attribute-based ORM / data access layer for C# and .NET Framework developers who want a small, predictable alternative to heavyweight ORMs.

It sits close to ADO.NET, but gives you:

- Strongly-typed table/column access via attributes and expressions
- A simple context model (`RelmContext` / `RelmQuickContext`)
- Helper methods for the common database shapes you actually use
- A clean, explicit pattern for transactions and error handling

It’s especially aimed at **.NET Core** apps and services that need something quick and focused, but it also works from modern .NET projects.

> ⚠️ **Status:** CoreRelm is under active development. APIs and examples may evolve as features are added and real-world scenarios are covered. Feedback, issues, and PRs are welcome.

> ❗ **.NET Core 9+ ONLY** ❗ For the .NET Framework 4.8 version of this library, please go to https://github.com/jdaugherty-bdl/SimpleRelm 

Documentation: https://jdaugherty-bdl.github.io/CoreRelm/index.html

---

## Features

- **Lightweight ORM**  
  Thin abstraction on top of ADO.NET to keep things transparent and predictable.

- **POCO-friendly mapping**  
  Map query results directly into your own C# classes without invasive attributes or base classes.

- **Explicit transactions**  
  You stay in control of when transactions begin, commit, and roll back — no hidden magic.

- **`using`-friendly API**  
  Designed to be used in a `using` block so connections/transactions are cleaned up correctly.

- **Framework & Core friendly**  
  Built with traditional .NET Framework apps in mind, but usable from modern .NET (Core) projects as well.

- **Actively evolving**  
  New features and refinements are being added as the library is used in real projects.