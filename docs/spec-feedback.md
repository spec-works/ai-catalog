# AI Card Specification — Feedback from Implementers

**From:** ai-catalog implementation team (SpecWorks)
**To:** AI Card spec authors
**Spec URL:** https://agent-card.github.io/ai-card/
**Date:** 2026-04-02
**Context:** We are building conformant parsers, validators, and CLI tools in .NET and Python. This feedback comes from actively implementing the spec — every item below caused us to stop, discuss, and make an interpretation decision. We want the spec to be clearer so the next implementer doesn't have to guess.

---

## 1. Ambiguities That Need Clarification

### 1.1 What Does `inline: null` Mean?

**What the spec says:** CatalogEntry MUST contain exactly one of `url` or `inline`. The CDDL uses `(url: text // inline: any)`.

**The problem:** JSON allows `{"url": null, "inline": {...}}` or `{"inline": null, "url": "..."}`. Is a key present with a `null` value "present" for the purposes of the exclusive-or rule? The CDDL `any` type includes `null`, so `inline: null` is technically valid CDDL — but it conflicts with the prose that says the entry "contains" the artifact inline.

**Why it matters:** Every parser must decide whether `{"url": "https://...", "inline": null}` is:
- Valid (only `url` is meaningfully present), or
- Invalid (both keys are present, violating exclusivity)

**What we assumed:** `null` values are treated as absent. An entry with `"inline": null` and a valid `url` is treated as url-only. (Rationale: `null` carries no content, so the field is not meaningfully "present".)

**Suggested spec language:** Add a note to §Catalog Entry:
> For the purposes of the `url`/`inline` exclusivity rule, a member whose value is `null` is treated as absent.

### 1.2 How Are `identity` and `identifier` Compared?

**What the spec says (§Identity):** "the `identity` field MUST match the entry's `identifier` field."

**The problem:** "Match" is not defined. URIs have complex comparison rules (RFC 3986 §6). Does `https://Example.COM/path` match `https://example.com/path`? Does `urn:example:agent` match `URN:example:agent`?

**Why it matters:** This is a P0 validation rule — Trust Manifests are rejected if identity doesn't match identifier. Different comparison algorithms produce different validation results on real-world data. Two conformant implementations could disagree on whether a catalog is valid.

**What we assumed:** Exact string comparison (byte-for-byte), with an optional normalization mode. (Rationale: simplest deterministic rule; normalization is lossy and scheme-dependent.)

**Suggested spec language:** Add to §Identity:
> The comparison of `identity` and `identifier` MUST use simple string equality (code-point-by-code-point comparison, as defined in RFC 3986 §6.2.1). Implementations MAY offer optional URI normalization but MUST NOT require it for conformance.

### 1.3 What Digest Algorithms Are "Shorter Than SHA-256"?

**What the spec says (§Digest Format):** "Consumers MUST reject digest values using algorithms shorter than SHA-256."

**The problem:** The spec doesn't define what "shorter" means, doesn't enumerate rejected algorithms, and doesn't specify how consumers should identify algorithms. Is `sha-256` the same as `sha256`? What about `blake2b-256` (same output length as SHA-256 but different algorithm)? What about future algorithms?

**Why it matters:** Every implementation must maintain a reject-list or accept-list. Without a defined algorithm naming convention, two implementations may disagree on whether `SHA256` (uppercase) or `sha-256` (hyphenated) is valid.

**What we assumed:**
- Reject: `md5`, `sha1` (output length < 256 bits)
- Accept: `sha256`, `sha384`, `sha512`, and any algorithm with ≥256-bit output
- Unknown algorithm names: accept with a warning (future-proofing)
- Algorithm names are case-sensitive, lowercase (matching OCI digest convention)

