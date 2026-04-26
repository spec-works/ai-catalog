# Leon — History

## Project Context
- **Project:** ai-catalog — SpecWorks Part implementing the AI Card specification
- **Spec:** https://agent-card.github.io/ai-card/ (AI Catalog: JSON format for discovering heterogeneous AI artifacts)
- **Languages:** .NET, Python
- **User:** Darrel Miller (from Dev Box)
- **Factory upstream:** spec-works/factory-squad (test-case-authoring skill inherited)

## Learnings

Team formed 2026-04-02. Initial setup — no code yet.

### 2026-04-02 — Full test fixture suite created (64 fixtures)

**Delivered:** 28 positive fixtures, 34 negative fixtures (33 JSON + 1 plain text), 2 marketplace conversion fixtures, plus README.

**Test patterns discovered:**
- **Inline content diversity is critical.** The spec says `inline` is "any JSON value" — must test object, string, number, array, and boolean. Five separate fixtures for this because implementations often assume inline is always an object.
- **Negative tests should isolate one error.** Early temptation to combine "missing publisher identifier AND displayName" into one fixture, but split them. One fixture per error condition makes failure diagnosis trivial for Roy and Pris.
- **Bundle validation is two-level.** A bundle entry's inline value must itself be a valid AI Catalog. Need both "valid nested catalog" (positive) and "invalid nested catalog" (negative) fixtures.
- **Trust identity binding is a P0 rule that's easy to miss.** `trustManifest.identity` MUST match `entry.identifier` — this cross-field validation requires a dedicated fixture.
- **Multi-version uniqueness has three cases:** same id + no version (MV-3), same id + same version (MV-2), and three entries where two clash (MV-2 variant). All three need separate fixtures.
- **Digest validation goes beyond format.** Not just `algorithm:hex` format — must also reject weak algorithms (SHA-1, MD5) per VD-3.
- **Marketplace conversion needs explicit mapping rules.** Documented the field mapping (plugin.name → `urn:claude:plugins:{name}`, etc.) in the expected fixture so implementations agree on the transformation.
- **Extension fields (x-*) must survive round-trip.** Separate fixture for this since it tests the "open model" behavior distinct from metadata.
- **Conformance levels map cleanly to fixture sets:** minimal-catalog.json (L1), discoverable-catalog.json (L2), trusted-catalog.json (L3). Each level's fixture includes everything from lower levels.

**Fixture design decisions:**
- Used realistic identifiers (URN, DID, HTTPS) with fictional but plausible organizations (Contoso, Fabrikam, Woodgrove, etc.)
- Every fixture has `test_ids` array linking to Tyrell's test case descriptions
- Every fixture has `spec_section` for traceability
- Negative fixtures use descriptive `expected_error` strings — implementations should pattern-match, not exact-match
- The `expected` object uses snake_case for assertion keys to be language-neutral

### 2026-04-02: Ready to Author Test Cases

**Architecture and spec analysis complete.** Both Deckard (ADR-007: test case design format) and Tyrell (108 test case descriptions from spec) have deliverables ready for Leon.

**ADR-007 test format:** Wrapper with description, spec_section, input, expected/expected_error.

**From Tyrell's spec extraction:** 108 test cases described across 15 edge case categories. ~80 requirements extracted.

**Target test count:** 20+ positive, 14+ negative, 2+ marketplace conversion pairs = 36+ fixtures total in `testcases/`.

**Next step:** Leon writes all test fixtures per ADR-007 format before Roy/Pris begin parsing/validation implementation (Phase 2 → Phase 3 dependency).

### 2026-04-17 — PR #33 Spec Delta: Major Fixture Overhaul (74 fixtures)

**Delivered:** Comprehensive test fixture update for specification PR #33. All fixtures updated for breaking changes (collections removed, inline→data rename). New fixtures added for version handling, metadata extensibility, security considerations, and new spec examples.

**DELETIONS (4 files):**
- `spec-example-collections.json` — spec removed collections concept
- `empty-collections.json` — collections removed
- `entries-and-collections.json` — collections removed
- `negative/missing-collection-fields.json` — collections removed

