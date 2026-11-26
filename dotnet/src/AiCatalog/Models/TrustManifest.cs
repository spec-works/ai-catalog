using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecWorks.AiCatalog.Models;

/// <summary>
/// Trust manifest containing contributor, subject, signature, and supporting evidence.
/// </summary>
public sealed class TrustManifest
{
    /// <summary>The contributor that supplied the trust manifest.</summary>
    [JsonPropertyName("contributor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Contributor { get; set; }

    /// <summary>The subject of an optional signature.</summary>
    [JsonPropertyName("subject")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Subject? Subject
    {
        get => _subject;
        set
        {
            ExplicitNullMemberTracker.Clear(this, "subject");
            _subject = value;
        }
    }
    private Subject? _subject;

    /// <summary>Optional signature over the subject.</summary>
    [JsonPropertyName("signature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CatalogSignature? Signature
    {
        get => _signature;
        set
        {
            ExplicitNullMemberTracker.Clear(this, "signature");
            _signature = value;
        }
    }
    private CatalogSignature? _signature;

    /// <summary>Additional signatures, whose semantics are intentionally ignored.</summary>
    [JsonPropertyName("additionalSignatures")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(PreservingNullableJsonElementConverter))]
    public JsonElement? AdditionalSignatures { get; set; }

    /// <summary>Optional trust schema defining the trust framework.</summary>
    [JsonPropertyName("trustSchema")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TrustSchema? TrustSchema
    {
        get => _trustSchema;
        set
        {
            ExplicitNullMemberTracker.Clear(this, "trustSchema");
            _trustSchema = value;
        }
    }
    private TrustSchema? _trustSchema;

    /// <summary>Optional list of attestations (verifiable proofs).</summary>
    [JsonPropertyName("attestations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Attestation>? Attestations
    {
        get => _attestations;
        set
        {
            ExplicitNullMemberTracker.Clear(this, "attestations");
            _attestations = value;
        }
    }
    private List<Attestation>? _attestations;

    /// <summary>Optional provenance links (lineage records).</summary>
    [JsonPropertyName("provenance")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ProvenanceLink>? Provenance
    {
        get => _provenance;
        set
        {
            ExplicitNullMemberTracker.Clear(this, "provenance");
            _provenance = value;
        }
    }
    private List<ProvenanceLink>? _provenance;

    /// <summary>Optional privacy policy URL.</summary>
    [JsonIgnore]
    public string? PrivacyPolicyUrl { get; set; }

    /// <summary>Optional terms of service URL.</summary>
    [JsonIgnore]
    public string? TermsOfServiceUrl { get; set; }

    /// <summary>Optional cryptographic signature.</summary>
    [JsonPropertyName("extensions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? Extensions
    {
        get => _extensions;
        set
        {
            ExplicitNullMemberTracker.Clear(this, "extensions");
            _extensions = value;
        }
    }
    private Dictionary<string, JsonElement>? _extensions;

    /// <summary>Compatibility property; it is not a v0.9 wire member.</summary>
    [JsonIgnore]
    public string Identity
    {
        get => Contributor ?? string.Empty;
        set => Contributor = value;
    }

    /// <summary>Compatibility property; it is not a v0.9 wire member.</summary>
    [JsonIgnore]
    public string? IdentityType { get; set; }

    /// <summary>Compatibility property; it is not a v0.9 wire member.</summary>
    [JsonIgnore]
    public JsonElement? Metadata { get; set; }

    /// <summary>Unknown properties at the object level, preserved for round-trip fidelity.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
