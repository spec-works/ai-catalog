using System.Globalization;
using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using SpecWorks.AiCatalog.Models;
using SpecWorks.AiCatalog.Parsing;

namespace SpecWorks.AiCatalog.Validation;

/// <summary>Validates the structural requirements of AI Catalog v0.9.</summary>
public static partial class AiCatalogValidator
{
    /// <summary>Validates structure and reports no more than Discoverable conformance.</summary>
    public static ValidationResult Validate(Models.AiCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var errors = new List<ValidationDiagnostic>();
        var warnings = new List<ValidationDiagnostic>();
        ValidateStructure(catalog, errors);

        var level = ConformanceLevel.Minimal;
        if (errors.Count == 0 && catalog.Host is not null &&
            !string.IsNullOrEmpty(catalog.Host.DisplayName))
        {
            level = ConformanceLevel.Discoverable;
        }

        if (HasTrustMaterial(catalog))
        {
            warnings.Add(new(DiagnosticSeverity.Warning,
                "Structural validation cannot establish Trusted conformance; cryptographic verification is required.",
                "trustManifests"));
        }

        return new ValidationResult
        {
            ConformanceLevel = level,
            Errors = errors,
            Warnings = warnings
        };
    }

    /// <summary>
    /// Validates a requested conformance level. Trusted is not granted because this
    /// validator does not perform signature or trust-chain verification.
    /// </summary>
    public static ValidationResult Validate(Models.AiCatalog catalog, ConformanceLevel level)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (level is < ConformanceLevel.Minimal or > ConformanceLevel.Trusted)
            throw new ArgumentOutOfRangeException(nameof(level));

        var errors = new List<ValidationDiagnostic>();
        var warnings = new List<ValidationDiagnostic>();
        ValidateStructure(catalog, errors);

        var structuralErrors = errors.Count;
        if (level >= ConformanceLevel.Discoverable)
            ValidateDiscoverable(catalog, errors);

        var discoverablePassed = errors.Count == structuralErrors;
        var resultLevel = level == ConformanceLevel.Minimal || structuralErrors > 0 || !discoverablePassed
            ? ConformanceLevel.Minimal
            : ConformanceLevel.Discoverable;

        if (level == ConformanceLevel.Trusted)
        {
            errors.Add(new(DiagnosticSeverity.Error,
                "Trusted conformance requires cryptographic verification, which this validator does not perform.",
                "trustManifests"));
        }

        if (HasTrustMaterial(catalog))
        {
            warnings.Add(new(DiagnosticSeverity.Warning,
                "Structural validation cannot establish trust; cryptographic verification is required.",
                "trustManifests"));
        }

