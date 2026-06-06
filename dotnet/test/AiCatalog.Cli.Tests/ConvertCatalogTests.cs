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
    public async Task GitHubProjector_WritesMarketplaceAndSkills()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var catalog = AiCatalogParser.Parse(CatalogJson);
        using var httpClient = new HttpClient();

        var projector = new GitHubProjector(catalog, workspace.CatalogPath, workspace.OutputDirectory, workspace.SourceDirectory, httpClient);

        await projector.ProjectAsync();

        var marketplacePath = Path.Combine(workspace.OutputDirectory, ".github", "plugin", "marketplace.json");
        Assert.True(File.Exists(marketplacePath));

        using var marketplaceDocument = JsonDocument.Parse(await File.ReadAllTextAsync(marketplacePath));
        var root = marketplaceDocument.RootElement;
        Assert.Equal("test-catalog", root.GetProperty("name").GetString());
        Assert.Equal("TestPublisher", root.GetProperty("owner").GetProperty("name").GetString());
        Assert.Equal("https://test.example", root.GetProperty("owner").GetProperty("url").GetString());

        var plugins = root.GetProperty("plugins").EnumerateArray().ToArray();
        Assert.Equal(2, plugins.Length);

        var pluginA = plugins.Single(plugin => plugin.GetProperty("name").GetString() == "plugin-a");
        Assert.Equal("plugins/plugin-a", pluginA.GetProperty("source").GetString());
        Assert.Equal("Test plugin A", pluginA.GetProperty("description").GetString());
        Assert.Equal("1.0.0", pluginA.GetProperty("version").GetString());
        Assert.Equal(new[] { "./skills/skill-one" }, pluginA.GetProperty("skills").EnumerateArray().Select(value => value.GetString()).ToArray());

        var pluginB = plugins.Single(plugin => plugin.GetProperty("name").GetString() == "plugin-b");
        Assert.Equal("plugins/plugin-b", pluginB.GetProperty("source").GetString());
        Assert.Equal(new[] { "./skills/skill-two", "./skills/skill-three" }, pluginB.GetProperty("skills").EnumerateArray().Select(value => value.GetString()).ToArray());

        AssertSkillFile(workspace.OutputDirectory, Path.Combine("plugins", "plugin-a", "skills", "skill-one", "SKILL.md"), SkillOneContent);
        AssertSkillFile(workspace.OutputDirectory, Path.Combine("plugins", "plugin-b", "skills", "skill-two", "SKILL.md"), SkillTwoContent);
        AssertSkillFile(workspace.OutputDirectory, Path.Combine("plugins", "plugin-b", "skills", "skill-three", "SKILL.md"), SkillThreeContent);
    }

    [Fact]
    public async Task CodexProjector_WritesMarketplacePluginJsonAndSkills()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var catalog = AiCatalogParser.Parse(CatalogJson);
        using var httpClient = new HttpClient();

        var projector = new CodexProjector(catalog, workspace.CatalogPath, workspace.OutputDirectory, workspace.SourceDirectory, httpClient);

        await projector.ProjectAsync();

        var marketplacePath = Path.Combine(workspace.OutputDirectory, ".agents", "plugins", "marketplace.json");
        Assert.True(File.Exists(marketplacePath));

        using var marketplaceDocument = JsonDocument.Parse(await File.ReadAllTextAsync(marketplacePath));
        var plugins = marketplaceDocument.RootElement.GetProperty("plugins").EnumerateArray().ToArray();
        Assert.Equal(2, plugins.Length);

        foreach (var plugin in plugins)
        {
            var source = plugin.GetProperty("source");
            Assert.Equal("local", source.GetProperty("source").GetString());
            Assert.StartsWith("./plugins/", source.GetProperty("path").GetString());
        }

        var pluginAJsonPath = Path.Combine(workspace.OutputDirectory, ".agents", "plugins", "plugins", "plugin-a", ".codex-plugin", "plugin.json");
        var pluginJsonPath = Path.Combine(workspace.OutputDirectory, ".agents", "plugins", "plugins", "plugin-b", ".codex-plugin", "plugin.json");
        Assert.True(File.Exists(pluginAJsonPath));
        Assert.True(File.Exists(pluginJsonPath));

        using var pluginDocument = JsonDocument.Parse(await File.ReadAllTextAsync(pluginJsonPath));
        var pluginRoot = pluginDocument.RootElement;
        Assert.Equal("plugin-b", pluginRoot.GetProperty("name").GetString());
        Assert.Equal("2.0.0", pluginRoot.GetProperty("version").GetString());
        Assert.Equal("Test plugin B", pluginRoot.GetProperty("description").GetString());
        Assert.Equal("./skills/", pluginRoot.GetProperty("skills").GetString());

        AssertSkillFile(workspace.OutputDirectory, Path.Combine(".agents", "plugins", "plugins", "plugin-a", "skills", "skill-one", "SKILL.md"), SkillOneContent);
        AssertSkillFile(workspace.OutputDirectory, Path.Combine(".agents", "plugins", "plugins", "plugin-b", "skills", "skill-two", "SKILL.md"), SkillTwoContent);
        AssertSkillFile(workspace.OutputDirectory, Path.Combine(".agents", "plugins", "plugins", "plugin-b", "skills", "skill-three", "SKILL.md"), SkillThreeContent);
    }

    [Fact]
    public async Task ClaudeProjector_WritesMarketplacePluginJsonAndSkills()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var catalog = AiCatalogParser.Parse(CatalogJson);
        using var httpClient = new HttpClient();

        var projector = new ClaudeProjector(catalog, workspace.CatalogPath, workspace.OutputDirectory, workspace.SourceDirectory, httpClient);

        await projector.ProjectAsync();

        var marketplacePath = Path.Combine(workspace.OutputDirectory, ".claude-plugin", "marketplace.json");
        Assert.True(File.Exists(marketplacePath));

        using var marketplaceDocument = JsonDocument.Parse(await File.ReadAllTextAsync(marketplacePath));
        var plugins = marketplaceDocument.RootElement.GetProperty("plugins").EnumerateArray().ToArray();
        Assert.Equal(2, plugins.Length);

        foreach (var plugin in plugins)
        {
            var source = plugin.GetProperty("source").GetString();
            Assert.NotNull(source);
            Assert.StartsWith("./", source);
        }

        var pluginJsonPath = Path.Combine(workspace.OutputDirectory, ".claude-plugin", "plugins", "plugin-a", ".claude-plugin", "plugin.json");
        var pluginBJsonPath = Path.Combine(workspace.OutputDirectory, ".claude-plugin", "plugins", "plugin-b", ".claude-plugin", "plugin.json");
        Assert.True(File.Exists(pluginJsonPath));
        Assert.True(File.Exists(pluginBJsonPath));

        using var pluginDocument = JsonDocument.Parse(await File.ReadAllTextAsync(pluginJsonPath));
        var pluginRoot = pluginDocument.RootElement;
        Assert.Equal("plugin-a", pluginRoot.GetProperty("name").GetString());
        Assert.Equal("1.0.0", pluginRoot.GetProperty("version").GetString());
        Assert.Equal("Test plugin A", pluginRoot.GetProperty("description").GetString());
        Assert.Equal("./skills/", pluginRoot.GetProperty("skills").GetString());

        AssertSkillFile(workspace.OutputDirectory, Path.Combine(".claude-plugin", "plugins", "plugin-a", "skills", "skill-one", "SKILL.md"), SkillOneContent);
        AssertSkillFile(workspace.OutputDirectory, Path.Combine(".claude-plugin", "plugins", "plugin-b", "skills", "skill-two", "SKILL.md"), SkillTwoContent);
        AssertSkillFile(workspace.OutputDirectory, Path.Combine(".claude-plugin", "plugins", "plugin-b", "skills", "skill-three", "SKILL.md"), SkillThreeContent);
    }

    [Fact]
    public async Task ConvertCatalog_WithoutFlags_GeneratesAllPlatformOutputs()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(ConvertCommand.Create());

        var console = new TestConsole();
        var exitCode = await rootCommand.InvokeAsync($"convert catalog \"{workspace.CatalogPath}\" --output \"{workspace.OutputDirectory}\"", console);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(workspace.OutputDirectory, ".github", "plugin", "marketplace.json")));
        Assert.True(File.Exists(Path.Combine(workspace.OutputDirectory, ".agents", "plugins", "marketplace.json")));
        Assert.True(File.Exists(Path.Combine(workspace.OutputDirectory, ".claude-plugin", "marketplace.json")));
    }

    [Fact]
    public async Task ConvertCatalog_WithSourceDirectory_CopiesSkillContentFromSource()
    {
        using var workspace = await TestWorkspace.CreateAsync(separateSourceDirectory: true);
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(ConvertCommand.Create());

        var console = new TestConsole();
        var exitCode = await rootCommand.InvokeAsync(
            $"convert catalog \"{workspace.CatalogPath}\" --output \"{workspace.OutputDirectory}\" --source-dir \"{workspace.SourceDirectory}\" --github",
            console);

        Assert.Equal(0, exitCode);
        AssertSkillFile(workspace.OutputDirectory, Path.Combine("plugins", "plugin-a", "skills", "skill-one", "SKILL.md"), SkillOneContent);
        AssertSkillFile(workspace.OutputDirectory, Path.Combine("plugins", "plugin-b", "skills", "skill-two", "SKILL.md"), SkillTwoContent);
        AssertSkillFile(workspace.OutputDirectory, Path.Combine("plugins", "plugin-b", "skills", "skill-three", "SKILL.md"), SkillThreeContent);
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
