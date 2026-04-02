# AI Catalog Specification — Requirements Checklist

**Specification:** AI Catalog (`application/ai-catalog+json`)
**Spec URL:** https://agent-card.github.io/ai-card/
**Spec Status:** Draft
**Extracted by:** Tyrell (Spec Reader)
**Date:** 2026-04-02
**RFC 2119 Priority Mapping:** MUST/MUST NOT/SHALL/SHALL NOT → P0 | SHOULD/SHOULD NOT/RECOMMENDED → P1 | MAY/OPTIONAL → P2

---

## § Media Type

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| MT-1 | An AI Catalog document is identified by the media type `application/ai-catalog+json` | P0 | Library must use this media type when serializing; parsers should accept documents with this content type |

---

## § Top-Level Structure

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| TL-1 | An AI Catalog document is a JSON object that MUST contain `specVersion` — a string indicating the version of this specification in "Major.Minor" format (e.g., "1.0") | P0 | Validate presence and format. Reject documents missing `specVersion`. |
| TL-2 | An AI Catalog document MUST contain `entries` — an array of Catalog Entry objects | P0 | Validate presence. Must be a JSON array. |
| TL-3 | The `entries` array MAY be empty | P2 | An empty array `[]` is valid. Do not reject. |
| TL-4 | `host` is OPTIONAL — a Host Info object identifying the operator of this catalog | P2 | Parse when present, do not require. |
| TL-5 | `collections` is OPTIONAL — an array of Collection Reference objects | P2 | Parse when present, do not require. |
| TL-6 | `metadata` is OPTIONAL — an open map of string keys to arbitrary values for custom or non-standard metadata | P2 | Preserve round-trip. Do not validate values. |

---

## § Host Info

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| HI-1 | The Host Info object MUST contain `displayName` — a string containing the human-readable name of the host | P0 | Validate presence when `host` is included. |
| HI-2 | `identifier` is OPTIONAL — a string containing a verifiable identifier for the host (e.g., a DID or domain name) | P2 | Parse when present. |
| HI-3 | `documentationUrl` is OPTIONAL — a string containing a URL to the host's documentation | P2 | Parse when present. |
| HI-4 | `logoUrl` is OPTIONAL — a string containing a URL to the host's logo | P2 | Parse when present. |
| HI-5 | `trustManifest` is OPTIONAL — a Trust Manifest object providing verifiable identity and trust metadata for the host itself | P2 | Parse when present. |

---

