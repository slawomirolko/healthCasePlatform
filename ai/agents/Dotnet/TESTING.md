# TESTING.md — .NET Test Conventions

Testing rules for all .NET projects in this repo. AI agents and skills (e.g. `olko-test`) read this file before running or writing tests.

## Framework & Libraries
- **Framework:** xUnit (`dotnet new xunit`)
- **Assertion library:** [Shouldly](https://github.com/shouldly/shouldly) — prefer over FluentAssertions or raw `Assert`
- **Mocking:** NSubstitute (when needed) — do not introduce Moq or other mocking frameworks
- **Test project naming:** `<SourceProject>.Tests` (e.g. `HealthCasePlatform.Domain.Tests`)
- **Integration test naming:** `<SourceProject>.Tests.Integration` or `<SourceProject>.Integration.Tests`

## Test Naming
`MethodName_Condition_ExpectedResult`

Examples:
- `Submit_WhenNotDraft_ThrowsInvalidOperationException`
- `Complete_WhenAlreadyCompleted_ThrowsInvalidOperationException`
- `AddDocument_AddsToDocumentsCollection`

## Test Location
Mirror the source folder structure under `tests/<Project>.Tests/`.

| Source | Test |
|--------|------|
| `src/HealthCasePlatform.Domain/Entities/RegulatoryCase.cs` | `tests/HealthCasePlatform.Domain.Tests/Entities/RegulatoryCaseTests.cs` |
| `src/HealthCasePlatform.Domain/Enums/CaseStatus.cs` | (no test — enum, no behavior) |

## Test Tier Strategy
- **Unit tests** — edge cases, error paths, boundaries, domain logic. Always present for domain methods.
- **Integration tests** — main happy paths and core workflows. Added when the persistence/API layer exists.
- When a test failure is in an integration test for an edge case → suggest moving it to a unit test.
- When a test failure is in a unit test for a main path → suggest adding an integration test covering that flow.

## Integration Test Infrastructure

When the API layer (`HealthCasePlatform.Api`) and its `.Tests.Integration` project exist, integration tests use **`WebApplicationFactory<Program>`** + **Testcontainers**. Read this section before writing or running API integration tests.

### Docker requirement
- **Docker engine must be running** on the host for `dotnet test` of any `*.Tests.Integration` project that uses Testcontainers.
- Testcontainers auto-pulls container images on first run (e.g. `mcr.microsoft.com/mssql/server:2022-latest`).
- Document this prerequisite wherever test commands are listed (AGENTS.md `Build & Verify`, CI pipelines).

### Libraries
- **`Microsoft.AspNetCore.Mvc.Testing`** — in-process host via `WebApplicationFactory<Program>`.
- **`Testcontainers.MsSql`** — ephemeral Docker SQL Server (matches the production provider `Microsoft.EntityFrameworkCore.SqlServer`).
- Do **not** substitute SQLite or in-memory providers for SQL Server integration tests — SqlServer-specific configuration (JSON columns, computed columns) must be exercised against the real provider.

### WebApplicationFactory pattern
- The API host's `Program` class must be exposed for the test assembly: add `public partial class Program;` at the end of `Program.cs`.
- A shared factory (`<Api>Factory : WebApplicationFactory<Program>, IAsyncLifetime`) owns the container lifecycle: `InitializeAsync` starts the container, `DisposeAsync` tears it down.
- Override the production DB registration in `ConfigureTestServices` — remove the existing `DbContextOptions<AppDbContext>` service descriptor and re-add `AddDbContext<AppDbContext>` pointing at the container's connection string. Production startup code (`MigrateAsync`, `AddDbContextCheck`) then runs against the container automatically.
- Tests consume the factory via `IClassFixture<TFactory>` (one container per test class) or `ICollectionFixture<TFactory>` (one container shared across the suite — promote when per-class startup cost becomes painful).

### Fixture reuse
- Prefer reusing the existing `<Api>Factory` over writing a new one per test class.
- A container is fresh per fixture instance — no cross-test cleanup logic is needed; cleanup is container disposal.

## Rules
- No log-check assertions unless the user explicitly asks for them.
- Never add skip logic to tests.
- Never modify generated files (EF migrations, gRPC stubs) — regenerate instead.
- Use the project's existing assertion/mock libraries — don't introduce new ones.
- Delete template-generated placeholder files (`UnitTest1.cs`) before committing.
