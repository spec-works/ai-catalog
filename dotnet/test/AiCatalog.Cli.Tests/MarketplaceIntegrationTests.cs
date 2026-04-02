using System.CommandLine;
using System.CommandLine.IO;
using System.Text.Json;
using SpecWorks.AiCatalog.Cli.Commands;
using SpecWorks.AiCatalog.Cli.Conversion;
using SpecWorks.AiCatalog.Parsing;
using SpecWorks.AiCatalog.Serialization;
using SpecWorks.AiCatalog.Validation;
using Xunit;

namespace SpecWorks.AiCatalog.Cli.Tests;

/// <summary>
/// Integration tests using real-world marketplace.json files from GitHub repositories:
/// spec-works/plugins and microsoft/work-iq.
/// </summary>
public class MarketplaceIntegrationTests
{
    private static readonly string IntegrationDir = Path.Combine(
        AppContext.BaseDirectory, "testcases", "integration");

    private static string SpecWorksFixturePath =>
        Path.Combine(IntegrationDir, "spec-works-plugins-marketplace.json");

    private static string WorkIqFixturePath =>
        Path.Combine(IntegrationDir, "work-iq-marketplace.json");

    #region Library Conversion — spec-works-plugins

    [Fact]
    public void SpecWorks_Convert_ProducesCorrectEntryCount()
    {
        var json = File.ReadAllText(SpecWorksFixturePath);
        var catalog = MarketplaceConverter.Convert(json);

        Assert.Equal(5, catalog.Entries.Count);
    }

    [Fact]
    public void SpecWorks_Convert_SetsSpecVersion()
    {
        var json = File.ReadAllText(SpecWorksFixturePath);
        var catalog = MarketplaceConverter.Convert(json);

        Assert.Equal("1.0", catalog.SpecVersion);
    }

    [Fact]
    public void SpecWorks_Convert_MarkMyWordEntryPreservesFields()
    {
        var json = File.ReadAllText(SpecWorksFixturePath);
        var catalog = MarketplaceConverter.Convert(json);

        var markmyword = catalog.Entries.Single(e => e.DisplayName == "markmyword");
        Assert.Equal("urn:marketplace:spec-works-plugins:markmyword", markmyword.Identifier);
        Assert.Equal("Bidirectional Markdown and Word (.docx) conversion.", markmyword.Description);
        Assert.Equal("1.0.0", markmyword.Version);
        Assert.Equal("plugins/markmyword", markmyword.Url);
        Assert.Equal("application/vnd.copilot.plugin+json", markmyword.MediaType);
    }

    [Fact]
    public void SpecWorks_Convert_AllEntriesHavePublisher()
    {
        var json = File.ReadAllText(SpecWorksFixturePath);
        var catalog = MarketplaceConverter.Convert(json);

        Assert.All(catalog.Entries, entry =>
        {
            Assert.NotNull(entry.Publisher);
            Assert.Equal("SpecWorks", entry.Publisher!.DisplayName);
            Assert.Equal("https://spec-works.github.io", entry.Publisher.Identifier);
        });
    }

    [Fact]
    public void SpecWorks_Convert_SkillsMappedToTags()
    {
        var json = File.ReadAllText(SpecWorksFixturePath);
        var catalog = MarketplaceConverter.Convert(json);

        var markmyword = catalog.Entries.Single(e => e.DisplayName == "markmyword");
        Assert.NotNull(markmyword.Tags);
        Assert.Single(markmyword.Tags!);
        Assert.Equal("markmyword-cli", markmyword.Tags![0]);

        var officetalk = catalog.Entries.Single(e => e.DisplayName == "officetalk");
        Assert.NotNull(officetalk.Tags);
        Assert.Equal("officetalk-cli", officetalk.Tags![0]);
    }

