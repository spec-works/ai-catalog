# AI Catalog — Shared Test Fixtures

Language-agnostic test fixtures for the AI Catalog specification (`application/ai-catalog+json`).  
Both the .NET (xUnit) and Python (pytest) implementations consume these fixtures.

## Fixture Format

### Positive Test Cases

```json
{
  "description": "Human-readable description of what this tests",
  "spec_section": "Section reference from the AI Card spec",
  "test_ids": ["TL-P01"],
  "input": { /* the AI Catalog JSON document to parse */ },
  "expected": {
    "valid": true,
    "entry_count": 1,
    "conformance_level": "minimal"
    /* additional assertions */
  }
}
```

### Negative Test Cases

```json
{
  "description": "What's wrong with this input",
  "spec_section": "Section reference",
  "test_ids": ["TL-N01"],
  "input": { /* invalid AI Catalog JSON */ },
  "expected_error": "description of expected error"
}
```

### Marketplace Conversion Test Cases

The `marketplace-input.json` and `marketplace-expected.json` pair tests the `convert marketplace` command. The input contains a Claude marketplace format; the expected output is the corresponding AI Catalog document.

## Key Fields

| Field | Required | Description |
|-------|----------|-------------|
| `description` | Yes | What the test validates |
| `spec_section` | Yes | Traceability to the AI Card spec section |
| `test_ids` | Yes | References to Tyrell's test case IDs from `docs/test-case-descriptions.md` |
| `input` | Yes | The JSON document under test (the `input` value **is** the catalog) |
| `expected` | For positive | Assertions about successful parse results |
| `expected_error` | For negative | Pattern/description of the expected validation error |

## Directory Layout

