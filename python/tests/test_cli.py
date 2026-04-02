"""Tests for the ai-catalog CLI.

Tests the three CLI commands:
- convert marketplace (validated against shared fixtures)
- explore (mocked HTTP, table and detail output)
- install (mocked HTTP, MCP config and skill download)
"""

from __future__ import annotations

import json
from pathlib import Path
from unittest.mock import MagicMock, patch

from click.testing import CliRunner

from aicatalog.cli.main import cli
from aicatalog.converter import convert_marketplace, convert_marketplace_file

# Path to shared cross-language test fixtures
TESTCASES_DIR = Path(__file__).resolve().parent.parent.parent / "testcases"


# ---------------------------------------------------------------------------
# Converter unit tests
# ---------------------------------------------------------------------------


class TestConverterUnit:
    """Unit tests for the marketplace converter logic."""

    def test_convert_single_plugin(self) -> None:
        plugin = {
            "name": "test-plugin",
            "display_name": "Test Plugin",
            "description": "A test plugin",
            "manifest_url": "https://example.com/manifest.json",
            "categories": ["test", "demo"],
            "version": "1.0.0",
            "publisher": {"name": "TestCo", "url": "https://testco.com"},
        }
        catalog = convert_marketplace({"plugins": [plugin]})
        assert catalog.spec_version == "1.0"
        assert len(catalog.entries) == 1
        entry = catalog.entries[0]
        assert entry.identifier == "urn:claude:plugins:test-plugin"
        assert entry.display_name == "Test Plugin"
        assert entry.media_type == "application/vnd.claude.code-plugin+json"
        assert entry.url == "https://example.com/manifest.json"
        assert entry.version == "1.0.0"
        assert entry.tags == ["test", "demo"]
        assert entry.publisher is not None
        assert entry.publisher.display_name == "TestCo"
        assert entry.publisher.identifier == "https://testco.com"

    def test_convert_empty_marketplace(self) -> None:
        catalog = convert_marketplace({"plugins": []})
        assert catalog.spec_version == "1.0"
        assert len(catalog.entries) == 0

    def test_convert_plugin_without_optional_fields(self) -> None:
        plugin = {
            "name": "minimal",
            "display_name": "Minimal",
            "manifest_url": "https://example.com/m.json",
        }
        catalog = convert_marketplace({"plugins": [plugin]})
        entry = catalog.entries[0]
        assert entry.identifier == "urn:claude:plugins:minimal"
        assert entry.version is None
        assert entry.tags == []
        assert entry.publisher is None


# ---------------------------------------------------------------------------
# convert marketplace CLI command — fixture-based tests
# ---------------------------------------------------------------------------


