# AI Catalog Specification — Test Case Descriptions

**Specification:** AI Catalog (`application/ai-catalog+json`)
**Spec URL:** https://agent-card.github.io/ai-card/
**Produced by:** Tyrell (Spec Reader)
**Date:** 2026-04-02

These test case descriptions are language-independent. Leon (Test Author) will use them to produce shared JSON fixtures in `testcases/` for all language implementations.

---

## Notation

- **Test ID**: `{Category}-{Number}` (e.g., `TL-P01` = Top-Level Positive 01)
- **P** suffix = positive test (valid input, should succeed)
- **N** suffix = negative test (invalid input, should fail)
- **Input sketch** = minimal JSON structure or description
- **Expected behavior** = parse succeeds / parse fails / validation warning

---

## 1. Top-Level Structure

| Test ID | Description | Input Sketch | Expected Behavior | Spec Ref |
|---------|------------|-------------|-------------------|----------|
| TL-P01 | Minimal valid catalog | `{"specVersion":"1.0","entries":[]}` | Parse succeeds. specVersion="1.0", entries is empty array. | § Top-Level Structure |
| TL-P02 | Catalog with all optional top-level fields | Catalog with `specVersion`, `entries`, `host`, `collections`, `metadata` | Parse succeeds. All fields populated. | § Top-Level Structure |
| TL-P03 | Catalog with empty entries array | `{"specVersion":"1.0","entries":[]}` | Parse succeeds. entries.length == 0. | TL-3 |
| TL-P04 | Catalog with unknown extension fields | `{"specVersion":"1.0","entries":[],"x-custom":"value"}` | Parse succeeds. Unknown field preserved or ignored. | EC-12 |
| TL-N01 | Missing specVersion | `{"entries":[]}` | Validation fails: specVersion required. | TL-1 |
| TL-N02 | Missing entries | `{"specVersion":"1.0"}` | Validation fails: entries required. | TL-2 |
| TL-N03 | specVersion is not a string (number) | `{"specVersion":1.0,"entries":[]}` | Validation fails: specVersion must be string. | TL-1, EC-11 |
| TL-N04 | specVersion is null | `{"specVersion":null,"entries":[]}` | Validation fails: specVersion must be string. | TL-1, EC-3 |
| TL-N05 | entries is not an array (object) | `{"specVersion":"1.0","entries":{}}` | Validation fails: entries must be array. | TL-2, EC-11 |
| TL-N06 | specVersion is empty string | `{"specVersion":"","entries":[]}` | Validation fails: specVersion must not be empty. | EC-3 |
| TL-N07 | Document is not a JSON object (array) | `[{"specVersion":"1.0"}]` | Validation fails: root must be object. | § Top-Level Structure |
| TL-N08 | Document is not JSON at all | `"not json"` (plain text) | Parse fails. | § Top-Level Structure |

---

## 2. Host Info

| Test ID | Description | Input Sketch | Expected Behavior | Spec Ref |
|---------|------------|-------------|-------------------|----------|
| HI-P01 | Host with only displayName | `{"host":{"displayName":"Acme"}, ...}` | Parse succeeds. Host with displayName only. | § Host Info |
| HI-P02 | Host with all optional fields | Host with displayName, identifier, documentationUrl, logoUrl, trustManifest | Parse succeeds. All fields populated. | § Host Info |
| HI-N01 | Host missing displayName | `{"host":{}, ...}` | Validation fails: displayName required on host. | HI-1 |
| HI-N02 | Host with displayName as non-string | `{"host":{"displayName":123}, ...}` | Validation fails: displayName must be string. | HI-1 |

---

## 3. Catalog Entry — Required Fields

