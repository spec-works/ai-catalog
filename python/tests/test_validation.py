"""Validation tests — conformance level detection and validation rules."""

from __future__ import annotations

import json
from typing import Any

import pytest

from aicatalog import (
    AiCatalog,
    AiCatalogParseError,
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


# ---------------------------------------------------------------------------
# Version handling tests (VH-1 through VH-8)
# ---------------------------------------------------------------------------


class TestVersionHandling:
    """Test specVersion Major.Minor format validation."""

    def test_version_1_0_accepted(self) -> None:
        """VH-P01: specVersion '1.0' is valid."""
        catalog = parse('{"specVersion": "1.0", "entries": []}')
        assert catalog.spec_version == "1.0"

    def test_version_1_1_accepted(self) -> None:
        """VH-P02: specVersion '1.1' (higher minor) is accepted within same major."""
        catalog = parse('{"specVersion": "1.1", "entries": []}')
        assert catalog.spec_version == "1.1"

    def test_version_1_99_accepted(self) -> None:
        """Higher minor versions within major 1 are accepted (VH-5)."""
        catalog = parse('{"specVersion": "1.99", "entries": []}')
        assert catalog.spec_version == "1.99"

    def test_version_0_1_accepted(self) -> None:
        """Major version 0 is accepted (lower than supported)."""
        catalog = parse('{"specVersion": "0.1", "entries": []}')
        assert catalog.spec_version == "0.1"

    def test_version_2_0_rejected(self) -> None:
        """VH-N01: specVersion '2.0' (unsupported major) is rejected."""
        with pytest.raises(AiCatalogParseError, match="unsupported specVersion major version"):
            parse('{"specVersion": "2.0", "entries": []}')

    def test_version_no_minor_rejected(self) -> None:
        """VH-N02: specVersion '1' (no minor) is rejected."""
        with pytest.raises(AiCatalogParseError, match="Major.Minor"):
            parse('{"specVersion": "1", "entries": []}')

    def test_version_three_segments_rejected(self) -> None:
        """VH-N03: specVersion '1.0.0' (extra segment) is rejected."""
        with pytest.raises(AiCatalogParseError, match="Major.Minor"):
            parse('{"specVersion": "1.0.0", "entries": []}')

    def test_version_negative_rejected(self) -> None:
        """VH-N04: specVersion '-1.0' (negative) is rejected."""
        with pytest.raises(AiCatalogParseError, match="non-negative"):
            parse('{"specVersion": "-1.0", "entries": []}')

    def test_version_non_integer_rejected(self) -> None:
        """VH-N05: specVersion 'a.b' (non-integer) is rejected."""
        with pytest.raises(AiCatalogParseError, match="non-negative integers"):
            parse('{"specVersion": "a.b", "entries": []}')


# ---------------------------------------------------------------------------
# Metadata validation tests (ME-2)
# ---------------------------------------------------------------------------


class TestMetadataValidation:
    """Test metadata key validation."""

    def test_empty_metadata_key_rejected(self) -> None:
        """ME-N01: Metadata with empty-string key is rejected at validation."""
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
            metadata={"": "bad-key"},
        )
        result = validate(catalog)
        assert not result.is_valid
        assert any("empty string" in e for e in result.errors)

    def test_valid_metadata_keys_accepted(self) -> None:
        """ME-P01: Metadata with non-empty keys is accepted."""
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
            metadata={"com.example.custom": "value", "simple-key": 42},
        )
        result = validate(catalog)
        assert result.is_valid

    def test_entry_empty_metadata_key_rejected(self) -> None:
        """ME-N01 at entry level: Empty metadata key on entry is rejected."""
        from aicatalog.models import CatalogEntry

        catalog = AiCatalog(
            spec_version="1.0",
            entries=[
                CatalogEntry(
                    identifier="urn:test:agent",
                    display_name="Test",
                    media_type="application/a2a-agent-card+json",
                    url="https://example.com/agent.json",
                    metadata={"": "bad"},
                )
            ],
        )
        result = validate(catalog)
        assert not result.is_valid
        assert any("empty string" in e for e in result.errors)


# ---------------------------------------------------------------------------
# url/data exclusivity tests
# ---------------------------------------------------------------------------


class TestUrlDataExclusivity:
    """Test url/data mutual exclusivity validation."""

    def test_both_url_and_data_rejected(self) -> None:
        """Entry with both url and data is rejected."""
        from aicatalog.models import CatalogEntry

        catalog = AiCatalog(
            spec_version="1.0",
            entries=[
                CatalogEntry(
                    identifier="urn:test:agent",
                    display_name="Test",
                    media_type="application/a2a-agent-card+json",
                    url="https://example.com/agent.json",
                    data={"key": "value"},
                )
            ],
        )
        result = validate(catalog)
        assert not result.is_valid
        assert any("'url' or 'data'" in e for e in result.errors)

    def test_neither_url_nor_data_rejected(self) -> None:
        """Entry with neither url nor data is rejected."""
        from aicatalog.models import CatalogEntry

        catalog = AiCatalog(
            spec_version="1.0",
            entries=[
                CatalogEntry(
                    identifier="urn:test:agent",
                    display_name="Test",
                    media_type="application/a2a-agent-card+json",
                )
            ],
        )
        result = validate(catalog)
        assert not result.is_valid
        assert any("'url' or 'data'" in e for e in result.errors)
