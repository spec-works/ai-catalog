"""Validation tests — conformance level detection and validation rules."""

from __future__ import annotations

import json
from typing import Any

from aicatalog import (
    AiCatalog,
    ConformanceLevel,
    parse,
    validate,
    validate_level,
)


def test_conformance_level_detection(positive_fixture: tuple[str, dict[str, Any]]) -> None:
    """Verify auto-detected conformance level matches expected."""
    name, fixture = positive_fixture
    expected = fixture["expected"]
    if "conformance_level" not in expected:
        return

    input_data = fixture["input"]
    catalog = parse(json.dumps(input_data))
    result = validate(catalog)

    expected_level = expected["conformance_level"]
    assert result.conformance_level.value == expected_level, (
        f"Expected conformance level '{expected_level}', got '{result.conformance_level.value}'"
    )


def test_validation_valid_flag(positive_fixture: tuple[str, dict[str, Any]]) -> None:
    """Verify the is_valid flag matches expected."""
    name, fixture = positive_fixture
    expected = fixture["expected"]
    if "valid" not in expected:
        return

    input_data = fixture["input"]
    catalog = parse(json.dumps(input_data))
    result = validate(catalog)

    assert result.is_valid == expected["valid"], (
        f"Expected valid={expected['valid']}, got valid={result.is_valid}. "
        f"Errors: {result.errors}"
    )


def test_validation_warnings(positive_fixture: tuple[str, dict[str, Any]]) -> None:
    """Verify expected warnings are produced."""
    name, fixture = positive_fixture
    expected = fixture["expected"]
    if "warnings" not in expected:
        return

    input_data = fixture["input"]
    catalog = parse(json.dumps(input_data))
    result = validate(catalog)

    for expected_warning in expected["warnings"]:
        assert any(
            expected_warning.lower() in w.lower() for w in result.warnings
        ), f"Expected warning '{expected_warning}' not found in {result.warnings}"


def test_no_security_warnings(positive_fixture: tuple[str, dict[str, Any]]) -> None:
    """Verify no security warnings for HTTPS-compliant catalogs."""
    name, fixture = positive_fixture
    expected = fixture["expected"]
    if not expected.get("no_security_warnings"):
        return

    input_data = fixture["input"]
    catalog = parse(json.dumps(input_data))
    result = validate(catalog)

    security_keywords = ["http", "https", "security", "digest"]
    security_warnings = [
        w for w in result.warnings
        if any(kw in w.lower() for kw in security_keywords)
    ]
    assert len(security_warnings) == 0, f"Unexpected security warnings: {security_warnings}"


def test_validate_against_minimal() -> None:
    """Validate a minimal catalog against MINIMAL level."""
    from aicatalog.models import CatalogEntry

    catalog = AiCatalog(
        spec_version="1.0",
        entries=[
            CatalogEntry(
                identifier="urn:test:agent",
                display_name="Test",
                media_type="application/a2a-agent-card+json",
                url="https://example.com/agent.json",
            )
        ],
    )
    result = validate_level(catalog, ConformanceLevel.MINIMAL)
    assert result.is_valid
    assert result.conformance_level == ConformanceLevel.MINIMAL


def test_validate_against_discoverable_fails_without_host() -> None:
    """Validate that DISCOVERABLE requires host info."""
    from aicatalog.models import CatalogEntry

    catalog = AiCatalog(
        spec_version="1.0",
        entries=[
            CatalogEntry(
                identifier="urn:test:agent",
                display_name="Test",
                media_type="application/a2a-agent-card+json",
                url="https://example.com/agent.json",
            )
        ],
    )
    result = validate_level(catalog, ConformanceLevel.DISCOVERABLE)
    assert not result.is_valid
    assert any("host" in e for e in result.errors)


def test_validate_against_trusted_fails_without_trust_manifest() -> None:
    """Validate that TRUSTED requires trust manifests on entries."""
    from aicatalog.models import CatalogEntry, HostInfo

    catalog = AiCatalog(
        spec_version="1.0",
        host=HostInfo(display_name="Test Host"),
        entries=[
            CatalogEntry(
                identifier="urn:test:agent",
                display_name="Test",
                media_type="application/a2a-agent-card+json",
                url="https://example.com/agent.json",
            )
        ],
    )
    result = validate_level(catalog, ConformanceLevel.TRUSTED)
    assert not result.is_valid
    assert any("trustManifest" in e for e in result.errors)
