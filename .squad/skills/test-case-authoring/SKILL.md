---
name: "test-case-authoring"
description: "Patterns for creating shared, language-agnostic JSON test fixtures from spec requirements"
domain: "testing"
confidence: "high"
source: "earned"
---

## Context

When implementing a specification in multiple languages, shared test fixtures are the primary mechanism for ensuring cross-language consistency. This skill covers how to create effective JSON fixtures that any language's test harness can consume.

## Patterns

### Fixture Wrapper Format

Every fixture is a JSON object wrapping the actual input with metadata:
- `description` — human-readable purpose
- `spec_section` — traceability to the spec
- `test_ids` — links to test case descriptions
- `input` — the actual document under test (IS the document, not a reference)
- `expected` / `expected_error` — what correct parsing/validation should produce

### One Error Per Negative Fixture

Never combine multiple errors in a single negative fixture. Isolating errors makes debugging trivial — when a test fails, you know exactly which rule is broken.

### Inline Content Diversity

When a spec says a field accepts "any JSON value," test all five JSON types: object, string, number, array, boolean. Implementations commonly assume object-only.

### Realistic Data Over Synthetic

Use plausible identifiers (`urn:contoso:agents:data-analyst`), realistic URLs (`https://agents.contoso.com/...`), and meaningful display names. Synthetic minimal data hides bugs that only surface with real-world patterns.

### Conformance Level Layering

Create one fixture per conformance level, where each higher level includes everything from lower levels. This validates that level checking is cumulative.

### Cross-Field Validation Fixtures

When the spec requires field X to match field Y (e.g., `trustManifest.identity` must match `entry.identifier`), create a dedicated fixture for the mismatch case — this is easy to miss in implementation.

### snake_case Assertion Keys

Use snake_case for all assertion keys in the `expected` object to be language-neutral. The test harness maps these to native conventions.

## Examples

```json
// Positive fixture
{
  "description": "Minimal valid catalog",
  "spec_section": "§ Top-Level Structure",
  "test_ids": ["TL-P01"],
  "input": { "specVersion": "1.0", "entries": [] },
  "expected": { "valid": true, "entry_count": 0, "conformance_level": "minimal" }
}

// Negative fixture
{
  "description": "Missing required specVersion",
  "spec_section": "§ Top-Level Structure (TL-1)",
  "test_ids": ["TL-N01"],
  "input": { "entries": [] },
  "expected_error": "missing required field: specVersion"
}
```

## Anti-Patterns

- **Don't combine multiple errors in one negative fixture** — isolate each error
- **Don't use exact error message matching** — use error categories/codes
- **Don't assume inline is always an object** — test all JSON value types
- **Don't skip edge cases for optional fields** — test missing required sub-fields when an optional parent is present
- **Don't create fixtures without spec_section** — every fixture must be traceable
