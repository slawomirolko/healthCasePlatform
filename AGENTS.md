# AGENTS.md — HealthCasePlatform

Project conventions for AI agents working in this repo.

## .NET Conventions

### SDK & Tooling
- **Target framework:** .NET 10 (`net10.0`) for all projects unless a project has a specific reason to differ.
- **Solution file:** use the `.slnx` (XML) solution format, not the legacy `.sln`.
- **Scaffold projects via the `dotnet new` templates** (`slnx`, `classlib`, `xunit`, `webapi`, etc.) — do not hand-write `.csproj`/`.slnx` files from scratch. Only edit the generated files to add references/packages.
- Delete the template-generated placeholder files (`Class1.cs`, `UnitTest1.cs`) before committing.

### Project Layout (Clean Architecture)
See `ai/agents/Dotnet/ARCHITECTURE.md` for full domain architecture rules (layering, slice-per-aggregate-root, consistency boundaries).
```
src/           # production projects (Domain, Application, Infrastructure, API, ...)
tests/         # test projects (mirror src/ names with `.Tests` suffix)
```
- Domain project has **no infrastructure package references** — no EF Core, no MediatR. It is the dependency leaf. `ErrorOr` is allowed (domain result type, not infrastructure).

### Test Conventions
See `ai/agents/Dotnet/TESTING.md` for full .NET test rules (framework, naming, location, tier strategy).

### Coding Style
See `ai/agents/Dotnet/CODING_STYLE.md` for full .NET coding style rules (formatting, naming, encapsulation, guard clauses, domain patterns).

### Settings & Configuration
See `ai/agents/Dotnet/SETTINGS.md` for the typed-settings (Options) pattern: record shape, registration, layering, defaults, secrets, validation.

### Build & Verify
- Build: `dotnet build`
- Test: `dotnet test`
- Style: `dotnet format --verify-no-changes --no-restore`
