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
public static class AiCatalogParser
{
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

        // Validate specVersion is present and correct type
        ValidateSpecVersion(root);

        // Validate entries is present and correct type
        ValidateEntriesType(root);

        // Validate host displayName type if host is present
        ValidateHostDisplayNameType(root);

        // Validate entry fields types
        ValidateEntryFieldTypes(root);

        // Deserialize using System.Text.Json
        var rawJson = root.GetRawText();
        try
        {
            var catalog = JsonSerializer.Deserialize<Models.AiCatalog>(rawJson, s_options)
                ?? throw new AiCatalogParseException("deserialization returned null");
            return catalog;
        }
        catch (JsonException ex)
        {
            throw new AiCatalogParseException($"failed to deserialize catalog: {ex.Message}", ex);
        }
    }

    private static void ValidateSpecVersion(JsonElement root)
    {
        if (!root.TryGetProperty("specVersion", out var specVersionElement))
        {
            throw new AiCatalogParseException("missing required field: specVersion");
        }

        if (specVersionElement.ValueKind == JsonValueKind.Null)
        {
            throw new AiCatalogParseException("specVersion must be a non-null string");
        }

        if (specVersionElement.ValueKind != JsonValueKind.String)
        {
            throw new AiCatalogParseException(
                $"specVersion must be a string, got {JsonValueKindName(specVersionElement.ValueKind)}");
        }

        var specVersion = specVersionElement.GetString();
        if (string.IsNullOrEmpty(specVersion))
        {
            throw new AiCatalogParseException("specVersion must not be empty");
        }
    }

    private static void ValidateEntriesType(JsonElement root)
    {
        if (!root.TryGetProperty("entries", out var entriesElement))
        {
            throw new AiCatalogParseException("missing required field: entries");
        }

        if (entriesElement.ValueKind != JsonValueKind.Array)
        {
            throw new AiCatalogParseException(
                $"entries must be an array, got {JsonValueKindName(entriesElement.ValueKind)}");
        }
    }

    private static void ValidateHostDisplayNameType(JsonElement root)
    {
        if (!root.TryGetProperty("host", out var hostElement) || hostElement.ValueKind != JsonValueKind.Object)
            return;

        if (hostElement.TryGetProperty("displayName", out var dnElement) && dnElement.ValueKind != JsonValueKind.String)
        {
            throw new AiCatalogParseException(
                $"host displayName must be a string, got {JsonValueKindName(dnElement.ValueKind)}");
        }
    }

    private static void ValidateEntryFieldTypes(JsonElement root)
    {
        if (!root.TryGetProperty("entries", out var entriesElement) || entriesElement.ValueKind != JsonValueKind.Array)
            return;

        int idx = 0;
        foreach (var entry in entriesElement.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                idx++;
                continue;
            }

            // Validate updatedAt type
            if (entry.TryGetProperty("updatedAt", out var updatedAt) && updatedAt.ValueKind != JsonValueKind.String)
            {
                throw new AiCatalogParseException(
                    $"updatedAt must be a string (RFC 3339 datetime), got {JsonValueKindName(updatedAt.ValueKind)}");
            }

            // Validate tags type
            if (entry.TryGetProperty("tags", out var tags))
            {
                if (tags.ValueKind != JsonValueKind.Array)
                {
                    throw new AiCatalogParseException(
                        $"tags must be an array of strings, got {JsonValueKindName(tags.ValueKind)}");
                }

                int tagIdx = 0;
                foreach (var tag in tags.EnumerateArray())
                {
                    if (tag.ValueKind != JsonValueKind.String)
                    {
                        throw new AiCatalogParseException(
                            $"tags must be an array of strings, found non-string element at index {tagIdx}");
                    }
                    tagIdx++;
                }
            }

            idx++;
        }
    }

    private static string JsonValueKindName(JsonValueKind kind) => kind switch
    {
        JsonValueKind.Object => "object",
        JsonValueKind.Array => "array",
        JsonValueKind.String => "string",
        JsonValueKind.Number => "number",
        JsonValueKind.True => "boolean",
        JsonValueKind.False => "boolean",
        JsonValueKind.Null => "null",
        JsonValueKind.Undefined => "undefined",
        _ => kind.ToString().ToLowerInvariant()
    };
}
