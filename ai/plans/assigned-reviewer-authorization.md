# Plan — Assigned-reviewer authorization (+ TeamLeader override) — Variant A: resource-based authorization

Goal: only the reviewer **assigned to a case's review track** can start that
review. TeamLeader can override (act regardless of assignment). Add a dedicated
assignment endpoint and integration tests for assigned vs unassigned reviewers.

> **Revision:** the earlier draft put the check in the domain (a Conflict error
> → **409**). That was the wrong layer for an *authorization* rule: it returned
> 409 for a forbidden action and threaded identity into the pure state machine.
> This variant (**A — resource-based authorization in the API layer**) returns
> the correct **403**, keeps the domain a pure state machine, and leaves the
> review commands/handlers **completely untouched**.

## Confirmed design choice — Variant A (resource-based authorization)
- **Authorization is an API-layer concern**, not a domain invariant. The state
  transition `Submitted → UnderScientificReview` is valid regardless of caller;
  "who may trigger it" is access control. ASP.NET Core has a purpose-built
  pattern for data-dependent access decisions: **resource-based authorization**
  (`IAuthorizationService.AuthorizeAsync(user, resource, requirement)` + a custom
  `IAuthorizationHandler`). It is the idiomatic, 403-returning extension of the
  role checks already on every endpoint.
- The **domain only stores data**: two nullable `AssignedXReviewerId` columns +
  assignment methods that record a reviewer. It enforces **no** access rule.
- Net effect: correct **403** semantics, domain purity preserved, and the review
  command/handler/domain-method signatures are **unchanged** (smallest blast
  radius of any variant).

## Context (what is true now)
- No "assigned reviewer" concept exists. `RegulatoryCase` has only `CreatedBy`.
  Review transitions are parameter-less state machines (`RegulatoryCase.cs:89-102`).
- Auth is **role-only at the HTTP layer** via `RequireRole` on each endpoint
  (`CasesEndpoints.cs`). No per-user identity: `FakeAuthHandler` hard-codes
  `ClaimTypes.Name = "test-user"`; only `X-Roles` is header-driven.
- `MapTransition` (`CasesEndpoints.cs:228`) is the single transition error
  channel: `NotFound` → 404, else → 409. There is **no** 403 path through it —
  so a domain-layer assignment check could only ever yield 409 (the flaw we fix
  by moving the decision to the API authz layer, which can return 403).
- `GetCaseQuery` already exists and returns `RegulatoryCase?` (read-only) —
  reused to load the entity for the resource-based check.
- Default integration principal carries **all roles** (incl. TeamLeader) when no
  `X-Roles` header is sent → `CasesEndpointsTests` (bare `factory.CreateClient()`)
  acts as TeamLeader today. Load-bearing for backward-compat.
- **Critical wrinkle:** the review endpoints currently gate on
  `.RequireRole(ScientificReviewer)` / `RequireRole(LegalReviewer)` alone. A
  TeamLeader is NOT in that set → would be 403'd at the gate before any
  assignment/override logic runs. So the override path **requires widening** the
  role gate to `{ScientificReviewer, TeamLeader}` (and legal) — see Design
  decision 3. (The earlier draft missed this and its "TeamLeader override" would
  have been unreachable.)

## Design decisions
1. **One `IAuthorizationHandler`, two marker requirements, no enum.**
   `ReviewScientificRequirement` / `ReviewLegalRequirement` (`IAuthorizationRequirement`
   markers). `ReviewCaseAuthorizationHandler : AuthorizationHandler` switches on
   the requirement type, reads the case from `context.Resource`, and
   `Succeed`s when `User.IsInRole(TeamLeader) || AssignedXReviewerId == userId`.
   No `ReviewTrack` enum, no JSON binding concerns.
2. **Resource-based check runs in an endpoint filter**, not the handler — to
   honor the Api hard rule "handler injects `IMediator` only." A reusable
   `ReviewAuthorizationFilter` (parameterized by requirement) loads the case via
   `GetCaseQuery`, calls `AuthorizeAsync(user, entity, requirement)`, and returns
   `TypedResults.Forbidden()` (403) or `TypedResults.NotFound()` (unknown case)
   before the handler runs. This parallels the existing `ValidationFilter`
   (filter = cross-cutting endpoint concern).
