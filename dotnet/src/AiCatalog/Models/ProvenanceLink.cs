using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecWorks.AiCatalog.Models;

/// <summary>
/// A provenance link describing lineage and origin of an artifact.
/// </summary>
public sealed class ProvenanceLink
{
    /// <summary>The relation type (e.g., "publishedFrom", "derivedFrom") (required).</summary>
    [JsonPropertyName("relation")]
    public string Relation { get; set; } = string.Empty;

    /// <summary>The source identifier (required).</summary>
    [JsonPropertyName("sourceId")]
    public string SourceId { get; set; } = string.Empty;

    /// <summary>Optional digest of the source.</summary>
    [JsonPropertyName("sourceDigest")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SourceDigest
    {
        get => _sourceDigest;
        set
        {
            ExplicitNullMemberTracker.Clear(this, "sourceDigest");
            _sourceDigest = value;
        }
    }
    private string? _sourceDigest;

    /// <summary>Optional registry URI.</summary>
    [JsonPropertyName("registryUri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RegistryUri
    {
        get => _registryUri;
        set
        {
            ExplicitNullMemberTracker.Clear(this, "registryUri");
            _registryUri = value;
        }
    }
    private string? _registryUri;

    /// <summary>Optional statement URI.</summary>
    [JsonPropertyName("statementUri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StatementUri
    {
        get => _statementUri;
        set
        {
            ExplicitNullMemberTracker.Clear(this, "statementUri");
            _statementUri = value;
        }
    }
    private string? _statementUri;

    /// <summary>Optional signature reference.</summary>
    [JsonPropertyName("signatureRef")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SignatureRef
    {
        get => _signatureRef;
        set
        {
            ExplicitNullMemberTracker.Clear(this, "signatureRef");
            _signatureRef = value;
        }
    }
    private string? _signatureRef;

    /// <summary>Unknown properties at the object level, preserved for round-trip fidelity.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
