using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using SpecWorks.AiCatalog.Models;

namespace SpecWorks.AiCatalog.Validation;

/// <summary>
/// Validates <see cref="AiCatalog"/> instances against the AI Card specification conformance levels.
/// </summary>
public static partial class AiCatalogValidator
{
    private static readonly HashSet<string> s_weakDigestAlgorithms = new(StringComparer.OrdinalIgnoreCase)
    {
        "md5", "sha1"
    };

    /// <summary>
    /// Validates a catalog and auto-detects the highest conformance level.
    /// </summary>
    /// <param name="catalog">The catalog to validate.</param>
    /// <returns>A <see cref="ValidationResult"/> with detected conformance level.</returns>
    public static ValidationResult Validate(Models.AiCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var errors = new List<ValidationDiagnostic>();
        var warnings = new List<ValidationDiagnostic>();

        // Always validate Minimal (core structural rules)
        ValidateMinimal(catalog, errors, warnings);

        if (errors.Count > 0)
        {
            return new ValidationResult
            {
                ConformanceLevel = ConformanceLevel.Minimal,
                Errors = errors,
                Warnings = warnings,
            };
        }

        // Check for Discoverable
        var discoverableErrors = new List<ValidationDiagnostic>();
        ValidateDiscoverable(catalog, discoverableErrors, warnings);

        if (discoverableErrors.Count > 0)
        {
            return new ValidationResult
            {
                ConformanceLevel = ConformanceLevel.Minimal,
                Errors = [],
                Warnings = [.. warnings, .. discoverableErrors.Select(e =>
                    new ValidationDiagnostic(DiagnosticSeverity.Warning, $"Not discoverable: {e.Message}", e.Path))],
            };
        }

        // Check for Trusted
        var trustedErrors = new List<ValidationDiagnostic>();
        ValidateTrusted(catalog, trustedErrors, warnings);

        if (trustedErrors.Count > 0)
        {
            return new ValidationResult
            {
                ConformanceLevel = ConformanceLevel.Discoverable,
                Errors = [],
                Warnings = [.. warnings, .. trustedErrors.Select(e =>
                    new ValidationDiagnostic(DiagnosticSeverity.Warning, $"Not trusted: {e.Message}", e.Path))],
            };
        }

        return new ValidationResult
        {
            ConformanceLevel = ConformanceLevel.Trusted,
            Errors = [],
            Warnings = warnings,
        };
    }

    /// <summary>
    /// Validates a catalog against a specific conformance level.
    /// </summary>
    /// <param name="catalog">The catalog to validate.</param>
    /// <param name="level">The conformance level to validate against.</param>
    /// <returns>A <see cref="ValidationResult"/> for the specified level.</returns>
    public static ValidationResult Validate(Models.AiCatalog catalog, ConformanceLevel level)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var errors = new List<ValidationDiagnostic>();
        var warnings = new List<ValidationDiagnostic>();

        // Minimal is always validated
        ValidateMinimal(catalog, errors, warnings);

        if (level >= ConformanceLevel.Discoverable)
        {
            ValidateDiscoverable(catalog, errors, warnings);
        }

        if (level >= ConformanceLevel.Trusted)
        {
            ValidateTrusted(catalog, errors, warnings);
        }

