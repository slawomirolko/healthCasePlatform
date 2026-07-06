# Project adapter — olko-investigate-existing

Inherits layer-control flags and project facts from `.agents/skill-config.md`.
Project-specific .NET conventions are covered by the repo-root `AGENTS.md` and the
layer `ai/agents/Dotnet/*.md` docs — do not duplicate them here.

```yaml
uses:
  - olko-plan-editor       # Step 6c: delegate improvement-plan creation
  - olko-agents-optimizer  # Step 4: AGENTS.md content methodology
```
