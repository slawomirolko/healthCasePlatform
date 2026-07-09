# AGENTS.md — HealthCasePlatform

Project conventions for AI agents working in this repo.

## .NET Conventions

### SDK & Tooling
- **Project naming:** every project starts with the `HealthCasePlatform.` prefix (e.g. `HealthCasePlatform.Domain`, `HealthCasePlatform.Api`).
- **Target framework:** .NET 10 (`net10.0`) for all projects unless a project project has a specific reason to differ.
- **Solution file:** use the `.slnx` (XML) solution format, not the legacy `.sln`.
- **Scaffold projects via the `dotnet new` templates** (`slnx`, `classlib`, `xunit`, `webapi`, etc.) — do not hand-write `.csproj`/`.slnx` files from scratch. Only edit the generated files to add references/packages.
- Delete the template-generated placeholder files (`Class1.cs`, `UnitTest1.cs`) before committing.

### Package Management (Central Package Management)
See `ai/agents/Dotnet/ARCHITECTURE.md` → *Package Management (Central Package Management)* for the CPM file, add-package workflow, and version variables.

### Project Layout (Clean Architecture)
See `ai/agents/Dotnet/ARCHITECTURE.md` for full domain architecture rules (layering, slice-per-aggregate-root, consistency boundaries).
```
src/           # production projects (Domain, Application, Infrastructure, API, ...)
tests/         # test projects (mirror src/ names with `.Tests` suffix)
```
- Domain project has **no infrastructure package references** — no EF Core, no MediatR. It is the dependency leaf. `ErrorOr` is allowed (domain result type, not infrastructure).

### Test Conventions
See `ai/agents/Dotnet/TESTING.md` for full .NET test rules (framework, naming, location, tier strategy).

### Coding Style
See `ai/agents/Dotnet/CODING_STYLE.md` for full .NET coding style rules (formatting, naming, encapsulation, guard clauses, domain patterns).

### Settings & Configuration
See `ai/agents/Dotnet/SETTINGS.md` for the typed-settings (Options) pattern: record shape, registration, layering, defaults, secrets, validation.

### Build & Verify
- Build: `dotnet build`
- Test: `dotnet test`
- Style: `dotnet format --verify-no-changes --no-restore`
- **Integration tests (`*.Tests.Integration`):** require Docker engine running — they spin up real dependencies (SQL Server) via Testcontainers. See `ai/agents/Dotnet/TESTING.md` → *Integration Test Infrastructure*.
- **Local dev DB:** run `docker compose up -d` (starts the SQL Server 2022 container on `localhost:1433`), then export `Database__ConnectionString` (template value in `.env.example`) before `dotnet run`. The API binds `DatabaseSettings:ConnectionString` from this env var; with it unset, startup crashes at `MigrateAsync` (`Program.cs:51`). Override for any non-local environment via the same env var.

### Domain quirks (non-inferable)
- **`RegulatoryCase.ChangeStatus(CaseStatus)` escape hatch:** public transition method with **no endpoint**; tested only for history appending (`ChangeStatus_AppendsHistoryEntryWithFromCurrentToNew`), not for invalid-transition/terminal blocking. Permits arbitrary non-terminal status jumps (e.g. `Draft`→`Approved`, skipping the workflow). Do not wire it to an endpoint without first restricting it or replacing it with explicit terminal transitions (`Approve`/`Reject`/`Archive`).
- **`CaseStatus` persisted as `int`:** `CaseStatus` is stored via `HasConversion<int>()` (`RegulatoryCaseConfiguration.cs`), so adding enum members is schema-neutral — only new tables/FKs require a migration.
- **`CreateCaseRequestValidator` (FluentValidation) is a validation superset of `RegulatoryCase.Create`.** `CreateCaseRequestValidator.cs:9-21` (Title/Country/CreatedBy/CaseTypeId) duplicates `RegulatoryCase.Create` (`RegulatoryCase.cs:42-66`). The endpoint filter `ValidationFilter<CreateCaseRequest>` (`CasesEndpoints.cs:16`) runs first and returns **400**; therefore the domain-error branch in `CreateCase` (`CasesEndpoints.cs:106`, `TypedResults.ValidationProblem` → 400) is **unreachable via HTTP**. Post-refactor the `RegulatoryCase.Create` call moved into `CaseService.CreateAsync` (Application, no FluentValidation at the service tier), so domain-create errors surface only there. The dedicated `CaseServiceTests` covering that branch (REFACTOR_PLAN.md §10.2) were **deferred** — the branch has no dedicated unit coverage and is exercised only via the create happy path through the integration tier.
