## API Design Feedback from Implementers (.NET + Python)

**Context:** We're building conformant .NET and Python libraries for the AI Catalog specification at [spec-works/ai-catalog](https://github.com/spec-works/ai-catalog). This feedback identifies 20 places where spec gaps created API design friction during implementation planning.

---

### 1. URL/Inline Exclusivity Requires Custom Validation Logic

The spec requires exactly one of `url` or `inline` for every catalog entry  a mutual exclusion constraint that lives outside the type system. Every language must implement custom validation. Schema languages (CDDL, JSON Schema) cannot cleanly express "exactly one of A or B." Consider adding: _"Catalogs SHOULD be validated immediately after parsing to confirm url/inline exclusivity."_

### 2. Open Metadata Maps Block Type Safety in Compiled Languages

`metadata` is an open map of `string  arbitrary value`  idiomatic in JSON but breaks strongly-typed APIs. .NET must use `Dictionary<string, object>` or `JsonElement`; Python loses type hints. Consider documenting a registry of well-known metadata keys with expected types.

### 3. Collection Navigation Model Is Underspecified

`collections` references other catalogs but doesn't define: circular reference handling, pagination, or merging semantics. Consider: _"Clients SHOULD resolve lazily, cache results, and implement cycle detection."_

### 4. Version Format Flexibility Breaks Semantic Analysis

`version` can be any string but SemVer is recommended. Implementations can't sort versions without knowing the format. Consider: _"If version is SemVer-compatible, clients SHOULD parse it as such. If parsing fails, sort by updatedAt."_

### 5. Trust Manifest Identity Binding Is Fragile

Trust Manifest `identity` MUST match entry `identifier`, but "match" is undefined. No normalization rule (case-sensitive? URI-normalized?). Cross-language implementations could disagree. Consider: _"Identifier matching MUST use case-sensitive exact string comparison."_

### 6. Missing Well-Known URI Discovery Semantics

`/.well-known/ai-catalog.json` is mentioned but HTTP semantics are undefined  no guidance on headers, redirects, caching, or error handling. Consider defining success response and client caching behavior.

### 7. Media Type Registry Is Informative, Not Normative

Appendix marketplace mappings (OCI, MCP Registry, Claude Plugins) aren't explicitly marked informative or normative. No version alignment with external specs. Consider clarifying status and a registry pattern.

### 8. Conformance Levels Aren't Enforced at Parsing

3 conformance levels defined but no normative test cases. Consider: _"Conformance levels are determined post-parsing. Implementations MAY provide a `DetermineConformanceLevel(catalog)  Level` helper."_

### 9. Digest Algorithm Vetting Creates Maintenance Burden

Weak algorithms must be rejected but there's no machine-readable registry. Each language maintains its own hardcoded list. Consider defining a weak-algorithm registry or referencing an existing one.

### 10. Unknown Version Handling Is Ad-Hoc

No definition of what happens when a 1.0 implementation encounters a 2.0 catalog. Consider: _"Implementations SHOULD warn on unrecognized specVersion but MUST NOT reject."_

### 11. Bundle Validation Depth Is Unconstrained

No maximum nesting depth defined for bundles. The spec recommends 8 for clients, but this isn't normative for validation. Consider normative test cases for valid/invalid depths.

### 12. Entry Identifier Normalization Isn't Specified

Identifiers can be URNs, URIs, or strings  no normalization rule for comparison. Uniqueness check relies on equality but "equal" is undefined. Consider: _"Identifiers MUST be compared using case-sensitive exact string matching."_

### 13. Publisher Identity Type Is Underspecified

`identityType` has no registry of valid values and no guidance on client behavior. Consider: _"identityType is a hint; it does not drive validation."_

### 14. Metadata Preservation Doesn't Define Round-Trip Semantics

Metadata must be "preserved" but round-trip semantics are undefined. Consider: _"Round-trip means parse  store  re-serialize  values identical as valid JSON."_

### 15. Tag Format Isn't Enforced

Tags are arbitrary strings with no constraints. Cross-catalog search has no normalization standard.

### 16. ProvenanceLink Targets Aren't Validated

No cycle detection requirement and no depth limit for provenance chains.

### 17. Media Type String Format Isn't Constrained

`mediaType` has no validation rule. No guidance on parameters like `charset=utf-8`. Consider: _"mediaType SHOULD be a valid RFC 2045 media type."_

### 18. Catalog Merging Semantics Are Undefined

No deduplication or merge strategy when combining catalogs from collections.

### 19. Backward Compatibility Strategy Is Missing

No definition of how spec additions/removals affect version compatibility.

### 20. Digest Format and Verification Isn't Prominently Defined

Digest format (`algorithm:hex-value`) is buried in verification procedures. Consider moving to a core section.

---

These fall into four buckets: **clarifications** (spec prose), **test cases** (edge cases), **registries** (well-known values), and **appendices** (implementation strategies). Happy to contribute test vectors or review proposed language changes.
