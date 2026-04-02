# specworks-aicatalog

Python implementation of the [AI Card specification](https://agent-card.github.io/ai-card/) (`application/ai-catalog+json`).

## Installation

```bash
pip install specworks-aicatalog
```

## Quick Start

```python
from aicatalog import parse, serialize, validate

# Parse
catalog = parse('{"specVersion": "1.0", "entries": []}')

# Validate
result = validate(catalog)
print(result.conformance_level)  # ConformanceLevel.MINIMAL

# Serialize
json_str = serialize(catalog)
```
