# AGENTS.md — Domain layer

Caveman-strict. Layer rules only. Shared rules → root `AGENTS.md` + `ai/agents/Dotnet/ARCHITECTURE.md`.

## What lives here

Dependency leaf. Aggregate roots, child entities, `<Entity>Errors`, slice-owned repo interfaces, `Enums/`, `Common/Entity`. No infrastructure.

## Hard rules

- No infra refs. No EF Core. No MediatR. No Mediator. No `IOptions<T>`. No settings types. No `[JsonPropertyName]`.
- Allowed package: `ErrorOr` (domain result type, not infra). Nothing else.
- Slice-per-aggregate-root. One folder per root (`Cases/`). Root + children + `<Entity>Errors` + repo interface all in slice. Nothing leak across slice.
- Repo interface owned here, impl in Infrastructure: `Cases/ICaseRepository.cs`. Primitives only in interface (no EF types). Domain query-bag-free.
- Child entities never ref siblings. Cross-aggregate = `Guid <Name>Id`, never object pointer.
- Entity = `sealed class` + `Entity` base. `private set` all props. Private parameterless ctor (persistence seam). `public static ErrorOr<T> Create(...)` factory = only public way in.
- Validation in factory. State transitions = methods return `ErrorOr<Success>`. No `try/catch`. No throw for control flow.
- Errors = `public static readonly Error` on `<Entity>Errors` class, same folder. Code `<Entity>.<Name>`. Never inline `Error.Validation("...")`.
- `Guid.CreateVersion7()` PKs. `DateTime.UtcNow`. Collections = `IReadOnlyList<T>` over `List<T> = []` backing field.

## Do not

- Add `Error.NotFound` here. Domain never fetches → never knows "not found". That = Application concern.
- Hand-write `.csproj`/`.slnx`. Scaffold via `dotnet new`. Edit generated only for refs/packages.
