# API Design Feedback on AI Catalog Specification

**Prepared by:** Deckard (Lead Architect)  
**Date:** 2026-04-02  
**Context:** Implementation planning for .NET and Python libraries; identifies places where spec gaps created API design friction.

---

## 1. URL/Inline Exclusivity Requires Custom Validation Logic

**Issue:** The spec requires exactly one of `url` or `inline` for every catalog entry. This is a **mutual exclusion constraint** — a guard that lives outside the type system.

**API Friction:**
- Every language must implement custom validation (not just serialization)
- Schema languages (CDDL, JSON Schema) cannot cleanly express "exactly one of A or B" — most require post-validation
- Clients may not discover the error until runtime
- No way to express this constraint in OpenAPI/GraphQL/Protobuf schemas cleanly

**Recommendation:** Consider adding a clarification to the spec:
- Example: _"Catalogs SHOULD be validated immediately after parsing to confirm url/inline exclusivity; tools may choose to fail fast or collect all violations."_
- Or: Add a media type profile parameter (e.g., `application/ai-catalog+json;profile=strict-validation`) for clients that want pre-validated streams.

---

## 2. Open Metadata Maps Block Type Safety in Compiled Languages

**Issue:** `metadata` (at catalog, entry, and host levels) is an open map of `string → arbitrary value`. This is idiomatic in JSON but breaks strongly-typed APIs.

**API Friction:**
- **.NET:** Must use `Dictionary<string, object>` or `JObject` — loses compile-time safety. IDEs can't autocomplete known keys.
- **Python:** `dict` is natural, but clients lose type hints. mypy can't validate metadata access patterns.
- **Serialization:** Each language must handle `object` values differently (JSON.NET, dataclasses, Pydantic).
- **Versioning:** If a future spec reserves metadata keys (e.g., `"x-deprecated"`), implementations won't know to handle them specially.

**Recommendation:**
- Document a registry of well-known metadata keys with their expected types (e.g., `"x-deprecated": bool`, `"x-canonical-id": string`).
- Suggest: _"Implementations MAY provide typed accessors for well-known metadata keys while preserving the ability to round-trip unknown keys."_
- Consider a future spec version with a `knownMetadata` schema reference.

---

## 3. Collection Navigation Model Is Underspecified

**Issue:** The `collections` array at the top level references other catalogs, but the spec doesn't define:
- Whether collection references are URLs, URNs, or inline catalogs
- How to resolve circular references (catalog A → catalog B → catalog A)
- Pagination strategy (does a collection have a size limit?)
- Merging semantics (are entries from collections deduplicated by identifier+version?)

**API Friction:**
- Clients can't safely build collection navigation without custom rules
- CLI tools can't know whether to pre-fetch all collections or stream them
- Multi-language consistency requires shared test fixtures, but the test cases don't cover these scenarios

**Recommendation:**
- Add a clarification: _"Collection References are URLs; clients SHOULD resolve them lazily and cache results. Clients SHOULD implement cycle detection using a visited-URLs set."_
- Specify whether collection resolution is additive (entries from collections augment top-level) or exclusive.
- Define a `maxCollectionDepth` advisory (e.g., "don't traverse more than 5 levels deep").

---

## 4. Version Format Flexibility Breaks Semantic Analysis

**Issue:** The spec allows `version` to be any string (not requiring semantic versioning), but also recommends semantic versioning. Implementations can't know whether a string like `"v1.2.3-beta"` is valid SemVer without parsing.

**API Friction:**
- **.NET & Python:** Must provide both a generic version string and an optional SemVer parser, creating two API paths.
- **Sorting:** Clients want `GetLatestByIdentifier()`, but can't sort versions without knowing the format. Fallback to `updatedAt` is awkward.
- **CLI comparison:** Commands like `install artifact:latest` need unambiguous version selection logic.

**Recommendation:**
- Clarify in spec: _"If version is SemVer-compatible, clients SHOULD parse it as such. If parsing fails, sort by updatedAt. Libraries MAY provide helpers: `TryParseSemVer(version) → (bool, Version)`."_
- Document the recommended sort order: SemVer desc, then updatedAt desc.