class TestConvertMarketplaceCLI:
    """CLI tests for 'ai-catalog convert marketplace'."""

    def test_convert_fixture_stdout(self) -> None:
        """Convert marketplace-input.json and verify output matches expected."""
        input_file = TESTCASES_DIR / "marketplace-input.json"
        expected_file = TESTCASES_DIR / "marketplace-expected.json"
        assert input_file.exists(), f"Missing fixture: {input_file}"
        assert expected_file.exists(), f"Missing fixture: {expected_file}"

        runner = CliRunner()
        result = runner.invoke(cli, ["convert", "marketplace", str(input_file)])
        assert result.exit_code == 0, f"CLI error: {result.output}"

        output_data = json.loads(result.output)
        expected_wrapper = json.loads(expected_file.read_text(encoding="utf-8"))
        expected_data = expected_wrapper["input"]

        # Verify structure matches expected
        assert output_data["specVersion"] == expected_data["specVersion"]
        assert len(output_data["entries"]) == len(expected_data["entries"])

        for actual_entry, expected_entry in zip(
            output_data["entries"], expected_data["entries"], strict=True
        ):
            assert actual_entry["identifier"] == expected_entry["identifier"]
            assert actual_entry["displayName"] == expected_entry["displayName"]
            assert actual_entry["mediaType"] == expected_entry["mediaType"]
            assert actual_entry["url"] == expected_entry["url"]
            if "version" in expected_entry:
                assert actual_entry["version"] == expected_entry["version"]
            if "tags" in expected_entry:
                assert actual_entry["tags"] == expected_entry["tags"]
            if "publisher" in expected_entry:
                assert actual_entry["publisher"] == expected_entry["publisher"]
            if "updatedAt" in expected_entry:
                assert actual_entry.get("updatedAt") == expected_entry["updatedAt"]

    def test_convert_fixture_to_file(self, tmp_path: Path) -> None:
        """Convert marketplace fixture and write to output file."""
        input_file = TESTCASES_DIR / "marketplace-input.json"
        output_file = tmp_path / "result.json"

        runner = CliRunner()
        result = runner.invoke(
            cli,
            ["convert", "marketplace", str(input_file), "--output", str(output_file)],
        )
        assert result.exit_code == 0
        assert output_file.exists()
        data = json.loads(output_file.read_text(encoding="utf-8"))
        assert data["specVersion"] == "1.0"
        assert len(data["entries"]) == 3

    def test_convert_validates_expected_properties(self) -> None:
        """Validate the expected assertion properties from the fixture."""
        expected_file = TESTCASES_DIR / "marketplace-expected.json"
        expected_wrapper = json.loads(expected_file.read_text(encoding="utf-8"))
        expected = expected_wrapper["expected"]

        input_file = TESTCASES_DIR / "marketplace-input.json"
        catalog = convert_marketplace_file(input_file)

        from aicatalog import validate

        vr = validate(catalog)

        assert vr.is_valid == expected["valid"]
        assert vr.conformance_level.value == expected["conformance_level"]
        assert len(catalog.entries) == expected["entry_count"]

        # All media types should equal the expected value
        for entry in catalog.entries:
            assert entry.media_type == expected["all_media_types_equal"]

        # All entries should have a publisher
        assert all(e.publisher is not None for e in catalog.entries) == expected[
            "all_entries_have_publisher"
        ]

    def test_convert_nonexistent_file(self) -> None:
        """CLI should error on missing input file."""
        runner = CliRunner()
        result = runner.invoke(cli, ["convert", "marketplace", "nonexistent.json"])
        assert result.exit_code != 0

    def test_convert_marketplace_file_wrapper_format(self) -> None:
        """Convert from test fixture wrapper (has 'input' key)."""
        input_file = TESTCASES_DIR / "marketplace-input.json"
        catalog = convert_marketplace_file(input_file)
        assert len(catalog.entries) == 3
        assert catalog.entries[0].identifier == "urn:claude:plugins:web-search"

    def test_convert_marketplace_file_raw_format(self, tmp_path: Path) -> None:
        """Convert from raw marketplace format (no wrapper)."""
        raw = {
            "plugins": [
                {
                    "name": "raw-test",
                    "display_name": "Raw Test",
                    "manifest_url": "https://example.com/raw.json",
                }
            ]
        }
        raw_file = tmp_path / "raw.json"
        raw_file.write_text(json.dumps(raw), encoding="utf-8")

        catalog = convert_marketplace_file(raw_file)
        assert len(catalog.entries) == 1
        assert catalog.entries[0].identifier == "urn:claude:plugins:raw-test"


# ---------------------------------------------------------------------------
# explore CLI command tests
# ---------------------------------------------------------------------------


_SAMPLE_CATALOG_JSON = json.dumps(
    {
        "specVersion": "1.0",
        "entries": [
            {
                "identifier": "urn:test:alpha",
                "displayName": "Alpha Tool",
                "mediaType": "application/vnd.mcp+json",
                "url": "https://example.com/alpha",
                "version": "1.0.0",
                "tags": ["search", "web"],
                "description": "Alpha description",
            },
            {
                "identifier": "urn:test:beta",
                "displayName": "Beta Skill",
                "mediaType": "application/vnd.skill+json",
                "url": "https://example.com/beta",
                "tags": ["productivity"],
            },
        ],
    }
)


