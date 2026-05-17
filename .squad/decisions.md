# Squad Decisions

## Active Decisions

### Architecture Decisions — Deckard (2026-04-02)

#### ADR-001: Project Structure

**Decision:** Follow SpecWorks factory conventions with a CLI as a separate project within each language's solution.

```
ai-catalog/
├── specs.json                              # Linkset descriptor (RFC 9264)
├── README.md                               # Project README
├── testcases/                              # Shared cross-language test fixtures
│   ├── README.md
│   ├── minimal-catalog.json                # Level 1: simplest valid catalog
│   ├── spec-example-multi-artifact.json    # Spec §Examples: multi-artifact catalog
│   ├── spec-example-collections.json       # Spec §Examples: catalog with collections
│   ├── spec-example-claude-plugin.json     # Spec §Appendix: Claude plugin entry
│   ├── all-properties.json                 # Every field populated
│   ├── empty-entries.json                  # Valid catalog with empty entries array
│   ├── multi-version-entries.json          # Same identifier, different versions
│   ├── nested-bundle.json                  # Inline nested catalog (bundle)
│   ├── discoverable-catalog.json           # Level 2: with host + collections
│   ├── trusted-catalog.json                # Level 3: with trust manifests
│   ├── inline-artifact.json                # Entry using inline instead of url
│   ├── marketplace-input.json              # Claude marketplace.json for conversion tests
│   ├── marketplace-expected.json           # Expected ai-catalog output from conversion
│   └── negative/
│       ├── missing-spec-version.json       # Missing required specVersion
│       ├── missing-entries.json            # Missing required entries array
│       ├── missing-entry-identifier.json   # Entry missing identifier
│       ├── missing-entry-display-name.json # Entry missing displayName
│       ├── missing-entry-media-type.json   # Entry missing mediaType
│       ├── missing-entry-content.json      # Entry missing both url and inline
│       ├── both-url-and-inline.json        # Entry with both url AND inline
│       ├── duplicate-identifier.json       # Same identifier without version differentiation
│       ├── trust-identity-mismatch.json    # trustManifest.identity ≠ entry.identifier
│       ├── invalid-json.txt                # Not valid JSON at all
│       ├── missing-attestation-type.json   # Attestation missing required type
│       ├── missing-attestation-uri.json    # Attestation missing required uri
│       ├── missing-publisher-fields.json   # Publisher missing required fields
│       └── missing-host-display-name.json  # Host missing required displayName
├── dotnet/
│   ├── README.md
│   ├── AICatalog.sln                       # Solution file
│   ├── src/
│   │   ├── AICatalog/
│   │   │   ├── AICatalog.csproj            # Library: SpecWorks.AICatalog
│   │   │   ├── Models/                     # Domain model types
│   │   │   ├── Parsing/                    # JSON deserialization
│   │   │   ├── Serialization/              # JSON serialization
│   │   │   └── Validation/                 # Conformance validation
│   │   └── AICatalog.Cli/
│   │       ├── AICatalog.Cli.csproj        # CLI tool: SpecWorks.AICatalog.Cli
│   │       └── Commands/                   # CLI command implementations
│   └── tests/
│       └── AICatalog.Tests/
│           ├── AICatalog.Tests.csproj
│           ├── ParsingTests.cs
│           ├── SerializationTests.cs
│           ├── ValidationTests.cs
│           └── ConverterTests.cs
└── python/
    ├── README.md
    ├── pyproject.toml
    ├── src/
    │   └── specworks_ai_catalog/
    │       ├── __init__.py                 # Public API exports
    │       ├── models.py                   # Domain model (dataclasses)
    │       ├── parser.py                   # JSON parsing
    │       ├── serializer.py               # JSON serialization
    │       ├── validator.py                # Conformance validation
    │       └── cli/                        # CLI entry point
    │           ├── __init__.py
    │           └── main.py
    └── tests/
        ├── conftest.py                     # Shared fixtures, testcases path
        ├── test_parsing.py
        ├── test_serialization.py
        ├── test_validation.py
        └── test_converter.py
```

**Rationale:** CLI lives in the same solution/package as the library (separate project) because it directly consumes the library with no need for a separate repo.

---

#### ADR-002: Domain Model — Public Types

**Decision:** One public type per spec concept (AiCatalog, CatalogEntry, HostInfo, CollectionReference, Publisher, TrustManifest, TrustSchema, Attestation, ProvenanceLink), mirroring CDDL schema exactly. PascalCase in both .NET and Python.

**Rationale:** CDDL schema is authoritative. One-to-one mapping avoids abstraction leaks.

---

#### ADR-003: Validation — Conformance Levels

**Decision:** Three levels (Minimal, Discoverable, Trusted) with structured ValidationResult (is_valid, conformance_level, errors[], warnings[]).

**Rationale:** Conformance levels are central to spec. Structured diagnostics enable exact reporting.