**Suggested spec language:** Add to §Digest Format:
> Algorithm identifiers MUST be lowercase ASCII strings. The following algorithms MUST be rejected: `md5`, `sha1`. The following algorithms MUST be accepted: `sha256`, `sha384`, `sha512`. Implementations SHOULD accept unknown algorithm identifiers whose output length is at least 256 bits and MAY emit a warning. Algorithm naming SHOULD follow the convention established by the OCI Image Specification (lowercase, no hyphens for SHA variants: `sha256`, `sha384`, `sha512`).

### 1.4 Mixed Versioned and Unversioned Entries With Same Identifier

**What the spec says (§Multi-Version Entries):**
- "When `version` is present, the combination of `identifier` and `version` MUST be unique."
- "When `version` is absent, `identifier` alone MUST be unique."

**The problem:** What about one entry with `identifier: "X"` and `version: "1.0"` alongside another entry with `identifier: "X"` and no `version`? The first rule is satisfied (no duplicate identifier+version pair). The second rule applies only to entries where version is absent — and there's only one such entry, so it's unique. Both rules pass independently, but the result is arguably confusing.

**Why it matters:** This is a real scenario — a catalog might list a "latest" unversioned entry alongside versioned historical entries. Implementations must decide whether to allow it, reject it, or warn.

**What we assumed:** Valid but emit a warning. The two rules are independently satisfied.

**Suggested spec language:** Add a clarifying note:
> Note: A catalog MAY contain an entry with a given `identifier` and no `version` alongside entries with the same `identifier` and explicit `version` values. This pattern represents a "latest" or "default" entry coexisting with versioned variants. Implementations SHOULD accept this but MAY warn that the interaction could cause ambiguity for version-resolution clients.

### 1.5 `specVersion` Format Strictness

**What the spec says (§Top-Level Structure):** `specVersion` is "a string indicating the version of this specification... in 'Major.Minor' format (e.g., '1.0')."

**The problem:** Is `"1"` valid? What about `"1.0.0"`? What about `"draft-1.0"`? The spec says "in Major.Minor format" but doesn't say MUST be in that format. Is this a hard constraint or a description of what compliant producers emit?

**Why it matters:** Forward compatibility. When the spec goes to version 2.0, parsers built today will encounter `"2.0"`. Should they reject it? Warn? What about a `"1.0-draft"` during the current draft period?

