# AGENTS.md — Api layer

Caveman-strict. Layer rules only. Shared rules → root `AGENTS.md` + `ai/agents/Dotnet/ARCHITECTURE.md`.

## What lives here

Thin endpoints. Routing, DI injection, req→`IMediator.Send` call, result→HTTP/DTO map. DTO records. FluentValidation filters. Composition root (`Program.cs`).

## Dependency

→ Application + Infrastructure (composition root only). Endpoints inject **`IMediator` only** (+ nothing else) — never `AppDbContext`, never Application handlers directly. Host wires `AddMediator()` + `AddInfrastructure(config)` (no `AddApplication`).

## Hard rules

- Endpoint = route + name + filter + handler. Handler injects `IMediator` (+ nothing else) + `CancellationToken`. No business rules here. No EF. No `IOptions`. Handler builds a command/query record and `await mediator.Send(message, ct)`.
- HTTP mapping stays in handler → status codes / content types / problem-detail `detail` strings byte-for-byte preserved. Routes/names/filters/result types unchanged on refactor.
- Entity→DTO map at boundary (`ToCaseResponse`, inline `Select`). Never expose domain entity in response. DTOs = `sealed record` in `Cases/`.
- Transitions: handler returns `ErrorOr<RegulatoryCase>`. Map `Error.Type == ErrorType.NotFound` → `TypedResults.NotFound()` (404); else → `TypedResults.Problem(409, title:"Conflict", type:"…6.5.8", detail: error.Description)`. Ok → `TypedResults.Ok(ToCaseResponse)`.
- Input validation = FluentValidation endpoint filter (`ValidationFilter<T>`) → 400 first. Domain factory errors after that = unreachable via HTTP (superset). See root `AGENTS.md` Domain quirks.
- `AddValidatorsFromAssemblyContaining<Program>`. `AddProblemDetails()` + `UseExceptionHandler()` + `UseStatusCodePages`. Health checks `AddDbContextCheck<AppDbContext>` + `MapHealthChecks("/health")`. Swagger.
- `MigrateAsync` startup block resolves `AppDbContext` (registered by `AddInfrastructure`) — unchanged. `public partial class Program;` at file end for `WebApplicationFactory<Program>`.
- `ApiFactory` test override: `RemoveAll<DbContextOptions<AppDbContext>>()` + re-add `AddDbContext`. Still applies — `AddInfrastructure` uses `AddDbContext`. No edit needed: `AddMediator` runs in the test host too, so integration tests exercise the real Mediator pipeline.
