using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecWorks.AiCatalog.Models;

/// <summary>
/// Host information describing the catalog operator.
/// </summary>
public sealed class HostInfo
{
    /// <summary>The human-readable display name (required).</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Optional identifier for the host.</summary>
    [JsonPropertyName("identifier")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Identifier { get; set; }

    /// <summary>Optional URL to host documentation.</summary>
    [JsonPropertyName("documentationUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DocumentationUrl { get; set; }

    /// <summary>Optional URL or data URI for the host logo.</summary>
    [JsonPropertyName("logoUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LogoUrl { get; set; }

    /// <summary>Optional trust manifest for the host.</summary>
    [JsonPropertyName("trustManifest")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TrustManifest? TrustManifest { get; set; }

    /// <summary>Unknown properties at the object level, preserved for round-trip fidelity.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
