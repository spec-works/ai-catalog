using SpecWorks.AiCatalog.Models;

namespace SpecWorks.AiCatalog.Cli.Conversion;

/// <summary>
/// Projects an AI Catalog into the OpenAI Codex CLI plugin marketplace layout.
/// </summary>
public sealed class CodexProjector : CatalogProjector
{
    public CodexProjector(Models.AiCatalog catalog, string inputFilePath, string outputDirectory, string sourceDirectory, HttpClient httpClient)
        : base(catalog, inputFilePath, outputDirectory, sourceDirectory, httpClient)
    {
    }

    public override async Task ProjectAsync(CancellationToken cancellationToken = default)
    {
        var marketplaceRoot = Path.Combine(OutputDirectory, ".agents", "plugins");
        var marketplacePath = Path.Combine(marketplaceRoot, "marketplace.json");
        var pluginsRoot = Path.Combine(marketplaceRoot, "plugins");
        var plugins = new List<object>(Catalog.Entries.Count);

        foreach (var entry in Catalog.Entries)
        {
            var pluginName = GetPluginName(entry);
            var skills = await ResolveSkillsAsync(entry, cancellationToken);
            await WriteSkillsAsync(pluginsRoot, entry, skills, cancellationToken);

            var pluginPath = Path.Combine(pluginsRoot, pluginName, ".codex-plugin", "plugin.json");
            var pluginJson = new
            {
                name = pluginName,
                version = GetEntryVersion(entry),
                description = GetEntryDescription(entry),
                skills = "./skills/",
            };
            await WriteJsonFileAsync(pluginPath, pluginJson, cancellationToken);

            plugins.Add(new
            {
                name = pluginName,
                source = new
                {
                    source = "local",
                    path = $"./plugins/{pluginName}",
                },
                policy = new
                {
                    installation = "AVAILABLE",
                    authentication = "ON_INSTALL",
                },
                category = "Productivity",
            });
        }

        var marketplace = new
        {
            name = CatalogName,
            @interface = new
            {
                displayName = CatalogDisplayName,
            },
            plugins,
        };

        await WriteJsonFileAsync(marketplacePath, marketplace, cancellationToken);
    }
}
