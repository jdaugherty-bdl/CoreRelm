# .github/copilot-instructions.md

## TL;DR (highest priority)
- Generate real, runnable code (no pseudocode, no “TODO: implement” placeholders).
- Keep changes minimal and scoped to the request.
- Follow the **Coding Style Rulebook** below (IDs `CS-###`). Style rules are **law**.
- Keep builds/tests passing: `dotnet build` and `dotnet test`.
- Do **not** hand-edit generated docs under `docs/api/`—update code/XML docs and rebuild docs instead.

<!--
HUMANS-ONLY: How to add a new style rule (intended to be out-of-band for Copilot)
1) Add a new rule under "Coding Style Rulebook (Append-only)".
2) Assign the next ID in sequence (CS-###). Never reuse IDs.
3) Write it in this format:
   - CS-###: <Short rule title>
     - Rule: <one sentence, imperative>
     - Rationale: <one sentence>
     - Example (optional): <short snippet>
4) If it conflicts with an existing rule, update the older rule to clarify precedence.
5) Keep rules objective and testable (avoid vague “clean code” language).
-->

---

## Operating principles
- Prefer correctness, clarity, and maintainability over cleverness.
- Don’t invent file paths/APIs/config; if not provided, propose a default and label it as an assumption.
- Don’t silently ignore errors—fail loudly with actionable messages.
- Avoid breaking public APIs unless explicitly requested.

---

## Safety / boundaries
- Avoid destructive operations unless explicitly requested and risks are called out.
- If contributing to migrations/docs/CI pipelines, be conservative and explain implications.

---

# Coding Style Rulebook (Append-only)
These rules are intended to grow over time. If a repo contains additional style configs (.editorconfig/analyzers),
follow them too—unless they conflict with an explicit CS rule below.

## Naming & layout
- CS-001: Public identifiers in PascalCase
  - Rule: Types/classes/methods/properties/constants use PascalCase.
  - Rationale: Matches existing code conventions and .NET norms.

- CS-002: Locals/parameters in camelCase
  - Rule: Local variables and parameters use camelCase.
  - Rationale: Consistency and readability.

- CS-003: Private fields in _underscoreCamelCase
  - Rule: Private members/fields use _underscoreCamelCase.
  - Rationale: Clear visibility and convention alignment.

- CS-004: Use block-scoped namespaces
  - Rule: Prefer `namespace X { ... }` (not file-scoped namespaces).
  - Rationale: Align with existing repo style.

## Bracing & formatting
- CS-010: Allman bracing
  - Rule: Braces go on the next line (Allman style).
  - Rationale: Consistency with existing codebase.

- CS-011: No single-line `if` statements
  - Rule: Don’t write `if (cond) DoThing();` on one line.
  - Rationale: Improves readability and reduces “accidental edit” bugs.

- CS-012: Prefer brace-less single-statement `if` blocks (with multiline formatting)
  - Rule: For a single statement controlled by `if`, omit braces, but format as multiline:
    ```
    if (cond)
        DoThing();
    ```
  - Rationale: Keeps code compact while staying readable.
  - Notes: If the controlled statement is complex, nested, or likely to grow, braces are allowed.

- CS-013: Use `var` when the type is obvious
  - Rule: Use `var` for local type inference whenever possible/clear.
  - Rationale: Reduces noise without harming readability.

- CS-014: Prefer descriptive names over terse names
  - Rule: Use descriptive variable names even for short-lived values (except common patterns like `i` loop index).
  - Rationale: Optimizes for maintainability.

- CS-015: Expression-bodied members only when judicious
  - Rule: Use expression-bodied members for true one-liners when it improves readability; don’t force it.
  - Rationale: Avoids compressed code that’s hard to scan.

## Nullability & safety
- CS-020: Enable nullable reference types
  - Rule: Nullable reference types should be enabled; avoid `!` unless proven safe.
  - Rationale: Prevents NREs and improves API clarity.

- CS-021: Avoid magic strings
  - Rule: Avoid hard-coded “magic” strings; prefer helpers or config settings.
  - Rationale: Renames/attribute overrides shouldn’t break logic.

## Collections & determinism
- CS-030: Prefer concrete collection types unless abstraction is needed
  - Rule: Prefer `List<T>`, `Dictionary<TKey,TValue>` etc. over interfaces unless abstraction is required.
  - Rationale: Clearer intent and simpler usage.

- CS-031: Use collection initializers for static dictionaries
  - Rule: For static dictionary data, prefer collection initializers for clarity.
  - Rationale: More readable than chained `Add`.

- CS-032: Use indexer (`["key"] = value`) syntax for dictionary initializers
  - Rule: Prefer:
    ```csharp
    var dict = new Dictionary<string, int>
    {
        ["one"] = 1,
        ["two"] = 2,
    };
    ```
    over `{ "one", 1 }` tuple syntax or post-init `Add`.
  - Rationale: Cleaner, consistent initialization.

