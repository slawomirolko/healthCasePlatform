# Audit Log — Implementation Plan

Write an audit entry for three events: **case created**, **status changed**, **decision made**.
SQL-only for now (MongoDB sink deferred — design must not block it). Integration tests must
**prove audit entries are created in the same transaction** as the business change.

Scope decision (see "Decisions" below): `DecisionMade` = terminal Approve/Reject transitions
(Option 2). `Decision` entity stays out of scope this round.

---

## 1. Files to change (grouped by directory)

### `src/HealthCasePlatform.Domain/Enums/`
- **CREATE** `AuditAction.cs` — `public enum AuditAction { CaseCreated = 1, StatusChanged = 2, DecisionMade = 3 }`.
  Lives in `Enums/` next to `CaseStatus`/`CasePriority` (consistent home, future cross-aggregate
  when MongoDB audit lands). `HasConversion<int>()` later → adding members is migration-free
  (per root AGENTS.md "Domain quirks").

### `src/HealthCasePlatform.Domain/Cases/`
- **CREATE** `AuditEntry.cs` — `sealed class AuditEntry : Entity` mirroring `CaseStatusHistory`:
  - Props (all `private set`): `Guid CaseId`, `AuditAction Action`, `string Actor`,
    `string? Detail`, `DateTime OccurredAt`.
  - Private parameterless ctor (persistence seam).
  - `public static ErrorOr<AuditEntry> Create(Guid caseId, AuditAction action, string actor, string? detail = null)` —
    guards `caseId != Guid.Empty` → `AuditEntryErrors.CaseIdEmpty`, `!IsNullOrWhiteSpace(actor)` →
    `AuditEntryErrors.ActorEmpty`; on success returns the entry. **`public`** (not `internal`) because the
    sole caller lives in **Application** (`CaseTransitionHelper`/`CreateCaseCommandHandler`) — `HealthCasePlatform.Domain.csproj`
    has **no** `<InternalsVisibleTo>` today, and the existing `internal CaseStatusHistory.Create` works only because
    its caller (`RegulatoryCase.RecordTransition` @ `RegulatoryCase.cs:196`) is inside the same Domain assembly. An
    `internal AuditEntry.Create` would be a **first** cross-assembly internal Domain caller → CS0122 in Application
    *and* in `Domain.Tests`. Returning `ErrorOr` (not throwing) satisfies the Domain rule "No throw for control flow";
    callers `.Value`-unwrap (guards pass — handlers already validated) or guard `result.IsError`.
  - `Id = Guid.CreateVersion7()`, `OccurredAt = DateTime.UtcNow`.
- **CREATE** `AuditEntryErrors.cs` — `CaseIdEmpty` / `ActorEmpty` `public static readonly Error` — **used**
  by `Create` (not dead). Code `AuditEntry.<Name>`, same `<Entity>Errors` style as siblings.

### `src/HealthCasePlatform.Domain/Cases/`
- **EDIT** `ICaseRepository.cs` — add two primitives-only methods (no EF types, per Domain rules):
  - `Task AddAuditEntryAsync(AuditEntry entry, CancellationToken cancellationToken);`
  - `Task<IReadOnlyList<AuditEntry>> GetAuditByCaseIdAsync(Guid caseId, CancellationToken cancellationToken);`

