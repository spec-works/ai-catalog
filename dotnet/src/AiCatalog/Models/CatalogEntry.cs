using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecWorks.AiCatalog.Models;

/// <summary>
/// A single catalog entry describing an AI artifact.
/// Must have exactly one of <see cref="Url"/> or <see cref="Data"/>.
/// </summary>
public sealed class CatalogEntry
{
    /// <summary>The unique identifier for this entry.</summary>
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    /// <summary>The optional human-readable display name.</summary>
    [JsonPropertyName("displayName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName
    {
        get => _displayName;
        set
        {
            ExplicitNullMemberTracker.Clear(this, "displayName");
            _displayName = value;
        }
    }
    private string? _displayName;

    /// <summary>The required artifact type.</summary>
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; set; }

    /// <summary>Optional digest of the artifact.</summary>
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

    /// <summary>URL to the artifact. Mutually exclusive with <see cref="Data"/>.</summary>
    [JsonPropertyName("url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url
    {
        get => _url;
        set
        {
            HasUrlMember = false;
            _url = value;
        }
    }
    private string? _url;

    [JsonIgnore]
    internal bool HasUrlMember { get; set; }

    /// <summary>Embedded artifact content (opaque JSON value). Mutually exclusive with <see cref="Url"/>.</summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(PreservingNullableJsonElementConverter))]
    public JsonElement? Data { get; set; }

    /// <summary>Optional version string for multi-version entries.</summary>
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

    /// <summary>Optional human-readable description of the artifact.</summary>
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

    /// <summary>Optional string tags for categorization.</summary>
    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Tags
    {
        get => _tags;
        set
        {
            ExplicitNullMemberTracker.Clear(this, "tags");
            _tags = value;
        }
    }
    private List<string>? _tags;

    /// <summary>Optional publisher information.</summary>
    [JsonPropertyName("publisher")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Publisher? Publisher
    {
        get => _publisher;
        set
        {
            ExplicitNullMemberTracker.Clear(this, "publisher");
            _publisher = value;
        }
    }
    private Publisher? _publisher;

    /// <summary>Optional trust manifests for this entry.</summary>
    [JsonPropertyName("trustManifests")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<TrustManifest>? TrustManifests
    {
        get => _trustManifests;
        set
        {
            ExplicitNullMemberTracker.Clear(this, "trustManifests");
            _trustManifests = value;
        }
    }
    private List<TrustManifest>? _trustManifests;

    /// <summary>Optional RFC 3339 datetime when the entry was last updated.</summary>
    [JsonPropertyName("updatedAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UpdatedAt
    {
        get => _updatedAt;
        set
        {
            ExplicitNullMemberTracker.Clear(this, "updatedAt");
            _updatedAt = value;
        }
    }
    private string? _updatedAt;

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

    /// <summary>Compatibility property; it is not a v0.9 wire member.</summary>
    [JsonIgnore]
    public string MediaType
    {
        get => Type ?? string.Empty;
        set => Type = value;
    }

    /// <summary>Compatibility property; it is not a v0.9 wire member.</summary>
    [JsonIgnore]
    public TrustManifest? TrustManifest
    {
        get => TrustManifests?.FirstOrDefault();
        set => TrustManifests = value is null ? null : [value];
    }

    /// <summary>Compatibility property; it is not a v0.9 wire member.</summary>
    [JsonIgnore]
    public JsonElement? Metadata { get; set; }

    /// <summary>Unknown properties preserved for forward compatibility (MUST ignore per VH-2).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
