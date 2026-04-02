"""Integration tests using real-world marketplace.json fixtures.

Tests conversion, validation, CLI end-to-end, and round-trip fidelity
against two real marketplace files from spec-works/plugins and microsoft/work-iq.
"""

from __future__ import annotations

import json
from pathlib import Path

import pytest
from click.testing import CliRunner

from aicatalog import (
    AiCatalog,
    CatalogEntry,
    ConformanceLevel,
    convert_marketplace,
    convert_marketplace_file,
    parse,
    serialize,
    validate,
)
from aicatalog.cli.main import cli
from aicatalog.converter import CLAUDE_PLUGIN_MEDIA_TYPE, CLAUDE_PLUGIN_URN_PREFIX

# ---------------------------------------------------------------------------
# Fixture paths
# ---------------------------------------------------------------------------

INTEGRATION_DIR = Path(__file__).resolve().parent.parent.parent / "testcases" / "integration"
SPEC_WORKS_FIXTURE = INTEGRATION_DIR / "spec-works-plugins-marketplace.json"
WORK_IQ_FIXTURE = INTEGRATION_DIR / "work-iq-marketplace.json"


@pytest.fixture
def spec_works_catalog() -> AiCatalog:
    """Convert spec-works marketplace fixture to AiCatalog."""
    return convert_marketplace_file(SPEC_WORKS_FIXTURE)


@pytest.fixture
def work_iq_catalog() -> AiCatalog:
    """Convert work-iq marketplace fixture to AiCatalog."""
    return convert_marketplace_file(WORK_IQ_FIXTURE)


@pytest.fixture
def cli_runner() -> CliRunner:
    return CliRunner()


# ===================================================================
# 1. Conversion produces valid AiCatalog objects
# ===================================================================


class TestConversionProducesValidCatalog:
    """Verify marketplace conversion produces structurally valid catalogs."""

    def test_spec_works_returns_ai_catalog(self, spec_works_catalog: AiCatalog) -> None:
        assert isinstance(spec_works_catalog, AiCatalog)
        assert spec_works_catalog.spec_version == "1.0"

    def test_work_iq_returns_ai_catalog(self, work_iq_catalog: AiCatalog) -> None:
        assert isinstance(work_iq_catalog, AiCatalog)
        assert work_iq_catalog.spec_version == "1.0"

    def test_spec_works_serializes_to_valid_json(self, spec_works_catalog: AiCatalog) -> None:
        json_str = serialize(spec_works_catalog)
        data = json.loads(json_str)
        assert "specVersion" in data
        assert "entries" in data
        assert isinstance(data["entries"], list)

    def test_work_iq_serializes_to_valid_json(self, work_iq_catalog: AiCatalog) -> None:
        json_str = serialize(work_iq_catalog)
        data = json.loads(json_str)
        assert "specVersion" in data
        assert "entries" in data
        assert isinstance(data["entries"], list)


# ===================================================================
# 2. Entry counts match plugin counts
# ===================================================================


class TestEntryCounts:
    """Verify correct number of entries survive conversion."""

    def test_spec_works_has_5_entries(self, spec_works_catalog: AiCatalog) -> None:
        assert len(spec_works_catalog.entries) == 5

    def test_work_iq_has_3_entries(self, work_iq_catalog: AiCatalog) -> None:
        assert len(work_iq_catalog.entries) == 3


# ===================================================================
# 3. Spot-check: specific fields survive conversion
# ===================================================================


