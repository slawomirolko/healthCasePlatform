# AGENTS.md — Application layer

Caveman-strict. Layer rules only. Shared rules → root `AGENTS.md` + `ai/agents/Dotnet/ARCHITECTURE.md`.

## What lives here

Use cases. `ICaseService` + `CaseService` per aggregate slice. Query models (`Cases/Models/`). Orchestrate repo + domain. Return domain entities + `ErrorOr`. No persistence.

## Dependency

→ Domain only. No Infrastructure ref. No EF Core. No `AppDbContext`. No `IOptions<T>`. No settings. No HTTP/serialization types.

## Hard rules

- One service interface per aggregate use cases. Registered by interface, consumed by interface. `AddApplication()` → `services.AddScoped<IXxxService, XxxService>()`.
- Service impl `sealed class`, explicit ctor for injected `IXxxRepository`. No primary ctor DI.
- Use `ErrorOr<T>` end-to-end. Domain error → propagate `result.Errors` (not re-wrap). Not-found = `Error.NotFound` produced HERE (repo returned null → domain never sees absence).
- Transitions return `ErrorOr<RegulatoryCase>` (single error channel): `Error.NotFound` (case missing) OR domain `Error.Conflict` (transition failed). Never `ErrorOr<T?>` (no null+error mix). Handler precedence unambiguous.
- Create flow: domain factory `RegulatoryCase.Create` → `repo.AddAsync` → `repo.SaveChangesAsync` → return entity. No FluentValidation at this tier (that = Api filter).
- List: clamp `Math.Max(1,page)` + `Math.Clamp(pageSize,1,100)`, normalize `Country.Trim().ToUpperInvariant()` here (moved from handler). Return `PagedResult<RegulatoryCase>`. Boundary maps entity→DTO.
- All I/O `async` + forward `CancellationToken cancellationToken = default`. No `.Result`/`.Wait()`.
- Models = `sealed record`. Query DTOs (`CaseListFilter`), paged carriers (`PagedResult<T>`). No domain behavior on them.
- C# user-defined implicit conv NOT applied from interface type. `ErrorOr<IReadOnlyList<T>>` from `IReadOnlyList<T>` → use `ErrorOrFactory.From(list)`, not bare `return list;`.

## DI

`DependencyInjection.AddApplication(IServiceCollection)` = one extension, called explicit in `Program.cs`. Granular per slice. No `AddXxx` hidden inside other extensions.
