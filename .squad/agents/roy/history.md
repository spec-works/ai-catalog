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
