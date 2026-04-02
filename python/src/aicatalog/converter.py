"""Marketplace-to-AI-Catalog conversion.

Converts Claude Code Plugins marketplace format to AI Catalog format
per § Mapping to Claude Code Plugins Marketplace and TFD-005.
"""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from .models import AiCatalog, CatalogEntry, Publisher
from .serializer import serialize

# Media type for Claude Code plugins (per TFD-005)
CLAUDE_PLUGIN_MEDIA_TYPE = "application/vnd.claude.code-plugin+json"

# Identifier pattern (per TFD-005)
CLAUDE_PLUGIN_URN_PREFIX = "urn:claude:plugins:"


def convert_marketplace_plugin(plugin: dict[str, Any]) -> CatalogEntry:
    """Convert a single marketplace plugin dict to a CatalogEntry."""
    name = plugin["name"]
    publisher = None
    if "publisher" in plugin and plugin["publisher"] is not None:
        pub_data = plugin["publisher"]
        publisher = Publisher(
            identifier=pub_data.get("url", ""),
            display_name=pub_data.get("name", ""),
        )

    return CatalogEntry(
        identifier=f"{CLAUDE_PLUGIN_URN_PREFIX}{name}",
        display_name=plugin.get("display_name", name),
        description=plugin.get("description"),
        media_type=CLAUDE_PLUGIN_MEDIA_TYPE,
        url=plugin.get("manifest_url"),
        version=plugin.get("version"),
        tags=plugin.get("categories", []),
        updated_at=plugin.get("updated_at"),
        publisher=publisher,
    )


def convert_marketplace(marketplace_data: dict[str, Any]) -> AiCatalog:
    """Convert a Claude marketplace.json structure to an AiCatalog.

    Args:
        marketplace_data: The raw marketplace data (the 'plugins' array container).

    Returns:
        An AiCatalog with converted entries.
    """
    plugins = marketplace_data.get("plugins", [])
    entries = [convert_marketplace_plugin(p) for p in plugins]
    return AiCatalog(spec_version="1.0", entries=entries)


def convert_marketplace_file(input_path: str | Path) -> AiCatalog:
    """Read a marketplace JSON file and convert to AiCatalog.

    The file may be either:
    - A raw marketplace document (has a top-level "plugins" key)
    - A test fixture wrapper (has "input" containing the marketplace data)
    """
    text = Path(input_path).read_text(encoding="utf-8")
    data = json.loads(text)

    # Support test fixture wrapper format
    if "input" in data and "plugins" in data["input"]:
        marketplace_data = data["input"]
    elif "plugins" in data:
        marketplace_data = data
    else:
        raise ValueError(
            "Input file must contain a 'plugins' array "
            "(either at root or inside a test fixture 'input' wrapper)"
        )

    return convert_marketplace(marketplace_data)


def convert_marketplace_to_json(input_path: str | Path, indent: int = 2) -> str:
    """Convert a marketplace file and return the AI Catalog as a JSON string."""
    catalog = convert_marketplace_file(input_path)
    return serialize(catalog)
