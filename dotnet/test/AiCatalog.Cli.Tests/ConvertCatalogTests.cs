using System.CommandLine;
using System.CommandLine.IO;
using System.Text.Json;
using SpecWorks.AiCatalog.Cli.Commands;
using SpecWorks.AiCatalog.Cli.Conversion;
using SpecWorks.AiCatalog.Parsing;
using Xunit;

namespace SpecWorks.AiCatalog.Cli.Tests;

public class ConvertCatalogTests
{
    private const string CatalogJson = """
{
  "specVersion": "1.0",
  "host": {
    "identifier": "test-catalog",
    "displayName": "Test Catalog"
  },
  "entries": [
    {
      "identifier": "urn:test:plugin-a",
      "displayName": "plugin-a",
      "mediaType": "application/vnd.copilot.plugin+json",
      "url": "plugins/plugin-a",
      "version": "1.0.0",
      "description": "Test plugin A",
      "tags": ["skill-one"],
      "publisher": { "identifier": "https://test.example", "displayName": "TestPublisher" }
    },
    {
      "identifier": "urn:test:plugin-b",
      "displayName": "plugin-b",
      "mediaType": "application/vnd.copilot.plugin+json",
      "url": "plugins/plugin-b",
      "version": "2.0.0",
      "description": "Test plugin B",
      "tags": ["skill-two", "skill-three"],
      "publisher": { "identifier": "https://test.example", "displayName": "TestPublisher" }
    }
  ]
}
""";

    private const string SkillOneContent = "# Skill One\nTest content";
    private const string SkillTwoContent = "# Skill Two\nTest content two";
    private const string SkillThreeContent = "# Skill Three\nTest content three";

    [Fact]
    public async Task UnifiedProjector_WritesUnifiedMarketplaceAndSkills()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var catalog = AiCatalogParser.Parse(CatalogJson);
        using var httpClient = new HttpClient();

        var projector = new UnifiedProjector(catalog, workspace.CatalogPath, workspace.OutputDirectory, workspace.SourceDirectory, httpClient);

        await projector.ProjectAsync();

        var marketplacePath = Path.Combine(workspace.OutputDirectory, "marketplace.json");
        var claudeMarketplacePath = Path.Combine(workspace.OutputDirectory, ".claude-plugin", "marketplace.json");
        Assert.True(File.Exists(marketplacePath));
        Assert.True(File.Exists(claudeMarketplacePath));

        using var marketplaceDocument = JsonDocument.Parse(await File.ReadAllTextAsync(marketplacePath));
        var root = marketplaceDocument.RootElement;
        Assert.Equal("test-catalog", root.GetProperty("name").GetString());
        Assert.Equal("TestPublisher", root.GetProperty("owner").GetProperty("name").GetString());
        Assert.Equal("https://test.example", root.GetProperty("owner").GetProperty("url").GetString());

        var plugins = root.GetProperty("plugins").EnumerateArray().ToArray();
        Assert.Equal(2, plugins.Length);

        var pluginA = plugins.Single(plugin => plugin.GetProperty("name").GetString() == "plugin-a");
        Assert.Equal("./plugins/plugin-a", pluginA.GetProperty("source").GetString());
        Assert.Equal("Test plugin A", pluginA.GetProperty("description").GetString());
        Assert.Equal("1.0.0", pluginA.GetProperty("version").GetString());
        Assert.False(pluginA.TryGetProperty("skills", out _));

        var pluginB = plugins.Single(plugin => plugin.GetProperty("name").GetString() == "plugin-b");
        Assert.Equal("./plugins/plugin-b", pluginB.GetProperty("source").GetString());
        Assert.False(pluginB.TryGetProperty("skills", out _));

        using var claudeMarketplaceDocument = JsonDocument.Parse(await File.ReadAllTextAsync(claudeMarketplacePath));
        var claudePlugins = claudeMarketplaceDocument.RootElement.GetProperty("plugins").EnumerateArray().ToArray();
        Assert.Equal(2, claudePlugins.Length);

