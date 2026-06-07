# Change Documentation Convention

## Purpose

Every feature branch must include a change document in `.squad/changes/` that
describes what changed and why. This replaces PR descriptions with a portable,
in-repo, grep-able artifact that travels with the code.

## Workflow

```
1. Create feature branch     →  git checkout -b feature/add-validate-command
2. Do the work               →  commits on the branch
3. Add a change doc           →  .squad/changes/YYYY-MM-DD-branch-name.md
4. Push                       →  git push origin feature/add-validate-command
5. CI runs automatically      →  squad-branch-merge.yml
   a. Merges main into branch (catch conflicts)
   b. Builds and tests .NET + Python
   c. If CI passes + change doc exists → merges to main
   d. Deletes feature branch
```

No PRs. No reviews. CI is the gate. The change doc is the record.

## File naming

```
.squad/changes/<date>-<branch-name>.md
```

Examples:
- `.squad/changes/2026-06-07-add-validate-command.md`
- `.squad/changes/2026-06-07-fix-media-type-detection.md`

## Template

```markdown
# <Title — what changed>

| Field | Value |
|-------|-------|
| **Branch** | `<branch-name>` |
| **Date** | <YYYY-MM-DD> |
| **Agent** | <Squad member who did the work, e.g. Roy (.NET Dev)> |
| **Issue** | <#N or "none"> |
| **Decision** | <ADR ref in decisions.md, or "none"> |

## What changed

<Brief description of the change — 2-5 sentences.>

## Why

<Motivation — why this change was needed.>

## Files touched

- `<path>` — <what changed in this file>
- `<path>` — <...>

## Tests

- <describe tests added/modified, or "existing tests cover this">

## Risks / rollback

- <anything to watch for after merge, or "low risk — additive change">
```

## Lifecycle

Change docs accumulate in `.squad/changes/`. Periodically (e.g., at release
time), they can be consolidated into a CHANGELOG entry and archived or removed.

## Why not PRs?

- Change docs are **in the repo** — portable across hosts, grep-able, agent-native
- Change docs are **in the git DAG** — immutable, unlike PR descriptions
- No platform lock-in to GitHub PR metadata
- No HITL ceremony overhead for a software factory
- CI provides the safety gate; the change doc provides the audit trail