3. **Widen the review role gates** to `RequireRole(ScientificReviewer, TeamLeader)`
   and `RequireRole(LegalReviewer, TeamLeader)`. The coarse gate still 403s
   unrelated roles (Auditor, CaseOfficer) **without a DB hit** and lets TeamLeader
   reach the resource-based check. The fine-grained decision (assigned vs override)
   lives in the requirement handler — the single source of truth.
4. **404 before 403 for unknown cases.** The filter loads the case first: missing
   → 404 (don't leak existence), present-but-unauthorized → 403. Matches the
   existing `*_WhenCaseUnknown_Returns404` behavior; the 404 source moves from
   `MapTransition` to the filter, behavior identical.
5. **Domain is pure.** Review methods stay parameter-less. Assignment methods only
   validate + set the string prop. The "who may assign" rule is the TeamLeader
   role gate on the assign endpoint (coarse, role-only — no resource check needed
   there). No identity/role strings enter Domain or Application.
6. **Accept one extra read per review action.** Resource-based authz needs the
   entity before the transition command runs, so the filter's `GetCaseQuery`
   (AsNoTracking) + the command's tracked `FindByIdAsync` = 2 round trips. For a
   human-initiated review action this is negligible. (Alternative: stash the
   loaded entity in `HttpContext.Items` for the handler — rejected; would push
   request-state coupling into the handler and muddy the command contract.)
7. **`ChangeStatus` escape hatch unchanged.** Only the two HTTP review endpoints
   are gated. `RegulatoryCase.ChangeStatus` (`RegulatoryCase.cs:110`) remains an
   unrestricted backdoor — consistent with the existing root-`AGENTS.md` quirk.
   Variant A is honest about this: API-layer authz guards HTTP entry points only;
   if universal enforcement (background jobs, non-HTTP callers) is ever needed,
   THEN reconsider a domain check (§Alternatives).

## Files to change

### Domain — `src/HealthCasePlatform.Domain/`  (pure data only)
- **EDIT** `Cases/RegulatoryCase.cs`
  - Add `public string? AssignedScientificReviewerId { get; private set; }` and
    `public string? AssignedLegalReviewerId { get; private set; }`.
  - Add `public ErrorOr<Success> AssignScientificReviewer(string reviewerId)` and
    `AssignLegalReviewer(string reviewerId)`: empty/whitespace → `ReviewerIdEmpty`;
    else set prop + `UpdatedAt = DateTime.UtcNow`. Reassignment allowed.
  - `StartScientificReview`/`StartLegalReview`/`Submit`/`RequestDecision`/
    `Approve`/`Reject` — **unchanged**.
- **EDIT** `Cases/RegulatoryCaseErrors.cs` — add `ReviewerIdEmpty`
  (`Error.Validation("RegulatoryCase.ReviewerIdEmpty", …)`). **No** Conflict
  errors (unauthorized review is no longer a domain outcome).

### Application — `src/HealthCasePlatform.Application/`  (review flow untouched)
- **NEW** `Cases/Commands/AssignScientificReviewerCommand.cs`
  (`sealed record (Guid Id, string ReviewerId) : ICommand<ErrorOr<RegulatoryCase>>`).
- **NEW** `Cases/Commands/AssignScientificReviewerCommandHandler.cs` —
  delegate to `CaseTransitionHelper.TransitionAsync(_repository, command.Id,
  c => c.AssignScientificReviewer(command.ReviewerId), cancellationToken)` (mirrors
  `StartScientificReviewCommandHandler.cs:17`, NOT `CreateCaseCommandHandler`).
  The domain method already returns `ErrorOr<Success>` — the exact shape the helper
  expects (`CaseTransitionHelper.cs:8-12`) — so the NotFound/Save orchestration is
  reused, not duplicated. One-liner handler body.
- **NEW** `Cases/Commands/AssignLegalReviewerCommand.cs` + `…Handler.cs` — symmetric
  (same `CaseTransitionHelper` delegation with `AssignLegalReviewer`).
- Review commands/handlers (`StartScientificReview*`, `StartLegalReview*`):
  **zero changes**.

### Api — `src/HealthCasePlatform.Api/`  (authorization lives here)
- **NEW** `Common/Authorization/ReviewRequirements.cs` — marker requirements
  `ReviewScientificRequirement : IAuthorizationRequirement` and
  `ReviewLegalRequirement : IAuthorizationRequirement`.
- **NEW** `Common/Authorization/ReviewCaseAuthorizationHandler.cs` — derives from the
  **non-generic** `AuthorizationHandler` base (which dispatches every requirement to
  `HandleRequirementAsync`) OR implements `IAuthorizationHandler` directly, so ONE class
  can evaluate both marker requirements by switching on the requirement type. Do **not**
  derive from `AuthorizationHandler<ReviewScientificRequirement>` — that generic base
  handles only one requirement type. Casts `context.Resource as RegulatoryCase`; resolves
  `userId` from `ClaimTypes.NameIdentifier` (fallback `Identity?.Name`); `Succeed`s when
  `User.IsInRole(AppRoles.TeamLeader)` or the matching `AssignedXReviewerId == userId`.
  (Registered singleton.)
- **NEW** `Common/Authorization/ReviewAuthorizationFilter.cs` — reusable
  `IEndpointFilter` (or factory delegate) parameterized by requirement: resolves
  `IMediator` + `IAuthorizationService` from `HttpContext.RequestServices`; reads
  route `id`; `GetCaseQuery` → null returns `TypedResults.NotFound()`; else
  `AuthorizeAsync(user, entity, requirement)` → fail returns
  `TypedResults.Forbidden()`; success → `next(ctx)`.
- **NEW** `Cases/AssignReviewerRequest.cs` — `sealed record AssignReviewerRequest(string ReviewerId)`.
- **NEW** `Cases/AssignReviewerRequestValidator.cs` — FluentValidation: `ReviewerId`
  `.NotEmpty().MaximumLength(100)` (superset of the domain guard, `ValidationFilter` →
  400 first; mirrors `CreateCaseRequestValidator.cs:19-21` `CreatedBy`). The
  `.MaximumLength(100)` is **required** to match the `nvarchar(100)` column
  (`RegulatoryCaseConfiguration` edit below) — without it, a >100-char `reviewerId`
  passes the filter + domain (whitespace check only) and EF throws `DbUpdateException`
  (truncation) → `GlobalExceptionHandler` → **500**.
- **EDIT** `Cases/CasesEndpoints.cs`
  - Widen the two review gates:
    `.RequireAuthorization(b => b.RequireRole(AppRoles.ScientificReviewer, AppRoles.TeamLeader))`
    and the legal equivalent. Add `.AddEndpointFilter(ReviewAuthorizationFilter.For(<requirement>))`
    to each.
  - Add `POST /cases/{id:guid}/assignment/scientific` → `AssignScientificReviewer`
    + `POST /cases/{id:guid}/assignment/legal` → `AssignLegalReviewer`, each
    `.AddEndpointFilter<ValidationFilter<AssignReviewerRequest>>()`
    `.RequireAuthorization(b => b.RequireRole(AppRoles.TeamLeader))`. Handlers
    build the command, `await mediator.Send`, reuse `MapTransition`
    (NotFound → 404, Validation → 409, ok → 200 + `ToCaseResponse`).
  - Review handlers themselves (`StartScientificReview`/`StartLegalReview`)
    stay `IMediator`-only — **no signature change**.
- **EDIT** `Program.cs` — register the handler:
  `builder.Services.AddSingleton<IAuthorizationHandler, ReviewCaseAuthorizationHandler>();`
  (`IAuthorizationService` already provided by existing `AddAuthorization()`; no
  new package.)

### Infrastructure — `src/HealthCasePlatform.Infrastructure/`
- **EDIT** `Persistence/Configurations/Cases/RegulatoryCaseConfiguration.cs` —
  add `builder.Property(c => c.AssignedScientificReviewerId).HasMaxLength(100);`
  and `…AssignedLegalReviewerId…` (nullable ⇒ no `IsRequired()`).
- **NEW** migration — generate via
  `dotnet ef migrations add AddReviewerAssignments --project src/HealthCasePlatform.Infrastructure --startup-project src/HealthCasePlatform.Api`
  (use the `dotnet-migration` skill). **Never hand-write.** Review the generated
  `Up`/`Down` before applying. Two nullable `nvarchar(100)` columns on
  `RegulatoryCases`; applied by the startup `MigrateAsync` block.

### Tests — unit
- **EDIT** `tests/HealthCasePlatform.Domain.Tests/Cases/RegulatoryCaseTests.cs` —
  add assignment tests (§Tests). `BringCaseTo` needs **no** change (review methods
  unchanged).
- **NEW** `tests/HealthCasePlatform.Application.Tests/Cases/Commands/AssignScientificReviewerCommandHandlerTests.cs`
  + `AssignLegalReviewerCommandHandlerTests.cs`.
- Existing `StartScientificReviewCommandHandlerTests` /
  `StartLegalReviewCommandHandlerTests` / `CaseHandlerTestBase` — **zero changes**
  (review command/handler/domain signatures unchanged).

### Tests — integration (`tests/HealthCasePlatform.Api.Tests.Integration/`)
- **EDIT** `FakeAuthHandler.cs` — read `X-User` header; absent ⇒ `"test-user"`
  (preserves every existing test). Set `ClaimTypes.NameIdentifier` (and keep
  `ClaimTypes.Name`) to that value, so the authz handler resolves a stable id.
- **EDIT** `ApiFactory.cs` — add
  `public HttpClient CreateClientAs(string userId, params string[] roles)` that
  sets both `X-User` and `X-Roles`. Leave `CreateClientWithRoles` as-is.
- **NEW** `AssignmentAuthorizationTests.cs` — see §Tests (the user's core ask).
- **EDIT** `WorkflowAuthorizationTests.cs` —
  `StartScientificReview_WithScientificReviewerRole_Returns200` and
  `StartLegalReview_WithLegalReviewerRole_Returns200` now hit the resource-based
  check (caller "test-user" is not assigned, not TeamLeader ⇒ **403**). Seed the
  assignment first (TeamLeader client assigns reviewerId = the acting client's id)
  so they stay 200. The `*_Returns403` (wrong-role) tests stay valid unchanged.

## Application flow
```
Scientific review — assigned reviewer:
  Client (X-User: "sci-1", X-Roles: ScientificReviewer)
    → UseAuthentication (FakeAuthHandler → NameIdentifier="sci-1", role=ScientificReviewer)
    → UseAuthorization  (RequireRole(ScientificReviewer, TeamLeader) ✓ — Auditor/CaseOfficer ⇒ 403 here, no DB)
    → ReviewAuthorizationFilter:
        GetCaseQuery(id) → case  (null ⇒ 404)
        authz.AuthorizeAsync(user, case, ReviewScientificRequirement):
          IsTeamLeader? no  →  AssignedScientificReviewerId == "sci-1"? yes ⇒ Succeed
        ⇒ proceed
    → ValidationFilter (n/a)
    → handler: mediator.Send(StartScientificReviewCommand(id))  ← UNCHANGED
    → case.StartScientificReview()  ← UNCHANGED pure transition
    → MapTransition → 200

Unassigned reviewer:  … AssignedScientificReviewerId == "sci-1"? no, not TeamLeader
  ⇒ AuthorizationHandlerContext stays un-succeeded ⇒ filter returns TypedResults.Forbidden() ⇒ 403

TeamLeader override (X-User: "chief", X-Roles: TeamLeader):
  role gate ✓ (TeamLeader in set) → filter → IsTeamLeader ⇒ Succeed ⇒ 200

Assign: TeamLeader POSTs {reviewerId:"sci-1"} → /assignment/scientific
  → RequireRole(TeamLeader) → ValidationFilter (400 if empty) →
  AssignScientificReviewerCommand → case.AssignScientificReviewer("sci-1") → 200
```

## Design patterns
- **Resource-based authorization (ASP.NET Core)** — `IAuthorizationService` +
  requirement + `AuthorizationHandler` evaluating the loaded resource. The
  idiomatic pattern for "may this user perform this action on this entity."
- **Endpoint filter as cross-cutting gate** — `ReviewAuthorizationFilter`
  parallels `ValidationFilter`; keeps handlers `IMediator`-only.
- **Coarse role gate + fine resource gate** — `RequireRole` 403s unrelated roles
  cheaply (no DB); the requirement handler owns the assigned/override decision.
- **Domain as pure data + state machine** — assignment is stored data; access
  control never crosses into Domain/Application (identity stays an HTTP boundary).

## Tests

### Unit — domain (`tests/HealthCasePlatform.Domain.Tests/Cases/RegulatoryCaseTests.cs`)
- `AssignScientificReviewer_SetsAssignedScientificReviewerId`
- `AssignScientificReviewer_WhenReviewerIdEmpty_ReturnsReviewerIdEmptyError`
- `AssignScientificReviewer_CanReassignExistingReviewer`
- `AssignLegalReviewer_SetsAssignedLegalReviewerId`
- `AssignLegalReviewer_WhenReviewerIdEmpty_ReturnsReviewerIdEmptyError`
- (No review-method actor tests — methods unchanged; existing review tests stay green.)

### Unit — application (`tests/HealthCasePlatform.Application.Tests/Cases/Commands/`)
- `AssignScientificReviewerCommandHandlerTests`
  - `Handle_WhenCaseNotFound_ReturnsNotFound`
  - `Handle_WhenReviewerIdValid_AssignsAndSaves`
  - `Handle_WhenReviewerIdEmpty_DoesNotPersist`
- `AssignLegalReviewerCommandHandlerTests` (symmetric trio)

### Integration (`tests/HealthCasePlatform.Api.Tests.Integration/AssignmentAuthorizationTests.cs`)
The user's explicit ask — assigned vs unassigned, both tracks + override:
- `StartScientificReview_WhenCallerIsAssignedScientificReviewer_Returns200`
- `StartScientificReview_WhenCallerIsUnassignedScientificReviewer_Returns403`
- `StartScientificReview_WhenCallerIsTeamLeaderOverride_Returns200`
- `StartLegalReview_WhenCallerIsAssignedLegalReviewer_Returns200`
- `StartLegalReview_WhenCallerIsUnassignedLegalReviewer_Returns403`
- `StartLegalReview_WhenCallerIsTeamLeaderOverride_Returns200`
- `AssignScientificReviewer_WithTeamLeaderRole_Returns200`
- `AssignScientificReviewer_WithWrongRole_Returns403`
- `AssignLegalReviewer_WithTeamLeaderRole_Returns200`
- `AssignLegalReviewer_WithWrongRole_Returns403`

Arrange pattern: seed case with the privileged (all-roles) client → submit →
assign reviewerId = `<actor id>` via a TeamLeader client → act with
`CreateClientAs(<actor>, <role>)`. Assert 403 bodies are
`application/problem+json` (StatusCodePages, suite-consistent with existing 403
tests). 20s timeout per TESTING.md. No mocks (integration rule). No log assertions.

## Structural impact (call-out)
1. **Correct 403 semantics** for unauthorized review (was 409 in the earlier
   draft). Clients/monitoring can now distinguish forbidden-review from a genuine
   state conflict (409).
2. **Review endpoint role gates widen** to include TeamLeader. Existing
   `*_WithWrongRole_Returns403` tests (Auditor/CaseOfficer actors) stay 403 —
   those roles are still outside the widened set.
3. **Two existing integration tests break and need a one-line seed.**
   `WorkflowAuthorizationTests.StartScientificReview_WithScientificReviewerRole_Returns200`
   and `StartLegalReview_WithLegalReviewerRole_Returns200` flip to 403 unless the
   reviewer is pre-assigned. `CasesEndpointsTests` stays green (default all-roles
   principal ⇒ TeamLeader ⇒ override succeeds at the resource check).
4. **404 source moves to the filter** for review endpoints (filter loads the case
   for authz). Behavior unchanged (still 404 `application/problem+json`); the
   `*_WhenCaseUnknown_Returns404` tests stay green.
5. **New DB migration** — 2 nullable `nvarchar(100)` columns on `RegulatoryCases`.
   Existing rows get NULL ⇒ only TeamLeader can review legacy cases until
   reassigned.
6. **`FakeAuthHandler` gains `X-User`** — default `"test-user"` keeps all existing
   tests green; altering that default silently breaks the suite (same
   load-bearing-default contract as `X-Roles`).
7. **One extra DB read per review action** (filter load + command load). Accepted.
8. **`ChangeStatus` escape hatch still bypasses the rule** — API-layer authz
   guards HTTP only. Documented, consistent with the existing quirk.
9. **Smallest blast radius** of any variant: review commands, handlers, domain
   methods, and their unit tests are **untouched**. New surface is additive
   (assignment) + one filter/requirement/handler.

## Risks & follow-ups (from investigation)
- **Prod non-functionality until an IdP lands (E2, LOW).** `Program.cs:23` `AddAuthentication()`
  is bare with no default scheme — the Api `AGENTS.md` already documents that unauthenticated
  prod hits on protected endpoints → `InvalidOperationException` → `GlobalExceptionHandler` →
  **500, not 401**. The new authz feature inherits this: it is **inert in prod** and only
  exercisable via the test host (`FakeAuthHandler`). No regression vs. existing role-gated
  endpoints; the feature cannot ship as user-facing until a real auth scheme is wired.
- **No audit trail for reassignment (X1).** `AssignScientificReviewer`/`AssignLegalReviewer`
  overwrite the prior value silently ("reassignment allowed"). `CaseStatusHistory` records only
  status transitions. For an authorization feature, consider an assignment audit (who/when/
  previous reviewer) — a dedicated assignment-history table or an extended history entry. Out
  of scope for Variant A; flag as a follow-up.
- **No state precondition on assignment (X2).** A legal reviewer can be assigned at `Draft` but
  cannot act until `UnderScientificReview`. Design choice, not a bug — decide whether assignment
  should require a minimum case state, or surface "assignable now" in a future UI.
- **Universal-enforcement gap (X3).** `RegulatoryCase.ChangeStatus` (`RegulatoryCase.cs:110`)
  and any future non-HTTP caller (background job/saga calling `StartScientificReview*Handler`
  directly) bypass the rule. Variant A is honest about guarding HTTP only (design decision #7).
  Revisit a domain/Application check if/when a non-HTTP caller appears.

## Alternatives (pros/cons)
- **(Chosen) A. Resource-based authorization in the API layer.** 403, domain
  pure, idiomatic, consistent with existing `RequireRole`. Cost: extra read per
  review; an endpoint filter + handler/requirement classes; guards HTTP only.
- **B. Domain invariant (earlier draft).** Universal enforcement *if* also wired
  into `ChangeStatus`/jobs. Cons: **409 for a forbidden action** (wrong layer),
  identity/override threaded into the pure state machine, review signatures
  ripple to every caller/test. Rejected — the rule is access control, not data
  consistency.
- **C. Application-handler check** (handler resolves claims, compares to loaded
  entity, returns an error). Middle ground; still 409 (single error channel)
  unless `MapTransition` is extended; splits the decision from the authz pipeline.
  Rejected.
- **D. Single assign endpoint + `ReviewTrack` enum.** Pro: one route. Con: enum
  JSON binding needs config (no global `JsonStringEnumConverter` today). Rejected
  for two explicit endpoints (each knows its track; no serialization).
- **E. One `AssignedReviewerId` for the whole case.** One person must cover both
  review tracks — doesn't match the two-role workflow. Rejected.
- **F. 403 via `MapTransition` surgery** (map a dedicated `Error.Forbidden` type
  → 403, keep the check in the domain). Gives 403 but re-introduces B's purity
  cost. Not needed — Variant A already yields 403 cleanly.

## New external dependencies
None. (`IAuthorizationService`/`IAuthorizationHandler` are in the ASP.NET Core
shared framework the Api project already references; `ClaimsPrincipal` binding is
built into minimal APIs; FluentValidation already registered.)

## AGENTS.md updates (apply after implementation; non-inferable only)
1. **`src/HealthCasePlatform.Api/AGENTS.md`** —
   - Authorization for the two review endpoints is now **two-stage**: a widened
     coarse role gate `RequireRole(<ReviewerRole>, TeamLeader)` + a
     **resource-based** check (`ReviewAuthorizationFilter` →
     `ReviewCaseAuthorizationHandler`) that 403s unless `IsTeamLeader ||
     AssignedXReviewerId == userId`. Unauthorized review ⇒ **403**
     `application/problem+json` (NOT 409).
   - New TeamLeader-only assignment endpoints (`/assignment/scientific`,
     `/assignment/legal`).
   - Register `ReviewCaseAuthorizationHandler` as a singleton `IAuthorizationHandler`
     in `Program.cs` (additive to existing `AddAuthorization()`).
   - The 404 for an unknown review target now comes from the filter, not
     `MapTransition` (behavior identical).
   - `FakeAuthHandler` now reads `X-User` (default `"test-user"`) into
     `ClaimTypes.NameIdentifier`; both `X-User`/`X-Roles` defaults are
     load-bearing for the existing suite.
2. **`src/HealthCasePlatform.Domain/AGENTS.md`** — RegulatoryCase now carries
   per-track assignment **data** (`AssignedScientificReviewerId`/
   `AssignedLegalReviewerId`) + assigning methods. It enforces **no** access
   rule — assignment authorization is an API-layer concern. `ChangeStatus`
   escape hatch unchanged (still bypasses any HTTP-level gate).
3. **Root `AGENTS.md` → Domain quirks** — add: unauthorized review returns **403**
   (API resource-based authz), not 409; the rule guards HTTP entry points only
   (`ChangeStatus` and non-HTTP callers bypass it); `CaseStatus` still
   `HasConversion<int>()` so only the 2-column migration is schema-relevant.