### `src/HealthCasePlatform.Application/Cases/Commands/`
- **EDIT** `CaseTransitionHelper.cs` — extend `TransitionAsync` with optional trailing audit params:
  ```csharp
  internal static async ValueTask<ErrorOr<RegulatoryCase>> TransitionAsync(
      ICaseRepository repository, Guid id,
      Func<RegulatoryCase, ErrorOr<Success>> transition,
      CancellationToken cancellationToken,
      string? actor = null,
      AuditAction? auditAction = null)
  ```
  - Capture `var fromStatus = entity.Status;` BEFORE `transition(entity)`.
  - After successful transition + before `SaveChangesAsync`: if `auditAction is not null`,
    `var toStatus = entity.Status;` then `var audit = AuditEntry.Create(entity.Id, auditAction.Value,
    actor!, $"{fromStatus} → {toStatus}");` → `await repository.AddAuditEntryAsync(audit.Value, ct);`
    (`actor!` non-null here — `auditAction` non-null ⇒ endpoint passed `GetUserId()`; `.Value` safe).
  - Single existing `SaveChangesAsync` flushes case + `CaseStatusHistory` + `AuditEntry` together
    (same EF transaction → atomic).
  - Trailing optional params ⇒ **assignment handlers' call sites compile unchanged** (omit them →
    no audit written; assignments are not "status changes", out of scope).
- **EDIT** `CreateCaseCommandHandler.cs` — between existing `AddAsync(entity)` and `SaveChangesAsync`:
  ```csharp
  var audit = AuditEntry.Create(entity.Id, AuditAction.CaseCreated, command.CreatedBy, entity.Title);
  await _repository.AddAuditEntryAsync(audit.Value, cancellationToken);
  ```
  `Create` returns `ErrorOr<AuditEntry>`; `.Value` is safe (`command.CreatedBy` non-empty — validated by
  `RegulatoryCase.Create` + `CreateCaseRequestValidator`). `command.CreatedBy` IS the actor (no command change for create).
- **EDIT** the 6 status-transition command records — add an **optional** `string? Actor = null`:
  `SubmitCaseCommand`, `StartScientificReviewCommand`, `StartLegalReviewCommand`,
  `RequestDecisionCommand` → `(Guid Id, string? Actor = null)`;
  `ApproveCaseCommand`, `RejectCaseCommand` → `(Guid Id, string? Actor = null)`.
  **Optional, not required** so the 6 existing transition-handler unit tests that build `new XxxCommand(id)`
  (`SubmitCaseCommandHandlerTests.cs:14`, `ApproveCaseCommandHandlerTests.cs:14`, et al.) compile unchanged —
  a required `Actor` would hit CS7036 across all 6 test files. Endpoints always pass `httpContext.User.GetUserId()`;
  a null `Actor` ⇒ helper writes no audit (identical to the assignment path). *Alternative (stricter contract):*
  required `string Actor` + edit each test `Command()` helper to `new(id, "actor")` (6 extra edits, no nullable leak).
  *(Assignment commands `AssignScientificReviewerCommand`/`AssignLegalReviewerCommand` UNCHANGED — out of scope.)*
- **EDIT** the 6 status-transition handlers — pass actor + action to the helper:
  - non-terminal (`Submit`/`StartScientificReview`/`StartLegalReview`/`RequestDecision`) →
    `..., cancellationToken, command.Actor, AuditAction.StatusChanged`
  - terminal (`Approve`/`Reject`) → `..., cancellationToken, command.Actor, AuditAction.DecisionMade`

### `src/HealthCasePlatform.Application/Cases/Queries/`
- **CREATE** `GetCaseAuditQuery.cs` — `sealed record GetCaseAuditQuery(Guid Id) : IQuery<ErrorOr<IReadOnlyList<AuditEntry>>>;`
  (returns `ErrorOr` so a missing case → `Error.NotFound`, mirroring `GetCaseHistoryQuery`'s
  `ExistsAsync` guard pattern).
- **CREATE** `GetCaseAuditQueryHandler.cs` — `sealed class` injecting `ICaseRepository` via explicit
  ctor; `if (!await _repository.ExistsAsync(command.Id, ct)) return CaseErrors.NotFound;` then
  `return ErrorOrFactory.From(await _repository.GetAuditByCaseIdAsync(command.Id, ct));`.