---

#### ADR-004: Parsing & Serialization Strategy

**Decision:** Parsing: JSON → AiCatalog. Serialization: AiCatalog → JSON (omit nulls, preserve ordering). Use System.Text.Json (.NET), json stdlib (Python). No external deps in core.

**Rationale:** Factory wisdom: start with parsing. Lightweight keeps library maximally compatible.

---

#### ADR-005: CLI Architecture

**Decision:** Three commands: `convert marketplace` (Claude → ai-catalog), `explore` (URL → interactive browse), `install` (download artifact, enable locally). Thin layer over library.

**Rationale:** Separate project ensures library has no CLI deps. Commands map to user requirements.

---

#### ADR-006: Cross-Language Consistency

**Decision:** Shared test fixtures enforce behavior parity. Same type names, API surface, validation errors. Idiomatic differences allowed (properties vs fields, etc).

**Rationale:** Fixtures are arbiter of compatibility.

---

#### ADR-007: Test Case Design

**Decision:** Wrapper format with spec_section references. 20+ positive, 14+ negative, 2+ marketplace pairs = 36+ fixtures total.

**Rationale:** Spec traceability + parsing assertions in one format.

---

#### ADR-008: specs.json Shape

**Decision:** RFC 9264 linkset format per factory convention D006.

**Rationale:** Standard announcement mechanism to factory.

---

### Spec Interpretation Decisions — Tyrell (2026-04-02)

#### TD-1: `inline: null` Treatment
**Decision:** Treat `null` as absent (open model).

#### TD-2: specVersion Format Strictness
**Decision:** Accept any non-empty string; warn on non-Major.Minor in strict mode.

#### TD-3: Mixed Versioned/Unversioned Same Identifier
**Decision:** Valid but warn (satisfies independent uniqueness rules).

#### TD-4: URI Comparison for identity/identifier Binding
**Decision:** Exact string comparison; normalize option available.

#### TD-5: Weak Digest Algorithm Rejection
**Decision:** Reject: md5, sha1. Accept: sha256+. Unknown algorithms accepted with warning.

#### TD-6: Unknown Fields — Closed Model (Override 2026-04-02)
**Decision:** Closed model — unknown fields at object level produce warnings. metadata object remains open for extension. specVersion carries minor version; new object members require minor version bump. Clients MUST NOT encounter unknown properties in a version they understand.

*Rationale (Updated):* User decision overrides original TD-6. Gives the spec explicit control over schema evolution. Adding members is a deliberate, versioned act rather than ad-hoc extension.

#### TD-7: Conformance Level Detection
**Decision:** Provide both auto-detect and validate-against APIs.

#### TD-8: Bundle Inline Validation
**Decision:** Preserve as opaque during parsing; recursively validate in validation mode if mediaType is ai-catalog+json.

#### TD-9: Appendices Are Informative
**Decision:** OCI, MCP Registry, Claude Code Plugin mappings are informative (not normative). Core library does not implement them.

---

### Test Fixture Design Decisions — Leon (2026-04-02)

#### TFD-001: Expected Error Strings Are Descriptive, Not Exact
**Decision:** The `expected_error` field in negative fixtures contains human-readable descriptions of what should fail, not exact error message strings that implementations must match character-for-character.

**Rationale:** .NET and Python will naturally produce different error messages. The fixture describes the *category* of error (e.g., "missing required field: mediaType on entry[0]"), and each implementation's test harness should check that the validation result contains the relevant error type/code, not that the message string matches exactly.

#### TFD-002: One Error Per Negative Fixture
**Decision:** Each negative fixture tests exactly one validation error. No fixture intentionally combines multiple errors.

**Rationale:** When a negative fixture fails unexpectedly, the developer needs to know immediately which single rule is broken. Combining errors makes it ambiguous whether the implementation caught error A but missed error B.

#### TFD-003: Assertion Keys Use snake_case
**Decision:** The `expected` object uses snake_case keys (e.g., `entry_count`, `conformance_level`, `has_url`) regardless of language conventions.

**Rationale:** JSON is the interchange format. snake_case is unambiguous in JSON and avoids collision with the camelCase used in the actual AI Catalog spec fields. Both .NET and Python test harnesses will map these to their native conventions.

#### TFD-004: test_ids Field Links to Tyrell's Test Descriptions
**Decision:** Every fixture includes a `test_ids` array referencing Tyrell's test case IDs (e.g., `["TL-P01", "CL-P01"]`).

**Rationale:** Enables traceability from fixtures back to the requirements extraction. If a test case from Tyrell's descriptions doesn't appear in any fixture's `test_ids`, we know there's a coverage gap.

#### TFD-005: Marketplace Conversion Uses urn:claude:plugins:{name} Identifier Pattern
**Decision:** When converting Claude marketplace plugins to AI Catalog entries, the identifier uses the pattern `urn:claude:plugins:{plugin-name}` and the mediaType is `application/vnd.claude.code-plugin+json`.

