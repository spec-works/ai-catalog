using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using SpecWorks.AiCatalog.Parsing;
using SpecWorks.AiCatalog.Serialization;

namespace SpecWorks.AiCatalog.Cli.Commands;

/// <summary>
/// The <c>publish</c> command — packages a catalog for remote distribution.
/// </summary>
public static class PublishCommand
{
    /// <summary>
    /// Creates the <c>publish</c> command.
    /// </summary>
    public static Command Create()
    {
        var inputArgument = new Argument<FileInfo>("input", "Path to ai-catalog.json file");
        var outputOption = new Option<DirectoryInfo>("--output", "Output directory for packaged catalog artifacts")
        {
            IsRequired = true
        };
        outputOption.AddAlias("-o");

        var cmd = new Command("publish", "Package a catalog for remote distribution (zip, sign, host)")
        {
            inputArgument,
            outputOption
        };

        cmd.SetHandler(async (InvocationContext context) =>
        {
            var inputFile = context.ParseResult.GetValueForArgument(inputArgument);
            var outputDirectory = context.ParseResult.GetValueForOption(outputOption);

            if (outputDirectory == null)
            {
                Console.Error.WriteLine("Error: output directory is required");
                context.ExitCode = 1;
                return;
            }

            if (!inputFile.Exists)
            {
                Console.Error.WriteLine($"Error: file not found: {inputFile.FullName}");
                context.ExitCode = 1;
                return;
            }

            try
            {
                await using var stream = inputFile.OpenRead();
                var catalog = AiCatalogParser.Parse(stream);

                Directory.CreateDirectory(outputDirectory.FullName);
                var stats = PublishArtifacts(catalog, inputFile.FullName, outputDirectory.FullName);

                var outputCatalogPath = Path.Combine(outputDirectory.FullName, "ai-catalog.json");
                await File.WriteAllTextAsync(outputCatalogPath, AiCatalogSerializer.Serialize(catalog));

                Console.WriteLine(
                    $"Published {stats.LocalEntries} local artifact(s) ({stats.SkillFiles} skill file(s), {stats.Packages} package(s)); left {stats.RemoteEntries} remote URL(s) unchanged; wrote {outputCatalogPath}");
            }
            catch (Exception ex) when (ex is AiCatalogException or IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                context.ExitCode = 1;
            }
        });

        return cmd;
    }

    private static PublishStats PublishArtifacts(Models.AiCatalog catalog, string inputFilePath, string outputDirectory)
    {
        var inputDirectory = Path.GetDirectoryName(inputFilePath) ?? Directory.GetCurrentDirectory();
        var stats = new PublishStats();

        foreach (var entry in catalog.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Url))
            {
                continue;
            }

            if (IsRemoteUrl(entry.Url))
            {
                stats.RemoteEntries++;
                continue;
            }

            var sourcePath = ResolveSourcePath(inputDirectory, entry.Url);
            var slug = GetEntrySlug(entry.Identifier);
            PublishedArtifact artifact;

            if (File.Exists(sourcePath))
            {
                artifact = PublishSkillFile(sourcePath, outputDirectory, slug);
            }
            else if (Directory.Exists(sourcePath))
            {
                artifact = PublishSkillDirectory(sourcePath, outputDirectory, slug);
            }
            else
            {
                throw new AiCatalogException($"Referenced path not found for entry '{entry.Identifier}': {sourcePath}");
            }

            entry.Url = artifact.RelativeUrl;
            entry.MediaType = artifact.MediaType;
            entry.Metadata = AddDigest(entry.Metadata, artifact.Digest);

            stats.LocalEntries++;
            if (artifact.IsPackage)
            {
                stats.Packages++;
            }
            else
            {
                stats.SkillFiles++;
            }
        }

        return stats;
    }

    private static bool IsRemoteUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveSourcePath(string inputDirectory, string url)
    {
        var normalizedPath = url.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(inputDirectory, normalizedPath));
    }

    private static PublishedArtifact PublishSkillFile(string sourcePath, string outputDirectory, string slug)
    {
        var relativeUrl = $"skills/{slug}/SKILL.md";
        var destinationPath = Path.Combine(outputDirectory, "skills", slug, "SKILL.md");
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.Copy(sourcePath, destinationPath, overwrite: true);

        return CreatePublishedArtifact(destinationPath, relativeUrl, "text/markdown; profile=ai-skill", isPackage: false);
    }

    private static PublishedArtifact PublishSkillDirectory(string sourceDirectory, string outputDirectory, string slug)
    {
        if (IsSimpleSkillDirectory(sourceDirectory, out var skillFile))
        {
            return PublishSkillFile(skillFile, outputDirectory, slug);
        }

        var packagesDirectory = Path.Combine(outputDirectory, "packages");
        Directory.CreateDirectory(packagesDirectory);

        var zipPath = Path.Combine(packagesDirectory, $"{slug}.zip");
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        ZipFile.CreateFromDirectory(sourceDirectory, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
        return CreatePublishedArtifact(zipPath, $"packages/{slug}.zip", "application/ai-skills+zip", isPackage: true);
    }

    private static bool IsSimpleSkillDirectory(string sourceDirectory, out string skillFile)
    {
        var directories = Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories).ToArray();
        var files = Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories).ToArray();

        if (directories.Length == 0
            && files.Length == 1
            && string.Equals(Path.GetFileName(files[0]), "SKILL.md", StringComparison.OrdinalIgnoreCase))
        {
            skillFile = files[0];
            return true;
        }

        skillFile = string.Empty;
        return false;
    }

    private static PublishedArtifact CreatePublishedArtifact(string filePath, string relativeUrl, string mediaType, bool isPackage)
    {
        return new PublishedArtifact(relativeUrl, mediaType, ComputeDigest(filePath), isPackage);
    }

    private static string ComputeDigest(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private static JsonElement AddDigest(JsonElement? metadata, string digest)
    {
        JsonObject metadataObject;

        if (metadata.HasValue && metadata.Value.ValueKind == JsonValueKind.Object)
        {
            metadataObject = JsonNode.Parse(metadata.Value.GetRawText())?.AsObject() ?? new JsonObject();
        }
        else
        {
            metadataObject = new JsonObject();
        }

        metadataObject["digest"] = digest;
        return JsonSerializer.SerializeToElement(metadataObject);
    }

    private static string GetEntrySlug(string identifier)
    {
        var trimmed = identifier.Trim().TrimEnd('/', '\\', ':');
        var lastSeparator = Math.Max(trimmed.LastIndexOf(':'), Math.Max(trimmed.LastIndexOf('/'), trimmed.LastIndexOf('\\')));
        var candidate = lastSeparator >= 0 ? trimmed[(lastSeparator + 1)..] : trimmed;
        var sanitized = new string(candidate.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '-').ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "entry" : sanitized;
    }

    private sealed class PublishStats
    {
        public int LocalEntries { get; set; }

        public int RemoteEntries { get; set; }

        public int SkillFiles { get; set; }

        public int Packages { get; set; }
    }

    private sealed record PublishedArtifact(string RelativeUrl, string MediaType, string Digest, bool IsPackage);
}