---

## 5. Trust Manifest Identity Binding Is Fragile

**Issue:** The spec requires that Trust Manifest identity MUST match entry identifier, but:
- No normalization rule for identifiers (case-sensitive? URI-normalized?)
- No guidance on what "match" means (exact string, semantic equivalence, both must be URIs?)
- Mismatch could silently fail trust verification or break parsing

**API Friction:**
- Validation layer needs a strict equality check, but there's no spec guidance on identifier canonicalization
- If identifier is a URI, should clients normalize it before comparing? (e.g., `http://example.com` vs. `http://example.com/`)
- Cross-language: Python and .NET handle URIs differently, making consistent comparison difficult

**Recommendation:**
- Add to spec: _"Identifier matching MUST use case-sensitive exact string comparison. URIs SHOULD be normalized per RFC 3986 Section 6.2.3 before comparison, or preserved as-is if not URIs."_
- Provide a test case: identity binding with and without URI normalization.

---

## 6. Missing Well-Known URI Discovery Semantics

**Issue:** The spec mentions `/.well-known/ai-catalog.json` but doesn't define:
- Is the path always `/` or configurable per domain?
- What HTTP headers should a well-known server return?
- Should clients follow redirects? Cache the well-known endpoint?
- How to handle 404 or timeout?

**API Friction:**
- CLI tooling (e.g., `copilot ask explore https://example.com`) can't reliably discover a catalog without guessing conventions
- No standard for marketplace discovery — each vendor (MCP Registry, Claude Plugins) implements differently
- Clients must hardcode fallback behaviors (try well-known, then try marketplace APIs, then prompt user)

**Recommendation:**
- Add to spec (Discovery section): _"Clients SHOULD first attempt GET `https://{domain}/.well-known/ai-catalog.json`. If 404, clients MAY check Link headers for `rel="ai-catalog"`. Clients SHOULD cache well-known responses for 24 hours and respect Cache-Control headers."_
- Define success response: HTTP 200, content-type `application/ai-catalog+json`, body is a valid catalog.

---

## 7. Media Type Registry Is Informative, Not Normative

**Issue:** The spec appendices map marketplace formats (OCI, MCP Registry, Claude Plugins) but:
- The mappings are marked "informative" — not part of the spec contract
- No version alignment: if the MCP Registry spec changes, which version of that mapping applies?
- Clients don't know if `application/vnd.github.copilot.manifest+json` is the authoritative media type or just an example

**API Friction:**
- Implementations must maintain a separate media type registry, but spec doesn't provide a canonical source
- CLI tools need to convert between formats but lack guidance on roundtrip fidelity
- If a new marketplace emerges, there's no process for adding it to the spec

**Recommendation:**
- Clarify: _"Media type mappings in appendices are examples; implementations MAY extend with additional mappings. The AI Catalog spec does not freeze marketplace formats — external specs own their own versioning."_
- Suggest a registry pattern: maintain a `media-type-mappings.json` in the spec repo with a version field, allowing implementations to fetch updates independently of the spec version.

---

## 8. Conformance Levels Aren't Enforced at Parsing

**Issue:** The spec defines 3 conformance levels (Minimal, Discoverable, Trusted), but:
- No normative test cases for each level
- Parser doesn't know which level to enforce until validation time
- "Discoverable" vs. "Trusted" distinction relies on presence of optional fields, not explicit leveling

**API Friction:**
- API must support two modes: `Parse(json)` and `Validate(json, conformanceLevel: L2)` — duplication
- Client code becomes: _if (conformanceLevel >= L2) check_optional_fields()_ — scattered logic
- Cross-language implementations may disagree on what "Discoverable" requires

**Recommendation:**
- Add clarification: _"Conformance levels are determined post-parsing by checking which optional fields are present. A Minimal catalog has only specVersion and entries. Discoverable catalogs SHOULD have host.displayName. Trusted catalogs MUST have trustManifest. Implementations MAY provide a `DetermineConformanceLevel(catalog) → Level` helper."_
- Add test cases for each level: minimal valid, discoverable valid, trusted valid.