### `src/HealthCasePlatform.Infrastructure/Persistence/`
- **EDIT** `AppDbContext.cs` — add `public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();`.
- **CREATE** `Configurations/Cases/AuditEntryConfiguration.cs` — `IEntityTypeConfiguration<AuditEntry>`:
  - table `AuditEntries`; `Id.ValueGeneratedNever()`; `Action.HasConversion<int>()`;
    `Actor.HasMaxLength(100).IsRequired()`; `Detail.HasMaxLength(2000)`; `OccurredAt.IsRequired()`;
    `HasIndex(x => x.CaseId)`; `HasOne<RegulatoryCase>().WithMany().HasForeignKey(x => x.CaseId)
    .OnDelete(DeleteBehavior.Cascade)` (mirrors `CaseStatusHistoryConfiguration`).
- **EDIT** `Repositories/CaseRepository.cs` — implement the two new methods:
  - `AddAuditEntryAsync` → `_db.AuditEntries.AddAsync(entry, ct);`
  - `GetAuditByCaseIdAsync` → `_db.AuditEntries.AsNoTracking().Where(x => x.CaseId == caseId)
    .OrderBy(x => x.OccurredAt).ThenBy(x => x.Id).ToListAsync(ct);`
- **GENERATE** migration via `dotnet ef migrations add AddAuditLog` (from the Infrastructure project,
  using the existing design-time factory). **Review generated file before applying.** Expected: new
  `AuditEntries` table + `IX_AuditEntries_CaseId` + FK→`RegulatoryCases.Id` Cascade. *No column changes
  to existing tables.*

### `src/HealthCasePlatform.Api/Common/`
- **CREATE** `UserExtensions.cs` — `public static string GetUserId(this ClaimsPrincipal user) =>
  user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name ?? "unknown";`
  Reads the claim that `FakeAuthHandler` sets (`test-user` default / `X-User` header value).

### `src/HealthCasePlatform.Api/Cases/`
- **CREATE** `AuditEntryResponse.cs` — `sealed record AuditEntryResponse(Guid Id, Guid CaseId,
  string Action, string Actor, string? Detail, DateTime OccurredAt);` (DTO at boundary — entity→DTO
  map inline in endpoint, per Api rules).
- **EDIT** `CasesEndpoints.cs`:
  - **New route** (after the `/history` route, line ~68):
    ```csharp
    group.MapGet("/cases/{id:guid}/audit", GetCaseAudit)
        .WithName("GetCaseAudit")
        .RequireAuthorization(b => b.RequireRole(AppRoles.Auditor, AppRoles.TeamLeader));
    ```
    `GetCaseAudit` handler: `mediator.Send(new GetCaseAuditQuery(id))`; on error →
    `TypedResults.NotFound()`; else map `AuditEntry`→`AuditEntryResponse`
    (`action.ToString()`), return `Ok<IReadOnlyList<AuditEntryResponse>>`. Mirrors `GetCaseHistory`.
  - **Thread actor** into the 6 status-transition endpoints: each handler gains an `HttpContext httpContext`
    param (minimal-API-injectable) and builds the command with `httpContext.User.GetUserId()`:
    - `new SubmitCaseCommand(id, httpContext.User.GetUserId())` (and 5 siblings).
    - Assignment endpoints + Create endpoint **unchanged** (Create already sends `CreatedBy`; assignments out of scope).

### No new packages
`Directory.Packages.props` needs **no** edit — everything uses already-referenced
(EF Core, Mediator, ErrorOr, xUnit, Shouldly, NSubstitute, Testcontainers). MongoDB driver is
**intentionally not added** this round.

---

## 2. Application flow

**Create** (`POST /cases`):
```
endpoint (CaseOfficer) → CreateCaseCommand(createdBy from body)
  → CreateCaseCommandHandler.Handle:
      RegulatoryCase.Create(...) → [domain errors? return]
      repo.AddAsync(entity)
      repo.AddAuditEntryAsync(AuditEntry.Create(id, CaseCreated, createdBy, title))   ← NEW
      repo.SaveChangesAsync()   ← case + audit in ONE EF transaction
  → 201 + CreateCaseResponse
```

