using SpecWorks.AiCatalog.Models;

namespace SpecWorks.AiCatalog.Cli.Conversion;

/// <summary>
/// Projects an AI Catalog into a unified plugin marketplace layout that contains all supported platform manifests.
/// </summary>
public sealed class UnifiedProjector : CatalogProjector
{
    public UnifiedProjector(Models.AiCatalog catalog, string inputFilePath, string outputDirectory, string sourceDirectory, HttpClient httpClient)
        : base(catalog, inputFilePath, outputDirectory, sourceDirectory, httpClient)
    {
    }

    public override async Task ProjectAsync(CancellationToken cancellationToken = default)
    {
        var pluginsRoot = Path.Combine(OutputDirectory, "plugins");
        var marketplacePlugins = new List<object>(Catalog.Entries.Count);
        var claudePlugins = new List<object>(Catalog.Entries.Count);

        foreach (var entry in Catalog.Entries)
        {
            var pluginName = GetPluginName(entry);
            var skills = await ResolveSkillsAsync(entry, cancellationToken);
            await WriteSkillsAsync(pluginsRoot, entry, skills, cancellationToken);

            var pluginRoot = Path.Combine(pluginsRoot, pluginName);
            var pluginManifest = CreatePluginManifest(pluginName, entry);

            await WriteJsonFileAsync(Path.Combine(pluginRoot, ".plugin", "plugin.json"), pluginManifest, cancellationToken);
            await WriteJsonFileAsync(Path.Combine(pluginRoot, ".codex-plugin", "plugin.json"), pluginManifest, cancellationToken);
            await WriteJsonFileAsync(Path.Combine(pluginRoot, ".github", "plugin", "marketplace.json"), CreatePluginMarketplace(pluginName, entry, skills), cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(pluginRoot, "README.md"), CreateReadme(pluginName, entry), cancellationToken);

            marketplacePlugins.Add(new
            {
                name = pluginName,
                source = $"./plugins/{pluginName}",
                description = GetEntryDescription(entry),
                version = GetEntryVersion(entry),
                skills = skills.Select(skill => $"./skills/{skill.Name}").ToArray(),
            });

            claudePlugins.Add(new
            {
                name = pluginName,
                display_name = string.IsNullOrWhiteSpace(entry.DisplayName) ? pluginName : entry.DisplayName,
                description = GetEntryDescription(entry),
                version = GetEntryVersion(entry),
                manifest_url = $"../plugins/{pluginName}/.plugin/plugin.json",
                source = $"../plugins/{pluginName}",
                keywords = GetKeywords(entry),
            });
        }

        await WriteJsonFileAsync(
            Path.Combine(OutputDirectory, "marketplace.json"),
            new
            {
                name = CatalogName,
                metadata = new
                {
                    description = CatalogDescription,
                    version = CatalogVersion,
                },
                owner = new
                {
                    name = PublisherName,
                    url = PublisherIdentifier,
                },
                plugins = marketplacePlugins,
            },
            cancellationToken);

        await WriteJsonFileAsync(
            Path.Combine(OutputDirectory, ".claude-plugin", "marketplace.json"),
            new
            {
                name = CatalogName,
                owner = new
                {
                    name = PublisherName,
                    url = PublisherIdentifier,
                },
                metadata = new
                {
                    pluginRoot = "../plugins",
                },
                plugins = claudePlugins,
            },
            cancellationToken);
    }

    private object CreatePluginManifest(string pluginName, CatalogEntry entry) => new
    {
        name = pluginName,
        version = GetEntryVersion(entry),
        description = GetEntryDescription(entry),
        skills = "./skills/",
    };

    private object CreatePluginMarketplace(string pluginName, CatalogEntry entry, IReadOnlyList<ResolvedSkill> skills) => new
    {
        name = CatalogName,
        metadata = new
        {
            description = CatalogDescription,
            version = CatalogVersion,
        },
        owner = new
        {
            name = PublisherName,
            url = PublisherIdentifier,
        },
        plugins = new[]
        {
            new
            {
                name = pluginName,
                source = "./",
                description = GetEntryDescription(entry),
                version = GetEntryVersion(entry),
                skills = skills.Select(skill => $"./skills/{skill.Name}").ToArray(),
            }
        },
    };

    private string CreateReadme(string pluginName, CatalogEntry entry)
    {
        var title = string.IsNullOrWhiteSpace(entry.DisplayName) ? pluginName : entry.DisplayName;
        var description = GetEntryDescription(entry);

        return string.IsNullOrWhiteSpace(description)
            ? $"# {title}{Environment.NewLine}"
            : $"# {title}{Environment.NewLine}{Environment.NewLine}{description}{Environment.NewLine}";
    }
}
