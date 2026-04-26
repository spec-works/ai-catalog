"""specworks-aicatalog — Python implementation of the AI Card specification.

Public API:
    parse, parse_file, serialize, serialize_to_dict,
    validate, validate_level,
    AiCatalog, CatalogEntry, HostInfo,
    Publisher, TrustManifest, TrustSchema, Attestation, ProvenanceLink,
    ConformanceLevel, ValidationResult,
    AiCatalogError, AiCatalogParseError, AiCatalogValidationError
"""

from .converter import convert_marketplace, convert_marketplace_file
from .exceptions import AiCatalogError, AiCatalogParseError, AiCatalogValidationError
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
    # Conversion
    "convert_marketplace",
    "convert_marketplace_file",
    # Validation
    "validate",
    "validate_level",
    "ConformanceLevel",
    "ValidationResult",
    # Models
    "AiCatalog",
    "CatalogEntry",
    "HostInfo",
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
