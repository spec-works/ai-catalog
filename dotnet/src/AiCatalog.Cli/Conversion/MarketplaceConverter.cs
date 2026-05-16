using System.IO.Compression;
using System.Text.Json;
using SpecWorks.AiCatalog.Models;

namespace SpecWorks.AiCatalog.Cli.Conversion;

/// <summary>
/// Converts marketplace JSON documents into AI Catalog documents.
/// Each plugin becomes a nested ai-catalog entry (not a plugin-specific media type).
/// Supports two marketplace formats:
/// <list type="bullet">
///   <item><description>Claude Code Plugins: plugins with <c>display_name</c>, <c>manifest_url</c>, <c>publisher</c></description></item>
///   <item><description>Copilot Plugins: plugins with <c>source</c>, <c>skills</c>, plus root-level <c>owner</c></description></item>
/// </list>
/// </summary>
public static class MarketplaceConverter
{
    private const string AiCatalogMediaType = "application/ai-catalog+json";
    private const string SkillZipMediaType = "application/zip";
    private const string ClaudeIdentifierPrefix = "urn:claude:plugins:";
    private const string MarketplaceIdentifierPrefix = "urn:marketplace:";

    /// <summary>
    /// Options controlling how skill assets are packaged during conversion.
    /// </summary>
    public sealed class PackagingOptions
    {
        /// <summary>Directory containing the marketplace.json (used to resolve relative skill paths).</summary>
        public string? SourceDir { get; init; }

        /// <summary>Directory where skill zip packages will be written (e.g., next to the output catalog).</summary>
        public string? OutputDir { get; init; }
    }

