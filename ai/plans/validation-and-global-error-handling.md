# Plan — FluentValidation + RFC 7807 ProblemDetails + Global Exception Handler

Goal: every error path (validation, domain, unhandled exception) returns RFC 7807
`application/problem+json`. Add a .NET 10 `IExceptionHandler` so exceptions become
500 ProblemDetails (env-gated detail). Add FluentValidation rules for the create
command + assert via integration tests for invalid payloads.

## Scope decisions (confirmed with user)

- **"update" = the 6 existing status-change transition commands.** No new metadata-edit
  feature. They already exist (`SubmitCaseCommand`, `StartScientificReviewCommand`,
  `StartLegalReviewCommand`, `RequestDecisionCommand`, `ApproveCaseCommand`,
  `RejectCaseCommand`) — each takes only a route `Guid id`.
- **Transition validation = route constraint only.** `{id:guid}` already rejects
  malformed/empty ids (no route match → 404). No request body exists, so no
  FluentValidation validator is applicable without changing command/endpoint signatures
  (user declined body DTO + command changes). Domain state-machine errors already map to
  `404`/`409` ProblemDetails (`CasesEndpoints.cs:218-236`). No transition code changes.
- **Global exception handler detail = environment-gated.** `IHostEnvironment.IsDevelopment()`
  → include `exception.ToString()` (message + stack). Production → omit detail (title +
  status + `traceId` only).
- **Validation errors already return ProblemDetails** (`ValidationFilter.cs:32` →
  `TypedResults.ValidationProblem`, `application/problem+json`). No change to the success
  path — only validator rules are strengthened.

## Files to change

### src/HealthCasePlatform.Api/Cases/CreateCaseRequestValidator.cs — EDIT
Strengthen rules to mirror DB column lengths
(`RegulatoryCaseConfiguration.cs:14-20`: Title 200, Description 2000, Country 2,
CreatedBy 100) + `IsInEnum()` on `Priority` (`CasePriority` starts at `Low = 1`, so `0`
is invalid). Note: `Country` length (2) is already enforced exactly by the existing
`^[A-Za-z]{2}$` regex, so no separate `MaximumLength(2)` is added — it would be dead.

```csharp
RuleFor(x => x.Title)
    .NotEmpty()
    .MaximumLength(200);

RuleFor(x => x.Description)
    .MaximumLength(2000);

RuleFor(x => x.CaseTypeId)
    .NotEmpty();

RuleFor(x => x.CreatedBy)
    .NotEmpty()
    .MaximumLength(100);

RuleFor(x => x.Country)
    .NotEmpty()
    .Matches(@"^[A-Za-z]{2}$")
    .WithMessage("'Country' must be a 2-letter ISO code.");

RuleFor(x => x.Priority)
    .IsInEnum();
```

### src/HealthCasePlatform.Api/Common/GlobalExceptionHandler.cs — NEW
.NET 10 `IExceptionHandler` (from `Microsoft.AspNetCore.Diagnostics`, already imported
in `Program.cs:7`). Writes a 500 ProblemDetails via the built-in `IProblemDetailsService`
(registered by existing `AddProblemDetails()` at `Program.cs:25`). Env-gated detail via
`IHostEnvironment`. Short-circuits `OperationCanceledException` (client disconnect /
`RequestAborted`) — returns `true` without writing a 500 so benign aborts are neither
logged as server errors nor leak a dev stack trace.

```csharp
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace HealthCasePlatform.Api.Common;

internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException)
        {
            return true;
        }

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred while processing your request.",
                Detail = environment.IsDevelopment() ? exception.ToString() : null,
            }
        });
    }
}
```

### src/HealthCasePlatform.Api/Program.cs — EDIT
Register the handler before building the app (keep existing `AddProblemDetails()` and
`UseExceptionHandler()`):

```csharp
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();   // NEW line, after AddProblemDetails
```

`app.UseExceptionHandler()` at line 31 already activates the exception-handler middleware
and runs registered `IExceptionHandler` implementations in order — no other change needed.
Existing `UseStatusCodePages` block (lines 32-40) stays untouched.

### tests/HealthCasePlatform.Api.Tests.Integration/InvalidPayloadTests.cs — NEW
New class for over-length / out-of-range **payload** validation cases that no existing
test covers. Uses the existing `ApiFactory` (`IClassFixture<ApiFactory>`), real Mediator
pipeline, real SQL Server via Testcontainers. Same style as `CasesEndpointsTests.cs`.

> Reuse note (do NOT duplicate existing coverage):
> - Non-guid transition route → 404: **extend** the existing `[Theory]`
>   `TransitionEndpoint_WhenCaseUnknown_Returns404` (`CasesEndpointsTests.cs:458`) with a
>   malformed-id `InlineData` case instead of a parallel test here.
> - 400 `Errors`-dict shape: **extend** existing `ProblemDetailsTests.cs` (already asserts
>   problem+json content type) with a `ValidationProblemDetails.Errors` case instead of a
>   new file.

