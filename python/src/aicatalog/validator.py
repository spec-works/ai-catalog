"""Conformance validation for AI Catalog documents.

Validates against the three conformance levels: Minimal, Discoverable, Trusted.
Provides both auto-detect and validate-against APIs.
"""

from __future__ import annotations

import re
from dataclasses import dataclass, field
from enum import Enum
from typing import Any

from .models import AiCatalog, CatalogEntry


class ConformanceLevel(Enum):
    """Conformance levels per the AI Card specification."""

    MINIMAL = "minimal"
    DISCOVERABLE = "discoverable"
    TRUSTED = "trusted"


@dataclass
class ValidationResult:
    """Result of conformance validation."""

    is_valid: bool
    conformance_level: ConformanceLevel
    errors: list[str] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)


# RFC 3339 full date-time pattern (simplified but covers standard forms)
_RFC3339_FULL = re.compile(
    r"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(\.\d+)?([Zz]|[+\-]\d{2}:\d{2})$"
)
_DATE_ONLY = re.compile(r"^\d{4}-\d{2}-\d{2}$")

# Weak digest algorithms (must reject)
_WEAK_DIGESTS = {"md5", "sha1"}

# URI scheme pattern
_URI_SCHEME = re.compile(r"^[a-zA-Z][a-zA-Z0-9+\-\.]*:")


def _validate_entries_common(
    catalog: AiCatalog, errors: list[str], warnings: list[str]
) -> None:
    """Validate rules that apply across all conformance levels."""

    # Identifier + version uniqueness
    seen: dict[tuple[str, str | None], int] = {}
    for i, entry in enumerate(catalog.entries):
        key = (entry.identifier, entry.version)
        if key in seen:
            if entry.version is not None:
                errors.append(
                    f"duplicate (identifier, version) pair: "
                    f"('{entry.identifier}', '{entry.version}')"
                )
            else:
                errors.append(
                    f"duplicate identifier '{entry.identifier}' without version differentiation"
                )
        else:
            seen[key] = i

    for i, entry in enumerate(catalog.entries):
        # url/inline exclusivity
        has_url = entry.url is not None
        has_inline = entry.inline is not None
        if has_url and has_inline:
            errors.append(
                f"entry[{i}] must have exactly one of 'url' or 'inline', found both"
            )
        elif not has_url and not has_inline:
            errors.append(f"entry[{i}] must have exactly one of 'url' or 'inline'")

        # URL must be HTTPS (not HTTP)
        if entry.url is not None and entry.url.startswith("http://"):
            errors.append("url uses HTTP; MUST be HTTPS per security requirements")

        # tags validation
        if entry.tags:
            for ti, tag in enumerate(entry.tags):
                if not isinstance(tag, str):
                    errors.append(
                        f"tags must be an array of strings, found non-string element at index {ti}"
                    )

        # updatedAt RFC 3339 validation
        if entry.updated_at is not None:
            if _DATE_ONLY.match(entry.updated_at):
                errors.append(
                    "updatedAt must be a full RFC 3339 datetime, got date-only value"
                )
            elif not _RFC3339_FULL.match(entry.updated_at):
                errors.append(
                    f"updatedAt is not a valid RFC 3339 datetime: '{entry.updated_at}'"
                )

        # Identifier should be a URI/URN (warning, not error)
        if entry.identifier and not _URI_SCHEME.match(entry.identifier):
            warnings.append("identifier SHOULD be a URN or URI")

        # Trust manifest validation
        if entry.trust_manifest is not None:
            _validate_trust_manifest(entry.trust_manifest, entry, i, errors, warnings)

        # Bundle inline validation (TD-8)
        if (
            entry.inline is not None
            and isinstance(entry.inline, dict)
            and entry.media_type == "application/ai-catalog+json"
        ):
            _validate_bundle_inline(entry.inline, i, errors)

    # Collection URL HTTPS check
    for i, col in enumerate(catalog.collections):
        if col.url.startswith("http://"):
            errors.append(
                f"collection[{i}] url uses HTTP; MUST be HTTPS per security requirements"
            )


