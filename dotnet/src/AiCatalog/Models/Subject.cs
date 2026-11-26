using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecWorks.AiCatalog.Models;

/// <summary>The artifact release described by a Trust Manifest signature.</summary>
public sealed class Subject
{
    /// <summary>The subject artifact identifier.</summary>
    [JsonPropertyName("identifier")]
    public string? Identifier { get; set; }

    /// <summary>The subject artifact type.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>The subject artifact digest.</summary>
    [JsonPropertyName("digest")]
    public string? Digest { get; set; }

    /// <summary>The optional subject release version.</summary>
    [JsonPropertyName("version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Version
    {
        get => _version;
        set
        {
            ExplicitNullMemberTracker.Clear(this, "version");
            _version = value;
        }
    }
    private string? _version;

    /// <summary>Unknown members preserved for round-trip fidelity.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
