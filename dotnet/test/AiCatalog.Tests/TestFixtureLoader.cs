using System.Text.Json;

namespace SpecWorks.AiCatalog.Tests;

/// <summary>
/// Loads shared test fixtures from the testcases/ directory.
/// </summary>
internal static class TestFixtureLoader
{
    private static string TestCasesDir
    {
        get
        {
            // Look for testcases in output directory first (copied by csproj)
            var outputDir = Path.Combine(AppContext.BaseDirectory, "testcases");
            if (Directory.Exists(outputDir))
                return outputDir;

            // Fallback: walk up from the test project directory
            var dir = AppContext.BaseDirectory;
            for (int i = 0; i < 10; i++)
            {
                var candidate = Path.Combine(dir, "testcases");
                if (Directory.Exists(candidate))
                    return candidate;
                dir = Path.GetDirectoryName(dir)!;
            }

            throw new DirectoryNotFoundException("Could not find testcases/ directory");
        }
    }

    // Fixtures that are not AI Catalog documents (used for conversion tests only)
    private static readonly HashSet<string> s_conversionOnlyFixtures = new(StringComparer.OrdinalIgnoreCase)
    {
        "marketplace-input",
        "marketplace-expected",
    };

    public static IEnumerable<object[]> GetPositiveTestCases()
    {
        var dir = TestCasesDir;
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            var filename = Path.GetFileNameWithoutExtension(file);

            // Skip conversion-only fixtures (not AI Catalog documents)
            if (s_conversionOnlyFixtures.Contains(filename))
                continue;

            var json = File.ReadAllText(file);

            JsonDocument doc;
            try { doc = JsonDocument.Parse(json); }
            catch { continue; }

            using (doc)
            {
                // Positive test cases have "expected" (not "expected_error")
                if (!doc.RootElement.TryGetProperty("expected", out _))
                    continue;
            }

            yield return [filename, json];
        }
    }

    public static IEnumerable<object[]> GetNegativeTestCases()
    {
        var negDir = Path.Combine(TestCasesDir, "negative");
        if (!Directory.Exists(negDir))
            yield break;

        foreach (var file in Directory.EnumerateFiles(negDir, "*.json"))
        {
            var filename = Path.GetFileNameWithoutExtension(file);
            var json = File.ReadAllText(file);

            JsonDocument doc;
            try { doc = JsonDocument.Parse(json); }
            catch { continue; } // invalid-json.txt won't parse

            using (doc)
            {
                if (!doc.RootElement.TryGetProperty("expected_error", out _))
                    continue;
            }

            yield return [filename, json];
        }
    }

    public static IEnumerable<object[]> GetNegativeTestCasesIncludingInvalidJson()
    {
        var negDir = Path.Combine(TestCasesDir, "negative");
        if (!Directory.Exists(negDir))
            yield break;

        foreach (var file in Directory.EnumerateFiles(negDir, "*.*"))
        {
            var filename = Path.GetFileNameWithoutExtension(file);
            var content = File.ReadAllText(file);
            yield return [filename, content];
        }
    }

    public static JsonElement GetInput(string fixtureJson)
    {
        using var doc = JsonDocument.Parse(fixtureJson);
        return doc.RootElement.GetProperty("input").Clone();
    }

    public static string GetInputJson(string fixtureJson)
    {
        using var doc = JsonDocument.Parse(fixtureJson);
        return doc.RootElement.GetProperty("input").GetRawText();
    }

    public static JsonElement GetExpected(string fixtureJson)
    {
        using var doc = JsonDocument.Parse(fixtureJson);
        return doc.RootElement.GetProperty("expected").Clone();
    }

    public static string GetExpectedError(string fixtureJson)
    {
        using var doc = JsonDocument.Parse(fixtureJson);
        return doc.RootElement.GetProperty("expected_error").GetString()!;
    }
}