**Rationale:** The spec doesn't dictate identifier format for converted entries, but a consistent URN pattern provides stable, unique identifiers. The marketplace plugin `name` field is already unique within a marketplace, making it a natural suffix.

---

### Copilot Directives — User (2026-04-02)

#### Closed Schema Model with Minor Versioning (2026-04-02T06-52)
**Decision:** The AI Card spec will use a closed schema model. specVersion will contain the minor version (e.g., "1.0", "1.1"). New object members require a minor version bump. Clients MUST NOT encounter unknown properties in a version they understand — the schema is closed at the CDDL level.

**Override:** This supersedes TD-6 (which previously stated "open model — preserve unknown fields, never reject").

**Why:** User decision — gives the spec explicit control over schema evolution. Adding members is a deliberate, versioned act rather than ad-hoc extension. Metadata fields remain the open extension point.

---

### CLI Implementation Decisions — Roy (2026-04-02T11:16)

#### CLI-D1: System.CommandLine Beta4
**Decision:** Used `System.CommandLine` 2.0.0-beta4.22272.1 for command parsing.

**Rationale:** Official .NET library, pre-release but production-proven and widely used. No stable alternative from Microsoft.

#### CLI-D2: MarketplaceConverter Handles Test Fixture Wrapper
**Decision:** `MarketplaceConverter.Convert()` accepts both raw marketplace JSON and test-fixture-wrapped format.

**Rationale:** Enables seamless testing without unwrapping. No ambiguity since `input` is not a valid marketplace field.

#### CLI-D3: Install Directory Convention
**Decision:** `.ai-catalog/mcp-config.json` for MCP configs, `.ai-catalog/skills/` for skill artifacts (relative to `--output-dir`).

**Rationale:** Scoped, discoverable installation. Avoids collisions with project files.

#### CLI-D4: PackAsTool for Distribution
**Decision:** `PackAsTool=true` and `ToolCommandName=ai-catalog` for `dotnet tool install` distribution.

**Rationale:** Standard .NET distribution mechanism ready for NuGet publishing.

---

### CLI Implementation Decisions — Pris (2026-04-02T11:16)

#### CLI-D1: Click over Typer
**Decision:** Used `click` instead of `typer` for CLI framework.

**Rationale:** No pydantic dependency, aligns with minimal-dependency core library design.

#### CLI-D2: Graceful Degradation for Optional Dependencies
**Decision:** `httpx` and `rich` optional at runtime; CLI falls back to `urllib.request` and plain text.

**Rationale:** Reduces entry barrier while maintaining enhanced UX when extras installed.

#### CLI-D3: Converter Supports Fixture Wrapper Format
**Decision:** `convert_marketplace_file()` auto-detects raw marketplace vs test fixture wrapper.

**Rationale:** Enables end-user usage and test validation without preprocessing.

#### CLI-D4: MCP Install Merges Config
**Decision:** `install --type mcp` merges into existing mcp-config.json rather than overwriting.

**Rationale:** Prevents data loss for users with pre-existing MCP configurations.

#### CLI-D5: Auto-detect Install Type
**Decision:** When `--type` omitted, auto-detect from entry's `mediaType` field (looks for "mcp" or "model-context-protocol" substrings).

**Rationale:** Reduces friction for common cases; explicit override available when needed.

---

### Integration Test Decisions — Roy & Pris (2026-04-02T11:50)

#### INT-D1: Copilot Marketplace Format Extension
**Decision:** Extended `MarketplaceConverter` to auto-detect and handle both Claude and copilot marketplace formats.

**Format comparison:**

| Aspect | Claude Format | Copilot Format |
|---|---|---|
| Detection | `display_name` present | `source` present, no `display_name` |
| Identifier prefix | `urn:claude:plugins:` | `urn:marketplace:{marketplace}:` |
| Display name source | `display_name` field | `name` field |
| URL source | `manifest_url` | `source` |
| Tags source | `categories[]` | `skills[]` (leaf names) |
| Media type | `application/vnd.claude.code-plugin+json` | `application/vnd.copilot.plugin+json` |
| Publisher source | Per-plugin `publisher` object | Root-level `owner` object |

When `owner` has no `url`, converter generates synthetic `urn:marketplace:owner:{name}` identifier to satisfy publisher.identifier requirement.

**Rationale:** Real-world marketplace.json files (spec-works/plugins, microsoft/work-iq) use copilot format. Support both for backward compatibility and practical interop.

**Consequence:** All 138 existing tests pass; 30 new .NET + 40 Python integration tests verify both formats.

#### INT-D2: Shared Integration Fixtures in testcases/integration/
**Decision:** Real-world marketplace.json files stored in `testcases/integration/` as raw (non-wrapped) shared cross-language fixtures per ADR-006.

