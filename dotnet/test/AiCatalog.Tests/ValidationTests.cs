using System.Text.Json;
using SpecWorks.AiCatalog.Parsing;
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
            SpecVersion = "1.0",
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
            SpecVersion = "1.0",
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
    public void Validate_TrustedCatalog_IsTrustedLevel()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "1.0",
            Host = new Models.HostInfo
            {
                DisplayName = "Test Host",
                TrustManifest = new Models.TrustManifest { Identity = "did:web:test.com" }
            },
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
        Assert.Equal(ConformanceLevel.Trusted, result.ConformanceLevel);
    }

    [Fact]
    public void Validate_MissingEntryContent_ReportsError()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "1.0",
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
            SpecVersion = "1.0",
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
    public void Validate_DuplicateIdentifierNoVersion_ReportsError()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "1.0",
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
    public void Validate_TrustIdentityMismatch_ReportsError()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "1.0",
            Entries = [new Models.CatalogEntry
            {
                Identifier = "urn:test:real",
                DisplayName = "Real Agent",
                MediaType = "application/json",
                Url = "https://example.com/real.json",
                TrustManifest = new Models.TrustManifest { Identity = "urn:test:different" }
            }]
        };

        var result = AiCatalogValidator.Validate(catalog);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("does not match"));
    }

    [Fact]
    public void Validate_WeakDigest_ReportsError()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "1.0",
            Host = new Models.HostInfo { DisplayName = "Test" },
            Entries = [new Models.CatalogEntry
            {
                Identifier = "urn:test:weak",
                DisplayName = "Weak Digest",
                MediaType = "application/json",
                Url = "https://example.com/weak.json",
                TrustManifest = new Models.TrustManifest
                {
                    Identity = "urn:test:weak",
                    Attestations = [new Models.Attestation
                    {
                        Type = "Audit",
                        Uri = "https://example.com/audit.pdf",
                        MediaType = "application/pdf",
                        Digest = "sha1:abc123"
                    }]
                }
            }]
        };

        var result = AiCatalogValidator.Validate(catalog);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("sha1") && e.Message.Contains("not accepted"));
    }

    [Fact]
    public void Validate_ValidateAgainstLevel_ReportsErrorsForLevel()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "1.0",
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
    public void Validate_HttpUrl_ReportsError()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "1.0",
            Entries = [new Models.CatalogEntry
            {
                Identifier = "urn:test:insecure",
                DisplayName = "Insecure",
                MediaType = "application/json",
                Url = "http://example.com/insecure.json"
            }]
        };

        var result = AiCatalogValidator.Validate(catalog);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("HTTP") && e.Message.Contains("HTTPS"));
    }

    [Fact]
    public void Validate_InvalidUpdatedAt_ReportsError()
    {
        var catalog = new Models.AiCatalog
        {
            SpecVersion = "1.0",
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
        Assert.Contains(result.Errors, e => e.Message.Contains("RFC 3339") && e.Message.Contains("date-only"));
    }

    [Fact]
    public void Validate_UnknownTopLevelFields_SilentlyIgnored()
    {
        // VH-2: Unknown fields within same major version MUST be ignored
        var json = """
        {
            "specVersion": "1.0",
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
    public void Validate_MetadataEmptyKey_ReportsError()
    {
        // ME-2: Metadata keys MUST be non-empty strings
        var json = """
        {
            "specVersion": "1.0",
            "entries": [
                {
                    "identifier": "urn:test:entry",
                    "displayName": "Test",
                    "mediaType": "application/json",
                    "url": "https://example.com/test.json"
                }
            ],
            "metadata": {"": "empty key value"}
        }
        """;
        var catalog = AiCatalogParser.Parse(json);
        var result = AiCatalogValidator.Validate(catalog);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("metadata key") && e.Message.Contains("non-empty"));
    }

    [Fact]
    public void Validate_MetadataValidKeys_Accepted()
    {
        var json = """
        {
            "specVersion": "1.0",
            "entries": [
                {
                    "identifier": "urn:test:entry",
                    "displayName": "Test",
                    "mediaType": "application/json",
                    "url": "https://example.com/test.json"
                }
            ],
            "metadata": {"com.example.key": "value", "simple": 42}
        }
        """;
        var catalog = AiCatalogParser.Parse(json);
        var result = AiCatalogValidator.Validate(catalog);

        Assert.True(result.IsValid);
    }
}
