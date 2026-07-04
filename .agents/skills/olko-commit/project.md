# olko-commit — project adapter (healthCasePlatform)

## Dependencies (uses)

```yaml
uses:
  - olko-commit-style
  - olko-commit-docs
  - olko-test
  - olko-commit-docker
```

## Pre-merge cleanup

Before the squash merge (Step 7.5 — Option 1 or Option 2), delete plan files
that have been fully implemented and are about to be merged:

- Glob `ai/plans/*.md` for plan files created or modified in this session.
- For each plan, check if its acceptance criteria are met (all checklist items `[x]`).
- If met: delete the plan file from the working tree (`git rm`), stage the deletion,
  and amend the commit so the merge does not bring the plan into `main`.
- If not met: leave the plan file — it still has work to do.

Rationale: plans are working artifacts, not permanent docs. Once implemented and
merged, they have no value in `main` and clutter the tree.