        return new ValidationResult
        {
            ConformanceLevel = resultLevel,
            Errors = errors,
            Warnings = warnings
        };
    }

    private static void ValidateStructure(Models.AiCatalog catalog, List<ValidationDiagnostic> errors)
    {
        foreach (var (path, trackedMember) in catalog.ExplicitNullTypedMemberParents)
        {
            if (TryGetCurrentMember(catalog, path, out var currentParent, out var currentValue) &&
                ReferenceEquals(trackedMember.Parent, currentParent) && currentValue is null &&
                ExplicitNullMemberTracker.IsMarked(currentParent!, trackedMember.MemberName))
                errors.Add(new(DiagnosticSeverity.Error, "member must be omitted instead of set to null", path));
        }

        if (string.IsNullOrEmpty(catalog.SpecVersion))
            errors.Add(new(DiagnosticSeverity.Error, "missing required field: specVersion", "specVersion"));
        else if (!MajorMinorPattern().IsMatch(catalog.SpecVersion))
            errors.Add(new(DiagnosticSeverity.Error,
                $"specVersion must be in Major.Minor format (e.g., '0.9'), found '{catalog.SpecVersion}'",
                "specVersion"));
        else
        {
            var versionParts = catalog.SpecVersion.Split('.');
            var majorIsValid = int.TryParse(versionParts[0], NumberStyles.None,
                CultureInfo.InvariantCulture, out var major);
            var minorIsValid = int.TryParse(versionParts[1], NumberStyles.None,
                CultureInfo.InvariantCulture, out _);
            if (!majorIsValid || !minorIsValid)
                errors.Add(new(DiagnosticSeverity.Error,
                    $"specVersion major and minor components must be non-negative integers, found '{catalog.SpecVersion}'",
                    "specVersion"));
            else if (major > AiCatalogParser.SupportedMajorVersion)
                errors.Add(new(DiagnosticSeverity.Error,
                    $"specVersion '{catalog.SpecVersion}' has unsupported major version {major}; this library supports major version {AiCatalogParser.SupportedMajorVersion}",
                    "specVersion"));
        }

        if (!catalog.HasEntriesMember)
            errors.Add(new(DiagnosticSeverity.Error, "missing required field: entries", "entries"));

        if (catalog.Entries is null)
        {
            errors.Add(new(DiagnosticSeverity.Error, "missing required field: entries", "entries"));
            return;
        }

        if (catalog.Host is not null)
        {
            if (string.IsNullOrEmpty(catalog.Host.DisplayName))
                errors.Add(new(DiagnosticSeverity.Error, "missing required field: displayName on host", "host.displayName"));
        }

        for (var i = 0; i < catalog.Entries.Count; i++)
            ValidateEntry(catalog.Entries[i], $"entries[{i}]", errors);

        ValidateIdentifierUniqueness(catalog.Entries, errors);
        ValidateExtensions(catalog.Extensions, "extensions", errors);
        if (catalog.Signature is not null)
            ValidateSignature(catalog.Signature, "signature", errors);
    }

    private static void ValidateEntry(CatalogEntry entry, string path, List<ValidationDiagnostic> errors)
    {
        if (entry is null)
        {
            errors.Add(new(DiagnosticSeverity.Error, "entry must be an object", path));
            return;
        }

        if (string.IsNullOrEmpty(entry.Identifier))
            errors.Add(new(DiagnosticSeverity.Error, "missing required field: identifier", $"{path}.identifier"));
        if (string.IsNullOrEmpty(entry.Type))
            errors.Add(new(DiagnosticSeverity.Error, "missing required field: type", $"{path}.type"));
        if (entry.Digest is not null)
            ValidateDigest(entry.Digest, $"{path}.digest", errors);

        var hasUrl = entry.HasUrlMember || entry.Url is not null;
        var hasData = entry.Data is { } data && data.ValueKind != JsonValueKind.Undefined;
        if (hasUrl == hasData)
            errors.Add(new(DiagnosticSeverity.Error,
                $"entry must have exactly one of 'url' or 'data'{(hasUrl ? ", found both" : string.Empty)}",
                path));
        if (entry.HasUrlMember && entry.Url is null)
            errors.Add(new(DiagnosticSeverity.Error, "url must be a string when present", $"{path}.url"));

        if (entry.UpdatedAt is not null)
            ValidateTimestamp(entry.UpdatedAt, $"{path}.updatedAt", errors);

        if (entry.Publisher is not null)
        {
            if (string.IsNullOrEmpty(entry.Publisher.Identifier))
                errors.Add(new(DiagnosticSeverity.Error, "missing required field: identifier on publisher", $"{path}.publisher.identifier"));
            if (string.IsNullOrEmpty(entry.Publisher.DisplayName))
                errors.Add(new(DiagnosticSeverity.Error, "missing required field: displayName on publisher", $"{path}.publisher.displayName"));
        }

        ValidateExtensions(entry.Extensions, $"{path}.extensions", errors);
        if (entry.TrustManifests is not null)
        {
            for (var i = 0; i < entry.TrustManifests.Count; i++)
                ValidateTrustManifest(entry.TrustManifests[i], entry, $"{path}.trustManifests[{i}]", errors);
        }
    }

    private static void ValidateTrustManifest(TrustManifest manifest, CatalogEntry entry, string path,
        List<ValidationDiagnostic> errors)
    {
        if (manifest is null)
        {
            errors.Add(new(DiagnosticSeverity.Error, "trust manifest must be an object", path));
            return;
        }

        if (string.IsNullOrEmpty(manifest.Contributor))
            errors.Add(new(DiagnosticSeverity.Error, "missing required field: contributor", $"{path}.contributor"));
        else if (!IsAbsoluteUri(manifest.Contributor))
            errors.Add(new(DiagnosticSeverity.Error, "contributor must be an absolute URI", $"{path}.contributor"));

        var hasAttestations = manifest.Attestations is { Count: > 0 };
        var hasProvenance = manifest.Provenance is { Count: > 0 };
        var hasExtensions = manifest.Extensions is { Count: > 0 };
        if (manifest.Signature is null && manifest.TrustSchema is null &&
            !hasAttestations && !hasProvenance && !hasExtensions)
        {
            errors.Add(new(DiagnosticSeverity.Error,
                "trust manifest must contain at least one signature, trustSchema, non-empty attestations, non-empty provenance, or non-empty extensions",
                path));
        }

        if (manifest.Signature is not null)
        {
            if (manifest.Subject is null)
                errors.Add(new(DiagnosticSeverity.Error, "signature requires subject", $"{path}.subject"));
            else
                ValidateSubject(manifest.Subject, entry, path, errors);

            ValidateSignature(manifest.Signature, $"{path}.signature", errors);
            if (!string.IsNullOrEmpty(manifest.Contributor) &&
                !string.Equals(manifest.Signature.Signer, manifest.Contributor, StringComparison.Ordinal))
                errors.Add(new(DiagnosticSeverity.Error,
                    "signature.signer must exactly equal contributor", $"{path}.signature.signer"));
            if (string.IsNullOrEmpty(entry.Digest))
                errors.Add(new(DiagnosticSeverity.Error, "signed manifest requires entry.digest", $"{path}.entry.digest"));
        }
        else if (manifest.Subject is not null)
        {
            ValidateSubject(manifest.Subject, entry, path, errors);
        }

        if (manifest.TrustSchema is not null)
        {
            if (string.IsNullOrEmpty(manifest.TrustSchema.Identifier))
                errors.Add(new(DiagnosticSeverity.Error, "missing required field: identifier", $"{path}.trustSchema.identifier"));
            if (string.IsNullOrEmpty(manifest.TrustSchema.Version))
                errors.Add(new(DiagnosticSeverity.Error, "missing required field: version", $"{path}.trustSchema.version"));
        }

        if (manifest.Attestations is not null)
        {
            for (var i = 0; i < manifest.Attestations.Count; i++)
            {
                var attestation = manifest.Attestations[i];
                var attPath = $"{path}.attestations[{i}]";
                if (attestation is null)
                {
                    errors.Add(new(DiagnosticSeverity.Error, "attestation must be an object", attPath));
                    continue;
                }
                if (string.IsNullOrEmpty(attestation.Type))
                    errors.Add(new(DiagnosticSeverity.Error, "missing required field: type", $"{attPath}.type"));
                if (string.IsNullOrEmpty(attestation.Uri))
                    errors.Add(new(DiagnosticSeverity.Error, "missing required field: uri", $"{attPath}.uri"));
                if (attestation.Digest is not null)
                    ValidateDigest(attestation.Digest, $"{attPath}.digest", errors);
                if (attestation.Size is < 0)
                    errors.Add(new(DiagnosticSeverity.Error, "size must be an unsigned integer", $"{attPath}.size"));
                if (attestation.Size is < 0)
                    errors.Add(new(DiagnosticSeverity.Error, "size must be an unsigned integer", $"{attPath}.size"));
            }
        }

        if (manifest.Provenance is not null)
        {
            for (var i = 0; i < manifest.Provenance.Count; i++)
            {
                var provenance = manifest.Provenance[i];
                var provPath = $"{path}.provenance[{i}]";
                if (provenance is null)
                {
                    errors.Add(new(DiagnosticSeverity.Error, "provenance item must be an object", provPath));
                    continue;
                }
                if (string.IsNullOrEmpty(provenance.Relation))
                    errors.Add(new(DiagnosticSeverity.Error, "missing required field: relation", $"{provPath}.relation"));
                if (string.IsNullOrEmpty(provenance.SourceId))
                    errors.Add(new(DiagnosticSeverity.Error, "missing required field: sourceId", $"{provPath}.sourceId"));
                if (provenance.SourceDigest is not null)
                    ValidateDigest(provenance.SourceDigest, $"{provPath}.sourceDigest", errors);
            }
        }

        ValidateExtensions(manifest.Extensions, $"{path}.extensions", errors);
    }

    private static void ValidateSubject(Subject subject, CatalogEntry entry, string path,
        List<ValidationDiagnostic> errors)
    {
        if (string.IsNullOrEmpty(subject.Identifier))
            errors.Add(new(DiagnosticSeverity.Error, "missing required field: identifier", $"{path}.subject.identifier"));
        else if (!string.Equals(subject.Identifier, entry.Identifier, StringComparison.Ordinal))
            errors.Add(new(DiagnosticSeverity.Error, "subject.identifier does not match entry.identifier", $"{path}.subject.identifier"));

        if (string.IsNullOrEmpty(subject.Type))
            errors.Add(new(DiagnosticSeverity.Error, "missing required field: type", $"{path}.subject.type"));
        else if (!string.Equals(subject.Type, entry.Type, StringComparison.Ordinal))
            errors.Add(new(DiagnosticSeverity.Error, "subject.type does not match entry.type", $"{path}.subject.type"));

        if (string.IsNullOrEmpty(subject.Digest))
            errors.Add(new(DiagnosticSeverity.Error, "missing required field: digest", $"{path}.subject.digest"));
        else
        {
            ValidateDigest(subject.Digest, $"{path}.subject.digest", errors);
            if (!string.Equals(subject.Digest, entry.Digest, StringComparison.Ordinal))
                errors.Add(new(DiagnosticSeverity.Error, "subject.digest does not match entry.digest", $"{path}.subject.digest"));
        }

        if (!string.Equals(subject.Version, entry.Version, StringComparison.Ordinal))
            errors.Add(new(DiagnosticSeverity.Error, "subject.version must match entry.version", $"{path}.subject.version"));
    }

    private static void ValidateSignature(CatalogSignature signature, string path,
        List<ValidationDiagnostic> errors)
    {
        if (string.IsNullOrEmpty(signature.Signer))
            errors.Add(new(DiagnosticSeverity.Error, "missing required field: signer", $"{path}.signer"));
        else if (!IsAbsoluteUri(signature.Signer))
            errors.Add(new(DiagnosticSeverity.Error, "signer must be an absolute URI", $"{path}.signer"));
        if (string.IsNullOrEmpty(signature.Profile))
            errors.Add(new(DiagnosticSeverity.Error, "missing required field: profile", $"{path}.profile"));
        if (string.IsNullOrEmpty(signature.Jws))
            errors.Add(new(DiagnosticSeverity.Error, "missing required field: jws", $"{path}.jws"));
        if (string.IsNullOrEmpty(signature.IssuedAt))
            errors.Add(new(DiagnosticSeverity.Error, "missing required field: issuedAt", $"{path}.issuedAt"));
        else
            ValidateTimestamp(signature.IssuedAt, $"{path}.issuedAt", errors);
        if (signature.ExpiresAt is not null)
        {
            ValidateTimestamp(signature.ExpiresAt, $"{path}.expiresAt", errors);
            if (signature.IssuedAt is { } issuedAtValue &&
                TryParseTimestamp(issuedAtValue, out var issuedAt) &&
                TryParseTimestamp(signature.ExpiresAt, out var expiresAt) &&
                expiresAt <= issuedAt)
                errors.Add(new(DiagnosticSeverity.Error,
                    "expiresAt must denote an instant later than issuedAt", $"{path}.expiresAt"));
        }
    }

    private static void ValidateTimestamp(string value, string path, List<ValidationDiagnostic> errors)
    {
        if (!TryParseTimestamp(value, out _))
        {
            errors.Add(new(DiagnosticSeverity.Error, $"value is not a valid RFC 3339 datetime: '{value}'", path));
        }
    }

    private static bool TryParseTimestamp(string value, out DateTimeOffset timestamp)
    {
        timestamp = default;
        return Rfc3339Pattern().IsMatch(value) &&
               DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out timestamp);
    }

    private static void ValidateDigest(string value, string path, List<ValidationDiagnostic> errors)
    {
        var separator = value.IndexOf(':');
        if (separator <= 0 || separator == value.Length - 1)
        {
            errors.Add(new(DiagnosticSeverity.Error, "digest must use algorithm:lowercase-hex format", path));
            return;
        }

        var algorithm = value[..separator];
        var hex = value[(separator + 1)..];
        if (!DigestAlgorithmPattern().IsMatch(algorithm) ||
            !LowercaseHexPattern().IsMatch(hex) ||
            hex.Length % 2 != 0)
        {
            errors.Add(new(DiagnosticSeverity.Error, "digest must use algorithm:lowercase-hex format", path));
            return;
        }

        var normalizedAlgorithm = algorithm.Replace("-", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
        if (IsKnownWeakDigestAlgorithm(normalizedAlgorithm))
        {
            errors.Add(new(DiagnosticSeverity.Error, "digest algorithms shorter than SHA-256 are not accepted", path));
            return;
        }

        var expectedHexLength = normalizedAlgorithm switch
        {
            "sha256" or "sha3256" => 64,
            "sha384" or "sha3384" => 96,
            "sha512" or "sha3512" => 128,
            _ => 0
        };
        if (expectedHexLength != 0 && hex.Length != expectedHexLength)
            errors.Add(new(DiagnosticSeverity.Error,
                $"digest length does not match algorithm '{algorithm}'", path));
    }

    private static bool IsKnownWeakDigestAlgorithm(string algorithm)
    {
        if (algorithm is "md2" or "md4" or "md5")
            return true;

        var shaPrefixLength = algorithm.StartsWith("sha3", StringComparison.Ordinal) ? 4 :
            algorithm.StartsWith("sha", StringComparison.Ordinal) ? 3 : 0;
        return shaPrefixLength > 0 &&
               int.TryParse(algorithm.AsSpan(shaPrefixLength), NumberStyles.None,
                   CultureInfo.InvariantCulture, out var outputBits) &&
               outputBits < 256;
    }

    private static bool IsAbsoluteUri(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out _);

    private static void ValidateExtensions(Dictionary<string, JsonElement>? extensions, string path,
        List<ValidationDiagnostic> errors)
    {
        if (extensions is null)
            return;

        foreach (var key in extensions.Keys)
        {
            if (string.IsNullOrWhiteSpace(key) || !IsNamespacedExtensionKey(key))
                errors.Add(new(DiagnosticSeverity.Error,
                    $"extension key '{key}' must be namespaced", path));
        }
    }

    private static bool IsNamespacedExtensionKey(string key) =>
        Uri.TryCreate(key, UriKind.Absolute, out _) ||
        (key.Contains('.') && DnsExtensionPattern().IsMatch(key));

    private static void ValidateIdentifierUniqueness(List<CatalogEntry> entries,
        List<ValidationDiagnostic> errors)
    {
        var seen = new HashSet<(string Identifier, string? Version)>();
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (string.IsNullOrEmpty(entry?.Identifier))
                continue;
            var key = (entry.Identifier, entry.Version);
            if (seen.Add(key))
                continue;
            var description = entry.Version is null
                ? $"duplicate identifier '{entry.Identifier}' without version differentiation"
                : $"duplicate (identifier, version) pair: ('{entry.Identifier}', '{entry.Version}')";
            errors.Add(new(DiagnosticSeverity.Error, description, $"entries[{i}]"));
        }
    }

    private static void ValidateDiscoverable(Models.AiCatalog catalog, List<ValidationDiagnostic> errors)
    {
        if (catalog.Host is null)
            errors.Add(new(DiagnosticSeverity.Error, "Discoverable level requires host", "host"));
        else if (string.IsNullOrEmpty(catalog.Host.DisplayName))
            errors.Add(new(DiagnosticSeverity.Error, "missing required field: displayName on host", "host.displayName"));
    }

    private static bool HasTrustMaterial(Models.AiCatalog catalog) =>
        catalog.Signature is not null ||
        catalog.Entries?.Any(entry => entry?.TrustManifests?.Count > 0) == true;

    private static bool TryGetCurrentMember(Models.AiCatalog catalog, string path,
        out object? parent, out object? value)
    {
        parent = null;
        value = null;
        var segments = path.Split('.');
        object current = catalog;

        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            var bracket = segment.IndexOf('[');
            var propertyName = bracket < 0 ? segment : segment[..bracket];
            var property = current.GetType().GetProperties()
                .FirstOrDefault(candidate =>
                    candidate.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == propertyName);
            if (property is null)
                return false;

            var propertyValue = property.GetValue(current);
            if (bracket >= 0)
            {
                var closeBracket = segment.IndexOf(']', bracket + 1);
                if (closeBracket < 0 ||
                    !int.TryParse(segment.AsSpan(bracket + 1, closeBracket - bracket - 1),
                        NumberStyles.None, CultureInfo.InvariantCulture, out var index) ||
                    propertyValue is not IList list || index < 0 || index >= list.Count ||
                    list[index] is not { } item)
                    return false;
                propertyValue = item;
            }

            if (i == segments.Length - 1)
            {
                parent = current;
                value = propertyValue;
                return true;
            }

            if (propertyValue is null)
                return false;
            current = propertyValue;
        }

        return false;
    }

    [GeneratedRegex(@"^\d+\.\d+$")]
    private static partial Regex MajorMinorPattern();

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9-]*$")]
    private static partial Regex DigestAlgorithmPattern();

    [GeneratedRegex(@"^[0-9a-f]+$")]
    private static partial Regex LowercaseHexPattern();

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.IgnoreCase)]
    private static partial Regex Rfc3339Pattern();

    [GeneratedRegex(@"^[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)+$")]
    private static partial Regex DnsExtensionPattern();
}
