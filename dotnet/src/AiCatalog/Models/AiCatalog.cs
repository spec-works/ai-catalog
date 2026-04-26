using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecWorks.AiCatalog.Models;

/// <summary>
/// Top-level AI Catalog document containing entries and optional host and metadata.
/// Maps to the root object of an <c>application/ai-catalog+json</c> document.
/// </summary>
public sealed class AiCatalog
{
    /// <summary>The specification version of this catalog (e.g., "1.0").</summary>
    [JsonPropertyName("specVersion")]
    public string SpecVersion { get; set; } = string.Empty;

    /// <summary>The list of catalog entries describing AI artifacts.</summary>
    [JsonPropertyName("entries")]
    public List<CatalogEntry> Entries { get; set; } = [];

    /// <summary>Optional host information for the catalog operator.</summary>
    [JsonPropertyName("host")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HostInfo? Host { get; set; }

    /// <summary>Optional open metadata map for extension data.</summary>
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Metadata { get; set; }

    /// <summary>Unknown properties preserved for forward compatibility (MUST ignore per VH-2).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