        var claudePluginA = claudePlugins.Single(plugin => plugin.GetProperty("name").GetString() == "plugin-a");
        Assert.Equal("plugin-a", claudePluginA.GetProperty("display_name").GetString());
        Assert.Equal("../plugins/plugin-a/.plugin/plugin.json", claudePluginA.GetProperty("manifest_url").GetString());

        AssertPluginLayout(workspace.OutputDirectory, "plugin-a", "1.0.0", "Test plugin A", new[] { "./skills/skill-one" }, SkillOneContent);
        AssertPluginLayout(workspace.OutputDirectory, "plugin-b", "2.0.0", "Test plugin B", new[] { "./skills/skill-two", "./skills/skill-three" }, SkillTwoContent, SkillThreeContent);
    }

    [Fact]
    public async Task Export_GeneratesUnifiedOutput()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(ExportCommand.Create());

        var console = new TestConsole();
        var exitCode = await rootCommand.InvokeAsync($"export \"{workspace.CatalogPath}\" --output \"{workspace.OutputDirectory}\"", console);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(workspace.OutputDirectory, "marketplace.json")));
        Assert.True(File.Exists(Path.Combine(workspace.OutputDirectory, ".claude-plugin", "marketplace.json")));
        Assert.True(File.Exists(Path.Combine(workspace.OutputDirectory, "plugins", "plugin-a", ".plugin", "plugin.json")));
        Assert.True(File.Exists(Path.Combine(workspace.OutputDirectory, "plugins", "plugin-a", ".codex-plugin", "plugin.json")));
        Assert.True(File.Exists(Path.Combine(workspace.OutputDirectory, "plugins", "plugin-a", ".github", "plugin", "plugin.json")));
    }

    [Fact]
    public async Task Export_WithSourceDirectory_CopiesSkillContentFromSource()
    {
        using var workspace = await TestWorkspace.CreateAsync(separateSourceDirectory: true);
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(ExportCommand.Create());

        var console = new TestConsole();
        var exitCode = await rootCommand.InvokeAsync(
            $"export \"{workspace.CatalogPath}\" --output \"{workspace.OutputDirectory}\" --source-dir \"{workspace.SourceDirectory}\"",
            console);

        Assert.Equal(0, exitCode);
        AssertSkillFile(workspace.OutputDirectory, Path.Combine("plugins", "plugin-a", "skills", "skill-one", "SKILL.md"), SkillOneContent);
        AssertSkillFile(workspace.OutputDirectory, Path.Combine("plugins", "plugin-b", "skills", "skill-two", "SKILL.md"), SkillTwoContent);
        AssertSkillFile(workspace.OutputDirectory, Path.Combine("plugins", "plugin-b", "skills", "skill-three", "SKILL.md"), SkillThreeContent);
    }

    [Fact]
    public async Task Export_Command_StillWorksWithUnifiedLayout()
    {
        using var workspace = await TestWorkspace.CreateAsync(separateSourceDirectory: true);
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(ExportCommand.Create());

        var console = new TestConsole();
        var exitCode = await rootCommand.InvokeAsync(
            $"export \"{workspace.CatalogPath}\" --output \"{workspace.OutputDirectory}\" --source-dir \"{workspace.SourceDirectory}\"",
            console);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(workspace.OutputDirectory, "marketplace.json")));
        Assert.True(File.Exists(Path.Combine(workspace.OutputDirectory, ".claude-plugin", "marketplace.json")));
    }

    private static void AssertPluginLayout(
        string outputDirectory,
        string pluginName,
        string version,
        string description,
        string[] expectedSkills,
        params string[] skillContents)
    {
        var pluginRoot = Path.Combine(outputDirectory, "plugins", pluginName);
        var pluginJsonPath = Path.Combine(pluginRoot, ".plugin", "plugin.json");
        var codexPluginJsonPath = Path.Combine(pluginRoot, ".codex-plugin", "plugin.json");
        var githubPluginJsonPath = Path.Combine(pluginRoot, ".github", "plugin", "plugin.json");

        Assert.True(File.Exists(pluginJsonPath));
        Assert.True(File.Exists(codexPluginJsonPath));
        Assert.True(File.Exists(githubPluginJsonPath));
        Assert.False(File.Exists(Path.Combine(pluginRoot, "README.md")));

        using var pluginDocument = JsonDocument.Parse(File.ReadAllText(pluginJsonPath));
        var pluginRootElement = pluginDocument.RootElement;
        Assert.Equal(pluginName, pluginRootElement.GetProperty("name").GetString());
        Assert.Equal(version, pluginRootElement.GetProperty("version").GetString());
        Assert.Equal(description, pluginRootElement.GetProperty("description").GetString());
        Assert.Equal("./skills/", pluginRootElement.GetProperty("skills").GetString());

        using var codexPluginDocument = JsonDocument.Parse(File.ReadAllText(codexPluginJsonPath));
        Assert.Equal(pluginName, codexPluginDocument.RootElement.GetProperty("name").GetString());
        Assert.Equal("./skills/", codexPluginDocument.RootElement.GetProperty("skills").GetString());

        using var githubPluginDocument = JsonDocument.Parse(File.ReadAllText(githubPluginJsonPath));
        var githubRootElement = githubPluginDocument.RootElement;
        Assert.Equal(pluginName, githubRootElement.GetProperty("name").GetString());
        Assert.Equal("./skills/", githubRootElement.GetProperty("skills").GetString());

        var skillNames = expectedSkills.Select(skill => skill.Replace("./skills/", string.Empty)).ToArray();
        for (var i = 0; i < skillNames.Length; i++)
        {
            AssertSkillFile(outputDirectory, Path.Combine("plugins", pluginName, "skills", skillNames[i], "SKILL.md"), skillContents[i]);
        }
    }

    private static void AssertSkillFile(string outputDirectory, string relativePath, string expectedContent)
    {
        var path = Path.Combine(outputDirectory, relativePath);
        Assert.True(File.Exists(path), $"Expected skill file to exist: {path}");
        Assert.Equal(expectedContent, File.ReadAllText(path));
    }

    private sealed class TestWorkspace : IDisposable
    {
        private TestWorkspace(string rootDirectory, string catalogPath, string sourceDirectory, string outputDirectory)
        {
            RootDirectory = rootDirectory;
            CatalogPath = catalogPath;
            SourceDirectory = sourceDirectory;
            OutputDirectory = outputDirectory;
        }

        public string RootDirectory { get; }

        public string CatalogPath { get; }

        public string SourceDirectory { get; }

        public string OutputDirectory { get; }

        public static async Task<TestWorkspace> CreateAsync(bool separateSourceDirectory = false)
        {
            var rootDirectory = Path.Combine(Path.GetTempPath(), "AiCatalog.ConvertCatalogTests", Guid.NewGuid().ToString("N"));
            var catalogDirectory = separateSourceDirectory ? Path.Combine(rootDirectory, "input") : rootDirectory;
            var sourceDirectory = separateSourceDirectory ? Path.Combine(rootDirectory, "source") : rootDirectory;
            var outputDirectory = Path.Combine(rootDirectory, "output");
            var catalogPath = Path.Combine(catalogDirectory, "catalog.json");

            Directory.CreateDirectory(catalogDirectory);
            Directory.CreateDirectory(sourceDirectory);
            Directory.CreateDirectory(outputDirectory);

            await File.WriteAllTextAsync(catalogPath, CatalogJson);
            await WriteSkillFileAsync(sourceDirectory, Path.Combine("plugins", "plugin-a", "skills", "skill-one", "SKILL.md"), SkillOneContent);
            await WriteSkillFileAsync(sourceDirectory, Path.Combine("plugins", "plugin-b", "skills", "skill-two", "SKILL.md"), SkillTwoContent);
            await WriteSkillFileAsync(sourceDirectory, Path.Combine("plugins", "plugin-b", "skills", "skill-three", "SKILL.md"), SkillThreeContent);

            return new TestWorkspace(rootDirectory, catalogPath, sourceDirectory, outputDirectory);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(RootDirectory))
                {
                    Directory.Delete(RootDirectory, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static async Task WriteSkillFileAsync(string sourceDirectory, string relativePath, string content)
        {
            var fullPath = Path.Combine(sourceDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllTextAsync(fullPath, content);
        }
    }
}
