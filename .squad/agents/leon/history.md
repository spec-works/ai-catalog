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
