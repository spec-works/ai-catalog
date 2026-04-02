using System.Text.Json;
using SpecWorks.AiCatalog.Models;

namespace SpecWorks.AiCatalog.Cli.Conversion;

/// <summary>
/// Converts marketplace JSON documents into AI Catalog documents.
/// Supports two marketplace formats:
/// <list type="bullet">
///   <item><description>Claude Code Plugins: plugins with <c>display_name</c>, <c>manifest_url</c>, <c>publisher</c></description></item>
///   <item><description>Copilot Plugins: plugins with <c>source</c>, <c>skills</c>, plus root-level <c>owner</c></description></item>
/// </list>
/// </summary>
public static class MarketplaceConverter
{
    private const string ClaudePluginMediaType = "application/vnd.claude.code-plugin+json";
    private const string CopilotPluginMediaType = "application/vnd.copilot.plugin+json";
    private const string ClaudeIdentifierPrefix = "urn:claude:plugins:";
    private const string MarketplaceIdentifierPrefix = "urn:marketplace:";

    /// <summary>
    /// Converts a marketplace JSON string to an <see cref="Models.AiCatalog"/>.
    /// </summary>
    /// <param name="marketplaceJson">The marketplace JSON string (containing a "plugins" array or wrapped in a test fixture with "input.plugins").</param>
    /// <returns>An AI Catalog document with one entry per marketplace plugin.</returns>
    /// <exception cref="AiCatalogException">Thrown when the marketplace JSON is invalid.</exception>
    public static Models.AiCatalog Convert(string marketplaceJson)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(marketplaceJson);
        }
        catch (JsonException ex)
        {
            throw new AiCatalogException($"Invalid marketplace JSON: {ex.Message}", ex);
        }

        using (doc)
        {
            return ConvertDocument(doc);
        }
    }

    /// <summary>
    /// Converts a marketplace JSON stream to an <see cref="Models.AiCatalog"/>.
    /// </summary>
    public static Models.AiCatalog Convert(Stream stream)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(stream);
        }
        catch (JsonException ex)
        {
            throw new AiCatalogException($"Invalid marketplace JSON: {ex.Message}", ex);
        }

        using (doc)
        {
            return ConvertDocument(doc);
        }
    }

    private static Models.AiCatalog ConvertDocument(JsonDocument doc)
    {
        var root = doc.RootElement;

        // Support both raw marketplace format {"plugins": [...]} and
        // test fixture format {"input": {"plugins": [...]}}
        JsonElement pluginsContainer;
        if (root.TryGetProperty("input", out var inputElement) && inputElement.ValueKind == JsonValueKind.Object)
        {
            pluginsContainer = inputElement;
        }
        else
        {
            pluginsContainer = root;
        }

        if (!pluginsContainer.TryGetProperty("plugins", out var pluginsArray) || pluginsArray.ValueKind != JsonValueKind.Array)
        {
            throw new AiCatalogException("Marketplace JSON must contain a 'plugins' array");
        }

        // Detect marketplace format: Claude uses display_name, copilot uses source
        var isCopilotFormat = IsCopilotFormat(pluginsArray);

        // Extract root-level owner for copilot format (applies to all entries)
        Publisher? sharedPublisher = null;
        string? marketplaceName = null;

        if (isCopilotFormat)
        {
            marketplaceName = pluginsContainer.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
                ? nameEl.GetString()
                : null;
            sharedPublisher = ExtractOwnerAsPublisher(pluginsContainer);
        }

        var entries = new List<CatalogEntry>();

        foreach (var plugin in pluginsArray.EnumerateArray())
        {
            entries.Add(isCopilotFormat
                ? ConvertCopilotPlugin(plugin, marketplaceName, sharedPublisher)
                : ConvertClaudePlugin(plugin));
        }

        return new Models.AiCatalog
        {
            SpecVersion = "1.0",
            Entries = entries
        };
    }

    /// <summary>
    /// Detects whether plugins use the copilot format (has <c>source</c>, no <c>display_name</c>).
    /// </summary>
    private static bool IsCopilotFormat(JsonElement pluginsArray)
    {
        foreach (var plugin in pluginsArray.EnumerateArray())
        {
            if (plugin.TryGetProperty("source", out _) && !plugin.TryGetProperty("display_name", out _))
                return true;
            if (plugin.TryGetProperty("display_name", out _))
                return false;
        }
        // Default to Claude format for empty arrays
        return false;
    }

    /// <summary>
    /// Extracts the root-level <c>owner</c> object as a <see cref="Publisher"/>.
    /// </summary>
    private static Publisher? ExtractOwnerAsPublisher(JsonElement container)
    {
        if (!container.TryGetProperty("owner", out var ownerElement) || ownerElement.ValueKind != JsonValueKind.Object)
            return null;

        var ownerName = ownerElement.TryGetProperty("name", out var n) ? n.GetString() : null;
        var ownerUrl = ownerElement.TryGetProperty("url", out var u) ? u.GetString() : null;

        if (ownerName == null && ownerUrl == null)
            return null;

        // Generate a URN identifier when no URL is provided
        var identifier = ownerUrl
            ?? $"urn:marketplace:owner:{ownerName!.ToLowerInvariant().Replace(' ', '-')}";

        return new Publisher
        {
            Identifier = identifier,
            DisplayName = ownerName ?? string.Empty
        };
    }

    private static CatalogEntry ConvertCopilotPlugin(JsonElement plugin, string? marketplaceName, Publisher? sharedPublisher)
    {
        var name = plugin.GetProperty("name").GetString()
            ?? throw new AiCatalogException("Plugin 'name' is required");

        // Build identifier: urn:marketplace:{marketplace}:{name} or urn:marketplace:plugins:{name}
        var identifier = marketplaceName != null
            ? $"{MarketplaceIdentifierPrefix}{marketplaceName}:{name}"
            : $"{MarketplaceIdentifierPrefix}plugins:{name}";

        var entry = new CatalogEntry
        {
            Identifier = identifier,
            DisplayName = name,
            MediaType = CopilotPluginMediaType,
        };

        if (plugin.TryGetProperty("source", out var sourceElement) && sourceElement.ValueKind == JsonValueKind.String)
        {
            entry.Url = sourceElement.GetString();
        }

        if (plugin.TryGetProperty("description", out var descElement) && descElement.ValueKind == JsonValueKind.String)
        {
            entry.Description = descElement.GetString();
        }

        if (plugin.TryGetProperty("version", out var versionElement) && versionElement.ValueKind == JsonValueKind.String)
        {
            entry.Version = versionElement.GetString();
        }

        // Map skills paths to tags (extract leaf name from each path)
        if (plugin.TryGetProperty("skills", out var skillsElement) && skillsElement.ValueKind == JsonValueKind.Array)
        {
            entry.Tags = skillsElement.EnumerateArray()
                .Where(s => s.ValueKind == JsonValueKind.String)
                .Select(s =>
                {
                    var path = s.GetString()!;
                    var lastSlash = path.LastIndexOf('/');
                    return lastSlash >= 0 ? path[(lastSlash + 1)..] : path;
                })
                .ToList();
        }

        if (sharedPublisher != null)
        {
            entry.Publisher = new Publisher
            {
                Identifier = sharedPublisher.Identifier,
                DisplayName = sharedPublisher.DisplayName
            };
        }

        return entry;
    }

    private static CatalogEntry ConvertClaudePlugin(JsonElement plugin)
    {
        var name = plugin.GetProperty("name").GetString()
            ?? throw new AiCatalogException("Plugin 'name' is required");
        var displayName = plugin.GetProperty("display_name").GetString()
            ?? throw new AiCatalogException("Plugin 'display_name' is required");
        var manifestUrl = plugin.GetProperty("manifest_url").GetString()
            ?? throw new AiCatalogException("Plugin 'manifest_url' is required");

        var entry = new CatalogEntry
        {
            Identifier = $"{ClaudeIdentifierPrefix}{name}",
            DisplayName = displayName,
            MediaType = ClaudePluginMediaType,
            Url = manifestUrl
        };

        if (plugin.TryGetProperty("description", out var descElement) && descElement.ValueKind == JsonValueKind.String)
        {
            entry.Description = descElement.GetString();
        }

        if (plugin.TryGetProperty("version", out var versionElement) && versionElement.ValueKind == JsonValueKind.String)
        {
            entry.Version = versionElement.GetString();
        }

        if (plugin.TryGetProperty("categories", out var categoriesElement) && categoriesElement.ValueKind == JsonValueKind.Array)
        {
            entry.Tags = categoriesElement.EnumerateArray()
                .Where(c => c.ValueKind == JsonValueKind.String)
                .Select(c => c.GetString()!)
                .ToList();
        }

        if (plugin.TryGetProperty("updated_at", out var updatedAtElement) && updatedAtElement.ValueKind == JsonValueKind.String)
        {
            entry.UpdatedAt = updatedAtElement.GetString();
        }

        if (plugin.TryGetProperty("publisher", out var publisherElement) && publisherElement.ValueKind == JsonValueKind.Object)
        {
            var pubName = publisherElement.TryGetProperty("name", out var pn) ? pn.GetString() : null;
            var pubUrl = publisherElement.TryGetProperty("url", out var pu) ? pu.GetString() : null;

            if (pubName != null || pubUrl != null)
            {
                entry.Publisher = new Publisher
                {
                    Identifier = pubUrl ?? string.Empty,
                    DisplayName = pubName ?? string.Empty
                };
            }
        }

        return entry;
    }
}
