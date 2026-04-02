using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecWorks.AiCatalog.Models;

/// <summary>
/// A single catalog entry describing an AI artifact.
/// Must have exactly one of <see cref="Url"/> or <see cref="Inline"/>.
/// </summary>
public sealed class CatalogEntry
{
    /// <summary>The unique identifier for this entry.</summary>
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    /// <summary>The human-readable display name.</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>The IANA media type describing the artifact format.</summary>
    [JsonPropertyName("mediaType")]
    public string MediaType { get; set; } = string.Empty;

    /// <summary>URL to the artifact. Mutually exclusive with <see cref="Inline"/>.</summary>
    [JsonPropertyName("url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; set; }

    /// <summary>Inline artifact content (opaque JSON value). Mutually exclusive with <see cref="Url"/>.</summary>
    [JsonPropertyName("inline")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Inline { get; set; }

    /// <summary>Optional version string for multi-version entries.</summary>
    [JsonPropertyName("version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Version { get; set; }

    /// <summary>Optional human-readable description of the artifact.</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>Optional string tags for categorization.</summary>
    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Tags { get; set; }

    /// <summary>Optional publisher information.</summary>
    [JsonPropertyName("publisher")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Publisher? Publisher { get; set; }

    /// <summary>Optional trust manifest for this entry.</summary>
    [JsonPropertyName("trustManifest")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TrustManifest? TrustManifest { get; set; }

    /// <summary>Optional RFC 3339 datetime when the entry was last updated.</summary>
    [JsonPropertyName("updatedAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UpdatedAt { get; set; }

    /// <summary>Optional open metadata map for extension data.</summary>
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Metadata { get; set; }

    /// <summary>Unknown properties at the object level, preserved for round-trip fidelity.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