class TestExploreCLI:
    """CLI tests for 'ai-catalog explore'."""

    def _mock_urlopen(self, url: str, catalog_json: str = _SAMPLE_CATALOG_JSON) -> MagicMock:
        """Create a mock for urllib.request.urlopen."""
        mock_resp = MagicMock()
        mock_resp.read.return_value = catalog_json.encode("utf-8")
        mock_resp.__enter__ = lambda s: s
        mock_resp.__exit__ = MagicMock(return_value=False)
        return mock_resp

    @patch("aicatalog.cli.main.parse")
    def test_explore_plain_output(self, mock_parse: MagicMock) -> None:
        """Test explore with plain text output (no rich)."""
        from aicatalog import parse as real_parse

        catalog = real_parse(_SAMPLE_CATALOG_JSON)
        mock_parse.return_value = catalog

        runner = CliRunner()
        with patch("aicatalog.cli.main._fetch_catalog", return_value=catalog):
            result = runner.invoke(cli, ["explore", "https://example.com/catalog.json"])

        assert result.exit_code == 0, f"CLI error: {result.output}"
        assert "Alpha Tool" in result.output
        assert "Beta Skill" in result.output

    @patch("aicatalog.cli.main._fetch_catalog")
    def test_explore_filter_tag(self, mock_fetch: MagicMock) -> None:
        """Test explore with tag filter."""
        from aicatalog import parse as real_parse

        catalog = real_parse(_SAMPLE_CATALOG_JSON)
        mock_fetch.return_value = catalog

        runner = CliRunner()
        result = runner.invoke(
            cli, ["explore", "https://example.com/catalog.json", "--filter-tag", "search"]
        )

        assert result.exit_code == 0
        assert "Alpha Tool" in result.output
        # Beta does not have 'search' tag
        assert "Beta Skill" not in result.output

    @patch("aicatalog.cli.main._fetch_catalog")
    def test_explore_filter_media_type(self, mock_fetch: MagicMock) -> None:
        """Test explore with media type filter."""
        from aicatalog import parse as real_parse

        catalog = real_parse(_SAMPLE_CATALOG_JSON)
        mock_fetch.return_value = catalog

        runner = CliRunner()
        result = runner.invoke(
            cli,
            [
                "explore",
                "https://example.com/catalog.json",
                "--filter-media-type",
                "application/vnd.skill+json",
            ],
        )

        assert result.exit_code == 0
        assert "Beta Skill" in result.output
        assert "Alpha Tool" not in result.output

    @patch("aicatalog.cli.main._fetch_catalog")
    def test_explore_show_detail(self, mock_fetch: MagicMock) -> None:
        """Test explore --show for entry details."""
        from aicatalog import parse as real_parse

        catalog = real_parse(_SAMPLE_CATALOG_JSON)
        mock_fetch.return_value = catalog

        runner = CliRunner()
        result = runner.invoke(
            cli,
            ["explore", "https://example.com/catalog.json", "--show", "urn:test:alpha"],
        )

        assert result.exit_code == 0
        assert "urn:test:alpha" in result.output
        assert "Alpha Tool" in result.output
        assert "Alpha description" in result.output

    @patch("aicatalog.cli.main._fetch_catalog")
    def test_explore_show_not_found(self, mock_fetch: MagicMock) -> None:
        """Test explore --show with non-existent entry."""
        from aicatalog import parse as real_parse

        catalog = real_parse(_SAMPLE_CATALOG_JSON)
        mock_fetch.return_value = catalog

        runner = CliRunner()
        result = runner.invoke(
            cli,
            ["explore", "https://example.com/catalog.json", "--show", "urn:test:missing"],
        )

        assert result.exit_code != 0
        assert "No entry with identifier" in result.output

    @patch("aicatalog.cli.main._fetch_catalog")
    def test_explore_json_output(self, mock_fetch: MagicMock) -> None:
        """Test explore --json-output."""
        from aicatalog import parse as real_parse

        catalog = real_parse(_SAMPLE_CATALOG_JSON)
        mock_fetch.return_value = catalog

        runner = CliRunner()
        result = runner.invoke(
            cli, ["explore", "https://example.com/catalog.json", "--json-output"]
        )

        assert result.exit_code == 0
        data = json.loads(result.output)
        assert data["specVersion"] == "1.0"
        assert len(data["entries"]) == 2


# ---------------------------------------------------------------------------
# install CLI command tests
# ---------------------------------------------------------------------------