| Test ID | Description | Input Sketch | Expected Behavior | Spec Ref |
|---------|------------|-------------|-------------------|----------|
| CE-P01 | Entry with url | Entry: identifier, displayName, mediaType, url | Parse succeeds. | § Catalog Entry |
| CE-P02 | Entry with inline (object) | Entry: identifier, displayName, mediaType, inline (JSON object) | Parse succeeds. inline is opaque object. | § Catalog Entry |
| CE-P03 | Entry with inline (string) | Entry: identifier, displayName, mediaType, inline: "hello" | Parse succeeds. inline is opaque string. | CE-8, EC-10 |
| CE-P04 | Entry with inline (number) | Entry: identifier, displayName, mediaType, inline: 42 | Parse succeeds. inline is opaque number. | CE-8, EC-10 |
| CE-P05 | Entry with inline (array) | Entry: identifier, displayName, mediaType, inline: [1,2,3] | Parse succeeds. inline is opaque array. | CE-8, EC-10 |
| CE-P06 | Entry with inline (boolean) | Entry: identifier, displayName, mediaType, inline: true | Parse succeeds. inline is opaque boolean. | CE-8, EC-10 |
| CE-P07 | Entry with all optional fields | Entry with description, tags, version, updatedAt, metadata, publisher, trustManifest | Parse succeeds. | § Catalog Entry |
| CE-N01 | Entry missing identifier | Entry without identifier | Validation fails: identifier required. | CE-1 |
| CE-N02 | Entry missing displayName | Entry without displayName | Validation fails: displayName required. | CE-3 |
| CE-N03 | Entry missing mediaType | Entry without mediaType | Validation fails: mediaType required. | CE-4 |
| CE-N04 | Entry with both url and inline | Entry has both url and inline properties | Validation fails: exactly one of url or inline required. | CE-5, EC-2 |
| CE-N05 | Entry with neither url nor inline | Entry has neither url nor inline | Validation fails: exactly one of url or inline required. | CE-5, EC-2 |
| CE-N06 | tags contains non-string element | Entry with tags: ["valid", 123] | Validation fails: tags must be array of strings. | CE-10, EC-11 |
| CE-N07 | updatedAt is not RFC 3339 | Entry with updatedAt: "not-a-date" | Validation fails: updatedAt must be RFC 3339. | CE-13, EC-14 |
| CE-N08 | updatedAt is date-only (no time) | Entry with updatedAt: "2026-03-15" | Validation fails: must be full RFC 3339 datetime. | EC-14 |
| CE-N09 | updatedAt as integer | Entry with updatedAt: 1711929600 | Validation fails: updatedAt must be string. | EC-14 |

---

## 4. Multi-Version Entries

| Test ID | Description | Input Sketch | Expected Behavior | Spec Ref |
|---------|------------|-------------|-------------------|----------|
| MV-P01 | Two entries same identifier, different versions | entries: [{id:"urn:x", version:"1.0",...}, {id:"urn:x", version:"2.0",...}] | Parse succeeds. Both entries present. | MV-1 |
| MV-P02 | Single entry without version | Entry with identifier but no version field | Parse succeeds. | § Multi-Version |
| MV-N01 | Duplicate identifier+version | Two entries with same identifier "urn:x" and same version "1.0" | Validation fails: duplicate (identifier, version). | MV-2 |
| MV-N02 | Duplicate identifier, both missing version | Two entries with same identifier "urn:x", neither has version | Validation fails: duplicate identifier without version. | MV-3 |
| MV-N03 | Three entries, two share identifier+version | Three entries; two with (urn:x, 1.0), one with (urn:x, 2.0) | Validation fails for the duplicate pair. | MV-2 |

---

## 5. Publisher Object

| Test ID | Description | Input Sketch | Expected Behavior | Spec Ref |
|---------|------------|-------------|-------------------|----------|
| PO-P01 | Publisher with required fields | publisher: {identifier:"did:web:x", displayName:"X"} | Parse succeeds. | § Publisher Object |
| PO-P02 | Publisher with identityType | publisher: {identifier:"did:web:x", displayName:"X", identityType:"did"} | Parse succeeds. | PO-3 |
| PO-N01 | Publisher missing identifier | publisher: {displayName:"X"} | Validation fails: identifier required. | PO-1 |
| PO-N02 | Publisher missing displayName | publisher: {identifier:"did:web:x"} | Validation fails: displayName required. | PO-2 |

---

## 6. Trust Manifest

| Test ID | Description | Input Sketch | Expected Behavior | Spec Ref |
|---------|------------|-------------|-------------------|----------|
| TM-P01 | Minimal trust manifest (identity only) | trustManifest: {identity:"urn:x"} on entry with identifier:"urn:x" | Parse succeeds. identity matches identifier. | § Trust Manifest, TI-1 |
| TM-P02 | Trust manifest with all optional fields | trustManifest with identity, identityType, trustSchema, attestations, provenance, privacyPolicyUrl, termsOfServiceUrl, signature, metadata | Parse succeeds. | § Trust Manifest |
| TM-P03 | Trust manifest on host | host with trustManifest: {identity:"did:web:x"} and identifier:"did:web:x" | Parse succeeds. | § Trust Manifest |
| TM-N01 | Trust manifest missing identity | trustManifest: {} on entry | Validation fails: identity required. | TI-1 |
| TM-N02 | Trust manifest identity doesn't match entry identifier | entry.identifier:"urn:a", trustManifest.identity:"urn:b" | Validation fails: identity must match identifier. | TI-3, TI-4 |
| TM-N03 | Trust manifest identity is empty string | trustManifest: {identity:""} | Validation fails: identity must be non-empty URI. | TI-1 |

