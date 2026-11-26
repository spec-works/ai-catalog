using System.Text.Json;
using SpecWorks.AiCatalog.Parsing;
using SpecWorks.AiCatalog.Serialization;
using SpecWorks.AiCatalog.Validation;
using Xunit;

namespace SpecWorks.AiCatalog.Tests;

/// <summary>
/// Tests for conformance level detection and validation.
/// </summary>
public class ValidationTests
{
    public static IEnumerable<object[]> PositiveFixtures => TestFixtureLoader.GetPositiveTestCases();

    [Theory]
    [MemberData(nameof(PositiveFixtures))]
    public void Validate_PositiveFixture_DetectsCorrectConformanceLevel(string name, string fixtureJson)
    {
        var inputJson = TestFixtureLoader.GetInputJson(fixtureJson);
        var expected = TestFixtureLoader.GetExpected(fixtureJson);

        var catalog = AiCatalogParser.Parse(inputJson);
        var result = AiCatalogValidator.Validate(catalog);

        if (expected.TryGetProperty("valid", out var valid) && valid.GetBoolean())
        {
            Assert.True(result.IsValid,
                $"Expected valid catalog for '{name}', got errors: [{string.Join(", ", result.Errors.Select(e => e.Message))}]");
        }

        if (expected.TryGetProperty("conformance_level", out var levelProp))
        {
            var expectedLevel = levelProp.GetString() switch
            {
                "minimal" => ConformanceLevel.Minimal,
                "level_1_minimal" => ConformanceLevel.Minimal,
                "discoverable" => ConformanceLevel.Discoverable,
                "trusted" => ConformanceLevel.Trusted,
                _ => throw new InvalidOperationException($"Unknown conformance level: {levelProp.GetString()}")
            };

            Assert.Equal(expectedLevel, result.ConformanceLevel);
        }
    }

