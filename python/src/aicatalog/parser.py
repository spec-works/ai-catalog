"""JSON parsing for the AI Card specification.

Converts raw JSON strings into domain model objects. Does NOT validate
conformance — that is handled separately by the validator module.
"""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from .exceptions import AiCatalogParseError
from .models import (
    AiCatalog,
    Attestation,
    CatalogEntry,
    HostInfo,
    ProvenanceLink,
    Publisher,
    TrustManifest,
    TrustSchema,
)

# camelCase JSON key → snake_case Python field
_CATALOG_KEYS = {"specVersion": "spec_version"}
_ENTRY_KEYS = {
    "displayName": "display_name",
    "mediaType": "media_type",
    "updatedAt": "updated_at",
    "trustManifest": "trust_manifest",
}
_HOST_KEYS = {
    "displayName": "display_name",
    "documentationUrl": "documentation_url",
    "logoUrl": "logo_url",
    "trustManifest": "trust_manifest",
}
_TRUST_MANIFEST_KEYS = {
    "identityType": "identity_type",
    "trustSchema": "trust_schema",
    "privacyPolicyUrl": "privacy_policy_url",
    "termsOfServiceUrl": "terms_of_service_url",
}
_TRUST_SCHEMA_KEYS = {
    "governanceUri": "governance_uri",
    "verificationMethods": "verification_methods",
}
_PUBLISHER_KEYS = {
    "displayName": "display_name",
    "identityType": "identity_type",
}
_PROVENANCE_KEYS = {
    "sourceId": "source_id",
    "sourceDigest": "source_digest",
    "registryUri": "registry_uri",
    "statementUri": "statement_uri",
    "signatureRef": "signature_ref",
}
_ATTESTATION_KEYS = {
    "mediaType": "media_type",
}

# Known fields for each object type (camelCase) — anything else is silently ignored (VH-2)
_CATALOG_KNOWN = {"specVersion", "entries", "host", "metadata"}
_ENTRY_KNOWN = {
    "identifier", "displayName", "mediaType", "url", "data", "version",
    "description", "tags", "publisher", "trustManifest", "updatedAt", "metadata",
}


def _remap(data: dict[str, Any], key_map: dict[str, str]) -> dict[str, Any]:
    """Remap camelCase keys to snake_case using the given mapping."""
    result: dict[str, Any] = {}
    for k, v in data.items():
        result[key_map.get(k, k)] = v
    return result


def _require_str(value: Any, field_name: str, context: str = "") -> str:
    ctx = f" on {context}" if context else ""
    if value is None:
        raise AiCatalogParseError(f"{field_name} must be a non-null string{ctx}")
    if not isinstance(value, str):
        type_name = type(value).__name__
        type_map = {
            "int": "number", "float": "number", "bool": "boolean",
            "list": "array", "dict": "object",
        }
        json_type = type_map.get(type_name, type_name)
        raise AiCatalogParseError(f"{field_name} must be a string, got {json_type}{ctx}")
    return value


def _parse_provenance(data: Any, context: str) -> ProvenanceLink:
    if not isinstance(data, dict):
        raise AiCatalogParseError(f"provenance link must be a JSON object in {context}")
    missing = [f for f in ("relation", "sourceId") if f not in data]
    if missing:
        mapped = [{"sourceId": "sourceId"}.get(f, f) for f in missing]
        raise AiCatalogParseError(f"missing required fields on {context}: {', '.join(mapped)}")
    remapped = _remap(data, _PROVENANCE_KEYS)
    return ProvenanceLink(
        relation=remapped["relation"],
        source_id=remapped["source_id"],
        source_digest=remapped.get("source_digest"),
        registry_uri=remapped.get("registry_uri"),
        statement_uri=remapped.get("statement_uri"),
        signature_ref=remapped.get("signature_ref"),
    )


def _parse_attestation(data: Any, context: str) -> Attestation:
    if not isinstance(data, dict):
        raise AiCatalogParseError(f"attestation must be a JSON object in {context}")
    for req_field in ("type", "uri", "mediaType"):
        if req_field not in data:
            raise AiCatalogParseError(f"missing required field: {req_field} on {context}")
    remapped = _remap(data, _ATTESTATION_KEYS)
    size = remapped.get("size")
    if size is not None:
        if not isinstance(size, int) or isinstance(size, bool):
            raise AiCatalogParseError(f"attestation size must be an integer in {context}")
        if size < 0:
            raise AiCatalogParseError(
                f"attestation size must be a non-negative integer, got {size}"
            )
    return Attestation(
        type=remapped["type"],
        uri=remapped["uri"],
        media_type=remapped["media_type"],
        digest=remapped.get("digest"),
        size=size,
        description=remapped.get("description"),
    )