class TestInstallCLI:
    """CLI tests for 'ai-catalog install'."""

    @patch("aicatalog.cli.main._fetch_catalog")
    def test_install_mcp(self, mock_fetch: MagicMock, tmp_path: Path) -> None:
        """Install an MCP entry adds to config file."""
        from aicatalog import parse as real_parse

        catalog = real_parse(_SAMPLE_CATALOG_JSON)
        mock_fetch.return_value = catalog

        config_file = tmp_path / "mcp-config.json"
        runner = CliRunner()
        result = runner.invoke(
            cli,
            [
                "install",
                "https://example.com/catalog.json",
                "urn:test:alpha",
                "--type",
                "mcp",
                "--config",
                str(config_file),
            ],
        )

        assert result.exit_code == 0, f"CLI error: {result.output}"
        assert config_file.exists()
        config = json.loads(config_file.read_text(encoding="utf-8"))
        assert "mcpServers" in config
        assert "alpha" in config["mcpServers"]
        assert config["mcpServers"]["alpha"]["url"] == "https://example.com/alpha"

    @patch("aicatalog.cli.main._fetch_catalog")
    def test_install_mcp_merges_existing(self, mock_fetch: MagicMock, tmp_path: Path) -> None:
        """Install MCP entry merges into existing config."""
        from aicatalog import parse as real_parse

        catalog = real_parse(_SAMPLE_CATALOG_JSON)
        mock_fetch.return_value = catalog

        config_file = tmp_path / "mcp-config.json"
        existing = {"mcpServers": {"existing": {"url": "https://old.com"}}}
        config_file.write_text(json.dumps(existing), encoding="utf-8")

        runner = CliRunner()
        result = runner.invoke(
            cli,
            [
                "install",
                "https://example.com/catalog.json",
                "urn:test:alpha",
                "--type",
                "mcp",
                "--config",
                str(config_file),
            ],
        )

        assert result.exit_code == 0
        config = json.loads(config_file.read_text(encoding="utf-8"))
        assert "existing" in config["mcpServers"]
        assert "alpha" in config["mcpServers"]

    @patch("aicatalog.cli.main._fetch_catalog")
    def test_install_skill_inline(self, mock_fetch: MagicMock, tmp_path: Path) -> None:
        """Install a skill entry with inline content."""
        from aicatalog.models import AiCatalog, CatalogEntry

        catalog = AiCatalog(
            spec_version="1.0",
            entries=[
                CatalogEntry(
                    identifier="urn:test:inline-skill",
                    display_name="Inline Skill",
                    media_type="application/vnd.skill+json",
                    inline={"tool": "something", "config": {"key": "value"}},
                )
            ],
        )
        mock_fetch.return_value = catalog

        skills_dir = tmp_path / "skills"
        runner = CliRunner()
        result = runner.invoke(
            cli,
            [
                "install",
                "https://example.com/catalog.json",
                "urn:test:inline-skill",
                "--type",
                "skill",
                "--skills-dir",
                str(skills_dir),
            ],
        )

        assert result.exit_code == 0, f"CLI error: {result.output}"
        dest = skills_dir / "inline-skill.json"
        assert dest.exists()
        data = json.loads(dest.read_text(encoding="utf-8"))
        assert data["tool"] == "something"

    @patch("aicatalog.cli.main._fetch_catalog")
    def test_install_entry_not_found(self, mock_fetch: MagicMock) -> None:
        """Install with non-existent entry ID."""
        from aicatalog import parse as real_parse

        catalog = real_parse(_SAMPLE_CATALOG_JSON)
        mock_fetch.return_value = catalog

        runner = CliRunner()
        result = runner.invoke(
            cli,
            ["install", "https://example.com/catalog.json", "urn:test:missing"],
        )

        assert result.exit_code != 0
        assert "not found" in result.output

    @patch("aicatalog.cli.main._fetch_catalog")
    def test_install_auto_detect_mcp(self, mock_fetch: MagicMock, tmp_path: Path) -> None:
        """Auto-detect MCP type from media type."""
        from aicatalog import parse as real_parse

        catalog = real_parse(_SAMPLE_CATALOG_JSON)
        mock_fetch.return_value = catalog

        config_file = tmp_path / "mcp-config.json"
        runner = CliRunner()
        # urn:test:alpha has mediaType "application/vnd.mcp+json" → auto-detect MCP
        result = runner.invoke(
            cli,
            [
                "install",
                "https://example.com/catalog.json",
                "urn:test:alpha",
                "--config",
                str(config_file),
            ],
        )

        assert result.exit_code == 0
        config = json.loads(config_file.read_text(encoding="utf-8"))
        assert "alpha" in config["mcpServers"]


# ---------------------------------------------------------------------------
# CLI help/version tests
# ---------------------------------------------------------------------------


class TestCLIGeneral:
    """General CLI tests."""

    def test_help(self) -> None:
        runner = CliRunner()
        result = runner.invoke(cli, ["--help"])
        assert result.exit_code == 0
        assert "AI Catalog CLI" in result.output

    def test_convert_help(self) -> None:
        runner = CliRunner()
        result = runner.invoke(cli, ["convert", "--help"])
        assert result.exit_code == 0
        assert "marketplace" in result.output

    def test_explore_help(self) -> None:
        runner = CliRunner()
        result = runner.invoke(cli, ["explore", "--help"])
        assert result.exit_code == 0
        assert "filter-tag" in result.output

    def test_install_help(self) -> None:
        runner = CliRunner()
        result = runner.invoke(cli, ["install", "--help"])
        assert result.exit_code == 0
        assert "CATALOG_URL" in result.output
