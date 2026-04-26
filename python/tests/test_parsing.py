"""Parsing tests — loads each positive fixture, parses input, verifies expected assertions."""

from __future__ import annotations

import json
from typing import Any

from aicatalog import AiCatalog, parse
from aicatalog.models import CatalogEntry


def _check_entry_assertions(entry: CatalogEntry, exp: dict[str, Any], index: int) -> None:
    """Verify expected assertions against a parsed entry."""
    if "identifier" in exp:
        assert entry.identifier == exp["identifier"], f"entry[{index}].identifier"
    if "display_name" in exp:
        assert entry.display_name == exp["display_name"], f"entry[{index}].display_name"
    if "media_type" in exp:
        assert entry.media_type == exp["media_type"], f"entry[{index}].media_type"
    if "has_url" in exp:
        assert (entry.url is not None) == exp["has_url"], f"entry[{index}].has_url"
    if "has_data" in exp:
        assert (entry.data is not None) == exp["has_data"], f"entry[{index}].has_data"
    # Legacy alias for backward compat with fixtures
    if "has_inline" in exp:
        assert (entry.data is not None) == exp["has_inline"], f"entry[{index}].has_inline"
    if "has_description" in exp:
        assert (entry.description is not None) == exp["has_description"]
    if "has_version" in exp:
        assert (entry.version is not None) == exp["has_version"]
    if "has_tags" in exp:
        assert (len(entry.tags) > 0) == exp["has_tags"]
    if "tag_count" in exp:
        assert len(entry.tags) == exp["tag_count"]
    if "has_updated_at" in exp:
        assert (entry.updated_at is not None) == exp["has_updated_at"]
    if "has_publisher" in exp:
        assert (entry.publisher is not None) == exp["has_publisher"]
    if "has_trust_manifest" in exp:
        assert (entry.trust_manifest is not None) == exp["has_trust_manifest"]
    if "has_metadata" in exp:
        assert (entry.metadata is not None) == exp["has_metadata"]
    if "trust_manifest_identity_matches" in exp and entry.trust_manifest is not None:
        matches = entry.trust_manifest.identity == entry.identifier
        assert matches == exp["trust_manifest_identity_matches"]
    if "publisher_has_identity_type" in exp and entry.publisher is not None:
        assert (entry.publisher.identity_type is not None) == exp["publisher_has_identity_type"]
    if "attestation_count" in exp and entry.trust_manifest is not None:
        assert len(entry.trust_manifest.attestations) == exp["attestation_count"]
    if "provenance_count" in exp and entry.trust_manifest is not None:
        assert len(entry.trust_manifest.provenance) == exp["provenance_count"]
    if "is_bundle" in exp:
        is_nested = (
            entry.media_type == "application/ai-catalog+json"
            and entry.data is not None
            and isinstance(entry.data, dict)
        )
        assert is_nested == exp["is_bundle"], f"entry[{index}].is_bundle"
    if "is_nested_catalog" in exp:
        is_nested = (
            entry.media_type == "application/ai-catalog+json"
            and entry.data is not None
            and isinstance(entry.data, dict)
        )
        assert is_nested == exp["is_nested_catalog"], f"entry[{index}].is_nested_catalog"
    if "nested_spec_version" in exp:
        assert isinstance(entry.data, dict)
        assert entry.data.get("specVersion") == exp["nested_spec_version"]
    if "nested_entry_count" in exp:
        assert isinstance(entry.data, dict)
        assert len(entry.data.get("entries", [])) == exp["nested_entry_count"]
    # Data type assertions
    if "data_is_object" in exp:
        assert isinstance(entry.data, dict) == exp["data_is_object"]
    if "data_is_array" in exp:
        assert isinstance(entry.data, list) == exp["data_is_array"]
    if "data_is_string" in exp:
        assert isinstance(entry.data, str) == exp["data_is_string"]
    if "data_is_boolean" in exp:
        assert isinstance(entry.data, bool) == exp["data_is_boolean"]
    if "data_is_number" in exp:
        is_num = isinstance(entry.data, (int, float)) and not isinstance(entry.data, bool)
        assert is_num == exp["data_is_number"]
    # Legacy inline type assertions (for fixtures not yet updated)
    if "inline_is_object" in exp:
        assert isinstance(entry.data, dict) == exp["inline_is_object"]
    if "inline_is_array" in exp:
        assert isinstance(entry.data, list) == exp["inline_is_array"]
    if "inline_is_string" in exp:
        assert isinstance(entry.data, str) == exp["inline_is_string"]
    if "inline_is_boolean" in exp:
        assert isinstance(entry.data, bool) == exp["inline_is_boolean"]
    if "inline_is_number" in exp:
        is_num = isinstance(entry.data, (int, float)) and not isinstance(entry.data, bool)
        assert is_num == exp["inline_is_number"]
    if "attestation_uri_scheme" in exp and entry.trust_manifest is not None:
        for att in entry.trust_manifest.attestations:
            scheme = att.uri.split(":")[0] if ":" in att.uri else ""
            assert scheme == exp["attestation_uri_scheme"]