---

## 9. Digest Algorithm Vetting Creates Maintenance Burden

**Issue:** The spec lists "weak" algorithms (MD5, SHA1) that must be rejected, but:
- No machine-readable registry of weak vs. strong algorithms
- No versioning: when SHA-2 becomes weak (eventually), how does spec evolve?
- Each language maintains its own cryptography library — weak-algorithm lists diverge

**API Friction:**
- **.NET & Python:** Each maintains a hardcoded list in validation code. Security updates lag spec.
- **CLI:** `convert marketplace --verify` command needs to know which algorithms to trust — requires spec version awareness.
- **Cross-language:** One library might accept SHA1, another rejects it. Interop suffers.

**Recommendation:**
- Add to spec: _"Implementations SHOULD maintain a weak-algorithm registry (MD5, SHA1). This list is non-normative and may be updated via errata. Implementations SHOULD check for updates periodically and MAY log warnings when weak algorithms are encountered."_
- Or: define a `security-advisory.json` file that libraries can fetch to stay current on algorithm recommendations.

---

## 10. Unknown Version Handling Is Ad-Hoc

**Issue:** The spec is at version 1.0, but doesn't define what happens when a future 2.0 catalog is encountered by a 1.0 implementation:
- Should parsing fail? Warn? Ignore unknown fields?
- Should clients validate unknown `specVersion` values?
- How backward-compatible should clients be?

**API Friction:**
- Parsers must decide: strict mode (reject unknown versions) or lenient mode (accept with warning)
- No spec guidance on migration strategy — implementations diverge
- CLI tools can't provide clear error messages for version mismatches

**Recommendation:**
- Add to spec: _"Implementations SHOULD warn if specVersion is not recognized but MAY attempt to parse if the document structure is compatible. Implementations MUST NOT reject documents with unknown specVersion values; instead, they SHOULD fail validation with a clear message indicating version incompatibility."_
- Document version evolution strategy: what breaking changes would trigger 2.0? (e.g., removing optional fields, changing url/inline to a union type)

---

## 11. Bundle Validation Depth Is Unconstrained

**Issue:** The spec allows entries with `inline` to contain full nested catalogs (bundles), but:
- No maximum nesting depth defined
- Recursive validation could cause stack overflow or DoS
- No guidance on memory limits for parsing deeply nested bundles

**API Friction:**
- Validation code must implement cycle detection and depth limits, but spec doesn't define them
- Cross-language implementations may choose different limits (Python stack vs. .NET heap), breaking interop
- Security: malicious catalogs could include crafted nested bundles to exhaust resources

**Recommendation:**
- Add to spec: _"Implementations SHOULD enforce a maximum bundle nesting depth of 10 levels to prevent DoS. Implementations MUST detect cycles in bundle references and fail validation if a cycle is detected."_
- Provide test cases: valid nested bundles (3 levels), invalid nested bundles (15 levels), cyclic bundle references.

---

## 12. Entry Identifier Normalization Isn't Specified

**Issue:** Identifiers can be URNs, URIs, or arbitrary strings. But:
- No normalization rule for comparison (e.g., case sensitivity, URI normalization)
- Uniqueness check (`MV-2`) relies on exact equality, but "equal" is undefined
- Clients might assume identifiers are always URIs and try to parse them

**API Friction:**
- Validation: is `http://example.com` equal to `http://example.com/`? Spec doesn't say.
- CLI: `find-entry --id "urn:isbn:1234"` might fail if internal comparison uses case-insensitive matching
- Clients building indexes must choose: normalize or preserve as-is?

**Recommendation:**
- Add to spec (CE-1): _"Identifiers MUST be compared using case-sensitive exact string matching. Clients SHOULD preserve identifiers as provided; normalization is not recommended unless explicitly documented by the catalog producer."_
- Provide examples: `urn:isbn:123`, `did:web:example.com`, `https://example.com/artifact#v1`, `artifact-name` (all valid, compared exactly as-is).