**RENAMES (4 files):**
- All `inline` → `data` throughout fixtures (22 files affected)
- `nested-bundle.json` → `nested-catalog.json` — "bundle" terminology replaced
- `cross-bundle-identifier-reuse.json` → `cross-catalog-identifier-reuse.json`
- `negative/both-url-and-inline.json` → `negative/both-url-and-data.json`
- `negative/invalid-bundle-inline.json` → `negative/invalid-nested-catalog.json`

**UPDATES (all existing fixtures):**
- Media type changes: `application/mcp-server+json` → `application/mcp-server-card+json` (8 files)
- Depth limit: 8 → 4 in nested catalog validation
- "Bundle" → "nested catalog entry" terminology (descriptions, identifiers, tags, expected values)
- `has_inline`/`inline_is_*` → `has_data`/`data_is_*` in expected assertions
- `is_bundle` → `is_nested_catalog` in expected assertions
- README updated with new fixture list, correct counts, removed collection references

**NEW FIXTURES (10 files):**

Version Handling (VH-*):
- `version-1.0.json` — valid Major.Minor format (VH-P01)
- `version-1.1.json` — forward compatibility, same major version (VH-P02)
- `negative/version-unsupported-major.json` — reject 2.0 (VH-N01)
- `negative/version-no-minor.json` — reject "1" format (VH-N02)
- `negative/version-three-segments.json` — reject "1.0.0" (VH-N03)
- `negative/version-negative.json` — reject negative version (VH-N04)
- `negative/version-non-integer.json` — reject "a.b" (VH-N05)

Metadata Extensibility (ME-*):
- `metadata-reverse-dns.json` — reverse-DNS vendor keys (ME-P01)
- `metadata-all-types.json` — all JSON value types (ME-P02)
- `negative/metadata-empty-key.json` — reject empty string keys (ME-N01)

Data Field (DA-*):
- `negative/data-null.json` — data: null treated as absent (DA-N03)
- (DA-P01..DA-P05 already covered by existing inline-* fixtures, now renamed)

Security & Nesting (SC-*, NC-*):
- `nested-depth-at-limit.json` — valid depth=4 (SC-P01)
- `negative/depth-exceeds-limit.json` — depth=5 exceeds limit (SC-N02)

New Spec Examples:
- `dual-protocol-agent.json` — agent supporting MCP + A2A via nested catalog (EX-P03)
- `hierarchical-catalog.json` — replaces collections pattern with nested catalogs (EX-P04)

**Key patterns discovered:**
- **Clean break on inline→data.** User decision: do NOT accept `inline` as deprecated alias. Implementations must only recognize `data`. This is a breaking change requiring coordination.
- **Depth limit 8→4 is a significant tightening.** Existing catalogs with depth 5-8 will now produce warnings. This is a normative SHOULD, not a MUST, so implementations warn rather than reject.
- **Version format is now strict.** Previously lenient (TD-2: "accept any string, warn"), now normative Major.Minor integers only (VH-1). This prevents ambiguous "1.0.0" or "v1.0" versions.
- **Metadata key validation is new.** Empty string keys were implicitly allowed before; now explicitly rejected (ME-2). Implementations must validate metadata key non-emptiness.
- **User decision on VH-2 tension.** Spec says "MUST ignore unrecognized fields" for forward compatibility. Our TD-6 closed-model decision now needs revisiting — flag for Deckard/Darrel.

**Final count:** 32 positive + 40 negative + 2 marketplace = 74 fixtures (was 64). Net +10 fixtures despite deleting 4.

### 2026-04-25T17:30 — PR #33 Fixture Update Complete

**Delivered:** All 74 fixtures updated and validated. Clean break on `inline`→`data` implemented per user decision.

**User decisions followed:**
1. Adopt spec's MUST-ignore rule (VH-2). No closed-model warnings.
2. Clean break on `inline`→`data`. No backward compatibility fixtures.

**Fixture strategy aligned:** Leon's decision (clean break) affirmed by Copilot user directive. All 74 fixtures now serve as the source-of-truth contract: only `data` field is valid.

**Roy and Pris implementation:** Both agents consumed all 74 fixtures successfully. Roy: 210 tests passing (168 lib + 42 CLI). Pris: 355 tests passing, ruff clean.

**Orchestration log:** `.squad/orchestration-log/2026-04-25T17-30-leon.md`