**What we assumed:** Accept any non-empty string. Warn (don't reject) when the value doesn't match `^\d+\.\d+$`. (Rationale: rejecting unknown versions breaks forward compatibility.)

**Suggested spec language:**
> The `specVersion` field MUST be a non-empty string. Producers MUST use the "Major.Minor" format (e.g., "1.0"). Consumers SHOULD accept any non-empty string value to support forward compatibility with future specification versions, and MAY emit a warning when the value does not match the expected format.

---

## 2. Missing Definitions

### 2.1 No JSON Schema Provided

The CDDL schema is normative, which is appropriate. However, the JSON ecosystem overwhelmingly uses JSON Schema for validation tooling. The absence of a JSON Schema means every implementation must manually translate CDDL → validation logic, and there's no standard way for JSON tooling (editors, linters, CI pipelines) to validate AI Catalog documents.

**Suggestion:** Provide an informative JSON Schema alongside the CDDL, or explicitly note that one is planned. The CDDL-to-JSON-Schema translation is mostly mechanical but has nuances (the `url`/`inline` exclusive-or is particularly awkward in JSON Schema — it requires `oneOf` with careful `required` constraints).

### 2.2 Open vs. Closed Model Not Stated

**What the spec says:** Nothing explicit about whether unknown/extension fields are permitted.

**The problem:** The CDDL schema as written uses `{}` (closed maps) for all object types. Strictly interpreted, a CatalogEntry with an `x-custom` field is invalid CDDL. But the spec says `metadata` is "an open map... for custom or non-standard metadata" — implying extension is expected, just channeled through `metadata`.

**Why it matters:** Real-world JSON formats always accumulate extension fields. If the model is closed, every parser must reject unknown keys — breaking forward compatibility when the spec adds new fields. If open, parsers must preserve unknown keys on round-trip.

**What we assumed:** Open model. Unknown fields are preserved, never rejected. (Rationale: open models are the norm for JSON formats, and the spec will certainly add fields in future versions.)

**Suggested spec language:** Add to §Top-Level Structure or a new §Extensibility section:
> All object types defined in this specification are open to extension. Implementations MUST ignore and SHOULD preserve members not defined in this specification. Custom metadata SHOULD be placed in the `metadata` member when available, but implementations MUST NOT reject documents containing unrecognized members at any level.

### 2.3 No Defined Algorithm Name Registry

Related to §1.3 above. The digest format `algorithm:hex-value` references "algorithm" but never defines the namespace. OCI uses a specific set (`sha256`, `sha384`, `sha512`). The IANA "Named Information Hash Algorithm Registry" uses different names (`sha-256`, `sha-384`). Implementers must guess which convention applies.

**Suggestion:** Either reference an existing registry (IANA Named Information, OCI) or define the canonical names inline. We'd recommend aligning with OCI conventions since the spec already has an OCI appendix.

### 2.4 `identityType` Values Not Enumerated

The spec mentions `identityType` in both Publisher and TrustManifest with examples like `"did"`, `"dns"`, `"spiffe"`. But there's no defined set of values, no registry, and no guidance on what clients should do with unrecognized values.

**Suggestion:** Either define a recommended set of values or explicitly state this is a free-form hint with no normative semantics.

---

## 3. Conformance Level Gaps

### 3.1 Level Definitions Are Structural, Not Behavioral

The three conformance levels are defined by what fields are present:
- L1: `specVersion` + `entries` (with required entry fields)
- L2: L1 + `host`
- L3: L2 + `trustManifest` (on entries and/or host)

**The problem:** This is clear for deterministic validation ("does this document satisfy Level N?"). But:
- L3 says "includes `trustManifest` objects on entries **and/or** the host." Does one trustManifest on one entry qualify? Or must every entry have one?
- L2 says "MAY be served at well-known URI" and "MAY include `collections`" — these are not requirements, so they can't be validated structurally.

**Impact:** We implemented L3 as "at least one trustManifest exists anywhere in the catalog (on any entry or on the host)." This is the most permissive reading. A stricter reading might require trustManifests on *all* entries.

**Suggestion:** Clarify the L3 threshold:
> A Trusted Catalog MUST include at least one `trustManifest` object, either on the `host` object or on one or more entries.

Or, if the intent is full coverage:
> A Trusted Catalog MUST include a `trustManifest` on the `host` object AND on every entry in the `entries` array.

### 3.2 Progressive Detection

The spec defines levels for producers to declare and consumers to validate. But a common use case is: "I received a catalog — what level does it satisfy?" This is auto-detection, not declared conformance.

**What we did:** We provide both `ValidateConformanceLevel(level)` (validate against a declared level) and `DetectConformanceLevel()` (find the highest level the document satisfies). The spec doesn't mention this use case.

**Suggestion:** Acknowledge the auto-detection pattern:
> Implementations MAY provide a function that determines the highest conformance level a given AI Catalog document satisfies, enabling progressive trust evaluation.

### 3.3 Entries at Different Trust Levels

A catalog can have some entries with trustManifests and others without. What conformance level does the catalog as a whole satisfy?

**What we assumed:** The *catalog* is L3 if it meets all L2 requirements plus has at least one trustManifest. Individual entries may vary.

**Suggestion:** Address this explicitly, or define per-entry conformance alongside catalog-level conformance.

---

## 4. Structural Concerns

### 4.1 `url` vs. `inline` Exclusivity in Schema Languages

The CDDL `(url: text // inline: any)` cleanly expresses the exclusive-or. However:
- In JSON Schema, this requires `oneOf: [{ required: ["url"] }, { required: ["inline"] }]` plus `not: { required: ["url", "inline"] }` — verbose and error-prone.
- In strongly-typed languages (C#, Java), this maps awkwardly to a single class with two nullable properties and a runtime invariant.

This is the most common validation check in the spec and the one most likely to be implemented incorrectly.

**Suggestion:** If a JSON Schema is provided, include a tested `oneOf` pattern. Consider noting in the spec that implementations typically model this as two optional fields with a runtime check, not as a discriminated union.

### 4.2 Multi-Version Uniqueness — The Two-Rule System

The uniqueness rules (§Multi-Version Entries) use two independent rules: one for versioned entries, one for unversioned entries. This is logically correct but non-obvious. Every implementer we've seen pauses on this section.

**Suggestion:** Add an explicit truth table or examples:

| Entry A | Entry B | Valid? |
|---------|---------|--------|
| `id: X, version: 1.0` | `id: X, version: 2.0` | ✅ Yes |
| `id: X, version: 1.0` | `id: X, version: 1.0` | ❌ No (duplicate identifier+version) |
| `id: X` (no version) | `id: X` (no version) | ❌ No (duplicate identifier without version) |
| `id: X, version: 1.0` | `id: X` (no version) | ✅ Yes (see §1.4 above) |
| `id: X` (no version) | `id: Y` (no version) | ✅ Yes (different identifiers) |

### 4.3 Bundle Validation Depth

When validating an `inline` value whose `mediaType` is `application/ai-catalog+json`, should the validator recursively validate the nested catalog? The spec says bundles are catalog entries whose content "is itself an AI Catalog document" — implying it should be valid. But the `inline` field is also described as "opaque to this specification."

**These two statements conflict.** The general rule says inline is opaque; the bundle definition says it's an AI Catalog.

**What we assumed:** During parsing, inline content is opaque (preserved as-is). During validation, if `mediaType` is `application/ai-catalog+json`, the inline content is recursively validated as an AI Catalog.

**Suggestion:** Clarify the interaction:
> The `inline` field is opaque to general catalog processing. However, when an entry's `mediaType` is `application/ai-catalog+json` and the `inline` value is a JSON object, validators SHOULD recursively validate the inline content as an AI Catalog document, subject to the nesting depth limit (§Security Considerations).

### 4.4 Trust Manifest Identity Binding Across Bundle Nesting

When a bundle (nested catalog) contains entries with their own trustManifests, the identity binding rule (`trustManifest.identity` MUST match `entry.identifier`) applies within the nested catalog. But there's no defined relationship between the *bundle entry's* trustManifest and the *inner entries'* trustManifests.

**Example:** Bundle entry has `identifier: "urn:suite"`, `trustManifest.identity: "urn:suite"`. Inner entry has `identifier: "urn:component"`, `trustManifest.identity: "urn:component"`. This is valid — but is there any implied trust delegation from bundle to inner entries?

**Suggestion:** State explicitly:
> Trust Manifests on bundle entries apply only to the bundle entry itself, not to entries within the nested catalog. Each entry's trust metadata is independent.

---

## 5. Normative vs. Informative Clarity

### 5.1 Appendix Status

The spec includes three appendices:
- Mapping to OCI Distribution
- Mapping to MCP Registry server.json
- Mapping to Claude Code Plugins Marketplace

**The problem:** These are not explicitly marked as informative or normative. They contain mapping tables and code examples that look like they could be normative (e.g., the OCI `artifactType` value, the MCP identifier mapping scheme).

**What we assumed:** All appendices are informative. Core library implementations need not implement any appendix mapping. (Rationale: the appendices describe *how* to use AI Catalog with other systems, not *what* AI Catalog requires.)

**Suggestion:** Add a clear marker at the start of each appendix:
> This appendix is informative and does not contain normative requirements.

### 5.2 Marketplace Mappings as Separate Documents

The OCI and Claude Code Plugin mappings are substantial — each could be a standalone document. Keeping them in the core spec inflates its size and creates the impression they're part of the core format.

**Suggestion:** Consider moving the appendices to companion documents referenced from the core spec. This keeps the core spec focused on the format definition and validation rules.

---

## 6. Suggestions for Improvement

### 6.1 Provide a JSON Schema

A JSON Schema (even informative) would dramatically reduce implementation effort. Most JSON tooling supports JSON Schema natively — editors provide autocomplete, CI pipelines validate documents, and code generators produce type definitions. The CDDL is good for the spec but doesn't plug into the JSON ecosystem.

### 6.2 Explicit Extensibility Language

Add an explicit statement that all object types are open to extension (see §2.2). This is the single most impactful clarification for implementers — it affects every type, every parser, and every round-trip test.

### 6.3 Algorithm Name Registry

Define or reference a canonical set of digest algorithm names (see §2.3). Without this, interoperability between producers and consumers is not guaranteed.

### 6.4 Canonical Examples for Every Object Type

The spec includes examples for full catalogs but not for individual objects in isolation. Add minimal examples for:
- A standalone TrustManifest
- A minimal Publisher
- A TrustSchema with verificationMethods
- An Attestation with digest verification
- A ProvenanceLink with sourceDigest

These would serve as test vectors and reduce ambiguity.

### 6.5 Consider a `specVersion` Validation Recommendation

Currently `specVersion` is defined as a string with a format description but no validation rule for consumers. Add guidance like:
> Consumers MUST NOT reject documents based solely on an unrecognized `specVersion` value. Consumers SHOULD parse the document according to the latest specification version they support and MAY emit a warning when the `specVersion` is unrecognized.

This enables forward compatibility by default.

### 6.6 Depth Limit Interaction Between Bundles and Collections

Both bundles (§Organizing Catalogs — Bundles) and collections (§Collections) recommend depth limits. The spec recommends a depth of 8 for bundles. But what about a mix? If a bundle at depth 3 contains a collection reference, does the collection count toward the bundle depth limit? Are they tracked independently?

**Suggestion:** Clarify whether bundle depth and collection depth are tracked independently or share a single counter. We implemented them as independent counters, but a shared limit would be simpler.

### 6.7 Consider `updatedAt` on Collections and Host

Currently `updatedAt` is defined only on CatalogEntry. Collections and Host objects have no timestamp. For cache invalidation and incremental sync, timestamps on these objects would be valuable.

### 6.8 Provenance Example Uses SHA-1

In the Claude Code Plugin entry example (§Example: Claude Code Plugin Entry), the provenance `sourceDigest` is:
```
"sourceDigest": "sha1:a0f1d5632b6f9e6c26eaa9806f5d8d454ca5b06f"
```
This uses SHA-1, which the spec says consumers MUST reject (§Digest Format: "Consumers MUST reject digest values using algorithms shorter than SHA-256"). The spec's own example violates its own normative requirement.

**Suggestion:** Update the example to use `sha256:...` or add a note explaining that this is a git commit hash (which is conventionally SHA-1) and is an exception to the digest rule when used in `sourceDigest` for git provenance.

---

## Summary

The AI Card spec is well-structured and the progressive complexity model (L1→L3) is a good design. The main themes from implementation:

1. **String comparison rules** are the biggest gap — identity matching, algorithm naming, and specVersion parsing all need explicit comparison semantics.
2. **Open vs. closed model** must be stated explicitly — this affects every type and every parser.
3. **Conformance level thresholds** need tightening — especially L3's "and/or" language.
4. **The spec's own examples should pass its own validation rules** (the SHA-1 provenance example).
5. **A JSON Schema** would pay for itself many times over in reduced implementation effort across the ecosystem.

We're happy to review proposed language changes or contribute test vectors. This feedback is offered constructively — the spec is a solid foundation, and these clarifications would make it significantly easier to implement correctly.
