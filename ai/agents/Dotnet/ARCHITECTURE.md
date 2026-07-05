# ARCHITECTURE.md — .NET Domain Architecture

Domain architecture rules for this repo. AI agents and skills read this file to verify structural decisions, dependency direction, and layering.

## Clean Architecture Layers

```
src/           # production projects
  HealthCasePlatform.Domain/          # dependency leaf — no infrastructure refs
  HealthCasePlatform.Application/     # use cases, CQRS handlers (future)
  HealthCasePlatform.Infrastructure/  # EF Core, external services (future)
  HealthCasePlatform.Api/             # ASP.NET Core endpoints (future)
tests/         # test projects mirror src/ names with .Tests suffix
```

**Dependency direction:** API → Application → Domain. Infrastructure → Application → Domain. Domain depends on nothing (except `ErrorOr` — domain result type, not infrastructure).

## Domain Project — Slice-per-Aggregate-Root

The Domain project is **not** organized by technical concern (`Entities/`, `ValueObjects/`, `Services/`). It is organized into **slices**, one per aggregate root. Each slice groups everything related to that aggregate in a single folder.

### Current structure (single aggregate, sliced)

The `Cases/` slice holds everything for the `RegulatoryCase` aggregate. `Common/` holds the shared `Entity` base. `Enums/` holds cross-slice enums.

```
HealthCasePlatform.Domain/
  Common/
    Entity.cs                  # abstract base shared across slices
  Enums/                       # shared enums (cross-slice)
    CaseStatus.cs
    CasePriority.cs
  Cases/                       # RegulatoryCase slice
    RegulatoryCase.cs          # aggregate root
    RegulatoryCaseErrors.cs
    CaseType.cs                # reference entity owned by this slice
    CaseTypeErrors.cs
    CaseDocument.cs            # child entity
    CaseDocumentErrors.cs
    CaseTask.cs
    CaseTaskErrors.cs
    Comment.cs
    CommentErrors.cs
    Decision.cs
    DecisionErrors.cs
    Events/                    # domain events for this aggregate (future)
    ICaseRepository.cs         # repository interface (future, owned by domain)
```

### Adding a second slice (when a new aggregate arrives)

When a second aggregate root appears (e.g. `User`, `Workflow`), add a new sibling folder under the Domain root. Each slice is self-contained:

```
HealthCasePlatform.Domain/
  Common/
    Entity.cs
    Errors/                    # shared error types if any
  Enums/
  Cases/                       # RegulatoryCase slice (as above)
  Users/                       # User slice (future)
    User.cs
    UserErrors.cs
    Role.cs
    RoleErrors.cs
    Events/
    IUserRepository.cs
```

### Slice rules

1. **One folder per aggregate root** — named after the aggregate (e.g. `Cases/`, `Users/`, `Workflows/`).
2. **Everything for the aggregate lives in its slice** — the root, its child entities, its `<Entity>Errors` classes, its domain events, its repository interface. Nothing related to one aggregate leaks into another slice's folder.
3. **Shared code lives in `Common/` and `Enums/`** — the `Entity` base, cross-cutting enums, shared error types. If a type is used by two slices, it goes here.
4. **No cross-slice references between child entities** — `CaseTask` never references `User` directly. If a task needs a user, it holds a `Guid UserId` (identity reference), not a `User` object. Cross-aggregate navigation is done through repository lookups, not object pointers.
5. **Repository interfaces belong to the slice** — `ICaseRepository.cs` lives in `Cases/`, not in a global `Repositories/` folder. The interface is owned by the domain; the implementation lives in Infrastructure.

## Aggregate Root Rules