## Strings, paths, and common helpers
- CS-040: Use `string.Empty` for empty-string constants
  - Rule: Prefer `string.Empty` over `""` when representing an empty string constant.
  - Rationale: Clarity and intent.

- CS-041: Prefer `string.IsNullOrWhiteSpace` / `IsNullOrEmpty`
  - Rule: Use built-ins instead of manual checks.
  - Rationale: Fewer bugs and clearer code.

- CS-042: Prefer interpolated strings
  - Rule: Prefer `$"..."` over `string.Format`.
  - Rationale: Readability.

- CS-043: Prefer `Path.Combine`
  - Rule: Use `Path.Combine` rather than manual path concatenation.
  - Rationale: Cross-platform correctness.

- CS-044: Sanitize filenames when needed
  - Rule: Use `Path.GetInvalidFileNameChars()` when accepting filename input.
  - Rationale: Prevents invalid output and security issues.

## IDisposable
- CS-050: Prefer using declarations
  - Rule: Prefer `using var x = ...;` where possible.
  - Rationale: Less indentation, safer cleanup.

## Iteration & LINQ
- CS-060: Prefer `foreach` unless index is needed
  - Rule: Use `foreach` over `for` for collections unless index is required.
  - Rationale: Less error-prone.

- CS-061: Avoid LINQ for complex loop logic
  - Rule: Use `for`/`foreach` instead of LINQ when the loop body is more than simple projection/filtering.
  - Rationale: Debuggability and maintainability.

## Comments & docs
- CS-070: Public APIs require XML docs
  - Rule: Add XML documentation comments for public classes/methods/properties.
  - Rationale: API clarity and generated docs quality.

- CS-071: Comment complex logic blocks (not every line)
  - Rule: Prefer comments above complex blocks (especially migrations/introspection) rather than inline noise.
  - Rationale: Cleaner code and better signal.

## Async
- CS-080: Async method naming and return types
  - Rule: Async methods end with `Async` and return `Task`/`Task<T>`.
  - Rationale: Standard .NET conventions.

- CS-081: Avoid `.Result` / `.Wait()`
  - Rule: Don’t block on tasks; use `await` and propagate async.
  - Rationale: Prevents deadlocks and improves responsiveness.

- CS-082: CancellationToken last parameter
  - Rule: Include `CancellationToken` and keep it last.
  - Rationale: Consistent callsites and composability.

- CS-083: Consider `ConfigureAwait(false)` in library code
  - Rule: Use `ConfigureAwait(false)` where appropriate for library code paths.
  - Rationale: Avoids sync-context capture surprises.

## Tests
- CS-100: Use descriptive test method names
  - Rule: Test method names should describe the scenario and expected behavior (e.g. `GetColumnName_ReturnsExpectedColumn_WhenPropertyIsValid`).
  - Rationale: Improves test readability and maintainability.

- CS-101: Arrange-Act-Assert structure
  - Rule: Structure tests with clear Arrange, Act, Assert sections (can be separated by blank lines and comments indicating each section).
  - Rationale: Improves readability and clarity of test intent.

- CS-102: Avoid test logic in loops or conditionals
  - Rule: Tests should be straightforward and not contain complex logic, loops, or conditionals that could obscure the intent.
  - Rationale: Keeps tests simple and focused on verifying behavior.

---

## Examples (style intent)
Correct:
```csharp
/// <summary>Gets the column name for the provided property.</summary>
public string GetModelNameColumn()
{
    var column = RelmHelper.GetColumnName<ExampleModel>(x => x.ModelName);
    return column;
}
```

Incorrect:
```csharp
// Magic string; breaks if renamed or overridden via attributes
public string GetModelNameColumn() => "model_name";
```

---

# Project / repo workflow (keep builds green)
## Repository layout
- `CoreRelm/` – Core library
- `CoreRelm.Tests/` – Unit tests
- `examples/CoreRelm.QuickStart/` – Runnable examples
- `docs/` – Generated docs (do not hand-edit `docs/api/`)

## Build / test
- `dotnet restore`
- `dotnet build`
- `dotnet test`

## DocFX
- Run commands from `CoreRelm/docs/`
- `docfx build`

## Visual Studio
- Rebuild Solution to compile
- Test Explorer -> Run All Tests
- To run examples: set `examples/CoreRelm.QuickStart` as startup project

## Documentation boundary
- Do **not** hand-edit generated docs under `docs/api/`.

---

## Metadata, Introspection, and Migrations
- Keep MySQL-specific logic isolated.
- Add tests for edge cases (indexes, FKs, triggers).
- Favor focused tests around helpers/resolvers/loaders/context behaviors.

---

## PR checklist
- Builds cleanly: `dotnet build`
- Tests pass: `dotnet test`
- Added/updated tests where behavior changed
- Public APIs have XML docs
- No edits to `docs/api` generated files
- Changes respect existing naming/attribute conventions
