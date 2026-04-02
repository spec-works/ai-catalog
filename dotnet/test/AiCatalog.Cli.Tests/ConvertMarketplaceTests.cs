using System.Text.Json;
using SpecWorks.AiCatalog.Cli.Conversion;
using SpecWorks.AiCatalog.Serialization;
using Xunit;

namespace SpecWorks.AiCatalog.Cli.Tests;

/// <summary>
/// Integration tests for the <c>convert marketplace</c> command using shared test fixtures.
/// </summary>
public class ConvertMarketplaceTests
{
    private static readonly string TestcasesDir = Path.Combine(
        AppContext.BaseDirectory, "testcases");

    [Fact]
    public void Convert_MarketplaceInput_ProducesExpectedCatalog()
    {
        // Arrange: read the shared fixtures
        var inputPath = Path.Combine(TestcasesDir, "marketplace-input.json");
        var expectedPath = Path.Combine(TestcasesDir, "marketplace-expected.json");

        Assert.True(File.Exists(inputPath), $"Fixture not found: {inputPath}");
        Assert.True(File.Exists(expectedPath), $"Fixture not found: {expectedPath}");

        var inputJson = File.ReadAllText(inputPath);
        var expectedJson = File.ReadAllText(expectedPath);

        // The expected fixture wraps the catalog in {"input": {...}, "expected": {...}}
        using var expectedDoc = JsonDocument.Parse(expectedJson);
        var expectedCatalogElement = expectedDoc.RootElement.GetProperty("input");
        var expectedAssertions = expectedDoc.RootElement.GetProperty("expected");

        // Act: convert marketplace JSON to catalog
        var catalog = MarketplaceConverter.Convert(inputJson);

        // Assert: verify entry count
        var expectedEntryCount = expectedAssertions.GetProperty("entry_count").GetInt32();
        Assert.Equal(expectedEntryCount, catalog.Entries.Count);

        // Assert: specVersion
        var expectedSpecVersion = expectedAssertions.GetProperty("spec_version").GetString();
        Assert.Equal(expectedSpecVersion, catalog.SpecVersion);

        // Assert: all media types equal
        var expectedMediaType = expectedAssertions.GetProperty("all_media_types_equal").GetString();
        Assert.All(catalog.Entries, entry => Assert.Equal(expectedMediaType, entry.MediaType));

        // Assert: all entries have publisher
        var allHavePublisher = expectedAssertions.GetProperty("all_entries_have_publisher").GetBoolean();
        if (allHavePublisher)
        {
            Assert.All(catalog.Entries, entry => Assert.NotNull(entry.Publisher));
        }
    }

    [Fact]
    public void Convert_MarketplaceInput_MatchesExpectedEntries()
    {
        // Arrange
        var inputPath = Path.Combine(TestcasesDir, "marketplace-input.json");
        var expectedPath = Path.Combine(TestcasesDir, "marketplace-expected.json");
        var inputJson = File.ReadAllText(inputPath);
        var expectedJson = File.ReadAllText(expectedPath);

        using var expectedDoc = JsonDocument.Parse(expectedJson);
        var expectedEntries = expectedDoc.RootElement.GetProperty("input").GetProperty("entries");

        // Act
        var catalog = MarketplaceConverter.Convert(inputJson);

        // Assert: verify each entry matches field-by-field
        var expectedArray = expectedEntries.EnumerateArray().ToList();
        Assert.Equal(expectedArray.Count, catalog.Entries.Count);

        for (int i = 0; i < expectedArray.Count; i++)
        {
            var expected = expectedArray[i];
            var actual = catalog.Entries[i];

            Assert.Equal(expected.GetProperty("identifier").GetString(), actual.Identifier);
            Assert.Equal(expected.GetProperty("displayName").GetString(), actual.DisplayName);
            Assert.Equal(expected.GetProperty("mediaType").GetString(), actual.MediaType);
            Assert.Equal(expected.GetProperty("url").GetString(), actual.Url);

            if (expected.TryGetProperty("version", out var v))
                Assert.Equal(v.GetString(), actual.Version);

            if (expected.TryGetProperty("description", out var d))
                Assert.Equal(d.GetString(), actual.Description);

            if (expected.TryGetProperty("updatedAt", out var u))
                Assert.Equal(u.GetString(), actual.UpdatedAt);

            if (expected.TryGetProperty("tags", out var tags))
            {
                Assert.NotNull(actual.Tags);
                var expectedTags = tags.EnumerateArray().Select(t => t.GetString()!).ToList();
                Assert.Equal(expectedTags, actual.Tags);
            }

            if (expected.TryGetProperty("publisher", out var pub))
            {
                Assert.NotNull(actual.Publisher);
                Assert.Equal(pub.GetProperty("identifier").GetString(), actual.Publisher!.Identifier);
                Assert.Equal(pub.GetProperty("displayName").GetString(), actual.Publisher.DisplayName);
            }
        }
    }

    [Fact]
    public void Convert_MarketplaceInput_RoundTripsToValidJson()
    {
        // Arrange
        var inputPath = Path.Combine(TestcasesDir, "marketplace-input.json");
        var inputJson = File.ReadAllText(inputPath);

        // Act: convert and serialize
        var catalog = MarketplaceConverter.Convert(inputJson);
        var serializedJson = AiCatalogSerializer.Serialize(catalog);

        // Assert: can parse the output as valid JSON and re-parse as a catalog
        var reparsed = SpecWorks.AiCatalog.Parsing.AiCatalogParser.Parse(serializedJson);
        Assert.Equal(catalog.Entries.Count, reparsed.Entries.Count);
        Assert.Equal(catalog.SpecVersion, reparsed.SpecVersion);
    }

    [Fact]
    public void Convert_MappingRules_VerifiesIdentifierPattern()
    {
        // Verify the urn:claude:plugins:{name} pattern from TFD-005
        var inputPath = Path.Combine(TestcasesDir, "marketplace-input.json");
        var inputJson = File.ReadAllText(inputPath);

        using var inputDoc = JsonDocument.Parse(inputJson);
        var plugins = inputDoc.RootElement.GetProperty("input").GetProperty("plugins");

        var catalog = MarketplaceConverter.Convert(inputJson);

        int i = 0;
        foreach (var plugin in plugins.EnumerateArray())
        {
            var pluginName = plugin.GetProperty("name").GetString();
            var expectedId = $"urn:claude:plugins:{pluginName}";
            Assert.Equal(expectedId, catalog.Entries[i].Identifier);
            i++;
        }
    }

    [Fact]
    public void Convert_InvalidInput_ThrowsAiCatalogException()
    {
        Assert.Throws<AiCatalogException>(() => MarketplaceConverter.Convert("not json"));
    }

    [Fact]
    public void Convert_MissingPluginsArray_ThrowsAiCatalogException()
    {
        Assert.Throws<AiCatalogException>(() => MarketplaceConverter.Convert("{}"));
    }
}
