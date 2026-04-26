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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
