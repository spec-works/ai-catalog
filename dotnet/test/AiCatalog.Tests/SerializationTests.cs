using System.Text.Json;
using SpecWorks.AiCatalog.Parsing;
using SpecWorks.AiCatalog.Serialization;
using Xunit;

namespace SpecWorks.AiCatalog.Tests;

/// <summary>
/// Round-trip tests: parse → serialize → parse, then compare.
/// </summary>
public class SerializationTests
{
    public static IEnumerable<object[]> PositiveFixtures => TestFixtureLoader.GetPositiveTestCases();

    [Theory]
    [MemberData(nameof(PositiveFixtures))]
    public void RoundTrip_PositiveFixture_ProducesEquivalentResult(string name, string fixtureJson)
    {
        var inputJson = TestFixtureLoader.GetInputJson(fixtureJson);
        var expected = TestFixtureLoader.GetExpected(fixtureJson);

        // Parse
        var catalog1 = AiCatalogParser.Parse(inputJson);

        // Serialize
        var serialized = AiCatalogSerializer.Serialize(catalog1);

        // Parse again
        var catalog2 = AiCatalogParser.Parse(serialized);

        // Compare structural equivalence
        Assert.Equal(catalog1.SpecVersion, catalog2.SpecVersion);
        Assert.Equal(catalog1.Entries.Count, catalog2.Entries.Count);

        // Verify same number of collections
        Assert.Equal(catalog1.Collections?.Count ?? 0, catalog2.Collections?.Count ?? 0);

        // Verify host round-trips
        Assert.Equal(catalog1.Host is not null, catalog2.Host is not null);
        if (catalog1.Host is not null && catalog2.Host is not null)
        {
            Assert.Equal(catalog1.Host.DisplayName, catalog2.Host.DisplayName);
            Assert.Equal(catalog1.Host.Identifier, catalog2.Host.Identifier);
        }

        // Verify entries round-trip
        for (int i = 0; i < catalog1.Entries.Count; i++)
        {
            var e1 = catalog1.Entries[i];
            var e2 = catalog2.Entries[i];

            Assert.Equal(e1.Identifier, e2.Identifier);
            Assert.Equal(e1.DisplayName, e2.DisplayName);
            Assert.Equal(e1.MediaType, e2.MediaType);
            Assert.Equal(e1.Url, e2.Url);
            Assert.Equal(e1.Version, e2.Version);
            Assert.Equal(e1.Description, e2.Description);
            Assert.Equal(e1.UpdatedAt, e2.UpdatedAt);

            // Inline round-trip
            bool e1HasInline = e1.Inline is not null && e1.Inline.Value.ValueKind != JsonValueKind.Undefined;
            bool e2HasInline = e2.Inline is not null && e2.Inline.Value.ValueKind != JsonValueKind.Undefined;
            Assert.Equal(e1HasInline, e2HasInline);
        }

        // Verify metadata round-trips
        if (expected.TryGetProperty("round_trip_preserves_metadata", out var preservesMeta) && preservesMeta.GetBoolean())
        {
            Assert.NotNull(catalog1.Metadata);
            Assert.NotNull(catalog2.Metadata);
            AssertJsonEquivalent(catalog1.Metadata.Value, catalog2.Metadata.Value);

            // Also check entry metadata
            for (int i = 0; i < catalog1.Entries.Count; i++)
            {
                if (catalog1.Entries[i].Metadata is not null)
                {
                    Assert.NotNull(catalog2.Entries[i].Metadata);
                    AssertJsonEquivalent(
                        catalog1.Entries[i].Metadata!.Value,
                        catalog2.Entries[i].Metadata!.Value);
                }
            }
        }
    }

    /// <summary>
    /// Compares two JsonElements for structural equivalence (ignoring whitespace differences).
    /// </summary>
    private static void AssertJsonEquivalent(JsonElement a, JsonElement b)
    {
        // Normalize by re-serializing both with consistent formatting
        var optionsCompact = new JsonSerializerOptions { WriteIndented = false };
        var jsonA = JsonSerializer.Serialize(a, optionsCompact);
        var jsonB = JsonSerializer.Serialize(b, optionsCompact);
        Assert.Equal(jsonA, jsonB);
    }

    [Fact]
    public void Serialize_NullCatalog_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => AiCatalogSerializer.Serialize(null!));
    }

    [Fact]
    public void Serialize_MinimalCatalog_OmitsNullFields()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "1.0",
            Entries = []
        };

        var json = AiCatalogSerializer.Serialize(catalog);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("specVersion", out _));
        Assert.True(root.TryGetProperty("entries", out _));
        Assert.False(root.TryGetProperty("host", out _));
        Assert.False(root.TryGetProperty("collections", out _));
        Assert.False(root.TryGetProperty("metadata", out _));
    }

    [Fact]
    public void Serialize_ToStream_ProducesValidJson()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "1.0",
            Entries = [new Models.CatalogEntry
            {
                Identifier = "urn:test:entry",
                DisplayName = "Test",
                MediaType = "application/json",
                Url = "https://example.com/test.json"
            }]
        };

        using var stream = new MemoryStream();
        AiCatalogSerializer.Serialize(catalog, stream);
        stream.Position = 0;
        var reparsed = AiCatalogParser.Parse(stream);

        Assert.Equal("1.0", reparsed.SpecVersion);
        Assert.Single(reparsed.Entries);
    }
}
