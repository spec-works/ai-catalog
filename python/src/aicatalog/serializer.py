"""JSON serialization for the AI Card specification.

Converts domain model objects back to JSON, mapping snake_case fields
to camelCase JSON keys and omitting None optional fields.
"""

from __future__ import annotations

import json
from typing import Any

from .models import (
    AiCatalog,
    Attestation,
    CatalogEntry,
    CollectionReference,
    HostInfo,
    ProvenanceLink,
    Publisher,
    TrustManifest,
    TrustSchema,
)


def _omit_none(d: dict[str, Any]) -> dict[str, Any]:
    """Remove keys with None values."""
    return {k: v for k, v in d.items() if v is not None}


def _omit_empty(d: dict[str, Any]) -> dict[str, Any]:
    """Remove keys with None values and empty lists."""
    result: dict[str, Any] = {}
    for k, v in d.items():
        if v is None:
            continue
        if isinstance(v, list) and len(v) == 0:
            continue
        result[k] = v
    return result


def _serialize_provenance(prov: ProvenanceLink) -> dict[str, Any]:
    return _omit_none({
        "relation": prov.relation,
        "sourceId": prov.source_id,
        "sourceDigest": prov.source_digest,
        "registryUri": prov.registry_uri,
        "statementUri": prov.statement_uri,
        "signatureRef": prov.signature_ref,
    })


def _serialize_attestation(att: Attestation) -> dict[str, Any]:
    return _omit_none({
        "type": att.type,
        "uri": att.uri,
        "mediaType": att.media_type,
        "digest": att.digest,
        "size": att.size,
        "description": att.description,
    })


def _serialize_trust_schema(ts: TrustSchema) -> dict[str, Any]:
    result: dict[str, Any] = {
        "identifier": ts.identifier,
        "version": ts.version,
    }
    if ts.governance_uri is not None:
        result["governanceUri"] = ts.governance_uri
    if ts.verification_methods:
        result["verificationMethods"] = ts.verification_methods
    return result


def _serialize_trust_manifest(tm: TrustManifest) -> dict[str, Any]:
    result: dict[str, Any] = {"identity": tm.identity}
    if tm.identity_type is not None:
        result["identityType"] = tm.identity_type
    if tm.trust_schema is not None:
        result["trustSchema"] = _serialize_trust_schema(tm.trust_schema)
    if tm.attestations:
        result["attestations"] = [_serialize_attestation(a) for a in tm.attestations]
    if tm.provenance:
        result["provenance"] = [_serialize_provenance(p) for p in tm.provenance]
    if tm.privacy_policy_url is not None:
        result["privacyPolicyUrl"] = tm.privacy_policy_url
    if tm.terms_of_service_url is not None:
        result["termsOfServiceUrl"] = tm.terms_of_service_url
    if tm.signature is not None:
        result["signature"] = tm.signature
    if tm.metadata is not None:
        result["metadata"] = tm.metadata
    return result


def _serialize_publisher(pub: Publisher) -> dict[str, Any]:
    result: dict[str, Any] = {
        "identifier": pub.identifier,
        "displayName": pub.display_name,
    }
    if pub.identity_type is not None:
        result["identityType"] = pub.identity_type
    return result


def _serialize_host(host: HostInfo) -> dict[str, Any]:
    result: dict[str, Any] = {"displayName": host.display_name}
    if host.identifier is not None:
        result["identifier"] = host.identifier
    if host.documentation_url is not None:
        result["documentationUrl"] = host.documentation_url
    if host.logo_url is not None:
        result["logoUrl"] = host.logo_url
    if host.trust_manifest is not None:
        result["trustManifest"] = _serialize_trust_manifest(host.trust_manifest)
    return result


def _serialize_collection(col: CollectionReference) -> dict[str, Any]:
    result: dict[str, Any] = {
        "displayName": col.display_name,
        "url": col.url,
    }
    if col.description is not None:
        result["description"] = col.description
    if col.tags:
        result["tags"] = col.tags
    return result


def _serialize_entry(entry: CatalogEntry) -> dict[str, Any]:
    result: dict[str, Any] = {
        "identifier": entry.identifier,
        "displayName": entry.display_name,
        "mediaType": entry.media_type,
    }
    if entry.url is not None:
        result["url"] = entry.url
    if entry.inline is not None:
        result["inline"] = entry.inline
    if entry.version is not None:
        result["version"] = entry.version
    if entry.description is not None:
        result["description"] = entry.description
    if entry.tags:
        result["tags"] = entry.tags
    if entry.updated_at is not None:
        result["updatedAt"] = entry.updated_at
    if entry.publisher is not None:
        result["publisher"] = _serialize_publisher(entry.publisher)
    if entry.trust_manifest is not None:
        result["trustManifest"] = _serialize_trust_manifest(entry.trust_manifest)
    if entry.metadata is not None:
        result["metadata"] = entry.metadata
    # Preserve extension fields
    for k, v in entry.extra_fields.items():
        result[k] = v
    return result


def serialize_to_dict(catalog: AiCatalog) -> dict[str, Any]:
    """Serialize an AiCatalog to a Python dict with camelCase JSON keys.

    Omits None optional fields and empty lists.
    Preserves metadata values exactly.
    """
    result: dict[str, Any] = {
        "specVersion": catalog.spec_version,
    }
    if catalog.host is not None:
        result["host"] = _serialize_host(catalog.host)
    result["entries"] = [_serialize_entry(e) for e in catalog.entries]
    if catalog.collections:
        result["collections"] = [_serialize_collection(c) for c in catalog.collections]
    if catalog.metadata is not None:
        result["metadata"] = catalog.metadata
    # Preserve extension fields
    for k, v in catalog.extra_fields.items():
        result[k] = v
    return result


def serialize(catalog: AiCatalog) -> str:
    """Serialize an AiCatalog to a JSON string.

    Round-trip: parse → serialize → parse should produce equivalent result.
    """
    return json.dumps(serialize_to_dict(catalog), indent=2, ensure_ascii=False)
