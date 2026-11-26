using System.Text.Json;
using System.Text.Json.Nodes;
using SpecWorks.AiCatalog.Parsing;
using SpecWorks.AiCatalog.Serialization;
using SpecWorks.AiCatalog.Validation;
using Xunit;

namespace SpecWorks.AiCatalog.Tests;

public sealed class V09FixtureTests
{
    public static IEnumerable<object[]> Fixtures => TestFixtureLoader.GetV09TestCases();

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void V09Fixture_ValidatesAccordingToExpectedOutcome(string name, string fixtureJson)
    {
        var inputJson = TestFixtureLoader.GetInputJson(fixtureJson);
        using var fixture = JsonDocument.Parse(fixtureJson);
        var root = fixture.RootElement;
        var catalog = AiCatalogParser.Parse(inputJson);
        var result = AiCatalogValidator.Validate(catalog);

        if (root.TryGetProperty("expected", out var expected))
        {
            if (expected.TryGetProperty("valid", out var expectedValidity))
                Assert.Equal(expectedValidity.GetBoolean(), result.IsValid);
            Assert.True(result.IsValid,
                $"{name}: {string.Join("; ", result.Errors.Select(error => error.Message))}");
            Assert.True(result.ConformanceLevel <= ConformanceLevel.Discoverable);
            if (expected.TryGetProperty("entry_count", out var expectedEntryCount))
                Assert.Equal(expectedEntryCount.GetInt32(), catalog.Entries.Count);
            if (expected.TryGetProperty("conformance_level", out var expectedLevel))
            {
                var conformanceLevel = expectedLevel.GetString() switch
                {
                    "level_1_minimal" or "minimal" => ConformanceLevel.Minimal,
                    "level_2_discoverable" or "discoverable" => ConformanceLevel.Discoverable,
                    _ => throw new InvalidOperationException($"Unknown conformance level: {expectedLevel.GetString()}")
                };
                Assert.Equal(conformanceLevel, result.ConformanceLevel);
            }

            if (expected.TryGetProperty("data_json_kinds", out var expectedKinds))
            {
                var actualKinds = catalog.Entries
                    .Where(entry => entry.Data is not null)
                    .Select(entry => entry.Data!.Value.ValueKind switch
                    {
                        JsonValueKind.True or JsonValueKind.False => "boolean",
                        var kind => kind.ToString().ToLowerInvariant()
                    });
                Assert.Equal(expectedKinds.EnumerateArray().Select(value => value.GetString()), actualKinds);
            }
            if (name == "trust-manifests-wire-shape")
            {
                Assert.NotEmpty(result.Warnings);
                Assert.Contains(result.Warnings, warning => warning.Message.Contains("cryptographic verification"));
            }
        }
        else
        {
            Assert.False(result.IsValid, $"{name} should fail structural validation");
            var expectedError = root.GetProperty("expected_error");
            var expectedMember = expectedError.TryGetProperty("member", out var member)
                ? member.GetString()
                : expectedError.TryGetProperty("code", out var code)
                    ? code.GetString() switch
                    {
                        "exactly_one_content_source" => "exactly one",
                        "duplicate_entry_key" => "duplicate",
                        "missing_required_member" => "missing required field",
                        _ => null
                    }
                    : null;
            if (expectedMember is not null)
                Assert.Contains(result.Errors, error =>
                    $"{error.Message} {error.Path}".Contains(expectedMember, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void V09Fixture_RoundTripsCurrentFieldsAndUnknownMembers(string name, string fixtureJson)
    {
        using var fixture = JsonDocument.Parse(fixtureJson);
        if (!fixture.RootElement.TryGetProperty("expected", out _))
            return;

        var originalJson = TestFixtureLoader.GetInputJson(fixtureJson);
        var first = AiCatalogParser.Parse(originalJson);
        var serialized = AiCatalogSerializer.Serialize(first);
        var second = AiCatalogParser.Parse(serialized);

        Assert.Equal(first.SpecVersion, second.SpecVersion);
        Assert.Equal(first.Entries.Count, second.Entries.Count);
        for (var i = 0; i < first.Entries.Count; i++)
        {
            Assert.Equal(first.Entries[i].Type, second.Entries[i].Type);
            Assert.Equal(first.Entries[i].Data?.ValueKind, second.Entries[i].Data?.ValueKind);
            if (first.Entries[i].Data is { } firstData && second.Entries[i].Data is { } secondData)
                Assert.True(JsonNode.DeepEquals(JsonNode.Parse(firstData.GetRawText()), JsonNode.Parse(secondData.GetRawText())));
            Assert.Equal(first.Entries[i].Digest, second.Entries[i].Digest);
            Assert.Equal(first.Entries[i].TrustManifests?.Count, second.Entries[i].TrustManifests?.Count);
        }

        if (name == "trust-manifests-wire-shape")
        {
            var entry = Assert.Single(second.Entries);
            var manifest = Assert.Single(entry.TrustManifests!);
            Assert.Equal("did:web:contoso.com", manifest.Contributor);
            Assert.Equal(entry.Identifier, manifest.Subject!.Identifier);
            Assert.Equal(entry.Type, manifest.Subject.Type);
            Assert.Equal(entry.Version, manifest.Subject.Version);
            Assert.Equal(entry.Digest, manifest.Subject.Digest);
            Assert.Equal("did-web-v1", manifest.Signature!.Profile);
            Assert.Equal(manifest.Contributor, manifest.Signature.Signer);

            var structural = AiCatalogValidator.Validate(second);
            Assert.NotEqual(ConformanceLevel.Trusted, structural.ConformanceLevel);
            var trustedTarget = AiCatalogValidator.Validate(second, ConformanceLevel.Trusted);
            Assert.NotEqual(ConformanceLevel.Trusted, trustedTarget.ConformanceLevel);
            Assert.False(trustedTarget.IsValid);
            Assert.Contains(trustedTarget.Errors, error => error.Message.Contains("cryptographic verification"));
        }

        if (name == "extensions-and-unrecognized-members")
        {
            Assert.NotNull(second.Extensions);
            Assert.NotNull(second.ExtensionData);
            Assert.NotNull(second.Entries[0].Extensions);
            Assert.NotNull(second.Entries[0].ExtensionData);
            using var serializedDocument = JsonDocument.Parse(serialized);
            Assert.True(serializedDocument.RootElement.TryGetProperty("experimentalCatalogNote", out _));
            Assert.True(serializedDocument.RootElement.GetProperty("entries")[0]
                .TryGetProperty("experimentalEntryNote", out _));
        }
    }

    [Fact]
    public void Parse_DataNull_IsPresentAndRoundTrips()
    {
        const string json = """{"specVersion":"0.9","entries":[{"identifier":"urn:test:null","type":"application/json","data":null}]}""";
        var catalog = AiCatalogParser.Parse(json);
        Assert.Equal(JsonValueKind.Null, Assert.Single(catalog.Entries).Data!.Value.ValueKind);

        var roundTripped = AiCatalogParser.Parse(AiCatalogSerializer.Serialize(catalog));
        Assert.Equal(JsonValueKind.Null, Assert.Single(roundTripped.Entries).Data!.Value.ValueKind);
    }

    [Fact]
    public void AdditionalSignatures_AcceptsAndPreservesAnyJsonValue()
    {
        const string json = """{"specVersion":"0.9","entries":[{"identifier":"urn:test:signatures","type":"application/json","url":"https://example.com/item.json","trustManifests":[{"contributor":"urn:test:contributor","trustSchema":{"identifier":"urn:test:schema","version":"1"},"additionalSignatures":null}]}],"additionalSignatures":["ignored",42,{"x":true}]}""";
        var catalog = AiCatalogParser.Parse(json);

        Assert.Equal(JsonValueKind.Array, catalog.AdditionalSignatures!.Value.ValueKind);
        var serializedJson = AiCatalogSerializer.Serialize(catalog);
        using var serialized = JsonDocument.Parse(serializedJson);
        Assert.Equal(JsonValueKind.Array,
            serialized.RootElement.GetProperty("additionalSignatures").ValueKind);
        var manifest = serialized.RootElement.GetProperty("entries")[0]
            .GetProperty("trustManifests")[0];
        Assert.Equal(JsonValueKind.Null, manifest.GetProperty("additionalSignatures").ValueKind);
    }

    [Fact]
    public void LegacyV09Names_ArePreservedAsUnknownButDoNotSatisfyCurrentFields()
    {
        const string json = """{"specVersion":"0.9","entries":[{"identifier":"urn:test:legacy","mediaType":"application/json","url":"https://example.com/item.json","trustManifest":{"identity":"urn:test:legacy"}}],"metadata":{"legacy":true}}""";
        var catalog = AiCatalogParser.Parse(json);
        var entry = Assert.Single(catalog.Entries);

        Assert.Null(entry.Type);
        Assert.Null(entry.TrustManifests);
        Assert.Null(catalog.Metadata);
        Assert.Contains("mediaType", entry.ExtensionData!.Keys);
        Assert.Contains("trustManifest", entry.ExtensionData.Keys);
        Assert.Contains("metadata", catalog.ExtensionData!.Keys);

        var result = AiCatalogValidator.Validate(catalog);
        Assert.Contains(result.Errors, error => error.Path == "entries[0].type");
    }

    [Fact]
    public void DefinedOptionalMembers_RoundTripThroughTheirTypedProperties()
    {
        const string json = """
        {
          "specVersion":"0.9",
          "entries":[{
            "identifier":"urn:test:defined-members",
            "type":"application/json",
            "url":"https://example.com/item.json",
            "publisher":{
              "identifier":"did:web:example.com",
              "displayName":"Example",
              "identityType":"did"
            },
            "trustManifests":[{
              "contributor":"did:web:example.com",
              "trustSchema":{
                "identifier":"urn:test:schema",
                "version":"1",
                "governanceUri":"https://example.com/governance",
                "verificationMethods":["did","x509"]
              },
              "provenance":[{
                "relation":"publishedFrom",
                "sourceId":"https://example.com/source",
                "sourceDigest":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
                "registryUri":"https://example.com/registry",
                "statementUri":"https://example.com/statement",
                "signatureRef":"did:web:example.com#key-1"
              }]
            }]
          }]
        }
        """;

        var catalog = AiCatalogParser.Parse(json);
        var entry = Assert.Single(catalog.Entries);
        Assert.Equal("did", entry.Publisher!.IdentityType);
        var manifest = Assert.Single(entry.TrustManifests!);
        Assert.Equal("https://example.com/governance", manifest.TrustSchema!.GovernanceUri);
        Assert.Equal(["did", "x509"], manifest.TrustSchema.VerificationMethods);
        var provenance = Assert.Single(manifest.Provenance!);
        Assert.Equal("sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            provenance.SourceDigest);
        Assert.Equal("https://example.com/registry", provenance.RegistryUri);
        Assert.Equal("https://example.com/statement", provenance.StatementUri);
        Assert.Equal("did:web:example.com#key-1", provenance.SignatureRef);

        var roundTripped = AiCatalogParser.Parse(AiCatalogSerializer.Serialize(catalog));
        var serializedEntry = Assert.Single(roundTripped.Entries);
        Assert.Equal("did", serializedEntry.Publisher!.IdentityType);
        Assert.Equal("https://example.com/governance",
            serializedEntry.TrustManifests![0].TrustSchema!.GovernanceUri);
        Assert.Equal("https://example.com/registry",
            serializedEntry.TrustManifests[0].Provenance![0].RegistryUri);
    }

    [Fact]
    public void HostExtensionsMember_IsPreservedAsUnrecognizedArbitraryJson()
    {
        const string json = """
        {
          "specVersion":"0.9",
          "host":{"displayName":"Example","extensions":["not","a","map"]},
          "entries":[]
        }
        """;

        var catalog = AiCatalogParser.Parse(json);
        var result = AiCatalogValidator.Validate(catalog);
        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(error => error.Message)));

        var hostExtensions = catalog.Host!.ExtensionData!["extensions"];
        Assert.Equal(JsonValueKind.Array, hostExtensions.ValueKind);

        var serialized = AiCatalogSerializer.Serialize(catalog);
        using var document = JsonDocument.Parse(serialized);
        Assert.Equal(JsonValueKind.Array,
            document.RootElement.GetProperty("host").GetProperty("extensions").ValueKind);
    }
}
