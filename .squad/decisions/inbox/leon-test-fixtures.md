# Test Fixture Design Decisions

**Date:** 2026-04-02  
**Author:** Leon (Test Author)  
**Status:** Active

---

## TFD-001: Expected Error Strings Are Descriptive, Not Exact

**Decision:** The `expected_error` field in negative fixtures contains human-readable descriptions of what should fail, not exact error message strings that implementations must match character-for-character.

**Rationale:** .NET and Python will naturally produce different error messages. The fixture describes the *category* of error (e.g., "missing required field: mediaType on entry[0]"), and each implementation's test harness should check that the validation result contains the relevant error type/code, not that the message string matches exactly.

---

## TFD-002: One Error Per Negative Fixture

**Decision:** Each negative fixture tests exactly one validation error. No fixture intentionally combines multiple errors.

**Rationale:** When a negative fixture fails unexpectedly, the developer needs to know immediately which single rule is broken. Combining errors makes it ambiguous whether the implementation caught error A but missed error B.

---

## TFD-003: Assertion Keys Use snake_case

**Decision:** The `expected` object uses snake_case keys (e.g., `entry_count`, `conformance_level`, `has_url`) regardless of language conventions.

**Rationale:** JSON is the interchange format. snake_case is unambiguous in JSON and avoids collision with the camelCase used in the actual AI Catalog spec fields. Both .NET and Python test harnesses will map these to their native conventions.

---

## TFD-004: test_ids Field Links to Tyrell's Test Descriptions

**Decision:** Every fixture includes a `test_ids` array referencing Tyrell's test case IDs (e.g., `["TL-P01", "CL-P01"]`).

**Rationale:** Enables traceability from fixtures back to the requirements extraction. If a test case from Tyrell's descriptions doesn't appear in any fixture's `test_ids`, we know there's a coverage gap.

---

## TFD-005: Marketplace Conversion Uses urn:claude:plugins:{name} Identifier Pattern

**Decision:** When converting Claude marketplace plugins to AI Catalog entries, the identifier uses the pattern `urn:claude:plugins:{plugin-name}` and the mediaType is `application/vnd.claude.code-plugin+json`.

**Rationale:** The spec doesn't dictate identifier format for converted entries, but a consistent URN pattern provides stable, unique identifiers. The marketplace plugin `name` field is already unique within a marketplace, making it a natural suffix.