        return new ValidationResult
        {
            ConformanceLevel = level,
            Errors = errors,
            Warnings = warnings,
        };
    }

    private static void ValidateMinimal(Models.AiCatalog catalog, List<ValidationDiagnostic> errors, List<ValidationDiagnostic> warnings)
    {
        // specVersion checks
        if (string.IsNullOrEmpty(catalog.SpecVersion))
        {
            errors.Add(new(DiagnosticSeverity.Error, "missing required field: specVersion", "specVersion"));
        }
        else
        {
            // Warn on non-Major.Minor format per TD-2
            if (!MajorMinorPattern().IsMatch(catalog.SpecVersion))
            {
                warnings.Add(new(DiagnosticSeverity.Warning,
                    $"specVersion '{catalog.SpecVersion}' is not in Major.Minor format", "specVersion"));
            }
        }

        // entries is required (already guaranteed by parser, but validate for direct construction)
        if (catalog.Entries is null)
        {
            errors.Add(new(DiagnosticSeverity.Error, "missing required field: entries", "entries"));
            return;
        }

        // Validate each entry
        for (int i = 0; i < catalog.Entries.Count; i++)
        {
            var entry = catalog.Entries[i];
            var prefix = $"entries[{i}]";
            ValidateEntry(entry, prefix, errors, warnings);
        }

        // Check identifier+version uniqueness
        ValidateIdentifierUniqueness(catalog.Entries, errors);

        // Validate collections if present
        if (catalog.Collections is not null)
        {
            for (int i = 0; i < catalog.Collections.Count; i++)
            {
                ValidateCollectionReference(catalog.Collections[i], $"collections[{i}]", errors, warnings);
            }
        }

        // Warn on unknown top-level extension properties (per Darrel's closed schema directive)
        if (catalog.ExtensionData is not null)
        {
            foreach (var key in catalog.ExtensionData.Keys)
            {
                warnings.Add(new(DiagnosticSeverity.Warning,
                    $"unknown property '{key}' at top level", key));
            }
        }
    }

    private static void ValidateEntry(CatalogEntry entry, string prefix,
        List<ValidationDiagnostic> errors, List<ValidationDiagnostic> warnings)
    {
        // Required fields
        if (string.IsNullOrEmpty(entry.Identifier))
        {
            errors.Add(new(DiagnosticSeverity.Error, "missing required field: identifier", $"{prefix}.identifier"));
        }

        if (string.IsNullOrEmpty(entry.DisplayName))
        {
            errors.Add(new(DiagnosticSeverity.Error, "missing required field: displayName", $"{prefix}.displayName"));
        }

        if (string.IsNullOrEmpty(entry.MediaType))
        {
            errors.Add(new(DiagnosticSeverity.Error, "missing required field: mediaType", $"{prefix}.mediaType"));
        }

        // url/inline exclusivity
        bool hasUrl = entry.Url is not null;
        bool hasInline = entry.Inline is not null && entry.Inline.Value.ValueKind != JsonValueKind.Undefined;

        if (hasUrl && hasInline)
        {
            errors.Add(new(DiagnosticSeverity.Error,
                $"{prefix} must have exactly one of 'url' or 'inline', found both", prefix));
        }
        else if (!hasUrl && !hasInline)
        {
            errors.Add(new(DiagnosticSeverity.Error,
                $"{prefix} must have exactly one of 'url' or 'inline'", prefix));
        }

        // URL HTTPS check
        if (hasUrl && entry.Url is not null)
        {
            ValidateHttpsUrl(entry.Url, $"{prefix}.url", errors, warnings);
        }

        // updatedAt RFC 3339 check
        if (entry.UpdatedAt is not null)
        {
            ValidateRfc3339DateTime(entry.UpdatedAt, $"{prefix}.updatedAt", errors, warnings);
        }

        // Publisher validation
        if (entry.Publisher is not null)
        {
            ValidatePublisher(entry.Publisher, $"{prefix}.publisher", errors, warnings);
        }

        // TrustManifest validation
        if (entry.TrustManifest is not null)
        {
            ValidateTrustManifest(entry.TrustManifest, entry.Identifier, $"{prefix}.trustManifest", errors, warnings);
        }

        // Inline bundle validation (if mediaType is ai-catalog+json)
        if (hasInline && entry.MediaType == "application/ai-catalog+json" && entry.Inline is not null)
        {
            ValidateInlineBundle(entry.Inline.Value, prefix, errors, warnings);
        }

        // Warn on unknown extension properties
        if (entry.ExtensionData is not null)
        {
            foreach (var key in entry.ExtensionData.Keys)
            {
                warnings.Add(new(DiagnosticSeverity.Warning,
                    $"unknown property '{key}' on {prefix}", $"{prefix}.{key}"));
            }
        }
    }

    private static void ValidateCollectionReference(CollectionReference collection, string prefix,
        List<ValidationDiagnostic> errors, List<ValidationDiagnostic> warnings)
    {
        var missingFields = new List<string>();

        if (string.IsNullOrEmpty(collection.DisplayName))
            missingFields.Add("displayName");

        if (string.IsNullOrEmpty(collection.Url))
            missingFields.Add("url");

        if (missingFields.Count > 0)
        {
            errors.Add(new(DiagnosticSeverity.Error,
                $"missing required fields on {prefix}: {string.Join(", ", missingFields)}", prefix));
        }
        else
        {
            ValidateHttpsUrl(collection.Url, $"{prefix}.url", errors, warnings);
        }
    }

    private static void ValidatePublisher(Publisher publisher, string prefix,
        List<ValidationDiagnostic> errors, List<ValidationDiagnostic> warnings)
    {
        var missingFields = new List<string>();

        if (string.IsNullOrEmpty(publisher.Identifier))
            missingFields.Add("identifier");

        if (string.IsNullOrEmpty(publisher.DisplayName))
            missingFields.Add("displayName");

        if (missingFields.Count > 0)
        {
            errors.Add(new(DiagnosticSeverity.Error,
                $"missing required fields on publisher: {string.Join(", ", missingFields)}", prefix));
        }
    }

    private static void ValidateTrustManifest(TrustManifest tm, string? entryIdentifier, string prefix,
        List<ValidationDiagnostic> errors, List<ValidationDiagnostic> warnings)
    {
        // identity is required
        if (string.IsNullOrEmpty(tm.Identity))
        {
            errors.Add(new(DiagnosticSeverity.Error,
                "missing required field: identity on trustManifest", $"{prefix}.identity"));
        }
        else if (entryIdentifier is not null && !string.IsNullOrEmpty(entryIdentifier)
                 && tm.Identity != entryIdentifier)
        {
            // TD-4: exact string comparison
            errors.Add(new(DiagnosticSeverity.Error,
                $"trustManifest.identity '{tm.Identity}' does not match entry identifier '{entryIdentifier}'",
                $"{prefix}.identity"));
        }

        // TrustSchema validation
        if (tm.TrustSchema is not null)
        {
            ValidateTrustSchema(tm.TrustSchema, $"{prefix}.trustSchema", errors, warnings);
        }

        // Attestation validation
        if (tm.Attestations is not null)
        {
            for (int i = 0; i < tm.Attestations.Count; i++)
            {
                ValidateAttestation(tm.Attestations[i], $"{prefix}.attestations[{i}]", errors, warnings);
            }
        }

        // Provenance validation
        if (tm.Provenance is not null)
        {
            for (int i = 0; i < tm.Provenance.Count; i++)
            {
                ValidateProvenanceLink(tm.Provenance[i], $"{prefix}.provenance[{i}]", errors, warnings);
            }
        }
    }

    private static void ValidateTrustSchema(TrustSchema schema, string prefix,
        List<ValidationDiagnostic> errors, List<ValidationDiagnostic> warnings)
    {
        var missingFields = new List<string>();

        if (string.IsNullOrEmpty(schema.Identifier))
            missingFields.Add("identifier");

        if (string.IsNullOrEmpty(schema.Version))
            missingFields.Add("version");

        if (missingFields.Count > 0)
        {
            errors.Add(new(DiagnosticSeverity.Error,
                $"missing required fields on trustSchema: {string.Join(", ", missingFields)}", prefix));
        }
    }

    private static void ValidateAttestation(Attestation attestation, string prefix,
        List<ValidationDiagnostic> errors, List<ValidationDiagnostic> warnings)
    {
        var missingFields = new List<string>();

        if (string.IsNullOrEmpty(attestation.Type))
            missingFields.Add("type");

        if (string.IsNullOrEmpty(attestation.Uri))
            missingFields.Add("uri");

        if (string.IsNullOrEmpty(attestation.MediaType))
            missingFields.Add("mediaType");

        if (missingFields.Count > 0)
        {
            errors.Add(new(DiagnosticSeverity.Error,
                $"missing required fields on attestation: {string.Join(", ", missingFields)}", prefix));
        }

        // Size must be non-negative
        if (attestation.Size is not null && attestation.Size < 0)
        {
            errors.Add(new(DiagnosticSeverity.Error,
                $"attestation size must be a non-negative integer, got {attestation.Size}", $"{prefix}.size"));
        }

        // Weak digest rejection (TD-5)
        if (attestation.Digest is not null)
        {
            ValidateDigest(attestation.Digest, $"{prefix}.digest", errors, warnings);
        }
    }

    private static void ValidateProvenanceLink(ProvenanceLink link, string prefix,
        List<ValidationDiagnostic> errors, List<ValidationDiagnostic> warnings)
    {
        var missingFields = new List<string>();

        if (string.IsNullOrEmpty(link.Relation))
            missingFields.Add("relation");

        if (string.IsNullOrEmpty(link.SourceId))
            missingFields.Add("sourceId");

        if (missingFields.Count > 0)
        {
            errors.Add(new(DiagnosticSeverity.Error,
                $"missing required fields on {prefix}: {string.Join(", ", missingFields)}", prefix));
        }

        // Weak digest rejection on sourceDigest
        if (link.SourceDigest is not null)
        {
            ValidateDigest(link.SourceDigest, $"{prefix}.sourceDigest", errors, warnings);
        }
    }

    private static void ValidateIdentifierUniqueness(List<CatalogEntry> entries, List<ValidationDiagnostic> errors)
    {
        var seen = new HashSet<string>();

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (string.IsNullOrEmpty(entry.Identifier))
                continue;

            // Key is (identifier, version) — version may be null
            var key = entry.Version is not null
                ? $"{entry.Identifier}\0{entry.Version}"
                : $"{entry.Identifier}\0";

            if (!seen.Add(key))
            {
                if (entry.Version is not null)
                {
                    errors.Add(new(DiagnosticSeverity.Error,
                        $"duplicate (identifier, version) pair: ('{entry.Identifier}', '{entry.Version}')",
                        $"entries[{i}]"));
                }
                else
                {
                    errors.Add(new(DiagnosticSeverity.Error,
                        $"duplicate identifier '{entry.Identifier}' without version differentiation",
                        $"entries[{i}]"));
                }
            }
        }
    }

    private static void ValidateDigest(string digest, string prefix,
        List<ValidationDiagnostic> errors, List<ValidationDiagnostic> warnings)
    {
        var colonIdx = digest.IndexOf(':');
        if (colonIdx <= 0)
        {
            warnings.Add(new(DiagnosticSeverity.Warning,
                $"digest '{digest}' does not follow algorithm:hex format", prefix));
            return;
        }

        var algorithm = digest[..colonIdx];
        if (s_weakDigestAlgorithms.Contains(algorithm))
        {
            errors.Add(new(DiagnosticSeverity.Error,
                $"digest algorithm '{algorithm}' is not accepted; minimum is SHA-256", prefix));
        }
    }

    private static void ValidateRfc3339DateTime(string value, string prefix,
        List<ValidationDiagnostic> errors, List<ValidationDiagnostic> warnings)
    {
        // Must be a full date-time, not just a date
        if (DateOnlyPattern().IsMatch(value))
        {
            errors.Add(new(DiagnosticSeverity.Error,
                "updatedAt must be a full RFC 3339 datetime, got date-only value", prefix));
            return;
        }

        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
        {
            errors.Add(new(DiagnosticSeverity.Error,
                $"updatedAt is not a valid RFC 3339 datetime: '{value}'", prefix));
        }
    }

    private static void ValidateHttpsUrl(string url, string prefix,
        List<ValidationDiagnostic> errors, List<ValidationDiagnostic> warnings)
    {
        // data: URIs are allowed
        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return;

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new(DiagnosticSeverity.Error,
                "url uses HTTP; MUST be HTTPS per security requirements", prefix));
        }
    }

    private static void ValidateInlineBundle(JsonElement inline, string prefix,
        List<ValidationDiagnostic> errors, List<ValidationDiagnostic> warnings)
    {
        if (inline.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new(DiagnosticSeverity.Error,
                $"inline catalog for {prefix} must be a JSON object", prefix));
            return;
        }

        if (!inline.TryGetProperty("specVersion", out _))
        {
            errors.Add(new(DiagnosticSeverity.Error,
                $"inline catalog for {prefix} is not a valid AI Catalog: missing required field specVersion",
                $"{prefix}.inline"));
            return;
        }

        if (!inline.TryGetProperty("entries", out var entriesEl) || entriesEl.ValueKind != JsonValueKind.Array)
        {
            errors.Add(new(DiagnosticSeverity.Error,
                $"inline catalog for {prefix} is not a valid AI Catalog: missing required field entries",
                $"{prefix}.inline"));
        }
    }

    private static void ValidateDiscoverable(Models.AiCatalog catalog,
        List<ValidationDiagnostic> errors, List<ValidationDiagnostic> warnings)
    {
        if (catalog.Host is null)
        {
            errors.Add(new(DiagnosticSeverity.Error,
                "Discoverable level requires host", "host"));
            return;
        }

        if (string.IsNullOrEmpty(catalog.Host.DisplayName))
        {
            errors.Add(new(DiagnosticSeverity.Error,
                "missing required field: displayName on host", "host.displayName"));
        }
    }

    private static void ValidateTrusted(Models.AiCatalog catalog,
        List<ValidationDiagnostic> errors, List<ValidationDiagnostic> warnings)
    {
        // At least one entry or host must have a trust manifest
        bool anyTrust = false;

        if (catalog.Host?.TrustManifest is not null)
        {
            anyTrust = true;
        }

        foreach (var entry in catalog.Entries)
        {
            if (entry.TrustManifest is not null)
            {
                anyTrust = true;
                break;
            }
        }

        if (!anyTrust)
        {
            errors.Add(new(DiagnosticSeverity.Error,
                "Trusted level requires at least one trustManifest on host or entries", "trustManifest"));
        }
    }

    [GeneratedRegex(@"^\d+\.\d+$")]
    private static partial Regex MajorMinorPattern();

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}$")]
    private static partial Regex DateOnlyPattern();
}