**Status transition** (`POST /cases/{id}/submission` etc.):
```
endpoint (role-gated) reads httpContext.User.GetUserId()
  → XxxCommand(id, actor)
  → XxxHandler.Handle → CaseTransitionHelper.TransitionAsync(..., actor, StatusChanged|DecisionMade):
      entity = repo.FindByIdAsync(id) → [null? NotFound]
      fromStatus = entity.Status
      result = transition(entity)                    ← domain mutates Status, appends CaseStatusHistory
      [result.IsError? return errors]
      toStatus = entity.Status
      if auditAction: repo.AddAuditEntryAsync(AuditEntry.Create(id, action, actor, "from → to"))  ← NEW
      repo.SaveChangesAsync()                        ← case + CaseStatusHistory + audit in ONE EF transaction
  → 200 / 404 / 409 (unchanged MapTransition)
```

**Read audit** (`GET /cases/{id}/audit`, Auditor|TeamLeader):
```
endpoint → GetCaseAuditQuery(id)
  → GetCaseAuditQueryHandler: repo.ExistsAsync? no→NotFound ; else repo.GetAuditByCaseIdAsync
  → 200 IReadOnlyList<AuditEntryResponse> (ordered OccurredAt, Id) | 404
```

---

## 3. Design patterns

- **Aggregate-child audit (same as `CaseStatusHistory`)** — audit rows are first-class Domain entities
  in the `Cases/` slice, added to the change tracker before a single `SaveChangesAsync`. EF Core wraps
  every `SaveChangesAsync` in an **implicit transaction** → case + history + audit commit/rollback
  together with **zero explicit transaction code** (the repo has none today and needs none).
- **Single Responsibility via `internal static` helper** — audit write centralized in
  `CaseTransitionHelper` (one seam for all transitions), not scattered across handlers.
- **Explicit data over ambient context** — `Actor` travels on the command record (house style:
  "values cross into Application as plain arguments"; mirrors existing `CreatedBy` on
  `CreateCaseCommand`). **No `IHttpContextAccessor`/`ICurrentUser` service introduced.**
- **Optional trailing params (Null Object)** — `actor`/`auditAction` default null ⇒ assignment
  handlers reuse the helper unchanged without writing spurious audits.
- **DTO at boundary** — `AuditEntry` never serialized directly; endpoint maps to `AuditEntryResponse`
  (`Action` as `string`).

---

## 4. Same-transaction strategy (the user's core requirement)

EF Core's default: **a single `SaveChangesAsync` executes inside one DB transaction**; any failure
rolls back **all** tracked changes in that call. Both new write points stage the `AuditEntry` on the
same tracked `AppDbContext` immediately before the existing `SaveChangesAsync` — so atomicity is
inherent, **no `IDbContextTransaction`/`IUnitOfWork` introduced** (would be the repo's first; rejected
as YAGNI). Tests below prove the guarantee in three modes: commit-on-success, none-on-logical-failure,
rollback-together-on-DB-failure.

---

## 5. Tests

### Unit tests — `tests/HealthCasePlatform.Application.Tests/Cases/Commands/`
(Mock `ICaseRepository` with NSubstitute, real `RegulatoryCase` domain, Shouldly. `InternalsVisibleTo`
already exposes `CaseErrors`/`CaseTransitionHelper`.)

- `CreateCaseCommandHandlerTests.Handle_OnSuccess_StagesCaseCreatedAuditEntry` —
  assert `repo.Received(1).AddAuditEntryAsync(
  Arg.Is<AuditEntry>(a => a.Action == AuditAction.CaseCreated && a.Actor == command.CreatedBy), _)`
  **before** `SaveChangesAsync` was awaited.
