# TESTING.md — .NET Test Conventions

Testing rules for all .NET projects in this repo. AI agents and skills (e.g. `olko-test`) read this file before running or writing tests.

## Framework & Libraries
- **Framework:** xUnit (`dotnet new xunit`)
- **Assertion library:** [Shouldly](https://github.com/shouldly/shouldly) — prefer over FluentAssertions or raw `Assert`
- **Mocking:** NSubstitute (when needed) — do not introduce Moq or other mocking frameworks
- **Test project naming:** `<SourceProject>.Tests` (e.g. `HealthCasePlatform.Domain.Tests`)
- **Integration test naming:** `<SourceProject>.Tests.Integration` or `<SourceProject>.Integration.Tests`

## Test Naming
`MethodName_Condition_ExpectedResult`

Examples:
- `Submit_WhenNotDraft_ThrowsInvalidOperationException`
- `Complete_WhenAlreadyCompleted_ThrowsInvalidOperationException`
- `AddDocument_AddsToDocumentsCollection`

## Test Location
Mirror the source folder structure under `tests/<Project>.Tests/`.

| Source | Test |
|--------|------|
| `src/HealthCasePlatform.Domain/Entities/RegulatoryCase.cs` | `tests/HealthCasePlatform.Domain.Tests/Entities/RegulatoryCaseTests.cs` |
| `src/HealthCasePlatform.Domain/Enums/CaseStatus.cs` | (no test — enum, no behavior) |

## Test Tier Strategy
- **Unit tests** — edge cases, error paths, boundaries, domain logic. Always present for domain methods.
- **Integration tests** — main happy paths and core workflows. Added when the persistence/API layer exists.
- When a test failure is in an integration test for an edge case → suggest moving it to a unit test.
- When a test failure is in a unit test for a main path → suggest adding an integration test covering that flow.

## Rules
- No log-check assertions unless the user explicitly asks for them.
- Never add skip logic to tests.
- Never modify generated files (EF migrations, gRPC stubs) — regenerate instead.
- Use the project's existing assertion/mock libraries — don't introduce new ones.
- Delete template-generated placeholder files (`UnitTest1.cs`) before committing.