**Fixtures:**
- `testcases/integration/spec-works-plugins-marketplace.json` — 5 copilot plugins from spec-works/plugins
- `testcases/integration/work-iq-marketplace.json` — 3 copilot plugins from microsoft/work-iq

**Rationale:** Shared fixtures ensure .NET and Python implement parity. Raw format (no wrapper) since converter already handles marketplace files. `integration/` subdirectory distinguishes from Leon's unit fixtures.

**Consequence:** Cross-language integration test suite; both toolchains can validate real-world plugin conversion end-to-end.

---

### Spec PR #33 Interpretation — Tyrell (2026-04-17, Approved 2026-04-25)

#### PR #33 Delta: Breaking Changes & New Normative Sections

**Context:** PR #33 to the AI Card spec introduces significant changes. Analysis in `docs/spec-delta-pr33.md`.

**Breaking changes:**
1. Collections concept deleted entirely — CollectionRef type, CR-1 through CR-7 requirements obsolete
2. `inline` field renamed to `data` on CatalogEntry
3. Nesting depth limit reduced from 8 → 4

**New normative sections:**
1. Version Handling (VH-1 through VH-6): Major.Minor format, forward compatibility, MUST-ignore unrecognized fields
2. Metadata Extensibility (ME-1, ME-2): Key naming rules, empty key rejection
3. Expanded Security: 4-layer trust model, circular reference detection, embedded content safety

**Requirements delta:** ~80 → ~85 (+5 net). 8 collection requirements deleted, ~13 version/metadata/security requirements added.

**Test case delta:** 108 → ~115 (+7 net). 8 collection tests deleted, ~15 new tests added.

---

### Copilot Directives — User (2026-04-25T13:30, Spec PR #33 Update)

#### Decision 1: Adopt Spec's MUST-ignore Rule (VH-2), Superseding TD-6

**Decision:** Consumers MUST ignore unrecognized fields within the same major version for forward compatibility.

**Override:** Supersedes TD-6 (closed model with warnings). Drop closed-model warnings for unknown top-level fields. Metadata remains the designated extension point.

**Why:** User decision aligning with spec normative text (VH-2). Simpler, more flexible forward compatibility than closed-model approach. Minor version bumps add optional fields; older consumers gracefully ignore unknown fields.

**Impact:** Both .NET and Python implementations: unknown fields silently ignored, preserved in extension storage for round-trip, no validation warnings.

---

#### Decision 2: Clean Break on `inline` → `data` Rename

**Decision:** Do NOT accept `inline` as a deprecated alias during transition. Only `data` is valid.

**Why:** User decision for explicit breaking change rather than gradual migration. Clean break simplifies implementation and forces coordinated version bump communication.

**Impact:** Existing catalogs with `inline` will fail validation. Parser rejects `inline` field entirely. Serializer emits only `data`. Coordinated release required across .NET and Python.

---

### Fixture Design Decisions for PR #33 — Leon (2026-04-17, Approved 2026-04-25)

#### Clean Break on `inline` → `data` (User Decision Affirmed)

**Decision:** All 74 fixtures use only `data` field. No fixtures accept `inline` as deprecated alias.

**Rationale:** User binding decision (approved 2026-04-25) states "clean break on inline → data. Do NOT accept inline as deprecated alias. Only data is valid."

**Impact:**
- Existing catalogs with `inline` will fail validation with new implementations
- Roy and Pris implementations reject `inline` field entirely (not just warn)
- Coordinated release needed across .NET and Python libraries
- Test fixtures serve as the contract: only `data` is valid

**Consequence:** 4 collection-related fixtures deleted, 22+ files updated from `inline` to `data`, 14 new fixtures added for version handling and other PR #33 changes. Final count: 74 fixtures (32 positive, 40 negative, 2 marketplace).

---

### Plugin Representation Decision — Roy & Pris (2026-04-26)

#### Plugin as Nested AI Catalog (User Decision, 2026-04-25T22:27)

**Decision:** A plugin is NOT represented by a plugin-specific media type. A plugin is either an inline or referenced ai-catalog that lists the entries contained in the plugin.

**Implementation:**
- All converted plugin entries now use `mediaType = "application/ai-catalog+json"`
- Copilot plugins with skills: skills become sub-entries inside a nested `data` field
  - Each skill: `identifier = {plugin-id}:{skill-name}`, `mediaType = application/json`, `url = skill-path`
- Copilot plugins without skills: use `url` from source field
- Claude plugins: `url` points to manifest; consumer dereferences
- URN prefix unchanged: `urn:claude:plugins:` remains valid as identifier scheme

**Changes:**
- `.NET MarketplaceConverter:` Removed `ClaudePluginMediaType`, `CopilotPluginMediaType` constants
- `Python converter.py:` Removed `CLAUDE_PLUGIN_MEDIA_TYPE`, `COPILOT_PLUGIN_MEDIA_TYPE` constants
- Shared fixture `testcases/marketplace-expected.json` updated for both implementations

