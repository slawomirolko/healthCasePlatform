# Project Adapter — olko-test

Local overload for the `olko-test` marketplace skill.

## Testing docs location
.NET testing conventions live at `ai/agents/Dotnet/TESTING.md` — **not** in the source tree.
.NET coding style rules live at `ai/agents/Dotnet/CODING_STYLE.md`.
The skill's default walk-up discovery (nearest `TESTING.md`/`CODING_STYLE.md` up the dir tree from each changed `.cs`) will not find them.
Always load both files explicitly before running or writing .NET tests.

Resolution order for .NET rules:
1. `ai/agents/Dotnet/TESTING.md` (testing) + `ai/agents/Dotnet/CODING_STYLE.md` (style)
2. Root `AGENTS.md` (project layout, build/verify commands)
3. Marketplace `olko-test` SKILL.md defaults

## Stack
- **Backend:** .NET 10 (`net10.0`)
- **Test framework:** xUnit
- **Assertion library:** Shouldly
- **Mocking:** NSubstitute (only when needed)
- **Test command:** `dotnet test`
- **Style command:** `dotnet format --verify-no-changes --no-restore`

## Test project discovery
- Source project `<Name>` → unit test project `<Name>.Tests.csproj`
- Integration test project `<Name>.Tests.Integration.csproj` or `<Name>.Integration.Tests.csproj`
- Architecture test project `<Name>.Architecture.Tests.csproj`
- Source projects live under `src/`; test projects mirror under `tests/`

## Conventions (summary — full rules in TESTING.md)
- Test naming: `MethodName_Condition_ExpectedResult`
- Test location: mirror source folder structure under `tests/<Project>.Tests/`
- No log-check assertions unless explicitly requested
- Never add skip logic to tests
- Never modify generated files — regenerate instead
