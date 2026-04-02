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

### 2026-04-02: CLI Tool Implementation Complete

**Delivered:** `ai-catalog` CLI tool at `dotnet/src/AiCatalog.Cli/` with 3 commands, plus integration tests at `dotnet/test/AiCatalog.Cli.Tests/`. All 151 tests pass (138 original + 13 new).

**Project layout additions:**
- `dotnet/src/AiCatalog.Cli/` — Console app (`SpecWorks.AiCatalog.Cli`), net9.0, System.CommandLine
- `dotnet/src/AiCatalog.Cli/Program.cs` — Entry point, wires root command with 3 subcommands
- `dotnet/src/AiCatalog.Cli/Commands/ConvertCommand.cs` — `convert marketplace <input-file> [--output <file>]`
- `dotnet/src/AiCatalog.Cli/Commands/ExploreCommand.cs` — `explore <url> [--filter-tag] [--filter-media-type] [--show]`
- `dotnet/src/AiCatalog.Cli/Commands/InstallCommand.cs` — `install <catalog-url> <entry-id> [--type mcp|skill] [--output-dir]`
- `dotnet/src/AiCatalog.Cli/Conversion/MarketplaceConverter.cs` — Core converter logic (marketplace→catalog)
- `dotnet/test/AiCatalog.Cli.Tests/ConvertMarketplaceTests.cs` — 6 tests against shared fixtures
- `dotnet/test/AiCatalog.Cli.Tests/CommandStructureTests.cs` — 7 tests for command parsing/help/integration

**Patterns chosen:**
- System.CommandLine beta4 for command parsing (latest stable-ish release)
- MarketplaceConverter as pure static class in Conversion/ — separates domain logic from CLI wiring
- Fixture format awareness: converter handles both raw `{"plugins":[...]}` and test-fixture-wrapped `{"input":{"plugins":[...]}}` formats
- Install command auto-detects type from mediaType (mcp vs skill) when --type not specified
- MCP install generates/merges `.ai-catalog/mcp-config.json`; skill install downloads to `.ai-catalog/skills/`
- CLI is a dotnet tool (`PackAsTool=true`, `ToolCommandName=ai-catalog`)
- Console.WriteLine output not captured by System.CommandLine TestConsole — file-output tests used for content verification
- TFD-005 mapping rules verified: `urn:claude:plugins:{name}` identifier pattern, all field mappings confirmed

**Test strategy:** ConvertMarketplaceTests verifies converter against shared fixtures (entry-by-entry field comparison, round-trip through parser, identifier pattern). CommandStructureTests verifies CLI argument parsing, help output, error handling, and file output.

---

## CLI Phase Complete (2026-04-02T11:16)

Orchestration logs written for Roy and Pris. Decisions merged from CLI-specific inbox. Both toolchains ready for integration.

### 2026-04-02: Integration Tests with Real Marketplace Fixtures

**Delivered:** 30 new integration tests using real-world marketplace.json files from spec-works/plugins and microsoft/work-iq repos. All 180 tests pass (138 original + 42 CLI total).

**Test fixtures added:**
- `testcases/integration/spec-works-plugins-marketplace.json` — 5 copilot plugins (markmyword, markmydeck, xregistry-mcp, officetalk, a2a-ask)
- `testcases/integration/work-iq-marketplace.json` — 3 copilot plugins (workiq, microsoft-365-agents-toolkit with 3 skills, workiq-productivity with 9 skills)

**Converter extended:** `MarketplaceConverter` now auto-detects two marketplace formats:
- **Claude format:** plugins with `display_name`, `manifest_url`, `publisher` (existing)
- **Copilot format:** plugins with `source`, `skills[]`, plus root-level `owner` (new)

**Key mapping rules for copilot format:**
- `identifier`: `urn:marketplace:{marketplace-name}:{plugin-name}`
- `displayName`: plugin.name
- `url`: plugin.source
- `mediaType`: `application/vnd.copilot.plugin+json`
- `tags`: leaf names extracted from skills[] paths
- `publisher`: derived from root `owner` (URL used as identifier, or synthetic URN `urn:marketplace:owner:{name}` when no URL)

**Learning:** Validator requires non-empty `publisher.identifier`. When marketplace owner has no URL, converter must generate a synthetic URN identifier to pass conformance validation.

**Integration test file:** `dotnet/test/AiCatalog.Cli.Tests/MarketplaceIntegrationTests.cs` — covers library conversion, CLI end-to-end file I/O, round-trip serialize→parse, conformance validation, stream-based conversion.

### 2026-04-02T11:50: Integration Tests with Real Marketplace Fixtures

**Delivered:** 30 new integration tests using real-world marketplace.json files. All 180 tests pass (138 original + 42 CLI total).

**Shared fixtures (added):**
- `testcases/integration/spec-works-plugins-marketplace.json` — 5 copilot plugins (markmyword, markmydeck, xregistry-mcp, officetalk, a2a-ask)
- `testcases/integration/work-iq-marketplace.json` — 3 copilot plugins (workiq, microsoft-365-agents-toolkit with 3 skills, workiq-productivity with 9 skills)

**Converter extension:** `MarketplaceConverter` now auto-detects and handles two marketplace formats:
- **Claude format:** plugins with `display_name`, `manifest_url`, per-plugin `publisher`
- **Copilot format:** plugins with `source`, `skills[]` array, root-level `owner`

**Key mapping for copilot format:**
- Identifier: `urn:marketplace:{marketplace}:{name}` (e.g., `urn:marketplace:spec-works-plugins:markmyword`)
- Display name: `plugin.name`
- URL: `plugin.source`
- Media type: `application/vnd.copilot.plugin+json`
- Tags: Extracted from `skills[]` path leaf names
- Publisher: Derived from root `owner` object; generates synthetic `urn:marketplace:owner:{name}` URN if owner lacks URL

**Test breakdown:** 30 new tests cover:
- Library conversion validation against both fixture sets
- CLI file I/O round-trip (convert → parse → serialize)
- Entry field verification (identifiers, names, types match expected patterns)
- Stream-based conversion

**Backward compatibility:** All 138 original tests pass; no existing behavior modified. Converter is backward compatible — Claude format conversion unchanged.

**Orchestration log:** `.squad/orchestration-log/2026-04-02T11-50-roy.md`
