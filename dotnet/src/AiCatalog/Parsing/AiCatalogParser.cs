using System.Text.Json;
using SpecWorks.AiCatalog.Models;

namespace SpecWorks.AiCatalog.Parsing;

/// <summary>
/// Exception thrown when JSON cannot be parsed into an AI Catalog document.
/// </summary>
public class AiCatalogParseException : AiCatalogException
{
    /// <summary>Initializes a new instance of <see cref="AiCatalogParseException"/>.</summary>
    public AiCatalogParseException(string message) : base(message) { }

    /// <summary>Initializes a new instance with the specified message and inner exception.</summary>
    public AiCatalogParseException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Parses JSON into <see cref="AiCatalog"/> instances.
/// Does NOT validate conformance — use <see cref="Validation.AiCatalogValidator"/> for that.
/// </summary>
public static partial class AiCatalogParser
{
    /// <summary>The major version of the spec this library supports.</summary>
    internal const int SupportedMajorVersion = 0;

    private static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNameCaseInsensitive = false,
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
    };

    /// <summary>
    /// Parses a JSON string into an <see cref="AiCatalog"/>.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>A parsed <see cref="AiCatalog"/> instance.</returns>
    /// <exception cref="AiCatalogParseException">Thrown when the JSON is malformed or has type mismatches.</exception>
    public static Models.AiCatalog Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        JsonDocument? doc = null;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new AiCatalogParseException($"invalid JSON: {ex.Message}", ex);
        }

        using (doc)
        {
            return ParseDocument(doc);
        }
    }

    /// <summary>
    /// Parses a JSON stream into an <see cref="AiCatalog"/>.
    /// </summary>
    /// <param name="stream">The stream containing JSON.</param>
    /// <returns>A parsed <see cref="AiCatalog"/> instance.</returns>
    /// <exception cref="AiCatalogParseException">Thrown when the JSON is malformed or has type mismatches.</exception>
    public static Models.AiCatalog Parse(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        JsonDocument? doc = null;
        try
        {
            doc = JsonDocument.Parse(stream);
        }
        catch (JsonException ex)
        {
            throw new AiCatalogParseException($"invalid JSON: {ex.Message}", ex);
        }

        using (doc)
        {
            return ParseDocument(doc);
        }
    }

    private static Models.AiCatalog ParseDocument(JsonDocument doc)
    {
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new AiCatalogParseException(
                $"root document must be a JSON object, got {root.ValueKind.ToString().ToLowerInvariant()}");
        }

        RejectDuplicateMembers(root, "$");

        try
        {
            var catalog = JsonSerializer.Deserialize<Models.AiCatalog>(root.GetRawText(), s_options)
                ?? throw new AiCatalogParseException("deserialization returned null");
            catalog.HasEntriesMember = root.TryGetProperty("entries", out _);
            CollectExplicitNullTypedMembers(root, catalog);
            if (catalog.Entries is not null &&
                root.TryGetProperty("entries", out var entries) &&
                entries.ValueKind == JsonValueKind.Array)
            {
                var index = 0;
                foreach (var entryElement in entries.EnumerateArray())
                {
                    if (index < catalog.Entries.Count &&
                        catalog.Entries[index] is { } parsedEntry &&
                        entryElement.ValueKind == JsonValueKind.Object)
                        parsedEntry.HasUrlMember = entryElement.TryGetProperty("url", out _);
                    index++;
                }
            }
            return catalog;
        }
        catch (JsonException ex)
        {
            throw new AiCatalogParseException($"failed to deserialize catalog: {ex.Message}", ex);
        }
    }

    private static void RejectDuplicateMembers(JsonElement element, string path)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject())
                {
                    if (!names.Add(property.Name))
                        throw new AiCatalogParseException(
                            $"duplicate JSON member '{property.Name}' at {path}");
                    RejectDuplicateMembers(property.Value, $"{path}.{property.Name}");
                }
                break;
            }
            case JsonValueKind.Array:
            {
                var index = 0;
                foreach (var item in element.EnumerateArray())
                    RejectDuplicateMembers(item, $"{path}[{index++}]");
                break;
            }
        }
    }

    private static void CollectExplicitNullTypedMembers(JsonElement root, Models.AiCatalog catalog)
    {
        RecordNullMembers(root, string.Empty, catalog, catalog,
            "specVersion", "entries", "host", "signature", "extensions");
        if (root.TryGetProperty("host", out var host) && host.ValueKind == JsonValueKind.Object)
            RecordNullMembers(host, "host", catalog, catalog.Host!,
                "displayName", "identifier", "documentationUrl", "logoUrl");
        if (root.TryGetProperty("signature", out var signature) && signature.ValueKind == JsonValueKind.Object)
            RecordNullMembers(signature, "signature", catalog, catalog.Signature!,
                "signer", "profile", "issuedAt", "expiresAt", "jws");

        if (!root.TryGetProperty("entries", out var entries) || entries.ValueKind != JsonValueKind.Array)
            return;

        var index = 0;
        foreach (var entry in entries.EnumerateArray())
        {
            var entryPath = $"entries[{index++}]";
            if (entry.ValueKind != JsonValueKind.Object)
                continue;

            RecordNullMembers(entry, entryPath, catalog, catalog.Entries[index - 1],
                "identifier", "displayName", "type", "digest",
                "version", "description", "tags", "publisher", "trustManifests", "updatedAt", "extensions");
            if (entry.TryGetProperty("publisher", out var publisher) && publisher.ValueKind == JsonValueKind.Object)
                RecordNullMembers(publisher, $"{entryPath}.publisher", catalog, catalog.Entries[index - 1].Publisher!,
                    "identifier", "displayName", "identityType");

            if (!entry.TryGetProperty("trustManifests", out var manifests) ||
                manifests.ValueKind != JsonValueKind.Array)
                continue;

            var manifestIndex = 0;
            foreach (var manifest in manifests.EnumerateArray())
            {
                var manifestPath = $"{entryPath}.trustManifests[{manifestIndex++}]";
                if (manifest.ValueKind != JsonValueKind.Object)
                    continue;

                RecordNullMembers(manifest, manifestPath, catalog,
                    catalog.Entries[index - 1].TrustManifests![manifestIndex - 1], "contributor", "subject", "signature",
                    "trustSchema", "attestations", "provenance", "extensions");
                if (manifest.TryGetProperty("subject", out var subject) && subject.ValueKind == JsonValueKind.Object)
                    RecordNullMembers(subject, $"{manifestPath}.subject", catalog,
                        catalog.Entries[index - 1].TrustManifests![manifestIndex - 1].Subject!,
                        "identifier", "type", "digest", "version");
                if (manifest.TryGetProperty("signature", out var manifestSignature) &&
                    manifestSignature.ValueKind == JsonValueKind.Object)
                    RecordNullMembers(manifestSignature, $"{manifestPath}.signature", catalog,
                        catalog.Entries[index - 1].TrustManifests![manifestIndex - 1].Signature!,
                        "signer", "profile", "issuedAt", "expiresAt", "jws");
                if (manifest.TryGetProperty("trustSchema", out var trustSchema) &&
                    trustSchema.ValueKind == JsonValueKind.Object)
                    RecordNullMembers(trustSchema, $"{manifestPath}.trustSchema", catalog,
                        catalog.Entries[index - 1].TrustManifests![manifestIndex - 1].TrustSchema!,
                        "identifier", "version", "governanceUri", "verificationMethods");

                CollectNullAttestationMembers(manifest, manifestPath, catalog,
                    catalog.Entries[index - 1].TrustManifests![manifestIndex - 1]);
                CollectNullProvenanceMembers(manifest, manifestPath, catalog,
                    catalog.Entries[index - 1].TrustManifests![manifestIndex - 1]);
            }
        }
    }

    private static void CollectNullAttestationMembers(JsonElement manifest, string manifestPath,
        Models.AiCatalog catalog, TrustManifest parsedManifest)
    {
        if (!manifest.TryGetProperty("attestations", out var attestations) ||
            attestations.ValueKind != JsonValueKind.Array)
            return;

        var index = 0;
        foreach (var attestation in attestations.EnumerateArray())
        {
            if (attestation.ValueKind == JsonValueKind.Object)
                RecordNullMembers(attestation, $"{manifestPath}.attestations[{index}]", catalog,
                    parsedManifest.Attestations![index],
                    "type", "uri", "digest", "size", "description");
            index++;
        }
    }

    private static void CollectNullProvenanceMembers(JsonElement manifest, string manifestPath,
        Models.AiCatalog catalog, TrustManifest parsedManifest)
    {
        if (!manifest.TryGetProperty("provenance", out var provenance) ||
            provenance.ValueKind != JsonValueKind.Array)
            return;

        var index = 0;
        foreach (var link in provenance.EnumerateArray())
        {
            if (link.ValueKind == JsonValueKind.Object)
                RecordNullMembers(link, $"{manifestPath}.provenance[{index}]", catalog,
                    parsedManifest.Provenance![index],
                    "relation", "sourceId", "sourceDigest", "registryUri", "statementUri", "signatureRef");
            index++;
        }
    }

    private static void RecordNullMembers(JsonElement obj, string path, Models.AiCatalog catalog, object parsedObject,
        params string[] typedMembers)
    {
        var names = new HashSet<string>(typedMembers, StringComparer.Ordinal);
        foreach (var property in obj.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Null && names.Contains(property.Name))
            {
                var memberPath = path.Length == 0 ? property.Name : $"{path}.{property.Name}";
                ExplicitNullMemberTracker.Mark(parsedObject, property.Name);
                catalog.ExplicitNullTypedMemberParents[memberPath] = (parsedObject, property.Name);
            }
        }
    }
}
