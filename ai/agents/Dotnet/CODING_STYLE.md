# CODING_STYLE.md — .NET Coding Style

Coding style rules for all .NET projects in this repo. AI agents and skills (e.g. `olko-commit-style`, `olko-test`) read this file before writing or reviewing .NET code.

## Formatting
- **Indentation:** 4 spaces (enforced by `dotnet format`).
- **Braces:** Allman style (opening brace on a new line) for types, methods, and statements. Exception: single-line expression-bodied members and one-liners (`private Entity() { }`).
- **Expression-bodied members** for trivial one-liners (`public override int GetHashCode() => ...;`, `public bool IsOverdue() => ...;`).
- **Collection expressions** for initialization: `[]`, not `new List<T>()` — e.g. `private readonly List<CaseDocument> _documents = [];`.
- **`using` directives:** outside the namespace, sorted alphabetically. Project-qualified (`using HealthCasePlatform.Domain.Enums;`) only when needed.
- **File-scoped namespaces** (`namespace Foo;`), not block-scoped.
- **One type per file**, filename matches type name.
- **Blank line** between members; no double blank lines.

## Types & Inheritance
- **`sealed`** on every class that is not designed to be inherited — domain entities, DTOs, services, handlers. Only the `Entity` base class is `abstract`.
- **Prefer `record`** over `class` for immutable data carriers that do not need identity or domain behavior: DTOs, view models, commands, queries, events, config objects. Do **not** use records for domain entities (they need private setters, mutable backing fields, and `Entity` inheritance). Typed settings records (`sealed record` + `SectionName` + `{ get; init; }`) follow `ai/agents/Dotnet/SETTINGS.md`.
- **Records are `sealed` by default** — no extra modifier needed.
- **Structs:** prefer `readonly record struct` for value objects when equality by value is required.

## Naming
- **Types, methods, public properties:** PascalCase (`RegulatoryCase`, `AddDocument`, `CaseTypeId`).
- **Private fields:** `_camelCase` with underscore prefix (`_documents`, `_tasks`).
- **Parameters, locals:** camelCase (`caseId`, `newAssignee`).
- **Enums:** PascalCase type name + PascalCase members (`CaseStatus.Submitted`). **Always explicitly numbered** — every member has an explicit value starting at 1 (`Draft = 1, Submitted = 2, ...`). Never rely on implicit zero-based numbering.
- **Interfaces:** `I` prefix (`IRepository<T>`).
- **Test methods:** `MethodName_Condition_ExpectedResult` (see `TESTING.md`).

## Properties & Encapsulation
- **`private set`** on all entity properties — mutation only via domain methods.
- **`protected set`** only on the `Id` property of the `Entity` base (persistence seam).
- **Expose collections as `IReadOnlyList<T>`** with a private mutable backing field:
  ```csharp
  private readonly List<CaseDocument> _documents = [];
  public IReadOnlyList<CaseDocument> Documents => _documents;
  ```
- **Nullable reference types enabled** (`<Nullable>enable</Nullable>`). Use `string?` for optionals, `string` for required.
- **`Guid` identifiers** — never `int`. Generate via `Guid.CreateVersion7()` in the factory (time-ordered UUID v7 — better for database indexing than random `Guid.NewGuid()`).

## Factories & Constructors
- **Static factory method** for creation: `public static ErrorOr<Type> Create(...)` — validates inputs, returns the entity on success or an `Error` on failure. This is the only public way to instantiate a domain entity.
  ```csharp
  public static ErrorOr<RegulatoryCase> Create(string title, string description, Guid caseTypeId, CasePriority priority, string createdBy)
  {
      if (string.IsNullOrWhiteSpace(title))
          return Error.Validation("Case title cannot be empty.");

      return new RegulatoryCase
      {
          Id = Guid.NewGuid(),
          Title = title,
          // ...
      };
  }
  ```
- **Private constructor** (default, parameterless) — used by the factory and as a persistence seam. Never public.
  ```csharp
  private RegulatoryCase() { }
  ```
- Factory parameters are **plain names** (not `pTitle`, `p_caseId`).
- All input validation lives in the factory, never in the constructor body.

## Error Handling — ErrorOr (no exceptions)
- **Never throw exceptions** for domain validation or state transitions. Return `ErrorOr<T>` instead.
- **Factories** return `ErrorOr<T>` — `Error.Validation(...)` for bad input, `Error.Conflict(...)` for business-rule violations.
- **Mutating methods** return `ErrorOr<Success>` — `Error.Conflict(...)` for invalid state transitions.
- **Queries** (pure methods like `IsOverdue`) return the value directly — no `ErrorOr` wrapper.
- **Predefined errors** — never inline `Error.Validation("...")` in domain code. Define each error once as a `public static readonly Error` field on a dedicated `<Entity>Errors` class that lives in the same directory as the entity:
  ```csharp
  public static class RegulatoryCaseErrors
  {
      public static readonly Error TitleEmpty =
          Error.Validation("RegulatoryCase.TitleEmpty", "Case title cannot be empty.");

      public static readonly Error NotDraft =
          Error.Conflict("RegulatoryCase.NotDraft", "Only a draft case can be submitted.");
  }
  ```