    [Fact]
    public void SpecWorks_Convert_AllExpectedPluginNamesPresent()
    {
        var json = File.ReadAllText(SpecWorksFixturePath);
        var catalog = MarketplaceConverter.Convert(json);

        var names = catalog.Entries.Select(e => e.DisplayName).ToHashSet();
        Assert.Contains("markmyword", names);
        Assert.Contains("markmydeck", names);
        Assert.Contains("xregistry-mcp", names);
        Assert.Contains("officetalk", names);
        Assert.Contains("a2a-ask", names);
    }

    [Fact]
    public void SpecWorks_Convert_IdentifierUsesMarketplacePrefix()
    {
        var json = File.ReadAllText(SpecWorksFixturePath);
        var catalog = MarketplaceConverter.Convert(json);

        Assert.All(catalog.Entries, entry =>
            Assert.StartsWith("urn:marketplace:spec-works-plugins:", entry.Identifier));
    }

    #endregion

    #region Library Conversion — work-iq

    [Fact]
    public void WorkIq_Convert_ProducesCorrectEntryCount()
    {
        var json = File.ReadAllText(WorkIqFixturePath);
        var catalog = MarketplaceConverter.Convert(json);

        Assert.Equal(3, catalog.Entries.Count);
    }

    [Fact]
    public void WorkIq_Convert_WorkiqEntryPreservesFields()
    {
        var json = File.ReadAllText(WorkIqFixturePath);
        var catalog = MarketplaceConverter.Convert(json);

        var workiq = catalog.Entries.Single(e => e.DisplayName == "workiq");
        Assert.Equal("urn:marketplace:work-iq:workiq", workiq.Identifier);
        Assert.StartsWith("Query Microsoft 365 data", workiq.Description);
        Assert.Equal("1.0.0", workiq.Version);
        Assert.Equal("./plugins/workiq", workiq.Url);
    }

    [Fact]
    public void WorkIq_Convert_AllEntriesHavePublisher()
    {
        var json = File.ReadAllText(WorkIqFixturePath);
        var catalog = MarketplaceConverter.Convert(json);

        Assert.All(catalog.Entries, entry =>
        {
            Assert.NotNull(entry.Publisher);
            Assert.Equal("Microsoft", entry.Publisher!.DisplayName);
        });
    }

    [Fact]
    public void WorkIq_Convert_OwnerWithoutUrlGetsSyntheticIdentifier()
    {
        var json = File.ReadAllText(WorkIqFixturePath);
        var catalog = MarketplaceConverter.Convert(json);

        // work-iq owner has no url, so publisher.identifier should be a generated URN
        Assert.All(catalog.Entries, entry =>
            Assert.Equal("urn:marketplace:owner:microsoft", entry.Publisher!.Identifier));
    }

    [Fact]
    public void WorkIq_Convert_MultiSkillPluginMapsAllSkillsToTags()
    {
        var json = File.ReadAllText(WorkIqFixturePath);
        var catalog = MarketplaceConverter.Convert(json);

        // microsoft-365-agents-toolkit has 3 skills
        var toolkit = catalog.Entries.Single(e => e.DisplayName == "microsoft-365-agents-toolkit");
        Assert.NotNull(toolkit.Tags);
        Assert.Equal(3, toolkit.Tags!.Count);
        Assert.Contains("install-atk", toolkit.Tags);
        Assert.Contains("declarative-agent-developer", toolkit.Tags);
        Assert.Contains("ui-widget-developer", toolkit.Tags);

        // workiq-productivity has 9 skills
        var productivity = catalog.Entries.Single(e => e.DisplayName == "workiq-productivity");
        Assert.NotNull(productivity.Tags);
        Assert.Equal(9, productivity.Tags!.Count);
        Assert.Contains("action-item-extractor", productivity.Tags);
        Assert.Contains("channel-digest", productivity.Tags);
    }