- **EXTEND existing** `CreateCaseCommandHandlerTests.Handle_WhenDomainCreateFails_DoesNotPersist`
  (`CreateCaseCommandHandlerTests.cs:48`) — same Arrange + execution path as a separate sibling would use;
  add `await repo.DidNotReceive().AddAuditEntryAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())`
  to its existing `AddAsync`/`SaveChangesAsync`-never-called assertions. Do **not** add a `..._DoesNotStageAudit`
  sibling (same Arrange ⇒ merge, per test-reuse rule).
- `CaseTransitionHelperTests.Transition_Submit_StagesStatusChangedAuditEntry` — seeded tracked case,
  call `TransitionAsync(..., actor, StatusChanged)`; assert audit `Action==StatusChanged`,
  `Detail == "Draft → Submitted"`, `Actor == actor`, exactly **one** `AddAuditEntryAsync` call and it
  precedes `SaveChangesAsync` (use `Received.InOrder`).
- `CaseTransitionHelperTests.Transition_Approve_StagesDecisionMadeAuditEntry` — pending-decision case,
  `AuditAction.DecisionMade`, `Detail == "PendingDecision → Approved"`.
- `CaseTransitionHelperTests.Transition_WithNullActor_WritesNoAudit` — assert `AddAuditEntryAsync`
  **never** called (assignment-handler path). **New test required** — distinct Arrange (no actor) from any handler test.
- `CaseTransitionHelperTests.Transition_WhenDomainFails_WritesNoAudit` — Submit a non-draft case →
  domain returns conflict → assert `AddAuditEntryAsync` and `SaveChangesAsync` never called. **New test required** —
  verifies the helper's guard ordering, not a handler behavior. (The happy-path `Transition_Submit_StagesStatusChangedAuditEntry`
  / `Transition_Approve_StagesDecisionMadeAuditEntry` cases below share Arrange with the existing handler tests —
  prefer folding the audit assertion into `SubmitCaseCommandHandlerTests.Handle_WhenCaseIsDraft_ReturnsSubmittedCase`
  / `ApproveCaseCommandHandlerTests.Handle_WhenCaseIsPendingDecision_ReturnsApprovedCase` as one extra
  `Received(1).AddAuditEntryAsync(...)` line rather than duplicating the Arrange in a helper-only test class.)

### Unit tests — `tests/HealthCasePlatform.Domain.Tests/Cases/`
- `AuditEntryTests.Create_WhenValid_ReturnsEntryWithExpectedFields`
- `AuditEntryTests.Create_WhenCaseIdEmpty_ReturnsCaseIdEmptyError` (assert `result.IsError` +
  `result.Errors.ShouldContain(AuditEntryErrors.CaseIdEmpty)` — **not** a throw; `Create` returns `ErrorOr`)
- `AuditEntryTests.Create_WhenActorEmpty_ReturnsActorEmptyError`

### Integration tests — `tests/HealthCasePlatform.Api.Tests.Integration/` (`IClassFixture<ApiFactory>`, real Testcontainers SQL)
**NEW** `AuditEndpointsTests.cs`:
- `CreateCase_OnSuccess_WritesCaseCreatedAuditEntry` — POST /cases (CaseOfficer client), then
  GET /cases/{id}/audit (Auditor client) → 1 entry, `Action == "CaseCreated"`, `Actor == CreatedBy`.
- `SubmitCase_OnSuccess_WritesStatusChangedAuditEntry` — create + submit (CaseOfficer), audit has
  `StatusChanged` entry with `Detail == "Draft → Submitted"`.
- `ApproveCase_OnSuccess_WritesDecisionMadeAuditEntry` — drive to PendingDecision then approve
  (TeamLeader); audit has `DecisionMade` entry with `Detail.Contains("Approved")`.
- `Workflow_AccumulatesAuditEntriesInOrder` — full track Draft→…→Approved; assert audit count and
  `OccurredAt` ascending (reuse `CaseTestSeeder.BringCaseToStateAsync`).
- `Transition_WhenDomainFails_WritesNoAuditEntry` — Submit then Submit again (2nd → 409); assert audit
  count unchanged (still 1, not 2). *Logical-failure path.*