- **Error code convention:** `<Entity>.<ErrorName>` as the first arg (e.g. `"CaseTask.AlreadyCompleted"`), human-readable description as the second.
- **Usage in entity:**
  ```csharp
  if (string.IsNullOrWhiteSpace(title))
      return RegulatoryCaseErrors.TitleEmpty;
  ```
- **Error types:** `Error.Validation`, `Error.Conflict`, `Error.NotFound`, `Error.Failure`, `Error.Unexpected`.
- **No `try/catch`** in domain code. Errors flow through return values.
- **Exceptions are reserved** for infrastructure (I/O, network, database) — never for domain logic.

## Domain Model Patterns
- **Aggregate Root** — only `RegulatoryCase` (and future roots) expose child entities. Children (`CaseDocument`, `CaseTask`, `Comment`, `Decision`) are added only via root methods (`AddDocument`, `AddTask`, etc.).
- **Rich Domain Model** — behavior lives on entities, not in external services. State transitions, completion, reassignment, and editing are all methods on the entity.
- **`DateTime.UtcNow`** for timestamps — never `DateTime.Now`.
- **State transitions** set `UpdatedAt` alongside the status change.
- **Terminal states** (Approved, Rejected, Archived) are guarded — no transition out.

## Comments
- **No comments** unless explicitly requested by the user. Code should be self-documenting via clear names and small methods.

## Method Hygiene
- ✅ Keep methods under **30 lines** — extract when longer.
- ❌ Do not use primary constructors for dependency injection — use explicit constructors for injected services.
- ✅ Method names describe a single responsibility (`Submit`, `Assign`, `Reassign`).
- ❌ Do not create method names containing `And` (e.g. `ValidateAndSave`, `FetchAndStore`) — split into separate methods.

## Control Flow & Parameters
- ✅ All control flow statements (`if`, `else`, `for`, `foreach`, `while`, `using`, `lock`) use braces, even for a single-statement body.
- ❌ Do not omit braces on control flow.
- ✅ `CancellationToken` parameters use the full name with a default: `CancellationToken cancellationToken = default`.
- ❌ Do not abbreviate (`ct`, `token`).

## Namespaces & File Organization
- ✅ Namespace must match the file's folder path from the project root (`HealthCasePlatform.Domain/Cases/RegulatoryCase.cs` → `HealthCasePlatform.Domain.Cases`).
- ❌ Do not use a namespace that doesn't match the file's directory path.
- ✅ `InternalsVisibleTo` is declared in `.csproj` only (`<InternalsVisibleTo Include="Project.Tests" />`), and only for the test project that needs it.
- ❌ Do not declare `[assembly: InternalsVisibleTo(...)]` in `.cs` / `AssemblyInfo.cs`.
- ❌ Do not expose internals to non-test projects.

## Modern C# (additions)
- ✅ Use `ArgumentNullException.ThrowIfNull(x)` / `ArgumentOutOfRangeException.ThrowIfNegativeOrZero(x)` instead of manual null/range checks.
- ✅ Use raw string literals (`""" ... """`) for multi-line strings and embedded JSON.
- ✅ Use `required` on non-nullable properties of DTOs/records when the constructor isn't enforcing it.
- ✅ Prefer `global using` directives in a dedicated `GlobalUsings.cs`.
- ❌ Do not use the null-forgiving operator (`!`) unless absolutely certain the value is non-null.

## Performance
- ✅ All I/O is `async`/`await` and forwards `CancellationToken`.
- ❌ Do not use blocking calls (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`) in async code.
- ✅ Use `ValueTask<T>` when a method may complete synchronously (e.g. cache hits).
- ✅ Materialize LINQ once (`.ToList()`) when enumerating multiple times.
- ❌ Do not enumerate an `IEnumerable<>` multiple times.
- ✅ Use `Any()` over `Count() > 0` for existence checks.
- ✅ Prefer `StringBuilder` over `+` concatenation inside loops.
- ✅ Pre-size collections when the count is known: `new List<CaseTask>(count)`.

## Silent Failures — Zero Tolerance
- ❌ Never return `null`, empty collections, or default values when a required file/resource/config is missing.
- ❌ Never swallow an exception and return an empty/default result without logging or throwing.
- ✅ If a file is expected but missing → throw `FileNotFoundException` with the path in the message.
- ✅ If a config key doesn't resolve → throw `InvalidOperationException` naming the expected key.
- ✅ Fail fast with a clear message; never silently skip processing.

## Rules
- The rule source of truth is this file + `AGENTS.md`. If a rule here ever contradicts a doc, the doc wins — surface the conflict.
- Never hardcode project-specific paths or behavior — read them from config or the project adapter.
- Run `dotnet format --verify-no-changes --no-restore` before committing. Exit 0 = clean.
