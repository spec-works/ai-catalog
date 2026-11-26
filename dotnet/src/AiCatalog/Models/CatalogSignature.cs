using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecWorks.AiCatalog.Models;

/// <summary>A signature record. This model does not verify its cryptographic content.</summary>
public sealed class CatalogSignature
{
    /// <summary>The signer identifier.</summary>
    [JsonPropertyName("signer")]
    public string? Signer { get; set; }

    /// <summary>The signature profile.</summary>
    [JsonPropertyName("profile")]
    public string? Profile { get; set; }

    /// <summary>The RFC 3339 issue timestamp.</summary>
    [JsonPropertyName("issuedAt")]
    public string? IssuedAt { get; set; }

    /// <summary>The optional RFC 3339 expiration timestamp.</summary>
    [JsonPropertyName("expiresAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExpiresAt
    {
        get => _expiresAt;
        set
        {
            ExplicitNullMemberTracker.Clear(this, "expiresAt");
            _expiresAt = value;
        }
    }
    private string? _expiresAt;

    /// <summary>The JSON Web Signature value.</summary>
    [JsonPropertyName("jws")]
    public string? Jws { get; set; }

    /// <summary>Unknown members preserved for round-trip fidelity.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
