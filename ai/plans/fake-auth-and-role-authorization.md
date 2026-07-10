# Plan — Fake auth + role-based workflow authorization

Goal: fake authenticated users for tests, secure all case workflow endpoints by
role, integration tests proving allow/deny (403).

## Context (what is true now)
- No auth at all. `Program.cs` has no `AddAuthentication` / `AddAuthorization`,
  no `UseAuthentication` / `UseAuthorization`.
- All `CasesEndpoints` are open (no `.RequireAuthorization`).
- Integration tests use shared `ApiFactory : WebApplicationFactory<Program>`
  (Testcontainers SQL). Bare `factory.CreateClient()` calls everywhere, no auth header.
- Api csproj = `Microsoft.NET.Sdk.Web` → ASP.NET Core shared framework
  (`AddAuthentication` / `AddAuthorization` / `AuthorizationPolicyBuilder` built in, no new pkg).
- Test csproj = `Microsoft.NET.Sdk` + `Microsoft.AspNetCore.Mvc.Testing`. ⚠ The
  `Microsoft.NET.Sdk` SDK does **not** auto-grant the `Microsoft.AspNetCore.App`
  shared framework, and `Microsoft.AspNetCore.Mvc.Testing`'s transitive deps do
  **not** bring `AuthenticationHandler<TOptions>` / `AuthenticationSchemeOptions` /
  `AddAuthentication` / `AddScheme` (those live in `Microsoft.AspNetCore.Authentication`,
  shared-framework only). Confirmed: no `<FrameworkReference>` exists in any csproj today.
  → `FakeAuthHandler.cs` and the `AddAuthentication("Fake").AddScheme<...>(...)` call will
  **not compile** unless an explicit `<FrameworkReference Include="Microsoft.AspNetCore.App" />`
  is added to the integration test csproj (see Files to change).

## Design decisions
1. **Auth = outer-layer (Api) concern.** Roles + policies live in Api `Common/`.
   Domain/Application untouched (no User slice; identity is HTTP boundary).
2. **Fake handler = test-only.** Lives in the integration test project, registered
   in `ApiFactory.ConfigureTestServices` as the default `"Fake"` scheme. Production
   gets `AddAuthentication()` (no scheme yet) + `AddAuthorization()` + the pipeline
   middlewares. ⚠ With a **bare** `AddAuthentication()` (no default scheme), an
   unauthenticated prod hit on a `RequireAuthorization` endpoint triggers
   `ChallengeAsync` with a null scheme → `InvalidOperationException` (no auth handler
   registered) → caught by `GlobalExceptionHandler` (outermost in pipeline) → **500
   `application/problem+json`, NOT 401**. The test host is unaffected (it sets default
   scheme `"Fake"`). Accept the 500-until-IdP state and document it, OR register a
   noop/real default scheme to get a clean 401. Do **not** claim "prod → 401".
3. **Backward-compat default.** Fake handler: no `X-Roles` header → principal carries
   ALL roles. Existing tests send no header → stay green, zero edits.
   `CreateClientWithRoles(params string[] roles)` adds the header for new auth tests.
4. **Role authorization = inline `RequireAuthorization(b => b.RequireRole(...))`**
   on each mutation endpoint (no named policies needed; minimal-API overload exists).
   Read endpoints = `.RequireAuthorization()` (any authenticated role, incl. Auditor).
5. **Every listed role gets a real endpoint** (user listed 5 roles — all must be used):
   - CaseOfficer → Create, Submit
   - ScientificReviewer → StartScientificReview
   - LegalReviewer → StartLegalReview
   - TeamLeader → RequestDecision, Approve, Reject
   - Auditor → read-only (List/Get/History, authenticated)
6. **403 not 401:** handler ALWAYS authenticates (Success). Wrong role →
   AuthorizationMiddleware → 403 Forbidden. Matches requirement.

## Files to change

### Production — `src/HealthCasePlatform.Api/`
- **NEW** `Common/AppRoles.cs` — `static class AppRoles` with 5 `const string`
  role names + `static readonly string[] All`.
- **EDIT** `Cases/CasesEndpoints.cs` — add `.RequireAuthorization(...)` to every
  endpoint (role-specific for mutations, bare authenticated for reads).
