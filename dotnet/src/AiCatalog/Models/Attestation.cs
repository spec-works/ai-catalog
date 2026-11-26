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

    /// <summary>Optional digest of the attestation document (e.g., "sha256:abc...").</summary>
    [JsonPropertyName("digest")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Digest
    {
        get => _digest;
        set
        {
            ExplicitNullMemberTracker.Clear(this, "digest");
            _digest = value;
        }
    }
    private string? _digest;

    /// <summary>Optional size in bytes of the attestation document (must be non-negative).</summary>
    [JsonPropertyName("size")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Size
    {
        get => _size;
        set
        {
            ExplicitNullMemberTracker.Clear(this, "size");
            _size = value;
        }
    }
    private long? _size;

    /// <summary>Optional human-readable description.</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description
    {
        get => _description;
        set
        {
            ExplicitNullMemberTracker.Clear(this, "description");
            _description = value;
        }
    }
    private string? _description;

    /// <summary>Compatibility property; it is not a v0.9 wire member.</summary>
    [JsonIgnore]
    public string MediaType { get; set; } = string.Empty;

    /// <summary>Unknown properties at the object level, preserved for round-trip fidelity.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
