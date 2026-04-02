using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecWorks.AiCatalog.Models;

/// <summary>
/// A reference to another catalog (collection) for partitioning.
/// </summary>
public sealed class CollectionReference
{
    /// <summary>The human-readable display name (required).</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>The URL to the referenced catalog (required).</summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>Optional description of the collection.</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>Optional string tags for categorization.</summary>
    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Tags { get; set; }

    /// <summary>Unknown properties at the object level, preserved for round-trip fidelity.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
