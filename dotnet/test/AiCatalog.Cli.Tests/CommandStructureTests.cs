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
    public async Task RootCommand_Help_ShowsAllCommands()
    {
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(ConvertCommand.Create());
        rootCommand.AddCommand(ExploreCommand.Create());
        rootCommand.AddCommand(InstallCommand.Create());

        var console = new TestConsole();
        var result = await rootCommand.InvokeAsync("--help", console);

        var output = console.Out.ToString()!;
        Assert.Contains("convert", output);
        Assert.Contains("explore", output);
        Assert.Contains("install", output);
    }

    [Fact]
    public async Task ConvertMarketplace_Help_ShowsOptions()
    {
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(ConvertCommand.Create());

        var console = new TestConsole();
        var result = await rootCommand.InvokeAsync("convert marketplace --help", console);

        var output = console.Out.ToString()!;
        Assert.Contains("input-file", output);
        Assert.Contains("--output", output);
    }

    [Fact]
    public async Task ExploreCommand_Help_ShowsOptions()
    {
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(ExploreCommand.Create());

        var console = new TestConsole();
        var result = await rootCommand.InvokeAsync("explore --help", console);

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
        var result = await rootCommand.InvokeAsync("install --help", console);

        var output = console.Out.ToString()!;
        Assert.Contains("catalog-url", output);
        Assert.Contains("entry-id", output);
        Assert.Contains("--type", output);
    }

    [Fact]
    public async Task ConvertMarketplace_MissingFile_ReturnsError()
    {
        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(ConvertCommand.Create());

        var console = new TestConsole();
        var exitCode = await rootCommand.InvokeAsync("convert marketplace nonexistent-file.json", console);

        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public async Task ConvertMarketplace_WithFixture_Succeeds()
    {
        var testcasesDir = Path.Combine(AppContext.BaseDirectory, "testcases");
        var inputPath = Path.Combine(testcasesDir, "marketplace-input.json");

        if (!File.Exists(inputPath))
        {
            return; // Skip if fixtures not available
        }

        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(ConvertCommand.Create());

        // Console.WriteLine output is not captured by TestConsole,
        // so we verify exit code here. Content correctness is verified
        // by ConvertMarketplaceTests.
        var console = new TestConsole();
        var exitCode = await rootCommand.InvokeAsync($"convert marketplace \"{inputPath}\"", console);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ConvertMarketplace_WithFixture_WritesToFile()
    {
        var testcasesDir = Path.Combine(AppContext.BaseDirectory, "testcases");
        var inputPath = Path.Combine(testcasesDir, "marketplace-input.json");

        if (!File.Exists(inputPath))
        {
            return; // Skip if fixtures not available
        }

        var outputPath = Path.Combine(AppContext.BaseDirectory, "test-output-marketplace.json");
        try
        {
            var rootCommand = new RootCommand("AI Catalog CLI");
            rootCommand.AddCommand(ConvertCommand.Create());

            var console = new TestConsole();
            var exitCode = await rootCommand.InvokeAsync(
                $"convert marketplace \"{inputPath}\" --output \"{outputPath}\"", console);

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
}
