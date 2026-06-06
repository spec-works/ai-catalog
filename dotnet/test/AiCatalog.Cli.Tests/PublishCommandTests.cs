using System.CommandLine;
using System.CommandLine.IO;
using System.Text.Json;
using SpecWorks.AiCatalog.Cli.Commands;
using Xunit;

namespace SpecWorks.AiCatalog.Cli.Tests;

public class PublishCommandTests
{
    private const string CatalogJson = """
{
  "specVersion": "1.0",
  "entries": [
    {
      "identifier": "urn:test:simple-skill",
      "displayName": "Simple Skill",
      "mediaType": "text/plain",
      "url": "./source-skill/SKILL.md"
    }
  ]
}
""";

    private const string SkillContent = "# Simple Skill\nThis is a local skill.";

    [Fact]
    public async Task Publish_LocalSkillFile_CopiesArtifactAndUpdatesCatalog()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(PublishCommand.Create());

        var console = new TestConsole();
        var exitCode = await rootCommand.InvokeAsync(
            $"publish \"{workspace.CatalogPath}\" --output \"{workspace.OutputDirectory}\"",
            console);

        Assert.Equal(0, exitCode);

        var outputCatalogPath = Path.Combine(workspace.OutputDirectory, "ai-catalog.json");
        var copiedSkillPath = Path.Combine(workspace.OutputDirectory, "skills", "simple-skill", "SKILL.md");

        Assert.True(File.Exists(outputCatalogPath));
        Assert.True(File.Exists(copiedSkillPath));
        Assert.Equal(SkillContent, await File.ReadAllTextAsync(copiedSkillPath));

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputCatalogPath));
        var entry = document.RootElement.GetProperty("entries").EnumerateArray().Single();

        Assert.Equal("skills/simple-skill/SKILL.md", entry.GetProperty("url").GetString());
        Assert.Equal("text/markdown; profile=ai-skill", entry.GetProperty("mediaType").GetString());
        Assert.Equal(SkillContent, await File.ReadAllTextAsync(copiedSkillPath));
        Assert.True(entry.GetProperty("metadata").TryGetProperty("digest", out var digest));
        Assert.StartsWith("sha256:", digest.GetString());
    }

    private sealed class TestWorkspace : IDisposable
    {
        private TestWorkspace(string rootDirectory, string catalogPath, string outputDirectory)
        {
            RootDirectory = rootDirectory;
            CatalogPath = catalogPath;
            OutputDirectory = outputDirectory;
        }

        public string RootDirectory { get; }

        public string CatalogPath { get; }

        public string OutputDirectory { get; }

        public static async Task<TestWorkspace> CreateAsync()
        {
            var rootDirectory = Path.Combine(Path.GetTempPath(), "AiCatalog.PublishCommandTests", Guid.NewGuid().ToString("N"));
            var outputDirectory = Path.Combine(rootDirectory, "output");
            var catalogPath = Path.Combine(rootDirectory, "ai-catalog.json");
            var sourceSkillPath = Path.Combine(rootDirectory, "source-skill", "SKILL.md");

            Directory.CreateDirectory(outputDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(sourceSkillPath)!);

            await File.WriteAllTextAsync(catalogPath, CatalogJson);
            await File.WriteAllTextAsync(sourceSkillPath, SkillContent);

            return new TestWorkspace(rootDirectory, catalogPath, outputDirectory);
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
    }
}
