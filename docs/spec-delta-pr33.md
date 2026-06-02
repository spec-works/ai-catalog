# Spec Delta Report: PR #33

**PR:** [Agent-Card/ai-catalog#33](https://github.com/Agent-Card/ai-catalog/pull/33) — "spec: Updates based on discussions during last meeting"
**Issue:** [Agent-Card/ai-card#28](https://github.com/Agent-Card/ai-catalog/issues/28)
**Analyzed by:** Tyrell (Spec Reader)
**Date:** 2026-04-17
**Baseline:** Our current implementation (80+ requirements, 108 test cases, 9 interpretation decisions)

---

## Executive Summary

PR #33 implements four decisions from the 2026-04-02 spec meeting. Three are **breaking changes** to the data model. One is additive. The spec also gained substantial new normative content (Version Handling, Metadata Extensibility, expanded Security Considerations) and numerous new examples.

**Impact level: HIGH.** Every model type, parser, serializer, validator, test fixture, and CLI command in both .NET and Python needs updates.

---

## 1. REMOVED: `collections` (Top-Level Array)

**Spec section:** § Top-Level Structure, § Organizing Catalogs, § CDDL Schema
**RFC 2119 level:** Was OPTIONAL (MAY)
**What changed:** The entire `collections` concept is removed. The top-level `collections` array, the `CollectionRef` type, and all references to collections are deleted.

### Impact on Our Implementation

| Area | What to Change |
|------|---------------|
| **Model** | Delete `CollectionRef` type entirely. Remove `collections` property from `AiCatalog`. |
| **CDDL** | Remove `CollectionRef` definition and `? collections: [* CollectionRef]` from `AICatalog`. |
| **Parser** | Stop parsing `collections`. With closed model (TD-6), unknown `collections` key on a 1.0 document should produce a warning. |
| **Serializer** | Stop emitting `collections`. |
| **Validator** | Remove all collection validation (CR-1 through CR-7 are **obsolete**). Remove Level 2 check "MAY include collections". |
| **Conformance** | Level 2 no longer mentions collections. |
| **Test fixtures** | Delete: `spec-example-collections.json`, `discoverable-catalog.json` (if it references collections), `negative/missing-host-display-name.json` (if collection-specific). Update `all-properties.json` to remove `collections`. |
| **Test cases** | Mark **OBSOLETE**: CR-P01..CR-P04, CR-N01, CR-N02, CL-P05 (collections at L2). TL-P02 (had `collections` in "all optional fields"). |
| **CLI** | Remove any collection traversal logic from `explore` command. |

### Affected Requirements (DELETE)

| Old ID | Requirement | Action |
|--------|------------|--------|
| TL-5 | `collections` is OPTIONAL | **DELETE** |
| CR-1 | CollectionRef MUST contain displayName | **DELETE** |
| CR-2 | CollectionRef MUST contain url | **DELETE** |
| CR-3 | Document at collection url MUST be valid AI Catalog | **DELETE** |
| CR-4 | Collection description is OPTIONAL | **DELETE** |
| CR-5 | Collection tags is OPTIONAL | **DELETE** |
| CR-6 | Collections are recursive | **DELETE** |
| CR-7 | Clients SHOULD impose max traversal depth for collections | **DELETE** |
| CL-3 | Level 2 MAY include collections | **MODIFY** — remove collections mention |
| EC-9 | Collection edge cases (all) | **DELETE** |

---

## 2. CHANGED: `inline` → `data` (Field Rename)

**Spec section:** § Catalog Entry, § CDDL Schema, all examples
**RFC 2119 level:** MUST (part of the url/data exclusive-or)
**What changed:** The `inline` field on CatalogEntry is renamed to `data`. The semantics are identical — it holds a JSON value containing the complete artifact document. The CDDL changes from `(url: text // inline: any)` to `(url: text // data: any)`.

### Impact on Our Implementation

| Area | What to Change |
|------|---------------|
| **Model** | Rename `Inline` property → `Data` on CatalogEntry type. |
| **CDDL** | Update `(url: text // inline: any)` → `(url: text // data: any)`. |
| **Parser** | Parse `"data"` instead of `"inline"`. For backward compatibility, consider also accepting `"inline"` with a deprecation warning. |
| **Serializer** | Emit `"data"` instead of `"inline"`. |
| **Validator** | Update CE-5: "MUST contain exactly one of `url` or `data`". Update mutual exclusivity check to use `data`. |
| **Test fixtures** | Update ALL fixtures using `"inline"` → `"data"`. Includes: `inline-artifact.json`, `nested-bundle.json`, `all-properties.json`, `negative/both-url-and-inline.json` (rename to `both-url-and-data.json`), `negative/missing-entry-content.json`. |
| **Test cases** | Update: CE-P05, CE-P06 (inline → data), CE-N03, CE-N04. TL-P02. |
| **Edge cases** | EC-2 and EC-10: Replace all `inline` references with `data`. |
| **Decision TD-1** | Update: `data: null` treated as absent (was `inline: null`). |

### Affected Requirements (MODIFY)

| Old ID | Old Text | New Text |
|--------|---------|----------|
| CE-5 | Entry MUST contain exactly one of `url` or `inline` | Entry MUST contain exactly one of `url` or `data` |
| CE-8 | `inline` — a JSON value containing the complete artifact document inline | `data` — a JSON value containing the complete artifact document inline |
| CD-2 | `(url: text // inline: any)` | `(url: text // data: any)` |
| BU-1 | Entry content via `url` or `inline` | Entry content via `url` or `data` |

---

## 3. CHANGED: Nesting Depth Limit 8 → 4

**Spec section:** § Organizing Catalogs (Nested Catalog Entries), § Security Considerations
**RFC 2119 level:** RECOMMENDED (SHOULD)
**What changed:** The recommended maximum nesting depth changed from 8 to 4. This appears in both the organizing catalogs section and security considerations.

### Impact on Our Implementation

| Area | What to Change |
|------|---------------|
| **Validator** | Change default max depth from 8 to 4. |
| **Constants** | Update `DEFAULT_MAX_NESTING_DEPTH = 4`. |
| **Test cases** | Update: BU-P02 (depth=8 → now exceeds limit, change to depth=4). BU-N01 (depth=9 → change to depth=5). |
| **Documentation** | Update any references to "recommended depth of 8". |

### Affected Requirements (MODIFY)

| Old ID | Old Text | New Text |
|--------|---------|----------|
| BU-3 | A depth limit of 8 is RECOMMENDED | A depth limit of 4 is RECOMMENDED |
| SC-3 | A maximum depth of 8 is RECOMMENDED | A maximum depth of 4 is RECOMMENDED |

---

## 4. REMOVED: "Bundle" Terminology

**Spec section:** § Organizing Catalogs
**RFC 2119 level:** N/A (terminology change)
**What changed:** The spec no longer uses the term "bundle." The section "Bundles (Nested Catalog Entries)" is now just "Nested Catalog Entries." Bundles are not a distinct concept anymore — they're just catalog entries with `mediaType: application/ai-catalog+json`. The concept of "multi-artifact packaging" is now described as an entry with a `publisher` containing a nested catalog.

### Impact on Our Implementation

| Area | What to Change |
|------|---------------|
| **Code comments/naming** | Rename any "bundle" references to "nested catalog entry". |
| **Test fixtures** | Rename `nested-bundle.json` → `nested-catalog.json` (or similar). |
| **Validator** | No functional change — validation logic is the same, just terminology. |
| **Documentation** | Replace "bundle" with "nested catalog entry" throughout. |
| **Requirements IDs** | BU-* prefix is misleading now. Consider renaming to NC-* (Nested Catalog). |

### Affected Requirements (RENAME)

| Old ID | New ID | Requirement |
|--------|--------|------------|
| BU-1 | NC-1 | A catalog entry whose `mediaType` is `application/ai-catalog+json` references/embeds another AI Catalog |
| BU-2 | NC-2 | Clients processing nested catalogs SHOULD impose max nesting depth |
| BU-3 | NC-3 | A depth limit of 4 is RECOMMENDED |
| BU-4 | NC-4 | Entry inside nested catalog MAY reuse same identifier as entry elsewhere |

---

## 5. NEW: Version Handling Section

**Spec section:** § Version Handling (new normative section)
**RFC 2119 level:** Mixed MUST/SHOULD
**What changed:** Entirely new section defining version format, compatibility rules, consumer behavior, and producer behavior for `specVersion`.

### New Requirements

| New ID | Requirement | Priority | Implementation Notes |
|--------|------------|----------|---------------------|
| VH-1 | `specVersion` value is "Major.Minor" string (e.g., "1.0", "1.1", "2.0"). Major and minor components are non-negative integers. | P0 | Validate format strictly. This supersedes TD-2 (which was lenient). |
| VH-2 | Minor version increments: spec adds new OPTIONAL fields. Documents are backward-compatible within same major version. Consumers MUST ignore unrecognized fields. | P0 | This is a normative MUST for field ignoring within same major version. **Contradicts our closed model decision (TD-6).** See [Interpretation Decision](#interpretation-decisions) below. |
| VH-3 | Major version increments: breaking changes. Consumers that do not support the major version SHOULD reject with informative error. | P1 | Implement major version check. |
| VH-4 | Consumers SHOULD parse specVersion before processing. | P1 | Parse specVersion first in parsing pipeline. |
| VH-5 | Consumers SHOULD accept documents whose major version matches, regardless of minor version. | P1 | Don't reject on minor version mismatch. |
| VH-6 | Consumers SHOULD reject documents whose major version is higher than supported, with informative error. | P1 | Implement version-too-high rejection. |
| VH-7 | Producers MUST set specVersion to the version they implement. | P0 | Serializer must emit correct specVersion. |
| VH-8 | Producers SHOULD NOT set specVersion higher than they conform to. | P1 | Advisory for serialization API. |

### Impact on Our Implementation

| Area | What to Change |
|------|---------------|
| **Parser** | Add specVersion parsing as first step. Parse Major.Minor format. |
| **Validator** | Add version validation: check Major.Minor format, check major version compatibility, warn/reject on unsupported major version. |
| **Model** | Consider parsed `(major, minor)` tuple alongside raw string. |
| **Test cases** | NEW test cases needed for version handling (see §Test Cases below). |
| **Decision TD-2** | **UPDATE**: specVersion format is now explicitly defined as Major.Minor. Stricter than our previous "accept any string, warn on non-Major.Minor". |

---

## 6. NEW: Metadata Extensibility Section

**Spec section:** § Metadata Extensibility (new normative section)
**RFC 2119 level:** Mixed MUST/SHOULD/MAY/RECOMMENDED
**What changed:** New section defining how `metadata` properties work across objects, key naming conventions, reserved keys policy, and value types.

### New Requirements

| New ID | Requirement | Priority | Implementation Notes |
|--------|------------|----------|---------------------|
| ME-1 | `metadata` appears on AICatalog, CatalogEntry, and TrustManifest as single extension point | P0 | Already implemented. Confirms current behavior. |
| ME-2 | Metadata keys MUST be non-empty strings | P0 | Add validation: reject empty-string keys in metadata maps. |
| ME-3 | Reverse-DNS prefix RECOMMENDED for vendor-specific keys | P1 | Advisory — no enforcement needed. |
| ME-4 | Consumers MAY ignore metadata entries that shadow standard fields | P2 | No enforcement needed. |
| ME-5 | No metadata keys reserved by this spec. Future versions MAY promote keys. | P2 | Informational. |
| ME-6 | Promoted metadata key SHOULD be retained for backward compatibility | P1 | Informational for future. |
| ME-7 | Metadata values MAY be any valid JSON type | P2 | Already supported (open map). |
| ME-8 | Consumers that do not recognize a metadata key SHOULD ignore it | P1 | Already supported. |

### Impact on Our Implementation

| Area | What to Change |
|------|---------------|
| **Validator** | Add: reject empty-string metadata keys (ME-2). Warn on keys shadowing standard fields (ME-4, optional). |
| **Test cases** | NEW: metadata with empty key → reject. Metadata with reverse-DNS keys → accept. Metadata shadowing standard field → warn. |

---

## 7. NEW: Expanded Security Considerations

**Spec section:** § Security Considerations (significantly expanded)
**RFC 2119 level:** Mixed MUST/SHOULD
**What changed:** The security section was restructured from 2 brief subsections into 7 detailed subsections: Trust Layers (4-layer model), Nested Catalog Depth and Circular References, Catalog Poisoning, Identifier Typosquatting, Stale Attestations, Embedded Content Safety, Privacy Considerations.

### New Requirements

| New ID | Requirement | Priority | Implementation Notes |
|--------|------------|----------|---------------------|
| SC-6 | Consumers SHOULD verify signatures when present and SHOULD reject Trust Manifests whose signature does not validate | P1 | Already tracked (VS-*). Now explicit in security section. |
| SC-7 | Consumers SHOULD treat Trust Manifest content as advisory (not authoritative) at Layer 0/1 | P1 | Advisory. Validation mode could warn. |
| SC-8 | Clients SHOULD track visited catalog URLs during recursive resolution and reject already-fetched URLs | P1 | **NEW**: Circular reference detection. Must track URL set during traversal. |
| SC-9 | When `data` contains embedded content, consumers MUST treat it as untrusted input | P0 | Already implied by CE-8 opaqueness. |
| SC-10 | Content with HTML/script-capable media types MUST be sandboxed, MUST NOT be executed in consumer's security context | P0 | Relevant for CLI `install` command. |
| SC-11 | Consumers SHOULD validate `data` content is well-formed JSON before processing | P1 | Add well-formedness check for `data` during validation. |
| SC-12 | Consumers SHOULD check `updatedAt` to assess attestation freshness | P1 | Advisory for trust verification consumers. |

### Impact on Our Implementation

| Area | What to Change |
|------|---------------|
| **Validator** | Add circular URL tracking in nested catalog traversal (SC-8). |
| **Validator** | Add `data` content well-formedness check (SC-11). |
| **CLI** | Ensure `install` command sandboxes content appropriately (SC-10). |
| **Test cases** | NEW: circular reference detection test. `data` with non-JSON media type handling. |

### Removed Requirement

| Old ID | Old Text | Action |
|--------|---------|--------|
| SC-1 | "AI Catalogs, artifacts, and Trust Manifests MUST be served over HTTPS" (as a standalone section) | **MODIFIED**: Now part of Layer 0 description. The HTTPS requirement is still present but described as transport security layer, not a standalone MUST. The normative language is weaker: "served over HTTPS" is described as a trust layer, not a global MUST. |

---

## 8. CHANGED: Media Type Updates in Examples

**Spec section:** All examples throughout the spec
**RFC 2119 level:** Informative (examples)
**What changed:**

| Old Media Type | New Media Type |
|----------------|---------------|
| `application/mcp-server+json` | `application/mcp-server-card+json` |
| `application/ai-skill` | `application/agentskill+zip` |

These appear in the well-known media type list (§ Catalog Entry) and all examples.

### Impact on Our Implementation

| Area | What to Change |
|------|---------------|
| **Test fixtures** | Update any media types in test fixtures that used old values. |
| **CLI** | Update MCP auto-detection (Pris CLI-D5) — look for `mcp-server-card` instead of `mcp-server`. |
| **Documentation** | Update media type references. |

---

## 9. NEW: Additional Examples

**Spec section:** Multiple new example sections
**What changed:** The spec gained several new inline examples and restructured its normative examples:

| Example | Status | Notes |
|---------|--------|-------|
| Host Info example (JSON) | **NEW** | Shows Host with all fields including data URI logo |
| Multi-Version Entries example (JSON) | **NEW** | Two versions of same agent |
| Trust Manifest example (JSON) | **NEW** | Complete trust manifest with attestations, provenance, signature |
| Trust Schema example (JSON) | **NEW** | Standalone trust schema |
| Attestation example (JSON) | **NEW** | SOC2 attestation with digest |
| Provenance Link example (JSON) | **NEW** | Full provenance with registryUri, signatureRef |
| Hierarchical Catalog example | **REPLACES** | Old "Catalog with Collections" → now uses nested catalog entries |
| Dual-Protocol Agent example | **NEW** | Shows MCP + A2A in single entry via nested catalog with `data` |
| Multi-Artifact Catalog example | **MODIFIED** | Uses `data` instead of `inline`, updated media types |
| Claude Code Plugin entry example | **REPLACED** | Old Endor Labs example → new generic examples with updated media types |

### Impact on Our Implementation

| Area | What to Change |
|------|---------------|
| **Test fixtures** | Consider adding test fixtures based on new examples (especially dual-protocol agent, hierarchical catalog). |
| **Test cases** | Add test cases for dual-protocol pattern (nested catalog in `data`). |
| **Fixtures to remove** | `spec-example-collections.json` → replace with `spec-example-hierarchical.json`. Old Claude plugin example → replace with new format. |

---

## 10. NEW: IANA Well-Known URI Registration

**Spec section:** § IANA Considerations
**RFC 2119 level:** Informative
**What changed:** Added formal IANA registration for `ai-catalog.json` well-known URI.

### Impact: None on code. Informational only.

---

## 11. NEW: MCP Server Cards (SEP-1649) Relationship

**Spec section:** § Mapping to MCP Registry (new subsection)
**What changed:** New subsection explaining the difference between MCP Registry `server.json` and MCP Server Cards. Notes that `server.json` uses `application/json` (no registered media type) while Server Cards use `application/mcp-server-card+json`.

### Impact on Our Implementation

| Area | What to Change |
|------|---------------|
| **CLI** | Auto-detect logic should handle both `application/json` (registry server.json) and `application/mcp-server-card+json` (Server Card) for MCP entries. |

---

## 12. CHANGED: Data Model Diagram

**Spec section:** § Data Model Overview
**What changed:**
- Removed `CollectionRef` class and `AICatalog --> "*" CollectionRef : collections` relationship
- Changed `url | inline` → `url | data` on CatalogEntry
- Changed `CatalogEntry --> "0..1" AICatalog : bundle` → `CatalogEntry --> "0..1" AICatalog : nested`
- Removed `CollectionRef ..> AICatalog : references`

### Impact: Update any generated diagrams or documentation referencing the data model.

---

## CDDL Schema Changes Summary

### Removed
```cddl
; DELETED entirely
CollectionRef = {
  displayName: text,
  url: text,
  ? description: text,
  ? tags: [* text]
}
```

### Changed
```cddl
; BEFORE
AICatalog = {
  specVersion: text,
  ? host: HostInfo,
  entries: [* CatalogEntry],
  ? collections: [* CollectionRef],
  ? metadata: { * text => any }
}

; AFTER
AICatalog = {
  specVersion: text,
  ? host: HostInfo,
  entries: [* CatalogEntry],
  ? metadata: { * text => any }
}
```

```cddl
; BEFORE
CatalogEntry = {
  ...
  (url: text // inline: any),
  ...
}

; AFTER
CatalogEntry = {
  ...
  (url: text // data: any),
  ...
}
```

### Unchanged
HostInfo, Publisher, TrustManifest, TrustSchema, Attestation, ProvenanceLink — all unchanged.

---

## Interpretation Decisions

### TD-2 UPDATE: specVersion Format Now Normative

**Previous decision:** Accept any non-empty string; warn on non-Major.Minor in strict mode.
**New spec text:** `specVersion` is explicitly defined as "Major.Minor" format with non-negative integer components.
**New decision:** Parse and validate Major.Minor format. Reject non-conforming values (empty string, non-integer components). This is stricter than before.

### TD-6 TENSION: Closed Model vs. Version Handling

**Previous decision (user override):** Closed model — unknown fields produce warnings.
**New spec text (VH-2):** "Consumers MUST ignore unrecognized fields" (within same major version, per minor version compatibility rules).
**Interpretation:** The spec now explicitly says MUST ignore unrecognized fields for forward compatibility within a major version. This **conflicts** with our closed-model decision. However, our closed-model decision was about schema enforcement for known spec versions. Proposed resolution: within a known major version, accept unknown fields silently (per VH-2). For unknown major versions, reject (per VH-6). Metadata remains the designated extension point.

**⚠️ This needs a team decision.** Flag for Deckard/Darrel.

### TD-NEW: `data` Backward Compatibility

**Question:** Should parsers accept both `inline` and `data` for backward compatibility with pre-PR33 documents?
**Recommendation:** Yes, accept `inline` as a deprecated alias for `data` with a warning. Serialize as `data` only. This allows existing documents to be read during migration.

---

## New Test Cases Needed

### Version Handling (VH-*)

| Test ID | Description | Expected |
|---------|------------|----------|
| VH-P01 | specVersion "1.0" | Accept |
| VH-P02 | specVersion "1.1" (higher minor) | Accept (same major) |
| VH-N01 | specVersion "2.0" (unsupported major) | Reject with informative error |
| VH-N02 | specVersion "1" (no minor) | Reject: invalid format |
| VH-N03 | specVersion "1.0.0" (extra segment) | Reject: invalid format |
| VH-N04 | specVersion "-1.0" (negative) | Reject: invalid format |
| VH-N05 | specVersion "a.b" (non-integer) | Reject: invalid format |

### Metadata Extensibility (ME-*)

| Test ID | Description | Expected |
|---------|------------|----------|
| ME-P01 | Metadata with reverse-DNS keys | Accept |
| ME-P02 | Metadata with any JSON value types | Accept |
| ME-N01 | Metadata with empty-string key | Reject |

### Security — Circular References (SC-*)

| Test ID | Description | Expected |
|---------|------------|----------|
| SC-P01 | Nested catalogs within depth limit | Accept |
| SC-N01 | Circular reference A→B→A | Reject |
| SC-N02 | Nested catalog depth > 4 | Reject/warn |

### Embedded Content Safety (ECS-*)

| Test ID | Description | Expected |
|---------|------------|----------|
| ECS-P01 | `data` with valid JSON object | Accept |
| ECS-N01 | `data` with malformed content (when mediaType suggests JSON) | Warn |

### Data Field (replaces Inline tests)

| Test ID | Description | Expected |
|---------|------------|----------|
| DA-P01 | Entry with `data` (JSON object) | Accept |
| DA-P02 | Entry with `data` (JSON array) | Accept |
| DA-P03 | Entry with `data` (JSON string) | Accept |
| DA-P04 | Entry with `data` (JSON number) | Accept |
| DA-P05 | Entry with `data` (boolean) | Accept |
| DA-N01 | Entry with both `url` and `data` | Reject |
| DA-N02 | Entry with neither `url` nor `data` | Reject |
| DA-N03 | Entry with `data: null` | Treat as absent (TD-1 updated) |

---

## Updated Test Cases (Modifications)

| Old Test ID | Change |
|-------------|--------|
| TL-P02 | Remove `collections` from "all optional fields" |
| CE-P05, CE-P06 | Rename `inline` → `data` in input sketch |
| CE-N03 | "Both url and inline" → "Both url and data" |
| CE-N04 | "Neither url nor inline" → "Neither url nor data" |
| BU-P01 | Rename test — no longer "bundle", now "nested catalog entry" |
| BU-P02 | Depth limit 8→4 threshold change |
| BU-N01 | Depth exceeds 4 (was 8) |
| CR-* (all) | **DELETE** — collections removed |
| CL-P05 | Remove collections check from L2 |

---

## Work Items Summary (for Roy, Pris, Leon)

### Roy (.NET) & Pris (Python) — Code Changes

1. **Delete** `CollectionRef` type and all collection-related code
2. **Rename** `Inline` → `Data` on CatalogEntry model
3. **Update** CDDL: remove CollectionRef, update AICatalog, update CatalogEntry
4. **Update** parser: `"data"` instead of `"inline"` (consider `"inline"` backward compat)
5. **Update** serializer: emit `"data"` instead of `"inline"`
6. **Update** validator: `data`/`url` exclusivity, depth limit 8→4
7. **Add** specVersion Major.Minor format validation (VH-1 through VH-8)
8. **Add** metadata key validation: reject empty string keys (ME-2)
9. **Add** circular reference detection in nested catalog traversal (SC-8)
10. **Add** `data` content well-formedness check (SC-11)
11. **Update** CLI media type detection (`mcp-server-card` instead of `mcp-server`)
12. **Update** all code comments/docs replacing "bundle" → "nested catalog"

### Leon (Test Author) — Fixture Changes

1. **Delete** all collection fixtures (`spec-example-collections.json`, collection negatives)
2. **Rename** `inline` → `data` in ALL fixtures
3. **Rename** `nested-bundle.json` → `nested-catalog.json`
4. **Rename** `negative/both-url-and-inline.json` → `negative/both-url-and-data.json`
5. **Update** depth limit test fixtures (8→4)
6. **Update** media types in fixtures (`mcp-server+json` → `mcp-server-card+json`, `ai-skill` → `agentskill+zip`)
7. **Create** new version handling test fixtures (VH-P01..VH-N05)
8. **Create** new metadata extensibility fixtures (ME-P01, ME-N01)
9. **Create** dual-protocol agent fixture (from new spec example)
10. **Create** hierarchical catalog fixture (replaces collections example)
11. **Create** circular reference test fixture

### Deckard (Architect) — Decision Needed

1. **Resolve TD-6 tension**: Closed model vs. VH-2 "MUST ignore unrecognized fields". Propose: adopt spec's MUST ignore within same major version, drop closed-model warnings for unknown fields.
2. **Backward compatibility**: Should parsers accept `inline` as deprecated alias for `data`?

---

## Requirements Count Delta

| Category | Before | After | Δ |
|----------|--------|-------|---|
| Requirements | ~80 | ~85 | +5 net (removed 8 CR-*, added ~13 VH/ME/SC) |
| Test cases | 108 | ~115 | +7 net (removed ~8 collection tests, added ~15 new) |
| Edge cases | 15 categories | 14 categories | -1 (EC-9 collections deleted) |
| CDDL types | 9 | 8 | -1 (CollectionRef deleted) |
| Interpretation decisions | 9 | 10 | +1 (TD-NEW: data backward compat) |
