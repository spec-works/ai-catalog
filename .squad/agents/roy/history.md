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

### CLI Skill Documentation Created

**Delivered:** `.squad/skills/dotnet-cli/SKILL.md` — project-level skill teaching any agent how to build, run, and use the AI Catalog .NET CLI from this repo.

**Covers:** All 3 commands (`convert marketplace`, `explore`, `install`) with full argument/option tables, real examples using test fixtures, common workflows (convert→inspect, explore→install), and 6 documented anti-patterns (missing `--` separator, wrong paths, missing subcommand, wrong identifier format, etc.).

**Source:** Documented from actual `--help` output and source code analysis of ConvertCommand.cs, ExploreCommand.cs, InstallCommand.cs. Confidence: high, source: manual (Darrel requested).

### 2026-07-18: CLI Demo — Convert & Explore Real Marketplace

**Ran:** `convert marketplace` on `testcases/integration/spec-works-plugins-marketplace.json` → produced `spec-works-catalog.json` at repo root with 5 entries (markmyword, markmydeck, xregistry-mcp, officetalk, a2a-ask). All entries correctly mapped: URN identifiers (`urn:marketplace:spec-works-plugins:{name}`), copilot plugin media type, publisher from owner, tags from skills paths.

**Ran:** `explore` on the generated catalog via local HTTP server. Table view, detail view (`--show`), and tag filtering (`--filter-tag`) all worked correctly.

**Observation:** `explore` command requires an HTTP(S) URL — `file://` URIs won't work since `HttpClient` doesn't support them by default. For local catalog inspection, serving via `python -m http.server` is a quick workaround. A future enhancement could add `file://` or local-path support to `explore`.

**Observation:** The `\u002B` JSON escape in the output for `+` in media types (e.g., `application/vnd.copilot.plugin\u002Bjson`) is valid JSON but less human-readable. System.Text.Json's default `JavaScriptEncoder` escapes the `+` character. Could be improved with `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` if desired.

### 2026-04-02T12-20: Marketplace Convert CLI Demo (Interactive)

**Ran:** Full convert marketplace workflow end-to-end with user observing interactively. Input: `testcases/integration/spec-works-plugins-marketplace.json` → Output: `spec-works-catalog.json` (5 entries).

**Also demoed:** `explore` command on generated catalog after serving with local HTTP server.

**Logged observations:**
- `explore` requires HTTP(S) URLs, not file:// paths (HttpClient limitation)
- Unicode escaping of + in media types produces `\u002B` (valid JSON, less readable)

### 2026-07-18: PR #33 Spec Delta — .NET Library & CLI Updated

**Delivered:** All .NET code aligned with PR #33 spec changes per Tyrell's spec-delta-pr33.md analysis. All 210 tests pass (168 library + 42 CLI).

**Breaking changes applied (clean break, no backward compat per user decision):**
- `CollectionReference` model deleted; `Collections` property removed from `AiCatalog`
- `CatalogEntry.Inline` renamed to `CatalogEntry.Data`; JSON key changed from `"inline"` to `"data"`
- Nesting depth limit reduced from 8 → 4
- Closed-model unknown-field warnings removed (MUST-ignore semantics per VH-2)

**New validation rules:**
- specVersion format: MUST be `Major.Minor` with non-negative integers (VH-1)
- specVersion major version compatibility check at parse time (VH-5/VH-6)
- Metadata keys: empty string keys rejected (ME-2)
- Parser is now `partial class` with `[GeneratedRegex]` for Major.Minor pattern

**CLI updates:**
- Media type constant: `application/vnd.mcp.server+json` → `application/vnd.mcp.server-card+json`
- "Inline content" → "Embedded content" in explore output

**Learnings:**
- Depth counting edge case: fixture has exactly 4 nesting levels; needed `>=` not `>` comparison against limit of 4
- Version error messages: differentiate between "has X.Y structure but non-integer components" vs "wrong format entirely" for better fixture error matching
- Semantic error matching in NegativeParsingTests requires shared key phrases between actual and expected errors; more specific error messages reduce matching fragility
- `[GeneratedRegex]` requires `partial class` declaration — applied to both Parser and Validator

### 2026-04-25T17:30 — PR #33 Update Complete

**Delivered:** All .NET code aligned with PR #33 spec changes. All 210 tests passing (168 library + 42 CLI).

**Breaking changes applied (clean break per user decision):**
- `CollectionReference` deleted
- `CatalogEntry.Inline` → `CatalogEntry.Data`
- Parser rejects `inline` field entirely
- Nesting depth 8 → 4
- Unknown fields silently ignored (MUST-ignore per VH-2)

**New validation rules:**
- specVersion Major.Minor format (VH-1 through VH-6)
- Metadata empty-key rejection (ME-2)

**CLI updates:**
- Media type constants updated
- Auto-detect expanded for `mcp-server-card`

**Orchestration log:** `.squad/orchestration-log/2026-04-25T17-30-roy.md`

**Status:** Ready for production release. Coordinated breaking change with Pris (Python). Both toolchains implemented identically per user directives.