- `GetAudit_WhenCaseMissing_Returns404`.
- `GetAudit_AsAuditorRole_Returns200`; `GetAudit_AsUnauthorizedRole_Returns403`
  (e.g. CaseOfficer-only client).

### Integration tests — `tests/HealthCasePlatform.Infrastructure.Tests.Integration/Persistence/` (`IClassFixture<DbFixture>`, real Testcontainers SQL, no web host)
**THE atomicity proof.** **NEW** `AuditTransactionTests.cs`:
- `SaveChanges_CaseAndAuditEntry_PersistedTogether` — on one context: `Add` a `RegulatoryCase` +
  `Add` an `AuditEntry`; `SaveChangesAsync`; reload from a **fresh** context → both rows present.
- `SaveChanges_CaseAndAuditEntry_RollBackTogether` — **the same-transaction proof**: open
  `await db.Database.BeginTransactionAsync()`, add case + audit, `SaveChangesAsync` (enlists in the
  ambient tx, does NOT commit), then `tx.RollbackAsync()`; reload from a fresh context → **neither**
  case **nor** audit row present. Demonstrates they share one transaction boundary.
- `SaveChanges_WhenAuditInsertFails_CaseNotPersisted` — deterministic poison-pill variant: stage a
  valid case + a valid audit, then also `_db.Entry(audit2).State = ...` add a second audit whose
  `Actor` is 200 chars (exceeds `HasMaxLength(100)`) so `SaveChangesAsync` throws `DbUpdateException`;
  catch it; fresh context → **case absent AND no audit row** (EF's implicit tx rolled back the whole
  batch). *This is the strongest "same transaction" assertion — proves the production single-`SaveChanges`
  path is atomic even when only one entity in the batch fails.* **Construction note:** `AuditEntry` props are
  `private set` + private ctor (mirrors `CaseStatusHistory`), so the invalid entry cannot be built via
  `new`/initializer. Build a valid entry via `AuditEntry.Create(...).Value`, `Attach` it, then mutate through
  the change tracker: `_db.Entry(audit).Property(x => x.Actor).CurrentValue = new string('x', 200);` (EF sets
  private setters via metadata reflection). No existing `AppDbContextTests.cs` test uses this trick — first use.

(All integration tests: Docker engine running, 20s per-test timeout, Shouldly assertions, no mocks,
no SQLite/in-memory — per TESTING.md.)

---

## 6. Decisions (answering "which option makes most business/architectural sense")

Three readings of "decision made" were weighed:
- **Option 1 — add a decision-record command+endpoint** using the dead-code `Decision` entity +
  `RecordDecision()`. **Business:** richest (captures decision text). **Architecture:** largest new
  surface (command+handler+validator+endpoint), speculative (team never wired `Decision`; gives a
  home to dead code on a hunch). **Rejected for this round.**
- **Option 2 — audit Approve/Reject as `DecisionMade`** (chosen). **Business:** Approve/Reject are
  the live, authorized, fully-tested terminal decisions — the genuine "decision made" moments.
  **Architecture:** one seam (`CaseTransitionHelper`), reuses live TeamLeader endpoints, enum is
  migration-free to extend. **Chosen.**
- **Option 3 — both.** Two "decision" events per action = audit noise; `Decision.MarkFinal` vs
  `Approve()` modeling clash. **Rejected.**

`AuditAction.DecisionMade` is reserved so Option 1 folds in later with **zero schema change** (enum
is `HasConversion<int>()`, per Domain quirks) when a real decision-record endpoint is wanted.

---

## 7. Suggestions / alternatives (pros & cons)

