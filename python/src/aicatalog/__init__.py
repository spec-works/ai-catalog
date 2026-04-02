"""specworks-aicatalog — Python implementation of the AI Card specification.

Public API:
    parse, parse_file, serialize, serialize_to_dict,
    validate, validate_level,
    AiCatalog, CatalogEntry, HostInfo, CollectionReference,
    Publisher, TrustManifest, TrustSchema, Attestation, ProvenanceLink,
    ConformanceLevel, ValidationResult,
    AiCatalogError, AiCatalogParseError, AiCatalogValidationError
"""

from .exceptions import AiCatalogError, AiCatalogParseError, AiCatalogValidationError
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
from .parser import parse, parse_file
from .serializer import serialize, serialize_to_dict
from .validator import ConformanceLevel, ValidationResult, validate, validate_level

__all__ = [
    # Parsing
    "parse",
    "parse_file",
    # Serialization
    "serialize",
    "serialize_to_dict",
    # Validation
    "validate",
    "validate_level",
    "ConformanceLevel",
    "ValidationResult",
    # Models
    "AiCatalog",
    "CatalogEntry",
    "HostInfo",
    "CollectionReference",
    "Publisher",
    "TrustManifest",
    "TrustSchema",
    "Attestation",
    "ProvenanceLink",
    # Exceptions
    "AiCatalogError",
    "AiCatalogParseError",
    "AiCatalogValidationError",
]
