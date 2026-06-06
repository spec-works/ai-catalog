# Generated Workflow for spec-works.github.io

These files are generated in the **ai-catalog** repo and intended to be applied
to the **spec-works/spec-works.github.io** repository.

## Files

| File | Destination in target repo | Purpose |
|------|---------------------------|---------|
| `docs.yml` | `.github/workflows/docs.yml` | Updated GitHub Actions workflow |

## What changed vs the original workflow

1. **Multi-version .NET setup** — added `9.0.x` alongside `10.0.x` because the
   ai-catalog CLI targets `net9.0` while DocFX needs `10.0.x`.
2. **`repository_dispatch` trigger** — the `plugins-updated` event type lets the
   `spec-works/plugins` repo trigger a site rebuild when marketplace.json changes.
   Add a workflow in that repo that sends:
   ```yaml
   - uses: peter-evans/repository-dispatch@v3
     with:
       repository: spec-works/spec-works.github.io
       event-type: plugins-updated
   ```
3. **AI Catalog generation steps** (inserted before "Upload artifact"):
   - Sparse-checkout of `spec-works/plugins` to get `marketplace.json`
   - Sparse-checkout of `spec-works/ai-catalog` to get CLI source (`dotnet/`)
   - Build the CLI (`dotnet build`)
   - Run `ai-catalog migrate` to produce `ai-catalog.json`
   - Output placed at `docs/_site/.well-known/ai-catalog.json`

## Resulting URL

After deployment, the catalog will be available at:

```
https://spec-works.github.io/.well-known/ai-catalog.json
```

## Content-Type

GitHub Pages serves `.json` files with `Content-Type: application/json` by
default, which is the correct type for AI Catalog. No custom `_headers` file or
additional configuration is needed.

## How to apply

Copy `docs.yml` into the target repo:

```bash
cp docs.yml /path/to/spec-works.github.io/.github/workflows/docs.yml
```

Then commit and push to `main` to trigger the updated pipeline.