---

## 7. Trust Schema Object

| Test ID | Description | Input Sketch | Expected Behavior | Spec Ref |
|---------|------------|-------------|-------------------|----------|
| TS-P01 | Trust schema with required fields | trustSchema: {identifier:"ts:1", version:"1.0"} | Parse succeeds. | § Trust Schema Object |
| TS-P02 | Trust schema with all optional fields | trustSchema with identifier, version, governanceUri, verificationMethods | Parse succeeds. | § Trust Schema Object |
| TS-N01 | Trust schema missing identifier | trustSchema: {version:"1.0"} | Validation fails. | TS-1 |
| TS-N02 | Trust schema missing version | trustSchema: {identifier:"ts:1"} | Validation fails. | TS-2 |

---

## 8. Attestation Object

| Test ID | Description | Input Sketch | Expected Behavior | Spec Ref |
|---------|------------|-------------|-------------------|----------|
| AT-P01 | Attestation with required fields | attestation: {type:"SOC2", uri:"https://...", mediaType:"application/pdf"} | Parse succeeds. | § Attestation Object |
| AT-P02 | Attestation with all optional fields | Attestation with type, uri, mediaType, digest, size, description | Parse succeeds. | § Attestation Object |
| AT-P03 | Attestation with Data URI | attestation uri: "data:application/jwt;base64,..." | Parse succeeds. | AT-2 |
| AT-N01 | Attestation missing type | attestation: {uri:"https://...", mediaType:"application/pdf"} | Validation fails. | AT-1 |
| AT-N02 | Attestation missing uri | attestation: {type:"SOC2", mediaType:"application/pdf"} | Validation fails. | AT-2 |
| AT-N03 | Attestation missing mediaType | attestation: {type:"SOC2", uri:"https://..."} | Validation fails. | AT-3 |
| AT-N04 | Attestation size is negative | size: -1 | Validation fails: must be unsigned integer. | AT-5, CD-4 |
| AT-N05 | Attestation size is float | size: 1.5 | Validation fails: must be unsigned integer. | AT-5, CD-4 |

---

## 9. Provenance Link Object

| Test ID | Description | Input Sketch | Expected Behavior | Spec Ref |
|---------|------------|-------------|-------------------|----------|
| PL-P01 | Provenance link with required fields | provenance: {relation:"publishedFrom", sourceId:"https://..."} | Parse succeeds. | § Provenance Link Object |
| PL-P02 | Provenance link with all optional fields | Provenance with relation, sourceId, sourceDigest, registryUri, statementUri, signatureRef | Parse succeeds. | § Provenance Link Object |
| PL-N01 | Provenance link missing relation | provenance: {sourceId:"https://..."} | Validation fails. | PL-1 |
| PL-N02 | Provenance link missing sourceId | provenance: {relation:"publishedFrom"} | Validation fails. | PL-2 |

---

## 10. Digest Validation

| Test ID | Description | Input Sketch | Expected Behavior | Spec Ref |
|---------|------------|-------------|-------------------|----------|
| DG-P01 | Valid SHA-256 digest | digest: "sha256:9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08" | Validation succeeds. | VD-1 |
| DG-P02 | Valid SHA-384 digest | digest: "sha384:<96-char hex>" | Validation succeeds. | VD-1 |
| DG-P03 | Valid SHA-512 digest | digest: "sha512:<128-char hex>" | Validation succeeds. | VD-1 |
| DG-N01 | SHA-1 digest (too weak) | digest: "sha1:abc123def456" | Validation fails: algorithm shorter than SHA-256. | VD-3 |
| DG-N02 | MD5 digest (too weak) | digest: "md5:abc123" | Validation fails: algorithm shorter than SHA-256. | VD-3 |
| DG-N03 | Missing colon separator | digest: "sha256abc123" | Validation fails: invalid format. | VD-1, EC-7 |
| DG-N04 | Empty hex value | digest: "sha256:" | Validation fails: empty hex value. | EC-7 |
| DG-N05 | Uppercase hex value | digest: "sha256:9F86D081" | Validation fails or warns: spec says lowercase. | VD-1, EC-7 |
| DG-N06 | Non-hex characters | digest: "sha256:xyz123" | Validation fails: invalid hex characters. | VD-1 |