## § Catalog Entry

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| CE-1 | A Catalog Entry MUST contain `identifier` — a string identifying this artifact | P0 | Validate presence. |
| CE-2 | The `identifier` SHOULD be a URN [RFC 8141] or URI [RFC 3986] | P1 | Warn on non-URI identifiers during validation. |
| CE-3 | A Catalog Entry MUST contain `displayName` — a string containing a human-readable name | P0 | Validate presence. |
| CE-4 | A Catalog Entry MUST contain `mediaType` — a string containing the media type that identifies the type of the referenced artifact | P0 | Validate presence. |
| CE-5 | A Catalog Entry MUST contain exactly one of `url` or `inline` | P0 | Reject entries with both `url` and `inline`. Reject entries with neither. |
| CE-6 | `url` — a string containing a URL where the full artifact document can be retrieved | P0 | Parse as string. |
| CE-7 | The document served at `url` SHOULD be served with the media type declared in the `mediaType` field | P1 | Library note only — not a parsing concern, but relevant for catalog producers. |
| CE-8 | `inline` — a JSON value containing the complete artifact document inline. The structure is determined by `mediaType` and is opaque to this specification. | P0 | Preserve as arbitrary JSON value. Do not validate structure (it's opaque). |
| CE-9 | `description` is OPTIONAL — a string containing a short description | P2 | Parse when present. |
| CE-10 | `tags` is OPTIONAL — an array of strings serving as keywords | P2 | Parse when present. Validate as array of strings. |
| CE-11 | `version` is OPTIONAL — a string containing the version of this artifact | P2 | Parse when present. |
| CE-12 | Semantic Versioning is RECOMMENDED for `version` but not required | P1 | Accept any string. Provide semver parsing utility. |
| CE-13 | `updatedAt` is OPTIONAL — a string containing an ISO 8601 [RFC 3339] timestamp | P2 | Parse when present. Validate as ISO 8601 / RFC 3339 datetime. |
| CE-14 | `metadata` is OPTIONAL — an open map of string keys to arbitrary values | P2 | Preserve round-trip. |
| CE-15 | `publisher` is OPTIONAL — a Publisher object. This is the sole location for publisher information; it is not duplicated in the Trust Manifest. | P2 | Parse when present. |
| CE-16 | `trustManifest` is OPTIONAL — a Trust Manifest object | P2 | Parse when present. |

---

## § Multi-Version Entries

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| MV-1 | A catalog MAY contain multiple entries with the same `identifier` and different `version` values | P2 | Do not reject duplicated identifiers if versions differ. |
| MV-2 | When `version` is present, the combination of `identifier` and `version` MUST be unique within the catalog | P0 | Validate: reject catalogs with duplicate (identifier, version) pairs. |
| MV-3 | When `version` is absent, `identifier` alone MUST be unique | P0 | Validate: reject catalogs with duplicate identifiers when entries lack version. |
| MV-4 | The `identifier` SHOULD be stable across versions and catalog locations | P1 | Advisory — not enforceable by a library, but should be documented. |
| MV-5 | Clients needing only the latest version SHOULD sort entries sharing the same `identifier` by `version` (when parseable as semantic version) or by `updatedAt`, and select the most recent | P1 | Provide a helper method: `GetLatestByIdentifier()`. |
| MV-6 | Clients needing a specific version SHOULD match on both `identifier` and `version` | P1 | Provide a helper method: `FindByIdentifierAndVersion()`. |

---

## § Publisher Object

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| PO-1 | The Publisher object MUST contain `identifier` — a string containing a verifiable identifier for the publisher organization | P0 | Validate presence when publisher is included. |
| PO-2 | The Publisher object MUST contain `displayName` — a string containing the human-readable name of the publisher | P0 | Validate presence when publisher is included. |
| PO-3 | `identityType` is OPTIONAL — a string providing a type hint for the publisher identifier (e.g., "did", "dns") | P2 | Parse when present. |

---

## § Trust Manifest

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| TM-1 | The Trust Manifest is an OPTIONAL companion to catalog entries and host objects | P2 | Do not require. |
| TM-2 | The Trust Manifest does NOT wrap the artifact — it sits alongside the artifact as a peer element within a Catalog Entry | P0 | Model as a sibling property, not a wrapper. |
| TM-3 | Publisher information is NOT duplicated in the Trust Manifest — the informational publisher identity is carried on the Catalog Entry | P0 | Do not add publisher fields to TrustManifest type. |
| TM-4 | Implementations that do not require trust metadata MAY ignore Trust Manifests entirely | P2 | Support parsing without trust validation. |

---

## § Trust Manifest — Identity

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| TI-1 | A Trust Manifest MUST contain `identity` — a globally unique URI [RFC 3986] that serves as the primary subject identifier | P0 | Validate presence when trustManifest is included. |
| TI-2 | `identity` SHOULD be a DID, SPIFFE ID, or URL | P1 | Accept any URI. Warn during strict validation if not a recognized scheme. |
| TI-3 | When a Trust Manifest appears within a Catalog Entry, the `identity` field MUST match the entry's `identifier` field | P0 | Validate: reject if `trustManifest.identity != entry.identifier`. |
| TI-4 | Consumers MUST reject a Trust Manifest whose `identity` does not match the containing entry's `identifier` | P0 | Enforce in validation mode. |
| TI-5 | When a Trust Manifest appears on a Host Info object, `identity` SHOULD match the host's `identifier` field when present | P1 | Warn on mismatch during validation. |
| TI-6 | When multiple entries share the same `identifier` (with different `version` values), each entry MAY carry its own Trust Manifest | P2 | No constraint — each version can have different trust metadata. |

---

## § Trust Manifest — Optional Members

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| TO-1 | `identityType` is OPTIONAL — a string providing a type hint for the identity URI. OPTIONAL when the type is evident from the URI scheme. | P2 | Parse when present. |
| TO-2 | `trustSchema` is OPTIONAL — a Trust Schema object | P2 | Parse when present. |
| TO-3 | `attestations` is OPTIONAL — an array of Attestation objects | P2 | Parse when present. |
| TO-4 | `provenance` is OPTIONAL — an array of Provenance Link objects | P2 | Parse when present. |
| TO-5 | `privacyPolicyUrl` is OPTIONAL — a string URL to the privacy policy | P2 | Parse when present. |
| TO-6 | `termsOfServiceUrl` is OPTIONAL — a string URL to the terms of service | P2 | Parse when present. |
| TO-7 | `signature` is OPTIONAL — a string containing a detached JWS [RFC 7515] signature computed over the Trust Manifest content | P2 | Parse when present. Provide signature verification API. |
| TO-8 | `metadata` is OPTIONAL — an open map of string keys to arbitrary values | P2 | Preserve round-trip. |

---

## § Trust Schema Object

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| TS-1 | A Trust Schema object MUST contain `identifier` — a string identifying the trust schema | P0 | Validate presence when trustSchema is included. |
| TS-2 | A Trust Schema object MUST contain `version` — a string indicating the schema version | P0 | Validate presence when trustSchema is included. |
| TS-3 | `governanceUri` is OPTIONAL — a string URI to the governance policy document | P2 | Parse when present. |
| TS-4 | `verificationMethods` is OPTIONAL — an array of strings identifying verification methods (e.g., "did", "x509", "dns-01") | P2 | Parse when present. |

---

## § Attestation Object

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| AT-1 | An Attestation object MUST contain `type` — a string identifying the attestation type (e.g., "SOC2-Type2", "HIPAA-Audit") | P0 | Validate presence. |
| AT-2 | An Attestation object MUST contain `uri` — a string containing the location of the attestation document. MAY be HTTPS URL or Data URI [RFC 2397] | P0 | Validate presence. |
| AT-3 | An Attestation object MUST contain `mediaType` — a string indicating the format (e.g., "application/pdf", "application/jwt") | P0 | Validate presence. |
| AT-4 | `digest` is OPTIONAL — a string containing a cryptographic hash for integrity verification (e.g., "sha256:abcd1234...") | P2 | Parse when present. Validate digest format. |
| AT-5 | `size` is OPTIONAL — an unsigned integer indicating the size in bytes | P2 | Parse when present. Validate as non-negative integer. |
| AT-6 | `description` is OPTIONAL — a string containing a human-readable label | P2 | Parse when present. |

---

## § Provenance Link Object

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| PL-1 | A Provenance Link MUST contain `relation` — a string describing the relationship (e.g., "materializedFrom", "derivedFrom", "publishedFrom") | P0 | Validate presence. |
| PL-2 | A Provenance Link MUST contain `sourceId` — a string identifying the source artifact or data | P0 | Validate presence. |
| PL-3 | `sourceDigest` is OPTIONAL — a string containing the digest of the source | P2 | Parse when present. Validate digest format. |
| PL-4 | `registryUri` is OPTIONAL — a string URI of the registry holding the source | P2 | Parse when present. |
| PL-5 | `statementUri` is OPTIONAL — a string URI of a provenance statement document | P2 | Parse when present. |
| PL-6 | `signatureRef` is OPTIONAL — a string referencing the key used to sign the provenance statement | P2 | Parse when present. |

---

## § Verification Procedures — Digest Format

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| VD-1 | Digests use the format `algorithm:hex-value`, where `algorithm` is a hash algorithm identifier and `hex-value` is the lowercase hexadecimal encoding of the hash output | P0 | Validate digest strings match `^[a-z0-9-]+:[0-9a-f]+$` pattern. |
| VD-2 | Producers SHOULD use SHA-256 [RFC 6234] or stronger | P1 | Default to SHA-256 when creating digests. |
| VD-3 | Consumers MUST reject digest values using algorithms shorter than SHA-256 | P0 | Reject: md5, sha1, sha-1. Accept: sha256, sha384, sha512. |

---

## § Verification Procedures — Signatures

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| VS-1 | The `signature` field carries a detached JWS [RFC 7515] computed over the Trust Manifest content | P0 | Parse as JWS compact serialization. |
| VS-2 | Canonicalize the Trust Manifest JSON using JCS (JSON Canonicalization Scheme) [RFC 8785]. Remove the `signature` field itself before canonicalization. | P0 | Implement JCS canonicalization. Remove `signature` key before canonicalization. |
| VS-3 | Sign (or verify) the canonical bytes as a detached JWS payload using the publisher's private (or public) key | P0 | Provide sign/verify APIs. |
| VS-4 | Encode the resulting JWS in compact serialization and store it in the `signature` field | P0 | Store in compact format (header.payload.signature). |

---

## § Organizing Catalogs — Bundles

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| BU-1 | A bundle is a catalog entry whose `mediaType` is `application/ai-catalog+json`. The entry's content (via `url` or `inline`) is itself an AI Catalog document. | P0 | When mediaType is `application/ai-catalog+json`, parse inline/url as AICatalog. |
| BU-2 | Clients processing bundles SHOULD impose a maximum nesting depth to prevent circular references | P1 | Implement depth tracking during traversal. |
| BU-3 | A depth limit of 8 is RECOMMENDED | P1 | Default max depth = 8. Make configurable. |
| BU-4 | An entry inside a bundle MAY reuse the same `identifier` as an entry elsewhere; this indicates the same logical artifact | P2 | Do not reject cross-bundle identifier reuse. |

---

## § Collections (Collection Reference)

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| CR-1 | A Collection Reference object MUST contain `displayName` — a string containing a human-readable name | P0 | Validate presence when collections are included. |
| CR-2 | A Collection Reference object MUST contain `url` — a string containing a URL where the child AI Catalog document can be retrieved | P0 | Validate presence. |
| CR-3 | The document at the collection `url` MUST be a valid AI Catalog | P0 | When resolving collections, validate the retrieved document. |
| CR-4 | `description` is OPTIONAL — a string describing what this collection contains | P2 | Parse when present. |
| CR-5 | `tags` is OPTIONAL — an array of strings serving as keywords | P2 | Parse when present. |
| CR-6 | Collections are recursive — a child catalog MAY itself contain `collections`, enabling multi-level hierarchies | P2 | Support recursive traversal. |
| CR-7 | Clients SHOULD impose a maximum traversal depth | P1 | Implement depth tracking. Share depth limit with bundles. |

---

## § Discovery — Location Independence

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| DI-1 | An AI Catalog document MAY be served from any URL | P2 | No URL validation required. |
| DI-2 | When served over HTTP, the document SHOULD be served with the media type `application/ai-catalog+json` | P1 | Set Content-Type when writing. Accept documents regardless of Content-Type when reading. |

---

## § Discovery — Well-Known URI

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| WK-1 | Hosts MAY serve an AI Catalog at `/.well-known/ai-catalog.json` | P2 | Provide discovery helper that tries this path. |
| WK-2 | Clients performing domain-level discovery SHOULD attempt to retrieve the well-known URL | P1 | Implement in discovery client. |
| WK-3 | If a valid AI Catalog document is returned, the client SHOULD use the `url` entries to retrieve individual artifacts | P1 | Advisory for client implementations. |

---

## § Discovery — Link Relation

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| LR-1 | Websites MAY advertise their AI Catalog by including an `ai-catalog` link relation in HTTP responses or HTML documents | P2 | Provide link relation discovery helper. |
| LR-2 | A server MAY include a `Link` header [RFC 8288] with `rel="ai-catalog"` | P2 | Parse Link headers during discovery. |
| LR-3 | An HTML page MAY include a `<link rel="ai-catalog">` element | P2 | Parse HTML link elements during discovery. |
| LR-4 | AI agents SHOULD check for the `ai-catalog` link relation on target websites | P1 | Part of discovery flow. |
| LR-5 | Discovery procedure: (1) Check Link header, (2) Parse HTML for `<link>`, (3) Fall back to well-known URI, (4) Validate response | P1 | Implement as a multi-step discovery method. |

---

## § Conformance Levels

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| CL-1 | Level 1 (Minimal Catalog): JSON document with `specVersion` and `entries`, each entry containing `identifier`, `displayName`, `mediaType`, and exactly one of `url` or `inline` | P0 | Implement Level 1 validation. |
| CL-2 | Level 2 (Discoverable Catalog): Level 1 + includes a `host` object identifying the catalog operator | P0 | Implement Level 2 validation = Level 1 + host present. |
| CL-3 | Level 2: MAY be served at well-known URI, MAY include `collections` | P2 | Not structurally enforceable. |
| CL-4 | Level 3 (Trusted Catalog): Level 2 + includes `trustManifest` objects on entries and/or the host | P0 | Implement Level 3 validation = Level 2 + trustManifest present. |
| CL-5 | Level 3: MAY include `publisher` objects on entries with verifiable identifiers | P2 | Parse when present. |
| CL-6 | Implementations MUST satisfy all requirements of their declared level | P0 | Provide `ValidateConformanceLevel(level)` method. |
| CL-7 | Consumers MAY ignore fields defined at higher conformance levels | P2 | Library should not error on unknown/higher-level fields. |
| CL-8 | Consumers SHOULD gracefully handle the absence of higher-level fields | P1 | Use nullable/optional types for all non-required fields. |

---

## § Security Considerations

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| SC-1 | AI Catalogs, artifacts, and Trust Manifests MUST be served over HTTPS (TLS 1.2 or later) to prevent tampering and eavesdropping | P0 | Validate URL schemes during strict validation. Warn/reject `http://` URLs. |
| SC-2 | Clients processing nested catalogs MUST enforce a maximum recursion depth to prevent denial-of-service attacks | P0 | Enforce during traversal. Default max = 8. |
| SC-3 | A maximum depth of 8 is RECOMMENDED | P1 | Default value. Make configurable. |
| SC-4 | Logo URLs SHOULD use Data URIs [RFC 2397] to avoid leaking client information through image fetch requests | P1 | Advisory for catalog producers. Warn during strict validation. |
| SC-5 | Publishers SHOULD carefully consider what information is included in `metadata` extension fields | P1 | Advisory — cannot be enforced by library. |

---

## § CDDL Schema (Normative)

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| CD-1 | The CDDL schema is normative — implementation types MUST match | P0 | Verify all model types against the CDDL definitions. |
| CD-2 | CatalogEntry uses `(url: text // inline: any)` — exactly one alternative | P0 | Matches CE-5. CDDL formalizes the exclusive-or. |
| CD-3 | `updatedAt` is typed as `tdate` in CDDL (RFC 3339 date-time) | P0 | Validate as RFC 3339 when present. |
| CD-4 | `size` is typed as `uint` in CDDL | P0 | Validate as non-negative integer. |

---

## § Design Goals (Informative but Implementation-Relevant)

| ID | Requirement | Priority | Implementation Notes |
|----|------------|----------|---------------------|
| DG-1 | The catalog MUST be capable of indexing any type of AI artifact without requiring knowledge of the artifact's internal schema | P0 | Do not validate `inline` content against `mediaType`. Treat as opaque. |
| DG-2 | Each catalog entry MUST declare its artifact type using a media type | P0 | Same as CE-4. |
| DG-3 | The catalog format supports nesting — a catalog entry can reference another AI Catalog | P0 | Same as BU-1. |

---

# Edge Cases

This section catalogs edge cases that test implementations should handle.

## EC-1: Missing Required Fields

- Missing `specVersion` → reject
- Missing `entries` → reject
- Missing `identifier` on CatalogEntry → reject
- Missing `displayName` on CatalogEntry → reject
- Missing `mediaType` on CatalogEntry → reject
- Missing `displayName` on Host Info when `host` is present → reject
- Missing `identity` on Trust Manifest when `trustManifest` is present → reject
- Missing required fields on Publisher (`identifier`, `displayName`) → reject
- Missing required fields on Trust Schema (`identifier`, `version`) → reject
- Missing required fields on Attestation (`type`, `uri`, `mediaType`) → reject
- Missing required fields on Provenance Link (`relation`, `sourceId`) → reject
- Missing required fields on Collection Reference (`displayName`, `url`) → reject

## EC-2: Entry Content Exclusivity (url vs inline)

- Entry has both `url` and `inline` → reject
- Entry has neither `url` nor `inline` → reject
- Entry has only `url` → accept
- Entry has only `inline` → accept
- Entry has `url` set to `null` and `inline` present → treat as inline-only (implementation decision — see decisions doc)
- Entry has `inline` set to `null` and `url` present → treat as url-only

## EC-3: Invalid or Unusual specVersion Values

- `specVersion` as empty string → reject
- `specVersion` as non-string (number, null, array) → reject
- `specVersion` as "1.0" → accept
- `specVersion` as "2.0" (future version) → accept with possible warning
- `specVersion` as "1" (missing minor) → implementation decision on strictness
- `specVersion` as "1.0.0" (extra segment) → implementation decision on strictness

## EC-4: Empty and Minimal Structures

- Completely empty entries array `[]` → valid Level 1 catalog
- Catalog with only `specVersion` and empty `entries` → valid
- Entry with only required fields → valid
- Host with only `displayName` → valid
- Trust Manifest with only `identity` → valid

## EC-5: Multi-Version Entry Uniqueness

- Two entries with same `identifier`, different `version` → valid
- Two entries with same `identifier`, same `version` → reject
- Two entries with same `identifier`, both missing `version` → reject
- One entry with `identifier` + `version`, another with same `identifier` but no `version` → implementation decision (see decisions doc)
- Entry with `version` "" (empty string) → ambiguous — treated as version present

## EC-6: Trust Manifest Identity Binding

- `trustManifest.identity` matches `entry.identifier` → valid
- `trustManifest.identity` differs from `entry.identifier` → reject
- `trustManifest.identity` matches but case differs → implementation decision (URI comparison rules)
- Host `trustManifest.identity` mismatches `host.identifier` → warn (SHOULD, not MUST)
- Trust Manifest on entry that has no `identifier` → impossible (identifier is required)

## EC-7: Digest Validation

- Digest `sha256:9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08` → valid
- Digest `sha1:abc123` → reject (algorithm shorter than SHA-256)
- Digest `md5:abc123` → reject
- Digest with no colon separator → reject (invalid format)
- Digest with uppercase hex → implementation decision (spec says "lowercase")
- Digest with empty value after colon → reject
- Digest `sha256:` (empty hex) → reject

## EC-8: Nested Catalog (Bundle) Depth

- Bundle nesting depth = 1 → valid
- Bundle nesting depth = 8 → valid (at recommended limit)
- Bundle nesting depth = 9 → should be rejected (exceeds recommended limit)
- Circular reference (catalog A bundles B which bundles A) → must detect and reject
- Bundle where `inline` contains invalid AI Catalog → reject

## EC-9: Collection Edge Cases

- Collection `url` pointing to non-catalog JSON → reject when resolved
- Collection with recursive child collections → valid, but enforce depth limit
- Empty `collections` array → valid
- Collection with all optional fields populated → valid

## EC-10: Inline Content Edge Cases

- `inline` is a JSON object → valid (most common)
- `inline` is a JSON array → valid (spec says "a JSON value")
- `inline` is a JSON string → valid
- `inline` is a JSON number → valid
- `inline` is `null` → ambiguous — treated as absent? (see decisions doc)
- `inline` is a boolean → valid (spec says "any")

## EC-11: Type Coercion and Unexpected JSON Types

- `specVersion` as integer `1` instead of string `"1.0"` → reject
- `entries` as object instead of array → reject
- `tags` containing non-string elements → reject
- `size` as negative number → reject (CDDL: `uint`)
- `size` as float → reject
- `metadata` with non-string keys → impossible in JSON (keys are always strings)

## EC-12: Unknown/Extension Fields

- Catalog with unknown top-level fields → should be preserved (open model)
- Entry with unknown fields → should be preserved
- Trust Manifest with unknown fields → should be preserved
- Library should not reject documents with unrecognized fields

## EC-13: URL Validation

- `url` as valid HTTPS URL → accept
- `url` as valid HTTP URL → accept with security warning (SC-1 says MUST be HTTPS)
- `url` as relative URL → implementation decision
- `url` as empty string → reject
- `url` as `file://` URL → accept? (spec says location-independent, "distributed as files")
- `documentationUrl`, `logoUrl` with invalid URL syntax → warn

## EC-14: updatedAt Validation

- Valid RFC 3339 timestamp `"2026-03-15T10:00:00Z"` → accept
- Timestamp without timezone `"2026-03-15T10:00:00"` → strict reject or lenient accept
- Timestamp as date only `"2026-03-15"` → reject (not full RFC 3339)
- Timestamp as integer (Unix epoch) → reject
- Timestamp in far future → accept (no constraint)
- Empty string → reject

## EC-15: Large Catalogs

- Catalog with 10,000+ entries → should work (performance concern)
- Entry with very large `inline` value (megabytes) → should work
- Deeply nested metadata objects → should work

---

# CDDL Schema Reference

Reproduced from the spec for implementer reference:

```cddl
AICatalog = {
  specVersion: text,
  ? host: HostInfo,
  entries: [* CatalogEntry],
  ? collections: [* CollectionRef],
  ? metadata: { * text => any }
}

HostInfo = {
  displayName: text,
  ? identifier: text,
  ? documentationUrl: text,
  ? logoUrl: text,
  ? trustManifest: TrustManifest
}

CollectionRef = {
  displayName: text,
  url: text,
  ? description: text,
  ? tags: [* text]
}

CatalogEntry = {
  identifier: text,
  displayName: text,
  mediaType: text,
  (url: text // inline: any),
  ? version: text,
  ? description: text,
  ? tags: [* text],
  ? publisher: Publisher,
  ? trustManifest: TrustManifest,
  ? updatedAt: tdate,
  ? metadata: { * text => any }
}

Publisher = {
  identifier: text,
  displayName: text,
  ? identityType: text
}

TrustManifest = {
  identity: text,
  ? identityType: text,
  ? trustSchema: TrustSchema,
  ? attestations: [* Attestation],
  ? provenance: [* ProvenanceLink],
  ? privacyPolicyUrl: text,
  ? termsOfServiceUrl: text,
  ? signature: text,
  ? metadata: { * text => any }
}

TrustSchema = {
  identifier: text,
  version: text,
  ? governanceUri: text,
  ? verificationMethods: [* text]
}

Attestation = {
  type: text,
  uri: text,
  mediaType: text,
  ? digest: text,
  ? size: uint,
  ? description: text
}

ProvenanceLink = {
  relation: text,
  sourceId: text,
  ? sourceDigest: text,
  ? registryUri: text,
  ? statementUri: text,
  ? signatureRef: text
}
```