**Rationale:** User directive for explicit control over plugin representation. Plugins map to AI Catalog semantics for unified discovery and consumption.

**Impact:**
- Roy: 210 tests passing (168 library + 42 CLI)
- Pris: 355 tests passing (ruff clean)
- Coordinated breaking change across both languages
- Ready for production release

**Consequence:** Both implementations aligned. Nested catalog model enables structured skill discovery. Breaking change requires major version bump and release notes.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

---

## Catalog State Management in A2A-Ask (Deckard, Proposal)

*Consolidated from deckard-catalog-cache.md*

### Recommendation

Treat catalog state as a **first-class local cache plus optional aliases**, stored under the existing A2A-Ask home directory but **kept separate from tokens**.

- Store catalogs under `~/.a2a-ask/catalogs/`
- Store user aliases under `~/.a2a-ask/catalog-aliases.json`
- Keep tokens in `~/.a2a-ask/tokens/` and continue keying them by **canonical resolved agent URL**, not by catalog alias
- Cache by **canonical catalog document URL + auth partition**, not just host origin
- Persist a **cache envelope** containing both the raw catalog response and a normalized agent index
- Use **TTL + HTTP validators (ETag/Last-Modified) + manual `--refresh`**
- Default to **trusting a fresh cache**, then refresh on expiry or on resolution failure
- Add lightweight **alias commands** for frequent catalogs, but keep cache behavior transparent by default

This gives human users and AI agents a stable multi-step workflow without paying the network round-trip on every invocation, while still respecting auth boundaries and handling catalog drift cleanly.

**Storage location:**
```text
~/.a2a-ask/
├── tokens/
├── catalogs/
│   ├── index.json
│   ├── <cache-key>.json
│   └── <cache-key>.bin          # encrypted payload when required
└── catalog-aliases.json
```

**Cache identity:** The primary cache key is the **canonical catalog document URL** plus **auth partition**:
```text
sha256(canonical-catalog-url + "\n" + auth-partition)
```

**Cache format:** Versioned **cache envelope** with both raw content and normalized projection:
```json
{
  "schemaVersion": 1,
  "locator": {
    "requested": "https://example.com",
    "catalogUrl": "https://example.com/.well-known/ai-catalog.json"
  },
  "authPartition": "anonymous",
  "fetchedAt": "2026-05-16T12:34:56Z",
  "expiresAt": "2026-05-16T12:49:56Z",
  "etag": "...",
  "lastModified": "...",
  "contentHash": "sha256:...",
  "rawCatalog": { "...": "server response JSON" },
  "agents": [
    {
      "identifier": "tax",
      "displayName": "Tax Agent",
      "description": "...",
      "tags": ["tax", "finance"],
      "agentCardUrl": "https://example.com/agents/tax/agent-card.json",
      "selectionTokens": ["tax", "Tax Agent", "finance"]
    }
  ]
}
```

**Cache lifecycle:**
- Default TTL: **15 minutes**
- Within TTL: use cache without network call
- After TTL: perform conditional GET using `ETag` and/or `Last-Modified`
- If refresh fails, use stale cache with warning (fail if `--refresh` was supplied)
- Manual controls: `--refresh` flag, `a2a-ask catalog refresh <locator>`, `a2a-ask catalog cache clear`

**CLI surface:**
- Existing: add `--refresh` to all catalog-aware commands
- New: `a2a-ask catalog list <url> [--refresh]`, `a2a-ask catalog show <url> [--agent <selector>] [--refresh]`
- New: `a2a-ask catalog refresh <locator>`, `a2a-ask catalog cache clear [<locator> | --all]`
- New: `a2a-ask catalog alias add|remove|list`

**AI-agent workflow:**
- `catalog list` and `catalog show` fully machine-readable with JSON output
- Include cache status, age/expiry, agent selector tokens
- AI agents choose once and reuse selectors without re-fetching

---

## AI Catalog Discovery in A2A-Ask (Deckard, Proposal)

*Consolidated from deckard-catalog-integration.md*

### Recommendation

Bring AI Catalog support into **A2A-Ask** by making the existing `discover`, `send`, `stream`, `task`, and `auth login` commands **catalog-aware**, while also adding a small explicit `catalog` command group for inspection.

Use a **NuGet dependency on `SpecWorks.AiCatalog`** for parsing/models/validation, but keep **catalog HTTP resolution and agent selection logic inside A2A-Ask**. That avoids parser duplication, avoids a second local cross-repo `ProjectReference`, and keeps A2A-specific routing policy in the A2A tool.

**Dependency strategy:**
- Add **package reference** from A2A-Ask to `SpecWorks.AiCatalog`; do **not** reimplement catalog parsing
- Boundary: `SpecWorks.AiCatalog` provides models/parsing/validation/constants/helpers; A2A-Ask owns resolution policy, selection policy, and A2A endpoint invocation
- Do not: take dependency on `AiCatalog.Cli`, add local `ProjectReference`, or add environment-variable-based local source dependency

