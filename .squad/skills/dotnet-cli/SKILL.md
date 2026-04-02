---
name: "dotnet-cli"
description: "How to build, run, and use the AI Catalog .NET CLI tool from this repo"
domain: "cli-tooling"
confidence: "high"
source: "manual"
---

## Context

The AI Catalog CLI is a .NET console app that lives in this repo at `dotnet/src/AiCatalog.Cli/`. It is **not** installed globally — you must run it via `dotnet run` from the repo root. Use this skill whenever you need to convert marketplace files, explore AI catalogs, or install catalog artifacts.

**Prerequisites:** .NET 9 SDK must be available (`dotnet --version`).

## Building and Running

### The Invocation Pattern

```
dotnet run --project dotnet/src/AiCatalog.Cli -- <command> [arguments] [options]
```

> **Critical:** The `--` separator between `dotnet run --project ...` and the CLI arguments is **mandatory**. Everything before `--` is for `dotnet run`; everything after `--` is passed to the CLI tool.

### Quick Check

```powershell
dotnet run --project dotnet/src/AiCatalog.Cli -- --help
```

Expected output:
```
AI Catalog CLI — convert, explore, and install AI artifacts

Commands:
  convert                           Convert between AI artifact catalog formats
  explore <url>                     Fetch and browse an AI Catalog from a URL
  install <catalog-url> <entry-id>  Install an artifact from an AI Catalog
```

## Commands

### 1. `convert marketplace` — Convert a marketplace.json to AI Catalog format

Converts Claude-format or Copilot-format `marketplace.json` files into the `application/ai-catalog+json` format.

**Syntax:**
```
dotnet run --project dotnet/src/AiCatalog.Cli -- convert marketplace <input-file> [options]
```

**Arguments:**

| Argument | Required | Description |
|---|---|---|
| `<input-file>` | Yes | Path to marketplace.json file |

**Options:**

| Option | Alias | Description |
|---|---|---|
| `--output <file>` | `-o` | Output file path. If omitted, writes to stdout. |
| `--help` | `-h`, `-?` | Show help |

**Supported formats (auto-detected):**
- **Claude format:** Plugins with `display_name`, `manifest_url`, per-plugin `publisher`
- **Copilot format:** Plugins with `source`, `skills[]`, root-level `owner`
- **Test fixture wrapper:** Also accepts `{"input": {"plugins": [...]}}` wrapped fixtures

**Examples:**

```powershell
# Convert a shared test fixture to stdout
dotnet run --project dotnet/src/AiCatalog.Cli -- convert marketplace testcases/marketplace-input.json

# Convert and save to a file
dotnet run --project dotnet/src/AiCatalog.Cli -- convert marketplace testcases/marketplace-input.json -o output-catalog.json

# Convert a Copilot-format integration fixture
dotnet run --project dotnet/src/AiCatalog.Cli -- convert marketplace testcases/integration/spec-works-plugins-marketplace.json -o spec-works-catalog.json

# Convert the WorkIQ marketplace fixture
dotnet run --project dotnet/src/AiCatalog.Cli -- convert marketplace testcases/integration/work-iq-marketplace.json
```

**Output behavior:**
- With `--output`: Writes JSON to file, prints `Converted N entries to <path>` to stdout
- Without `--output`: Prints the full AI Catalog JSON to stdout
- On error: Prints `Error: <message>` to stderr, exits with code 1

---

### 2. `explore` — Fetch and browse a remote AI Catalog

Fetches an AI Catalog document from a URL and displays its entries in a table. Supports filtering and detail views.

**Syntax:**
```
dotnet run --project dotnet/src/AiCatalog.Cli -- explore <url> [options]
```

**Arguments:**

| Argument | Required | Description |
|---|---|---|
| `<url>` | Yes | URL of an AI Catalog document (must be absolute) |

**Options:**

| Option | Description |
|---|---|
| `--filter-tag <tag>` | Filter entries to only those containing this tag (case-insensitive) |
| `--filter-media-type <type>` | Filter entries by exact media type (case-insensitive) |
| `--show <identifier>` | Show full details for a specific entry by its identifier (exact match) |
| `--help` | Show help |

