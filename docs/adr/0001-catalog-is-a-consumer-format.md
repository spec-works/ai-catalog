# ADR-0001: AI Catalog is a Consumer Format, Not a Registry Format

**Date:** 2026-06-12
**Status:** Accepted
**Deciders:** Darrel Miller

## Context

As the AI Catalog CLI has evolved, features like `migrate` (marketplace → catalog conversion) have begun pulling registry-side metadata into the catalog format — synthetic publisher URNs, MCP server configurations, skill packaging details, and other operational data that belongs to the registry, not to consumers.

This creates two risks:

1. **Data leakage.** Registry metadata may include information that is private to the registry operator — internal identifiers, server configurations, access patterns, operational details. Encoding this into a consumer-facing format risks exposing information that should remain behind the registry boundary.

2. **Scope creep.** Attempting to make AI Catalog a complete representation of registry state leads to an ever-growing format that duplicates what purpose-built registry standards already handle well.

## Decision

**AI Catalog is a consumer-facing projection format, not a registry storage format.**

- AI Catalog documents are designed to be **served to consumers** so they can discover and install AI resources (agents, skills, MCP servers, etc.).
- AI Catalog is **not designed for maintaining a registry**. There is information needed when keeping a registry of AI resources that AI Catalog intentionally does not support.
- Registry operators should use purpose-built standards like [xRegistry](https://xregistry.io) to maintain their source of truth. AI Catalog documents should be **projected from** that registry metadata at publish time.
- The CLI should focus on **consuming** catalogs (discover, install, validate) and **projecting** catalogs from registry sources. It should not attempt to be a round-trip authoring tool for registry metadata.
- Features that pull private or operational registry data into the catalog format should be avoided or redesigned to project only consumer-relevant information.

## Consequences

- The `migrate` command converts marketplace formats into catalog format as a **one-way projection**, not a lossless round-trip. Information loss from source to catalog is expected and acceptable.
- The catalog format will not grow to accommodate registry-side concerns (e.g., MCP server launch configurations, internal identifiers, access control metadata).
- Registry operators who need full-fidelity storage should maintain their data in xRegistry or equivalent, and generate AI Catalog documents as a distribution artifact.
- Consumer-side tooling (discover, install, validate, export) remains the CLI's primary mission.