- **EDIT** `Program.cs` — add `AddAuthentication()` + `AddAuthorization()`;
  add `app.UseAuthentication()` + `app.UseAuthorization()` in pipeline (before
  endpoint mapping, after exception/status-code pages).

### Tests — `tests/HealthCasePlatform.Api.Tests.Integration/`
- **EDIT** `HealthCasePlatform.Api.Tests.Integration.csproj` — add
  `<FrameworkReference Include="Microsoft.AspNetCore.App" />` (required for
  `AuthenticationHandler<TOptions>` / `AddAuthentication` / `AddScheme`; see ⚠ above).
- **EDIT** `CasesEndpointsTests.cs` — **extract** the private seeding helpers
  `CreateCaseAsync()` (`CasesEndpointsTests.cs:27`) and `BringCaseToStateAsync(...)`
  (`:34`) into a shared integration helper (e.g. a `static class CaseTestSeeder`, or
  instance methods on `ApiFactory`) so `WorkflowAuthorizationTests` reuses them instead
  of duplicating the seed Arrange. Both classes hit the same "create + drive through
  status" path; wrong-role tests must seed with the privileged (all-roles) client, then
  act with the scoped client.
- **NEW** `FakeAuthHandler.cs` — `sealed class FakeAuthHandler :
  AuthenticationHandler<AuthenticationSchemeOptions>`. Reads `X-Roles` header;
  absent → all roles; present → comma-split + trim those roles only. Always
  `AuthenticateResult.Success`. (Pin the delimiter = comma so multi-role is defined.)
- **EDIT** `ApiFactory.cs` — in `ConfigureTestServices`: register
  `AddAuthentication("Fake").AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>("Fake")`.
  Add `public HttpClient CreateClientWithRoles(params string[] roles)`.
- **NEW** `WorkflowAuthorizationTests.cs` — allow + 403 tests (see Tests section).

## Application flow
```
Request
  → UseExceptionHandler → UseStatusCodePages
  → UseAuthentication  (FakeAuthHandler builds ClaimsPrincipal w/ roles; prod: no scheme → anonymous)
  → UseAuthorization   (endpoint policy: RequireRole checked → 403 if role missing, 200/201 if ok)
  → endpoint filter (ValidationFilter) → IMediator.Send → result→HTTP map
```
Test client w/ header `X-Roles: Auditor` hits `/submission` (needs CaseOfficer):
authenticated ✓, role mismatch → AuthorizationMiddleware short-circuits → **403**.

## Design patterns
- **Strategy / handler plug-in** — `AuthenticationHandler<TOptions>` (ASP.NET Core
  built-in auth extension point). Fake impl swapped in at the composition root (test host).
- **Role-based authorization policy** — `AuthorizationPolicyBuilder.RequireRole`
  per endpoint (policy object pattern, minimal).
- **Header-driven test identity** — test fixture as authenticated-user factory
  (factory method pattern, mirrors existing `CreateClient()` reuse).

## Tests (integration — `WorkflowAuthorizationTests.cs`)
Naming `Method_Condition_Result` (no Or/And). Arrange w/ privileged (all-roles) client,
act w/ role-scoped client. 20s timeout per TESTING.md.

Required by user:
- `SubmitCase_WithCaseOfficerRole_Returns200` — seed Draft; act as CaseOfficer → OK.
- `StartScientificReview_WithScientificReviewerRole_Returns200` — seed Submitted (admin submit); act as ScientificReviewer → OK.
- `SubmitCase_WithWrongRole_Returns403` — seed Draft; act as Auditor → Forbidden.

Full role coverage (all 5 roles exercised):
- `CreateCase_WithCaseOfficerRole_Returns201`
- `CreateCase_WithWrongRole_Returns403` (Auditor)
- `StartLegalReview_WithLegalReviewerRole_Returns200`
- `StartLegalReview_WithWrongRole_Returns403` (CaseOfficer)
- `RequestDecision_WithTeamLeaderRole_Returns200`
- `RequestDecision_WithWrongRole_Returns403` (ScientificReviewer)
- `ApproveCase_WithTeamLeaderRole_Returns200`
- `ApproveCase_WithWrongRole_Returns403` (Auditor)
- `ListCases_WithAuditorRole_Returns200` (read access for read-only role)

