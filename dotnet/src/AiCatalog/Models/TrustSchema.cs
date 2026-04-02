using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecWorks.AiCatalog.Models;

/// <summary>
/// Trust schema defining the trust framework and verification methods.
/// </summary>
public sealed class TrustSchema
{
    /// <summary>The trust schema identifier (required).</summary>
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    /// <summary>The trust schema version (required).</summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>Optional URI to the governance document.</summary>
    [JsonPropertyName("governanceUri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GovernanceUri { get; set; }

    /// <summary>Optional list of verification methods (e.g., "did", "x509", "dns-01").</summary>
    [JsonPropertyName("verificationMethods")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? VerificationMethods { get; set; }

    /// <summary>Unknown properties at the object level, preserved for round-trip fidelity.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