---

## 13. Publisher Identity Type Is Underspecified

**Issue:** The Publisher object has an optional `identityType` field that can be "did", "dns", or other values, but:
- No registry of valid identity types
- No guidance on how clients should use this hint
- If identityType is "did", what validation should occur?

**API Friction:**
- CLI: `get-publisher --verify did:web:example.com` can't know how to verify without hardcoding DID resolution logic
- Clients must assume identityType is purely informational — it doesn't drive behavior
- Cross-language: no shared library for resolving DIDs or DNS names

**Recommendation:**
- Clarify: _"identityType is a hint for clients; it does not drive validation. Known types include 'did' (Decentralized Identifiers), 'dns' (DNS names), 'email' (email addresses). Clients MAY use this hint to inform trust decisions but MUST NOT require it."_
- Note: Full identity resolution (DID method resolution, DNS validation) is out of scope for this spec; implementations should defer to identity providers.

---

## 14. Metadata Preservation Doesn't Require Validation

**Issue:** The spec says `metadata` must be "preserved" for round-trip, but:
- Doesn't define the serialization format exactly (JSON or custom?)
- Doesn't say whether unknown types (e.g., circular references in JSON) must be preserved
- No guidance on what round-trip means (exact string? semantic equality?)

**API Friction:**
- **.NET:** Must decide: use `JObject` (preserves JSON fidelity) or `Dictionary` (loses some structure)?
- **Python:** `json.dumps()` won't preserve `NaN`, `Infinity` — spec doesn't clarify expectations
- **CLI:** Convert command can't guarantee fidelity without explicit serialization rules

