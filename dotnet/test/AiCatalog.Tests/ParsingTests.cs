using System.Text.Json;
using SpecWorks.AiCatalog.Parsing;
using Xunit;

namespace SpecWorks.AiCatalog.Tests;

/// <summary>
/// Tests that parse each positive test fixture and verify expected assertions.
/// </summary>
public class ParsingTests
{
    public static IEnumerable<object[]> PositiveFixtures => TestFixtureLoader.GetPositiveTestCases();

    [Theory]
    [MemberData(nameof(PositiveFixtures))]
    public void Parse_PositiveFixture_ProducesExpectedResult(string name, string fixtureJson)
    {
        var inputJson = TestFixtureLoader.GetInputJson(fixtureJson);
        var expected = TestFixtureLoader.GetExpected(fixtureJson);

        var catalog = AiCatalogParser.Parse(inputJson);

        Assert.NotNull(catalog);

        // Verify spec_version if expected
        if (expected.TryGetProperty("spec_version", out var specVersion))
        {
            Assert.Equal(specVersion.GetString(), catalog.SpecVersion);
        }

        // Verify entry_count
        if (expected.TryGetProperty("entry_count", out var entryCount))
        {
            Assert.Equal(entryCount.GetInt32(), catalog.Entries.Count);
        }

        // Verify collection_count
        if (expected.TryGetProperty("collection_count", out var collectionCount))
        {
            Assert.Equal(collectionCount.GetInt32(), catalog.Collections?.Count ?? 0);
        }

        // Verify has_host
        if (expected.TryGetProperty("has_host", out var hasHost))
        {
            Assert.Equal(hasHost.GetBoolean(), catalog.Host is not null);
        }

        // Verify has_metadata
        if (expected.TryGetProperty("has_metadata", out var hasMetadata))
        {
            Assert.Equal(hasMetadata.GetBoolean(), catalog.Metadata is not null);
        }

        // Verify host details
        if (expected.TryGetProperty("host", out var hostExpected) && catalog.Host is not null)
        {
            VerifyHostExpectations(hostExpected, catalog.Host);
        }

        // Verify entries details
        if (expected.TryGetProperty("entries", out var entriesExpected))
        {
            VerifyEntriesExpectations(entriesExpected, catalog.Entries);
        }
    }

