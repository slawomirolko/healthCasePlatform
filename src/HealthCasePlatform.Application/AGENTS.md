# AGENTS.md — Application layer

Caveman-strict. Layer rules only. Shared rules → root `AGENTS.md` + `ai/agents/Dotnet/ARCHITECTURE.md`.

## What lives here

Use cases = CQRS handlers. `Mediator` command/query messages + handlers per aggregate slice (`Cases/Commands/`, `Cases/Queries/`). Handlers orchestrate repo + domain, return domain entities + `ErrorOr`. Query models (`Cases/Models/`). No persistence.

## Dependency

→ Domain only. No Infrastructure ref. No EF Core. No `AppDbContext`. No `IOptions<T>`. No settings. No HTTP/serialization types.

## Hard rules

- Use cases = `sealed class` handlers (one responsibility each). Handler injects `ICaseRepository` via explicit ctor — no primary ctor DI. Handlers auto-registered by `AddMediator` in the host (Api) — no manual DI extension, no `AddApplication`.
- Messages = `sealed record` carrying use-case inputs: `ICommand<ErrorOr<T>>` (mutate) / `IQuery<T>` (read). One message + handler per use case. Handlers return `ValueTask<TResponse>` (Mediator).
- Use `ErrorOr<T>` end-to-end. Domain error → propagate `result.Errors` (not re-wrap). Not-found = `Error.NotFound` produced HERE via `internal static CaseErrors.NotFound` (repo returned null → domain never sees absence). `InternalsVisibleTo` exposes it to `Application.Tests`.
- Transitions share orchestration via `internal static CaseTransitionHelper.TransitionAsync(repo, id, transition, ct)`. Transitions return `ErrorOr<RegulatoryCase>` (single error channel): `Error.NotFound` (case missing) OR domain `Error.Conflict` (transition failed). Never `ErrorOr<T?>` (no null+error mix). Handler precedence unambiguous.
- Create flow (`CreateCaseCommandHandler`): domain factory `RegulatoryCase.Create` → `repo.AddAsync` → `repo.SaveChangesAsync` → return entity. No FluentValidation at this tier (that = Api filter).
- List (`ListCasesQueryHandler`): clamp `Math.Max(1,page)` + `Math.Clamp(pageSize,1,100)`, normalize `Country.Trim().ToUpperInvariant()` HERE. Return `PagedResult<RegulatoryCase>`. Boundary maps entity→DTO. Filter args flattened into the query message (no `CaseListFilter` bag).
- All I/O `async` + forward `CancellationToken cancellationToken = default`. No `.Result`/`.Wait()`.
- Models = `sealed record`. Paged carriers (`PagedResult<T>`). No domain behavior on them.
- C# user-defined implicit conv NOT applied from interface type. `ErrorOr<IReadOnlyList<T>>` from `IReadOnlyList<T>` → use `ErrorOrFactory.From(list)`, not bare `return list;`.

## DI

No `AddApplication` extension — handlers auto-registered by `AddMediator(o => { o.ServiceLifetime = ServiceLifetime.Scoped; o.Assemblies = [typeof(XxxCommand).Assembly]; })` called explicitly in `Program.cs`. Scoped is load-bearing: handlers inject Scoped `ICaseRepository` (uses Scoped `AppDbContext`); Mediator's default Singleton would throw `Cannot consume scoped service from singleton` at first dispatch.
