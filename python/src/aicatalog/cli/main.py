"""ai-catalog CLI — convert, explore, and install AI Catalog artifacts.

Commands:
    ai-catalog convert marketplace <input-file> [--output <file>]
    ai-catalog explore <url> [--filter-tag TAG] [--filter-media-type TYPE]
    ai-catalog install <catalog-url> <entry-id> [--type mcp|skill] [--config PATH]
"""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

import click

from aicatalog import parse, serialize, serialize_to_dict, validate
from aicatalog.converter import convert_marketplace_file
from aicatalog.models import AiCatalog, CatalogEntry


@click.group()
@click.version_option(package_name="specworks-aicatalog")
def cli() -> None:
    """AI Catalog CLI — work with AI Card specification documents."""


# ---------------------------------------------------------------------------
# convert command group
# ---------------------------------------------------------------------------


@cli.group()
def convert() -> None:
    """Convert external formats to AI Catalog."""


@convert.command("marketplace")
@click.argument("input_file", type=click.Path(exists=True, dir_okay=False))
@click.option(
    "--output", "-o",
    type=click.Path(dir_okay=False),
    default=None,
    help="Output file path (default: stdout).",
)
def convert_marketplace(input_file: str, output: str | None) -> None:
    """Convert a Claude marketplace.json to ai-catalog.json."""
    try:
        catalog = convert_marketplace_file(input_file)
    except (json.JSONDecodeError, ValueError, OSError) as exc:
        raise click.ClickException(str(exc)) from exc

    result_json = serialize(catalog)

    if output:
        Path(output).write_text(result_json + "\n", encoding="utf-8")
        click.echo(f"Wrote {output}")
    else:
        click.echo(result_json)


# ---------------------------------------------------------------------------
# explore command
# ---------------------------------------------------------------------------


def _fetch_catalog(url: str) -> AiCatalog:
    """Fetch and parse a remote AI Catalog from a URL."""
    try:
        import httpx

        with httpx.Client(follow_redirects=True) as client:
            resp = client.get(url, headers={"Accept": "application/ai-catalog+json"})
            resp.raise_for_status()
            return parse(resp.text)
    except ImportError:
        import urllib.request

        req = urllib.request.Request(url, headers={"Accept": "application/ai-catalog+json"})
        with urllib.request.urlopen(req) as resp:  # noqa: S310
            text = resp.read().decode("utf-8")
        return parse(text)


def _format_entry_row(i: int, entry: CatalogEntry) -> list[str]:
    """Format a catalog entry as a display row."""
    tags = ", ".join(entry.tags) if entry.tags else ""
    trust = "yes" if entry.trust_manifest else ""
    version = entry.version or ""
    return [
        str(i + 1), entry.identifier, entry.display_name,
        entry.media_type, version, tags, trust,
    ]


def _print_entries_rich(entries: list[CatalogEntry], catalog: AiCatalog) -> None:
    """Display entries using rich tables."""
    from rich.console import Console
    from rich.table import Table

    console = Console()
    vr = validate(catalog)

    table = Table(title="AI Catalog Entries", show_lines=True)
    table.add_column("#", style="dim", width=4)
    table.add_column("Identifier", style="cyan")
    table.add_column("Name", style="bold")
    table.add_column("Media Type", style="green")
    table.add_column("Version")
    table.add_column("Tags")
    table.add_column("Trust", style="yellow")

    for i, entry in enumerate(entries):
        row = _format_entry_row(i, entry)
        table.add_row(*row)

    console.print(f"\n[bold]Spec Version:[/bold] {catalog.spec_version}")
    console.print(f"[bold]Conformance:[/bold] {vr.conformance_level.value}")
    if catalog.host:
        console.print(f"[bold]Host:[/bold] {catalog.host.display_name}")
    console.print(f"[bold]Entries:[/bold] {len(entries)} shown / {len(catalog.entries)} total\n")
    console.print(table)


