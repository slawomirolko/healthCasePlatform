# Audit Log — SQL vs Mongo

Two adapters behind a single port (`IAuditLogWriter`); config picks one at startup.

## Overview

Audit entries (`AuditEntry`) can be stored in either SQL Server (default) or MongoDB.
Selection is config-driven via `Audit:Provider` (`SqlServer` | `MongoDb`).
Both adapters implement `IAuditLogWriter` (Domain port) and satisfy the same behavioral contract.

## Guaranteed by both (the contract)

The abstract base `AuditLogWriterContractTests` defines 7 obligations run for **each** adapter:

1. All six fields of `AuditEntry` (`Id`, `CaseId`, `Action`, `Actor`, `Detail`, `OccurredAt`) round-trip byte-equal.
2. `GetByCaseIdAsync` ordering = `OccurredAt` asc, then `Id` asc.
3. Unknown case → empty list (not null, not throw).
4. `GetByCaseIdAsync` filters to the requested case only.
5. Every `AuditAction` enum member round-trips (SQL `int` ↔ Mongo `Int32` value parity).
6. `Detail == null` survives (not coerced to empty string).
7. Unicode `Detail` (transition arrow `→`) survives.

## Transactional semantics

### SQL (default — atomic)

`SqlAuditLogWriter.WriteAsync` **stages** the entry on the request's scoped `AppDbContext` (calls `AddAsync`, never `SaveChanges`).
The handler's existing `SaveChangesAsync` call flushes case + history + audit in **one EF transaction**.
Guarantee: if the business op succeeds, the audit is persisted; if it fails, the audit rolls back with it.

Proven by `AuditTransactionTests` (SQL-only — deliberately **not** in the shared contract):
- `SaveChanges_CaseAndAuditEntry_PersistedTogether`
- `SaveChanges_CaseAndAuditEntry_RollBackTogether`
- `SaveChanges_WhenAuditInsertFails_CaseNotPersisted`

### Mongo (best-effort — NOT atomic with case save)

`MongoAuditLogWriter.WriteAsync` calls `InsertOneAsync` **immediately**.
The SQL `SaveChangesAsync` that follows is a separate operation — Mongo cannot enroll in the EF transaction.

**Failure mode:** if the SQL save (case + history) rolls back, the Mongo audit is an **orphan** (already written, cannot be un-written).
This is accepted: audit must never block the business op (Domain quirk), and single-provider Mongo config means there is no SQL audit to disagree with.

## Schema / indexing

| Aspect | SQL | Mongo |
|---|---|---|
| Store | `AuditEntries` table | `auditEntries` collection |
| PK / `_id` | `Guid` (app-generated `Guid.CreateVersion7()`) | `Guid` (`_id`, `NullIdGenerator`) |
| Case link | `CaseId` column + FK→`RegulatoryCases` Cascade | `CaseId` field (no FK) |
| Index | `IX_AuditEntries_CaseId` | Add `CaseId` ascending index if read volume grows |
| `Action` | `int` (`HasConversion<int>()`) | `Int32` (`EnumSerializer<Int32>`) |
| `Detail` | `nvarchar(2000)` | BSON UTF-8 string |

## Query / ordering equivalence

Both order results by `OccurredAt` asc, then `Id` asc.
Both filter by `CaseId`.

## FK / referential

- **SQL:** enforces the case must exist (FK constraint). `SeedCaseAsync` in the SQL contract test inserts the parent row first.
- **Mongo:** no referential constraint. `SeedCaseAsync` in the Mongo contract test is a no-op (returns a random `Guid`).

## When to pick which

- **SQL (default):** when audit must be **atomic** with the business operation. This is the non-breaking default — zero config change required for existing deployments.
- **Mongo:** when audit is a **high-volume append-only** analytics/immutable-log concern and atomicity with the case save is not required. Better write throughput, separate scaling story, no SQL transaction overhead.

## Config knobs

| Setting | Env var | Default | Values |
|---|---|---|---|
| `Audit:Provider` | `Audit__Provider` | `SqlServer` | `SqlServer` \| `MongoDb` |
| `MongoAudit:ConnectionString` | `MongoAudit__ConnectionString` | (env-only) | MongoDB connection URI |
| `MongoAudit:Database` | `MongoAudit__Database` | `HealthCasePlatform` | database name |
| `MongoAudit:CollectionName` | `MongoAudit__CollectionName` | `auditEntries` | collection name |

Connection string is **env-only** (parity with `DatabaseSettings:ConnectionString` — never in `appsettings.json`).

## Test map

| Test | Scope | Location |
|---|---|---|
| `AuditLogWriterContractTests` (×7 [Fact]s + ×3 [Theory]) | Shared behavioral contract | `Infrastructure.Tests.Integration/Persistence/Audit/` |
| `SqlAuditLogWriterContractTests` | SQL diamond | same |
| `MongoAuditLogWriterContractTests` | Mongo diamond | same |
| `AuditTransactionTests` (×3) | SQL-only atomicity | `Infrastructure.Tests.Integration/Persistence/` |
| `AuditEndpointsTests` (×7) | HTTP layer (SQL default) | `Api.Tests.Integration/` |
| `GetCaseAuditQueryHandlerTests` (×2) | Application query handler | `Application.Tests/Cases/Queries/` |