class TestFieldFidelity:
    """Spot-check that names, descriptions, identifiers, and versions survive."""

    # -- spec-works fixture --

    def test_markmyword_identifier(self, spec_works_catalog: AiCatalog) -> None:
        entry = _find_entry(spec_works_catalog, "markmyword")
        assert entry.identifier == f"{CLAUDE_PLUGIN_URN_PREFIX}markmyword"

    def test_markmyword_display_name(self, spec_works_catalog: AiCatalog) -> None:
        entry = _find_entry(spec_works_catalog, "markmyword")
        assert entry.display_name == "markmyword"

    def test_markmyword_description(self, spec_works_catalog: AiCatalog) -> None:
        entry = _find_entry(spec_works_catalog, "markmyword")
        assert entry.description == "Bidirectional Markdown and Word (.docx) conversion."

    def test_markmyword_version(self, spec_works_catalog: AiCatalog) -> None:
        entry = _find_entry(spec_works_catalog, "markmyword")
        assert entry.version == "1.0.0"

    def test_markmydeck_description(self, spec_works_catalog: AiCatalog) -> None:
        entry = _find_entry(spec_works_catalog, "markmydeck")
        assert entry.description == "Markdown to PowerPoint (.pptx) conversion."

    def test_a2a_ask_identifier(self, spec_works_catalog: AiCatalog) -> None:
        entry = _find_entry(spec_works_catalog, "a2a-ask")
        assert entry.identifier == f"{CLAUDE_PLUGIN_URN_PREFIX}a2a-ask"

    def test_officetalk_version(self, spec_works_catalog: AiCatalog) -> None:
        entry = _find_entry(spec_works_catalog, "officetalk")
        assert entry.version == "0.1.0"

    def test_all_spec_works_entries_have_claude_media_type(
        self, spec_works_catalog: AiCatalog
    ) -> None:
        for entry in spec_works_catalog.entries:
            assert entry.media_type == CLAUDE_PLUGIN_MEDIA_TYPE

    # -- work-iq fixture --

    def test_workiq_identifier(self, work_iq_catalog: AiCatalog) -> None:
        entry = _find_entry(work_iq_catalog, "workiq")
        assert entry.identifier == f"{CLAUDE_PLUGIN_URN_PREFIX}workiq"

    def test_workiq_description(self, work_iq_catalog: AiCatalog) -> None:
        entry = _find_entry(work_iq_catalog, "workiq")
        assert "Microsoft 365" in entry.description

    def test_m365_toolkit_version(self, work_iq_catalog: AiCatalog) -> None:
        entry = _find_entry(work_iq_catalog, "microsoft-365-agents-toolkit")
        assert entry.version == "1.1.1"

    def test_workiq_productivity_description(self, work_iq_catalog: AiCatalog) -> None:
        entry = _find_entry(work_iq_catalog, "workiq-productivity")
        assert "productivity" in entry.description.lower()

    def test_all_work_iq_entries_have_claude_media_type(
        self, work_iq_catalog: AiCatalog
    ) -> None:
        for entry in work_iq_catalog.entries:
            assert entry.media_type == CLAUDE_PLUGIN_MEDIA_TYPE

    def test_all_entries_have_identifiers_with_urn_prefix(
        self, spec_works_catalog: AiCatalog, work_iq_catalog: AiCatalog
    ) -> None:
        for catalog in (spec_works_catalog, work_iq_catalog):
            for entry in catalog.entries:
                assert entry.identifier.startswith(CLAUDE_PLUGIN_URN_PREFIX)


# ===================================================================
# 4. Conformance validation of converted catalogs
# ===================================================================


class TestConformanceValidation:
    """Validate conformance levels and expected validation results."""

    def test_spec_works_conformance_is_minimal(self, spec_works_catalog: AiCatalog) -> None:
        result = validate(spec_works_catalog)
        assert result.conformance_level == ConformanceLevel.MINIMAL

    def test_work_iq_conformance_is_minimal(self, work_iq_catalog: AiCatalog) -> None:
        result = validate(work_iq_catalog)
        assert result.conformance_level == ConformanceLevel.MINIMAL

    def test_spec_works_has_no_host(self, spec_works_catalog: AiCatalog) -> None:
        assert spec_works_catalog.host is None

    def test_work_iq_has_no_host(self, work_iq_catalog: AiCatalog) -> None:
        assert work_iq_catalog.host is None

    def test_validation_reports_missing_content(self, spec_works_catalog: AiCatalog) -> None:
        """Marketplace plugins lack url/inline, so validation should report errors."""
        result = validate(spec_works_catalog)
        content_errors = [e for e in result.errors if "url" in e or "inline" in e]
        assert len(content_errors) == 5  # one per entry

    def test_work_iq_validation_reports_missing_content(
        self, work_iq_catalog: AiCatalog
    ) -> None:
        result = validate(work_iq_catalog)
        content_errors = [e for e in result.errors if "url" in e or "inline" in e]
        assert len(content_errors) == 3  # one per entry


# ===================================================================
# 5. Round-trip: convert → serialize → parse → validate
# ===================================================================


class TestRoundTrip:
    """Verify converted catalogs survive serialize → parse round-trip."""

    def test_spec_works_round_trip(self, spec_works_catalog: AiCatalog) -> None:
        json_str = serialize(spec_works_catalog)
        parsed = parse(json_str)
        assert len(parsed.entries) == len(spec_works_catalog.entries)
        for orig, rt in zip(spec_works_catalog.entries, parsed.entries, strict=True):
            assert orig.identifier == rt.identifier
            assert orig.display_name == rt.display_name
            assert orig.description == rt.description
            assert orig.media_type == rt.media_type
            assert orig.version == rt.version

    def test_work_iq_round_trip(self, work_iq_catalog: AiCatalog) -> None:
        json_str = serialize(work_iq_catalog)
        parsed = parse(json_str)
        assert len(parsed.entries) == len(work_iq_catalog.entries)
        for orig, rt in zip(work_iq_catalog.entries, parsed.entries, strict=True):
            assert orig.identifier == rt.identifier
            assert orig.display_name == rt.display_name
            assert orig.description == rt.description
            assert orig.media_type == rt.media_type
            assert orig.version == rt.version

    def test_round_trip_preserves_spec_version(
        self, spec_works_catalog: AiCatalog
    ) -> None:
        json_str = serialize(spec_works_catalog)
        parsed = parse(json_str)
        assert parsed.spec_version == spec_works_catalog.spec_version

    def test_round_trip_conformance_is_stable(
        self, work_iq_catalog: AiCatalog
    ) -> None:
        """Conformance level should be the same before and after round-trip."""
        before = validate(work_iq_catalog)
        json_str = serialize(work_iq_catalog)
        parsed = parse(json_str)
        after = validate(parsed)
        assert before.conformance_level == after.conformance_level
        assert len(before.errors) == len(after.errors)

    def test_double_round_trip_produces_identical_json(
        self, spec_works_catalog: AiCatalog
    ) -> None:
        """serialize → parse → serialize should produce identical JSON."""
        json1 = serialize(spec_works_catalog)
        parsed = parse(json1)
        json2 = serialize(parsed)
        assert json1 == json2


