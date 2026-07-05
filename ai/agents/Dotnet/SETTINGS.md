# SETTINGS.md — .NET Settings & Configuration

Typed-settings (Options) pattern rules for all .NET projects. AI agents and skills read this file before adding or consuming configuration.

## Settings type
- **`sealed record`** with a `public const string SectionName = "..."`.
- Properties use **`{ get; init; }`**.
- **No business defaults in code** — defaults live in `appsettings.json`.
- **Non-nullable `string`** properties: initialize with **`= string.Empty`** as a compilation requirement (nullable reference types), *not* as a business default.

```csharp
public sealed record MyClientSettings
{
    public const string SectionName = "MyClient";
    public string BaseUrl { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; }
}
```

## Registration (in the host's `Program.cs`)
```csharp
builder.Services.Configure<MyClientSettings>(
    builder.Configuration.GetSection(MyClientSettings.SectionName));
builder.Services.AddOptions<MyClientSettings>().ValidateOnStart();
```

## Injection
- Inject **`IOptions<MyClientSettings>`** (or **`IOptionsSnapshot<T>`** for hot-reload).
- Only in the **outer layer** (API / host / Infrastructure).

## Cross-layer flow
- **Settings never reach Application-layer services.**
- The outer layer reads configuration and passes values down as method arguments / request objects.

## Defaults
- **Defaults in `appsettings.json`.**
- `appsettings.Development.json` / `appsettings.Test.json` are **overrides only**.
- **No defaults in the records.**

## Connection Strings
- ✅ Load every connection string from configuration (`appsettings*.json`) or env / user secrets — never hardcode.
- ✅ Each connection string has its own settings type (`sealed record` + `SectionName` + `{ get; init; }`), following the same pattern as client settings (e.g. `DatabaseSettings`).
- ❌ Do not read raw `Configuration["ConnectionStrings:..."]` inside services — bind it to a settings record first.

## Startup Validation
- ✅ Fail fast on missing external prerequisites at startup.
- ✅ Throw project-specific exceptions that extend a system type (e.g. `InvalidOperationException`) with a clear message naming what's missing.
- ✅ Keep startup-validation logic in the outer layer (API / Infrastructure), not in Application or Domain.

## Secrets
- **Never in `appsettings*.json` or `.cs`.**
- Env vars / user secrets. Nested sections use `__` → `:` (e.g. `MyClient__ApiKey`).
- ✅ Document required secrets in a template file (`.env.example` / `appsettings.example.json`) with placeholder values — never real values.
- ✅ In CI/CD, inject secrets via environment variables (they override local config after `AddEnvironmentVariables()`).
- ❌ Never commit a real secrets file to git — keep it gitignored.

## Validation
- **`ValidateOnStart()`** — fail fast at startup.
- **No silent fallbacks** to `null` / empty for required values.

## References
- Coding style: `ai/agents/Dotnet/CODING_STYLE.md`
- Architecture: `ai/agents/Dotnet/ARCHITECTURE.md`
- Test conventions: `ai/agents/Dotnet/TESTING.md`
- Project conventions: `AGENTS.md`