---

## 11. Collection Reference

| Test ID | Description | Input Sketch | Expected Behavior | Spec Ref |
|---------|------------|-------------|-------------------|----------|
| CO-P01 | Collection with required fields | collection: {displayName:"Finance", url:"https://..."} | Parse succeeds. | § Collections |
| CO-P02 | Collection with all optional fields | Collection with displayName, url, description, tags | Parse succeeds. | § Collections |
| CO-P03 | Multiple collections | collections array with 3 entries | Parse succeeds. | § Collections |
| CO-P04 | Empty collections array | collections: [] | Parse succeeds. | EC-9 |
| CO-N01 | Collection missing displayName | collection: {url:"https://..."} | Validation fails. | CR-1 |
| CO-N02 | Collection missing url | collection: {displayName:"Finance"} | Validation fails. | CR-2 |

---

## 12. Bundles (Nested Catalogs)

| Test ID | Description | Input Sketch | Expected Behavior | Spec Ref |
|---------|------------|-------------|-------------------|----------|
| BN-P01 | Bundle entry with inline nested catalog | Entry with mediaType "application/ai-catalog+json" and inline containing valid catalog | Parse succeeds. Nested catalog parsed. | BU-1 |
| BN-P02 | Bundle entry with url reference | Entry with mediaType "application/ai-catalog+json" and url pointing to catalog | Parse succeeds. | BU-1 |
| BN-P03 | Bundle with depth = 2 | Catalog → bundle entry → nested catalog with entries | Parse succeeds. | BU-1 |
| BN-P04 | Identifier reuse across bundle and parent | Parent entry and bundle child entry share same identifier | Parse succeeds. | BU-4 |
| BN-N01 | Bundle inline is invalid catalog | Entry with mediaType "application/ai-catalog+json" and inline: {"bad":"data"} | Validation fails: inline catalog missing specVersion/entries. | EC-8 |
| BN-N02 | Bundle nesting exceeds depth limit | 10-deep nested bundles | Validation fails: exceeds max depth. | SC-2, BU-2 |

---

## 13. Conformance Levels

| Test ID | Description | Input Sketch | Expected Behavior | Spec Ref |
|---------|------------|-------------|-------------------|----------|
| CL-P01 | Valid Level 1 (Minimal) catalog | specVersion + entries (each with identifier, displayName, mediaType, url) | Conforms to Level 1. | CL-1 |
| CL-P02 | Valid Level 2 (Discoverable) catalog | Level 1 + host with displayName | Conforms to Level 2. | CL-2 |
| CL-P03 | Valid Level 3 (Trusted) catalog | Level 2 + trustManifest on at least one entry or host | Conforms to Level 3. | CL-4 |
| CL-N01 | Level 2 check fails — no host | Catalog without host, asked to validate as Level 2 | Validation fails: host required for Level 2. | CL-2 |
| CL-N02 | Level 3 check fails — no trustManifest | Level 2 catalog without any trustManifest, asked to validate as Level 3 | Validation fails: trustManifest required for Level 3. | CL-4 |

---

## 14. Security Edge Cases

| Test ID | Description | Input Sketch | Expected Behavior | Spec Ref |
|---------|------------|-------------|-------------------|----------|
| SC-P01 | All URLs use HTTPS | Catalog with all URLs as https:// | Validation succeeds. | SC-1 |
| SC-N01 | URL uses HTTP (not HTTPS) | Entry with url: "http://example.com/card.json" | Validation warns/fails: MUST be HTTPS. | SC-1 |
| SC-N02 | logoUrl not Data URI | Host with logoUrl: "https://example.com/logo.png" | Validation warns: SHOULD use Data URI. | SC-4 |

---

## 15. Spec Example Validation

These tests validate that the examples from the specification parse correctly.

