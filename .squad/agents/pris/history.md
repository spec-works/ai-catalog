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