    [Fact]
    public void Parse_NullString_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => AiCatalogParser.Parse((string)null!));
    }

    [Fact]
    public void Parse_NullStream_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => AiCatalogParser.Parse((Stream)null!));
    }

    [Fact]
    public void Parse_EmptyEntriesArray_Succeeds()
    {
        var json = """{"specVersion":"1.0","entries":[]}""";
        var catalog = AiCatalogParser.Parse(json);
        Assert.NotNull(catalog);
        Assert.Equal("1.0", catalog.SpecVersion);
        Assert.Empty(catalog.Entries);
    }

    [Fact]
    public void Parse_Stream_Succeeds()
    {
        var json = """{"specVersion":"1.0","entries":[]}""";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        var catalog = AiCatalogParser.Parse(stream);
        Assert.NotNull(catalog);
        Assert.Equal("1.0", catalog.SpecVersion);
    }

    private static void VerifyHostExpectations(JsonElement hostExpected, Models.HostInfo host)
    {
        if (hostExpected.TryGetProperty("display_name", out var dn))
            Assert.Equal(dn.GetString(), host.DisplayName);

        if (hostExpected.TryGetProperty("has_identifier", out var hasId))
            Assert.Equal(hasId.GetBoolean(), host.Identifier is not null);

        if (hostExpected.TryGetProperty("has_documentation_url", out var hasDocUrl))
            Assert.Equal(hasDocUrl.GetBoolean(), host.DocumentationUrl is not null);

        if (hostExpected.TryGetProperty("has_logo_url", out var hasLogo))
            Assert.Equal(hasLogo.GetBoolean(), host.LogoUrl is not null);

        if (hostExpected.TryGetProperty("has_trust_manifest", out var hasTm))
            Assert.Equal(hasTm.GetBoolean(), host.TrustManifest is not null);
    }

    private static void VerifyEntriesExpectations(JsonElement entriesExpected, List<Models.CatalogEntry> entries)
    {
        int idx = 0;
        foreach (var entryExpected in entriesExpected.EnumerateArray())
        {
            Assert.True(idx < entries.Count, $"Expected entry at index {idx} but only {entries.Count} entries exist");
            var entry = entries[idx];

            if (entryExpected.TryGetProperty("identifier", out var id))
                Assert.Equal(id.GetString(), entry.Identifier);

            if (entryExpected.TryGetProperty("display_name", out var dn))
                Assert.Equal(dn.GetString(), entry.DisplayName);

            if (entryExpected.TryGetProperty("media_type", out var mt))
                Assert.Equal(mt.GetString(), entry.MediaType);

            if (entryExpected.TryGetProperty("has_url", out var hasUrl))
                Assert.Equal(hasUrl.GetBoolean(), entry.Url is not null);

            if (entryExpected.TryGetProperty("has_inline", out var hasInline))
            {
                bool actualHasInline = entry.Inline is not null && entry.Inline.Value.ValueKind != JsonValueKind.Undefined;
                Assert.Equal(hasInline.GetBoolean(), actualHasInline);
            }

            if (entryExpected.TryGetProperty("has_version", out var hasVersion))
                Assert.Equal(hasVersion.GetBoolean(), entry.Version is not null);

            if (entryExpected.TryGetProperty("has_description", out var hasDesc))
                Assert.Equal(hasDesc.GetBoolean(), entry.Description is not null);

            if (entryExpected.TryGetProperty("has_tags", out var hasTags))
                Assert.Equal(hasTags.GetBoolean(), entry.Tags is not null && entry.Tags.Count > 0);

            if (entryExpected.TryGetProperty("has_updated_at", out var hasUpdated))
                Assert.Equal(hasUpdated.GetBoolean(), entry.UpdatedAt is not null);

            if (entryExpected.TryGetProperty("has_publisher", out var hasPub))
                Assert.Equal(hasPub.GetBoolean(), entry.Publisher is not null);

            if (entryExpected.TryGetProperty("has_trust_manifest", out var hasTm))
                Assert.Equal(hasTm.GetBoolean(), entry.TrustManifest is not null);

            if (entryExpected.TryGetProperty("has_metadata", out var hasMeta))
                Assert.Equal(hasMeta.GetBoolean(), entry.Metadata is not null);

            if (entryExpected.TryGetProperty("inline_is_object", out var isObj) && isObj.GetBoolean())
            {
                Assert.NotNull(entry.Inline);
                Assert.Equal(JsonValueKind.Object, entry.Inline.Value.ValueKind);
            }

            if (entryExpected.TryGetProperty("inline_is_string", out var isStr) && isStr.GetBoolean())
            {
                Assert.NotNull(entry.Inline);
                Assert.Equal(JsonValueKind.String, entry.Inline.Value.ValueKind);
            }

            if (entryExpected.TryGetProperty("trust_manifest_identity_matches", out var tmMatch) && tmMatch.GetBoolean())
            {
                Assert.NotNull(entry.TrustManifest);
                Assert.Equal(entry.Identifier, entry.TrustManifest.Identity);
            }

            if (entryExpected.TryGetProperty("publisher_has_identity_type", out var pubIdType) && pubIdType.GetBoolean())
            {
                Assert.NotNull(entry.Publisher);
                Assert.NotNull(entry.Publisher.IdentityType);
            }

            if (entryExpected.TryGetProperty("attestation_count", out var attCount))
            {
                Assert.NotNull(entry.TrustManifest);
                Assert.Equal(attCount.GetInt32(), entry.TrustManifest.Attestations?.Count ?? 0);
            }

            if (entryExpected.TryGetProperty("provenance_count", out var provCount))
            {
                Assert.NotNull(entry.TrustManifest);
                Assert.Equal(provCount.GetInt32(), entry.TrustManifest.Provenance?.Count ?? 0);
            }

            // Bundle assertions
            if (entryExpected.TryGetProperty("is_bundle", out var isBundle) && isBundle.GetBoolean())
            {
                Assert.Equal("application/ai-catalog+json", entry.MediaType);
                Assert.NotNull(entry.Inline);
                Assert.Equal(JsonValueKind.Object, entry.Inline.Value.ValueKind);

                if (entryExpected.TryGetProperty("nested_spec_version", out var nsv))
                {
                    Assert.True(entry.Inline.Value.TryGetProperty("specVersion", out var sv));
                    Assert.Equal(nsv.GetString(), sv.GetString());
                }

                if (entryExpected.TryGetProperty("nested_entry_count", out var nec))
                {
                    Assert.True(entry.Inline.Value.TryGetProperty("entries", out var ne));
                    Assert.Equal(nec.GetInt32(), ne.GetArrayLength());
                }
            }

            idx++;
        }
    }
}
