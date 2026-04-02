"""Shared test fixtures and configuration for aicatalog tests.

Discovers and loads all JSON test fixtures from the shared testcases/ directory.
"""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

import pytest

# Path to shared cross-language test fixtures
TESTCASES_DIR = Path(__file__).resolve().parent.parent.parent / "testcases"
NEGATIVE_DIR = TESTCASES_DIR / "negative"


def _load_fixture(path: Path) -> dict[str, Any]:
    """Load a test fixture wrapper from a JSON file."""
    text = path.read_text(encoding="utf-8")
    return json.loads(text)



# Fixtures that are conversion inputs (not direct AI Catalog documents)
_CONVERSION_FIXTURES = {"marketplace-input"}


def _collect_positive_fixtures() -> list[tuple[str, dict[str, Any]]]:
    """Collect all positive (non-negative) JSON fixtures."""
    fixtures = []
    for p in sorted(TESTCASES_DIR.glob("*.json")):
        if p.stem in _CONVERSION_FIXTURES:
            continue
        try:
            data = _load_fixture(p)
        except json.JSONDecodeError:
            continue
        if "input" in data:
            fixtures.append((p.stem, data))
    return fixtures


def _collect_negative_fixtures() -> list[tuple[str, dict[str, Any] | str]]:
    """Collect all negative test fixtures (JSON + the .txt special case)."""
    fixtures = []
    for p in sorted(NEGATIVE_DIR.glob("*.json")):
        try:
            data = _load_fixture(p)
        except json.JSONDecodeError:
            continue
        if "expected_error" in data:
            fixtures.append((p.stem, data))
    # Special case: invalid-json.txt
    txt = NEGATIVE_DIR / "invalid-json.txt"
    if txt.exists():
        fixtures.append(("invalid-json", txt.read_text(encoding="utf-8")))
    return fixtures


# Materialized lists for parametrization
POSITIVE_FIXTURES = _collect_positive_fixtures()
NEGATIVE_FIXTURES = _collect_negative_fixtures()

POSITIVE_IDS = [name for name, _ in POSITIVE_FIXTURES]
NEGATIVE_IDS = [name for name, _ in NEGATIVE_FIXTURES]


@pytest.fixture(params=POSITIVE_FIXTURES, ids=POSITIVE_IDS)
def positive_fixture(request: pytest.FixtureRequest) -> tuple[str, dict[str, Any]]:
    """Parametrized fixture yielding (name, fixture_data) for each positive test case."""
    return request.param


@pytest.fixture(params=NEGATIVE_FIXTURES, ids=NEGATIVE_IDS)
def negative_fixture(request: pytest.FixtureRequest) -> tuple[str, dict[str, Any] | str]:
    """Parametrized fixture yielding (name, fixture_data_or_raw_text) for each negative case."""
    return request.param
