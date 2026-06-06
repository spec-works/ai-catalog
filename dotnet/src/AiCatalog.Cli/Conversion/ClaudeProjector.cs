using SpecWorks.AiCatalog.Models;

namespace SpecWorks.AiCatalog.Cli.Conversion;

/// <summary>
/// Projects an AI Catalog into the Claude Code plugin marketplace layout.
/// </summary>
public sealed class ClaudeProjector : CatalogProjector
{
    public ClaudeProjector(Models.AiCatalog catalog, string inputFilePath, string outputDirectory, string sourceDirectory, HttpClient httpClient)
        : base(catalog, inputFilePath, outputDirectory, sourceDirectory, httpClient)
    {
    }

    public override async Task ProjectAsync(CancellationToken cancellationToken = default)
    {
        var marketplaceRoot = Path.Combine(OutputDirectory, ".claude-plugin");
        var marketplacePath = Path.Combine(marketplaceRoot, "marketplace.json");
        var pluginsRoot = Path.Combine(marketplaceRoot, "plugins");
        var plugins = new List<object>(Catalog.Entries.Count);

        foreach (var entry in Catalog.Entries)
        {
            var pluginName = GetPluginName(entry);
            var skills = await ResolveSkillsAsync(entry, cancellationToken);
            await WriteSkillsAsync(pluginsRoot, entry, skills, cancellationToken);

            var pluginPath = Path.Combine(pluginsRoot, pluginName, ".claude-plugin", "plugin.json");
            var pluginJson = new
            {
                name = pluginName,
                description = GetEntryDescription(entry),
                version = GetEntryVersion(entry),
                skills = "./skills/",
            };
            await WriteJsonFileAsync(pluginPath, pluginJson, cancellationToken);

            plugins.Add(new
            {
                name = pluginName,
                description = GetEntryDescription(entry),
                version = GetEntryVersion(entry),
                source = $"./plugins/{pluginName}",
                keywords = GetKeywords(entry),
            });
        }

        var marketplace = new
        {
            name = CatalogName,
            owner = new
            {
                name = PublisherName,
            },
            metadata = new
            {
                pluginRoot = "./plugins",
            },
            plugins,
        };

        await WriteJsonFileAsync(marketplacePath, marketplace, cancellationToken);
    }
}
