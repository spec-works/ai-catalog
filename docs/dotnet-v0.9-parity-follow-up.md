# .NET v0.9 parity slice

This implementation follows the pinned [AI Catalog v0.9 specification](https://github.com/Agent-Card/ai-catalog/blob/19208af2f8adbc0a6457e3ff36ce691a304fbd97/specification/ai-catalog.md).

## Implemented

- Models the v0.9 entry wire members, including required `type`, optional `displayName`, mutually exclusive `url`/`data`, and an array of `trustManifests`.
- Preserves all JSON values in `data` (including explicit `null`), reserved `additionalSignatures`, namespaced extensions, and unrecognized members.
- Maps defined optional publisher, trust-schema, provenance, and signature members to their wire names. Host members not defined by the spec, including a member named `extensions`, remain unrecognized JSON and are preserved without being interpreted.
- Keeps JSON parsing separate from structural validation. Parsing rejects duplicate JSON member names recursively (comparing decoded names); validation checks required members, content-source exclusivity including explicit `url: null`, explicit nulls for recognized typed members (including optional `version`), duplicate entry keys, unsigned attestation sizes, Trust Manifest bindings, URI/timestamp and digest structure. This is not a complete CDDL/schema validator; do not infer full structural alignment from these checks.
- Treats v0.9 as major version 0, not as v1.0. Consumers accept the supported major across minor versions and report higher unsupported majors. Marketplace conversion now emits `specVersion: "0.9"`.
- Applies the spec's `(identifier, version)` uniqueness key consistently in library and CLI validation.
- The CLI recognizes the v0.9 specification's listed core and ecosystem artifact types without warnings. Publishing local skill files and bundles emits `application/agent-skills+md` and `application/agent-skills+zip`, respectively.
- Never reports `Trusted` based only on a manifest or signature being present.

The shared `testcases/v0.9/` fixtures are consumed by the .NET tests. Round-trip tests cover arbitrary JSON `data`, extension and unrecognized-member preservation, and signed-manifest wire shape.

The marketplace converter's emitted `specVersion` was changed to `"0.9"` in this worktree. Its legacy artifact type names remain unresolved; reconcile those changes on a fresh base because upstream changed the converter independently. Version manifests are likewise left unchanged because this checkout is 12 commits behind upstream and the currently published package is 0.6.4; handle version integration from the fresh base.

Explicit-null diagnostics retain the parent model object from parsing and confirm that the same parent and null-valued member are still present at validation time. Changing the member to a non-null value or replacing its parent avoids a stale diagnostic. This presence tracking is structural-validation state, not a general immutable snapshot or a complete schema validator.

## Verification boundary and follow-up

This slice does **not** verify signatures or establish trust. The JWS value is opaque input; structural validation does not establish signer authentication, publisher authorization, artifact integrity, or a trust decision. Digest checks validate representation and reject recognized weak algorithms; they do not hash artifact bytes.

A future verification layer should be separate from parsing and structural validation. It should accept explicit local policy and a supported profile verifier, reconstruct the spec-defined JCS payload (omitting only the immediate `signature.jws` and applicable `additionalSignatures`), authenticate the signer, enforce signature time validity, and bind the signed subject to the entry. Artifact retrieval and digest verification should use an injected safe-fetch boundary with scheme/address/redirect, size, and timeout restrictions. Only a successful verification result combined with the required authorization and release-coverage checks may contribute to Trusted conformance; unsupported profiles or failed checks must never fall back to trust based on metadata presence.

Until that layer exists, the .NET API intentionally caps reported conformance at Discoverable and returns a diagnostic when Trusted is requested.