    [Fact]
    public void WorkIq_Convert_AgentsToolkitVersionPreserved()
    {
        var json = File.ReadAllText(WorkIqFixturePath);
        var catalog = MarketplaceConverter.Convert(json);

        var toolkit = catalog.Entries.Single(e => e.DisplayName == "microsoft-365-agents-toolkit");
        Assert.Equal("1.1.1", toolkit.Version);
    }

    #endregion

    #region Round-Trip — serialize → parse back → validate

    [Fact]
    public void SpecWorks_RoundTrip_SerializeAndReparse()
    {
        var json = File.ReadAllText(SpecWorksFixturePath);
        var catalog = MarketplaceConverter.Convert(json);
        var serialized = AiCatalogSerializer.Serialize(catalog);

        var reparsed = AiCatalogParser.Parse(serialized);

        Assert.Equal(catalog.SpecVersion, reparsed.SpecVersion);
        Assert.Equal(catalog.Entries.Count, reparsed.Entries.Count);
        for (int i = 0; i < catalog.Entries.Count; i++)
        {
            Assert.Equal(catalog.Entries[i].Identifier, reparsed.Entries[i].Identifier);
            Assert.Equal(catalog.Entries[i].DisplayName, reparsed.Entries[i].DisplayName);
            Assert.Equal(catalog.Entries[i].Description, reparsed.Entries[i].Description);
            Assert.Equal(catalog.Entries[i].Version, reparsed.Entries[i].Version);
            Assert.Equal(catalog.Entries[i].MediaType, reparsed.Entries[i].MediaType);
        }
    }

    [Fact]
    public void WorkIq_RoundTrip_SerializeAndReparse()
    {
        var json = File.ReadAllText(WorkIqFixturePath);
        var catalog = MarketplaceConverter.Convert(json);
        var serialized = AiCatalogSerializer.Serialize(catalog);

        var reparsed = AiCatalogParser.Parse(serialized);

        Assert.Equal(catalog.SpecVersion, reparsed.SpecVersion);
        Assert.Equal(catalog.Entries.Count, reparsed.Entries.Count);
        for (int i = 0; i < catalog.Entries.Count; i++)
        {
            Assert.Equal(catalog.Entries[i].Identifier, reparsed.Entries[i].Identifier);
            Assert.Equal(catalog.Entries[i].DisplayName, reparsed.Entries[i].DisplayName);
            Assert.Equal(catalog.Entries[i].Description, reparsed.Entries[i].Description);
        }
    }

    [Fact]
    public void SpecWorks_RoundTrip_OutputIsValidJson()
    {
        var json = File.ReadAllText(SpecWorksFixturePath);
        var catalog = MarketplaceConverter.Convert(json);
        var serialized = AiCatalogSerializer.Serialize(catalog);

        // Verify it parses as valid JSON
        using var doc = JsonDocument.Parse(serialized);
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
        Assert.True(doc.RootElement.TryGetProperty("specVersion", out _));
        Assert.True(doc.RootElement.TryGetProperty("entries", out _));
    }

    [Fact]
    public void WorkIq_RoundTrip_OutputIsValidJson()
    {
        var json = File.ReadAllText(WorkIqFixturePath);
        var catalog = MarketplaceConverter.Convert(json);
        var serialized = AiCatalogSerializer.Serialize(catalog);

        using var doc = JsonDocument.Parse(serialized);
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
        Assert.True(doc.RootElement.TryGetProperty("specVersion", out _));
        Assert.True(doc.RootElement.TryGetProperty("entries", out _));
    }

    #endregion

    #region Conformance Validation

    [Fact]
    public void SpecWorks_ConvertedCatalog_ValidatesAsMinimal()
    {
        var json = File.ReadAllText(SpecWorksFixturePath);
        var catalog = MarketplaceConverter.Convert(json);

        var result = AiCatalogValidator.Validate(catalog, ConformanceLevel.Minimal);

        Assert.True(result.IsValid, $"Validation errors: {string.Join("; ", result.Errors.Select(e => e.Message))}");
        Assert.Equal(ConformanceLevel.Minimal, result.ConformanceLevel);
    }