# ===================================================================
# 6. CLI end-to-end: convert marketplace command
# ===================================================================


class TestCliConvertMarketplace:
    """Test the CLI `convert marketplace` command with real fixtures."""

    def test_spec_works_cli_stdout(self, cli_runner: CliRunner) -> None:
        result = cli_runner.invoke(cli, ["convert", "marketplace", str(SPEC_WORKS_FIXTURE)])
        assert result.exit_code == 0
        data = json.loads(result.output)
        assert data["specVersion"] == "1.0"
        assert len(data["entries"]) == 5

    def test_work_iq_cli_stdout(self, cli_runner: CliRunner) -> None:
        result = cli_runner.invoke(cli, ["convert", "marketplace", str(WORK_IQ_FIXTURE)])
        assert result.exit_code == 0
        data = json.loads(result.output)
        assert data["specVersion"] == "1.0"
        assert len(data["entries"]) == 3

    def test_cli_output_is_parseable_ai_catalog(self, cli_runner: CliRunner) -> None:
        result = cli_runner.invoke(cli, ["convert", "marketplace", str(SPEC_WORKS_FIXTURE)])
        assert result.exit_code == 0
        catalog = parse(result.output)
        assert isinstance(catalog, AiCatalog)
        assert len(catalog.entries) == 5

    def test_cli_output_to_file(self, cli_runner: CliRunner, tmp_path: Path) -> None:
        out_file = tmp_path / "output.json"
        result = cli_runner.invoke(
            cli,
            ["convert", "marketplace", str(WORK_IQ_FIXTURE), "--output", str(out_file)],
        )
        assert result.exit_code == 0
        assert out_file.exists()
        data = json.loads(out_file.read_text(encoding="utf-8"))
        assert len(data["entries"]) == 3

    def test_cli_output_file_parses_back(self, cli_runner: CliRunner, tmp_path: Path) -> None:
        out_file = tmp_path / "catalog.json"
        result = cli_runner.invoke(
            cli,
            ["convert", "marketplace", str(SPEC_WORKS_FIXTURE), "--output", str(out_file)],
        )
        assert result.exit_code == 0
        catalog = parse(out_file.read_text(encoding="utf-8"))
        assert len(catalog.entries) == 5
        vr = validate(catalog)
        assert vr.conformance_level == ConformanceLevel.MINIMAL

    def test_cli_spot_check_entry_in_output(self, cli_runner: CliRunner) -> None:
        result = cli_runner.invoke(cli, ["convert", "marketplace", str(WORK_IQ_FIXTURE)])
        assert result.exit_code == 0
        data = json.loads(result.output)
        identifiers = [e["identifier"] for e in data["entries"]]
        assert f"{CLAUDE_PLUGIN_URN_PREFIX}workiq" in identifiers
        assert f"{CLAUDE_PLUGIN_URN_PREFIX}microsoft-365-agents-toolkit" in identifiers
        assert f"{CLAUDE_PLUGIN_URN_PREFIX}workiq-productivity" in identifiers


# ===================================================================
# 7. Convert via library function (dict input, not file)
# ===================================================================


class TestConvertMarketplaceDict:
    """Test convert_marketplace() with dicts loaded from fixtures."""

    def test_spec_works_dict_conversion(self) -> None:
        data = json.loads(SPEC_WORKS_FIXTURE.read_text(encoding="utf-8"))
        catalog = convert_marketplace(data)
        assert len(catalog.entries) == 5

    def test_work_iq_dict_conversion(self) -> None:
        data = json.loads(WORK_IQ_FIXTURE.read_text(encoding="utf-8"))
        catalog = convert_marketplace(data)
        assert len(catalog.entries) == 3

    def test_dict_and_file_produce_same_result(self) -> None:
        data = json.loads(SPEC_WORKS_FIXTURE.read_text(encoding="utf-8"))
        from_dict = convert_marketplace(data)
        from_file = convert_marketplace_file(SPEC_WORKS_FIXTURE)
        assert serialize(from_dict) == serialize(from_file)


# ===================================================================
# Helpers
# ===================================================================


def _find_entry(catalog: AiCatalog, plugin_name: str) -> CatalogEntry:
    """Find a catalog entry by its original plugin name."""
    target_id = f"{CLAUDE_PLUGIN_URN_PREFIX}{plugin_name}"
    for entry in catalog.entries:
        if entry.identifier == target_id:
            return entry
    raise AssertionError(f"No entry found for plugin '{plugin_name}' (id: {target_id})")
