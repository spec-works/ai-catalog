using SpecWorks.AiCatalog.Cli.Commands;
using System.CommandLine;
using System.CommandLine.IO;
using Xunit;

namespace SpecWorks.AiCatalog.Cli.Tests;

/// <summary>
/// Integration tests for the CLI command structure and argument parsing.
/// </summary>
public class CommandStructureTests
{
    [Fact]
    public async Task RootCommand_Help_ShowsNewCommandsOnly()
    {
        var rootCommand = CreateRootCommand();

        var console = new TestConsole();
        await rootCommand.InvokeAsync("--help", console);

        var output = console.Out.ToString()!;
        Assert.Contains("migrate", output);
        Assert.Contains("export", output);
        Assert.Contains("publish", output);
        Assert.Contains("explore", output);
        Assert.Contains("install", output);
        Assert.DoesNotContain("convert", output);
    }

    [Fact]
    public async Task Migrate_Help_ShowsOptions()
    {
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(MigrateCommand.Create());

        var console = new TestConsole();
        await rootCommand.InvokeAsync("migrate --help", console);

        var output = console.Out.ToString()!;
        Assert.Contains("input", output);
        Assert.Contains("--output", output);
    }

    [Fact]
    public async Task Export_Help_ShowsOptions()
    {
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(ExportCommand.Create());

        var console = new TestConsole();
        await rootCommand.InvokeAsync("export --help", console);

        var output = console.Out.ToString()!;
        Assert.Contains("input", output);
        Assert.Contains("--output", output);
        Assert.Contains("--github", output);
        Assert.Contains("--codex", output);
        Assert.Contains("--claude", output);
    }

    [Fact]
    public async Task Publish_Help_ShowsOptions()
    {
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(PublishCommand.Create());

        var console = new TestConsole();
        await rootCommand.InvokeAsync("publish --help", console);

        var output = console.Out.ToString()!;
        Assert.Contains("input", output);
        Assert.Contains("--output", output);
    }

    [Fact]
    public async Task ExploreCommand_Help_ShowsOptions()
    {
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(ExploreCommand.Create());

        var console = new TestConsole();
        await rootCommand.InvokeAsync("explore --help", console);

        var output = console.Out.ToString()!;
        Assert.Contains("url", output);
        Assert.Contains("--filter-tag", output);
        Assert.Contains("--filter-media-type", output);
        Assert.Contains("--show", output);
    }

    [Fact]
    public async Task InstallCommand_Help_ShowsOptions()
    {
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(InstallCommand.Create());

        var console = new TestConsole();
        await rootCommand.InvokeAsync("install --help", console);

        var output = console.Out.ToString()!;
        Assert.Contains("catalog-url", output);
        Assert.Contains("entry-id", output);
        Assert.Contains("--type", output);
    }

    [Fact]
    public async Task Migrate_MissingFile_ReturnsError()
    {
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(MigrateCommand.Create());

        var console = new TestConsole();
        var exitCode = await rootCommand.InvokeAsync("migrate nonexistent-file.json", console);

        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public async Task Migrate_WithFixture_Succeeds()
    {
        var testcasesDir = Path.Combine(AppContext.BaseDirectory, "testcases");
        var inputPath = Path.Combine(testcasesDir, "marketplace-input.json");

        if (!File.Exists(inputPath))
        {
            return;
        }

        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(MigrateCommand.Create());

        var console = new TestConsole();
        var exitCode = await rootCommand.InvokeAsync($"migrate \"{inputPath}\"", console);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Migrate_WithFixture_WritesToFile()
    {
        var testcasesDir = Path.Combine(AppContext.BaseDirectory, "testcases");
        var inputPath = Path.Combine(testcasesDir, "marketplace-input.json");

        if (!File.Exists(inputPath))
        {
            return;
        }

        var outputPath = Path.Combine(AppContext.BaseDirectory, "test-output-marketplace.json");
        try
        {
            var rootCommand = new RootCommand("AI Catalog CLI");
            rootCommand.AddCommand(MigrateCommand.Create());

            var console = new TestConsole();
            var exitCode = await rootCommand.InvokeAsync(
                $"migrate \"{inputPath}\" --output \"{outputPath}\"", console);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));

            var content = File.ReadAllText(outputPath);
            Assert.Contains("specVersion", content);
            Assert.Contains("urn:claude:plugins:web-search", content);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    private static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand("AI Catalog CLI — migrate, export, publish, explore, and install AI artifacts");
        rootCommand.AddCommand(MigrateCommand.Create());
        rootCommand.AddCommand(ExportCommand.Create());
        rootCommand.AddCommand(PublishCommand.Create());
        rootCommand.AddCommand(ExploreCommand.Create());
        rootCommand.AddCommand(InstallCommand.Create());
        return rootCommand;
    }
}
