using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecWorks.AiCatalog.Models;

/// <summary>
/// Top-level AI Catalog document containing entries and optional host and metadata.
/// Maps to the root object of an <c>application/ai-catalog+json</c> document.
/// </summary>
public sealed class AiCatalog
{
    /// <summary>The specification version of this catalog (e.g., "0.9").</summary>
    [JsonPropertyName("specVersion")]
    public string SpecVersion { get; set; } = string.Empty;

    /// <summary>The list of catalog entries describing AI artifacts.</summary>
    [JsonPropertyName("entries")]
    public List<CatalogEntry> Entries { get; set; } = [];

    /// <summary>Whether the source document contained the required entries member.</summary>
    [JsonIgnore]
    internal bool HasEntriesMember { get; set; } = true;

    [JsonIgnore]
    internal Dictionary<string, (object Parent, string MemberName)> ExplicitNullTypedMemberParents { get; } = new(StringComparer.Ordinal);

    /// <summary>Optional host information for the catalog operator.</summary>
    [JsonPropertyName("host")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HostInfo? Host
    {
        get => _host;
        set
        {
            ExplicitNullMemberTracker.Clear(this, "host");
            _host = value;
        }
    }
    private HostInfo? _host;

    /// <summary>Optional namespaced extension values.</summary>
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

    /// <summary>Optional catalog signature.</summary>
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

    /// <summary>Compatibility property; it is not a v0.9 wire member.</summary>
    [JsonIgnore]
    public JsonElement? Metadata { get; set; }

    /// <summary>Unknown properties preserved for forward compatibility (MUST ignore per VH-2).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
