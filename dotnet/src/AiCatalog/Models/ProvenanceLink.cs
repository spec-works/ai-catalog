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
    public string? SourceDigest { get; set; }

    /// <summary>Optional registry URI.</summary>
    [JsonPropertyName("registryUri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RegistryUri { get; set; }

    /// <summary>Optional statement URI.</summary>
    [JsonPropertyName("statementUri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StatementUri { get; set; }

    /// <summary>Optional signature reference.</summary>
    [JsonPropertyName("signatureRef")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SignatureRef { get; set; }

    /// <summary>Unknown properties at the object level, preserved for round-trip fidelity.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
