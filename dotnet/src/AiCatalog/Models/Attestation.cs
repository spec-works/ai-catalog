using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecWorks.AiCatalog.Models;

/// <summary>
/// An attestation representing a verifiable proof (audit report, compliance cert, etc.).
/// </summary>
public sealed class Attestation
{
    /// <summary>The attestation type (e.g., "SOC2-Type2") (required).</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>URI to the attestation document (required).</summary>
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    /// <summary>Media type of the attestation document (required).</summary>
    [JsonPropertyName("mediaType")]
    public string MediaType { get; set; } = string.Empty;

    /// <summary>Optional digest of the attestation document (e.g., "sha256:abc...").</summary>
    [JsonPropertyName("digest")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Digest { get; set; }

    /// <summary>Optional size in bytes of the attestation document (must be non-negative).</summary>
    [JsonPropertyName("size")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Size { get; set; }

    /// <summary>Optional human-readable description.</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>Unknown properties at the object level, preserved for round-trip fidelity.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
