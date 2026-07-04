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

## Secrets
- **Never in `appsettings*.json` or `.cs`.**
- Env vars / user secrets. Nested sections use `__` → `:` (e.g. `MyClient__ApiKey`).

## Validation
- **`ValidateOnStart()`** — fail fast at startup.
- **No silent fallbacks** to `null` / empty for required values.

## References
- Coding style: `ai/agents/Dotnet/CODING_STYLE.md`
- Architecture: `ai/agents/Dotnet/ARCHITECTURE.md`
- Test conventions: `ai/agents/Dotnet/TESTING.md`
- Project conventions: `AGENTS.md`