**Discovery flow:**
- Deterministic two-stage: resolve input to catalog/agent, then resolve to one A2A agent candidate
- **Origin-only URL** (`https://example.com`) → try `/.well-known/ai-catalog.json` first; if no usable catalog, fall back to current A2A discovery
- **Explicit JSON URL** → fetch once and inspect content shape (specVersion + entries = AI Catalog; name + agent-card-like = A2A agent card)
- **Nested catalogs:** support inline and referenced nested entries with bounded recursion depth

**Catalog resolution returns:**
- Normalized list of `ResolvedCatalogAgent` records with catalog URL, entry identifier, display name, description/tags, agent-card URL, resolved agent endpoint

**Agent selection policy:**
- Exact catalog entry identifier match
- Exact display name (case-insensitive)
- Exact tag match
- Substring fallback (case-insensitive)
- Deterministic lexical scorer for multi-agent ambiguity
- Auto-select only when top candidate clearly better
- Fail with ambiguity message listing candidates with `@name` syntax otherwise

**Target syntax: `@` addressing**
```
a2a-ask send @tax@intuit -m "question"      # Agent from specific catalog
a2a-ask send @tax -m "question"             # Agent from any known catalog
a2a-ask send https://example.com -m "..."   # Direct URL (unchanged)
a2a-ask discover @tax@intuit                 # Discover specific agent
a2a-ask catalog list https://example.com     # List agents in catalog
```

**Commands:**
- Enhance existing: `discover`, `send`, `stream`, `task get/cancel`, `auth login` (accept `@` targets)
- New explicit: `a2a-ask catalog list <url>`, `a2a-ask catalog show <url>`

**Skill implications:** Update `a2a-ask-cli` skill to be **catalog-first**:
1. If user gives host or catalog URL, run `a2a-ask catalog list <url> --output text` first
2. If one candidate, proceed; if several, choose using intent and `@agentName@catalogAlias` syntax
3. Run `discover`/`send`/`stream` with explicit `@` target

**Files to modify in A2A-Ask:**
- New: CatalogCommand.cs, TargetParser.cs, CatalogInputResolver.cs, CatalogAgentResolver.cs, CatalogSelectionPolicy.cs
- Existing: Program.cs, CommonOptions.cs, DiscoverCommand.cs, SendCommand.cs, StreamCommand.cs, TaskCommand.cs, AuthLoginCommand.cs, ConsoleFormatter.cs, README.md, skills/a2a-ask-cli/SKILL.md
- Tests: CatalogDiscoverTests.cs, CatalogSendTests.cs, TargetParserTests.cs, CatalogSelectionPolicyTests.cs

---

## A2A-Ask Toolbox Architecture (Deckard, Proposal)

*Consolidated from deckard-toolbox-architecture.md*

### Recommendation

The A2A-Ask toolbox architecture should follow clean separation of concerns:

**Core components:**
- **Catalog resolution:** deterministic two-stage flow (input → catalog/agent → resolved endpoint)
- **Token management:** keyed by canonical agent endpoint, separate from catalog caching
- **Command routing:** `@` prefix signals catalog mode; everything else is treated as direct URL
- **HTTP layer:** reusable catalog fetching with ETag validation, recursive nested resolution, HTTP error handling

**Trait: Machine-readable output**
- All commands support `--output json|text` for scripting/automation
- JSON output includes canonical URLs, cache status, selector tokens for reuse
- Deterministic output format for skill workflows

**Trait: Deterministic behavior**
- No hidden model calls or heuristics
- Explicit `@agent@catalog` addressing for disambiguation
- Lexical scoring for auto-selection (debuggable, testable)
- Version stability for cross-invocation determinism

**Trait: Gradual adoption**
- Existing direct-URL workflows unchanged
- `@` syntax optional; catalog addressing only when user chooses
- Backward compatibility with legacy A2A endpoints
- Fallback to current A2A discovery if no catalog exists

---

## NuGet Readiness Decision (Roy, Approved)

*Consolidated from roy-nuget-readiness.md*

### Decision

1. Keep the package version at `0.1.0` and publish under `PackageId` `SpecWorks.AiCatalog`.
2. Target `net8.0;net10.0` for the library so the package includes the current LTS while retaining compatibility with the previous LTS.
3. Use the repository root `README.md` as the NuGet package readme by linking it into the package as `README.md`.
4. Add a GitHub Actions workflow at `.github/workflows/publish-nuget.yml` that restores, tests, packs, uploads artifacts, and publishes on `v*` tags or manual dispatch when `NUGET_API_KEY` is present.

