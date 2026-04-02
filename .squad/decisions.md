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

#### TD-6: Unknown Fields — Open vs Closed Model
**Decision:** Open model — preserve unknown fields, never reject.

#### TD-7: Conformance Level Detection
**Decision:** Provide both auto-detect and validate-against APIs.

#### TD-8: Bundle Inline Validation
**Decision:** Preserve as opaque during parsing; recursively validate in validation mode if mediaType is ai-catalog+json.

#### TD-9: Appendices Are Informative
**Decision:** OCI, MCP Registry, Claude Code Plugin mappings are informative (not normative). Core library does not implement them.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
