"""Custom exception hierarchy for the aicatalog package."""

from __future__ import annotations


class AiCatalogError(Exception):
    """Base exception for all AI Catalog errors."""


class AiCatalogParseError(AiCatalogError):
    """Raised when JSON parsing or structural deserialization fails."""


class AiCatalogValidationError(AiCatalogError):
    """Raised when conformance validation fails."""
