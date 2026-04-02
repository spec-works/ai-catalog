---
name: "project-conventions"
description: "Core conventions and patterns for the ai-catalog codebase"
domain: "project-conventions"
confidence: "high"
source: "earned"
---

## Context

This skill applies to all work on the ai-catalog Part — a SpecWorks implementation of the AI Card specification (`application/ai-catalog+json`). It defines the project structure, naming conventions, domain model, and testing patterns specific to this Part.

## Patterns

### Domain Model Types

Nine public types, one per spec concept. Same PascalCase names in both .NET and Python:

- `AiCatalog` — top-level document (specVersion, entries[], host?, collections?, metadata?)
- `CatalogEntry` — single artifact (identifier, displayName, mediaType, url|inline, version?, description?, tags?, publisher?, trustManifest?, updatedAt?, metadata?)
- `HostInfo` — catalog operator (displayName, identifier?, documentationUrl?, logoUrl?, trustManifest?)
- `CollectionReference` — catalog partition (displayName, url, description?, tags?)
- `Publisher` — artifact publisher (identifier, displayName, identityType?)
- `TrustManifest` — trust metadata (identity, identityType?, trustSchema?, attestations?, provenance?, privacyPolicyUrl?, termsOfServiceUrl?, signature?, metadata?)
- `TrustSchema` — trust framework (identifier, version, governanceUri?, verificationMethods?)
- `Attestation` — verifiable proof (type, uri, mediaType, digest?, size?, description?)
- `ProvenanceLink` — lineage record (relation, sourceId, sourceDigest?, registryUri?, statementUri?, signatureRef?)

### Entry Content Exclusivity

A CatalogEntry MUST have exactly one of `url` or `inline`. Both null = invalid. Both set = invalid. This is a fundamental validation rule.

### Conformance Levels

Three levels, each building on the previous:
- **Minimal (1):** specVersion + entries with required fields
- **Discoverable (2):** Minimal + host with displayName
- **Trusted (3):** Discoverable + trustManifest on entries/host

### Package Names

- .NET library: `SpecWorks.AICatalog` (namespace: `SpecWorks.AICatalog`)
- .NET CLI tool: `SpecWorks.AICatalog.Cli` (command: `ai-catalog`)
- Python package: `specworks-ai-catalog` (import: `specworks_ai_catalog`)

### CLI Commands

- `ai-catalog convert marketplace <path-or-url>` — marketplace.json → ai-catalog
- `ai-catalog explore <url>` — load and browse a catalog
- `ai-catalog install <identifier>` — download and enable a skill/plugin

### Testing

Shared test fixtures in `testcases/` using wrapper format with `input`, `expected`, and `spec_section` fields. Both .NET (xUnit) and Python (pytest) consume the same fixtures. Target: 36+ test cases.

## Examples

```json
// Minimal valid catalog (testcase)
{
  "description": "Minimal valid catalog with one entry",
  "spec_section": "Level 1: Minimal Catalog",
  "input": {
    "specVersion": "1.0",
    "entries": [{
      "identifier": "urn:example:test",
      "displayName": "Test",
      "mediaType": "application/mcp-server+json",
      "url": "https://example.com/test.json"
    }]
  },
  "expected": {
    "entry_count": 1,
    "conformance_level": "minimal"
  }
}
```

## Anti-Patterns

- **Don't add external dependencies to the core library** — stdlib only (System.Text.Json, json+dataclasses)
- **Don't merge spec concepts into a single type** — one type per CDDL definition
- **Don't validate during parsing** — parse() checks JSON syntax; validate() checks spec conformance
- **Don't duplicate publisher info in TrustManifest** — publisher is on CatalogEntry only
- **Don't ignore the url|inline exclusivity rule** — this is a MUST-level requirement
