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

### 2026-04-02 — Spec Feedback Document Produced

**Output:** `docs/spec-feedback.md` — consolidated implementer feedback to spec authors.

**Key themes from implementation feedback:**
1. **String comparison rules** are the biggest gap — identity/identifier matching, digest algorithm naming, and specVersion parsing all need explicit comparison semantics defined.
2. **Open vs. closed model** not stated — affects every parser. CDDL maps are technically closed but real JSON formats need extension. Must be explicit.
3. **Conformance level thresholds** need tightening — L3's "and/or" language is ambiguous (one trustManifest vs. all entries).
4. **Spec example violates own rules** — Claude Code Plugin appendix uses SHA-1 in sourceDigest, which the normative §Digest Format says MUST be rejected.
5. **JSON Schema absence** is a practical gap — the JSON ecosystem runs on JSON Schema; providing one (even informative) would reduce implementation effort significantly.
6. **Mixed versioned/unversioned uniqueness** is a real edge case the spec should address with examples or a truth table.

**Sources used:** All 9 TD decisions, Deckard's 8 ADRs, full requirements checklist (80+ requirements, 15 edge case categories), and direct spec reading. Organized into 6 categories per Darrel's request.

### 2026-04-02T10-55: Spec Feedback Document Completed

**Output:** `docs/spec-feedback.md` finalized — 21 KB, 18 spec issues across 6 categories.

**Used by:** Feedback document consumed as input by Scribe for orchestration logging and cross-team history updates.

**Next:** Feedback submitted to spec authors; complements Deckard's architecture feedback for forward compatibility planning.

### 2026-04-17 — Spec Delta Analysis for PR #33

**Output:** `docs/spec-delta-pr33.md` — comprehensive delta report comparing PR #33 changes against our 80+ requirements baseline.

**Key spec changes in PR #33 (from 2026-04-02 meeting decisions):**
1. **`collections` removed entirely** — CollectionRef type deleted. Hierarchy now achieved exclusively through nested catalog entries (entries with mediaType `application/ai-catalog+json`). Requirements CR-1 through CR-7 are obsolete. EC-9 (collection edge cases) deleted.
2. **`inline` renamed to `data`** — CatalogEntry field rename. CDDL changes from `(url: text // inline: any)` to `(url: text // data: any)`. All examples updated. TD-1 now applies to `data: null`.
3. **Nesting depth limit 8 → 4** — RECOMMENDED max depth changed from 8 to 4 in both organizing catalogs and security sections. Per upstream ADR-0001.
4. **"Bundle" terminology eliminated** — no longer a distinct concept. Just "nested catalog entries." Multi-artifact packaging described as entry with publisher containing nested catalog.
5. **New normative sections:** Version Handling (Major.Minor format, compatibility rules, MUST ignore unrecognized fields within same major), Metadata Extensibility (key naming, empty key rejection), expanded Security Considerations (4-layer trust model, circular reference detection, embedded content safety).
6. **Media type updates in examples:** `application/mcp-server+json` → `application/mcp-server-card+json`, `application/ai-skill` → `application/agentskill+zip`.

**Decisions impacted:**
- TD-2 (specVersion format) now has normative Major.Minor definition — stricter than before.
- TD-6 (closed model) in tension with new VH-2 "Consumers MUST ignore unrecognized fields" — flagged for team decision.
- TD-1 updated: `data: null` instead of `inline: null`.
- New decision needed: backward compatibility for `inline` → `data` migration.

**Requirements delta:** ~80 → ~85 (+5 net). 8 collection reqs deleted, ~13 version/metadata/security reqs added.
**Test case delta:** 108 → ~115 (+7 net). 8 collection tests deleted, ~15 new tests added.
**CDDL types:** 9 → 8 (CollectionRef deleted).

### 2026-04-25T17:30 — Spec PR #33 Update Complete

**Output:** Spec PR #33 delta analysis finalized. Decisions flagged for team review and subsequently resolved.

**What happened:** Tyrell completed comprehensive delta analysis. Two decisions required:
1. TD-6 tension: New VH-2 "MUST ignore unrecognized fields" conflicts with closed-model warnings
2. Backward compatibility: Accept `inline` as deprecated alias?

**User decision (2026-04-25T13:30):**
1. Adopt spec's MUST-ignore rule (VH-2). Supersede TD-6. Drop closed-model warnings.
2. Clean break on `inline`→`data`. No deprecated alias. Only `data` is valid.

**Consequence:** Roy and Pris have explicit direction for implementation. Leon's fixture updates (clean break) aligned with user decision. Both agents proceeded to implementation phase successfully.

**Test results:** Roy (210 tests), Pris (355 tests) — all passing.

**Orchestration log:** `.squad/orchestration-log/2026-04-25T17-30-tyrell.md`