### tests/HealthCasePlatform.Api.Tests/ — NEW PROJECT (optional, recommended)
Convention-correct home for Api-layer unit tests (mirrors
`src/HealthCasePlatform.Api` + `.Tests` suffix per `TESTING.md:9`). Scaffold via
`dotnet new xunit`, project-ref Api, add NSubstitute + Shouldly (versions in CPM).
First occupant: `GlobalExceptionHandlerTests`. Add to `HealthCasePlatform.slnx`.
Delete template `UnitTest1.cs`.

> Alternative (lighter): skip the new project and fold the handler unit test into the
> existing `.Tests.Integration` project as a fast Docker-free test. See Suggestions.

## Flow of the application (error paths)

```
Request
  → route binding ({id:guid} rejects malformed → no match → 404 ProblemDetails)
  → ValidationFilter<T> runs FluentValidation
       invalid → TypedResults.ValidationProblem(errors)  → 400 application/problem+json
  → endpoint handler → IMediator.Send(command)
       → handler → domain ErrorOr<...>
            success       → 2xx + DTO
            Error.NotFound → TypedResults.NotFound()                      → 404 ProblemDetails
            Error.Conflict  → TypedResults.Problem(409, "Conflict", ...)   → 409 ProblemDetails
  ── unhandled exception anywhere in the pipeline ──
   → UseExceptionHandler middleware → GlobalExceptionHandler.TryHandleAsync
        OperationCanceledException → return true (no body, not logged as 500)
        else → IProblemDetailsService.TryWriteAsync → 500 application/problem+json
            Development: Detail = exception.ToString() (message + stack)
            Production : Detail = null (title + status + traceId only)
```

All four error surfaces (400 validation, 404 not-found, 409 conflict, 500 exception)
emit RFC 7807 `application/problem+json` via the shared `AddProblemDetails()` plumbing.

## Design patterns