1. **`AuditEntry.Create` returns `ErrorOr<AuditEntry>` (chosen), handler discards the error path.**
   Resolved in favor of `ErrorOr` (over an earlier throw-on-bad-input draft) for two reasons found during
   plan review: (a) the Domain rule is "No throw for control flow" — throwing `InvalidOperationException`
   from a factory violates it; (b) `ErrorOr` keeps `AuditEntryErrors.cs` alive (the throw draft left those
   `Error` fields dead code). The handler `.Value`-unwraps because audit must never block the business op and
   is only called from handlers that already validated — this **discard-the-error-path** behavior is the inverse
   of every other Domain factory (which propagates errors); see `## AGENTS.md Updates` below.
2. **Audit read endpoint authorization**: `RequireRole(Auditor, TeamLeader)`. *Con:* CaseOfficer/
   reviewers can't read their own case's audit. *Alternative:* bare `.RequireAuthorization()` (any
   authenticated) like `/history`. Pick per whether audit is sensitive in your domain.
3. **FK cascade vs restrict on `AuditEntries.CaseId`.** Chose Cascade (mirrors `CaseStatusHistory`,
   and there's no case-deletion path today). *Risk:* if hard-delete lands later, audit rows vanish.
   *Alternative:* `DeleteBehavior.Restrict` + keep audit after deletion (classic audit-log semantics).
   Revisit when soft/hard delete is introduced.
4. **MongoDB-later readiness.** SQL entity is intentionally plain (no serialization attributes, DTO at
   boundary). When MongoDB sink lands: add an outbox/interceptor that also publishes an audit event, or
   a `SaveChanges` override that mirrors entries to Mongo. Current design blocks nothing — the
   `AuditAction` enum + `AuditEntry` shape carry straight over.
5. **Explicit `IDbContextTransaction` / `IUnitOfWork`.** **Not introduced** — would be the repo's first,
   and EF's implicit per-`SaveChanges` transaction already guarantees atomicity. Add only if a single
   handler ever needs to span **two** `SaveChangesAsync` calls (none today).
6. **`ICurrentUser` service vs `Actor` on commands.** Chose `Actor` on commands (explicit, testable,
   matches `CreatedBy` precedent). *Alternative:* inject `ICurrentUser` (wraps `IHttpContextAccessor`)
   so handlers' signatures stay stable. *Con:* ambient context, harder to unit-test, a new
   abstraction + wiring. Not worth it for 6 commands.

---

## 8. Structural impact

- **New DB table** `AuditEntries` (+ migration `AddAuditLog`) — only schema change.
- **6 transition command records** gain an **optional** `Actor` parameter → their **endpoint call sites** change
  (add `HttpContext httpContext` param + `httpContext.User.GetUserId()`). Existing transition-handler unit tests
  compile unchanged (optional param). HTTP contract (routes, status codes, request/response bodies) **unchanged** —
  `Actor` is sourced server-side from claims, never from the request body.
- **1 new read route** `GET /cases/{id}/audit`.
- `ICaseRepository` gains 2 methods → `CaseRepository` + unit-test mocks updated.
- No layer-dependency changes (Domain stays the leaf; Application still → Domain only; Infrastructure
  implements the Domain-owned interface). No new project, no new package.

---

## 9. Verify

- `dotnet build`
- `dotnet test` (Docker engine running for `*.Tests.Integration`)
- `dotnet format --verify-no-changes --no-restore`
- Manual: `dotnet ef migrations script` — eyeball the `AddAuditLog` script before applying.

---

## AGENTS.md Updates

Apply **after** the build lands (only the non-inferable quirk; everything else is code-inferable):

- **Domain `AGENTS.md`** — add one line under *Hard rules* / domain-factory notes:
  > `AuditEntry.Create` returns `ErrorOr<AuditEntry>` like every Domain factory, **but its Application
  > callers discard the error path** (`.Value`-unwrap) — audit must never block the business op. This is the
  > inverse of every other Domain factory (which propagates errors). Do not "fix" the discard into a
  > propagation; it is intentional.

(No slice/layer AGENTS.md additions for the write path, the `/audit` route, the `Actor` command param, the
migration, or the test locations — all inferable from code.)
