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
    /// <summary>Recommended maximum nesting depth for nested catalog entries (NC-3, SC-3).</summary>
    internal const int DefaultMaxNestingDepth = 4;

    private static readonly HashSet<string> s_weakDigestAlgorithms = new(StringComparer.OrdinalIgnoreCase)
    {
        "md5", "sha1"
    };

    /// <summary>
    /// Validates a catalog and auto-detects the highest conformance level.
    /// </summary>
    public static ValidationResult Validate(Models.AiCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var errors = new List<ValidationDiagnostic>();
        var warnings = new List<ValidationDiagnostic>();

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
    public static ValidationResult Validate(Models.AiCatalog catalog, ConformanceLevel level)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var errors = new List<ValidationDiagnostic>();
        var warnings = new List<ValidationDiagnostic>();

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
            // VH-1: Must be Major.Minor format
            if (!MajorMinorPattern().IsMatch(catalog.SpecVersion))
            {
                var dotParts = catalog.SpecVersion.Split('.');
                if (dotParts.Length == 2)
                {
                    errors.Add(new(DiagnosticSeverity.Error,
                        $"specVersion major and minor components must be non-negative integers, found '{catalog.SpecVersion}'", "specVersion"));
                }
                else
                {
                    errors.Add(new(DiagnosticSeverity.Error,
                        $"specVersion must be in Major.Minor format (e.g., '1.0'), found '{catalog.SpecVersion}'", "specVersion"));
                }
            }
        }

        // entries is required
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

        // ME-2: Validate metadata keys (reject empty string keys)
        ValidateMetadataKeys(catalog.Metadata, "metadata", errors);

        // VH-2: Unknown fields within same major version are silently ignored (no warnings)
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

        // url/data exclusivity (CE-5: Entry MUST contain exactly one of url or data)
        bool hasUrl = entry.Url is not null;
        bool hasData = entry.Data is not null && entry.Data.Value.ValueKind != JsonValueKind.Undefined;

        if (hasUrl && hasData)
        {
            errors.Add(new(DiagnosticSeverity.Error,
                $"{prefix} must have exactly one of 'url' or 'data', found both", prefix));
        }
        else if (!hasUrl && !hasData)
        {
            errors.Add(new(DiagnosticSeverity.Error,
                $"{prefix} must have exactly one of 'url' or 'data'", prefix));
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

        // Nested catalog entry validation (if mediaType is ai-catalog+json)
        if (hasData && entry.MediaType == "application/ai-catalog+json" && entry.Data is not null)
        {
            ValidateNestedCatalogEntry(entry.Data.Value, prefix, errors, warnings, 1);
        }

        // ME-2: Validate metadata keys on entry
        ValidateMetadataKeys(entry.Metadata, $"{prefix}.metadata", errors);
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
        if (string.IsNullOrEmpty(tm.Identity))
        {
            errors.Add(new(DiagnosticSeverity.Error,
                "missing required field: identity on trustManifest", $"{prefix}.identity"));
        }
        else if (entryIdentifier is not null && !string.IsNullOrEmpty(entryIdentifier)
                 && tm.Identity != entryIdentifier)
        {
            errors.Add(new(DiagnosticSeverity.Error,
                $"trustManifest.identity '{tm.Identity}' does not match entry identifier '{entryIdentifier}'",
                $"{prefix}.identity"));
        }

        if (tm.TrustSchema is not null)
        {
            ValidateTrustSchema(tm.TrustSchema, $"{prefix}.trustSchema", errors, warnings);
        }

        if (tm.Attestations is not null)
        {
            for (int i = 0; i < tm.Attestations.Count; i++)
            {
                ValidateAttestation(tm.Attestations[i], $"{prefix}.attestations[{i}]", errors, warnings);
            }
        }

        if (tm.Provenance is not null)
        {
            for (int i = 0; i < tm.Provenance.Count; i++)
            {
                ValidateProvenanceLink(tm.Provenance[i], $"{prefix}.provenance[{i}]", errors, warnings);
            }
        }

        // ME-2: Validate metadata keys on trust manifest
        ValidateMetadataKeys(tm.Metadata, $"{prefix}.metadata", errors);
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

        if (attestation.Size is not null && attestation.Size < 0)
        {
            errors.Add(new(DiagnosticSeverity.Error,
                $"attestation size must be a non-negative integer, got {attestation.Size}", $"{prefix}.size"));
        }

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

    private static void ValidateMetadataKeys(JsonElement? metadata, string prefix, List<ValidationDiagnostic> errors)
    {
        if (metadata is null || metadata.Value.ValueKind != JsonValueKind.Object)
            return;

        foreach (var prop in metadata.Value.EnumerateObject())
        {
            if (string.IsNullOrEmpty(prop.Name))
            {
                errors.Add(new(DiagnosticSeverity.Error,
                    "metadata key must be a non-empty string", $"{prefix}"));
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
        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return;

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new(DiagnosticSeverity.Error,
                "url uses HTTP; MUST be HTTPS per security requirements", prefix));
        }
    }

    /// <summary>
    /// Validates a nested catalog entry (formerly called "bundle").
    /// Checks structure and enforces nesting depth limit (NC-3: max depth 4).
    /// </summary>
    private static void ValidateNestedCatalogEntry(JsonElement data, string prefix,
        List<ValidationDiagnostic> errors, List<ValidationDiagnostic> warnings, int depth)
    {
        if (depth >= DefaultMaxNestingDepth)
        {
            warnings.Add(new(DiagnosticSeverity.Warning,
                $"nested catalog depth exceeds recommended limit of {DefaultMaxNestingDepth}",
                prefix));
            return;
        }

        if (data.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new(DiagnosticSeverity.Error,
                $"nested catalog entry for {prefix} must be a JSON object", prefix));
            return;
        }

        if (!data.TryGetProperty("specVersion", out _))
        {
            errors.Add(new(DiagnosticSeverity.Error,
                $"nested catalog entry for {prefix} is not a valid AI Catalog: missing required field specVersion",
                $"{prefix}.data"));
            return;
        }

        if (!data.TryGetProperty("entries", out var entriesEl) || entriesEl.ValueKind != JsonValueKind.Array)
        {
            errors.Add(new(DiagnosticSeverity.Error,
                $"nested catalog entry for {prefix} is not a valid AI Catalog: missing required field entries",
                $"{prefix}.data"));
            return;
        }

        // Recursively check nested entries for further nesting
        foreach (var nestedEntry in entriesEl.EnumerateArray())
        {
            if (nestedEntry.ValueKind != JsonValueKind.Object)
                continue;

            if (nestedEntry.TryGetProperty("mediaType", out var mt)
                && mt.GetString() == "application/ai-catalog+json"
                && nestedEntry.TryGetProperty("data", out var nestedData))
            {
                ValidateNestedCatalogEntry(nestedData, prefix, errors, warnings, depth + 1);
            }
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
