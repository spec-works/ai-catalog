# Roy  History

## Project Context
- **Project:** ai-catalog  SpecWorks Part implementing the AI Card specification
- **Spec:** https://agent-card.github.io/ai-card/ (AI Catalog: JSON format for discovering heterogeneous AI artifacts)
- **Languages:** .NET, Python
- **User:** Darrel Miller (from Dev Box)
- **Factory upstream:** spec-works/factory-squad (dotnet-patterns skill inherited)

## Core Context

**Phase 1 (2026-04-02 to 2026-04-02T11-57):** Full .NET core library (v0.1.0) and CLI tool delivered. 151 tests passing using 74 shared test fixtures. Core implementation with marketplace converter, explore/install/convert commands.

**Architecture:**
- Library: SpecWorks.AiCatalog (.NET 8/9, no external deps) with 9 domain types, Parser/Serializer/Validator
- CLI: AiCatalog.Cli (System.CommandLine) with convert/explore/install commands
- Marketplace converter: Claude format (display_name/manifest_url) + Copilot format (source/skills[])
- Conformance levels: MINIMAL/DISCOVERABLE/TRUSTED with auto-detect
- Key pattern: [JsonExtensionData] preserves unknown fields for round-trip

**Later work (2026-04-03 to 2026-07-18):**
- Integration tests (30 tests, real spec-works/work-iq plugins, 180 total passing)
- CLI skill documentation (dotnet-cli SKILL.md)
- PR #33 spec delta: breaking changes (inlinedata, collections removal, nesting 84, unknown-field MUST-ignore)
- Plugin-as-catalog refactor (plugins=nested catalogs, skills=sub-entries)
- GitHub Pages integration workflow (docs.yml for spec-works.github.io)
- All 210 tests passing (168 library + 42 CLI) after PR #33; production-ready

## Learnings

### 2026-05-16T23:48  A2A-Ask Issues #2/#3 Fixed: Direct Send + v0.3 Client Selection

**Community-reported issues fixed on A2A-Ask repository:**

- **Issue #2:** v0.3 agents rejecting JSON-RPC method names (SendMessage vs Send mismatch)
- **Issue #3:** Unnecessary agent card fetch before every send/stream/task command

**Solution implemented:**
- Modified CommonOptions.CreateClientAsync() to select client version from --a2a-version flag instead of card probe
- Direct URLs now bypass card fetch entirely (only applies to direct endpoint URLs)
- Catalog-resolved targets (@agent@catalog) still fetch cards as needed
- v0.3 support uses correct PascalCase method names (SendMessage, StreamMessage, PostTask)

**Changes:**
- CommonOptions.cs  Version-based client selection
- SendCommand.cs, StreamCommand.cs, TaskCommand.cs  Direct URL handling
- DirectClientTests.cs, RequestCaptureState.cs  New integration tests
- TestAgentServer/Program.cs  Extended test endpoints

**Verification:** Both direct and catalog scenarios tested; no regressions.

**Outcome:** Issues closed on GitHub; commit d2d0589 merged to master; pushed.

### 2026-05-16 Documentation updates for catalog discovery and A2A-Ask

**Docs updated:**
- `ai-catalog\README.md` — added `.NET Library API` coverage for `KnownMediaTypes` and `CatalogEntryExtensions.IsA2AAgentCard()`.
- `A2A-Ask\README.md` — added concise catalog integration guidance, target syntax, catalog commands, direct-send behavior, and `--a2a-version 0.3` guidance.
- `A2A-Ask\skill\SKILL.md` — added executable catalog command guidance, target syntax, direct-send notes, v0.3 guidance, and catalog-first workflow updates.
- `A2A-Ask\docs\cli-reference.md` — added `catalog list` / `catalog show` reference entries and direct URL/version notes.

**Structure decisions:**
- Documented the actual CLI surface as implemented: `catalog show <target>` resolves a specific agent via `@agent@catalog`, rather than inventing a separate entry-id argument.
- Called out bare `@agent` targets as parsed-but-incomplete in Phase 1 so docs stay accurate while still explaining the full target grammar.

### 2026-05-16T23:56 Documentation & A2A Integration

**Updated documentation across repositories to reflect implementation work on catalog discovery and v0.3 compatibility:**

- **ai-catalog/README.md** — Added `.NET Library API` section with `KnownMediaTypes` constants and `CatalogEntryExtensions.IsA2AAgentCard()` method documentation
- **A2A-Ask/README.md** — Concise catalog integration guidance: @agent@catalog syntax, catalog commands, direct-send behavior, --a2a-version flag
- **A2A-Ask/skill/SKILL.md** — Executable catalog command guidance, target syntax, direct-send notes, v0.3 workflow
- **A2A-Ask/docs/cli-reference.md** — Reference entries for `catalog list` and `catalog show` with version selection guidance

**Clarifications documented:**
- v0.3 agents use PascalCase JSON-RPC methods (SendMessage, StreamMessage, PostTask)
- Direct URLs bypass card fetch; only @agent@catalog references trigger catalog resolution
- --a2a-version 0.3 flag enables v0.3 client selection

**Outcome:** All documentation updated; ready for publication.

### 2026-05-17 OAuth2 client registration for A2A-Ask issue #4

**Implemented in `A2A-Ask`:** persistent OAuth2 client registration and issuer-based auto-selection for `auth login`.

- Added `dotnet/src/A2A-Ask/Auth/ClientRegistrationStore.cs` using the same DPAPI-on-Windows / JSON-on-other-platforms persistence pattern as `TokenStore`.
- Extended `dotnet/src/A2A-Ask/Commands/AuthLoginCommand.cs` with `auth register-client`, `auth list-clients`, and `auth remove-client`, plus issuer extraction from discovery metadata or token authority.
- Updated `dotnet/src/A2A-Ask/Auth/DeviceCodeFlow.cs` and `AuthCodeFlow.cs` so stored client IDs are honored during interactive login; device code and refresh requests now preserve optional RFC 8707 `resource` values.
- Extended `TokenResult` to persist `ClientId` and `Resource` so auto-refresh reuses the same OAuth client context.
- Added focused tests in `dotnet/tests/A2A-Ask.Tests/AuthCommandTests.cs` and `ClientRegistrationStoreTests.cs`.

**Verification:** `dotnet build --nologo` and `dotnet test --nologo --no-build` both passed from `A2A-Ask\dotnet` (107 tests).
