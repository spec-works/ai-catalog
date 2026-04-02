using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecWorks.AiCatalog.Models;

/// <summary>
/// Trust manifest containing identity, attestations, provenance, and policy information.
/// </summary>
public sealed class TrustManifest
{
    /// <summary>The identity this trust manifest is about (required). Must match the containing entry's identifier.</summary>
    [JsonPropertyName("identity")]
    public string Identity { get; set; } = string.Empty;

    /// <summary>Optional identity type (e.g., "did", "urn").</summary>
    [JsonPropertyName("identityType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IdentityType { get; set; }

    /// <summary>Optional trust schema defining the trust framework.</summary>
    [JsonPropertyName("trustSchema")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TrustSchema? TrustSchema { get; set; }

    /// <summary>Optional list of attestations (verifiable proofs).</summary>
    [JsonPropertyName("attestations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Attestation>? Attestations { get; set; }

    /// <summary>Optional provenance links (lineage records).</summary>
    [JsonPropertyName("provenance")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ProvenanceLink>? Provenance { get; set; }

    /// <summary>Optional privacy policy URL.</summary>
    [JsonPropertyName("privacyPolicyUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PrivacyPolicyUrl { get; set; }

    /// <summary>Optional terms of service URL.</summary>
    [JsonPropertyName("termsOfServiceUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TermsOfServiceUrl { get; set; }

    /// <summary>Optional cryptographic signature.</summary>
    [JsonPropertyName("signature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Signature { get; set; }

    /// <summary>Optional open metadata map for extension data.</summary>
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Metadata { get; set; }

    /// <summary>Unknown properties at the object level, preserved for round-trip fidelity.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