def _parse_trust_schema(data: Any, context: str) -> TrustSchema:
    if not isinstance(data, dict):
        raise AiCatalogParseError(f"trustSchema must be a JSON object in {context}")
    missing = [f for f in ("identifier", "version") if f not in data]
    if missing:
        raise AiCatalogParseError(
            f"missing required fields on trustSchema: {', '.join(missing)}"
        )
    remapped = _remap(data, _TRUST_SCHEMA_KEYS)
    return TrustSchema(
        identifier=remapped["identifier"],
        version=remapped["version"],
        governance_uri=remapped.get("governance_uri"),
        verification_methods=remapped.get("verification_methods", []),
    )


def _parse_trust_manifest(data: Any, context: str) -> TrustManifest:
    if not isinstance(data, dict):
        raise AiCatalogParseError(f"trustManifest must be a JSON object in {context}")
    if "identity" not in data:
        raise AiCatalogParseError("missing required field: identity on trustManifest")
    remapped = _remap(data, _TRUST_MANIFEST_KEYS)
    trust_schema = None
    if "trust_schema" in remapped and remapped["trust_schema"] is not None:
        trust_schema = _parse_trust_schema(remapped["trust_schema"], f"{context}.trustSchema")
    attestations = []
    for i, att in enumerate(remapped.get("attestations", [])):
        attestations.append(_parse_attestation(att, f"attestation[{i}]"))
    provenance = []
    for i, prov in enumerate(remapped.get("provenance", [])):
        provenance.append(_parse_provenance(prov, f"provenance[{i}]"))
    return TrustManifest(
        identity=remapped["identity"],
        identity_type=remapped.get("identity_type"),
        trust_schema=trust_schema,
        attestations=attestations,
        provenance=provenance,
        privacy_policy_url=remapped.get("privacy_policy_url"),
        terms_of_service_url=remapped.get("terms_of_service_url"),
        signature=remapped.get("signature"),
        metadata=remapped.get("metadata"),
    )


def _parse_publisher(data: Any, context: str) -> Publisher:
    if not isinstance(data, dict):
        raise AiCatalogParseError(f"publisher must be a JSON object in {context}")
    missing = [f for f in ("identifier", "displayName") if f not in data]
    if missing:
        raise AiCatalogParseError(
            f"missing required fields on publisher: {', '.join(missing)}"
        )
    remapped = _remap(data, _PUBLISHER_KEYS)
    return Publisher(
        identifier=remapped["identifier"],
        display_name=remapped["display_name"],
        identity_type=remapped.get("identity_type"),
    )


def _parse_host(data: Any) -> HostInfo:
    if not isinstance(data, dict):
        raise AiCatalogParseError("host must be a JSON object")
    if "displayName" not in data:
        raise AiCatalogParseError("missing required field: displayName on host")
    dn = data["displayName"]
    if not isinstance(dn, str):
        type_name = type(dn).__name__
        type_map = {"int": "number", "float": "number", "bool": "boolean"}
        json_type = type_map.get(type_name, type_name)
        raise AiCatalogParseError(f"host displayName must be a string, got {json_type}")
    remapped = _remap(data, _HOST_KEYS)
    trust_manifest = None
    if "trust_manifest" in remapped and remapped["trust_manifest"] is not None:
        trust_manifest = _parse_trust_manifest(remapped["trust_manifest"], "host.trustManifest")
    return HostInfo(
        display_name=remapped["display_name"],
        identifier=remapped.get("identifier"),
        documentation_url=remapped.get("documentation_url"),
        logo_url=remapped.get("logo_url"),
        trust_manifest=trust_manifest,
    )


def _parse_entry(data: Any, index: int) -> CatalogEntry:
    if not isinstance(data, dict):
        raise AiCatalogParseError(f"entry[{index}] must be a JSON object")

    for req_field, display in [
        ("identifier", "identifier"),
        ("displayName", "displayName"),
        ("mediaType", "mediaType"),
    ]:
        if req_field not in data:
            raise AiCatalogParseError(
                f"missing required field: {display} on entry[{index}]"
            )

    remapped = _remap(data, _ENTRY_KEYS)

    # Parse tags
    tags = remapped.get("tags", [])
    if not isinstance(tags, list):
        raise AiCatalogParseError(
            f"tags must be an array of strings, got {type(tags).__name__}"
        )
    for i, tag in enumerate(tags):
        if not isinstance(tag, str):
            raise AiCatalogParseError(
                f"tags must be an array of strings, found non-string element at index {i}"
            )

    # updatedAt type check
    updated_at = remapped.get("updated_at")
    if updated_at is not None and not isinstance(updated_at, str):
        type_name = type(updated_at).__name__
        type_map = {"int": "number", "float": "number", "bool": "boolean"}
        json_type = type_map.get(type_name, type_name)
        raise AiCatalogParseError(
            f"updatedAt must be a string (RFC 3339 datetime), got {json_type}"
        )

    # Parse publisher
    publisher = None
    if "publisher" in remapped and remapped["publisher"] is not None:
        publisher = _parse_publisher(remapped["publisher"], f"entry[{index}].publisher")

    # Parse trust manifest
    trust_manifest = None
    if "trust_manifest" in remapped and remapped["trust_manifest"] is not None:
        trust_manifest = _parse_trust_manifest(
            remapped["trust_manifest"], f"entry[{index}].trustManifest"
        )

    # Collect extension fields (anything not in known set)
    extra_fields: dict[str, Any] = {}
    for k, v in data.items():
        if k not in _ENTRY_KNOWN:
            extra_fields[k] = v

    return CatalogEntry(
        identifier=remapped["identifier"],
        display_name=remapped["display_name"],
        media_type=remapped["media_type"],
        url=remapped.get("url"),
        data=remapped.get("data"),
        version=remapped.get("version"),
        description=remapped.get("description"),
        tags=tags,
        publisher=publisher,
        trust_manifest=trust_manifest,
        updated_at=updated_at,
        metadata=remapped.get("metadata"),
        extra_fields=extra_fields,
    )


