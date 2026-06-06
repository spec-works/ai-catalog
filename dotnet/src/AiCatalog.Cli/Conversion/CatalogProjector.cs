using System.Text.Json;
using SpecWorks.AiCatalog.Models;
using SpecWorks.AiCatalog.Parsing;

namespace SpecWorks.AiCatalog.Cli.Conversion;

/// <summary>
/// Base class for projecting AI Catalog entries into platform-specific plugin marketplace layouts.
/// </summary>
public abstract class CatalogProjector
{
    private const string SkillDocumentName = "SKILL.md";

    protected CatalogProjector(Models.AiCatalog catalog, string inputFilePath, string outputDirectory, string sourceDirectory, HttpClient httpClient)
    {
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        InputFilePath = Path.GetFullPath(inputFilePath);
        OutputDirectory = Path.GetFullPath(outputDirectory);
        SourceDirectory = Path.GetFullPath(sourceDirectory);
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public string PlatformName => GetType().Name.Replace("Projector", string.Empty, StringComparison.Ordinal).ToLowerInvariant();

    protected Models.AiCatalog Catalog { get; }

    protected string InputFilePath { get; }

    protected string OutputDirectory { get; }

    protected string SourceDirectory { get; }

    protected HttpClient HttpClient { get; }

    protected string CatalogName => GetCatalogName();

    protected string CatalogDisplayName => Catalog.Host?.DisplayName ?? CatalogName;

    protected string CatalogDescription => GetCatalogDescription();

    protected string CatalogVersion => GetCatalogVersion();

    protected string PublisherName => GetPublisherName();

    protected string? PublisherIdentifier => Catalog.Entries
        .Select(entry => entry.Publisher?.Identifier)
        .FirstOrDefault(identifier => !string.IsNullOrWhiteSpace(identifier));

    public abstract Task ProjectAsync(CancellationToken cancellationToken = default);

    public static async Task<Models.AiCatalog> LoadCatalogAsync(string inputFilePath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(inputFilePath);
        return AiCatalogParser.Parse(stream);
    }

    protected async Task<IReadOnlyList<ResolvedSkill>> ResolveSkillsAsync(CatalogEntry entry, CancellationToken cancellationToken = default)
    {
        var skillNames = GetSkillNames(entry);
        var resolvedSkills = new List<ResolvedSkill>(skillNames.Count);

        foreach (var skillName in skillNames)
        {
            var content = await ResolveSkillContentAsync(entry, skillName, cancellationToken);
            resolvedSkills.Add(new ResolvedSkill(skillName, content));
        }

        return resolvedSkills;
    }

    protected async Task<string> ResolveSkillContentAsync(CatalogEntry entry, string skillName, CancellationToken cancellationToken = default)
    {
        if (entry.Data.HasValue)
        {
            var data = entry.Data.Value;
            if (TryGetNestedCatalog(data, out var nestedCatalog))
            {
                var nestedEntry = nestedCatalog.Entries.FirstOrDefault(candidate =>
                    string.Equals(GetNestedSkillName(candidate), skillName, StringComparison.OrdinalIgnoreCase));
                if (nestedEntry != null)
                {
                    if (TryExtractInlineSkillContent(nestedEntry.Data, out var nestedInlineContent))
                    {
                        return nestedInlineContent;
                    }

                    if (!string.IsNullOrWhiteSpace(nestedEntry.Url))
                    {
                        return await ResolveSkillContentFromLocationAsync(nestedEntry.Url!, entry.Url, skillName, cancellationToken);
                    }
                }
            }

            if (data.ValueKind == JsonValueKind.Object
                && data.TryGetProperty("skills", out var skillsElement)
                && skillsElement.ValueKind == JsonValueKind.Object
                && skillsElement.TryGetProperty(skillName, out var skillElement)
                && TryExtractInlineSkillContent(skillElement, out var skillContent))
            {
                return skillContent;
            }

            if (TryExtractInlineSkillContent(data, out var inlineContent))
            {
                return inlineContent;
            }
        }

        if (string.IsNullOrWhiteSpace(entry.Url))
        {
            throw new AiCatalogException($"Entry '{entry.DisplayName}' does not define a source URL or embedded skill content for '{skillName}'.");
        }

        return await ResolveSkillContentFromLocationAsync(entry.Url, null, skillName, cancellationToken);
    }

    protected async Task WriteJsonFileAsync(string path, object value, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? OutputDirectory);
        var json = JsonSerializer.Serialize(value, SerializerOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    protected async Task WriteSkillsAsync(string pluginsRoot, CatalogEntry entry, IReadOnlyList<ResolvedSkill> skills, CancellationToken cancellationToken = default)
    {
        foreach (var skill in skills)
        {
            var skillPath = Path.Combine(pluginsRoot, GetPluginName(entry), "skills", skill.Name, SkillDocumentName);
            Directory.CreateDirectory(Path.GetDirectoryName(skillPath)!);
            await File.WriteAllTextAsync(skillPath, skill.Content, cancellationToken);
        }
    }

    protected static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = null,
        WriteIndented = true,
    };

    protected string GetPluginName(CatalogEntry entry)
    {
        var rawName = !string.IsNullOrWhiteSpace(entry.DisplayName)
            ? entry.DisplayName
            : entry.Identifier;
        return SanitizeName(rawName);
    }

    protected string GetEntryDescription(CatalogEntry entry) => entry.Description ?? string.Empty;

    protected string GetEntryVersion(CatalogEntry entry) => string.IsNullOrWhiteSpace(entry.Version) ? "1.0.0" : entry.Version;

    protected IReadOnlyList<string> GetKeywords(CatalogEntry entry) => (entry.Tags ?? [])
        .Where(tag => !string.IsNullOrWhiteSpace(tag))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private IReadOnlyList<string> GetSkillNames(CatalogEntry entry)
    {
        var tags = (entry.Tags ?? [])
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (tags.Count > 0)
        {
            return tags;
        }

        if (entry.Data.HasValue && TryGetNestedCatalog(entry.Data.Value, out var nestedCatalog))
        {
            return nestedCatalog.Entries
                .Select(GetNestedSkillName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()!;
        }

        if (entry.Data.HasValue
            && entry.Data.Value.ValueKind == JsonValueKind.Object
            && entry.Data.Value.TryGetProperty("skills", out var skillsElement)
            && skillsElement.ValueKind == JsonValueKind.Object)
        {
            return skillsElement.EnumerateObject()
                .Select(property => property.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return Array.Empty<string>();
    }

    private async Task<string> ResolveSkillContentFromLocationAsync(string location, string? parentLocation, string skillName, CancellationToken cancellationToken)
    {
        foreach (var filePath in GetLocalSkillCandidates(location, parentLocation, skillName))
        {
            if (File.Exists(filePath))
            {
                return await File.ReadAllTextAsync(filePath, cancellationToken);
            }
        }

        foreach (var uri in GetRemoteSkillCandidates(location, parentLocation, skillName))
        {
            using var response = await HttpClient.GetAsync(uri, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync(cancellationToken);
            }
        }

        throw new AiCatalogException($"Could not resolve skill '{skillName}' from source '{location}'. Checked local source directory '{SourceDirectory}' and any reachable remote URLs.");
    }

    private IEnumerable<string> GetLocalSkillCandidates(string location, string? parentLocation, string skillName)
    {
        if (Path.IsPathRooted(location))
        {
            foreach (var candidate in ExpandLocalCandidates(location, skillName))
            {
                yield return candidate;
            }

            yield break;
        }

        foreach (var basePath in GetRelativeLocationBases(location, parentLocation))
        {
            foreach (var candidate in ExpandLocalCandidates(basePath, skillName))
            {
                yield return candidate;
            }
        }
    }

    private IEnumerable<string> ExpandLocalCandidates(string basePath, string skillName)
    {
        var normalizedBasePath = basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (normalizedBasePath.EndsWith(SkillDocumentName, StringComparison.OrdinalIgnoreCase))
        {
            yield return normalizedBasePath;
            yield break;
        }

        yield return Path.Combine(normalizedBasePath, SkillDocumentName);
        yield return Path.Combine(normalizedBasePath, "skills", skillName, SkillDocumentName);
    }

    private IEnumerable<string> GetRelativeLocationBases(string location, string? parentLocation)
    {
        var normalizedLocation = NormalizeRelativePath(location);
        yield return Path.Combine(SourceDirectory, normalizedLocation);

        if (!string.IsNullOrWhiteSpace(parentLocation) && !IsRemoteLocation(parentLocation!))
        {
            yield return Path.Combine(SourceDirectory, NormalizeRelativePath(parentLocation!), normalizedLocation);
        }
    }

    private IEnumerable<Uri> GetRemoteSkillCandidates(string location, string? parentLocation, string skillName)
    {
        if (TryCreateRemoteUri(location, out var absoluteLocation))
        {
            foreach (var candidate in ExpandRemoteCandidates(absoluteLocation, skillName))
            {
                yield return candidate;
            }
        }

        if (!string.IsNullOrWhiteSpace(parentLocation)
            && TryCreateRemoteUri(parentLocation!, out var parentUri)
            && !TryCreateRemoteUri(location, out _))
        {
            var relativeUri = new Uri(EnsureTrailingSlash(parentUri), NormalizeRelativePath(location).Replace(Path.DirectorySeparatorChar, '/'));
            foreach (var candidate in ExpandRemoteCandidates(relativeUri, skillName))
            {
                yield return candidate;
            }
        }
    }

    private IEnumerable<Uri> ExpandRemoteCandidates(Uri baseUri, string skillName)
    {
        var normalizedBase = baseUri.AbsoluteUri.TrimEnd('/');
        if (normalizedBase.EndsWith(SkillDocumentName, StringComparison.OrdinalIgnoreCase))
        {
            yield return baseUri;
            yield break;
        }

        yield return new Uri(EnsureTrailingSlash(baseUri), SkillDocumentName);
        yield return new Uri(EnsureTrailingSlash(baseUri), $"skills/{Uri.EscapeDataString(skillName)}/{SkillDocumentName}");
    }

    private static bool TryExtractInlineSkillContent(JsonElement? element, out string content)
    {
        content = string.Empty;
        if (!element.HasValue)
        {
            return false;
        }

        var value = element.Value;
        if (value.ValueKind == JsonValueKind.String)
        {
            content = value.GetString() ?? string.Empty;
            return true;
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            if (value.TryGetProperty("content", out var contentElement) && contentElement.ValueKind == JsonValueKind.String)
            {
                content = contentElement.GetString() ?? string.Empty;
                return true;
            }

            if (value.TryGetProperty("markdown", out var markdownElement) && markdownElement.ValueKind == JsonValueKind.String)
            {
                content = markdownElement.GetString() ?? string.Empty;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetNestedCatalog(JsonElement data, out Models.AiCatalog nestedCatalog)
    {
        nestedCatalog = default!;
        if (data.ValueKind != JsonValueKind.Object || !data.TryGetProperty("entries", out _))
        {
            return false;
        }

        try
        {
            nestedCatalog = AiCatalogParser.Parse(data.GetRawText());
            return true;
        }
        catch (AiCatalogException)
        {
            return false;
        }
    }

    private static string GetNestedSkillName(CatalogEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.DisplayName))
        {
            return entry.DisplayName;
        }

        if (!string.IsNullOrWhiteSpace(entry.Url))
        {
            var trimmed = entry.Url!.TrimEnd('/', '\\');
            var lastSlash = Math.Max(trimmed.LastIndexOf('/'), trimmed.LastIndexOf('\\'));
            return lastSlash >= 0 ? trimmed[(lastSlash + 1)..] : trimmed;
        }

        var identifierParts = entry.Identifier.Split(':', StringSplitOptions.RemoveEmptyEntries);
        return identifierParts.Length > 0 ? identifierParts[^1] : entry.Identifier;
    }

    private string GetCatalogName()
    {
        if (Catalog.Metadata.HasValue
            && Catalog.Metadata.Value.ValueKind == JsonValueKind.Object
            && Catalog.Metadata.Value.TryGetProperty("name", out var nameElement)
            && nameElement.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(nameElement.GetString()))
        {
            return SanitizeName(nameElement.GetString()!);
        }

        if (!string.IsNullOrWhiteSpace(Catalog.Host?.Identifier))
        {
            var identifier = Catalog.Host.Identifier!;
            if (Uri.TryCreate(identifier, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
            {
                return SanitizeName(uri.Host);
            }

            return SanitizeName(identifier);
        }

        if (!string.IsNullOrWhiteSpace(Catalog.Host?.DisplayName))
        {
            return SanitizeName(Catalog.Host.DisplayName);
        }

        return SanitizeName(Path.GetFileNameWithoutExtension(InputFilePath));
    }

    private string GetCatalogDescription()
    {
        if (Catalog.Metadata.HasValue
            && Catalog.Metadata.Value.ValueKind == JsonValueKind.Object
            && Catalog.Metadata.Value.TryGetProperty("description", out var descriptionElement)
            && descriptionElement.ValueKind == JsonValueKind.String)
        {
            return descriptionElement.GetString() ?? string.Empty;
        }

        return $"Generated from {Path.GetFileName(InputFilePath)}";
    }

    private string GetCatalogVersion()
    {
        if (Catalog.Metadata.HasValue
            && Catalog.Metadata.Value.ValueKind == JsonValueKind.Object
            && Catalog.Metadata.Value.TryGetProperty("version", out var versionElement)
            && versionElement.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(versionElement.GetString()))
        {
            return versionElement.GetString()!;
        }

        return "1.0.0";
    }

    private string GetPublisherName()
    {
        var publisherName = Catalog.Entries
            .Select(entry => entry.Publisher?.DisplayName)
            .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

        return publisherName
            ?? Catalog.Host?.DisplayName
            ?? CatalogDisplayName;
    }

    private static bool IsRemoteLocation(string location) => TryCreateRemoteUri(location, out _);

    private static bool TryCreateRemoteUri(string location, out Uri uri)
    {
        if (Uri.TryCreate(location, UriKind.Absolute, out uri!))
        {
            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            {
                return true;
            }
        }

        uri = null!;
        return false;
    }

    private static Uri EnsureTrailingSlash(Uri uri)
    {
        var absoluteUri = uri.AbsoluteUri;
        return absoluteUri.EndsWith("/", StringComparison.Ordinal)
            ? uri
            : new Uri($"{absoluteUri}/");
    }

    private static string NormalizeRelativePath(string path) => path.TrimStart('.', '/', '\\')
        .Replace('/', Path.DirectorySeparatorChar)
        .Replace('\\', Path.DirectorySeparatorChar);

    private static string SanitizeName(string value)
    {
        var sanitized = new string(value
            .Trim()
            .Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.' ? ch : '-')
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "catalog" : sanitized;
    }

    protected sealed record ResolvedSkill(string Name, string Content);
}
