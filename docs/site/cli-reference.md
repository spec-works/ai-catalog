# CLI Reference

Complete command reference for the `ai-catalog` CLI tool.

## Installation

```bash
dotnet tool install --global SpecWorks.AiCatalog.Cli
```

## Commands

### `ai-catalog explore <url>`

Fetch and browse an AI Catalog from a URL. Displays entries in a table format with optional filtering and detail views.

```bash
ai-catalog explore <url> [options]
```

**Arguments:**

| Argument | Description |
|----------|-------------|
| `url` | URL of an AI Catalog document |

**Options:**

| Option | Description |
|--------|-------------|
| `--filter-tag <tag>` | Filter entries by tag |
| `--filter-media-type <type>` | Filter entries by media type |
| `--show <identifier>` | Show detailed info for a specific entry |

**Examples:**

```bash
# List all entries in a catalog
ai-catalog explore https://spec-works.github.io/.well-known/ai-catalog.json

# Filter by tag
ai-catalog explore https://spec-works.github.io/.well-known/ai-catalog.json --filter-tag agent

# Filter by media type
ai-catalog explore https://spec-works.github.io/.well-known/ai-catalog.json \
  --filter-media-type "application/vnd.a2a.agent-card+json"

# Show details for a specific entry
ai-catalog explore https://spec-works.github.io/.well-known/ai-catalog.json \
  --show "urn:specworks:a2a-ask"
```

**Output (list mode):**

```
AI Catalog (specVersion: 1.0, conformance: Discoverable)
Host: SpecWorks

Identifier                               Display Name              Media Type                          Version
------------------------------------------------------------------------------------------------------------------
urn:specworks:a2a-ask                    A2A-Ask                   application/vnd.a2a.agent-card+json 1.1.0
urn:specworks:markmyword                 MarkMyWord                text/markdown                       1.0.0
...

5 entries shown (of 5 total)
```

**Output (detail mode with `--show`):**

```
Identifier:  urn:specworks:a2a-ask
Display Name: A2A-Ask
Media Type:  application/vnd.a2a.agent-card+json
Description: CLI tool for interacting with A2A protocol agents
Version:     1.1.0
URL:         https://github.com/spec-works/A2A-Ask
Tags:        agent, a2a, cli

Publisher:
  Identifier:  spec-works
  Display Name: SpecWorks
```

---

### `ai-catalog convert marketplace <file>`

Convert a Claude-format `marketplace.json` to a standard `ai-catalog.json` document.

```bash
ai-catalog convert marketplace <input-file> [options]
```

**Arguments:**

| Argument | Description |
|----------|-------------|
| `input-file` | Path to the marketplace.json file |

**Options:**

| Option | Alias | Description |
|--------|-------|-------------|
| `--output` | `-o` | Output file path (defaults to stdout) |

**Examples:**

```bash
# Convert and print to stdout
ai-catalog convert marketplace marketplace.json

# Convert and save to file
ai-catalog convert marketplace marketplace.json -o ai-catalog.json
```

---

### `ai-catalog install <catalog-url> <entry-id>`

Download and install an artifact from a catalog. Automatically detects whether the entry is an MCP server card or a skill based on its media type.

```bash
ai-catalog install <catalog-url> <entry-id> [options]
```

**Arguments:**

| Argument | Description |
|----------|-------------|
| `catalog-url` | URL of the AI Catalog |
| `entry-id` | Identifier of the entry to install |

**Options:**

| Option | Description |
|--------|-------------|
| `--type <mcp\|skill>` | Override auto-detection of artifact type |
| `--output-dir <dir>` | Base directory for installation (default: `.`) |

**Examples:**

```bash
# Install an MCP server (auto-detected)
ai-catalog install https://spec-works.github.io/.well-known/ai-catalog.json \
  "urn:specworks:xregistry-mcp"

# Install a skill with explicit type
ai-catalog install https://spec-works.github.io/.well-known/ai-catalog.json \
  "urn:specworks:a2a-ask" --type skill

# Install to a specific directory
ai-catalog install https://spec-works.github.io/.well-known/ai-catalog.json \
  "urn:specworks:markmyword" --output-dir ~/my-project
```

**MCP server installation** writes to `.ai-catalog/mcp-config.json`:

```json
{
  "mcpServers": {
    "xregistry-mcp": {
      "url": "https://example.com/mcp",
      "name": "xRegistry MCP Server",
      "mediaType": "application/vnd.mcp.server-card+json"
    }
  }
}
```

**Skill installation** downloads the artifact to `.ai-catalog/skills/<name>.<ext>`.

## Global Options

| Option | Description |
|--------|-------------|
| `--version` | Show version information |
| `-?, -h, --help` | Show help and usage |