    [Fact]
    public void WorkIq_ConvertedCatalog_ValidatesAsMinimal()
    {
        var json = File.ReadAllText(WorkIqFixturePath);
        var catalog = MarketplaceConverter.Convert(json);

        var result = AiCatalogValidator.Validate(catalog, ConformanceLevel.Minimal);

        Assert.True(result.IsValid, $"Validation errors: {string.Join("; ", result.Errors.Select(e => e.Message))}");
        Assert.Equal(ConformanceLevel.Minimal, result.ConformanceLevel);
    }

    [Fact]
    public void SpecWorks_ConvertedCatalog_AutoDetectsMinimal()
    {
        var json = File.ReadAllText(SpecWorksFixturePath);
        var catalog = MarketplaceConverter.Convert(json);

        var result = AiCatalogValidator.Validate(catalog);

        Assert.True(result.IsValid);
        // Without host, can't be Discoverable — should be Minimal
        Assert.Equal(ConformanceLevel.Minimal, result.ConformanceLevel);
    }

    [Fact]
    public void WorkIq_ConvertedCatalog_AutoDetectsMinimal()
    {
        var json = File.ReadAllText(WorkIqFixturePath);
        var catalog = MarketplaceConverter.Convert(json);

        var result = AiCatalogValidator.Validate(catalog);

        Assert.True(result.IsValid);
        Assert.Equal(ConformanceLevel.Minimal, result.ConformanceLevel);
    }

    #endregion

    #region CLI End-to-End Tests

