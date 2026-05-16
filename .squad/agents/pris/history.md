# Pris — History

## Project Context
- **Project:** ai-catalog — SpecWorks Part implementing the AI Card specification
- **Spec:** https://agent-card.github.io/ai-card/ (AI Catalog: JSON format for discovering heterogeneous AI artifacts)
- **Languages:** .NET, Python
- **User:** Darrel Miller (from Dev Box)
- **Factory upstream:** spec-works/factory-squad (python-patterns skill inherited)

## Core Context

**Phase 1 (2026-04-02 to 2026-04-02T11-57):** Domain model and core library (v0.1.0) delivered. 242 tests passing using 74 shared test fixtures (28 positive, 34 negative, 2 marketplace conversion pairs). Full Python implementation matching Roy's .NET library with identical error messages and validation logic.

**Key patterns established:**
- snake_case Python fields ↔ camelCase JSON keys via explicit key maps
- `extra_fields: dict[str, Any]` preserves unknown fields for round-trip (closed schema per user directive)
- Parser validates JSON structure; validator checks spec conformance (MINIMAL/DISCOVERABLE/TRUSTED levels)
- Marketplace converter auto-detects Claude vs Copilot format; generates URN identifiers
- Conformance auto-detect: MINIMAL → DISCOVERABLE → TRUSTED

**Integration tests:** 306 total tests with real marketplace plugins from spec-works and work-iq.

## Learnings

### CLI Implementation Complete

**New modules:**
- `python/src/aicatalog/converter.py` — marketplace-to-catalog conversion logic (TFD-005 URN pattern, media type mapping)
- `python/src/aicatalog/cli/__init__.py` + `cli/main.py` — click-based CLI with 3 commands
- `python/tests/test_cli.py` — 24 tests covering all commands

**CLI commands:**
- `ai-catalog convert marketplace <file> [--output]` — converts Claude marketplace.json to ai-catalog.json
- `ai-catalog explore <url> [--filter-tag] [--filter-media-type] [--show] [--json-output]` — fetches remote catalog, displays in rich table or plain text
- `ai-catalog install <url> <entry-id> [--type mcp|skill] [--config] [--skills-dir]` — installs MCP entries to mcp-config.json or downloads skills

