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
- `IAuditLogWriter` has two adapters: `SqlAuditLogWriter` (Scoped, shares the request `AppDbContext`, stages-only — never `SaveChanges`) and `MongoAuditLogWriter` (Scoped, `IMongoClient` Singleton). Provider switch lives in `AddInfrastructure` (bind `AuditSettings`, branch on `Provider`). `AuditEntry` mapped to Mongo via `BsonClassMap` — **explicitly `MapMember` every field**: `AutoMap()` skips the private setters (`AuditEntry.cs:9-13`) and the `protected set Id` (`Entity.cs:5`); only the explicit maps persist (`CaseId`/`Actor`/`Detail`/`OccurredAt` would be silently dropped otherwise). `EnumSerializer<Int32>` for `Action`. No `[Bson*]` attributes on the Domain entity. If private-setter reflection breaks on a driver bump, fall back to `internal AuditEntry.Hydrate` + `InternalsVisibleTo` Infrastructure.
- **First `IHostedService` registrations** (`OutboxDispatcher` + `CaseSubmittedConsumer`) live in `AddInfrastructure`. They resolve `IConnectionFactory` (Singleton, **never** `IConnection` directly — `CreateConnectionAsync` blocks/throws when the broker is down) and open their own connection inside a retry-tolerant `ExecuteAsync` (`PeriodicTimer`/bounded retry, **never let an exception escape** — .NET's default `BackgroundServiceExceptionBehavior = StopHost` would crash the whole API on broker outage). RabbitMQ.Client 7.x is fully async (`CreateConnectionAsync`/`CreateChannelAsync`/`AsyncEventingBasicConsumer`). `RabbitMqHealthCheck` is registered in `AddInfrastructure` (not Program.cs) because the type is `internal sealed`. **`INotificationWriter` registration moved out of the SQL-audit `else`-branch** to always-registered — the consumer needs it regardless of `Audit:Provider` (also fixes a latent bug: under `Audit:Provider=MongoDb` it was previously unregistered). `RabbitMqSettings` + `AddOptions<...>().ValidateOnStart()` registered here.

## DI

`DependencyInjection.AddInfrastructure(IConfiguration)` = one extension, explicit in `Program.cs`. Binds `DatabaseSettings`, `AddDbContext<AppDbContext>` (options lambda reads `IOptions<DatabaseSettings>`), `AddScoped<IXxxRepository, XxxRepository>()`. No `ValidateOnStart()` until `DatabaseSettings` gains a validator/annotation.
