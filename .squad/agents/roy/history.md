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

### 2026-07-18: Generated GitHub Actions Workflow for spec-works.github.io

**Delivered:** Updated `docs.yml` workflow at `generated/spec-works.github.io/docs.yml` that integrates ai-catalog generation into the existing DocFX build pipeline for the spec-works.github.io site.

**Key decisions:**
- `.well-known/ai-catalog.json` path for catalog serving (standard well-known URI pattern)
- Multi-version .NET setup (`9.0.x` for CLI + `10.0.x` for DocFX) via `setup-dotnet` multi-line version syntax

### 2026-05-16T23:48 — A2A-Ask Issues #2/#3 Fixed: Direct Send + v0.3 Client Selection

**Community-reported issues fixed on A2A-Ask repository:**

- **Issue #2:** v0.3 agents rejecting JSON-RPC method names (SendMessage vs Send mismatch)
- **Issue #3:** Unnecessary agent card fetch before every send/stream/task command

**Solution implemented:**
- Modified `CommonOptions.CreateClientAsync()` to select client version from `--a2a-version` flag instead of card probe
- Direct URLs now bypass card fetch entirely (only applies to direct endpoint URLs)
- Catalog-resolved targets (@agent@catalog) still fetch cards as needed
- v0.3 support uses correct PascalCase method names (SendMessage, StreamMessage, PostTask)

**Changes:**
- CommonOptions.cs — Version-based client selection
- SendCommand.cs, StreamCommand.cs, TaskCommand.cs — Direct URL handling
- DirectClientTests.cs, RequestCaptureState.cs — New integration tests
- TestAgentServer/Program.cs — Extended test endpoints

**Verification:** Both direct and catalog scenarios tested; no regressions.

**Outcome:** Issues closed on GitHub; commit d2d0589 merged to master; pushed.
- `repository_dispatch` with `plugins-updated` event type for cross-repo rebuild triggers
- Sparse checkout for both `spec-works/plugins` and `spec-works/ai-catalog` to minimize clone size
- Separate `dotnet build` + `dotnet run --no-build` steps for clarity and caching potential
- GitHub Pages default `application/json` Content-Type is sufficient; no custom `_headers` needed
- Checkout paths use `_` prefix (`_plugins`, `_ai-catalog`) to avoid collisions with site content

**Files created:**
- `generated/spec-works.github.io/docs.yml` — the workflow to copy into the target repo
- `generated/spec-works.github.io/README.md` — explains what changed and how to apply

### 2026-07-18: Plugin-as-Catalog Refactor — MarketplaceConverter

**Directive from Darrel:** A plugin is NOT represented by a plugin-specific media type. A plugin is an inline or referenced ai-catalog that lists the entries it contains.

**Changes:**
- Removed `ClaudePluginMediaType` (`application/vnd.claude.code-plugin+json`) and `CopilotPluginMediaType` (`application/vnd.copilot.plugin+json`) constants
- All plugin entries now use `MediaType = "application/ai-catalog+json"`
- **Copilot plugins with skills:** skills become sub-entries in a nested `Data` catalog (one CatalogEntry per skill with `application/json` mediaType and the skill path as `url`). Parent entry uses `Data` instead of `Url` (respecting url/data exclusivity per CE-5).
- **Claude plugins:** `Url` kept pointing to manifest; consumer dereferences it.
- **Copilot plugins without skills:** fall back to `Url` from `source`.

**Key architectural pattern:** Plugin = nested ai-catalog. Skills within a plugin become catalog entries inside the plugin's embedded Data catalog. Identifier pattern for skill sub-entries: `{plugin-identifier}:{skill-leaf-name}`.

**Tests updated:** `ConvertMarketplaceTests.cs` unchanged (Claude fixture already had correct expected mediaType). `MarketplaceIntegrationTests.cs` updated: skill-tag assertions replaced with nested-entry assertions. All 210 tests pass (168 library + 42 CLI).

**Shared fixture:** `testcases/marketplace-expected.json` already had `application/ai-catalog+json` (updated by Leon previously).

### 2026-04-26T02:27 — Plugin Media Type Refactoring Complete

**Delivered:** Refactored .NET MarketplaceConverter for unified plugin-as-catalog representation. All 210 tests passing (168 library + 42 CLI).

**Breaking change applied per user directive:**
- Removed `ClaudePluginMediaType` constant (`application/vnd.claude.code-plugin+json`)
- Removed `CopilotPluginMediaType` constant (`application/vnd.copilot.plugin+json`)
- All converted plugins now use `MediaType = "application/ai-catalog+json"`

**Copilot plugins with skills:**
- Skills become sub-entries in nested `Data` catalog (one CatalogEntry per skill)
- Each skill sub-entry: `identifier = {plugin}:{skill-leaf-name}`, `mediaType = application/json`, `url = skill-path`
- Parent entry uses `Data` instead of `Url` (respects url/data exclusivity per CE-5)

**Claude plugins & plugins without skills:**
- Keep `Url` pointing to manifest; consumer dereferences

**Test results:** All 210 tests pass (168 library + 42 CLI). Coordinated with Pris (Python).

**Orchestration log:** `.squad/orchestration-log/2026-04-26T022700Z-roy.md`

**Status:** Ready for coordinated production release with Python library. Plugin model now unified across both implementations.

### 2026-05-16: NuGet Packaging Readiness for SpecWorks.AiCatalog

