# Deckard — History

## Project Context
- **Project:** ai-catalog — SpecWorks Part implementing the AI Card specification
- **Spec:** https://agent-card.github.io/ai-card/ (AI Catalog: JSON format for discovering heterogeneous AI artifacts)
- **Languages:** .NET, Python
- **User:** Darrel Miller (from Dev Box)
- **Factory upstream:** spec-works/factory-squad (skills, decisions, routing inherited)

## Learnings

Team formed 2026-04-02. Initial setup — no code yet.

### 2026-05-16: A2A-Ask Catalog Integration Proposal

**Decision proposal written:** `.squad/decisions/inbox/deckard-catalog-integration.md`

**Key findings:**
- Recommended `A2A-Ask` consume `SpecWorks.AiCatalog` via NuGet rather than duplicate parsing or add another hard cross-repo project reference.
- Best UX is explicit `catalog list/show` plus making `discover`/`send`/`stream`/`task`/`auth login` catalog-aware with a shared `--agent` selector.
- Auto-routing should stay deterministic: only auto-select for a single clear candidate; otherwise surface choices and require `--agent`.
- Catalog entry media types for A2A cards are currently inconsistent across repo artifacts (`application/a2a-agent-card+json` vs `application/vnd.a2a.agent-card+json`), so alias handling and eventual normalization are required.

### 2026-05-16: A2A-Ask Catalog Cache Proposal

**Decision proposal written:** `.squad/decisions/inbox/deckard-catalog-cache.md`

**Key findings:**
- Recommended a first-class catalog cache under `~/.a2a-ask/catalogs/`, separate from `tokens/` but within the same CLI home directory.
- Cache identity should be canonical catalog document URL plus auth partition, not just host origin, so multi-path and per-user catalogs stay isolated.
- Persist a versioned envelope containing both raw catalog JSON and a normalized agent index, enabling fast follow-up `--agent` resolution across separate CLI invocations.
- Use read-through caching with a 15-minute TTL, conditional HTTP refresh, explicit `--refresh`, stale fallback on transient failures, and no persistence for `Cache-Control: no-store` responses.
- Support catalog aliases for ergonomics, but keep token storage keyed by canonical resolved agent URL rather than `(catalog, agent)` to avoid coupling auth to discovery paths.

### 2026-04-02: Architecture Decisions

**Architecture decisions recorded:** `.squad/decisions/inbox/deckard-architecture.md` (ADR-001 through ADR-008)

**Key decisions:**
- Project structure: `dotnet/src/AICatalog/` (library) + `dotnet/src/AICatalog.Cli/` (CLI tool), `python/src/specworks_ai_catalog/` (library + CLI)
- Domain model: 9 public types mirroring CDDL schema (AiCatalog, CatalogEntry, HostInfo, CollectionReference, Publisher, TrustManifest, TrustSchema, Attestation, ProvenanceLink)
- Validation: 3 conformance levels (Minimal, Discoverable, Trusted) with structured diagnostics
- Parsing: System.Text.Json (.NET), json stdlib (Python) — no external deps in core lib
- CLI commands: `convert marketplace`, `explore`, `install` — thin layer over library
- Cross-language consistency enforced via shared `testcases/` fixtures (36+ test cases)
- specs.json uses RFC 9264 linkset format per factory convention

**Phased plan:** 8 phases — Spec Analysis → Test Cases → Parsing → Validation → CLI (3 phases) → Packaging

**User preferences (Darrel Miller):**
- Wants marketplace.json conversion as a CLI feature
- Wants catalog exploration (load from URL, browse entries)
- Wants skill/MCP plugin download and local enable
- Prioritized: library first, then test cases, then CLI

**Spec insights:**
- AI Card spec defines `application/ai-catalog+json` media type
- Entry content: exactly one of `url` or `inline` (mutual exclusion rule)
- Multi-version entries: identifier+version uniqueness when version present
- Trust manifest identity MUST match entry identifier
- Spec appendixes define mappings for OCI, MCP Registry, and Claude Code Plugins marketplace
- CDDL schema is normative reference for field requirements

### 2026-04-02: Tyrell's Spec Requirements Extracted

**Tyrell produced 80+ requirements, 108 test case descriptions, and 9 interpretation decisions.** Ready for test case authoring (Leon) and implementation (Roy/Pris).

**Key interpretation decisions (TD-1 through TD-9):** inline null handling, version format flexibility, mixed versioning, URI comparison, weak digest rejection, open model for unknown fields, conformance auto-detect, nested bundle validation, informative appendices scope.