def _validate_bundle_inline(inline: dict[str, Any], index: int, errors: list[str]) -> None:
    """Validate inline bundle catalog (TD-8: recursive validation)."""
    if "specVersion" not in inline:
        errors.append(
            f"inline catalog for entry[{index}] is not a valid AI Catalog: "
            "missing required field specVersion"
        )
        return
    if "entries" not in inline:
        errors.append(
            f"inline catalog for entry[{index}] is not a valid AI Catalog: "
            "missing required field entries"
        )


def _validate_trust_manifest(
    tm: Any,
    entry: CatalogEntry,
    index: int,
    errors: list[str],
    warnings: list[str],
) -> None:
    """Validate trust manifest rules."""
    from .models import TrustManifest

    if not isinstance(tm, TrustManifest):
        return

    # identity must match entry identifier (TD-4: exact string comparison)
    if tm.identity != entry.identifier:
        errors.append(
            f"trustManifest.identity '{tm.identity}' does not match "
            f"entry identifier '{entry.identifier}'"
        )

    # Attestation digest validation
    for att in tm.attestations:
        if att.digest is not None:
            _validate_digest(att.digest, errors)

    # Provenance digest validation
    for prov in tm.provenance:
        if prov.source_digest is not None:
            _validate_digest(prov.source_digest, errors)


def _validate_digest(digest: str, errors: list[str]) -> None:
    """Validate digest algorithm strength (TD-5)."""
    if ":" in digest:
        algo = digest.split(":")[0].lower()
        if algo in _WEAK_DIGESTS:
            errors.append(
                f"digest algorithm '{algo}' is not accepted; minimum is SHA-256"
            )


def _is_discoverable(catalog: AiCatalog) -> bool:
    """Check if catalog satisfies Level 2 (Discoverable): has host info."""
    return catalog.host is not None


def _is_trusted(catalog: AiCatalog) -> bool:
    """Check Level 3 (Trusted): host + all entries have trust manifests."""
    if not _is_discoverable(catalog):
        return False
    if not catalog.entries:
        return False
    return all(e.trust_manifest is not None for e in catalog.entries)


def _detect_level(catalog: AiCatalog) -> ConformanceLevel:
    """Auto-detect highest conformance level."""
    if _is_trusted(catalog):
        return ConformanceLevel.TRUSTED
    if _is_discoverable(catalog):
        return ConformanceLevel.DISCOVERABLE
    return ConformanceLevel.MINIMAL


def validate(catalog: AiCatalog) -> ValidationResult:
    """Validate a catalog and auto-detect its conformance level.

    Returns a ValidationResult with errors, warnings, and the detected level.
    """
    errors: list[str] = []
    warnings: list[str] = []

    _validate_entries_common(catalog, errors, warnings)

    level = _detect_level(catalog)

    return ValidationResult(
        is_valid=len(errors) == 0,
        conformance_level=level,
        errors=errors,
        warnings=warnings,
    )


def validate_level(
    catalog: AiCatalog, level: ConformanceLevel
) -> ValidationResult:
    """Validate a catalog against a declared conformance level.

    Validates common rules plus level-specific requirements.
    """
    errors: list[str] = []
    warnings: list[str] = []

    _validate_entries_common(catalog, errors, warnings)

    if level in (ConformanceLevel.DISCOVERABLE, ConformanceLevel.TRUSTED) and catalog.host is None:
            errors.append(
                f"conformance level '{level.value}' requires host information"
            )

    if level == ConformanceLevel.TRUSTED:
        for i, entry in enumerate(catalog.entries):
            if entry.trust_manifest is None:
                errors.append(
                    f"conformance level 'trusted' requires trustManifest on entry[{i}]"
                )

    detected = _detect_level(catalog)
    effective_level = level if len(errors) == 0 else detected

    return ValidationResult(
        is_valid=len(errors) == 0,
        conformance_level=effective_level,
        errors=errors,
        warnings=warnings,
    )