**Examples:**

```powershell
# List all entries in a remote catalog
dotnet run --project dotnet/src/AiCatalog.Cli -- explore https://example.com/ai-catalog.json

# Filter entries by tag
dotnet run --project dotnet/src/AiCatalog.Cli -- explore https://example.com/ai-catalog.json --filter-tag "mcp"

# Filter by media type
dotnet run --project dotnet/src/AiCatalog.Cli -- explore https://example.com/ai-catalog.json --filter-media-type "application/vnd.mcp.server+json"

# Show details for a specific entry
dotnet run --project dotnet/src/AiCatalog.Cli -- explore https://example.com/ai-catalog.json --show "urn:example:my-tool"
```

**Output behavior:**
- **List mode (default):** Shows conformance level, host info, then a table of entries with Identifier, Display Name, Media Type, and Version columns. Long values are truncated with `...`.
- **Detail mode (`--show`):** Shows all fields for the matching entry including publisher, trust manifest, attestations, and inline content.
- On error: Prints `Error: <message>` to stderr, exits with code 1

---

### 3. `install` — Install an artifact from a remote AI Catalog

Fetches a catalog from a URL, finds the specified entry, and installs it locally. Auto-detects whether the artifact is an MCP server config or a skill based on its `mediaType`.

**Syntax:**
```
dotnet run --project dotnet/src/AiCatalog.Cli -- install <catalog-url> <entry-id> [options]
```

**Arguments:**

| Argument | Required | Description |
|---|---|---|
| `<catalog-url>` | Yes | URL of the AI Catalog |
| `<entry-id>` | Yes | Identifier of the entry to install (exact string match) |

**Options:**

| Option | Description |
|---|---|
| `--type <type>` | Artifact type hint: `mcp` or `skill`. Auto-detected from mediaType if omitted. |
| `--output-dir <dir>` | Base directory for installation. Defaults to current directory (`.`). |
| `--help` | Show help |

**Type auto-detection:**
- If `mediaType` contains `"mcp"` (case-insensitive) → installs as MCP server config
- Otherwise → installs as a skill artifact