def _parse_dict(data: dict[str, Any]) -> AiCatalog:
    """Parse a Python dict into an AiCatalog model."""
    if not isinstance(data, dict):
        type_name = type(data).__name__
        type_map = {
            "list": "array", "int": "number", "float": "number",
            "str": "string", "bool": "boolean",
        }
        json_type = type_map.get(type_name, type_name)
        raise AiCatalogParseError(f"root document must be a JSON object, got {json_type}")

    # specVersion — parse first per VH-4
    if "specVersion" not in data:
        raise AiCatalogParseError("missing required field: specVersion")
    sv = data["specVersion"]
    if sv is None:
        raise AiCatalogParseError("specVersion must be a non-null string")
    sv = _require_str(sv, "specVersion")
    if sv == "":
        raise AiCatalogParseError("specVersion must not be empty")

    # Validate Major.Minor format (VH-1)
    _validate_spec_version(sv)

    # entries
    if "entries" not in data:
        raise AiCatalogParseError("missing required field: entries")
    raw_entries = data["entries"]
    if not isinstance(raw_entries, list):
        type_name = type(raw_entries).__name__
        type_map = {
            "dict": "object", "int": "number", "float": "number",
            "str": "string", "bool": "boolean",
        }
        json_type = type_map.get(type_name, type_name)
        raise AiCatalogParseError(f"entries must be an array, got {json_type}")

    entries = [_parse_entry(e, i) for i, e in enumerate(raw_entries)]

    # host
    host = None
    if "host" in data and data["host"] is not None:
        host = _parse_host(data["host"])

    # metadata
    metadata = data.get("metadata")

    # Unknown fields are silently ignored per VH-2 (MUST-ignore rule)
    extra_fields: dict[str, Any] = {}
    for k, v in data.items():
        if k not in _CATALOG_KNOWN:
            extra_fields[k] = v

    return AiCatalog(
        spec_version=sv,
        entries=entries,
        host=host,
        metadata=metadata,
        extra_fields=extra_fields,
    )


# Supported major version
_SUPPORTED_MAJOR_VERSION = 1


def _validate_spec_version(sv: str) -> None:
    """Validate specVersion is Major.Minor format with non-negative integers (VH-1).

    Rejects unsupported major versions with informative error (VH-6).
    Accepts any minor version within the supported major version (VH-5).
    """
    parts = sv.split(".")
    if len(parts) != 2:
        raise AiCatalogParseError(
            f"specVersion must be in Major.Minor format (e.g., '1.0'), found '{sv}'"
        )
    try:
        major = int(parts[0])
        minor = int(parts[1])
    except ValueError:
        raise AiCatalogParseError(
            f"specVersion major and minor components must be "
            f"non-negative integers, found '{sv}'"
        ) from None
    if major < 0 or minor < 0:
        raise AiCatalogParseError(
            f"specVersion major and minor components must be non-negative integers, found '{sv}'"
        )
    if major > _SUPPORTED_MAJOR_VERSION:
        raise AiCatalogParseError(
            f"unsupported specVersion major version: {major} "
            f"(this implementation supports major version {_SUPPORTED_MAJOR_VERSION})"
        )


def parse(json_string: str) -> AiCatalog:
    """Parse a JSON string into an AiCatalog.

    Raises AiCatalogParseError on malformed JSON or structural issues.
    Does NOT validate conformance.
    """
    try:
        data = json.loads(json_string)
    except (json.JSONDecodeError, ValueError) as e:
        raise AiCatalogParseError(f"invalid JSON: {e}") from e

    if not isinstance(data, dict):
        type_name = type(data).__name__
        type_map = {
            "list": "array", "int": "number", "float": "number",
            "str": "string", "bool": "boolean",
        }
        json_type = type_map.get(type_name, type_name)
        raise AiCatalogParseError(f"root document must be a JSON object, got {json_type}")

    return _parse_dict(data)


def parse_file(path: str | Path) -> AiCatalog:
    """Parse a JSON file into an AiCatalog.

    Raises AiCatalogParseError on file read errors, malformed JSON, or structural issues.
    """
    try:
        text = Path(path).read_text(encoding="utf-8")
    except OSError as e:
        raise AiCatalogParseError(f"cannot read file: {e}") from e
    return parse(text)
