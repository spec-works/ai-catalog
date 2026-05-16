# AI Catalog

A .NET and Python toolkit for working with [AI Catalog](https://agent-card.github.io/ai-card/) documents (`application/ai-catalog+json`). Parse, validate, serialize, explore, and install AI artifacts from catalog registries.

## What is AI Catalog?

AI Catalog is an open specification for describing collections of AI artifacts — agent cards, MCP server cards, skills, and other machine-readable resources. It provides a standardized format for discovery, trust, and installation of AI components.

Key concepts:
- **Catalog** — A JSON document listing AI artifacts with metadata
- **Entry** — A single artifact with identifier, media type, URL or embedded data, and optional trust manifest
- **Conformance Levels** — Minimal (Level 1), Discoverable (Level 2), Trusted (Level 3)

## Packages

| Package | Language | Install |
|---------|----------|---------|
| [SpecWorks.AiCatalog](https://www.nuget.org/packages/SpecWorks.AiCatalog) | .NET 8/10 | `dotnet add package SpecWorks.AiCatalog` |
| [SpecWorks.AiCatalog.Cli](https://www.nuget.org/packages/SpecWorks.AiCatalog.Cli) | .NET 9 CLI | `dotnet tool install --global SpecWorks.AiCatalog.Cli` |
| [specworks-aicatalog](https://pypi.org/project/specworks-aicatalog/) | Python | `pip install specworks-aicatalog` |

## Quick Start — Library (.NET)

```csharp
using SpecWorks.AiCatalog.Parsing;
using SpecWorks.AiCatalog.Validation;
using SpecWorks.AiCatalog.Serialization;

// Parse a catalog document
var json = File.ReadAllText("ai-catalog.json");
var catalog = AiCatalogParser.Parse(json);

// Validate conformance level
var result = AiCatalogValidator.Validate(catalog);
Console.WriteLine($"Conformance: {result.ConformanceLevel}"); // Minimal, Discoverable, or Trusted

// Iterate entries
foreach (var entry in catalog.Entries)
{
    Console.WriteLine($"{entry.DisplayName} ({entry.MediaType})");
}

// Serialize back to JSON
var output = AiCatalogSerializer.Serialize(catalog);
```

## Quick Start — Library (Python)

```python
from aicatalog import parse, serialize, validate

catalog = parse('{"specVersion": "1.0", "entries": []}')
result = validate(catalog)
print(result.conformance_level)  # ConformanceLevel.MINIMAL
json_str = serialize(catalog)
```

## Quick Start — CLI

```bash
# Install the CLI tool
dotnet tool install --global SpecWorks.AiCatalog.Cli

# Explore a remote catalog
ai-catalog explore https://spec-works.github.io/.well-known/ai-catalog.json

# Show details for a specific entry
ai-catalog explore https://spec-works.github.io/.well-known/ai-catalog.json --show "urn:specworks:a2a-ask"

# Filter by tag
ai-catalog explore https://spec-works.github.io/.well-known/ai-catalog.json --filter-tag agent

# Convert a marketplace.json to ai-catalog format
ai-catalog convert marketplace marketplace.json -o ai-catalog.json

# Install an artifact from a catalog
ai-catalog install https://spec-works.github.io/.well-known/ai-catalog.json "urn:specworks:a2a-ask"
```

## CLI Commands

| Command | Description |
|---------|-------------|
| `ai-catalog explore <url>` | Fetch and browse an AI Catalog from a URL |
| `ai-catalog convert marketplace <file>` | Convert a Claude marketplace.json to ai-catalog.json |
| `ai-catalog install <catalog-url> <entry-id>` | Install an artifact (skill or MCP server) from a catalog |

### `explore`

Fetches a catalog from a URL and displays entries in a table. Supports filtering and detail views.

| Option | Description |
|--------|-------------|
| `--filter-tag <tag>` | Filter entries by tag |
| `--filter-media-type <type>` | Filter entries by media type |
| `--show <identifier>` | Show detailed info for one entry |

### `convert marketplace`

Converts a Claude-format `marketplace.json` into a standard `ai-catalog.json` document.

| Option | Description |
|--------|-------------|
| `-o, --output <file>` | Output file path (defaults to stdout) |

### `install`

Downloads and installs an artifact from a catalog. Auto-detects MCP server cards vs skills.

| Option | Description |
|--------|-------------|
| `--type <mcp\|skill>` | Override auto-detection |
| `--output-dir <dir>` | Base directory for installation (default: `.`) |

## .NET Library API

### Parsing

```csharp
// From string
var catalog = AiCatalogParser.Parse(jsonString);

// From stream
using var stream = File.OpenRead("catalog.json");
var catalog = AiCatalogParser.Parse(stream);
```

### Validation

```csharp
var result = AiCatalogValidator.Validate(catalog);
// result.ConformanceLevel: Minimal (1), Discoverable (2), or Trusted (3)
// result.Errors: list of validation issues
// result.IsValid: true if no errors
```

### Serialization

```csharp
// To string
var json = AiCatalogSerializer.Serialize(catalog);

// To stream
AiCatalogSerializer.Serialize(catalog, outputStream);
```

### Models

- `AiCatalog` — Top-level document with `SpecVersion`, `Entries`, optional `Host` and `Metadata`
- `CatalogEntry` — An artifact entry with `Identifier`, `DisplayName`, `MediaType`, `Url`/`Data`, `Publisher`, `TrustManifest`, `Tags`
- `HostInfo` — Catalog operator info
- `Publisher` — Entry publisher info
- `TrustManifest` — Trust and attestation data

## Specification

This library implements the [AI Card specification](https://agent-card.github.io/ai-card/) (`application/ai-catalog+json`), including:

- **VH-1**: `specVersion` validation (Major.Minor format)
- **VH-2**: Unknown properties preserved via `[JsonExtensionData]`
- **VH-4/5/6**: Major version compatibility checking
- **Conformance levels**: Minimal → Discoverable → Trusted

## Contributing

See the [SpecWorks Factory](https://spec-works.github.io/) for contributing guidelines.

## License

MIT