- **One root per slice** — the aggregate root is the only entity that exposes child entities. Children are added only via root methods (`AddDocument`, `AddTask`, etc.).
- **Children never reference siblings directly** — a `CaseTask` does not hold a reference to a `CaseDocument`. They share `CaseId` (the root's identity), not object pointers.
- **Identity is `Guid`** — every entity has `Guid Id`. Cross-aggregate references use `Guid <AggregateName>Id` (e.g. `Guid CaseTypeId`), never a foreign-key navigation property.
- **Consistency boundary** — all invariants are enforced on the root. A child cannot transition to an invalid state without the root's knowledge.

## Settings & Configuration (cross-layer)
- Settings are read in the **outer layer only** (API / host / Infrastructure) — injected as `IOptions<T>` / `IOptionsSnapshot<T>`.
- **Domain and Application never depend on configuration types** — no `IOptions<T>`, no settings records. Values cross into them as plain method arguments / request objects.
- See `ai/agents/Dotnet/SETTINGS.md` for the full typed-settings pattern.

## Package Management (Central Package Management)
- **NuGet versions are centralized** in `Directory.Packages.props` at the repo root (`ManagePackageVersionsCentrally=true`). Do **not** put `Version=` on `PackageReference` entries in `.csproj` files — `dotnet add package` / restore will reject them.
- To add a package: add a `<PackageVersion Include="..." Version="..." />` entry in `Directory.Packages.props`, then a versionless `<PackageReference Include="..." />` in the target `.csproj`. (`dotnet add package` does both automatically when CPM is on.)
- Version the related framework families via the MSBuild variables declared in `Directory.Packages.props` (`EfCoreVersion`, `ExtensionsVersion`, `AspNetCoreVersion`, `TestSdkVersion`, `XunitVersion`, `XunitRunnerVersion`, `CoverletVersion`, `ShouldlyVersion`) — bump one variable to upgrade the whole family.
- Transitive pinning is off (`CentralPackageTransitivePinningEnabled=false`); only directly-referenced packages need a `PackageVersion` entry.

### CPM hygiene
- ✅ Keep `<PackageVersion>` entries alphabetically sorted within the single `ItemGroup`.
- ✅ Bump a package by editing only its `Version` in `Directory.Packages.props` — never in a `.csproj`.
- ✅ When upgrading a framework family (EF Core, ASP.NET Core, xUnit), update every related `PackageVersion` entry in one commit so the family stays coherent.
- ❌ Do not add per-project `Directory.Packages.props` files — exactly one lives at the repo root.

## Dependency Injection & Composition Root
- ✅ Each outer layer exposes **one** DI extension method per concern (`AddApplication()`, `AddInfrastructure()`, `AddPersistence()`) and each is called explicitly in `Program.cs`.
- ✅ Every service has its own interface; register by interface and consume interfaces in constructors.
- ✅ Keep DI registration granular per slice so settings/config stay isolated.
- ❌ Do not register concrete-only service classes without an interface.
- ❌ Do not hide `services.AddXxx(...)` calls inside other extension methods — each host wires them explicitly in `Program.cs`.

## Persistence / EF Core
- ✅ Configure every entity with `IEntityTypeConfiguration<T>` where `T` is the **Domain** entity (`RegulatoryCase`, `CaseTask`, `CaseDocument`, `Decision`, `Comment`, `CaseType`) — one file per entity under `Persistence/Configurations/<Slice>/`.
- ✅ Register configurations via `modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly)` in `OnModelCreating`.
- ✅ Keep a design-time `IDesignTimeDbContextFactory<AppDbContext>` so `dotnet ef` discovers the context without a running host.
- ✅ Guid PKs are application-generated: `builder.Property(x => x.Id).ValueGeneratedNever()`.
- ✅ Constrain columns explicitly: `IsRequired()`, `HasMaxLength(n)` for strings, `HasPrecision(18, 2)` for decimals, `HasConversion<int>()` for enums.
- ✅ Map collection backing fields: `builder.Navigation(x => x.Documents).UsePropertyAccessMode(PropertyAccessMode.Field)`.
- ✅ Apply global query filters for soft-delete patterns (`builder.HasQueryFilter(p => !p.IsDeleted)`).
- ✅ Read-only queries use `AsNoTracking()`; project with `.Select()` when only a subset of columns is needed.
- ✅ Eager-loading multiple collections uses `AsSplitQuery()` to avoid cartesian explosion.
- ✅ Bulk updates/deletes use `ExecuteUpdateAsync` / `ExecuteDeleteAsync`.
- ❌ Do not use synchronous EF methods (`.ToList()`, `.First()`) — always async.
- ❌ Do not hand-write migration files — generate with `dotnet ef migrations add <Name>`.
- ✅ Review every generated migration before applying; roll back an unapplied bad migration with `dotnet ef migrations remove`.
- ✅ `Database.Migrate()` runs at startup to **apply** existing migrations only — never as a generation workflow.
- ❌ Do not expose Domain entities directly in API responses — always map to a DTO at the boundary.

## Serialization & DTO Boundaries
- ✅ Domain and Application models NEVER carry `System.Text.Json.Serialization` attributes (`[JsonPropertyName]`, etc.).
- ✅ DTOs with serialization attributes live in the **Infrastructure** layer.
- ✅ Convert Infrastructure DTOs → Application/Domain models with mapper extension methods (`XxxMapper.MapToModel(dto)`).
- ❌ Do not let serialization concerns leak into the Domain or Application layers.

## HTTP Clients (Infrastructure)
- ✅ Use **typed HTTP clients** (wrapper classes) registered with `AddHttpClient<IXxxClient, XxxClient>` — never inject raw `HttpClient`.
- ✅ Each client has its own DI extension method in the Infrastructure project, called explicitly from `Program.cs`.
- ✅ Use **Polly** policies for all retry / circuit-breaker behavior.
- ❌ Do not implement retry with manual loops, recursion, or custom delay logic.
- ❌ Do not introduce adapter patterns over HTTP clients.
- ✅ **CRITICAL: HTTP client methods THROW on failure — never return `null`.** Let `HttpRequestException` / transient failures propagate and add diagnostic context (URL, status, payload size) to thrown exceptions. Silent `null` returns hide bugs.

## API Layer Conventions
- ✅ The API layer is thin — endpoints delegate to Application services; no business rules in endpoints.
- ✅ Use `WebApplicationBuilder` / `WebApplication` — no legacy `Startup` / `WebHost` pattern.
- ✅ Centralize unhandled exceptions with `AddProblemDetails()`, `AddExceptionHandler<T>()`, `UseExceptionHandler()` — map known exceptions to RFC 7807 problem details.
- ✅ Application/API input validation uses `FluentValidation` (endpoint filters / pipeline); domain validation stays in factory methods returning `ErrorOr<T>`.
- ❌ Do not let HTTP / serialization concerns cross into Application or Domain layers.
- ✅ Health checks: `AddHealthChecks()` + `MapHealthChecks("/health")`, plus `AddDbContextCheck<AppDbContext>()` for DB probes.

## References
- Settings & configuration: `ai/agents/Dotnet/SETTINGS.md`
- Coding style: `ai/agents/Dotnet/CODING_STYLE.md`
- Test conventions: `ai/agents/Dotnet/TESTING.md`
- Project conventions: `AGENTS.md`