def test_parse_positive(positive_fixture: tuple[str, dict[str, Any]]) -> None:
    """Parse each positive fixture and verify expected assertions."""
    name, fixture = positive_fixture
    input_data = fixture["input"]
    expected = fixture["expected"]
    json_str = json.dumps(input_data)

    catalog = parse(json_str)
    assert isinstance(catalog, AiCatalog)

    # Top-level assertions
    if "spec_version" in expected:
        assert catalog.spec_version == expected["spec_version"]
    if "entry_count" in expected:
        assert len(catalog.entries) == expected["entry_count"]
    if "has_host" in expected:
        assert (catalog.host is not None) == expected["has_host"]
    if "has_metadata" in expected:
        assert (catalog.metadata is not None) == expected["has_metadata"]

    # Host assertions
    if "host" in expected and catalog.host is not None:
        host_exp = expected["host"]
        if "display_name" in host_exp:
            assert catalog.host.display_name == host_exp["display_name"]
        if "has_identifier" in host_exp:
            assert (catalog.host.identifier is not None) == host_exp["has_identifier"]
        if "has_documentation_url" in host_exp:
            assert (catalog.host.documentation_url is not None) == host_exp["has_documentation_url"]
        if "has_logo_url" in host_exp:
            assert (catalog.host.logo_url is not None) == host_exp["has_logo_url"]
        if "has_trust_manifest" in host_exp:
            assert (catalog.host.trust_manifest is not None) == host_exp["has_trust_manifest"]

    # Host display name from top-level
    if "host_display_name" in expected and catalog.host is not None:
        assert catalog.host.display_name == expected["host_display_name"]

    # Entry assertions
    if "entries" in expected:
        entry_exps = expected["entries"]
        for i, entry_exp in enumerate(entry_exps):
            assert i < len(catalog.entries), (
                f"Expected entry[{i}] but only {len(catalog.entries)} entries"
            )
            _check_entry_assertions(catalog.entries[i], entry_exp, i)

    # Extension fields preserved
    if "extension_fields_preserved" in expected:
        for ext_field in expected["extension_fields_preserved"]:
            assert ext_field in catalog.extra_fields, f"extension field '{ext_field}' not preserved"

    if "entry_extension_fields_preserved" in expected:
        for ext_field in expected["entry_extension_fields_preserved"]:
            assert any(
                ext_field in e.extra_fields for e in catalog.entries
            ), f"entry extension field '{ext_field}' not preserved"

    # Multi-version checks
    if "entries_share_identifier" in expected:
        ids = [e.identifier for e in catalog.entries]
        assert len(ids) != len(set(ids)) == expected["entries_share_identifier"]

    if "all_versions_unique" in expected and expected["all_versions_unique"]:
        versions = [(e.identifier, e.version) for e in catalog.entries]
        assert len(versions) == len(set(versions))

    # Media types
    if "media_types_present" in expected:
        actual_types = {e.media_type for e in catalog.entries}
        for mt in expected["media_types_present"]:
            assert mt in actual_types, f"media type '{mt}' not found"

    if "all_media_types_equal" in expected and expected["all_media_types_equal"]:
        types = {e.media_type for e in catalog.entries}
        if catalog.entries:
            assert len(types) == 1

    # Identifier schemes
    if "identifier_schemes" in expected:
        for scheme in expected["identifier_schemes"]:
            assert any(
                e.identifier.startswith(scheme + ":") or e.identifier.startswith(scheme + "//")
                for e in catalog.entries
            ), f"identifier scheme '{scheme}' not found"

    # All entries have publisher
    if "all_entries_have_publisher" in expected and expected["all_entries_have_publisher"]:
        for e in catalog.entries:
            assert e.publisher is not None

    # Cross-bundle identifier reuse
    if "cross_bundle_identifier_reuse" in expected and expected["cross_bundle_identifier_reuse"]:
        assert "reused_identifier" in expected

    # Security
    if "all_urls_https" in expected and expected["all_urls_https"]:
        for e in catalog.entries:
            if e.url:
                assert e.url.startswith("https://"), f"Entry URL not HTTPS: {e.url}"

    # host_has_trust_manifest
    if "host_has_trust_manifest" in expected and catalog.host is not None:
        assert (catalog.host.trust_manifest is not None) == expected["host_has_trust_manifest"]

    # entries_with_trust_manifest
    if "entries_with_trust_manifest" in expected:
        count = sum(1 for e in catalog.entries if e.trust_manifest is not None)
        assert count == expected["entries_with_trust_manifest"]
