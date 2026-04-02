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