**Recommendation:**
- Clarify: _"Metadata values MUST be preserved as valid JSON. Round-trip preservation means: parse JSON → store → re-serialize to JSON → values are identical. Implementations are not required to support non-JSON values (e.g., undefined, circular references)."_
- Example: if input has `"metadata": {"count": 1, "name": "test"}`, output must have identical structure (not `{"name": "test", "count": 1}` — order doesn't matter in JSON, but structure must match).

---

## 15. Tag Format Isn't Enforced

**Issue:** Tags are an array of strings with no constraint:
- No guidance on what a valid tag looks like (alphanumeric? special chars allowed? max length?)
- No recommendation for tag naming (kebab-case? PascalCase?)
- Search/filtering across catalogs has no standard

**API Friction:**
- CLI: `search --tag "machine-learning"` vs. `search --tag "MachineLearning"` — case sensitivity is unclear
- Clients building tag indices must normalize inconsistently across catalogs
- No way to standardize tag vocabulary across marketplaces

**Recommendation:**
- Add: _"Tags are arbitrary strings. Implementations MAY recommend lowercase, kebab-case conventions in documentation but MUST support case-sensitive matching. For cross-catalog search, implementations SHOULD normalize tags (lowercase, trim whitespace) internally but preserve originals in output."_
- Suggest: _"Well-known tags (e.g., 'ml', 'data-processing', 'text-generation') MAY be documented in appendices as examples but are not normative."_

---

## 16. ProvenanceLink Targets Aren't Validated

**Issue:** `ProvenanceLink` can reference `ProvenanceLink` or `Attestation` objects, but:
- No cycle detection requirement (a link can reference itself indirectly)
- No depth limit (chains could be arbitrarily deep)
- Clients don't know how to traverse or verify link chains

**API Friction:**
- Validation must implement cycle detection but spec doesn't define the algorithm
- Performance: clients might fetch all provenance links recursively, causing cascading HTTP requests
- Cross-language: different cycle detection strategies could lead to interop issues

**Recommendation:**
- Add: _"Provenance chains MUST be acyclic; implementations SHOULD detect cycles and fail validation if one is detected. Implementations SHOULD enforce a maximum chain depth of 5 links to prevent DoS. Clients SHOULD NOT recursively fetch all links; instead, they SHOULD validate only the immediate link target."_

---

## 17. Media Type String Format Isn't Constrained

**Issue:** The `mediaType` field must be a string, but:
- No validation rule (should be RFC 2045 format?)
- No guidance on whether `application/json; charset=utf-8` is valid
- Clients might not recognize custom media types like `application/vnd.custom+json`

**API Friction:**
- **.NET & Python:** May or may not parse media type strings; no shared grammar
- CLI: `convert marketplace` needs to match media types but can't without parsing
- Interop: one implementation rejects `text/plain; charset=utf-8`, another accepts it

**Recommendation:**
- Clarify: _"mediaType SHOULD be a valid RFC 2045 media type (e.g., 'application/json', 'text/plain'). Parameters (e.g., 'charset=utf-8') are allowed but optional. Implementations SHOULD parse media types leniently; a simple string check (contains '/') is acceptable."_
- Test case: various media type formats (valid and invalid).

---

## 18. Catalog Merging Semantics Are Undefined

**Issue:** If a catalog references collections and entries appear in multiple, the spec doesn't define:
- Should entries be deduplicated or merged?
- If a client wants to combine catalogs, what's the merge strategy?
- Should version uniqueness be enforced across collections?

**API Friction:**
- CLI: `install skill1 skill2` — if both reference the same artifact from different collections, what happens?
- Clients must implement custom merge logic without spec guidance
- Cross-language: merge semantics could diverge significantly

**Recommendation:**
- Add: _"When merging multiple catalogs (e.g., top-level + collections), entries SHOULD be deduplicated by (identifier, version). If duplicates exist, the first occurrence is preferred. Clients building merged views SHOULD validate the merged catalog as a single document to ensure uniqueness constraints hold."_

---

## 19. Backward Compatibility Strategy Is Missing

**Issue:** The spec doesn't define how implementations should handle additions to the spec:
- If a new required field is added in v2.0, how should v1.0 implementations behave?
- If a field meaning changes, are old documents still valid?
- How do clients know whether they're compatible?

**API Friction:**
- Version negotiation is ad-hoc (no spec guidance on User-Agent, Accept headers, etc.)
- Clients must hardcode version checks in business logic
- Long-term maintenance: libraries can't auto-upgrade behavior

**Recommendation:**
- Add appendix: _"Backward Compatibility Strategy"_
- Define: additions are allowed (new optional fields), removals are breaking (bump major version), meaning changes are breaking (bump major version)
- Recommend: _"Clients SHOULD accept documents with unknown fields (per TD-6) and warn on unknown specVersion values. Implementations SHOULD document their maximum supported specVersion."_

---

## 20. Digest Format and Verification Isn't Normative

**Issue:** Trust verification relies on digest formats, but:
- The digest format spec is in verification procedures (§7) but not prominently linked
- No clear spec reference for digest encoding (hex? base64?)
- Weak algorithm rejection is mentioned but not enforced in CDDL schema

**API Friction:**
- CLI: `verify --digest sha256:abc123` — is this hex or base64? No clear standard.
- Implementations must guess or require configuration
- Cross-language: digest encoding could diverge (one uses hex, one uses base64)

**Recommendation:**
- Move digest format definition to core section (not just verification procedures)
- Clarify: _"Digests MUST use hex encoding (lowercase, no prefix). Format: 'algorithm:hexvalue' (e.g., 'sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855'). Implementations that reject weak algorithms SHOULD validate digest format at parse time."_

---

## Summary for Spec Evolution

These 20 issues represent **areas where the spec's flexibility created implementation friction**. Most can be addressed by:

1. **Clarifications:** Add spec prose explaining ambiguous behavior (no format changes needed)
2. **Test cases:** Provide negative tests for edge cases (deepnesting, cycles, version mismatches)
3. **Registries:** Define well-known values (digest algorithms, identity types, media types)
4. **Appendices:** Document implementation strategies (version negotiation, merging, normalization)

**For this implementation (.NET + Python):**
- Treat clarifications as design constraints
- Use test cases to validate edge case handling
- Implement helper methods for common patterns (SemVer sorting, media type parsing, cycle detection)
- Document assumptions in README/comments when spec is silent