**Impact on Deckard's architecture:** ADR-002 (domain model) and ADR-003 (validation) now have concrete requirements from spec extraction. ADR-004 (parsing) aligns with TD-6 (open model for unknown fields).

### 2026-04-02: Spec Feedback Written — API Design Perspective

**Deckard produced spec feedback document:** `docs/spec-feedback-architecture.md` — 20 items covering implementation friction, cross-language challenges, and forward compatibility.

**Key findings:**
- **API Friction:** url/inline exclusivity, metadata open maps, collection navigation, version format flexibility, trust identity binding
- **Cross-Language Challenges:** identifier normalization, digest algorithm vetting, unknown version handling
- **Tooling Gaps:** well-known URI discovery, media type registry, conformance levels, bundle validation depth
- **Forward Compatibility:** spec versioning strategy missing, backward compatibility rules undefined

**Recommendations:** Mostly clarifications (spec prose), test cases for edge cases, and registries (digest algorithms, identity types, well-known tags).

**Output:** Supplements Tyrell's detailed spec feedback with API design angle — focused on what makes libraries hard to build consistently across .NET and Python.

### 2025-01-27: Toolbox Architecture Proposal

**Decision proposal written:** `.squad/decisions/inbox/deckard-toolbox-architecture.md`

**Key design:**
- Evolved AI Catalog from "catalog parser" to "standard toolbox" — a service locator pattern for AI tools across heterogeneous protocols (A2A, MCP, CLI skills).
- New `SpecWorks.AiCatalog.Toolbox` namespace provides: `CatalogToolbox` (multi-catalog loader), `ToolEntry` (enriched entries with resolved URLs and capabilities), deterministic query/scoring API, and pluggable `MediaTypeRegistry`.
- **Deterministic not LLM-based**: Scoring uses text matching (displayName, description, tags) with transparent 0-1.0 scores. Clients can add semantic ranking themselves. No hidden model calls.
- **Metadata-only by default**: Doesn't fetch agent cards during indexing (performance, privacy, caching complexity). Clients fetch referenced content lazily.
- **Client-owned concerns**: Caching strategy (a2a-ask in `~/.a2a-ask/catalogs/`), auth, tool invocation, and selection policy stay in clients. Toolbox provides indexing and filtering infrastructure.
- **Media type awareness**: Ships with default mappings (A2A agent cards, MCP servers, Copilot/Claude skills, nested catalogs). Extensible via registry.
- **Multi-catalog federation**: Loads multiple catalogs in parallel, resolves relative URLs, handles deduplication, supports nested catalogs with bounded recursion.

**Phased rollout:** Core toolbox (1-2 weeks) → Multi-catalog queries (1 week) → CLI integration (1 week) → a2a-ask integration (separate repo) → Documentation (1 week).

**Key files to create:**
- `dotnet/src/AICatalog/Toolbox/CatalogToolbox.cs` — main API
- `dotnet/src/AICatalog/Toolbox/ToolEntry.cs` — enriched catalog entry
- `dotnet/src/AICatalog/Toolbox/MediaTypeRegistry.cs` — capability mapping
- Python equivalents under `python/src/specworks_ai_catalog/toolbox/`

**User preferences (Darrel Miller):**
- Wants AI Catalog to be the "standard toolbox" for tool discovery across clients
- Wants deterministic resolution with LLM as optional client enhancement
- Wants a2a-ask to consume toolbox via NuGet for `@agent@catalog` targeting

### 2026-05-16: A2A Discovery Helper Surface Added

**Implementation completed:** Added minimal helper surface to `SpecWorks.AiCatalog` v0.1.0 to support A2A-Ask catalog integration.

**Added files:**
- `dotnet/src/AiCatalog/KnownMediaTypes.cs` — static class with well-known media type constants:
  - `AiCatalog` = `"application/ai-catalog+json"`
  - `A2AAgentCard` = `"application/a2a-agent-card+json"`  
  - `A2AAgentCardVendor` = `"application/vnd.a2a.agent-card+json"`
- `dotnet/src/AiCatalog/CatalogEntryExtensions.cs` — extension method `IsA2AAgentCard()` with case-insensitive matching
- `dotnet/test/AiCatalog.Tests/A2ADiscoveryHelperTests.cs` — 12 comprehensive unit tests

**Key decisions:**
- Media type comparison uses `StringComparison.OrdinalIgnoreCase` per RFC 2045
- Extension method checks both standard and vendor-prefixed A2A media types
- Null-safe implementation with proper exception handling
- Comprehensive test coverage including case-insensitivity, edge cases, and partial matches

**Test results:** All 12 new tests pass; integration verified with existing build pipeline.
