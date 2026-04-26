using System.Text.Json;
using SpecWorks.AiCatalog.Parsing;
using SpecWorks.AiCatalog.Validation;
using Xunit;

namespace SpecWorks.AiCatalog.Tests;

/// <summary>
/// Tests that negative test fixtures produce the expected parse or validation errors.
/// </summary>
public class NegativeParsingTests
{
    public static IEnumerable<object[]> NegativeFixtures => TestFixtureLoader.GetNegativeTestCases();

    [Theory]
    [MemberData(nameof(NegativeFixtures))]
    public void ParseOrValidate_NegativeFixture_ProducesExpectedError(string name, string fixtureJson)
    {
        var expectedError = TestFixtureLoader.GetExpectedError(fixtureJson);
        var inputJson = TestFixtureLoader.GetInputJson(fixtureJson);

        // Some errors are caught at parse time, others at validation time
        try
        {
            var catalog = AiCatalogParser.Parse(inputJson);

            // Parse succeeded — error must come from validation
            var result = AiCatalogValidator.Validate(catalog);

            var allMessages = result.Errors.Select(e => e.Message)
                .Concat(result.Warnings.Select(w => w.Message))
                .ToList();

            Assert.True(allMessages.Count > 0,
                $"Expected error/warning containing '{expectedError}' for test '{name}', but validation produced no diagnostics.");

            Assert.Contains(allMessages, msg => msg.Contains(expectedError, StringComparison.OrdinalIgnoreCase)
                || expectedError.Contains(msg, StringComparison.OrdinalIgnoreCase)
                || ErrorsMatchSemantically(msg, expectedError));
        }
        catch (AiCatalogParseException ex)
        {
            // Parse failed — the error message should match
            Assert.True(
                ex.Message.Contains(expectedError, StringComparison.OrdinalIgnoreCase)
                || expectedError.Contains(ex.Message, StringComparison.OrdinalIgnoreCase)
                || ErrorsMatchSemantically(ex.Message, expectedError),
                $"Parse error for '{name}': expected message containing '{expectedError}' but got '{ex.Message}'");
        }
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsParseException()
    {
        var invalidContent = "This is not valid JSON at all.";
        Assert.Throws<AiCatalogParseException>(() => AiCatalogParser.Parse(invalidContent));
    }

    [Fact]
    public void Parse_RootArray_ThrowsParseException()
    {
        var json = """[{"specVersion":"1.0","entries":[]}]""";
        var ex = Assert.Throws<AiCatalogParseException>(() => AiCatalogParser.Parse(json));
        Assert.Contains("root document must be a JSON object", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Semantic match: checks if both messages refer to the same field/concept error.
    /// </summary>
    private static bool ErrorsMatchSemantically(string actual, string expected)
    {
        // Normalize for comparison
        var normalizedActual = actual.ToLowerInvariant();
        var normalizedExpected = expected.ToLowerInvariant();

        // Key phrases to check for overlap
        string[] keyPhrases = [
            "missing required field",
            "must be a string",
            "must be an array",
            "must not be empty",
            "must have exactly one",
            "duplicate",
            "does not match",
            "not accepted",
            "rfc 3339",
            "non-negative",
            "must be https",
            "http",
            "identity",
            "specversion",
            "entries",
            "displayname",
            "identifier",
            "mediatype",
            "url",
            "data",
            "inline",
            "trustmanifest",
            "attestation",
            "provenance",
            "publisher",
            "trustschema",
            "digest",
            "sha-256",
            "sha1",
            "sha256",
            "major.minor",
            "major version",
            "unsupported",
            "non-negative integers",
            "metadata key",
            "non-empty",
            "nested catalog",
            "depth",
            "nesting",
            "limit",
        ];

        // Check if they share enough key phrases
        int matches = keyPhrases.Count(p => normalizedActual.Contains(p) && normalizedExpected.Contains(p));
        return matches >= 2;
    }
}
