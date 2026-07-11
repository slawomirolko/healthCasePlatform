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
- Domain project has **no infrastructure package references** — no EF Core, no MediatR, no Mediator. It is the dependency leaf. `ErrorOr` is allowed (domain result type, not infrastructure).

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
- **Local dev DB:** run `docker compose up -d` (starts the SQL Server 2022 container on `localhost:1433`), then export `Database__ConnectionString` (template value in `.env.example`) before `dotnet run`. The API binds `DatabaseSettings:ConnectionString` from this env var; with it unset, startup crashes at `MigrateAsync` (`Program.cs:55`). Override for any non-local environment via the same env var.
- **Local dev RabbitMQ:** run `docker compose up -d rabbitmq` (broker on `localhost:5672`, management UI `:15672`) before `dotnet run`. The `OutboxDispatcher` + `CaseSubmittedConsumer` hosted services connect on startup; `appsettings.json` carries local defaults (`guest`/`guest`) — override via `RabbitMq__UserName`/`RabbitMq__Password` env vars for non-local. Broker outage does **not** crash the host (hosted services retry in `ExecuteAsync` and never throw).

### Domain quirks (non-inferable)
- **`RegulatoryCase.ChangeStatus(CaseStatus)` escape hatch:** public transition method with **no endpoint**; tested only for history appending (`ChangeStatus_AppendsHistoryEntryWithFromCurrentToNew`), not for invalid-transition/terminal blocking. Permits arbitrary non-terminal status jumps (e.g. `Draft`→`Approved`, skipping the workflow). Do not wire it to an endpoint without first restricting it or replacing it with explicit terminal transitions (`Approve`/`Reject`/`Archive`).
- **`CaseStatus` persisted as `int`:** `CaseStatus` is stored via `HasConversion<int>()` (`RegulatoryCaseConfiguration.cs`), so adding enum members is schema-neutral — only new tables/FKs require a migration.
- **`CreateCaseRequestValidator` (FluentValidation) is a validation superset of `RegulatoryCase.Create`.** `CreateCaseRequestValidator.cs:9-29` (Title/Description/CreatedBy/Country/Priority) duplicates `RegulatoryCase.Create` (`RegulatoryCase.cs:42-66`). The endpoint filter `ValidationFilter<CreateCaseRequest>` (`CasesEndpoints.cs:18`) runs first and returns **400**; therefore the domain-error branch in `CreateCase` (`CasesEndpoints.cs:109`, `TypedResults.ValidationProblem` → 400) is **unreachable via HTTP**. Post-refactor the `RegulatoryCase.Create` call moved into `CreateCaseCommandHandler.Handle` (Application, no FluentValidation at the handler tier), so domain-create errors surface only there. The dedicated handler unit tests covering that branch (the deferred `CaseServiceTests`, REFACTOR_PLAN.md §10.2) are now delivered in `HealthCasePlatform.Application.Tests` (`CreateCaseCommandHandlerTests.Handle_WhenTitleEmpty_ReturnsValidationErrors` / `Handle_WhenDomainCreateFails_DoesNotPersist`).
- **Unauthorized review returns 403 (API resource-based authz), not 409.** `StartScientificReview`/`StartLegalReview` are two-stage-gated: widened coarse role gate `RequireRole(<ReviewerRole>, TeamLeader)` + a resource-based check (`ReviewAuthorizationFilter` → `ReviewCaseAuthorizationHandler`) that 403s unless `IsTeamLeader || AssignedXReviewerId == userId`. The rule guards **HTTP entry points only** — `ChangeStatus` and any non-HTTP caller (background job/saga calling the handler directly) bypass it. `AssignedScientificReviewerId`/`AssignedLegalReviewerId` are nullable `nvarchar(100)` columns; `CaseStatus` still `HasConversion<int>()` so only the 2-column migration is schema-relevant (existing rows get NULL ⇒ only TeamLeader can review legacy cases until reassigned). `AssignReviewerRequestValidator.MaximumLength(100)` matches the column — without it a >100-char id passes filter+domain and EF throws `DbUpdateException` → 500.
- **Audit is dual-adapter behind `IAuditLogWriter` (Domain port, Infra adapters).** `SqlAuditLogWriter` **stages** on the request's scoped `AppDbContext` (do NOT add `SaveChanges` there) so the handler's single `SaveChangesAsync` flushes case+history+audit atomically — same guarantee as the old `ICaseRepository.AddAuditEntryAsync` path. `MongoAuditLogWriter` writes immediately (`InsertOneAsync`) and **cannot** share the SQL transaction → a Mongo audit may be orphaned if the SQL save rolls back. Selection = `Audit:Provider` (`SqlServer` default | `MongoDb`); default is non-breaking (existing SQL behavior). `IAuditLogWriter` carries read+write so the `/audit` endpoint resolves to the same store it wrote to. Shared behavioral contract = `AuditLogWriterContractTests` (run per adapter); SQL-only atomicity = `AuditTransactionTests`.
- **`CaseSubmitted` notification is asynchronous via RabbitMQ + transactional outbox.** `SubmitCaseCommandHandler` enqueues an `OutboxMessage` in the same EF transaction as the case submit (`ICaseEventOutbox`, staged by `SqlCaseEventOutbox` on the request `AppDbContext`). An `OutboxDispatcher` BackgroundService relays staged rows to RabbitMQ exchange `case-events` / queue `case-submitted`; a `CaseSubmittedConsumer` BackgroundService writes the `Notification` row in its **own** transaction. Therefore the `Notification` row is **eventual** (exists after relay + delivery, not at HTTP 200) — tests must poll, not assert immediately. The **enqueue** is atomic (no lost event); the **notification write** is not transactional with the case. `INotificationWriter`/`SqlNotificationWriter` staging contract is unchanged; the consumer owns `SaveChangesAsync`. **Broker outage does not block the business op:** when RabbitMQ is down the case transition still commits and the outbox row stays pending (`ProcessedAtUtc == null`) — the dispatcher retries on its next tick. The consumer is **idempotent on redelivery** via the unique index `IX_Notifications_CaseId_Type` (created in the `AddOutbox` migration): a duplicate delivery's `SaveChangesAsync` throws a unique-violation (`2627`/`2601`) which the consumer catches and acks. First `IHostedService` registrations in the repo; both resolve `IConnectionFactory` (Singleton) and open their own connection in a retry-tolerant `ExecuteAsync`. `INotificationWriter` registration is now provider-independent (moved out of the SQL-audit `else`-branch — also fixes a latent bug where it was unregistered under `Audit:Provider=MongoDb`).
