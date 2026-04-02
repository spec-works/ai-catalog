using System.Text.Json;
using SpecWorks.AiCatalog.Models;

namespace SpecWorks.AiCatalog.Serialization;

/// <summary>
/// Serializes <see cref="AiCatalog"/> instances to JSON.
/// Omits null optional fields and preserves metadata values exactly.
/// </summary>
public static class AiCatalogSerializer
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        PropertyNamingPolicy = null, // We use [JsonPropertyName] attributes
    };

    /// <summary>
    /// Serializes an <see cref="AiCatalog"/> to a JSON string.
    /// </summary>
    /// <param name="catalog">The catalog to serialize.</param>
    /// <returns>A JSON string representation.</returns>
    public static string Serialize(Models.AiCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return JsonSerializer.Serialize(catalog, s_options);
    }

    /// <summary>
    /// Serializes an <see cref="AiCatalog"/> to a stream.
    /// </summary>
    /// <param name="catalog">The catalog to serialize.</param>
    /// <param name="stream">The stream to write to.</param>
    public static void Serialize(Models.AiCatalog catalog, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(stream);
        JsonSerializer.Serialize(stream, catalog, s_options);
    }
}
