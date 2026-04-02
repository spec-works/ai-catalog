"""Negative tests — loads each negative fixture, verifies parse/validate raises expected errors."""

from __future__ import annotations

import json
from typing import Any

import pytest

from aicatalog import AiCatalogParseError, parse, validate


def test_negative(negative_fixture: tuple[str, dict[str, Any] | str]) -> None:
    """Each negative fixture should produce the expected error during parse or validate."""
    name, fixture_data = negative_fixture

    # Special case: invalid-json.txt (raw text, not JSON wrapper)
    if isinstance(fixture_data, str):
        with pytest.raises(AiCatalogParseError):
            parse(fixture_data)
        return

    input_data = fixture_data["input"]
    expected_error = fixture_data["expected_error"]
    json_str = json.dumps(input_data)

    # Some errors are structural (parse-time), others are validation-time
    try:
        catalog = parse(json_str)
    except AiCatalogParseError as e:
        # Parse error — check the error message matches
        assert expected_error.lower() in str(e).lower(), (
            f"Expected error containing '{expected_error}', got '{e}'"
        )
        return

    # If parsing succeeded, validate and check for errors
    result = validate(catalog)
    assert not result.is_valid or len(result.errors) > 0 or len(result.warnings) > 0, (
        f"Expected validation failure for '{name}', but got valid result"
    )
    all_messages = result.errors + result.warnings
    matched = any(expected_error.lower() in msg.lower() for msg in all_messages)
    assert matched, (
        f"Expected error '{expected_error}' not found in: {all_messages}"
    )