**Requested by:** Darrel Miller — prepare the .NET library for NuGet consumption by the A2A-Ask CLI.

**What changed:**
- `dotnet/src/AiCatalog/AiCatalog.csproj` now targets `net8.0;net10.0`, keeping a previous LTS target while adding the current LTS for package consumers.
- Added `PackageProjectUrl` and linked the repo-root `README.md` into the package so `PackageReadmeFile` resolves during `dotnet pack`.
- Added `.github/workflows/publish-nuget.yml` to restore, test, pack, upload artifacts, and publish on version tags/manual dispatch when `NUGET_API_KEY` is configured.
- Updated `README.md` package matrix from .NET 8/9 to .NET 8/10 so the packaged readme matches the shipped TFMs.

**Key learnings / patterns:**
- `PackageReadmeFile` alone is insufficient when the README lives outside the project directory; the file must also be included as a packed item (`..\..\..\README.md` linked as `README.md`).
- `dotnet package search SpecWorks.AiCatalog --source https://api.nuget.org/v3/index.json` returned no existing package, so `0.1.0` is still available.
- Baseline/final validation commands for release readiness are `dotnet test .\dotnet\AiCatalog.sln -c Release --nologo` and `dotnet pack .\dotnet\src\AiCatalog\AiCatalog.csproj -c Release --nologo`.
- There was no NuGet publish workflow under `.github/workflows/` before this readiness pass; only docs/squad automation existed.

**Key file paths:**
- Library package project: `dotnet/src/AiCatalog/AiCatalog.csproj`
- Publish workflow: `.github/workflows/publish-nuget.yml`
- Packaged readme source: `README.md`


### 2026-05-16: A2A-Ask Catalog Integration Phase 1

**Delivered in `C:\src\github\spec-works\A2A-Ask`:** Added `SpecWorks.AiCatalog` package consumption, catalog parsing/resolution helpers, a new `catalog` command group, and catalog-aware resolution for `discover`/`send`. Build and test now pass with `dotnet build dotnet/ --nologo` and `dotnet test dotnet/ --nologo` from the A2A-Ask repo root.

**Architecture / patterns:**
- Catalog resolution lives in A2A-Ask (`dotnet/src/A2A-Ask/Catalog/`) while parsing stays in `SpecWorks.AiCatalog` via `AiCatalogParser` from the NuGet package.
- Phase 1 treats the `@catalog` token as a host/origin shorthand, not persisted alias storage; `CatalogInputResolver.ResolveCatalogDocumentUri()` maps host-like values to `https://...` by default and `http://localhost...` for local/dev hosts.
- `CommonOptions.ResolveTargetAsync()` is the reuse point for catalog-aware command routing, with an overload that accepts `HttpClient` so integration tests can exercise catalog resolution against the in-process test server.
- A2A candidate matching order implemented per Deckard proposal: exact identifier  exact display name (case-insensitive)  exact tag  substring fallback.

**Key file paths:**
- Catalog helpers: `dotnet/src/A2A-Ask/Catalog/TargetParser.cs`, `CatalogInputResolver.cs`, `ResolvedCatalogAgent.cs`
- Command wiring: `dotnet/src/A2A-Ask/Commands/CatalogCommand.cs`, `CommonOptions.cs`, `DiscoverCommand.cs`, `SendCommand.cs`, `Program.cs`
- Test coverage: `dotnet/tests/A2A-Ask.Tests/TargetParserTests.cs`, `ConsoleFormatterTests.cs`, `dotnet/tests/A2A-Ask.IntegrationTests/CatalogInputResolverTests.cs`, `dotnet/tests/TestAgentServer/Program.cs`
- NuGet source mapping for the published package: `C:\src\github\spec-works\A2A-Ask\nuget.config`

### 2026-05-16: A2A-Ask Direct Send + v0.3 Client Fixes

**Delivered in `C:\src\github\spec-works\A2A-Ask`:** Fixed direct-url client creation so `send`/`stream`/`task` no longer fetch an agent card before sending requests, and threaded `--a2a-version` into client creation so v0.3 targets use the compat client with legacy JSON-RPC method names.

**Architecture / patterns:**
- `CommonOptions.CreateClientAsync()` now has a direct-URL path that instantiates the request client from the user-supplied endpoint; card fetch is reserved for catalog-resolved targets that already carry an `AgentCardUrl`.
- Version switching is centralized in `CommonOptions.IsV03()` and uses `A2A.V0_3Compat.V03CompatClientFactory` for v0.3, keeping command handlers thin.
- `SendCommand` now treats plain URLs as direct endpoints and only uses catalog resolution for `@agent@catalog` style targets.
- Integration coverage uses lightweight test-only endpoints in `dotnet/tests/TestAgentServer/Program.cs` plus `RequestCaptureState` to assert both no-card-fetch behavior and v0.3 method-name selection.

**Key file paths:**
- Client creation and version switching: `dotnet/src/A2A-Ask/Commands/CommonOptions.cs`
- Command wiring: `dotnet/src/A2A-Ask/Commands/SendCommand.cs`, `StreamCommand.cs`, `TaskCommand.cs`
- Test server probes: `dotnet/tests/TestAgentServer/Program.cs`, `RequestCaptureState.cs`
- Integration tests: `dotnet/tests/A2A-Ask.IntegrationTests/DirectClientTests.cs`