    [Fact]
    public async Task Cli_ConvertSpecWorks_ExitCodeZero()
    {
        Assert.True(File.Exists(SpecWorksFixturePath), $"Fixture not found: {SpecWorksFixturePath}");

        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(ConvertCommand.Create());

        var console = new TestConsole();
        var exitCode = await rootCommand.InvokeAsync(
            $"convert marketplace \"{SpecWorksFixturePath}\"", console);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Cli_ConvertWorkIq_ExitCodeZero()
    {
        Assert.True(File.Exists(WorkIqFixturePath), $"Fixture not found: {WorkIqFixturePath}");

        var rootCommand = new RootCommand("AI Catalog CLI");
        rootCommand.AddCommand(ConvertCommand.Create());

        var console = new TestConsole();
        var exitCode = await rootCommand.InvokeAsync(
            $"convert marketplace \"{WorkIqFixturePath}\"", console);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Cli_ConvertSpecWorks_WritesToOutputFile()
    {
        Assert.True(File.Exists(SpecWorksFixturePath));

        var outputPath = Path.Combine(AppContext.BaseDirectory, "test-output-specworks.json");
        try
        {
            var rootCommand = new RootCommand("AI Catalog CLI");
            rootCommand.AddCommand(ConvertCommand.Create());

            var console = new TestConsole();
            var exitCode = await rootCommand.InvokeAsync(
                $"convert marketplace \"{SpecWorksFixturePath}\" --output \"{outputPath}\"", console);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));

            var content = File.ReadAllText(outputPath);
            Assert.Contains("specVersion", content);
            Assert.Contains("urn:marketplace:spec-works-plugins:markmyword", content);
            Assert.Contains("urn:marketplace:spec-works-plugins:a2a-ask", content);

            // Parse the output file to validate structure
            var catalog = AiCatalogParser.Parse(content);
            Assert.Equal(5, catalog.Entries.Count);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task Cli_ConvertWorkIq_WritesToOutputFile()
    {
        Assert.True(File.Exists(WorkIqFixturePath));

        var outputPath = Path.Combine(AppContext.BaseDirectory, "test-output-workiq.json");
        try
        {
            var rootCommand = new RootCommand("AI Catalog CLI");
            rootCommand.AddCommand(ConvertCommand.Create());

            var console = new TestConsole();
            var exitCode = await rootCommand.InvokeAsync(
                $"convert marketplace \"{WorkIqFixturePath}\" --output \"{outputPath}\"", console);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));

            var content = File.ReadAllText(outputPath);
            Assert.Contains("specVersion", content);
            Assert.Contains("urn:marketplace:work-iq:workiq", content);
            Assert.Contains("Microsoft", content);

            // Parse the output file to validate structure
            var catalog = AiCatalogParser.Parse(content);
            Assert.Equal(3, catalog.Entries.Count);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task Cli_ConvertSpecWorks_OutputFileConforms()
    {
        Assert.True(File.Exists(SpecWorksFixturePath));

        var outputPath = Path.Combine(AppContext.BaseDirectory, "test-output-specworks-conform.json");
        try
        {
            var rootCommand = new RootCommand("AI Catalog CLI");
            rootCommand.AddCommand(ConvertCommand.Create());

            var console = new TestConsole();
            await rootCommand.InvokeAsync(
                $"convert marketplace \"{SpecWorksFixturePath}\" --output \"{outputPath}\"", console);

            var content = File.ReadAllText(outputPath);
            var catalog = AiCatalogParser.Parse(content);
            var result = AiCatalogValidator.Validate(catalog, ConformanceLevel.Minimal);

            Assert.True(result.IsValid, $"CLI output validation errors: {string.Join("; ", result.Errors.Select(e => e.Message))}");
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task Cli_ConvertWorkIq_OutputFileConforms()
    {
        Assert.True(File.Exists(WorkIqFixturePath));

        var outputPath = Path.Combine(AppContext.BaseDirectory, "test-output-workiq-conform.json");
        try
        {
            var rootCommand = new RootCommand("AI Catalog CLI");
            rootCommand.AddCommand(ConvertCommand.Create());

            var console = new TestConsole();
            await rootCommand.InvokeAsync(
                $"convert marketplace \"{WorkIqFixturePath}\" --output \"{outputPath}\"", console);

            var content = File.ReadAllText(outputPath);
            var catalog = AiCatalogParser.Parse(content);
            var result = AiCatalogValidator.Validate(catalog, ConformanceLevel.Minimal);

            Assert.True(result.IsValid, $"CLI output validation errors: {string.Join("; ", result.Errors.Select(e => e.Message))}");
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    #endregion

    #region Stream-based conversion

    [Fact]
    public void SpecWorks_StreamConvert_MatchesStringConvert()
    {
        var json = File.ReadAllText(SpecWorksFixturePath);
        var fromString = MarketplaceConverter.Convert(json);

        using var stream = File.OpenRead(SpecWorksFixturePath);
        var fromStream = MarketplaceConverter.Convert(stream);

        Assert.Equal(fromString.Entries.Count, fromStream.Entries.Count);
        for (int i = 0; i < fromString.Entries.Count; i++)
        {
            Assert.Equal(fromString.Entries[i].Identifier, fromStream.Entries[i].Identifier);
            Assert.Equal(fromString.Entries[i].DisplayName, fromStream.Entries[i].DisplayName);
        }
    }

    [Fact]
    public void WorkIq_StreamConvert_MatchesStringConvert()
    {
        var json = File.ReadAllText(WorkIqFixturePath);
        var fromString = MarketplaceConverter.Convert(json);

        using var stream = File.OpenRead(WorkIqFixturePath);
        var fromStream = MarketplaceConverter.Convert(stream);

        Assert.Equal(fromString.Entries.Count, fromStream.Entries.Count);
        for (int i = 0; i < fromString.Entries.Count; i++)
        {
            Assert.Equal(fromString.Entries[i].Identifier, fromStream.Entries[i].Identifier);
            Assert.Equal(fromString.Entries[i].DisplayName, fromStream.Entries[i].DisplayName);
        }
    }

    #endregion
}
