"""Serialization tests — round-trip: parse → serialize → parse, compare."""

from __future__ import annotations

import json
from typing import Any

from aicatalog import AiCatalog, parse, serialize, serialize_to_dict


def _compare_catalogs(original: AiCatalog, roundtripped: AiCatalog) -> None:
    """Deep comparison of two AiCatalog instances."""
    assert original.spec_version == roundtripped.spec_version
    assert len(original.entries) == len(roundtripped.entries)
    assert len(original.collections) == len(roundtripped.collections)

    # Host
    assert (original.host is None) == (roundtripped.host is None)
    if original.host and roundtripped.host:
        assert original.host.display_name == roundtripped.host.display_name
        assert original.host.identifier == roundtripped.host.identifier
        assert original.host.documentation_url == roundtripped.host.documentation_url
        assert original.host.logo_url == roundtripped.host.logo_url

    # Entries
    for i, (orig, rt) in enumerate(
        zip(original.entries, roundtripped.entries, strict=True)
    ):
        assert orig.identifier == rt.identifier, f"entry[{i}].identifier"
        assert orig.display_name == rt.display_name, f"entry[{i}].display_name"
        assert orig.media_type == rt.media_type, f"entry[{i}].media_type"
        assert orig.url == rt.url, f"entry[{i}].url"
        assert orig.inline == rt.inline, f"entry[{i}].inline"
        assert orig.version == rt.version, f"entry[{i}].version"
        assert orig.description == rt.description, f"entry[{i}].description"
        assert orig.tags == rt.tags, f"entry[{i}].tags"
        assert orig.updated_at == rt.updated_at, f"entry[{i}].updated_at"
        assert orig.metadata == rt.metadata, f"entry[{i}].metadata"

        # Publisher
        assert (orig.publisher is None) == (rt.publisher is None)
        if orig.publisher and rt.publisher:
            assert orig.publisher.identifier == rt.publisher.identifier
            assert orig.publisher.display_name == rt.publisher.display_name
            assert orig.publisher.identity_type == rt.publisher.identity_type

        # Trust manifest
        assert (orig.trust_manifest is None) == (rt.trust_manifest is None)
        if orig.trust_manifest and rt.trust_manifest:
            assert orig.trust_manifest.identity == rt.trust_manifest.identity
            assert len(orig.trust_manifest.attestations) == len(rt.trust_manifest.attestations)
            assert len(orig.trust_manifest.provenance) == len(rt.trust_manifest.provenance)

    # Collections
    for i, (orig, rt) in enumerate(
        zip(original.collections, roundtripped.collections, strict=True)
    ):
        assert orig.display_name == rt.display_name, f"collection[{i}].display_name"
        assert orig.url == rt.url, f"collection[{i}].url"
        assert orig.description == rt.description, f"collection[{i}].description"
        assert orig.tags == rt.tags, f"collection[{i}].tags"

    # Metadata
    assert original.metadata == roundtripped.metadata


def test_round_trip(positive_fixture: tuple[str, dict[str, Any]]) -> None:
    """Parse → serialize → parse should produce equivalent result."""
    name, fixture = positive_fixture
    input_data = fixture["input"]
    json_str = json.dumps(input_data)

    # Parse original
    original = parse(json_str)

    # Serialize back
    serialized = serialize(original)

    # Parse again
    roundtripped = parse(serialized)

    # Compare
    _compare_catalogs(original, roundtripped)


def test_round_trip_preserves_metadata(positive_fixture: tuple[str, dict[str, Any]]) -> None:
    """Verify metadata preservation in round-trip."""
    name, fixture = positive_fixture
    expected = fixture["expected"]

    if not expected.get("round_trip_preserves_metadata"):
        return

    input_data = fixture["input"]
    json_str = json.dumps(input_data)

    original = parse(json_str)
    serialized_dict = serialize_to_dict(original)
    roundtripped = parse(json.dumps(serialized_dict))

    # Top-level metadata must match exactly
    assert original.metadata == roundtripped.metadata

    # Entry-level metadata must match
    for i, (orig, rt) in enumerate(zip(original.entries, roundtripped.entries, strict=True)):
        assert orig.metadata == rt.metadata, f"entry[{i}].metadata not preserved"


def test_serialize_omits_none() -> None:
    """Verify that None optional fields are omitted from serialized output."""
    from aicatalog.models import CatalogEntry

    catalog = AiCatalog(
        spec_version="1.0",
        entries=[
            CatalogEntry(
                identifier="urn:test:agent",
                display_name="Test Agent",
                media_type="application/a2a-agent-card+json",
                url="https://example.com/agent.json",
            )
        ],
    )
    d = serialize_to_dict(catalog)
    entry = d["entries"][0]
    assert "version" not in entry
    assert "description" not in entry
    assert "publisher" not in entry
    assert "trustManifest" not in entry
    assert "updatedAt" not in entry
    assert "metadata" not in entry
    assert "tags" not in entry  # empty list omitted


def test_serialize_to_dict_camel_case() -> None:
    """Verify snake_case → camelCase mapping in serialized output."""
    from aicatalog.models import CatalogEntry

    catalog = AiCatalog(
        spec_version="1.0",
        entries=[
            CatalogEntry(
                identifier="urn:test:agent",
                display_name="Test Agent",
                media_type="application/a2a-agent-card+json",
                url="https://example.com/agent.json",
                updated_at="2026-01-01T00:00:00Z",
            )
        ],
    )
    d = serialize_to_dict(catalog)
    assert "specVersion" in d
    assert "spec_version" not in d
    entry = d["entries"][0]
    assert "displayName" in entry
    assert "mediaType" in entry
    assert "updatedAt" in entry
