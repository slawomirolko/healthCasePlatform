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

## References
- Settings & configuration: `ai/agents/Dotnet/SETTINGS.md`
- Coding style: `ai/agents/Dotnet/CODING_STYLE.md`
- Test conventions: `ai/agents/Dotnet/TESTING.md`
- Project conventions: `AGENTS.md`