**Patterns:**
- CLI uses `click` (not typer) — simpler, no pydantic dep, matches project's no-external-deps-in-core philosophy
- HTTP fetch tries `httpx` first, falls back to stdlib `urllib.request` (no hard runtime dep)
- Display tries `rich` first, falls back to plain text columns
- Converter supports both raw marketplace JSON and test fixture wrapper format
- MCP install merges into existing config files (doesn't overwrite)
- Auto-detects install type from media type when `--type` not specified

**Dependencies added to pyproject.toml:**
- `[cli]` extras group: click, httpx, rich
- `[dev]` group updated to include CLI deps for testing
- `console_scripts` entry point: `ai-catalog = aicatalog.cli.main:main`

**Test results:** 266 total (242 core + 24 CLI), ruff clean.

---

## CLI Phase Complete (2026-04-02T11:16)

Orchestration logs written for Roy and Pris. Decisions merged from CLI-specific inbox. Both toolchains ready for integration.

### Integration Tests with Real Marketplace Fixtures

**Fixtures created:** `testcases/integration/spec-works-plugins-marketplace.json` (5 plugins) and `testcases/integration/work-iq-marketplace.json` (3 plugins). Shared with .NET at `testcases/integration/`.

**Test file:** `python/tests/test_integration.py` — 40 tests across 7 test classes:
- `TestConversionProducesValidCatalog` (4): Conversion returns AiCatalog, serializes to valid JSON
- `TestEntryCounts` (2): 5 entries for spec-works, 3 for work-iq
- `TestFieldFidelity` (14): Spot-checks identifiers, names, descriptions, versions, media types, URN prefixes
- `TestConformanceValidation` (6): MINIMAL conformance (no host), expected url/inline validation errors
- `TestRoundTrip` (5): serialize→parse fidelity, stable conformance, double round-trip identical JSON
- `TestCliConvertMarketplace` (6): CLI stdout, file output, parse-back, entry spot-checks via CliRunner
- `TestConvertMarketplaceDict` (3): Dict-based conversion, file/dict equivalence

**Total suite:** 306 tests (266 existing + 40 integration), ruff clean.

**Key finding:** Marketplace plugins lack `manifest_url`, so converted entries have no `url`/`inline`. Validator correctly reports one content error per entry. This is expected — the marketplace format doesn't carry artifact URLs.

### 2026-04-02T11:50: Integration Tests with Real Marketplace Fixtures

**Delivered:** 40 new integration tests using real-world marketplace.json files. All 306 tests pass (266 original + 40 integration).

**Shared fixtures (added, coordinated with Roy):**
- `testcases/integration/spec-works-plugins-marketplace.json` — 5 copilot plugins from spec-works/plugins
- `testcases/integration/work-iq-marketplace.json` — 3 copilot plugins from microsoft/work-iq

**Test file:** `python/tests/test_integration.py` — 40 tests across 7 test classes:
- `TestConversionProducesValidCatalog` (4): Conversion returns AiCatalog, serializes to valid JSON
- `TestEntryCounts` (2): Verify 5 and 3 plugin counts respectively
- `TestFieldFidelity` (14): Spot-check identifiers (URN pattern), names, descriptions, versions, media types, publisher data
- `TestConformanceValidation` (6): MINIMAL conformance level, one content validation error per entry (missing url/inline)
- `TestRoundTrip` (5): Serialize→parse fidelity, stable conformance level, double round-trip produces identical JSON
- `TestCliConvertMarketplace` (6): CLI stdout output, file-based conversion, parse-back verification, entry name checks
- `TestConvertMarketplaceDict` (3): Dict-based conversion API, file/dict equivalence

**Converter parity with Roy:**
- Auto-detects Claude format (`display_name`) vs copilot format (`source`)
- Copilot format generates `urn:marketplace:{marketplace}:{name}` identifiers
- Creates synthetic `urn:marketplace:owner:{name}` when marketplace owner lacks URL
- Both formats produce `application/vnd.copilot.plugin+json` media type for copilot format

**Expected behavior learning:** Marketplace entries have no `url` or `inline`; validator reports single content error per entry during MINIMAL conformance check. This is correct — marketplace format doesn't include artifact content references.

**Linting:** ruff clean (0 errors)

**Backward compatibility:** All 266 original tests pass; no existing behavior modified.

**Orchestration log:** `.squad/orchestration-log/2026-04-02T11-50-pris.md`

### PR #33 Spec Delta Update

**Spec changes applied:** PR #33 ("Updates based on discussions during last meeting") — 4 breaking changes + new normative content.

**Model changes:**
- Deleted `CollectionReference` dataclass and all collection-related code (removed from spec)
- Renamed `inline` field → `data` on `CatalogEntry` (clean break, no backward compat per user decision)
- Removed `collections` list from `AiCatalog`
- Public types reduced from 9 to 8

**Parser changes:**
- Reads `"data"` instead of `"inline"` — does NOT accept `"inline"` as alias (user decision #2)
- Added `specVersion` Major.Minor format validation at parse time (VH-1 through VH-6)
- Rejects unsupported major versions (>1) with informative error
- Accepts any minor version within major version 1
- Removed collection parsing, `_COLLECTION_KEYS`, `_parse_collection()`
- Unknown fields silently ignored per VH-2 MUST-ignore rule (user decision #1)

**Serializer changes:**
- Emits `"data"` instead of `"inline"`
- Removed collection serialization (`_serialize_collection` is now a stub)

**Validator changes:**
- `url`/`data` mutual exclusivity (was `url`/`inline`)
- Nesting depth limit changed from 8 → 4 (`DEFAULT_MAX_NESTING_DEPTH = 4`)
- Added recursive nesting depth validation with proper depth counting (root=1)
- Added metadata empty-key validation (ME-2) on catalog, entry, and trust manifest metadata
- Removed collection URL HTTPS check
- Renamed "bundle" terminology → "nested catalog entry" throughout

**CLI changes:**
- `_install_skill` now reads `entry.data` instead of `entry.inline`
- MCP auto-detection expanded: added `"mcp-server-card"` to indicators
- Error message updated: "no url or data content" instead of "no url or inline content"

**Test fixtures updated:** Leon's fixtures already renamed/updated. Additional fixture updates for `inline`→`data`, collections removal, version handling, metadata validation.

**Test results:** 355 tests passing (up from 306). New tests: version handling (VH-P/N), metadata validation (ME-N01), url/data exclusivity. Ruff clean (0 errors).

**Key decisions followed:**
- User decision #1: Adopted MUST-ignore for unknown fields (VH-2). Dropped closed-model warnings.
- User decision #2: Clean break on `inline`→`data`. No deprecated alias.

### 2026-04-25T17:30 — PR #33 Update Complete

**Delivered:** All Python code aligned with PR #33 spec changes. All 355 tests passing; ruff clean (0 errors).

**Breaking changes applied (clean break per user decision):**
- `CollectionReference` dataclass deleted
- `inline` field → `data` on `CatalogEntry`
- Parser rejects `inline` field entirely
- Nesting depth 8 → 4
- Unknown fields silently ignored (MUST-ignore per VH-2)

**New validation rules:**
- specVersion Major.Minor format (VH-1 through VH-6)
- Metadata empty-key rejection (ME-2)

**CLI updates:**
- Error messages updated for data content
- Auto-detect expanded for `mcp-server-card`

**Parity with Roy:** Identical error messages, validation logic, CLI behavior. Both toolchains tested against all 74 shared fixtures.

**Orchestration log:** `.squad/orchestration-log/2026-04-25T17-30-pris.md`

**Status:** Ready for production release via PyPI. Coordinated breaking change with Roy (.NET). Both implementations follow user directives identically.

### Plugin-as-Catalog Refactor (Darrel Directive)

**Directive:** A plugin is NOT represented by a plugin-specific media type. A plugin is a nested ai-catalog entry whose url points to the manifest.

**Changes made:**
- `converter.py`: Replaced `CLAUDE_PLUGIN_MEDIA_TYPE` ("application/vnd.claude.code-plugin+json") with `AI_CATALOG_MEDIA_TYPE` ("application/ai-catalog+json"). Kept `CLAUDE_PLUGIN_URN_PREFIX` unchanged.
- `test_cli.py`: Updated media type assertion in `test_convert_single_plugin`.
- `test_integration.py`: Updated import from `CLAUDE_PLUGIN_MEDIA_TYPE` → `AI_CATALOG_MEDIA_TYPE`, all assertion references.
- `testcases/marketplace-expected.json`: All 3 entries and expected assertions updated to `application/ai-catalog+json`.
- Non-marketplace fixtures (`claude-plugin-entry.json`, `mixed-media-types.json`, `spec-example-multi-artifact.json`) still reference old media type — these are Leon/Roy's domain and not marketplace conversion outputs.

### Plugin-as-Catalog Refactor (Darrel Directive)

**Directive:** A plugin is NOT represented by a plugin-specific media type. A plugin is a nested ai-catalog entry whose url points to the manifest.

**Changes made:**
- `converter.py`: Replaced `CLAUDE_PLUGIN_MEDIA_TYPE` ("application/vnd.claude.code-plugin+json") with `AI_CATALOG_MEDIA_TYPE` ("application/ai-catalog+json"). Kept `CLAUDE_PLUGIN_URN_PREFIX` unchanged.
- `test_cli.py`: Updated media type assertion in `test_convert_single_plugin`.
- `test_integration.py`: Updated import from `CLAUDE_PLUGIN_MEDIA_TYPE` → `AI_CATALOG_MEDIA_TYPE`, all assertion references.
- `testcases/marketplace-expected.json`: All 3 entries and expected assertions updated to `application/ai-catalog+json`.
- Non-marketplace fixtures (`claude-plugin-entry.json`, `mixed-media-types.json`, `spec-example-multi-artifact.json`) still reference old media type — these are Leon/Roy's domain and not marketplace conversion outputs.

**Test results:** 355 tests passing, ruff clean (0 errors).

**Orchestration log:** `.squad/orchestration-log/2026-04-26T022700Z-pris.md`

**Status:** Ready for coordinated production release with Roy (.NET). Plugin model now unified across both implementations.
