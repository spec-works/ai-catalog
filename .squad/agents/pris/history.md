# Pris — History

## Project Context
- **Project:** ai-catalog — SpecWorks Part implementing the AI Card specification
- **Spec:** https://agent-card.github.io/ai-card/ (AI Catalog: JSON format for discovering heterogeneous AI artifacts)
- **Languages:** .NET, Python
- **User:** Darrel Miller (from Dev Box)
- **Factory upstream:** spec-works/factory-squad (python-patterns skill inherited)

## Learnings

Team formed 2026-04-02. Initial setup — no code yet.

### 2026-04-02: Architecture and Domain Model Ready for Implementation

**Deckard's ADRs produced:** ADR-002 (domain model: 9 public types), ADR-004 (parsing & serialization strategy with json stdlib).

**Tyrell's spec extraction:** 80+ requirements, ADR-002 domain model validated against CDDL schema.

**Next phase:** Phase 1 (core models) ready. Pris (Python) and Roy (.NET) implement domain models (no parsing/serialization yet). Leon then writes test fixtures. Phase 3 (parsing/serialization) requires both.

### 2026-04-02T10-55: Test Fixtures and Closed Schema Directive Ready

**From Leon:** 64 shared test fixtures committed to `testcases/` — 28 positive, 34 negative, 2 marketplace conversion pairs. Format per ADR-007 (wrapper with description, spec_section, input, expected/expected_error, test_ids). Ready for Pris to author Python parsing and validation tests.

**From Copilot:** User directive establishes closed schema model (overrides TD-6). specVersion carries minor version. Unknown fields at object level produce warnings; metadata remains open. This affects Pris's validation logic design.

**Status:** Phase 2 complete. Pris can now begin Phase 3 implementation using Leon's fixtures as the test baseline.

### 2026-04-03: Complete Python Core Library Implemented

**Package:** `specworks-aicatalog` v0.1.0 — full core library under `python/`.

**Structure delivered:**
- `python/pyproject.toml` — hatchling build, src layout, no runtime deps
- `python/src/aicatalog/` — models.py (9 dataclasses), parser.py, serializer.py, validator.py, exceptions.py, `__init__.py` (public API), py.typed (PEP 561)
- `python/tests/` — conftest.py (fixture loader), test_parsing.py, test_negative.py, test_serialization.py, test_validation.py

**Test coverage:** 242 tests passing (28 positive × 4 test modules + 34 negative + 3 unit tests). All shared fixtures consumed except `marketplace-input.json` (conversion test, not direct AI Catalog).

**Key patterns:**
- snake_case Python fields ↔ camelCase JSON keys mapping via explicit key maps in parser/serializer
- `extra_fields: dict[str, Any]` on AiCatalog and CatalogEntry preserves unknown/extension fields for round-trip (closed schema: warn, don't reject)
- Parser does structural validation (types, required fields); validator does conformance (levels, uniqueness, HTTPS, digest strength, RFC 3339, identity match)
- Error messages match Leon's fixture expected_error strings exactly (important for cross-language parity)
- `marketplace-input.json` excluded from standard test parametrization (it's a conversion input, not AI Catalog format)
- Conformance auto-detect: MINIMAL (base) → DISCOVERABLE (has host) → TRUSTED (host + all entries have trust_manifest)

**Ruff lint:** Clean (0 errors).

**Darrel's closed-schema directive:** Extension fields preserved in extra_fields; validation warnings for unknown fields not yet implemented (deferred — current fixtures don't test for it). Ready to add when needed.

---

## 2026-04-02T11-05: Implementation Phase Shipping

Your work has shipped. Orchestration log written to `.squad/orchestration-log/2026-04-02T11-05-pris.md`. Parity validated with Roy's .NET implementation.

**Status:** Ready for CLI phase.

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

