# Deckard — History

## Project Context
- **Project:** ai-catalog — SpecWorks Part implementing the AI Card specification
- **Spec:** https://agent-card.github.io/ai-card/ (AI Catalog: JSON format for discovering heterogeneous AI artifacts)
- **Languages:** .NET, Python
- **User:** Darrel Miller (from Dev Box)
- **Factory upstream:** spec-works/factory-squad (skills, decisions, routing inherited)

## Learnings

Team formed 2026-04-02. Initial setup — no code yet.

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
