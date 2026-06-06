# AI Catalog

A .NET and Python toolkit for working with [AI Catalog](https://github.com/Agent-Card/ai-catalog) documents (`application/ai-catalog+json`). Parse, validate, serialize, explore, and install AI artifacts from catalog registries.

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

# Migrate a marketplace.json to ai-catalog format
ai-catalog migrate marketplace.json -o ai-catalog.json

# Export an AI Catalog to platform-specific outputs
ai-catalog export ai-catalog.json -o out

# Publish a catalog package (stub)
ai-catalog publish ai-catalog.json -o dist

# Install an artifact from a catalog
ai-catalog install https://spec-works.github.io/.well-known/ai-catalog.json "urn:specworks:a2a-ask"
```

## CLI Commands

| Command | Description |
|---------|-------------|
| `ai-catalog migrate <file>` | Migrate a marketplace.json to AI Catalog format |
| `ai-catalog export <file> -o <dir>` | Export an AI Catalog to platform-specific plugin formats |
| `ai-catalog publish <file> -o <dir>` | Package a catalog for remote distribution (currently a stub) |
| `ai-catalog explore <url>` | Fetch and browse an AI Catalog from a URL |
| `ai-catalog install <catalog-url> <entry-id>` | Install an artifact (skill or MCP server) from a catalog |

### `migrate`

Migrates a Claude-format `marketplace.json` into a standard `ai-catalog.json` document.

| Option | Description |
|--------|-------------|
| `-o, --output <file>` | Output file path (defaults to stdout) |

### `export`

Exports an `ai-catalog.json` file into platform-specific plugin marketplace formats.

| Option | Description |
|--------|-------------|
| `-o, --output <dir>` | Output directory for generated plugin marketplace files |
| `--source-dir <dir>` | Base directory for resolving relative plugin source paths |
| `--github` | Generate GitHub Copilot CLI marketplace output |
| `--codex` | Generate OpenAI Codex CLI marketplace output |
| `--claude` | Generate Claude Code plugin marketplace output |

### `publish`

Packages a catalog for remote distribution (`zip`, signing, hosting). This command currently prints `Not yet implemented`.

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

### A2A Discovery Helpers

Use the helper surface in `SpecWorks.AiCatalog` when you need to identify A2A agent cards inside a catalog.

- `KnownMediaTypes.AiCatalog` — `application/ai-catalog+json`
- `KnownMediaTypes.A2AAgentCard` — `application/a2a-agent-card+json`
- `KnownMediaTypes.A2AAgentCardVendor` — `application/vnd.a2a.agent-card+json`
- `CatalogEntryExtensions.IsA2AAgentCard()` — returns `true` when a `CatalogEntry` media type matches either A2A agent card media type, using case-insensitive comparison.

```csharp
using SpecWorks.AiCatalog;
using SpecWorks.AiCatalog.Models;

CatalogEntry entry = GetEntry();

if (entry.IsA2AAgentCard())
{
    Console.WriteLine($"Resolved A2A entry: {entry.Identifier}");
}
```

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
