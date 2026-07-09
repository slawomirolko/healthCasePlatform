# AGENTS.md — Infrastructure layer

Caveman-strict. Layer rules only. Shared rules → root `AGENTS.md` + `ai/agents/Dotnet/ARCHITECTURE.md`.

## What lives here

Persistence. `AppDbContext`, `IEntityTypeConfiguration<T>` per entity, EF migrations, `DesignTimeAppDbContextFactory`, `DatabaseSettings`, repo impls (`Persistence/Repositories/`).

## Dependency

→ Domain only (default). Implements Domain-owned `IXxxRepository`. No Application ref unless §8 alt-2 (`ICaseQueryService` server-side projection) adopted — then add `Infrastructure → Application`. Currently NOT added.

## Hard rules

- Repo = `sealed class`, explicit ctor `AppDbContext`. Preserve exact tracked/AsNoTracking/OrderBy from prior endpoint logic. Transitions = tracked `FindByIdAsync`. Reads = `AsNoTracking`.
- Every entity = `IEntityTypeConfiguration<T>` under `Persistence/Configurations/<Slice>/`. Register via `ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly)`. One file per entity.
- Guid PK app-generated: `ValueGeneratedNever()`. Enums `HasConversion<int>()`. Strings `IsRequired().HasMaxLength(n)`. Collection backing fields `UsePropertyAccessMode(Field)`.
- Migrations = `dotnet ef migrations add <Name>`. Never hand-write. Never edit generated. Roll back unapplied = `dotnet ef migrations remove`. Review before apply. `Database.Migrate()` startup = apply only.
- Read-only = `AsNoTracking()`. Bulk = `ExecuteUpdateAsync`/`ExecuteDeleteAsync`. Split query on multi-collection eager. No sync EF methods (`.ToList()`/`.First()`).
- Keep `IDesignTimeDbContextFactory<AppDbContext>` so `dotnet ef` finds context without host.
- No domain logic here. No `ErrorOr`. Repo returns primitives/entities. "Not found" = null return, Application decides.

## DI

`DependencyInjection.AddInfrastructure(IConfiguration)` = one extension, explicit in `Program.cs`. Binds `DatabaseSettings`, `AddDbContext<AppDbContext>` (options lambda reads `IOptions<DatabaseSettings>`), `AddScoped<IXxxRepository, XxxRepository>()`. No `ValidateOnStart()` until `DatabaseSettings` gains a validator/annotation.