```
testcases/
├── README.md                          ← this file
├── minimal-catalog.json               ← Level 1 simplest valid catalog
├── empty-entries.json                 ← valid catalog with empty entries array
├── inline-artifact.json               ← entry using data (JSON object)
├── inline-string.json                 ← data as JSON string
├── inline-number.json                 ← data as JSON number
├── inline-array.json                  ← data as JSON array
├── inline-boolean.json                ← data as JSON boolean
├── spec-example-multi-artifact.json   ← multi-artifact with nested catalog
├── hierarchical-catalog.json          ← hierarchical organization with nested catalogs
├── dual-protocol-agent.json           ← dual-protocol agent (MCP + A2A)
├── all-properties.json                ← every field on every object populated
├── multi-version-entries.json         ← same identifier, different versions
├── nested-catalog.json                ← nested catalog entry
├── nested-depth-at-limit.json         ← nested catalog at depth limit (4)
├── discoverable-catalog.json          ← Level 2 with host info
├── trusted-catalog.json               ← Level 3 with trust manifests
├── extension-fields.json              ← unknown x- fields preserved
├── mixed-media-types.json             ← A2A, MCP, Claude plugin, Parquet, nested catalog
├── identifier-formats.json            ← URN, HTTPS, DID identifiers
├── identifier-plain-string.json       ← non-URI identifier (warns)
├── host-minimal.json                  ← host with only displayName
├── attestation-data-uri.json          ← attestation using data: URI
├── mcp-registry-catalog.json          ← MCP registry as AI Catalog
├── metadata-round-trip.json           ← nested metadata preservation
├── metadata-reverse-dns.json          ← metadata with reverse-DNS keys
├── metadata-all-types.json            ← metadata with all JSON value types
├── claude-plugin-entry.json           ← Claude Code plugin entry
├── all-https-urls.json                ← all URLs use HTTPS
├── entry-all-optional-fields.json     ← entry with every optional field
├── cross-catalog-identifier-reuse.json← identifier reuse across nested catalogs
├── version-1.0.json                   ← specVersion 1.0 format validation
├── version-1.1.json                   ← specVersion 1.1 forward compatibility
├── marketplace-input.json             ← Claude marketplace.json input
├── marketplace-expected.json          ← expected ai-catalog conversion output
└── negative/
    ├── missing-spec-version.json          ← TL-1: specVersion required
    ├── missing-entries.json               ← TL-2: entries required
    ├── missing-entry-identifier.json      ← CE-1: identifier required
    ├── missing-entry-display-name.json    ← CE-3: displayName required
    ├── missing-entry-media-type.json      ← CE-4: mediaType required
    ├── missing-entry-content.json         ← CE-5: url or data required
    ├── both-url-and-data.json             ← CE-5: not both
    ├── data-null.json                     ← DA-N03: data: null treated as absent
    ├── duplicate-identifier.json          ← MV-3: unique when no version
    ├── duplicate-identifier-version.json  ← MV-2: unique (id, version)
    ├── triple-entry-duplicate-version.json← MV-2: three entries, two clash
    ├── trust-identity-mismatch.json       ← TI-3: identity must match id
    ├── invalid-json.txt                   ← not valid JSON at all
    ├── missing-attestation-type.json      ← AT-1: type required
    ├── missing-attestation-uri.json       ← AT-2: uri required
    ├── missing-attestation-media-type.json← AT-3: mediaType required
    ├── missing-publisher-fields.json      ← PO-1/2: id + displayName
    ├── missing-host-display-name.json     ← HI-1: displayName required
    ├── missing-trust-manifest-identity.json ← TI-1: identity required
    ├── missing-trust-schema-fields.json   ← TS-1/2: identifier + version
    ├── missing-provenance-fields.json     ← PL-1/2: relation + sourceId
    ├── spec-version-wrong-type.json       ← TL-1: must be string
    ├── spec-version-null.json             ← TL-1: must not be null
    ├── spec-version-empty.json            ← TL-1: must not be empty
    ├── version-unsupported-major.json     ← VH-N01: reject unsupported major version
    ├── version-no-minor.json              ← VH-N02: Major.Minor format required
    ├── version-three-segments.json        ← VH-N03: no patch version allowed
    ├── version-negative.json              ← VH-N04: non-negative integers required
    ├── version-non-integer.json           ← VH-N05: integer components required
    ├── metadata-empty-key.json            ← ME-N01: non-empty keys required
    ├── entries-wrong-type.json            ← TL-2: must be array
    ├── root-not-object.json               ← root must be object
    ├── invalid-tags-type.json             ← CE-10: tags must be strings
    ├── invalid-updated-at.json            ← CE-13: RFC 3339 required
    ├── updated-at-wrong-type.json         ← CE-13: must be string
    ├── weak-digest-algorithm.json         ← VD-3: reject SHA-1
    ├── attestation-negative-size.json     ← AT-5: size must be uint
    ├── host-display-name-wrong-type.json  ← HI-1: must be string
    ├── invalid-nested-catalog.json        ← NC-1: data must be valid catalog
    ├── depth-exceeds-limit.json           ← SC-N02: depth > 4 exceeds limit
    └── http-url-not-https.json            ← SC-1: HTTPS required

## How to Consume in Tests

### .NET (xUnit)

```csharp
var fixturePath = Path.Combine(TestContext.TestCasesDir, "minimal-catalog.json");
var fixture = JsonSerializer.Deserialize<TestFixture>(File.ReadAllText(fixturePath));
var catalog = AiCatalogParser.Parse(JsonSerializer.Serialize(fixture.Input));
Assert.Equal(fixture.Expected.EntryCount, catalog.Entries.Count);
```

### Python (pytest)

```python
import json, pytest
from pathlib import Path

TESTCASES = Path(__file__).parent.parent.parent / "testcases"

def load_fixture(name):
    return json.loads((TESTCASES / name).read_text())

def test_minimal_catalog():
    fixture = load_fixture("minimal-catalog.json")
    catalog = parse(json.dumps(fixture["input"]))
    assert len(catalog.entries) == fixture["expected"]["entry_count"]
```

## Test Coverage Summary

| Category | Positive | Negative | Total |
|----------|----------|----------|-------|
| Positive fixtures | 32 | — | 32 |
| Negative fixtures | — | 40 | 40 |
| Marketplace conversion | 2 | — | 2 |
| **Total** | **34** | **40** | **74** |

## Spec Section Cross-Reference

Every fixture includes a `spec_section` field linking back to the AI Card spec and a `test_ids` array
referencing Tyrell's test case descriptions in `docs/test-case-descriptions.md`.