- **`IExceptionHandler` (strategy, framework-provided)** — single centralized
  exception→ProblemDetails mapping registered in the composition root. Replaces the bare
  `UseExceptionHandler()` default that emitted a minimal body with no detail/title.
  Matches `ARCHITECTURE.md:145` ("Centralize unhandled exceptions with
  `AddProblemDetails()`, `AddExceptionHandler<T>()`, `UseExceptionHandler()`").
- **Endpoint filter pipeline (`ValidationFilter<T>`)** — validation stays at the Api
  boundary (unchanged). Per `Application/AGENTS.md` FluentValidation is an Api-layer
  concern, not a Mediator `IPipelineBehavior`.
- **RFC 7807 ProblemDetails** — single error contract across validation / domain / exception.

## Tests

### Integration — `tests/HealthCasePlatform.Api.Tests.Integration/InvalidPayloadTests.cs`
All assert `application/problem+json` content type. Require Docker (Testcontainers).

| Test name | Expected |
|---|---|
| `CreateCase_WithMalformedJson_Returns400ProblemDetails` | send raw invalid JSON (`"{ invalid"`, content-type `application/json`) → **400 — VERIFY first, see *Risks to verify*** |
| `CreateCase_WithTitleTooLong_Returns400ProblemDetails` | `Title` length 201 → 400 |
| `CreateCase_WithDescriptionTooLong_Returns400ProblemDetails` | `Description` length 2001 → 400 |
| `CreateCase_WithCreatedByTooLong_Returns400ProblemDetails` | `CreatedBy` length 101 → 400 |
| `CreateCase_WithPriorityOutOfRange_Returns400ProblemDetails` | `[Theory]` Priority `0` and `999` → 400 |

**Reused — extend existing tests (do not recreate in `InvalidPayloadTests`):**
- `TransitionEndpoint_WhenCaseUnknown_Returns404` (`CasesEndpointsTests.cs:458`) — add a
  malformed-id `InlineData` case (`/api/v1/cases/not-a-guid/{submission,approval,rejection}`
  → 404) to this existing `[Theory]` instead of a parallel test.
- `ProblemDetailsTests.cs` — add a 400 case: deserialize `ValidationProblemDetails`, assert
  `Errors` dict non-empty + keyed by property name (this file already asserts problem+json
  content type for 404s).

Existing create-invalid tests (`CasesEndpointsTests.cs:102-143`) stay green (rules are
strictly additive — `NotEmpty` + `MaximumLength` supersets of prior `NotEmpty`).

### Unit — `tests/HealthCasePlatform.Api.Tests/Common/GlobalExceptionHandlerTests.cs` (new project)
No Docker. Real `IProblemDetailsService` from a minimal
`ServiceCollection().AddProblemDetails()`; `DefaultHttpContext` with `MemoryStream` body;
`IHostEnvironment` stubbed via NSubstitute (`IsDevelopment()` true/false).

| Test name | Expected |
|---|---|
| `TryHandleAsync_InDevelopment_Returns500WithExceptionDetail` | status 500, content-type `application/problem+json`, `Detail` contains exception message + stack |
| `TryHandleAsync_InProduction_Returns500WithoutExceptionDetail` | status 500, `Detail` is null |
| `TryHandleAsync_OnCancellation_ReturnsTrueWithoutWriting500` | `OperationCanceledException` → returns `true`, status stays 200/default, no body written |

End-to-end 500 is unit-tested (not integration) because **no production path throws** —
the domain returns `ErrorOr` and all errors are mapped to 4xx. There is no real throw site
to exercise over HTTP without injecting a test-only fault (forbidden: TESTING.md bans test
doubles in integration tests). The handler unit test proves the mapping directly.

## Structural impact

- One new Api source file (`Common/GlobalExceptionHandler.cs`).
- One new line in `Program.cs` (handler registration) — order matters: after
  `AddProblemDetails()`.
- **New test project** `HealthCasePlatform.Api.Tests` added to `HealthCasePlatform.slnx`
  (if the recommended home is chosen).
- No DB migration (no schema change — `CaseStatus`/`CasePriority` already `HasConversion<int>()`,
  validator rules are API-only).
- HTTP contract of existing endpoints unchanged (routes, status codes, content types
  preserved); only validation `400` responses may now also fire on over-length/enum fields.

## Risks to verify during implementation

1. **Malformed JSON → 400 vs 500.** `CreateCase_WithMalformedJson_Returns400ProblemDetails`
   assumes the framework maps a bad JSON body to **400** `application/problem+json`. In
   minimal APIs body deserialization happens at binding time; if the `JsonException` is NOT
   caught by binding, the new `GlobalExceptionHandler` will intercept it and write a **500**,
   contradicting the assertion. The outcome is .NET-10 / minimal-API-version-dependent and is
   *directly changed* by this plan.
   - Verify minimal-API binding behavior for malformed JSON on .NET 10 **before** asserting 400.
   - If it surfaces as a 500, special-case the handler: map `BadHttpRequestException` /
     `JsonException` → 400 ProblemDetails before the 500 default (keep the env-gated detail
     off for 4xx).

2. **Cancellation guard coverage.** The `OperationCanceledException` short-circuit has no
   production throw site (no path throws), so it is exercised only by the handler unit test
   (add a `TryHandleAsync_OnCancellation_ReturnsTrueWithoutWriting500` case alongside the
   dev/prod cases) — not by integration (TESTING.md bans test doubles there).

## AGENTS.md Updates (apply during implementation)

1. **`src/HealthCasePlatform.Api/AGENTS.md`** — the composition-root line
   "`AddProblemDetails()` + `UseExceptionHandler()` + `UseStatusCodePages`" gains
   `+ AddExceptionHandler<GlobalExceptionHandler>()`. Ordering note: it MUST be registered
   after `AddProblemDetails()` (the handler injects `IProblemDetailsService`). Also note the
   env-gated detail (`IHostEnvironment.IsDevelopment()` → `exception.ToString()`, else null).
2. **Root `AGENTS.md`** — fix stale `file:line` refs (drift since written):
   `Program.cs:51` (MigrateAsync) → `Program.cs:53`; `CasesEndpoints.cs:16`
   (`ValidationFilter<CreateCaseRequest>`) → `CasesEndpoints.cs:18`.

## Suggestions / alternate patterns (pros & cons)

1. **Handler-test home — new `Api.Tests` project vs fold into `.Tests.Integration`.**
   - New project (recommended): convention-correct (`TESTING.md:9`), fast Docker-free unit
     home for future Api-layer tests. Con: one-time scaffolding + `.slnx` + CPM bookkeeping.
   - Fold into integration project: zero new project, test runs without Docker inside the
     integration assembly. Con: project name says "Integration" but hosts a pure unit test
     (mild convention drift).
2. **Mediator validation pipeline behavior instead of endpoint filter?** Rejected —
   `Application/AGENTS.md` forbids FluentValidation at the handler tier ("that = Api
   filter"). Keep the existing `ValidationFilter<T>`.
3. **Map specific exception types in the handler** (e.g. `DbUpdateConcurrencyException` →
   409, `OperationCanceledException` → 499, `NotImplementedException` → 501). Optional
   extension — not needed now (no path throws), but the handler is the natural place to add
   it later. Would switch the `Detail`/`Title` per type while keeping the 500 default.
4. **`AddProblemDetails(o => o.CustomizeProblemDetails = ...)` to inject `traceId` /
   uniform `Instance` on every response.** Optional polish — currently each result type
   sets its own fields. Centralizing `traceId` improves correlation. Out of scope unless
   wanted.

## External dependencies

None new. `FluentValidation` 12.1.1 + `FluentValidation.DependencyInjectionExtensions`
already in `Directory.Packages.props` (lines 31-32, `$(FluentValidationVersion)`).
`IExceptionHandler` + `IProblemDetailsService` ship with .NET 10
(`Microsoft.AspNetCore.Diagnostics`). NSubstitute + Shouldly (for the new unit test
project) already have CPM `<PackageVersion>` entries.

## Verify

- `dotnet build`
- `dotnet test` (integration project needs Docker engine running for Testcontainers)
- `dotnet format --verify-no-changes --no-restore`