    /// <summary>
    /// Converts a marketplace JSON string to an <see cref="Models.AiCatalog"/>.
    /// </summary>
    public static Models.AiCatalog Convert(string marketplaceJson, PackagingOptions? packaging = null)
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
            return ConvertDocument(doc, packaging);
        }
    }

    /// <summary>
    /// Converts a marketplace JSON stream to an <see cref="Models.AiCatalog"/>.
    /// </summary>
    public static Models.AiCatalog Convert(Stream stream, PackagingOptions? packaging = null)
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
            return ConvertDocument(doc, packaging);
        }
    }

    private static Models.AiCatalog ConvertDocument(JsonDocument doc, PackagingOptions? packaging)
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
                ? ConvertCopilotPlugin(plugin, marketplaceName, sharedPublisher, packaging)
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

    private static CatalogEntry ConvertCopilotPlugin(JsonElement plugin, string? marketplaceName, Publisher? sharedPublisher, PackagingOptions? packaging)
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
            MediaType = AiCatalogMediaType,
        };

        if (plugin.TryGetProperty("description", out var descElement) && descElement.ValueKind == JsonValueKind.String)
        {
            entry.Description = descElement.GetString();
        }

        if (plugin.TryGetProperty("version", out var versionElement) && versionElement.ValueKind == JsonValueKind.String)
        {
            entry.Version = versionElement.GetString();
        }

        if (sharedPublisher != null)
        {
            entry.Publisher = new Publisher
            {
                Identifier = sharedPublisher.Identifier,
                DisplayName = sharedPublisher.DisplayName
            };
        }

        // Get the plugin source folder for resolving relative skill paths
        var pluginSource = plugin.TryGetProperty("source", out var srcEl) && srcEl.ValueKind == JsonValueKind.String
            ? srcEl.GetString()
            : null;

        // If skills are present, package each skill folder as a zip and create nested catalog entries
        if (plugin.TryGetProperty("skills", out var skillsElement) && skillsElement.ValueKind == JsonValueKind.Array)
        {
            var canPackage = packaging?.SourceDir != null && packaging?.OutputDir != null;
            var skillEntries = new List<CatalogEntry>();

            foreach (var s in skillsElement.EnumerateArray())
            {
                if (s.ValueKind != JsonValueKind.String)
                    continue;

                var skillPath = s.GetString()!;
                var leafName = skillPath.LastIndexOf('/') is var idx && idx >= 0
                    ? skillPath[(idx + 1)..]
                    : skillPath;

                var skillEntry = new CatalogEntry
                {
                    Identifier = $"{identifier}:{leafName}",
                    DisplayName = leafName,
                };

                if (canPackage)
                {
                    var zipRelPath = PackageSkill(skillPath, pluginSource, leafName, packaging!);
                    if (zipRelPath != null)
                    {
                        skillEntry.MediaType = SkillZipMediaType;
                        skillEntry.Url = zipRelPath;
                    }
                    else
                    {
                        // Skill folder not found — fall back to raw path
                        skillEntry.MediaType = "application/json";
                        skillEntry.Url = skillPath;
                    }
                }
                else
                {
                    // No packaging options — use raw path as before
                    skillEntry.MediaType = "application/json";
                    skillEntry.Url = skillPath;
                }

                skillEntries.Add(skillEntry);
            }

            if (skillEntries.Count > 0)
            {
                var nestedCatalog = new Models.AiCatalog
                {
                    SpecVersion = "1.0",
                    Entries = skillEntries
                };

                // Serialize the nested catalog to a JsonElement for the Data property
                var nestedJson = JsonSerializer.Serialize(nestedCatalog, s_serializerOptions);
                entry.Data = JsonDocument.Parse(nestedJson).RootElement.Clone();
            }
        }

        // If no nested Data was created, fall back to url from source
        if (entry.Data == null)
        {
            if (pluginSource != null)
            {
                entry.Url = pluginSource;
            }
        }

        return entry;
    }

    /// <summary>
    /// Zips the skill folder and writes it to the output skills/ directory.
    /// Returns the relative path to the zip (e.g., "skills/a2a-ask-cli.zip"), or null if the folder doesn't exist.
    /// </summary>
    private static string? PackageSkill(string skillPath, string? pluginSource, string leafName, PackagingOptions packaging)
    {
        // Skill paths in marketplace.json are relative to the plugin's source folder.
        // E.g., plugin source = "plugins/a2a-ask", skill = "./skills/a2a-ask-cli"
        // The full path from repo root: plugins/a2a-ask/skills/a2a-ask-cli

        var normalizedSkillPath = skillPath.TrimStart('.', '/', '\\');
        var sourceDir = packaging.SourceDir!;

        var candidatePaths = new List<string>();

        // Primary: relative to plugin source folder within sourceDir
        if (pluginSource != null)
        {
            candidatePaths.Add(Path.Combine(sourceDir, pluginSource, normalizedSkillPath));
        }

        // Fallback: relative to sourceDir directly
        candidatePaths.Add(Path.Combine(sourceDir, normalizedSkillPath));

        // Fallback: walk up from sourceDir looking for the plugin source structure
        // This handles marketplace.json in a subdirectory (e.g., .github/plugin/)
        if (pluginSource != null)
        {
            var dir = sourceDir;
            for (int i = 0; i < 4; i++)
            {
                var parent = Directory.GetParent(dir)?.FullName;
                if (parent == null || parent == dir) break;
                dir = parent;
                candidatePaths.Add(Path.Combine(dir, pluginSource, normalizedSkillPath));
            }
        }

        string? skillDir = null;
        foreach (var candidate in candidatePaths)
        {
            if (Directory.Exists(candidate))
            {
                skillDir = candidate;
                break;
            }
        }

        if (skillDir == null)
            return null;

        var skillsOutputDir = Path.Combine(packaging.OutputDir!, "skills");
        Directory.CreateDirectory(skillsOutputDir);

        var zipFileName = $"{leafName}.zip";
        var zipPath = Path.Combine(skillsOutputDir, zipFileName);

        // Remove existing zip if present
        if (File.Exists(zipPath))
            File.Delete(zipPath);

        ZipFile.CreateFromDirectory(skillDir, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);

        return $"skills/{zipFileName}";
    }

    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = null,
    };

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
            MediaType = AiCatalogMediaType,
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