    [Fact]
    public void Validate_MinimalCatalog_IsMinimalLevel()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "0.9",
            Entries = [new Models.CatalogEntry
            {
                Identifier = "urn:test:entry",
                DisplayName = "Test Entry",
                MediaType = "application/json",
                Url = "https://example.com/test.json"
            }]
        };

        var result = AiCatalogValidator.Validate(catalog);
        Assert.True(result.IsValid);
        Assert.Equal(ConformanceLevel.Minimal, result.ConformanceLevel);
    }

    [Fact]
    public void Validate_DiscoverableCatalog_IsDiscoverableLevel()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "0.9",
            Host = new Models.HostInfo { DisplayName = "Test Host" },
            Entries = [new Models.CatalogEntry
            {
                Identifier = "urn:test:entry",
                DisplayName = "Test Entry",
                MediaType = "application/json",
                Url = "https://example.com/test.json"
            }]
        };

        var result = AiCatalogValidator.Validate(catalog);
        Assert.True(result.IsValid);
        Assert.Equal(ConformanceLevel.Discoverable, result.ConformanceLevel);
    }

    [Fact]
    public void Validate_TrustManifestPresence_DoesNotEstablishTrustedLevel()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "0.9",
            Host = new Models.HostInfo
            {
                DisplayName = "Test Host",
            },
            Entries = [new Models.CatalogEntry
            {
                Identifier = "urn:test:entry",
                Type = "application/json",
                Url = "https://example.com/test.json"
            }]
        };
        catalog.Entries[0].TrustManifests =
        [
            new Models.TrustManifest
            {
                Contributor = "did:web:test.com",
                TrustSchema = new Models.TrustSchema { Identifier = "urn:test:schema", Version = "1" }
            }
        ];

        var result = AiCatalogValidator.Validate(catalog);
        Assert.True(result.IsValid);
        Assert.Equal(ConformanceLevel.Discoverable, result.ConformanceLevel);
        Assert.Contains(result.Warnings, warning => warning.Message.Contains("cryptographic verification"));
        var trustedTarget = AiCatalogValidator.Validate(catalog, ConformanceLevel.Trusted);
        Assert.False(trustedTarget.IsValid);
        Assert.NotEqual(ConformanceLevel.Trusted, trustedTarget.ConformanceLevel);
        Assert.Contains(trustedTarget.Errors, error => error.Message.Contains("cryptographic verification"));
    }

    [Fact]
    public void Validate_MissingEntryContent_ReportsError()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "0.9",
            Entries = [new Models.CatalogEntry
            {
                Identifier = "urn:test:entry",
                DisplayName = "Test Entry",
                MediaType = "application/json",
                // No url, no data
            }]
        };

        var result = AiCatalogValidator.Validate(catalog);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("url") && e.Message.Contains("data"));
    }

    [Fact]
    public void Validate_BothUrlAndData_ReportsError()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "0.9",
            Entries = [new Models.CatalogEntry
            {
                Identifier = "urn:test:entry",
                DisplayName = "Test Entry",
                MediaType = "application/json",
                Url = "https://example.com/test.json",
                Data = JsonDocument.Parse("{}").RootElement,
            }]
        };

        var result = AiCatalogValidator.Validate(catalog);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("both"));
    }

    [Fact]
    public void Validate_ExplicitNullUrlWithData_IsNotTreatedAsAbsent()
    {
        var catalog = AiCatalogParser.Parse("""
        {"specVersion":"0.9","entries":[{"identifier":"urn:test:null-url","type":"application/json","url":null,"data":null}]}
        """);

        var result = AiCatalogValidator.Validate(catalog);

        Assert.Contains(result.Errors, error => error.Path == "entries[0]" &&
            error.Message.Contains("found both"));
        Assert.Contains(result.Errors, error => error.Path == "entries[0].url" &&
            error.Message.Contains("must be a string"));
    }

    [Fact]
    public void Validate_AbsentUrlWithExplicitNullData_IsValid()
    {
        var catalog = AiCatalogParser.Parse("""
        {"specVersion":"0.9","entries":[{"identifier":"urn:test:null-data","type":"application/json","data":null}]}
        """);

        Assert.True(AiCatalogValidator.Validate(catalog).IsValid);
    }

    [Fact]
    public void Validate_NullVersion_IsRejectedInsteadOfTreatedAsOmitted()
    {
        var catalog = AiCatalogParser.Parse("""
        {"specVersion":"0.9","entries":[{"identifier":"urn:test:null-version","type":"application/json","url":"https://example.com/item.json","version":null}]}
        """);

        var result = AiCatalogValidator.Validate(catalog);

        Assert.Contains(result.Errors, error => error.Path == "entries[0].version" &&
            error.Message.Contains("omitted instead of set to null"));
    }

    [Fact]
    public void Validate_ExplicitNullCacheTracksMutableModelChanges()
    {
        var catalog = AiCatalogParser.Parse("""
        {
          "specVersion":"0.9",
          "host":{"displayName":"Example","identifier":null},
          "entries":[{
            "identifier":"urn:test:null-version",
            "type":"application/json",
            "url":"https://example.com/item.json",
            "version":null,
            "description":null,
            "trustManifests":[{
              "contributor":"did:web:example.com",
              "attestations":[{"type":"report","uri":"https://example.com/report.pdf","description":null}]
            }]
          }]
        }
        """);

        Assert.Contains(AiCatalogValidator.Validate(catalog).Errors,
            error => error.Path == "entries[0].version");

        catalog.Host!.Identifier = "did:web:example.com";
        catalog.Host.Identifier = null;
        catalog.Entries[0].Version = "1.0";
        catalog.Entries[0].Version = null;
        catalog.Entries[0].Description = "A description";
        catalog.Entries[0].Description = null;
        catalog.Entries[0].TrustManifests![0].Attestations![0].Description = "Annual report";
        catalog.Entries[0].TrustManifests[0].Attestations[0].Description = null;

        var result = AiCatalogValidator.Validate(catalog);

        Assert.DoesNotContain(result.Errors, error =>
            error.Path is "host.identifier" or "entries[0].version" or "entries[0].description" or
                "entries[0].trustManifests[0].attestations[0].description");
        using var serialized = JsonDocument.Parse(AiCatalogSerializer.Serialize(catalog));
        Assert.False(serialized.RootElement.GetProperty("entries")[0].TryGetProperty("version", out _));
    }

    [Fact]
    public void Validate_ExplicitNullUrlMutationClearsPresenceMarker()
    {
        var catalog = AiCatalogParser.Parse("""
        {"specVersion":"0.9","entries":[{"identifier":"urn:test:url-mutation","type":"application/json","url":null,"data":null}]}
        """);

        catalog.Entries[0].Url = "https://example.com/item.json";
        catalog.Entries[0].Url = null;
        var result = AiCatalogValidator.Validate(catalog);

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(error => error.Message)));
    }

    [Fact]
    public void Validate_NullOptionalTypedHostMember_IsRejectedButArbitraryDataNullRemainsAllowed()
    {
        var catalog = AiCatalogParser.Parse("""
        {
          "specVersion":"0.9",
          "host":{"displayName":"Example","identifier":null},
          "entries":[{"identifier":"urn:test:null-host","type":"application/json","data":null}]
        }
        """);

        var result = AiCatalogValidator.Validate(catalog);

        Assert.Contains(result.Errors, error => error.Path == "host.identifier");
        Assert.Equal(JsonValueKind.Null, catalog.Entries[0].Data!.Value.ValueKind);
    }

    [Fact]
    public void Validate_DuplicateIdentifierNoVersion_ReportsError()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "0.9",
            Entries =
            [
                new Models.CatalogEntry
                {
                    Identifier = "urn:test:dup",
                    DisplayName = "First",
                    MediaType = "application/json",
                    Url = "https://example.com/1.json"
                },
                new Models.CatalogEntry
                {
                    Identifier = "urn:test:dup",
                    DisplayName = "Second",
                    MediaType = "application/json",
                    Url = "https://example.com/2.json"
                }
            ]
        };

        var result = AiCatalogValidator.Validate(catalog);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("duplicate"));
    }

    [Fact]
    public void Validate_TrustSubjectMismatch_ReportsError()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "0.9",
            Entries = [new Models.CatalogEntry
            {
                Identifier = "urn:test:real",
                Type = "application/json",
                Url = "https://example.com/real.json",
                TrustManifests =
                [
                    new Models.TrustManifest
                    {
                        Contributor = "urn:test:contributor",
                        TrustSchema = new Models.TrustSchema { Identifier = "urn:test:schema", Version = "1" },
                        Subject = new Models.Subject
                        {
                            Identifier = "urn:test:different",
                            Type = "application/json",
                            Digest = "sha256:example"
                        }
                    }
                ]
            }]
        };

        var result = AiCatalogValidator.Validate(catalog);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("subject.identifier does not match"));
    }

    [Fact]
    public void Validate_DigestPresence_DoesNotClaimCryptographicVerification()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "0.9",
            Host = new Models.HostInfo { DisplayName = "Test" },
            Entries = [new Models.CatalogEntry
            {
                Identifier = "urn:test:weak",
                Type = "application/json",
                Url = "https://example.com/weak.json",
                Digest = "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
                TrustManifests =
                [
                    new Models.TrustManifest
                {
                    Contributor = "urn:test:contributor",
                    TrustSchema = new Models.TrustSchema { Identifier = "urn:test:schema", Version = "1" }
                }
                ]
            }]
        };

        var result = AiCatalogValidator.Validate(catalog);
        Assert.True(result.IsValid);
        Assert.NotEqual(ConformanceLevel.Trusted, result.ConformanceLevel);
        Assert.Contains(result.Warnings, warning => warning.Message.Contains("cryptographic verification"));
    }

    [Fact]
    public void Validate_WeakOrMalformedDigest_IsRejectedStructurally()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "0.9",
            Entries =
            [
                new Models.CatalogEntry
                {
                    Identifier = "urn:test:weak-digest",
                    Type = "application/json",
                    Url = "https://example.com/weak.json",
                    Digest = "sha1:0123456789abcdef0123456789abcdef01234567"
                }
            ]
        };

        var result = AiCatalogValidator.Validate(catalog);
        Assert.Contains(result.Errors, error =>
            error.Path == "entries[0].digest" && error.Message.Contains("shorter than SHA-256"));

        catalog.Entries[0].Digest = "sha128:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        result = AiCatalogValidator.Validate(catalog);
        Assert.Contains(result.Errors, error =>
            error.Path == "entries[0].digest" && error.Message.Contains("shorter than SHA-256"));

        catalog.Entries[0].Digest = "sha256:not-hex";
        result = AiCatalogValidator.Validate(catalog);
        Assert.Contains(result.Errors, error =>
            error.Path == "entries[0].digest" && error.Message.Contains("lowercase-hex"));
    }

    [Fact]
    public void Validate_NegativeAttestationSize_IsRejected()
    {
        var catalog = AiCatalogParser.Parse("""
        {
          "specVersion":"0.9",
          "entries":[{
            "identifier":"urn:test:negative-size",
            "type":"application/json",
            "url":"https://example.com/item.json",
            "trustManifests":[{
              "contributor":"did:web:example.com",
              "attestations":[{"type":"report","uri":"https://example.com/report.pdf","size":-1}]
            }]
          }]
        }
        """);

        var result = AiCatalogValidator.Validate(catalog);

        Assert.Contains(result.Errors, error =>
            error.Path == "entries[0].trustManifests[0].attestations[0].size" &&
            error.Message.Contains("unsigned integer"));
    }

    [Fact]
    public void Validate_UnsignedManifestSubjectDigestMustMatchEntry()
    {
        var catalog = AiCatalogParser.Parse("""
        {
          "specVersion":"0.9",
          "entries":[{
            "identifier":"urn:test:subject-digest",
            "type":"application/json",
            "url":"https://example.com/item.json",
            "digest":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            "trustManifests":[{
              "contributor":"did:web:example.com",
              "subject":{
                "identifier":"urn:test:subject-digest",
                "type":"application/json",
                "digest":"sha256:abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789"
              },
              "trustSchema":{"identifier":"urn:test:schema","version":"1"}
            }]
          }]
        }
        """);

        var result = AiCatalogValidator.Validate(catalog);
        Assert.Contains(result.Errors, error => error.Message.Contains("subject.digest does not match entry.digest"));
    }

    [Fact]
    public void Validate_SignedManifestRequiresExactContributorSignerAndLaterExpiry()
    {
        var catalog = AiCatalogParser.Parse("""
        {
          "specVersion":"0.9",
          "entries":[{
            "identifier":"urn:test:signature",
            "type":"application/json",
            "url":"https://example.com/item.json",
            "digest":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            "trustManifests":[{
              "contributor":"did:web:example.com",
              "subject":{
                "identifier":"urn:test:signature",
                "type":"application/json",
                "digest":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
              },
              "signature":{
                "signer":"did:web:other.example",
                "profile":"did-web-v1",
                "issuedAt":"2026-09-12T10:00:00Z",
                "expiresAt":"2026-09-12T10:00:00Z",
                "jws":"not-verified"
              }
            }]
          }]
        }
        """);

        var result = AiCatalogValidator.Validate(catalog);
        Assert.Contains(result.Errors, error => error.Path?.EndsWith(".signature.signer") == true);
        Assert.Contains(result.Errors, error => error.Path?.EndsWith(".signature.expiresAt") == true);
        Assert.NotEqual(ConformanceLevel.Trusted, result.ConformanceLevel);
    }

    [Fact]
    public void Validate_ContributorAndSignerMustBeAbsoluteUris()
    {
        var catalog = AiCatalogParser.Parse("""
        {
          "specVersion":"0.9",
          "entries":[{
            "identifier":"urn:test:relative-identities",
            "type":"application/json",
            "url":"https://example.com/item.json",
            "trustManifests":[{
              "contributor":"relative-contributor",
              "signature":{
                "signer":"relative-signer",
                "profile":"profile",
                "issuedAt":"2026-09-12T10:00:00Z",
                "jws":"value"
              }
            }]
          }]
        }
        """);

        var result = AiCatalogValidator.Validate(catalog);
        Assert.Contains(result.Errors, error => error.Path?.EndsWith(".contributor") == true &&
            error.Message.Contains("absolute URI"));
        Assert.Contains(result.Errors, error => error.Path?.EndsWith(".signature.signer") == true &&
            error.Message.Contains("absolute URI"));
    }

    [Fact]
    public void Validate_ValidateAgainstLevel_ReportsErrorsForLevel()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "0.9",
            Entries = [new Models.CatalogEntry
            {
                Identifier = "urn:test:entry",
                DisplayName = "Test Entry",
                MediaType = "application/json",
                Url = "https://example.com/test.json"
            }]
        };

        var result = AiCatalogValidator.Validate(catalog, ConformanceLevel.Discoverable);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("host"));
    }

    [Fact]
    public void Validate_HttpUrl_DoesNotApplyOutOfScopeFetchPolicy()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "0.9",
            Entries = [new Models.CatalogEntry
            {
                Identifier = "urn:test:insecure",
                Type = "application/json",
                Url = "http://example.com/insecure.json"
            }]
        };

        var result = AiCatalogValidator.Validate(catalog);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_InvalidUpdatedAt_ReportsError()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "0.9",
            Entries = [new Models.CatalogEntry
            {
                Identifier = "urn:test:bad-date",
                DisplayName = "Bad Date",
                MediaType = "application/json",
                Url = "https://example.com/bad.json",
                UpdatedAt = "2026-03-15"
            }]
        };

        var result = AiCatalogValidator.Validate(catalog);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("RFC 3339"));
    }

    [Fact]
    public void Validate_UnknownTopLevelFields_SilentlyIgnored()
    {
        // VH-2: Unknown fields within same major version MUST be ignored
        var json = """
        {
            "specVersion": "0.9",
            "entries": [],
            "x-custom": "value"
        }
        """;
        var catalog = AiCatalogParser.Parse(json);
        var result = AiCatalogValidator.Validate(catalog);

        Assert.True(result.IsValid);
        // No warnings about unknown fields per VH-2
        Assert.DoesNotContain(result.Warnings, w => w.Message.Contains("unknown property"));
    }

    [Fact]
    public void Validate_SpecVersionInvalidFormat_ReportsError()
    {
        // VH-1: Must be Major.Minor format
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "v1",
            Entries = [new Models.CatalogEntry
            {
                Identifier = "urn:test:entry",
                DisplayName = "Test",
                MediaType = "application/json",
                Url = "https://example.com/test.json"
            }]
        };

        var result = AiCatalogValidator.Validate(catalog);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("Major.Minor"));
    }

    [Fact]
    public void Validate_ExtensionsRequireNamespacedKeys()
    {
        // ME-2: Metadata keys MUST be non-empty strings
        var json = """
        {
            "specVersion": "0.9",
            "entries": [
                {
                    "identifier": "urn:test:entry",
                    "type": "application/json",
                    "url": "https://example.com/test.json"
                }
            ],
            "extensions": {"": "empty key value"}
        }
        """;
        var catalog = AiCatalogParser.Parse(json);
        var result = AiCatalogValidator.Validate(catalog);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("extension key") && e.Message.Contains("namespaced"));
    }

    [Fact]
    public void Validate_MetadataValidKeys_Accepted()
    {
        var json = """
        {
            "specVersion": "0.9",
            "entries": [
                {
                    "identifier": "urn:test:entry",
                    "type": "application/json",
                    "url": "https://example.com/test.json"
                }
            ],
            "extensions": {"com.example.key": "value", "https://example.com/ext": 42}
        }
        """;
        var catalog = AiCatalogParser.Parse(json);
        var result = AiCatalogValidator.Validate(catalog);

        Assert.True(result.IsValid);
    }
}