### Rationale
- Including `net10.0` aligns the library with the current LTS expectation in team guidance, while `net8.0` keeps the package usable for consumers not yet on .NET 10.
- Reusing the repo root README avoids creating a second, divergent package readme and fixes the immediate `NU5039` pack failure.
- A dedicated publish workflow closes the biggest release-process gap without publishing anything immediately.

### Evidence
- `dotnet pack dotnet/src/AiCatalog/AiCatalog.csproj -c Release` initially failed with `NU5039: The readme file 'README.md' does not exist in the package.`
- After the csproj update, `dotnet test dotnet/AiCatalog.sln -c Release --nologo && dotnet pack dotnet/src/AiCatalog/AiCatalog.csproj -c Release --nologo` succeeded.
- `dotnet package search SpecWorks.AiCatalog --source https://api.nuget.org/v3/index.json` confirmed readiness for publication.

---

## A2A-Ask Catalog Integration Phase 1 (Roy, 2026-05-16)

*Merged from roy-a2a-ask-phase1.md*

### Decision

For Phase 1, treat the `@catalog` portion of `@agent@catalog` and `@@catalog` as a **host/origin shorthand**, not persisted alias storage.

### Why

Deckard's proposal reserves persistent alias management for a later phase, but Darrel asked for working `@` syntax now. Interpreting the catalog token as a host/origin keeps Phase 1 immediately usable (`@open@localhost:1234`, `@@example.com`) without inventing local persistence, config files, or migration behavior ahead of Phase 2.

### Implementation Notes

- `CatalogInputResolver.ResolveCatalogDocumentUri()` maps host-like values to `https://...` by default.
- Local/dev hosts (`localhost`, `127.0.0.1`, `[::1]`) map to `http://...` so in-repo testing stays frictionless.
- The explicit NuGet package dependency is satisfied via `A2A-Ask\nuget.config`, which maps `SpecWorks.AiCatalog` to the published `0.1.0` nupkg in `ai-catalog\dotnet\src\AiCatalog\bin\Release`.

### Consequences

- Phase 1 users can exercise catalog routing immediately with stable, deterministic syntax.
- Future alias persistence can layer on top by resolving stored aliases before falling back to host/origin shorthand.
- Tests can validate catalog routing against local ASP.NET test hosts without adding more cross-repo wiring.

---

## A2A Discovery Helper Surface (Deckard, 2026-05-16)

*Merged from deckard-a2a-helpers.md*

### Summary

Added minimal helper surface to `SpecWorks.AiCatalog` v0.1.0 to enable A2A-Ask and other consumers to discover and identify A2A Agent Card entries within AI Catalogs.

### Changes Implemented

#### 1. KnownMediaTypes Constants

Created `dotnet/src/AiCatalog/KnownMediaTypes.cs` — static class with well-known media type constants:

```csharp
public static class KnownMediaTypes
{
    public const string AiCatalog = "application/ai-catalog+json";
    public const string A2AAgentCard = "application/a2a-agent-card+json";
    public const string A2AAgentCardVendor = "application/vnd.a2a.agent-card+json";
}
```

**Rationale:**
- Centralizes media type strings to reduce duplication across consumers
- Enables future normalization of A2A vendor media type variants
- Provides well-documented constants via XML documentation

#### 2. CatalogEntry Extension Method

Created `dotnet/src/AiCatalog/CatalogEntryExtensions.cs` with `IsA2AAgentCard()` method:

```csharp
public static bool IsA2AAgentCard(this CatalogEntry entry)
```

**Features:**
- Case-insensitive media type comparison per RFC 2045
- Recognizes both standard and vendor-prefixed A2A media types
- Null-safe with proper exception handling
- Handles null/empty MediaType values gracefully

**Rationale:**
- Eliminates scattered media type checking logic across consumers
- RFC 2045 compliance ensures case-insensitive MIME type handling
- Vendor media type aliasing supports ecosystem interoperability until standardization

#### 3. Comprehensive Test Suite

Created `dotnet/test/AiCatalog.Tests/A2ADiscoveryHelperTests.cs` with 12 unit tests:

- Constant correctness (3 tests)
- Standard A2A media type detection (1 test)
- Vendor-prefixed A2A media type detection (1 test)
- Case-insensitivity for both variants (2 tests)
- Non-matching media types (1 test)
- Edge cases: empty, null, partial matches (3 tests)
- Null safety (1 test)

**All 12 tests pass.**

### Design Rationale

#### Media Type Aliasing

The ecosystem currently uses both `application/a2a-agent-card+json` and `application/vnd.a2a.agent-card+json`. Rather than forcing consumers to check both or requiring ecosystem-wide normalization immediately, this helper provides **aliasing** until standardization is complete.

**Future path:** When the ecosystem standardizes on a single canonical A2A media type, a deprecation period can retire the vendor variant.

#### Extension Method over Static Helper

