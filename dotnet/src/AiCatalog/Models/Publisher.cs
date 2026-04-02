using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecWorks.AiCatalog.Models;

/// <summary>
/// Publisher information for a catalog entry.
/// </summary>
public sealed class Publisher
{
    /// <summary>The publisher's identifier (required).</summary>
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    /// <summary>The publisher's display name (required).</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Optional identity type (e.g., "did", "urn").</summary>
    [JsonPropertyName("identityType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IdentityType { get; set; }

    /// <summary>Unknown properties at the object level, preserved for round-trip fidelity.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
