using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Security.Cryptography;
using System.Text.Json;
using SpecWorks.AiCatalog.Parsing;
using SpecWorks.AiCatalog.Validation;

namespace SpecWorks.AiCatalog.Cli.Commands;

/// <summary>
/// The <c>validate</c> command — validates an AI Catalog document and local artifacts.
/// </summary>
public static class ValidateCommand
{
    private static readonly HashSet<string> s_knownMediaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/ai-catalog+json",
        "application/ai-skills+zip",
        "text/markdown; profile=ai-skill",
        "application/vnd.mcp.server-card+json",
        "application/json",
        "text/markdown",
    };

    /// <summary>
    /// Creates the <c>validate</c> command.
    /// </summary>
    public static Command Create()
    {
        var inputArgument = new Argument<FileInfo>("input", "Path to ai-catalog.json file");
        var strictOption = new Option<bool>("--strict", "Treat warnings as errors");

        var cmd = new Command("validate", "Validate an AI Catalog document and its local artifacts")
        {
            inputArgument,
            strictOption
        };

        cmd.SetHandler((InvocationContext context) =>
        {
            var inputFile = context.ParseResult.GetValueForArgument(inputArgument);
            var strict = context.ParseResult.GetValueForOption(strictOption);
            var stdout = context.Console.Out;
            var stderr = context.Console.Error;

            if (inputFile is null || !inputFile.Exists)
            {
                WriteLine(stderr, $"Error: file not found: {inputFile?.FullName ?? "(null)"}");
                context.ExitCode = 1;
                return;
            }

            try
            {
                using var stream = inputFile.OpenRead();
                var catalog = AiCatalogParser.Parse(stream);
                var validationResult = AiCatalogValidator.Validate(catalog);

                var errors = validationResult.Errors.ToList();
                var warnings = validationResult.Warnings.ToList();
                var inputDirectory = inputFile.DirectoryName ?? Directory.GetCurrentDirectory();

                ValidateEntries(catalog, inputDirectory, errors, warnings);

                WriteLine(stdout, $"Conformance level: {validationResult.ConformanceLevel}");

                if (errors.Count > 0)
                {
                    PrintDiagnostics(stderr, "Errors", errors);
                }

                if (warnings.Count > 0)
                {
                    PrintDiagnostics(stdout, "Warnings", warnings);
                }

                WriteLine(stdout, $"{errors.Count} errors, {warnings.Count} warnings");
                context.ExitCode = errors.Count > 0 || (strict && warnings.Count > 0) ? 1 : 0;
            }
            catch (Exception ex) when (ex is AiCatalogException or IOException or UnauthorizedAccessException)
            {
                WriteLine(stderr, $"Error: {ex.Message}");
                context.ExitCode = 1;
            }
        });

        return cmd;
    }

    private static void ValidateEntries(Models.AiCatalog catalog, string inputDirectory, List<ValidationDiagnostic> errors, List<ValidationDiagnostic> warnings)
    {
        // Check for duplicate identifiers
        var seenIdentifiers = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < catalog.Entries.Count; i++)
        {
            var id = catalog.Entries[i].Identifier;
            if (string.IsNullOrWhiteSpace(id))
                continue;

            if (seenIdentifiers.TryGetValue(id, out var firstIndex))
            {
                errors.Add(new ValidationDiagnostic(
                    DiagnosticSeverity.Error,
                    $"Duplicate identifier '{id}' (first seen at entries[{firstIndex}])",
                    $"entries[{i}].identifier"));
            }
            else
            {
                seenIdentifiers[id] = i;
            }
        }

        for (int i = 0; i < catalog.Entries.Count; i++)
        {
            var entry = catalog.Entries[i];
            var entryPrefix = $"entries[{i}]";

            if (!IsKnownMediaType(entry.MediaType))
            {
                warnings.Add(new ValidationDiagnostic(
                    DiagnosticSeverity.Warning,
                    $"Unknown media type '{entry.MediaType}'",
                    $"{entryPrefix}.mediaType"));
            }

            if (string.IsNullOrWhiteSpace(entry.Url) || !IsLocalUrl(entry.Url))
            {
                continue;
            }

            if (!TryResolvePath(inputDirectory, entry.Url, out var resolvedPath, out var resolveError))
            {
                errors.Add(new ValidationDiagnostic(
                    DiagnosticSeverity.Error,
                    $"Invalid local path '{entry.Url}': {resolveError}",
                    $"{entryPrefix}.url"));
                continue;
            }

            var fileExists = File.Exists(resolvedPath);
            var directoryExists = Directory.Exists(resolvedPath);
            if (!fileExists && !directoryExists)
            {
                errors.Add(new ValidationDiagnostic(
                    DiagnosticSeverity.Error,
                    $"Referenced local path does not exist: {resolvedPath}",
                    $"{entryPrefix}.url"));
                continue;
            }

            if (!fileExists || !TryGetDigest(entry.Metadata, out var digest))
            {
                continue;
            }

            if (!digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new ValidationDiagnostic(
                    DiagnosticSeverity.Error,
                    $"Unsupported digest format '{digest}'; expected sha256:hexvalue",
                    $"{entryPrefix}.metadata.digest"));
                continue;
            }

            var actualDigest = ComputeDigest(resolvedPath);
            if (!string.Equals(digest, actualDigest, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new ValidationDiagnostic(
                    DiagnosticSeverity.Error,
                    $"Digest mismatch for '{resolvedPath}': expected {digest}, actual {actualDigest}",
                    $"{entryPrefix}.metadata.digest"));
            }
        }
    }

    private static bool IsKnownMediaType(string mediaType)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
        {
            return true;
        }

        return s_knownMediaTypes.Contains(mediaType)
            || mediaType.Contains("a2a", StringComparison.OrdinalIgnoreCase)
            || mediaType.Contains("agent-card", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLocalUrl(string url)
    {
        return !url.StartsWith("http", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryResolvePath(string inputDirectory, string url, out string resolvedPath, out string? error)
    {
        try
        {
            var normalizedPath = url.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            resolvedPath = Path.IsPathRooted(normalizedPath)
                ? Path.GetFullPath(normalizedPath)
                : Path.GetFullPath(Path.Combine(inputDirectory, normalizedPath));
            error = null;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            resolvedPath = string.Empty;
            error = ex.Message;
            return false;
        }
    }

    private static bool TryGetDigest(JsonElement? metadata, out string digest)
    {
        digest = string.Empty;

        if (!metadata.HasValue || metadata.Value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!metadata.Value.TryGetProperty("digest", out var digestElement) || digestElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        digest = digestElement.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(digest);
    }

    private static string ComputeDigest(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private static void PrintDiagnostics(IStandardStreamWriter writer, string heading, IReadOnlyList<ValidationDiagnostic> diagnostics)
    {
        WriteLine(writer);
        WriteLine(writer, $"{heading}:");

        foreach (var diagnostic in diagnostics)
        {
            if (string.IsNullOrWhiteSpace(diagnostic.Path))
            {
                WriteLine(writer, $"  - {diagnostic.Message}");
            }
            else
            {
                WriteLine(writer, $"  - {diagnostic.Path}: {diagnostic.Message}");
            }
        }
    }

    private static void WriteLine(IStandardStreamWriter writer, string text = "")
    {
        writer.Write(text + Environment.NewLine);
    }
}
