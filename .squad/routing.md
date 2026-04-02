# Work Routing — ai-catalog

Inherits factory routing from spec-works/factory-squad. Cast names mapped to factory roles.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Specification analysis | Tyrell (Spec Reader) | Extract requirements from AI Card spec, map MUST/SHOULD/MAY |
| .NET implementation | Roy (.NET Dev) | C# code, .csproj settings, NuGet packaging, xUnit tests |
| Python implementation | Pris (Python Dev) | Python code, pyproject.toml, PyPI publishing, pytest |
| Shared test cases | Leon (Test Author) | testcases/*.json fixtures, negative cases, cross-language validation |
| Architecture decisions | Deckard (Lead) | ADRs, scope trade-offs, specification version support |
| Code review | Deckard (Lead) | Review PRs, check quality, enforce conventions |
| Session logging | Scribe | Automatic — never needs routing |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | Deckard |
| `squad:deckard` | Architecture, scope, review tasks | Deckard |
| `squad:tyrell` | Spec analysis tasks | Tyrell |
| `squad:roy` | .NET implementation tasks | Roy |
| `squad:pris` | Python implementation tasks | Pris |
| `squad:leon` | Test fixture tasks | Leon |

## Rules

1. **Spec first** — before implementing anything, Tyrell extracts requirements from the specification.
2. **Test cases before code** — Leon writes shared test fixtures in `testcases/` before Roy and Pris implement.
3. **Convention compliance on every PR** — validate project structure, specs.json, and README.
4. **Cross-language consistency** — both .NET and Python implementations must pass the same shared test cases.
5. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
6. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
7. **Quick facts → coordinator answers directly.** Don't spawn an agent for factual questions.