**Install locations (relative to `--output-dir`):**
- **MCP:** `.ai-catalog/mcp-config.json` — merges into existing config (won't overwrite)
- **Skill:** `.ai-catalog/skills/<sanitized-name>.<ext>` — downloads artifact file

**Examples:**

```powershell
# Install an MCP server from a remote catalog
dotnet run --project dotnet/src/AiCatalog.Cli -- install https://example.com/ai-catalog.json "urn:example:my-mcp-server"

# Install with explicit type and output directory
dotnet run --project dotnet/src/AiCatalog.Cli -- install https://example.com/ai-catalog.json "urn:example:my-skill" --type skill --output-dir ./my-project

# Let it auto-detect the type
dotnet run --project dotnet/src/AiCatalog.Cli -- install https://example.com/ai-catalog.json "urn:example:tool" --output-dir .
```

**Output behavior:**
- Prints `Installing: <display-name> (<identifier>)` and `Type: <mcp|skill>`
- MCP: Writes merged config, prints path and server name, shows copy-paste snippet
- Skill: Downloads artifact to `.ai-catalog/skills/`, prints file path
- On error: Prints `Error: <message>` to stderr, exits with code 1

## Common Workflows

### Workflow 1: Convert a marketplace file and inspect the result

```powershell
# Step 1: Convert marketplace to AI Catalog
dotnet run --project dotnet/src/AiCatalog.Cli -- convert marketplace testcases/integration/spec-works-plugins-marketplace.json -o my-catalog.json

# Step 2: (Optional) Verify the output is valid by serving it locally or inspecting with jq/cat
Get-Content my-catalog.json | ConvertFrom-Json | Select-Object specVersion
```

### Workflow 2: Explore and install from a live catalog

```powershell
# Step 1: Browse what's available
dotnet run --project dotnet/src/AiCatalog.Cli -- explore https://example.com/ai-catalog.json

# Step 2: Filter to find MCP servers
dotnet run --project dotnet/src/AiCatalog.Cli -- explore https://example.com/ai-catalog.json --filter-media-type "application/vnd.mcp.server+json"

# Step 3: Get details on a specific entry
dotnet run --project dotnet/src/AiCatalog.Cli -- explore https://example.com/ai-catalog.json --show "urn:example:interesting-tool"

# Step 4: Install it
dotnet run --project dotnet/src/AiCatalog.Cli -- install https://example.com/ai-catalog.json "urn:example:interesting-tool"
```

### Workflow 3: Convert and validate with the library tests

```powershell
# Convert then run the test suite to verify everything still passes
dotnet run --project dotnet/src/AiCatalog.Cli -- convert marketplace testcases/marketplace-input.json -o test-output.json
dotnet test dotnet/AiCatalog.sln --no-build --verbosity quiet
```

## Available Test Fixtures

These files in the repo can be used as inputs for testing and demos:

| Fixture | Path | Use with |
|---|---|---|
| Claude marketplace | `testcases/marketplace-input.json` | `convert marketplace` |
| Copilot marketplace (spec-works) | `testcases/integration/spec-works-plugins-marketplace.json` | `convert marketplace` |
| Copilot marketplace (work-iq) | `testcases/integration/work-iq-marketplace.json` | `convert marketplace` |
| Expected conversion output | `testcases/marketplace-expected.json` | Comparison reference |
| Minimal catalog | `testcases/minimal-catalog.json` | Reference |
| All properties catalog | `testcases/all-properties.json` | Reference |

## Anti-Patterns

### ❌ Forgetting the `--` separator

```powershell
# WRONG — dotnet run interprets "convert" as a dotnet argument
dotnet run --project dotnet/src/AiCatalog.Cli convert marketplace input.json

# CORRECT — double-dash separates dotnet args from CLI args
dotnet run --project dotnet/src/AiCatalog.Cli -- convert marketplace input.json
```

### ❌ Using a relative project path from the wrong directory

```powershell
# WRONG — path is relative to repo root; this fails from other directories
cd dotnet/src
dotnet run --project AiCatalog.Cli -- --help

# CORRECT — always run from repo root, or adjust the path
cd C:\src\github\spec-works\ai-catalog
dotnet run --project dotnet/src/AiCatalog.Cli -- --help
```

### ❌ Passing file paths where URLs are expected (and vice versa)

```powershell
# WRONG — explore expects a URL, not a file path
dotnet run --project dotnet/src/AiCatalog.Cli -- explore testcases/minimal-catalog.json

# WRONG — convert marketplace expects a file path, not a URL
dotnet run --project dotnet/src/AiCatalog.Cli -- convert marketplace https://example.com/marketplace.json

# CORRECT
dotnet run --project dotnet/src/AiCatalog.Cli -- explore https://example.com/ai-catalog.json
dotnet run --project dotnet/src/AiCatalog.Cli -- convert marketplace testcases/marketplace-input.json
```

### ❌ Forgetting `marketplace` subcommand under `convert`

```powershell
# WRONG — convert is a command group, not a command
dotnet run --project dotnet/src/AiCatalog.Cli -- convert testcases/marketplace-input.json

# CORRECT — must specify the subcommand
dotnet run --project dotnet/src/AiCatalog.Cli -- convert marketplace testcases/marketplace-input.json
```

### ❌ Using wrong identifier for install

```powershell
# WRONG — install uses exact string match on identifier, not display name
dotnet run --project dotnet/src/AiCatalog.Cli -- install https://example.com/catalog.json "My Cool Tool"

# CORRECT — use the entry's identifier field (often a URN)
dotnet run --project dotnet/src/AiCatalog.Cli -- install https://example.com/catalog.json "urn:example:my-cool-tool"
```

### ❌ Not building first after code changes

```powershell
# If you've modified the CLI source, dotnet run will rebuild automatically.
# But if you want to verify the build succeeds first:
dotnet build dotnet/src/AiCatalog.Cli
dotnet run --project dotnet/src/AiCatalog.Cli -- --help
```