Extension method (`entry.IsA2AAgentCard()`) was chosen over static helper for ergonomics:
- More discoverable when working with `CatalogEntry` objects in IDEs
- Consistent with .NET conventions for domain model helpers
- Enables future method chaining on catalog results

#### Scope Constraints

These helpers are **intentionally minimal**:
- No HTTP fetching (left to consumers per architecture decision)
- No resolution policy (left to consumers per architecture decision)
- No A2A endpoint binding logic (belongs in A2A-Ask)
- Pure data + validation layer

### Test Coverage

All tests pass; coverage includes:
- Happy path: both media type variants
- Case-insensitivity: 4 case variants per media type
- Unhappy path: non-matching media types, empty strings
- Edge cases: null entries, partial matches
- Robustness: null safety

### Impact

#### A2A-Ask (Immediate Consumer)

Enables A2A-Ask to:
```csharp
var entry = catalog.Entries.FirstOrDefault();
if (entry.IsA2AAgentCard())
{
    // Resolve and invoke the agent
}
```

#### Future Consumers

Any tool consuming AI Catalogs can now:
- Use `KnownMediaTypes` constants for consistency
- Filter catalog entries by A2A agent card type via `IsA2AAgentCard()`
- Avoid re-implementing media type detection logic

### NuGet Package Implications

- No new external dependencies added
- No breaking changes to existing API
- `SpecWorks.AiCatalog` v0.1.0 already published; helpers available to all consumers
- Version remains stable; next release may consolidate with upcoming Toolbox work

### Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Vendor media type never standardizes | Constants provide extensibility point for future registry pattern |
| Case-sensitivity issues in ecosystem | RFC 2045 compliance ensures consistent handling across implementations |
| A2A-Ask needs additional helpers | Minimal surface here; more helpers can be added if demand emerges |

### Decision

**Approved.** Implement minimal A2A discovery helpers as described. These enable A2A-Ask integration without scope creep or additional abstractions. Future Toolbox work (ADR-007 revival) will handle multi-catalog indexing and capability mapping as a separate phase.

---

### A2A-Ask: Direct Endpoint + v0.3 Client Selection — Roy (2026-05-16)

**Context:** GitHub issues #2 and #3 showed that A2A-Ask was resolving an agent card for every `send`/`stream`/`task` invocation and always creating the v1 JSON-RPC client. This broke direct endpoint scenarios and caused v0.3 agents to reject PascalCase JSON-RPC methods like `SendMessage`.

**Decision:** Use the user-supplied endpoint directly for `send`/`stream`/`task` client creation, and select the client implementation from `--a2a-version` instead of probing the target for a card first.

**Rationale:**
- Direct URLs may not have a card at `/.well-known/agent-card.json`, may require different auth for card discovery, or may intentionally differ from the card URL.
- The CLI already has enough user intent to choose protocol behavior: direct URL + `--a2a-version`.
- Catalog-resolved targets remain safe to fetch because the catalog already provided an explicit `AgentCardUrl`.
- Centralizing the switch in `CommonOptions.CreateClientAsync()` avoids duplicating version logic across command handlers.

**Implementation notes:**
- Direct URLs use `A2AClient` for v1 and `A2A.V0_3Compat.V03CompatClientFactory` for v0.3.
- Catalog-resolved targets still fetch the card when needed to obtain the final endpoint URL.
- `SendCommand` treats plain URLs as direct endpoints and reserves catalog resolution for `@agent@catalog` references.
- Integration tests use test-only direct endpoints plus request capture to verify both no-card-fetch behavior and v0.3 method names.

**Files Modified:**
- CommonOptions.cs (version/client selection logic)
- SendCommand.cs (direct URL handling)
- StreamCommand.cs (direct URL handling)
- TaskCommand.cs (direct URL handling)
- DirectClientTests.cs (new test fixture)
- RequestCaptureState.cs (new test helper)
- TestAgentServer/Program.cs (extended test endpoints)

**Outcome:** Issues #2 and #3 closed; merged to master (commit d2d0589).

---

### A2A-Ask Catalog Targeting Documentation — Roy (2026-05-17)

**Decision:** Document the implemented A2A-Ask catalog targeting surface exactly as shipped: `catalog show <target>` selects a specific agent through `@agent@catalog`, while bare `@agent` targets remain parsed but incomplete in Phase 1 without an explicit catalog host or URL.

**Why:** The user request referenced a two-argument `catalog show <catalog-url> <entry-id>` shape, but the actual CLI surface in `CatalogCommand` and `TargetParser` uses a single target argument plus `@agent@catalog` addressing. Recording this avoids future docs drift and keeps AI-facing instructions aligned with the executable CLI behavior.

**Implications:**
- README, SKILL.md, and CLI reference should describe `catalog show <target>` rather than inventing a second positional argument.
- Docs should explain all three target forms (`@agent`, `@agent@catalog`, `@@catalog`) while clearly noting the current Phase 1 limitation on bare `@agent` use.
