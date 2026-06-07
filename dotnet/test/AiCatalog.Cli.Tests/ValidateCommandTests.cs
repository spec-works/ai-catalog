using System.CommandLine;
using System.CommandLine.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using SpecWorks.AiCatalog.Cli.Commands;
using Xunit;

namespace SpecWorks.AiCatalog.Cli.Tests;

public class ValidateCommandTests
{
    private const string SkillContent = "# Test Skill\nThis is a local skill.";

    [Fact]
    public async Task Validate_ValidCatalogWithExistingFiles_ReturnsSuccess()
    {
        using var workspace = await TestWorkspace.CreateAsync(CreateCatalogJson());
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(ValidateCommand.Create());

        var console = new TestConsole();
        var exitCode = await rootCommand.InvokeAsync($"validate \"{workspace.CatalogPath}\"", console);

        Assert.Equal(0, exitCode);
        Assert.Contains("Conformance level: Trusted", console.Out.ToString());
        Assert.Contains("0 errors, 0 warnings", console.Out.ToString());
        Assert.Equal(string.Empty, console.Error.ToString());
    }

    [Fact]
    public async Task Validate_MissingFilePath_ReturnsError()
    {
        using var workspace = await TestWorkspace.CreateAsync(CreateCatalogJson(url: "./missing/SKILL.md"), createSkillFile: false);
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(ValidateCommand.Create());

        var console = new TestConsole();
        var exitCode = await rootCommand.InvokeAsync($"validate \"{workspace.CatalogPath}\"", console);

        Assert.Equal(1, exitCode);
        Assert.Contains("Referenced local path does not exist", console.Error.ToString());
        Assert.Contains("1 errors, 0 warnings", console.Out.ToString());
    }

    [Fact]
    public async Task Validate_UnknownMediaType_ReportsWarning()
    {
        using var workspace = await TestWorkspace.CreateAsync(CreateCatalogJson(mediaType: "application/x-custom-agent"));
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(ValidateCommand.Create());

        var console = new TestConsole();
        var exitCode = await rootCommand.InvokeAsync($"validate \"{workspace.CatalogPath}\"", console);

        Assert.Equal(0, exitCode);
        Assert.Contains("Warnings:", console.Out.ToString());
        Assert.Contains("Unknown media type 'application/x-custom-agent'", console.Out.ToString());
        Assert.Contains("0 errors, 1 warnings", console.Out.ToString());
    }

    [Fact]
    public async Task Validate_DigestMismatch_ReturnsError()
    {
        using var workspace = await TestWorkspace.CreateAsync(CreateCatalogJson(digest: "sha256:0000000000000000000000000000000000000000000000000000000000000000"));
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(ValidateCommand.Create());

        var console = new TestConsole();
        var exitCode = await rootCommand.InvokeAsync($"validate \"{workspace.CatalogPath}\"", console);

        Assert.Equal(1, exitCode);
        Assert.Contains("Digest mismatch", console.Error.ToString());
        Assert.Contains("1 errors, 0 warnings", console.Out.ToString());
    }

    [Fact]
    public async Task Validate_StrictMode_TreatsWarningsAsErrors()
    {
        using var workspace = await TestWorkspace.CreateAsync(CreateCatalogJson(mediaType: "application/x-custom-agent"));
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(ValidateCommand.Create());

        var console = new TestConsole();
        var exitCode = await rootCommand.InvokeAsync($"validate \"{workspace.CatalogPath}\" --strict", console);

        Assert.Equal(1, exitCode);
        Assert.Contains("Unknown media type 'application/x-custom-agent'", console.Out.ToString());
        Assert.Contains("0 errors, 1 warnings", console.Out.ToString());
    }

    [Fact]
    public async Task Validate_DuplicateIdentifiers_ReportsError()
    {
        var catalog = new JsonObject
        {
            ["specVersion"] = "1.0",
            ["host"] = new JsonObject
            {
                ["displayName"] = "Test Host",
                ["trustManifest"] = new JsonObject { ["identity"] = "urn:test:host" },
            },
            ["entries"] = new JsonArray(
                new JsonObject
                {
                    ["identifier"] = "urn:test:duplicate",
                    ["displayName"] = "First",
                    ["mediaType"] = "text/markdown; profile=ai-skill",
                    ["url"] = "./skill/SKILL.md",
                },
                new JsonObject
                {
                    ["identifier"] = "urn:test:duplicate",
                    ["displayName"] = "Second",
                    ["mediaType"] = "text/markdown; profile=ai-skill",
                    ["url"] = "./skill/SKILL.md",
                }),
        };

        using var workspace = await TestWorkspace.CreateAsync(catalog.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(ValidateCommand.Create());

        var console = new TestConsole();
        var exitCode = await rootCommand.InvokeAsync($"validate \"{workspace.CatalogPath}\"", console);

        Assert.Equal(1, exitCode);
        Assert.Contains("Duplicate identifier 'urn:test:duplicate'", console.Error.ToString());
    }

    private static string CreateCatalogJson(string mediaType = "text/markdown; profile=ai-skill", string url = "./skill/SKILL.md", string? digest = null)
    {
        var entry = new JsonObject
        {
            ["identifier"] = "urn:test:skill",
            ["displayName"] = "Test Skill",
            ["mediaType"] = mediaType,
            ["url"] = url,
        };

        if (digest is not null)
        {
            entry["metadata"] = new JsonObject
            {
                ["digest"] = digest,
            };
        }

        var catalog = new JsonObject
        {
            ["specVersion"] = "1.0",
            ["host"] = new JsonObject
            {
                ["displayName"] = "Test Host",
                ["trustManifest"] = new JsonObject
                {
                    ["identity"] = "urn:test:host",
                },
            },
            ["entries"] = new JsonArray(entry),
        };

        return catalog.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private sealed class TestWorkspace : IDisposable
    {
        private TestWorkspace(string rootDirectory, string catalogPath, string skillPath)
        {
            RootDirectory = rootDirectory;
            CatalogPath = catalogPath;
            SkillPath = skillPath;
        }

        public string RootDirectory { get; }

        public string CatalogPath { get; }

        public string SkillPath { get; }

        public static async Task<TestWorkspace> CreateAsync(string catalogJson, bool createSkillFile = true)
        {
            var rootDirectory = Path.Combine(Path.GetTempPath(), "AiCatalog.ValidateCommandTests", Guid.NewGuid().ToString("N"));
            var catalogPath = Path.Combine(rootDirectory, "ai-catalog.json");
            var skillPath = Path.Combine(rootDirectory, "skill", "SKILL.md");

            Directory.CreateDirectory(rootDirectory);
            await File.WriteAllTextAsync(catalogPath, catalogJson);

            if (createSkillFile)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(skillPath)!);
                await File.WriteAllTextAsync(skillPath, SkillContent);
            }

            return new TestWorkspace(rootDirectory, catalogPath, skillPath);
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