def _print_entries_plain(entries: list[CatalogEntry], catalog: AiCatalog) -> None:
    """Display entries as plain text."""
    vr = validate(catalog)
    click.echo(f"\nSpec Version: {catalog.spec_version}")
    click.echo(f"Conformance:  {vr.conformance_level.value}")
    if catalog.host:
        click.echo(f"Host:         {catalog.host.display_name}")
    click.echo(f"Entries:      {len(entries)} shown / {len(catalog.entries)} total\n")

    header = f"{'#':<4} {'Identifier':<40} {'Name':<25} {'Media Type':<35} {'Ver':<8} {'Tags'}"
    click.echo(header)
    click.echo("-" * len(header))
    for i, entry in enumerate(entries):
        tags = ", ".join(entry.tags) if entry.tags else ""
        version = entry.version or ""
        click.echo(
            f"{i+1:<4} {entry.identifier:<40} {entry.display_name:<25} "
            f"{entry.media_type:<35} {version:<8} {tags}"
        )


def _print_entry_detail(entry: CatalogEntry) -> None:
    """Print detailed info about a single entry."""
    click.echo(f"\n  Identifier:  {entry.identifier}")
    click.echo(f"  Name:        {entry.display_name}")
    click.echo(f"  Media Type:  {entry.media_type}")
    if entry.description:
        click.echo(f"  Description: {entry.description}")
    if entry.version:
        click.echo(f"  Version:     {entry.version}")
    if entry.url:
        click.echo(f"  URL:         {entry.url}")
    if entry.tags:
        click.echo(f"  Tags:        {', '.join(entry.tags)}")
    if entry.updated_at:
        click.echo(f"  Updated:     {entry.updated_at}")
    if entry.publisher:
        click.echo(f"  Publisher:   {entry.publisher.display_name} ({entry.publisher.identifier})")
    if entry.trust_manifest:
        tm = entry.trust_manifest
        click.echo(f"  Trust:       identity={tm.identity}")
        if tm.attestations:
            click.echo(f"               attestations: {len(tm.attestations)}")
        if tm.provenance:
            click.echo(f"               provenance links: {len(tm.provenance)}")
    click.echo()


@cli.command()
@click.argument("url")
@click.option("--filter-tag", "-t", multiple=True, help="Filter entries by tag.")
@click.option("--filter-media-type", "-m", default=None, help="Filter entries by media type.")
@click.option("--show", "-s", default=None, help="Show details for entry by identifier.")
@click.option("--json-output", "-j", is_flag=True, help="Output as JSON instead of table.")
def explore(
    url: str,
    filter_tag: tuple[str, ...],
    filter_media_type: str | None,
    show: str | None,
    json_output: bool,
) -> None:
    """Fetch and browse a remote AI Catalog.

    URL should point to an AI Catalog JSON document.
    """
    try:
        catalog = _fetch_catalog(url)
    except Exception as exc:
        raise click.ClickException(f"Failed to fetch catalog: {exc}") from exc

    # Filter entries
    entries = list(catalog.entries)
    if filter_tag:
        tag_set = set(filter_tag)
        entries = [e for e in entries if tag_set.intersection(e.tags)]
    if filter_media_type:
        entries = [e for e in entries if e.media_type == filter_media_type]

    # Show detail for a specific entry
    if show:
        match = [e for e in catalog.entries if e.identifier == show]
        if not match:
            raise click.ClickException(f"No entry with identifier '{show}'")
        for entry in match:
            _print_entry_detail(entry)
        return

    # JSON output mode
    if json_output:
        d = serialize_to_dict(catalog)
        click.echo(json.dumps(d, indent=2, ensure_ascii=False))
        return

    # Table output
    try:
        _print_entries_rich(entries, catalog)
    except ImportError:
        _print_entries_plain(entries, catalog)


# ---------------------------------------------------------------------------
# install command
# ---------------------------------------------------------------------------


def _build_mcp_config_snippet(entry: CatalogEntry) -> dict[str, Any]:
    """Build an MCP server config snippet from a catalog entry."""
    server_config: dict[str, Any] = {}
    if entry.url:
        server_config["url"] = entry.url
    if entry.description:
        server_config["description"] = entry.description
    # Derive a server name from identifier
    name = entry.identifier.rsplit(":", 1)[-1] if ":" in entry.identifier else entry.identifier
    return {"mcpServers": {name: server_config}}