Unit tests: none — authz is HTTP middleware, only integration-testable.

## Structural impact (call-out)
- **All case endpoints now require auth.** Unauthenticated prod calls → **500
  `application/problem+json`** (not 401) until a real/noop default auth scheme is
  wired — see Design decision 2 ⚠. `/health` + Swagger stay open (not under
  `RequireAuthorization`; no `FallbackPolicy` added).
- **Pipeline gets 2 middlewares** (`UseAuthentication`, `UseAuthorization`), placed
  after `UseExceptionHandler`/`UseStatusCodePages`, before endpoint mapping. Because
  StatusCodePages sits **outside** auth, every 403/401 from the authz layer is rewritten
  to `application/problem+json` (empty body filled in) — so the new `*_Returns403`
  tests may also assert `Content-Type: application/problem+json` for suite consistency.
- **Existing integration tests unchanged & green** — fake handler defaults to all
  roles when no header. No assertion edits to `CasesEndpointsTests` / `ListCasesTests` /
  others (only the seed-helper *extraction* refactor; behavior unchanged).
- **No DB migration** — roles are claims, not persisted. No new tables/FKs.
- **No new NuGet packages** — but the integration test csproj **does** need a new
  `<FrameworkReference Include="Microsoft.AspNetCore.App" />` (see Files to change).

## Alternatives (pros/cons)
- **A. Fake handler in PRODUCTION (gated by env).** Pro: dev runs without real IdP.
  Con: test-only code shipped to prod = security smell. → Rejected unless dev-run needed.
- **B. Fake handler defaults to ANONYMOUS (no header → no roles).** Pro: stricter,
  no magic all-roles default. Con: every existing integration test must be edited to
  add a role header (high churn across 6 test files). → Rejected for churn;
  revisit if existing tests should assert auth too.
- **C. Named policies in `AddAuthorization`** (`AddPolicy("Submit", p=>p.RequireRole(...))`)
  instead of inline builder. Pro: reusable, centralized. Con: indirection for 6 roles.
  → Optional later; inline is clearer now.

## New external dependencies
None.

## AGENTS.md Updates (apply after implementation)
Target file: `src/HealthCasePlatform.Api/AGENTS.md` (the only AGENTS.md reachable from
touched files; no `tests/**/AGENTS.md` exists, and none is to be created). Non-inferable
items only:

1. **Auth pipeline ordering (cross-boundary rule).** `UseAuthentication()` +
   `UseAuthorization()` are registered AFTER `UseExceptionHandler()` / `UseStatusCodePages()`
   and BEFORE endpoint mapping. This ordering is load-bearing: it is why a 403/401 from the
   authz layer becomes `application/problem+json` (StatusCodePages fills the empty body) and
   why `GlobalExceptionHandler` can catch auth-pipeline exceptions. Add to the existing
   pipeline-registration bullet in the Hard rules / What lives here section.
2. **Prod "secured" quirk (non-inferable gotcha).** With the bare `AddAuthentication()`
   (no default scheme), an unauthenticated prod call on a protected endpoint surfaces as
   **500 `application/problem+json`**, NOT 401 (challenge throws → `GlobalExceptionHandler`
   → 500). Test host is fine (default scheme `"Fake"`). Register in the same "non-inferable
   gotcha" register as the root AGENTS.md domain quirks; note the fix = wire a real/noop
   default scheme when a real IdP lands.
3. **`/health` + Swagger deliberately open.** They are not under `RequireAuthorization`;
   do NOT add an authorization `FallbackPolicy` (it would close `/health`). Guard note.
4. **Fake handler all-roles default (test convention).** No `X-Roles` header ⇒ principal
   carries ALL roles ⇒ existing integration tests stay green with zero header edits.
   Removing/altering this default silently breaks all 6 integration test files. Guard note
   (non-inferable; lives in test project which has no AGENTS.md — record here as a pointer
   or add when a tests-layer AGENTS.md is created).
