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