def _install_mcp(entry: CatalogEntry, config_path: Path) -> None:
    """Add MCP server entry to an mcp config file."""
    snippet = _build_mcp_config_snippet(entry)

    existing = json.loads(config_path.read_text(encoding="utf-8")) if config_path.exists() else {}

    servers = existing.setdefault("mcpServers", {})
    servers.update(snippet["mcpServers"])

    config_path.parent.mkdir(parents=True, exist_ok=True)
    config_path.write_text(
        json.dumps(existing, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )

    name = list(snippet["mcpServers"].keys())[0]
    click.echo(f"Added MCP server '{name}' to {config_path}")


def _install_skill(entry: CatalogEntry, skills_dir: Path) -> None:
    """Download/copy a skill artifact to the local skills directory."""
    skills_dir.mkdir(parents=True, exist_ok=True)

    if entry.url:
        try:
            import httpx

            with httpx.Client(follow_redirects=True) as client:
                resp = client.get(entry.url)
                resp.raise_for_status()
                content = resp.content
        except ImportError:
            import urllib.request

            with urllib.request.urlopen(entry.url) as resp:  # noqa: S310
                content = resp.read()

        name = entry.identifier.rsplit(":", 1)[-1] if ":" in entry.identifier else entry.identifier
        dest = skills_dir / f"{name}.json"
        dest.write_bytes(content)
        click.echo(f"Downloaded skill '{entry.display_name}' to {dest}")

    elif entry.inline is not None:
        name = entry.identifier.rsplit(":", 1)[-1] if ":" in entry.identifier else entry.identifier
        dest = skills_dir / f"{name}.json"
        if isinstance(entry.inline, (dict, list)):
            dest.write_text(
                json.dumps(entry.inline, indent=2, ensure_ascii=False) + "\n",
                encoding="utf-8",
            )
        else:
            dest.write_text(str(entry.inline), encoding="utf-8")
        click.echo(f"Installed skill '{entry.display_name}' to {dest}")
    else:
        raise click.ClickException(f"Entry '{entry.identifier}' has no url or inline content")


@cli.command()
@click.argument("catalog_url")
@click.argument("entry_id")
@click.option(
    "--type", "install_type",
    type=click.Choice(["mcp", "skill"], case_sensitive=False),
    default=None,
    help="Installation type (auto-detected from media type if not specified).",
)
@click.option(
    "--config",
    type=click.Path(dir_okay=False),
    default=None,
    help="MCP config file path (default: ./mcp-config.json).",
)
@click.option(
    "--skills-dir",
    type=click.Path(file_okay=False),
    default=None,
    help="Skills directory (default: ./skills/).",
)
def install(
    catalog_url: str,
    entry_id: str,
    install_type: str | None,
    config: str | None,
    skills_dir: str | None,
) -> None:
    """Install an artifact from an AI Catalog.

    Fetches CATALOG_URL, finds the entry with ENTRY_ID, and installs it.
    For MCP servers: adds to mcp-config.json.
    For other types: downloads to a local skills directory.
    """
    try:
        catalog = _fetch_catalog(catalog_url)
    except Exception as exc:
        raise click.ClickException(f"Failed to fetch catalog: {exc}") from exc

    # Find entry
    matches = [e for e in catalog.entries if e.identifier == entry_id]
    if not matches:
        available = [e.identifier for e in catalog.entries]
        raise click.ClickException(
            f"Entry '{entry_id}' not found. Available: {', '.join(available)}"
        )
    entry = matches[0]

    # Auto-detect install type from media type
    if install_type is None:
        mcp_indicators = {"mcp", "model-context-protocol"}
        if any(ind in entry.media_type.lower() for ind in mcp_indicators):
            install_type = "mcp"
        else:
            install_type = "skill"

    if install_type == "mcp":
        config_path = Path(config) if config else Path("mcp-config.json")
        _install_mcp(entry, config_path)
    else:
        sd = Path(skills_dir) if skills_dir else Path("skills")
        _install_skill(entry, sd)


def main() -> None:
    """Entry point for the CLI."""
    cli()


if __name__ == "__main__":
    main()