| Test ID | Description | Input Sketch | Expected Behavior | Spec Ref |
|---------|------------|-------------|-------------------|----------|
| EX-P01 | Minimal catalog example (3 entries) | Verbatim from § Top-Level Structure example | Parse succeeds. 3 entries. specVersion "1.0". | § Top-Level Structure |
| EX-P02 | Multi-artifact catalog with nested plugin | Verbatim from § Example: Multi-Artifact Catalog with Nested Plugin | Parse succeeds. 3 top entries. Bundle entry has inline catalog with 3 sub-entries. | § Example: Multi-Artifact |
| EX-P03 | Catalog with collections | Verbatim from § Example: Catalog with Collections | Parse succeeds. 1 entry + 3 collections. | § Example: Collections |
| EX-P04 | Claude Code Plugin entry | Verbatim from § Example: Claude Code Plugin Entry | Parse succeeds. Publisher and trustManifest present. | § Example: Claude Code Plugin |
| EX-P05 | MCP Server as catalog entry | Verbatim from § Mapping to MCP Registry example | Parse succeeds. mediaType is application/mcp-server+json. | § Mapping to MCP Registry |
| EX-P06 | MCP Registry as AI Catalog | Verbatim from § MCP Registry as AI Catalog example | Parse succeeds. 3 MCP server entries with host. | § Mapping to MCP Registry |
| EX-P07 | Plugin bundle as nested catalog | Verbatim from § Plugin Bundles as Nested Catalogs example | Parse succeeds. mediaType is application/ai-catalog+json with inline sub-catalog. | § Mapping to Claude Code |
| EX-P08 | Marketplace as AI Catalog | Verbatim from § Marketplace as AI Catalog example | Parse succeeds. 3 Claude plugin entries. | § Mapping to Claude Code |

---

## 16. Round-Trip Fidelity

| Test ID | Description | Input Sketch | Expected Behavior | Spec Ref |
|---------|------------|-------------|-------------------|----------|
| RT-P01 | Parse then serialize preserves all fields | Parse any valid catalog, serialize back to JSON, parse again | All fields match. No data loss. | General |
| RT-P02 | Unknown fields survive round-trip | Catalog with extension fields `x-custom` | Extension fields present after parse → serialize → parse. | EC-12 |
| RT-P03 | Metadata map survives round-trip | Entry with metadata containing nested objects | All metadata preserved exactly. | CE-14, TL-6 |
| RT-P04 | Inline content survives round-trip | Entry with complex inline object | Inline content identical after round-trip. | CE-8 |

---

## 17. Identifier Format Validation

| Test ID | Description | Input Sketch | Expected Behavior | Spec Ref |
|---------|------------|-------------|-------------------|----------|
| ID-P01 | URN identifier | identifier: "urn:example:agent:name" | Validation succeeds. Recognized as URN. | CE-2 |
| ID-P02 | HTTPS URI identifier | identifier: "https://example.com/agent/name" | Validation succeeds. Recognized as URI. | CE-2 |
| ID-P03 | DID identifier | identifier: "did:web:example.com" | Validation succeeds. Recognized as URI. | CE-2 |
| ID-W01 | Plain string identifier (no URI scheme) | identifier: "my-agent" | Parse succeeds, but validation warns: SHOULD be URN or URI. | CE-2 |

---

## 18. Mixed Content Catalogs

| Test ID | Description | Input Sketch | Expected Behavior | Spec Ref |
|---------|------------|-------------|-------------------|----------|
| MX-P01 | Catalog with entries and collections | Catalog has both entries[] and collections[] | Parse succeeds. Both populated. | § Organizing Catalogs |
| MX-P02 | Entries of different media types | Catalog with A2A, MCP, ai-catalog, and custom mediaType entries | Parse succeeds. Library is artifact-agnostic. | DG-1 |
| MX-P03 | Entry with url to non-AI artifact | Entry with mediaType "application/parquet" and url to data file | Parse succeeds. Library doesn't validate artifact type. | DG-1 |

---

## Summary Statistics

| Category | Positive | Negative | Total |
|----------|----------|----------|-------|
| Top-Level Structure | 4 | 8 | 12 |
| Host Info | 2 | 2 | 4 |
| Catalog Entry | 7 | 9 | 16 |
| Multi-Version | 2 | 3 | 5 |
| Publisher | 2 | 2 | 4 |
| Trust Manifest | 3 | 3 | 6 |
| Trust Schema | 2 | 2 | 4 |
| Attestation | 3 | 5 | 8 |
| Provenance Link | 2 | 2 | 4 |
| Digest Validation | 3 | 6 | 9 |
| Collection Reference | 4 | 2 | 6 |
| Bundles | 4 | 2 | 6 |
| Conformance Levels | 3 | 2 | 5 |
| Security | 1 | 2 | 3 |
| Spec Examples | 8 | 0 | 8 |
| Round-Trip | 4 | 0 | 4 |
| Identifier Format | 3 | 0 | 3+1 warn |
| Mixed Content | 3 | 0 | 3 |
| **TOTAL** | **60** | **48** | **108+** |
