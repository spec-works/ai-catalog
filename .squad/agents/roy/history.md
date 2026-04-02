# Roy — History

## Project Context
- **Project:** ai-catalog — SpecWorks Part implementing the AI Card specification
- **Spec:** https://agent-card.github.io/ai-card/ (AI Catalog: JSON format for discovering heterogeneous AI artifacts)
- **Languages:** .NET, Python
- **User:** Darrel Miller (from Dev Box)
- **Factory upstream:** spec-works/factory-squad (dotnet-patterns skill inherited)

## Learnings

Team formed 2026-04-02. Initial setup — no code yet.

### 2026-04-02: Architecture and Domain Model Ready for Implementation

**Deckard's ADRs produced:** ADR-002 (domain model: 9 public types), ADR-004 (parsing & serialization strategy with System.Text.Json).

**Tyrell's spec extraction:** 80+ requirements, ADR-002 domain model validated against CDDL schema.

**Next phase:** Phase 1 (core models) ready. Roy (.NET) and Pris (Python) implement domain models (no parsing/serialization yet). Leon then writes test fixtures. Phase 3 (parsing/serialization) requires both.

### 2026-04-02T10-55: Test Fixtures and Closed Schema Directive Ready

**From Leon:** 64 shared test fixtures committed to `testcases/` — 28 positive, 34 negative, 2 marketplace conversion pairs. Format per ADR-007 (wrapper with description, spec_section, input, expected/expected_error, test_ids). Ready for Roy to author .NET parsing and validation tests.

**From Copilot:** User directive establishes closed schema model (overrides TD-6). specVersion carries minor version. Unknown fields at object level produce warnings; metadata remains open. This affects Roy's validation logic design.

**Status:** Phase 2 complete. Roy can now begin Phase 3 implementation using Leon's fixtures as the test baseline.

### 2026-04-02: Complete .NET Core Library Implementation

**Delivered:** Full `dotnet/` project structure with solution, library, and tests — all 138 tests passing.

**Project layout:**
- `dotnet/AiCatalog.sln` — Solution (library + tests)
- `dotnet/src/AiCatalog/` — Library: `SpecWorks.AiCatalog` (net8.0;net9.0, SourceLink enabled)
- `dotnet/test/AiCatalog.Tests/` — xUnit tests consuming shared `testcases/` fixtures

**Key files:**
- `Models/` — 9 domain types (AiCatalog, CatalogEntry, HostInfo, CollectionReference, Publisher, TrustManifest, TrustSchema, Attestation, ProvenanceLink) using System.Text.Json attributes + `[JsonExtensionData]` for unknown-field preservation
- `Parsing/AiCatalogParser.cs` — Static `Parse(string)` / `Parse(Stream)` methods; validates JSON structure (root object, specVersion type/presence, entries array type, updatedAt type, tags type) during parsing; throws `AiCatalogParseException`
- `Serialization/AiCatalogSerializer.cs` — Static `Serialize()` methods; omits null optionals, preserves metadata, indented output
- `Validation/AiCatalogValidator.cs` — `Validate(catalog)` auto-detects highest conformance level; `Validate(catalog, level)` validates against a specific level; checks all MUST requirements (url/inline exclusivity, identifier+version uniqueness, trust identity match, weak digest rejection, RFC 3339 dates, HTTPS URLs, inline bundle structure, collection/publisher/trust schema required fields)
- `Validation/ConformanceLevel.cs` — Minimal, Discoverable, Trusted enum
- `Validation/ValidationResult.cs` — IsValid, ConformanceLevel, Errors[], Warnings[]

**Patterns chosen:**
- `[JsonExtensionData]` on all model types — preserves unknown properties for round-trip AND enables closed-schema warnings during validation (per Darrel's directive overriding TD-6)
- Parse vs Validate separation — parser checks JSON syntax/structure, validator checks spec conformance; this means some negative test cases fail at parse time (type mismatches, missing specVersion) and others at validation time (missing fields, identity mismatch)
- Auto-detect mode puts unmet higher-level requirements in Warnings (not Errors), keeping IsValid = true for the achieved level
- marketplace-input/expected fixtures are excluded from parsing/validation tests (they're Claude marketplace format, not AI Catalog)
- Metadata round-trip comparison uses normalized compact JSON to avoid indentation drift

**Test breakdown:** 138 total — ~28 positive parsing × 3 suites (parsing, serialization, validation) + ~33 negative + 15 unit tests

**Next:** CLI project (ADR-001/005), marketplace converter, Python parity

---

## 2026-04-02T11-05: Implementation Phase Shipping

Your work has shipped. Orchestration log written to `.squad/orchestration-log/2026-04-02T11-05-roy.md`. Parity validated with Pris's Python implementation.

**Status:** Ready for CLI phase.
