# Tyrell — History

## Project Context
- **Project:** ai-catalog — SpecWorks Part implementing the AI Card specification
- **Spec:** https://agent-card.github.io/ai-card/ (AI Catalog: JSON format for discovering heterogeneous AI artifacts)
- **Languages:** .NET, Python
- **User:** Darrel Miller (from Dev Box)
- **Factory upstream:** spec-works/factory-squad (spec-reading skill inherited)

## Learnings

Team formed 2026-04-02. Initial setup — no code yet.

### 2026-04-02 — Full Spec Extraction Complete

**Spec structure:** The AI Card spec (~65K chars) has these major sections:
- Core data model: AICatalog → CatalogEntry, HostInfo, CollectionRef, Publisher
- Trust layer: TrustManifest → TrustSchema, Attestation, ProvenanceLink
- Verification Procedures: digest format, JWS signatures, key resolution
- Organization: Bundles (nested catalogs via entries) vs Collections (top-level hierarchy)
- Discovery: well-known URI `/.well-known/ai-catalog.json`, Link relation `rel="ai-catalog"`
- Conformance Levels: L1 Minimal, L2 Discoverable, L3 Trusted
- Informative appendices: OCI mapping, MCP Registry mapping, Claude Code Plugin mapping

**Key interpretation decisions (9 total, documented in decisions/inbox/tyrell-spec-requirements.md):**
- `inline: null` treated as absent (TD-1)
- specVersion accepts any string, warns on non-Major.Minor (TD-2)
- Mixed versioned/unversioned same-identifier is valid but warned (TD-3)
- identity/identifier match uses exact string comparison (TD-4)
- Weak digest = md5, sha1; unknown algorithms accepted with warning (TD-5)
- Open model — unknown fields preserved, never rejected (TD-6)
- Conformance: both auto-detect and validate-against APIs (TD-7)
- Bundle inline validated recursively only in validation mode (TD-8)
- Appendices are informative, not normative (TD-9)

**Edge cases found:** 15 categories, see docs/requirements.md §Edge Cases. Highlights:
- url+inline mutual exclusivity is the most common validation check
- Multi-version uniqueness has subtle mixed-versioned/unversioned corner case
- Trust identity binding (MUST match) is the hardest validation rule
- Digest algorithm rejection requires maintaining a weak-algorithm list
- Nested bundle depth enforcement requires recursion tracking

**Requirements count:** ~80 individual requirements extracted across all sections. 108+ test cases described.

**CDDL schema:** Normative schema included in spec — all model types should match exactly.

**Factory patterns applied:** Used spec-reading skill (D005 shared test fixtures, RFC 2119 mapping, test case format).

### 2026-04-02: Deckard's Architecture ADRs (ADR-001 through ADR-008) Ready

**Architecture ADRs produced:** 8 comprehensive decisions covering project structure, domain model, validation, parsing, CLI, cross-language consistency, test case design, and specs.json shape.

**Impact on Tyrell's work:** ADR-002 (domain model 9 types) aligns with spec structure extracted. ADR-003 (3 conformance levels) matches spec definition. ADR-007 (test wrapper format) frames how to structure the 108 test cases identified.
