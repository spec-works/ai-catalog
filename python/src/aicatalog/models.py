"""Domain model types for the AI Card specification.

Eight public dataclasses mapping 1:1 to the CDDL schema.
Python uses snake_case; JSON mapping to camelCase is handled in parser/serializer.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


@dataclass
class ProvenanceLink:
    """Provenance link describing the origin or derivation of an artifact."""

    relation: str
    source_id: str
    source_digest: str | None = None
    registry_uri: str | None = None
    statement_uri: str | None = None
    signature_ref: str | None = None


@dataclass
class Attestation:
    """An attestation (audit, certification) associated with a trust manifest."""

    type: str
    uri: str
    media_type: str
    digest: str | None = None
    size: int | None = None
    description: str | None = None


@dataclass
class TrustSchema:
    """Trust governance schema referenced by a trust manifest."""

    identifier: str
    version: str
    governance_uri: str | None = None
    verification_methods: list[str] = field(default_factory=list)


@dataclass
class TrustManifest:
    """Trust manifest providing identity, attestations, and provenance."""

    identity: str
    identity_type: str | None = None
    trust_schema: TrustSchema | None = None
    attestations: list[Attestation] = field(default_factory=list)
    provenance: list[ProvenanceLink] = field(default_factory=list)
    privacy_policy_url: str | None = None
    terms_of_service_url: str | None = None
    signature: str | None = None
    metadata: dict[str, Any] | None = None


@dataclass
class Publisher:
    """Publisher of a catalog entry."""

    identifier: str
    display_name: str
    identity_type: str | None = None


@dataclass
class HostInfo:
    """Host information for the catalog provider."""

    display_name: str
    identifier: str | None = None
    documentation_url: str | None = None
    logo_url: str | None = None
    trust_manifest: TrustManifest | None = None


@dataclass
class CatalogEntry:
    """A single entry in the AI Catalog."""

    identifier: str
    display_name: str
    media_type: str
    url: str | None = None
    data: Any | None = None
    version: str | None = None
    description: str | None = None
    tags: list[str] = field(default_factory=list)
    publisher: Publisher | None = None
    trust_manifest: TrustManifest | None = None
    updated_at: str | None = None
    metadata: dict[str, Any] | None = None
    extra_fields: dict[str, Any] = field(default_factory=dict)


@dataclass
class AiCatalog:
    """Top-level AI Catalog document."""

    spec_version: str
    entries: list[CatalogEntry] = field(default_factory=list)
    host: HostInfo | None = None
    metadata: dict[str, Any] | None = None
    extra_fields: dict[str, Any] = field(default_factory=dict)
